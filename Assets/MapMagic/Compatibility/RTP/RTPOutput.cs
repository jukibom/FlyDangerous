#if MAPMAGIC2 //shouldn't work if MM assembly not compiled

using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Products;

namespace MapMagic.Nodes.MatrixGenerators 
{

	[System.Serializable]
	[GeneratorMenu( 
		menu = "Map/Output", 
		name = "RTP", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName = "GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class RTPOutput200 : BaseTexturesOutput<RTPOutput200.RTPLayer>
	{
		#if RTP
		public ReliefTerrain rtp = null;
		#endif

		public class RTPLayer : BaseTextureLayer { }


		public override void Generate (TileData data, StopToken stop) 
		{
			//generating
			MatrixWorld[] dstMatrices = BaseGenerate(data, stop);

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(RTPOutput200), layers[i],  dstMatrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			//purging if no outputs
			if (data.OutputsCount(typeof(RTPOutput200), inSubs:true) == 0)
			{
				if (stop!=null && stop.stop) return;
				data.MarkApply(CustomShaderOutput200.ApplyData.Empty);
				return;
			}

			//creating control textures contents
			Color[][] colors = null; //TODO: re-use colors array
//			CustomShaderOutput200.BlendControlTextures(ref colors, typeof(RTPOutput200), data);

			//pushing to apply
			if (stop!=null && stop.stop) return;
			var controlTexturesData = new CustomShaderOutput200.ApplyData() {
				textureColors = colors,
				textureFormat = TextureFormat.RGBA32,
				textureBaseMapDistance = 10000000, //no base map
				textureNames = new string[colors!=null ? colors.Length : 0] };

			for (int t=0; t<controlTexturesData.textureNames.Length; t++)
				controlTexturesData.textureNames[t] = "_Control" + (t+1);

			Graph.OnOutputFinalized?.Invoke(typeof(RTPOutput200), data, controlTexturesData, stop);
			data.MarkApply(controlTexturesData);
		}


		public override void ClearApplied (TileData data, Terrain terrain)
		{

		}
	}

}
#endif