#if MAPMAGIC2 //shouldn't work if MM assembly not compiled

using System.Collections.Generic;
using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Products;
using MapMagic.Terrains;


namespace MapMagic.Nodes.MatrixGenerators 
{
	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "MegaSplat", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class MegaSplatOutput200 : BaseTexturesOutput<MegaSplatOutput200.MegaSplatLayer>
	{
		public static float clusterNoiseScale = 0.05f;

		private string[] clusterNames = new string[0];

		public static bool smoothFallof = false;

		//public Input wetnessIn = new Input(InoutType.Map);
		//public Input puddlesIn = new Input(InoutType.Map);
		//public Input displaceDampenIn = new Input(InoutType.Map);

		public class MegaSplatLayer : BaseTextureLayer { } //inheriting empty to draw it's editor

		public override void Generate (TileData data, StopToken stop) 
		{
			//generating
			MatrixWorld[] dstMatrices = BaseGenerate(data, stop);

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(MegaSplatOutput200), layers[i],  dstMatrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			#if __MEGASPLAT__
			if (data.globals.megaSplatTexList==null) return;

			//creating control textures
			if (stop!=null && stop.stop) return;
			data.GatherOutputs (typeof(MegaSplatOutput200),
				out MegaSplatLayer[] layers,
				out MatrixWorld[] matrices,
				out MatrixWorld[] masks,
				inSubs:true);
			float[] opacities = layers.Select(l=>l.Opacity);
			int[] channelNums = layers.Select(l=>l.channelNum);

			//purging if no outputs
			if (matrices.Length == 0)
			{
				if (stop!=null && stop.stop) return;
				data.MarkApply(CustomShaderOutput200.ApplyData.Empty);
				return;
			}

			Color[] controlColors = BlendMegaSplat(data.area, data.heights, data.globals.megaSplatTexList as MegaSplatTextureList,
				matrices, masks, opacities, channelNums);

			//pushing to apply
			if (stop!=null && stop.stop) return;
			//var applyData = new ApplyData() { colors = colors };
			var applyData = new CustomShaderOutput200.ApplyData()
			{
				textureColors = new Color[][] { controlColors },
				textureNames = new string[] { "_SplatControl" },
				textureFormat = TextureFormat.RGBA32
			};

			Graph.OnOutputFinalized?.Invoke(typeof(CustomShaderOutput200), data, applyData, stop);
			data.MarkApply(applyData);

			#endif
		}


		public override void ClearApplied (TileData data, Terrain terrain)
		{

		}

		#if __MEGASPLAT__
		public static Color[] BlendMegaSplat (Area area, Matrix heights, MegaSplatTextureList textureList,
			IList<Matrix> matrices, IList<Matrix> biomeMasks, IList<float> opacities, IList<int> channelNums,
			StopToken stop=null)
		{
			int count = matrices.Count;
			CoordRect activeRect = area.active.rect;
			Color[] controlMap = new Color[activeRect.Count];

			//getting matrices rect
			CoordRect matrixRect = new CoordRect(0,0,0,0);
			for (int m=0; m<count; m++)
				if (matrices[m] != null) matrixRect = matrices[m].rect;

			//checking rect
			for (int m=0; m<count; m++)
				if (matrices[m] != null  &&  matrices[m].rect != matrixRect)
					throw new System.Exception("MapMagic: Matrix rect mismatch");
			for (int b=0; b<count; b++)
				if (biomeMasks[b] != null  &&  biomeMasks[b].rect != matrixRect)
					throw new System.Exception("MapMagic: Biome matrix rect mismatch");

			//preparing row re-use array
			float[] values = new float[count];

			//blending
			
			for (int x=0; x<activeRect.size.x; x++)
				for (int z=0; z<activeRect.size.z; z++)
				{
					int matrixPosX = activeRect.offset.x + x;
					int matrixPosZ = activeRect.offset.z + z;
					int matrixPos = (matrixPosZ-matrixRect.offset.z)*matrixRect.size.x + matrixPosX - matrixRect.offset.x;

					int colorsPos = z*activeRect.size.x + x; //(z-colorsRect.offset.z)*colorsRect.size.x + x - colorsRect.offset.x;

					// find highest two layers
					int botOutputIdx = 0;
					int topOutputIdx = 0;
					float botWeight = 0;
					float topWeight = 0;

					for (int i = 0; i<count; i++)
					{
						//value
						float val = matrices[i].arr[matrixPos];

						//multiply with biome
						Matrix biomeMask = biomeMasks[i];
						if (biomeMask != null) //no empty biomes in list (so no mask == root biome)
							val *= biomeMask.arr[matrixPos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
						
						//clamp
						if (val < 0) val = 0; if (val > 1) val = 1;

						//finding if it's highest 
						if (val > botWeight)
						{
							topWeight = botWeight;
							topOutputIdx = botOutputIdx;

							botWeight = val;
							botOutputIdx = i;
						}

						//or 2nd highest
						else if (val > topWeight)
						{
							topOutputIdx = i;
							topWeight = val;
						}
					}

					
					//converting layer index to texture index
					int topClusterIdx = channelNums[topOutputIdx];
					int botClusterIdx = channelNums[botOutputIdx];

					Vector3 worldPos = area.active.CoordToWorld(x,z);
					float heightRatio = heights!=null? heights.arr[matrixPos] : 0.5f; //0 is the bottom point, 1 is the maximum top
					Vector3 normal = new Vector3(0,1,0); //TODO: get normal from matrix

					int topTexIdx = textureList.clusters[topClusterIdx].GetIndex(worldPos * clusterNoiseScale, normal, heightRatio);
					int botTexIdx = textureList.clusters[botClusterIdx].GetIndex(worldPos * clusterNoiseScale, normal, heightRatio);

					//swapping indexes to make topIdx always on top
					/*if (botIdx > topIdx) 
					{
						int tempIdx = topIdx;
						topIdx = botIdx;
						botIdx = tempIdx;

						float tempWeight = topWeight;
						topWeight = botWeight;
						botWeight = tempWeight;
					}*/

					//finding blend
					float totalWeight = topWeight + botWeight;	if (totalWeight<0.01f) totalWeight = 0.01f; //Mathf.Max and Clamp are slow
					float blend = botWeight / totalWeight;		if (blend>1) blend = 1;

					//adjusting blend curve
					if (smoothFallof) blend = (Mathf.Sqrt(blend) * (1-blend)) + blend*blend*blend;  //Magic secret formula! Inverse to 3*x^2 - 2*x^3

					//setting color
					controlMap[colorsPos] = new Color(botTexIdx / 255.0f, topTexIdx / 255.0f, 1.0f - blend, 1.0f);

					//params
					/*for (int i = 0; i<specialCount; i++)
					{
						float biomeVal = specialBiomeMasks[i]!=null? specialBiomeMasks[i].array[pos] : 1;

						if (specialWetnessMatrices[i]!=null) result.param[pos].b = specialWetnessMatrices[i].array[pos] * biomeVal;
						if (specialPuddlesMatrices[i]!=null) 
						{
							result.param[pos].a = specialPuddlesMatrices[i].array[pos] * biomeVal;
							result.param[pos].r = 0.5f;
							result.param[pos].g = 0.5f;
						}
						if (specialDampeningMatrices[i]!=null) result.control[pos].a = specialDampeningMatrices[i].array[pos] * biomeVal;
					}*/
				}

			return controlMap;
		}
		#endif
	}


}

#endif //MAPMAGIC2