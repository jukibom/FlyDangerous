using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Products;

#if !__MICROSPLAT__
namespace MapMagic.Nodes.MatrixGenerators {
	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "MicroSplat", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class MicroSplatOutput200 : BaseTexturesOutput<MicroSplatOutput200.MicroSplatLayer>
	{

		public override void Generate (TileData data, StopToken stop) { }
		public override void ClearApplied (TileData data, Terrain terrain) { }

        public class MicroSplatLayer : BaseTextureLayer 
		{ 
			[NonSerialized] public TerrainLayer prototype = null; //used in case 'add std' enabled
		}

		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) { }
	}
}
#endif