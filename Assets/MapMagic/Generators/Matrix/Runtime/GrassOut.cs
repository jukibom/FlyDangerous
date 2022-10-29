using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Products;
using MapMagic.Terrains;

namespace MapMagic.Nodes.MatrixGenerators
{
	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "Grass", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/GrassOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Grass")]
	public class GrassOutput200 : OutputGenerator, IInlet<MatrixWorld>
	{
		public OutputLevel outputLevel = OutputLevel.Main;
		public override OutputLevel OutputLevel { get{ return outputLevel; } }

		public float density = 0.5f; //number of meshes per map pixel. Should not be confused with opacity, it's not used for layer blending.
		public DetailPrototype prototype = new DetailPrototype() { dryColor = new Color(0.95f, 1f, 0.65f), healthyColor = new Color(0.5f, 0.65f, 0.35f) };

		public enum GrassRenderMode { Grass, Billboard, MeshVertexLit, MeshUnlit };
		public GrassRenderMode renderMode;


		public override void Generate (TileData data, StopToken stop) 
		{
			//loading source
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 

//			if (!enabled) 
//				{ data.finalize.Remove(finalizeAction, this); return; }

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				data.StoreOutput(this, typeof(GrassOutput200), this, src);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop)
		{
			//creating splats and prototypes arrays
			int layersCount = data.OutputsCount(typeof(GrassOutput200), inSubs:true);
			int splatsSize = data.area.active.rect.size.x;

			int[][,] detailArr = new int[layersCount][,];
			DetailPrototype[] prototypes = new DetailPrototype[layersCount];

			//filling arrays
			int i=0;
			foreach ((GrassOutput200 output, MatrixWorld product, MatrixWorld biomeMask) 
				in data.Outputs<GrassOutput200,MatrixWorld,MatrixWorld>(typeof(GrassOutput200), inSubs:true))
			{
				if (stop!=null && stop.stop) return;

				detailArr[i] = CreateDetailLayer(product, biomeMask, output.density*product.PixelSize.x*product.PixelSize.z, i, data, stop);
				prototypes[i] = output.prototype;
				i++;
			}

			//pushing to apply
			if (stop!=null && stop.stop) return;
			ApplyData applyData = new ApplyData() {detailLayers=detailArr, detailPrototypes=prototypes, patchResolution=data.globals.grassResPerPatch};
			Graph.OnOutputFinalized?.Invoke(typeof(GrassOutput200), data, applyData, stop);
			data.MarkApply(applyData);
		}


		private static int[,] CreateDetailLayer (Matrix matrix, Matrix biomeMask, float density, int randomNum, TileData data, StopToken stop)
		{
			//Matrix matrix = (Matrix)output.product;
			//Matrix biomeMask = output.biomeMask;
			//GrassLayer layer = (GrassLayer)output.layer;

			int downscaleRatio = data.globals.grassResDownscale;
			int arrSize = (data.area.active.rect.size.x-1) / downscaleRatio + 1;
			int fullSize = data.area.full.rect.size.x;
			int margins = data.area.Margins;

			int[,] detail = new int[arrSize, arrSize];

			if (matrix == null) return detail;

			for (int x = 0; x < arrSize; x++)
				for (int z = 0; z < arrSize; z++)
				{
					if (stop!=null && stop.stop) return null;

					int pos = (z*downscaleRatio+margins)*fullSize + (x*downscaleRatio+margins);
					float val = matrix.arr[pos];

					//interpolating value since detail resolution is 512, while height is 513
					//val += matrix.arr[pos+1] + matrix.arr[pos+fullSize] + matrix.arr[pos+fullSize+1]; //margins should prevent reading out of bounds
					//val /= 4;

					//or using minimal interpolation (creates better visual effect - grass isn't growing where it should not)
					float val1 = matrix.arr[pos+1];				if (val1<val) val=val1;
					float val2 = matrix.arr[pos+fullSize];		if (val2<val) val=val2;
					float val3 = matrix.arr[pos+fullSize+1];	if (val3<val) val=val3;
					
					//multiply with biome
					if (biomeMask != null) //no empty biomes in list (so no mask == root biome)
						val *= biomeMask.arr[pos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
					
					if (val < 0) val = 0; if (val > 1) val = 1;

					//the number of bushes in pixel
					val *= density*downscaleRatio*downscaleRatio;

					//random
					float rnd = data.random.Random(randomNum, x,z);
					
					//converting to integer with random
					int intVal = (int)val;
					float remain = val - intVal;
					if (remain>rnd) intVal++;

					detail[z, x] = intVal;
				}

			return detail;
		}

		
		public class ApplyData : IApplyData
		{
			public int[][,] detailLayers;
			public DetailPrototype[] detailPrototypes;
			public int patchResolution = 16;
			//public CoordRect rect; //storing both offset and size (in case the layers length is 0)


			public void Apply (Terrain terrain)
			{
				if (terrain==null || terrain.Equals(null) || terrain.terrainData==null) return; //chunk removed during apply

				int resolution = detailLayers[0].GetLength(1);
				terrain.terrainData.SetDetailResolution(resolution, patchResolution);
				
				terrain.terrainData.detailPrototypes = detailPrototypes;

				for (int i=0; i<detailLayers.Length; i++)
					terrain.terrainData.SetDetailLayer(0, 0, i, detailLayers[i]);
			}

			public static ApplyData Empty
				{get{ return new ApplyData() { detailLayers=new int[0][,], detailPrototypes=new DetailPrototype[0] }; }}

			public int Resolution => detailLayers.Length != 0 ? detailLayers[0].GetLength(0) : 0;

			#if UN_MapMagic
			public void ApplyUNature (Terrain terrain)
			/// Just in case I'll need to return compatibility
			{
				
				uNatureGrassTuple uNatureTuple = null;

				if (FoliageCore_MainManager.instance != null)
				{
					uNatureTuple = (uNatureGrassTuple)dataBox;
					grassTuple = uNatureTuple.tupleInformation;
				}
				else
				{
					//Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
					//yield break;
					grassTuple = (TupleSet<int[][,], DetailPrototype[]>)dataBox;
				}

				int[][,] details = grassTuple.item1;
				DetailPrototype[] prototypes = grassTuple.item2;

				//resolution
				int resolution = details[0].GetLength(1);
				terrain.terrainData.SetDetailResolution(resolution, patchResolution);

				if (FoliageCore_MainManager.instance != null)
				{
					UNMapMagic_Manager.RegisterGrassPrototypesChange(prototypes);
				}

				//prototypes
				terrain.terrainData.detailPrototypes = prototypes;

				if (FoliageCore_MainManager.instance != null)
				{
					UNMapMagic_Manager.ApplyGrassOutput(uNatureTuple);
				}
				else
				{
					//Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
					//yield break;
					for (int i = 0; i < details.Length; i++)
					{
						terrain.terrainData.SetDetailLayer(0, 0, i, details[i]);
					}
				}
			}
			#endif
		}


		public override void ClearApplied (TileData data, Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;
			Vector3 terrainSize = terrainData.size;

			terrainData.detailPrototypes = new DetailPrototype[0];
			terrainData.SetDetailResolution(32, 32);
		}
	}
}
