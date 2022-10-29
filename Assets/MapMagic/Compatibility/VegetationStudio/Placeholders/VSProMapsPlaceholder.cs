using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Products;
using MapMagic.Terrains;
using MapMagic.Nodes;


#if !VEGETATION_STUDIO_PRO
namespace MapMagic.VegetationStudio
{
	[System.Serializable]
	[GeneratorMenu(
		//menu = "Map/Output", 
		name = "VS Pro Maps", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld))]
	public class VSProMapsOut : OutputGenerator, IInlet<MatrixWorld>
	{
		public OutputLevel outputLevel = OutputLevel.Main;
		public override OutputLevel OutputLevel { get{ return outputLevel; } }

		//[Val("Package", type = typeof(VegetationPackagePro))] public VegetationPackagePro package; //in globals
		
		public float density = 0.5f;
		
		public int maskGroup = 0;
		public int textureChannel = 0;

		public override void Generate (TileData data, StopToken stop) { }
		public override void ClearApplied (TileData data, Terrain terrain) { }
	}
}
#endif