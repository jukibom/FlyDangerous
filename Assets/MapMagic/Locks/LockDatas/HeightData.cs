using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Terrains;
using MapMagic.Nodes;
using MapMagic.Nodes.MatrixGenerators;

namespace MapMagic.Locks
{
	public class HeightData : ILockData
	{
		public float[,] heightsArr;

		CoordCircle circle;
		int resolution; //to determine if it's changed and avoid writing. And to rescale terraindata on terrain reset


		public void Read (Terrain terrain, Lock lk) 
		{
			TerrainData terrainData = terrain.terrainData;

			resolution = terrainData.heightmapResolution;
			circle = new CoordCircle(terrain, resolution, lk.worldPos, lk.worldRadius, lk.worldTransition);

			heightsArr = terrainData.GetHeights(circle.rect.offset.x, circle.rect.offset.z, circle.rect.size.x, circle.rect.size.z);
		}


		public void WriteInThread (IApplyData applyData)
		{
			if (!(applyData is HeightOutput200.IApplyHeightData)) return;

			if (applyData.Resolution != resolution) return; //Don't perform lock if resolution changed

			Matrix terrainMatrix = new Matrix(circle.rect);
			ImportMatrix(terrainMatrix, applyData);

			Matrix lockMatrix = new Matrix(circle.rect);
			lockMatrix.ImportHeights(heightsArr);

			lockMatrix.ExtendCircular(circle.center, circle.radius-1, circle.transition+2, 50);
			terrainMatrix.BlendStamped(terrainMatrix, lockMatrix, circle.center.x, circle.center.z, circle.radius, circle.transition, smoothFallof:true);
			//terrainMatrix.Fill(lockMatrix);

			ExportMatrix(terrainMatrix, applyData);
		}


		public (Matrix heightSrc, Matrix heightDst) WriteWithHeightDelta (HeightOutput200.IApplyHeightData applyData)
		{
			Matrix terrainMatrix = new Matrix(circle.rect); //changed matrix with new height
			ImportMatrix(terrainMatrix, applyData);

			Matrix lockMatrix = new Matrix(circle.rect); //un-changed lock matrix
			lockMatrix.ImportHeights(heightsArr);

			Matrix stampMatrix = new Matrix(lockMatrix);
			float heightDelta = GetHeightDelta(lockMatrix, terrainMatrix);
			stampMatrix.Add(heightDelta);

			stampMatrix.ExtendCircular(circle.center, circle.radius-1, circle.transition+2, 50);
			terrainMatrix.BlendStamped(terrainMatrix, stampMatrix, circle.center.x, circle.center.z, circle.radius, circle.transition, smoothFallof:true);

			ExportMatrix(terrainMatrix, applyData);

			return (lockMatrix, terrainMatrix);
		}


		public void WriteInApply (Terrain terrain, bool resizeTerrain=false) 
		{ 
			if (heightsArr == null) return; // Don't perform lock if nothing is stored

			TerrainData terrainData = terrain.terrainData;

			if (terrain.terrainData.heightmapResolution != resolution)
			{
				if (resizeTerrain)
				{
					Vector3 prevSize = terrainData.size;
					terrainData.heightmapResolution = resolution;
					terrainData.size = prevSize;
				}

				else 
					return;
			}

			terrainData.SetHeights(circle.rect.offset.x, circle.rect.offset.z, heightsArr);
		}

		public void ApplyHeightDelta (Matrix src, Matrix dst) { }


		public void ResizeFrom (ILockData otherData)
		{
			HeightData other = (HeightData)otherData;

			Matrix otherMatrix = new Matrix(other.circle.rect);
			otherMatrix.ImportHeights(other.heightsArr);

			Matrix matrix = new Matrix(circle.rect);
			MatrixOps.Resize(otherMatrix, matrix);

			matrix.ExportHeights(heightsArr);
		}


		/*public float GetHeightDelta (Dictionary<Type,IApplyData> applyDatas)
		{
			float lockedAvg = GetAvgInCircle(heightsArr, circle.center-circle.rect.offset, circle.radius);
			Vector2 lockMinMax = GetMinMaxInRadius(heightsArr, circle.center-circle.rect.offset, circle.radius);

			Matrix terrainMatrix = GetApplyMatrix(applyDatas);
			if (terrainMatrix == null) return 0;
			float genAvg = GetAvgInCircle(terrainMatrix, circle.center, circle.radius);
			//TODO: read directly without loading matrix

			float heightDelta = genAvg - lockedAvg;
			if (lockMinMax.x + heightDelta < 0) heightDelta = 0 - lockMinMax.x; //lockMin
			if (lockMinMax.y + heightDelta > 1) heightDelta = 1 - lockMinMax.y; //lockMax

			return heightDelta;
		}*/


		private float GetHeightDelta (Matrix lockMatrix, Matrix genMatrix)
		{
			float lockAvg = GetAvgInCircle(lockMatrix, circle.center, circle.radius);
			Vector2 lockMinMax = GetMinMaxInRadius(lockMatrix, circle.center, circle.radius);

			float genAvg = GetAvgInCircle(genMatrix, circle.center, circle.radius);

			float heightDelta = genAvg - lockAvg;
			if (lockMinMax.x + heightDelta < 0) heightDelta = 0 - lockMinMax.x; //lockMin
			if (lockMinMax.y + heightDelta > 1) heightDelta = 1 - lockMinMax.y; //lockMax

			return heightDelta;
		}


