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


#if VEGETATION_STUDIO_PRO
using AwesomeTechnologies.VegetationSystem;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.Vegetation.Masks;
using AwesomeTechnologies.Vegetation.PersistentStorage;
#endif

namespace MapMagic.VegetationStudio
{
	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "VS Pro Maps", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Grass")]
	public class VSProMapsOut : OutputGenerator, IInlet<MatrixWorld>
	{
		public OutputLevel outputLevel = OutputLevel.Main;
		public override OutputLevel OutputLevel { get{ return outputLevel; } }

		//[Val("Package", type = typeof(VegetationPackagePro))] public VegetationPackagePro package; //in globals
		
		public float density = 0.5f;
		
		public int maskGroup = 0;
		public int textureChannel = 0;


		public override void Generate (TileData data, StopToken stop) 
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return;
			if (data.globals.vegetationSystem == null || data.globals.vegetationPackage == null) return;

			if (enabled)
			{
				data.StoreOutput(this, typeof(VSProMapsOut), this, src);  //adding src since it's not changing
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop)
		{
			#if VEGETATION_STUDIO_PRO

			//creating splats and prototypes arrays
			int layersCount = data.OutputsCount(finalizeAction, inSubs:true);
			int splatsSize = data.area.active.rect.size.x - 1; //-1 is a resolution fix from forums:
					//https://forum.unity.com/threads/released-mapmagic-2-infinite-procedural-land-generator.875470/page-22#post-7025689
					//seems to be working, but probably there are some cases it's not

			//preparing texture colors
			VegetationPackagePro package = data.globals.vegetationPackage as VegetationPackagePro;
			Color[][] colors = new Color[package!=null ? package.TextureMaskGroupList.Count : 0][];
			for (int c=0; c<colors.Length; c++)
				colors[c] = new Color[splatsSize*splatsSize];
			int[] maskGroupNums = new int[colors.Length];

			//filling colors
			int i=0;
			foreach ((VSProMapsOut output, MatrixWorld matrix, MatrixWorld biomeMask) 
				in data.Outputs<VSProMapsOut,MatrixWorld,MatrixWorld>(typeof(VSProMapsOut), inSubs:true))
			{
				if (matrix == null || package==null) continue;
				BlendLayer(colors, data.area, matrix, biomeMask, output.density, output.maskGroup, output.textureChannel, stop);
				maskGroupNums[output.maskGroup] = output.maskGroup; //TODO: removed i/4, test this! (http://mm2.idea.informer.com/proj/?ia=134562)
				i++;
			}

			//pushing to apply
			if (stop!=null && stop.stop) return;
			ApplyData applyData = new ApplyData() {
				srcSystem=data.globals.vegetationSystem as VegetationSystemPro, 
				package=package, 
				colors=colors, 
				maskGroupNums=maskGroupNums,
				copyVS= data.globals.vegetationSystemCopy  };
			Graph.OnOutputFinalized?.Invoke(typeof(VSProMapsOut), data, applyData, stop);
			data.MarkApply(applyData);

			#endif
		}


