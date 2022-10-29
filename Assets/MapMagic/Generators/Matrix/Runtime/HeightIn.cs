using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Terrains;

#if UN_MapMagic
using uNature.Core.Extensions.MapMagicIntegration;
using uNature.Core.FoliageClasses;
#endif


namespace MapMagic.Nodes.MatrixGenerators
{
/*
	[System.Serializable]
	[GeneratorMenu (menu="Map/Input", name ="Height In", section=1, disengageable = true)]
	public class HeightInput : StandardGenerator, IOutlet<MatrixWorld>, IPrepare
	{
		[Val("Use Initial TerrainData Holder")] public bool useInitial;
		[Val("Subtract MapMagic Changes")] public bool useDelta;

		public override IEnumerable<Inlet> Inlets () { yield break; }


		public void Prepare (TileData data, Terrain terrain)
		{
			InitialTerrainData initial = null;
			TerrainHeightData heightData = new TerrainHeightData();

			if (useInitial)
				initial = terrain.GetComponent<InitialTerrainData>();

			//using initial data holder
			//if (initial != null && !initial.Equals(null))
			//	heightData.heights2D = initial.initialHeights;

			//reading terrain directly
			else
				heightData.Read(terrain);

			data.SetPrepare(this, heightData);
		}

		public override void GenerateProduct (TileData data, StopToken stop)
		{
			//getting terrain reads
			TerrainHeightData heightData = data.GetPrepare<TerrainHeightData>(this);

			int heightRes = heightData.heights2D.GetLength(0) - 1;
			CoordRect centerRect = data.area.active.rect;
			MatrixWorld matrix;

			//no interpolation
			if (heightRes == centerRect.size.x)
			{
				matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize);
				FillMatrixWithFloat2D(matrix, centerRect, heightData.heights2D);
			}

			//with resize
			else
			{
				float sizeFactor = 1f * heightRes / centerRect.size.x;
				Matrix scaledMatrix = new Matrix(data.area.full.rect * sizeFactor);
				CoordRect scaledCenterRect = centerRect * sizeFactor;

				FillMatrixWithFloat2D(scaledMatrix, scaledCenterRect, heightData.heights2D);

				Matrix rescaledMatrix = new Matrix(scaledMatrix);
				rescaledMatrix.Resize(data.area.full.rect);
				matrix = new MatrixWorld(rescaledMatrix.rect, data.area.full.worldPos, data.area.full.worldSize, rescaledMatrix.arr);
			}

			//subtracting mapmagic apply
			if (useDelta)
			{
				Matrix lastAppliedHeight = data.heights; //TODO: use a special applied data (asset? for serialization)
				if (matrix.rect == lastAppliedHeight.rect)
				{
					for (int i=0; i<matrix.arr.Length; i++)
					{
						float delta = lastAppliedHeight.arr[i] - matrix.arr[i];
					}
				}
			}
			
			data.products[this] = ,matrix);
		}


		public void FillMatrixWithFloat2D (Matrix matrix, CoordRect rect, float[,] heights2D)
		/// Fills the rect area of the matrix with heights2D array. Area outsize rect is filled with edge values
		{
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					int ax = x - rect.offset.x;
					int az = z - rect.offset.z;

					if (ax<0) ax = 0; if (ax>rect.size.x) ax = rect.size.x;
					if (az<0) az = 0; if (az>rect.size.z) az = rect.size.z;

					float val = heights2D[az,ax];
					matrix[x,z] = val;
				}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Input", name ="Splats In", section=1, disengageable = true)]
	public class SplatsInput : StandardGenerator, IOutlet<MatrixWorld>, IPrepare, ISubGraph
	{
		[Val("Channel")] public int channel;

		public override IEnumerable<Inlet> Inlets () { yield break; }


		public void Prepare (TileData data, Terrain terrain)
		{
			TerrainSplatData splatsData = new TerrainSplatData();

			splatsData.Read(terrain);

			data.SetPrepare(this, splatsData);
		}

		public override void GenerateProduct (TileData data, StopToken stop)
		{
			//getting terrain reads
			TerrainSplatData splatsData = data.GetPrepare<TerrainSplatData>(this);

			int splatsRes = splatsData.splats.GetLength(0);
			CoordRect centerRect = data.area.active.rect;
			MatrixWorld matrix;

			//no interpolation
			if (splatsRes == centerRect.size.x)
			{
				matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize);
				FillMatrixWithFloat3D(matrix, centerRect, splatsData.splats, channel);
			}

			//with resize
			else
			{
				float sizeFactor = 1f * splatsRes / centerRect.size.x;
				Matrix scaledMatrix = new Matrix (data.area.full.rect * sizeFactor);
				CoordRect scaledCenterRect = centerRect * sizeFactor;

				FillMatrixWithFloat3D(scaledMatrix, scaledCenterRect, splatsData.splats, channel);

				Matrix rescaledMatrix = new Matrix(scaledMatrix);
				rescaledMatrix.Resize(data.area.full.rect);
				matrix = new MatrixWorld(rescaledMatrix.rect, data.area.full.worldPos, data.area.full.worldSize, rescaledMatrix.arr);
			}

			data.products[this] = ,matrix);
		}

		public void FillMatrixWithFloat3D (Matrix matrix, CoordRect rect, float[,,] splats, int ch)
		/// Fills the rect area of the matrix with heights2D array. Area outsize rect is filled with edge values
		{
			if (ch >= splats.GetLength(2)) return;

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					int ax = x - rect.offset.x;
					int az = z - rect.offset.z;

					if (ax<0) ax = 0; if (ax>rect.size.x-1) ax = rect.size.x-1;
					if (az<0) az = 0; if (az>rect.size.z-1) az = rect.size.z-1;

					float val = splats[az,ax, ch];
					matrix[x,z] = val;
				}
		}
	}
*/
}