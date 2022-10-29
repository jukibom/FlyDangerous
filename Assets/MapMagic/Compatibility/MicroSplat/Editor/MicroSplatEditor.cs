#if MAPMAGIC2 //shouldn't work if MM assembly not compiled

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Core;  //used once to get tile size
using MapMagic.Products;
using MapMagic.Nodes.MatrixGenerators;

#if __MICROSPLAT__
using JBooth.MicroSplat;
#endif

namespace MapMagic.Nodes.GUI
{

	public static class MicroSplatEditor 
	{
		private static Texture2DArray lastIconsArr = null; //caching texture array to avoid fetching from MS each frame (surprisingly it's slow)
		private static Material lastMicroSplatMat = null;

		[UnityEditor.InitializeOnLoadMethod]
		static void EnlistInMenu ()
		{
			CreateRightClick.generatorTypes.Add(typeof(MicroSplatOutput200));
		}


		[Draw.Editor(typeof(MicroSplatOutput200))]
		public static void DrawMicroSplat (MicroSplatOutput200 gen)
		{
			#if !__MICROSPLAT__
			using (Cell.LinePx(60))
				Draw.Helpbox("MicroSplat doesn't seem to be installed, or MicroSplat compatibility is not enabled in settings");
			#endif
			if (GraphWindow.current.mapMagic != null  &&  GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)
				using (Cell.LineStd)
				{
					//Cell.current.fieldWidth = 0.5f;
					using (Cell.LineStd) 
						GeneratorDraw.DrawGlobalVar(ref mapMagicObject.terrainSettings.material, "Material");
					
					using (Cell.LinePx(0))
						using (new Draw.FoldoutGroup(ref gen.guiAdvanced, "Advanced"))
						if (gen.guiAdvanced)
						{
							using (Cell.LineStd) 
							{
								Cell.current.fieldWidth = 0.15f;
								GeneratorDraw.DrawGlobalVar(ref GraphWindow.current.mapMagic.Globals.assignComponent, "Set Component");
							}
					
							#if __MICROSPLAT__
							if (GraphWindow.current.mapMagic.Globals.assignComponent)
								using (Cell.LineStd) 
									GraphWindow.current.mapMagic.Globals.microSplatPropData = GeneratorDraw.DrawGlobalVar<MicroSplatPropData>(
										GraphWindow.current.mapMagic.Globals.microSplatPropData==null ? null : (MicroSplatPropData)mapMagicObject.globals.microSplatPropData, 
										"PropData");
							#endif

							//using (Cell.LineStd) 
							//	Draw.Label("Apply Type");
							
							using (Cell.LineStd)
								GeneratorDraw.DrawGlobalVar(ref mapMagicObject.globals.microSplatApplyType, "Apply Type");
							
							using (Cell.LineStd)  
							{ 
								Cell.current.fieldWidth = 0.15f; 
								Cell.current.disabled = mapMagicObject.globals.microSplatApplyType==Globals.MicroSplatApplyType.Splats; //no tex names when applying  
								GeneratorDraw.DrawGlobalVar(ref mapMagicObject.globals.useCustomControlTextures, "Custom Splatmaps"); 
							}

							using (Cell.LineStd)  
							{ 
								Cell.current.fieldWidth = 0.15f; 
								Cell.current.disabled = mapMagicObject.globals.microSplatApplyType==Globals.MicroSplatApplyType.Splats; //no normals texture when applying only splats 
								GeneratorDraw.DrawGlobalVar(ref mapMagicObject.globals.microSplatNormals, "Add Normals Tex"); 
							}

							//using (Cell.LineStd)  
							//	{ Cell.current.fieldWidth = 0.15f; GeneratorDraw.DrawGlobalVar(ref GraphWindow.current.mapMagic.globals.microSplatTerrainDescriptor, "Add Descriptor"); }
						}

					if (Cell.current.valChanged)
						mapMagicObject.ApplyTerrainSettings();
				}
			else
				using (Cell.LinePx(18+18)) Draw.Label("Not assigned to current \nMapMagic object");

			using (Cell.LinePx(0)) CheckShader(gen);
			//using (Cell.LinePx(0)) CheckCustomSplatmaps(gen);

			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawMicroSplatLayer);
		}

		private static void DrawMicroSplatLayer (Generator tgen, int num)
		{
			MicroSplatOutput200 gen = (MicroSplatOutput200)tgen;
			MicroSplatOutput200.MicroSplatLayer layer = gen.layers[num];
			if (layer == null) return;

			Material microSplatMat = null;
			if (GraphWindow.current.mapMagic != null   &&  GraphWindow.current.mapMagic is MapMagicObject mmo)
				microSplatMat = mmo.terrainSettings.material;

			Cell.EmptyLinePx(3);
			using (Cell.LinePx(28))
			{
				//Cell.current.margins = new Padding(0,0,0,1); //1-pixel more padding from the bottom since layers are 1 pixel overlayed

				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);

				Cell.EmptyRowPx(10);

				//icon
				if (microSplatMat != lastMicroSplatMat  &&  microSplatMat != null  &&  microSplatMat.HasProperty("_Diffuse"))
				{
					lastIconsArr = (Texture2DArray)microSplatMat?.GetTexture("_Diffuse");
					lastMicroSplatMat = microSplatMat;
				}

				using (Cell.RowPx(28)) 
				{
					if (lastIconsArr != null) 
						Draw.TextureIcon(lastIconsArr, layer.channelNum);
				}

				Cell.EmptyRowPx(10);

				//index
				using (Cell.Row)
				{
					Cell.EmptyLine();
					using (Cell.LineStd)
					{
						int newIndex = Draw.Field(layer.channelNum, "Index");
						if (newIndex >= 0 && (lastIconsArr==null || newIndex < lastIconsArr.depth))
						{
							layer.channelNum = newIndex;
							layer.prototype = null;
						}
					}
					Cell.EmptyLine();
				}

				//terrain layer (if enabled)
				if (GraphWindow.current.mapMagic!=null  &&  
					GraphWindow.current.mapMagic is MapMagicObject mapMagicObject  && 
					mapMagicObject.globals.microSplatApplyType!=Globals.MicroSplatApplyType.Textures) //no need to create layers for textures-only mode
				{
					TerrainLayer tlayer = layer.prototype;
					if (tlayer == null)
						{ tlayer = new TerrainLayer(); layer.prototype = tlayer; }
					if (tlayer.diffuseTexture == null)
						tlayer.diffuseTexture = lastIconsArr.GetTexture(num);
				}

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
			Cell.EmptyLinePx(3);
		}

		public static void CheckShader (MicroSplatOutput200 gen)
		{
			if (GraphWindow.current.mapMagic == null   ||  !(GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)) return;

			Material mat = mapMagicObject.terrainSettings.material;
			if (mat==null || !mat.shader.name.Contains("MicroSplat"))
			{
				using (Cell.LinePx(50))
					using (Cell.Padded(3))
						Draw.Helpbox("The assigned material is not MicroSplat", UnityEditor.MessageType.Error);
			}
		}

		public static void CheckCustomSplatmaps (MicroSplatOutput200 gen)
		{
			if (GraphWindow.current.mapMagic == null   ||  !(GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)) return;

			Material mat = mapMagicObject.terrainSettings.material;
			if (mat != null && !mat.HasProperty("_CustomControl0"))
			{
				using (Cell.LinePx(60))
					using (Cell.Padded(3))
						Draw.Helpbox("Use Custom Splatmaps is not enabled in the material", UnityEditor.MessageType.Error);
			}
		}
	}
}

#endif