		public static void BlendLayer (Color[][] colors, Area area, MatrixWorld matrix, MatrixWorld biomeMask, float opacity, int maskGroup, int textureChannel, StopToken stop=null)
		{
			Color[] cols = colors[maskGroup];
			int splatsSize = area.active.rect.size.x - 1; //-1 is a resolution fix from forums:
					//https://forum.unity.com/threads/released-mapmagic-2-infinite-procedural-land-generator.875470/page-22#post-7025689
					//seems to be working, but probably there are some cases it's not
			int fullSize = area.full.rect.size.x;
			int margins = area.Margins;

			for (int x=0; x<splatsSize; x++)
				for (int z=0; z<splatsSize; z++)
				{
					if (stop!=null && stop.stop) return;

					int matrixPos = (z+margins)*fullSize + (x+margins);

					float val = matrix.arr[matrixPos];

					if (biomeMask != null) //no empty biomes in list (so no mask == root biome)
						val *= biomeMask.arr[matrixPos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here

					val *= opacity;

					if (val < 0) val = 0; if (val > 1) val = 1;

					int colsPos = z*splatsSize + x;
					switch (textureChannel)
					{
						case 0: cols[colsPos].r += val; break;
						case 1: cols[colsPos].g += val; break;
						case 2: cols[colsPos].b += val; break;
						case 3: cols[colsPos].a += val; break;
					}
				}
		}


		public override void ClearApplied (TileData data, Terrain terrain)
		{
			VegetationSystemPro system = VSProOps.GetCopyVegetationSystem(terrain);
			if (system == null) system = data.globals.vegetationSystem as VegetationSystemPro;
			if (system == null) system = GameObject.FindObjectOfType<VegetationSystemPro>();


		}

		#if VEGETATION_STUDIO_PRO
		public class ApplyData : IApplyData
		{
			public VegetationSystemPro srcSystem;
			public VegetationPackagePro package;
			public Color[][] colors;
			public int[] maskGroupNums;
			public bool copyVS;

			public void Read (Terrain terrain)  { throw new System.NotImplementedException(); }

			public void Apply (Terrain terrain)
			{
				//updating system
				VegetationSystemPro copySystem = null;  //we'll need it to set up tile
				if (copyVS)
				{
					copySystem = VSProOps.GetCopyVegetationSystem(terrain); 
					if (copySystem == null) copySystem = VSProOps.CopyVegetationSystem(srcSystem, terrain.transform.parent);
					VSProOps.UpdateCopySystem(copySystem, terrain, package, srcSystem);
				}

				else
					VSProOps.UpdateSourceSystem(srcSystem, terrain);

				//applying
				Texture2D[] textures = WriteTextures(null, colors);
				VSProOps.SetTextures(
					copyVS ? copySystem : srcSystem, 
					package, textures, maskGroupNums, terrain.GetWorldRect());

				//tile obj (serialization and disable purpose)
				Transform tileTfm = terrain.transform.parent;
				VSProMapsTile vsTile = tileTfm.GetComponent<VSProMapsTile>();
				if (vsTile == null) vsTile = tileTfm.gameObject.AddComponent<VSProMapsTile>();
				vsTile.system = copyVS ? copySystem : srcSystem;
				vsTile.package = package;
				vsTile.terrainRect = terrain.GetWorldRect();
				vsTile.textures = textures;
				vsTile.maskGroupNums = maskGroupNums;
				vsTile.masksApplied = true;
			}

			public static ApplyData Empty
			{get{
				return new ApplyData() { 
					colors = new Color[0][],
					maskGroupNums = new int[0]  };
			}}

			public int Resolution
			{get{
				if (colors.Length==0) return 0;
				else return (int)Mathf.Sqrt(colors[0].Length);
			}}
		}


		public static Texture2D[] WriteTextures (Texture2D[] oldTextures, Color[][] colors)
		{
			int numTextures = colors.Length;
			if (numTextures==0) return new Texture2D[0];
			int resolution = (int)Mathf.Sqrt(colors[0].Length);

			Texture2D[] textures = new Texture2D[numTextures];

			//making textures of colors in coroutine
			for (int i=0; i<numTextures; i++)
			{
				//trying to reuse last used texture
				Texture2D tex;
				if (oldTextures != null  &&
					i < oldTextures.Length  &&
					oldTextures[i] != null && 
					oldTextures[i].width == resolution && 
					oldTextures[i].height == resolution)
						tex = oldTextures[i];
					
				else
				{
					#if UNITY_EDITOR
					if (textures[i] != null && !UnityEditor.AssetDatabase.Contains(textures[i]))
					#endif
						GameObject.DestroyImmediate(textures[i]);

					tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, true);
					tex.wrapMode = TextureWrapMode.Mirror; //to avoid border seams
				}
					
				tex.SetPixels(0,0,tex.width,tex.height,colors[i]);
				tex.Apply();

				textures[i] = tex;
			}

			return textures;
		}




		#endif
	}
}