		private static float GetAvgInCircle (Matrix matrix, Coord center, int radius)
		/// Gets average value of the circumference with given radius (not the internal area)
		{
			Coord min = center-radius; 
			Coord max = center+radius;

			float avgVal = 0;
			int sum = 0;

			int numSamples = (int)(Mathf.PI * radius * 2);
			float radStep = Mathf.PI*2 / numSamples;
			for (int i=0; i<numSamples; i++)
			{
				float radAngle = radStep * i;
				float dirX = Mathf.Sin(radAngle);
				float dirZ = Mathf.Cos(radAngle);

				int sX = center.x + (int)(dirX*(radius-2)); //to make sure evaluating in locked area
				int sZ = center.z + (int)(dirZ*(radius-2));

				if (sX < min.x || sX >= max.x || sZ < min.z || sZ >= max.z) continue;

				int pos = (sZ-matrix.rect.offset.z)*matrix.rect.size.x + sX - matrix.rect.offset.x;
				float val = matrix.arr[pos];

				avgVal += val;
				sum ++;
			}

			if (sum >= 0) avgVal /= sum;
			return avgVal;
		}


		private static float GetAvgInCircle (float[,] heightsArr, Coord center, int radius)
		/// Gets average value of the circumference with given radius (not the internal area)
		{
			Coord min = center-radius; 
			Coord max = center+radius;

			float avgVal = 0;
			int sum = 0;

			int numSamples = (int)(Mathf.PI * radius * 2);
			float radStep = Mathf.PI*2 / numSamples;
			for (int i=0; i<numSamples; i++)
			{
				float radAngle = radStep * i;
				float dirX = Mathf.Sin(radAngle);
				float dirZ = Mathf.Cos(radAngle);

				int sX = center.x + (int)(dirX*(radius-2)); //to make sure evaluating in locked area
				int sZ = center.z + (int)(dirZ*(radius-2));

				if (sX < min.x || sX >= max.x || sZ < min.z || sZ >= max.z) continue;

				float val = heightsArr[sX,sZ];

				avgVal += val;
				sum ++;
			}

			if (sum >= 0) avgVal /= sum;
			return avgVal;
		}


		private static Vector2 GetMinMaxInRadius (Matrix matrix, Coord center, int radius)
		/// Gets minimum and maximum within radius (including internal area)
		{
			Coord min = center-radius; 
			Coord max = center+radius;
			
			float minVal = 200000000;
			float maxVal = -200000000;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					float dist = Mathf.Sqrt((x-center.x)*(x-center.x) + (z-center.z)*(z-center.z));
					if (dist > radius-1) continue;

					int pos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
					float val = matrix.arr[pos];

					//float val = data.heights2D[z-data.offset.z, x-data.offset.x]; //x and z are swapped

					if (val < minVal) minVal = val;
					if (val > maxVal) maxVal = val;
				}
			
			return new Vector2(minVal, maxVal);
		}


		private static Vector2 GetMinMaxInRadius (float[,] heightsArr, Coord center, int radius)
		/// Gets minimum and maximum within radius (including internal area)
		{
			Coord min = center-radius; 
			Coord max = center+radius;
			
			float minVal = 200000000;
			float maxVal = -200000000;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					float dist = Mathf.Sqrt((x-center.x)*(x-center.x) + (z-center.z)*(z-center.z));
					if (dist > radius-1) continue;

					float val = heightsArr[x,z];

					//float val = data.heights2D[z-data.offset.z, x-data.offset.x]; //x and z are swapped

					if (val < minVal) minVal = val;
					if (val > maxVal) maxVal = val;
				}
			
			return new Vector2(minVal, maxVal);
		}


		private static void ImportMatrix (Matrix terrainMatrix, IApplyData applyData)
		{
			if (applyData is HeightOutput200.ApplySetData heightSetData)
				terrainMatrix.ImportHeights(heightSetData.heights2D, new Coord(0,0));

			else if (applyData is HeightOutput200.ApplySplitData heightSplitData)
				terrainMatrix.ImportHeightStrips(heightSplitData.heights2DSplits, new Coord(0,0));
			
			#if UNITY_2019_1_OR_NEWER
			else if (applyData is HeightOutput200.ApplyTexData heightTexData)
				terrainMatrix.ImportRawFloat(heightTexData.texBytes, new Coord(0,0), new Coord(heightTexData.res,heightTexData.res), mult:2);
			#endif
		}

		private static void ExportMatrix (Matrix terrainMatrix, IApplyData applyData)
		{
			if (applyData is HeightOutput200.ApplySetData heightSetData)
				terrainMatrix.ExportHeights(heightSetData.heights2D, new Coord(0,0));

			else if (applyData is HeightOutput200.ApplySplitData heightSplitData)
				terrainMatrix.ExportHeightStrips(heightSplitData.heights2DSplits, new Coord(0,0));

			#if UNITY_2019_1_OR_NEWER
			else if (applyData is HeightOutput200.ApplyTexData heightTexData)
				terrainMatrix.ExportRawFloat(heightTexData.texBytes, new Coord(0,0), new Coord(heightTexData.res,heightTexData.res), mult:0.5f);
			#endif
		}
	}
}