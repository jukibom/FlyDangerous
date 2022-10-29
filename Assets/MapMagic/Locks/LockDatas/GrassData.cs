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
	public class GrassData : ILockData
	{
		public int[][,] lockLayers;
		public DetailPrototype[] lockPrototypes;
		public int patchResolution = 16;

		CoordCircle circle;
		int resolution; //to determine if it's changed and avoid writing


		public void Read (Terrain terrain, Lock lk) 
		{
			TerrainData terrainData = terrain.terrainData;
			if (terrainData.detailPrototypes == null || terrainData.detailPrototypes.Length == 0)
				{ lockLayers=null; lockPrototypes=null; return; }
				// Don't perform lock if no grass prototypes

			resolution = terrain.terrainData.detailResolution;
			circle = new CoordCircle(terrain, resolution, lk.worldPos, lk.worldRadius, lk.worldTransition);

			
			lockPrototypes = terrainData.detailPrototypes;
			lockLayers = new int[lockPrototypes.Length][,];
			for (int i=0; i<lockPrototypes.Length; i++)
			{
				int[,] layer = terrainData.GetDetailLayer(circle.rect.offset.x, circle.rect.offset.z, circle.rect.size.x, circle.rect.size.z, i);
				lockLayers[i] = layer;
			}
			patchResolution = terrainData.detailResolutionPerPatch;
		}


		public void WriteInThread (IApplyData applyData)
		{
			if (! (applyData is GrassOutput200.ApplyData applyGrassData) ) return;

			if (lockLayers == null || lockPrototypes == null) return; // Don't perform lock if nothing is stored
			if (Mathf.ClosestPowerOfTwo(applyGrassData.Resolution) != resolution) return; //Don't perform lock if resolution changed

			UnifyPrototypes(ref applyGrassData.detailPrototypes, ref applyGrassData.detailLayers, ref lockPrototypes, ref lockLayers);

			for (int c=0; c<lockPrototypes.Length; c++)
				Stamp(applyGrassData.detailLayers[c], lockLayers[c], circle.rect.offset, circle.center, circle.fullRadius);
		}


		public void WriteInApply (Terrain terrain, bool resizeTerrain=false) 
		{ 
			TerrainData terrainData = terrain.terrainData;

			if (lockLayers == null || lockPrototypes == null) return; // Don't perform lock if nothing is stored

			if (terrain.terrainData.detailResolution != resolution)
			{
				if (resizeTerrain)
					terrainData.SetDetailResolution(resolution, patchResolution);
				else 
					return;
			}

			terrainData.detailPrototypes = lockPrototypes;
			for (int i=0; i<lockLayers.Length; i++)
				terrainData.SetDetailLayer(circle.rect.offset.x, circle.rect.offset.z, i, lockLayers[i]);
			
		}


		public void ApplyHeightDelta (Matrix src, Matrix dst) { }


		public void ResizeFrom (ILockData otherData)
		{
			GrassData src = (GrassData)otherData;

			if (src.lockLayers==null || lockLayers==null) return;
			int numLayers = Mathf.Min(src.lockLayers.Length, lockLayers.Length);
			if (numLayers == 0) return;

			for (int i=0; i<numLayers; i++)
			{
				int[,] srcLayer = src.lockLayers[i];
				int[,] dstLayer = lockLayers[i];

				int srcResX = dstLayer.GetLength(0);
				int srcResZ = dstLayer.GetLength(1);

				int dstResX = srcLayer.GetLength(0);
				int dstResZ = srcLayer.GetLength(1);

				float resFactorX = 1f*dstResX/srcResX;
				float resFactorZ = 1f*dstResZ/srcResZ; 

			
				for (int dstX=0; dstX<dstResX; dstX++)
					for (int dstZ=0; dstZ<dstResZ; dstZ++)
					{
						int srcX = (int)(dstX/resFactorX);
						int srcZ = (int)(dstZ/resFactorZ);

						dstLayer[dstX,dstZ] = srcLayer[srcX,srcZ];
					}
			}
		}


		private static void UnifyPrototypes (ref DetailPrototype[] basePrototypes, ref int[][,] baseData, 
											 ref DetailPrototype[] addPrototypes, ref int[][,] addData)
		/// Makes both datas prototypes arrays equal, and the layers arrays relevant to prototypes (empty arrays)
		/// Safe per-channel blend could be performed after this operation
		{
			//guard if prototypes have not been changed
			if (ArrayTools.MatchExactly(basePrototypes, addPrototypes)) return;

			//creating array of unified prototypes
			List<DetailPrototype> unifiedPrototypes = new List<DetailPrototype>();
			unifiedPrototypes.AddRange(basePrototypes); //do not change the base prototypes order
			for (int p=0; p<addPrototypes.Length; p++)
			{
				if (!unifiedPrototypes.Contains(addPrototypes[p]))
					unifiedPrototypes.Add(addPrototypes[p]);
			}

			//lut to convert prototypes indexes
			Dictionary<int,int> baseToUnifiedIndex = new Dictionary<int, int>();
			Dictionary<int,int> addToUnifiedIndex = new Dictionary<int, int>();

			for (int p=0; p<basePrototypes.Length; p++)
				baseToUnifiedIndex.Add(p, unifiedPrototypes.IndexOf(basePrototypes[p]));  //should be 1,2,3,4,5, but doing this in case unified prototypes gather will be optimized

			for (int p=0; p<addPrototypes.Length; p++)
				addToUnifiedIndex.Add(p, unifiedPrototypes.IndexOf(addPrototypes[p]));

			//re-creating base data
			{
				int[][,] newBaseData = new int[unifiedPrototypes.Count][,];
			
				int baseDataLayers = baseData.Length;
				for (int i=0; i<baseDataLayers; i++)
					newBaseData[ baseToUnifiedIndex[i] ] = baseData[i];

				baseData = newBaseData;
			}

			//re-creating add data
			{
				int[][,] newAddData = new int[unifiedPrototypes.Count][,];
			
				int addDataLayers = addData.Length;
				for (int i=0; i<addDataLayers; i++)
					newAddData[ addToUnifiedIndex[i] ] = addData[i];

				addData = newAddData;
			}
			//saving prototypes
			basePrototypes = unifiedPrototypes.ToArray();
			addPrototypes = unifiedPrototypes.ToArray();
		}


		private static void Stamp (int[,] arr, int[,] stamp, Coord stampOffset, Coord center, int radius)
		/// stamps one splat array onto the other using coords center, radius and transition
		/// using channelCompliance to swap channels according their prototypes
		{
			int stampResX = stamp.GetLength(1); //x and z are swapped
			int stampResZ = stamp.GetLength(0);

			for (int x=0; x<stampResX; x++)
				for (int z=0; z<stampResZ; z++)
				{
					int sx = stampOffset.x + x;
					int sz = stampOffset.z + z;

					float dist = Mathf.Sqrt((sx-center.x)*(sx-center.x) + (sz-center.z)*(sz-center.z));
					if (dist > radius) continue;

					arr[sz, sx] = stamp[z, x];

					//SOON: soft blending. Convert each channel to matrix, extend fields, stamp and blend instead.
				}
		}
	}
}