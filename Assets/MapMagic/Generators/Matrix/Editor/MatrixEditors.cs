using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.GUI;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes.GUI;
using MapMagic.Nodes.MatrixGenerators;

namespace MapMagic.Nodes.GUI
{
	public static class GeneratorEditors
	{
		[Draw.Editor(typeof(MatrixGenerators.Blur200))]
		public static void DrawSelector (MatrixGenerators.Blur200 gen)
		{
			//drawing standard class values

			using (Cell.Padded(1,1,0,0)) 
			{
				//radius warning
				MapMagicObject mapMagic = GraphWindow.current.mapMagic as MapMagicObject;
				if (mapMagic != null)
				{
					float pixelSize = mapMagic.tileSize.x / (int)mapMagic.tileResolution;
					if (gen.blur * gen.downsample > mapMagic.tileMargins * pixelSize)
					{
						Cell.EmptyLinePx(2);
						using (Cell.LinePx(40))
							using (Cell.Padded(2,2,0,0)) 
							{
								Draw.Element(UI.current.styles.foldoutBackground);
								using (Cell.LinePx(15)) Draw.Label("Current setup can");
								using (Cell.LinePx(15)) Draw.Label("create tile seams");
								using (Cell.LinePx(15)) Draw.URL("More", url:"https://gitlab.com/denispahunov/mapmagic/-/wikis/Tile_Seams_Reasons");
							}
						Cell.EmptyLinePx(2);
					}
				}
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.Curve200))]
		public static void DrawCurve (MatrixGenerators.Curve200 gen)
		{
			using (Cell.LinePx(GeneratorDraw.nodeWidth)) //square cell
				//using (Timer.Start("DrawCurve"))
			{
				Draw.Rect(new Color(1,1,1,0.5f)); //background

				using (Cell.Padded(5))
					CurveDraw.DrawCurve(gen.curve, gen.histogram);
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.Levels200))]
		public static void DrawLevels (MatrixGenerators.Levels200 gen)
		{
			using (Cell.LinePx( (GeneratorDraw.nodeWidth-10)/2 )) //square internal grid cell
				//using (Timer.Start("DrawLevels"))
			{
				Draw.Rect(new Color(1,1,1,0.5f));

				using (Cell.Padded(1,1,7,0))
					LevelsDraw.DrawLevels(ref gen.inMin, ref gen.inMax, ref gen.gamma, ref gen.outMin, ref gen.outMax, gen.histogram);
			}

			using (Cell.LineStd)
			{
				if (!gen.guiParams)
					Draw.Rect(new Color(1,1,1,0.5f));

				using (new Draw.FoldoutGroup(ref gen.guiParams, "Parameters", isLeft:true))
					if (gen.guiParams)
					{
						using (Cell.LineStd) { 
							Draw.Field(ref gen.inMin, "In Low");  
							Cell.current.Expose(gen.id, "inMin", typeof(float));
							Draw.AddFieldToCellObj(typeof(MatrixGenerators.Levels200), "inMin"); }
						using (Cell.LineStd) {
							Draw.Field(ref gen.gamma, "Gamma");
							Cell.current.Expose(gen.id, "gamma", typeof(float));
							Draw.AddFieldToCellObj(typeof(MatrixGenerators.Levels200), "gamma"); }
						using (Cell.LineStd) {
							Draw.Field(ref gen.inMax, "In High");
							Cell.current.Expose(gen.id, "inMax", typeof(float));
							Draw.AddFieldToCellObj(typeof(MatrixGenerators.Levels200), "inMax"); }

						Cell.EmptyLinePx(5);
												
						using (Cell.LineStd) {
							Draw.Field(ref gen.outMin, "Out Low");
							Cell.current.Expose(gen.id, "outMin", typeof(float));
							Draw.AddFieldToCellObj(typeof(MatrixGenerators.Levels200), "outMin"); }
						using (Cell.LineStd) {
							Draw.Field(ref gen.outMax, "Out High");
							Cell.current.Expose(gen.id, "outMax", typeof(float));
							Draw.AddFieldToCellObj(typeof(MatrixGenerators.Levels200), "outMax"); }
					}
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.Selector200))]
		public static void DrawSelector (MatrixGenerators.Selector200 gen)
		{
			using (Cell.Padded(1,1,0,0)) 
			{
				using (Cell.LineStd) Draw.Field(ref gen.rangeDet, "Set Range");
				using (Cell.LineStd) 
				{
					Draw.Field(ref gen.units, "Units");
					Cell.current.Expose(gen.id, "units", typeof(int));
					Draw.AddFieldToCellObj(typeof(MatrixGenerators.Selector200), "units");
				}

				if (gen.rangeDet == MatrixGenerators.Selector200.RangeDet.MinMax)
				{
					using (Cell.LineStd) 
					{
						Draw.Field(ref gen.from, "From");
						Cell.current.Expose(gen.id, "from", typeof(Vector2));
						Draw.AddFieldToCellObj(typeof(MatrixGenerators.Selector200), "from");
					}
					using (Cell.LineStd) 
					{
						Draw.Field(ref gen.to, "To");
						Cell.current.Expose(gen.id, "to", typeof(Vector2));
						Draw.AddFieldToCellObj(typeof(MatrixGenerators.Selector200), "to");
					}
				}
				else
				{
					float from = (gen.from.x + gen.from.y)/2;
					float to = (gen.to.x + gen.to.y)/2;
					float transition = (gen.from.y - gen.from.x);

					using (Cell.LineStd) 
					{
						Draw.Field(ref from, "From");
						Draw.AddFieldToCellObj(typeof(MatrixGenerators.Selector200), "from"); //not a single value, but the one with transition
					}
					using (Cell.LineStd) 
					{
						Draw.Field(ref to, "To");
						Draw.AddFieldToCellObj(typeof(MatrixGenerators.Selector200), "to");
					}
					using (Cell.LineStd) Draw.Field(ref transition, "Transition");

					gen.from.x = from-transition/2;
					gen.from.y = from+transition/2;
					gen.to.x = to-transition/2;
					gen.to.y = to+transition/2;
				}
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.HeightOutput200))]
		public static void HeightOutputEditor (MatrixGenerators.HeightOutput200 heightOut)
		{
			using (Cell.Padded(1,1,0,0))
			{
				using (Cell.LinePx(0))
				{
					Cell.current.fieldWidth = 0.4f;

					if (GraphWindow.current.mapMagic != null)
					{
						using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(ref GraphWindow.current.mapMagic.Globals.height, "Height");
						using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(ref GraphWindow.current.mapMagic.Globals.heightInterpolation, "Interpolate");
					}
					else
						using (Cell.LinePx(18+18)) Draw.Label("Not assigned to current \nMapMagic object");

					using (Cell.LineStd) Draw.Field(ref heightOut.outputLevel, "Out Level");

					if (Cell.current.valChanged)
						GraphWindow.current?.RefreshMapMagic(heightOut);
				}
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.UnityCurve200))]
		public static void CurveGeneratorEditor (MatrixGenerators.UnityCurve200 gen)
		{
			using (Cell.LinePx(GeneratorDraw.nodeWidth+4)) //don't really know why 4
				using (Cell.Padded(5))
				{
					Draw.AnimationCurve(gen.curve);
					Draw.AddFieldToCellObj(typeof(MatrixGenerators.UnityCurve200), "curve");

					if (Cell.current.valChanged)
						GraphWindow.current?.RefreshMapMagic(gen);
				}
		}

		[Draw.Editor(typeof(Blend200))]
		public static void BlendGeneratorEditor (Blend200 gen)
		{
			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawBlendLayer);
		}
		
		private static void DrawBlendLayer (Generator tgen, int num)
		{
			Blend200 gen = (Blend200)tgen;
			Blend200.Layer layer = gen.layers[num];

			Cell.EmptyLinePx(2);

			using (Cell.LineStd)
			{
				using (Cell.RowPx(0)) 
					GeneratorDraw.DrawInlet(layer.inlet, gen);
				Cell.EmptyRowPx(10);

				using (Cell.RowPx(20)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Layer"));

				if (num == 0)
				{
					layer.algorithm = MatrixGenerators.Blend200.BlendAlgorithm.add;
					using (Cell.Row) Draw.Label("Background");
				}

				else
				{
					if (!layer.guiExpanded)
					{
						using (Cell.Row) {
							layer.algorithm = Draw.Field(layer.algorithm); }
							//Draw.AddFieldToCellObj(typeof(MatrixGenerators.Blend200.Layer), "algorithm"); }
							//could not be exposed since it's layer value, not generator one
						using (Cell.RowPx(20)) layer.guiExpanded = Draw.LayerChevron(layer.guiExpanded);
					}

					else
					{
						using (Cell.Row) 
						{
							using (Cell.LineStd) {
								layer.algorithm = Draw.Field(layer.algorithm); }
								//Draw.AddFieldToCellObj(typeof(MatrixGenerators.Blend200.Layer), "algorithm"); }
							using (Cell.LineStd) 
							{
								Draw.FieldDragIcon(ref layer.opacity, UI.current.textures.GetTexture("DPUI/Icons/Opacity")); 
								Cell.current.Expose(gen.id, "layers", typeof(float), arrIndex:num); 
								//Draw.AddFieldToCellObj(typeof(MatrixGenerators.Blend200.Layer), "opacity"); }
							}
						}

						using (Cell.RowPx(20))
							using (Cell.LineStd) layer.guiExpanded = Draw.LayerChevron(layer.guiExpanded);
					}
				
					/*using (Cell.RowPx(35)) 
					{
						//Draw.Field(ref layer.opacity);
						Draw.FieldDragIcon(ref layer.opacity, UI.current.textures.GetTexture("DPUI/Icons/Opacity"));
					}*/
				}

				Cell.EmptyRowPx(3);
			}


			Cell.EmptyLinePx(2);
		}

		[Draw.Editor(typeof(Normalize200))]
		public static void NormalizeGeneratorEditor (Normalize200 gen)
		{
			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawNormalizeLayer);
		}
		
		private static void DrawNormalizeLayer (Generator tgen, int num)
		{
			Normalize200 gen = (Normalize200)tgen;
			Normalize200.NormalizeLayer layer = gen.layers[num];

			if (layer == null) return;

			using (Cell.LinePx(20))
			{
				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);

				Cell.EmptyRowPx(10);

				using (Cell.RowPx(73))
				{
					if (num==0) Draw.Label("Background");
					else Draw.Label("Layer " + num);
				}

				using (Cell.RowPx(10)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Opacity"));
				using (Cell.Row) layer.Opacity = Draw.Field(layer.Opacity);

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.GrassOutput200))]
		public static void DrawGrassOutput (MatrixGenerators.GrassOutput200 grassOut)
		{
			using (Cell.Padded(1,1,0,0)) 
			{
				//Cell.current.margins = new Padding(4,4,4,4);

				using (Cell.LinePx(0))
				{
					Cell.current.fieldWidth = 0.6f;

					using (Cell.LineStd) Draw.Field(ref grassOut.renderMode, "Mode");

					if (grassOut.renderMode == MatrixGenerators.GrassOutput200.GrassRenderMode.MeshUnlit || grassOut.renderMode == MatrixGenerators.GrassOutput200.GrassRenderMode.MeshVertexLit)
					{
						using (Cell.LineStd) grassOut.prototype.prototype = Draw.Field(grassOut.prototype.prototype, "Object");
						grassOut.prototype.prototypeTexture = null; //otherwise this texture will be included to build even if not displayed
						grassOut.prototype.usePrototypeMesh = true;
					}
					else
					{
						using (Cell.LineStd) grassOut.prototype.prototypeTexture = Draw.Field(grassOut.prototype.prototypeTexture, "Texture");
						grassOut.prototype.prototype = null; //otherwise this object will be included to build even if not displayed
						grassOut.prototype.usePrototypeMesh = false;
					}
					switch (grassOut.renderMode)
					{
						case MatrixGenerators.GrassOutput200.GrassRenderMode.Grass: grassOut.prototype.renderMode = DetailRenderMode.Grass; break;
						case MatrixGenerators.GrassOutput200.GrassRenderMode.Billboard: grassOut.prototype.renderMode = DetailRenderMode.GrassBillboard; break;
						case MatrixGenerators.GrassOutput200.GrassRenderMode.MeshVertexLit: grassOut.prototype.renderMode = DetailRenderMode.VertexLit; break;
						case MatrixGenerators.GrassOutput200.GrassRenderMode.MeshUnlit: grassOut.prototype.renderMode = DetailRenderMode.Grass; break;
					}
				}

				using (Cell.LinePx(0))
				{
					Cell.current.fieldWidth = 0.4f;

					using (Cell.LineStd) Draw.Field(ref grassOut.density, "Density");
					using (Cell.LineStd) grassOut.prototype.dryColor = Draw.Field(grassOut.prototype.dryColor, "Dry");
					using (Cell.LineStd) grassOut.prototype.healthyColor = Draw.Field(grassOut.prototype.healthyColor, "Healthy");

					Vector2 temp = new Vector2(grassOut.prototype.minWidth, grassOut.prototype.maxWidth);
					using (Cell.LineStd) Draw.Field(ref temp, "Width", xName:"Min", yName:"Max", xyWidth:25);
					grassOut.prototype.minWidth = temp.x; grassOut.prototype.maxWidth = temp.y;

					temp = new UnityEngine.Vector2(grassOut.prototype.minHeight, grassOut.prototype.maxHeight);
					using (Cell.LineStd) Draw.Field(ref temp, "Height", xName:"Min", yName:"Max", xyWidth:25);
					grassOut.prototype.minHeight = temp.x; grassOut.prototype.maxHeight = temp.y;

					using (Cell.LineStd) grassOut.prototype.noiseSpread = (float)Draw.Field(grassOut.prototype.noiseSpread, "Noise");

					#if UNITY_2021_2_OR_NEWER
					using (Cell.LineStd) grassOut.prototype.useInstancing = Draw.Toggle(grassOut.prototype.useInstancing, "Use Instancing");
					#endif

					if (GraphWindow.current.mapMagic != null  &&  GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)
					{
						using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(ref mapMagicObject.globals.grassResDownscale, "Downscale");
						using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(ref mapMagicObject.globals.grassResPerPatch, "Res/Patch");
					}
				}
			}
		}


		[Draw.Editor(typeof(TexturesOutput200))]
		public static void TexturesGeneratorEditor (TexturesOutput200 gen)
		{
			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:true);
			using (Cell.LinePx(0))  GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawTexturesLayer);
		}
		
		private static void DrawTexturesLayer (Generator tgen, int num)
		{
			TexturesOutput200 texOut = (TexturesOutput200)tgen;
			TexturesOutput200.TextureLayer layer = texOut.layers[num];
			if (layer == null) return;

			Cell.EmptyLinePx(3);
			using (Cell.LinePx(28))
			{
				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, texOut);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);
				
				Cell.EmptyRowPx(10);
					
				Texture2D tex = layer.prototype!=null ? layer.prototype.diffuseTexture : UI.current.textures.GetTexture("DPUI/Backgrounds/Empty");
				using (Cell.RowPx(28)) Draw.TextureIcon(tex); 

				using (Cell.Row)
				{
					Cell.current.trackChange = false;
					Draw.EditableLabel(ref layer.name);
				}

				using (Cell.RowPx(20)) 
				{
					Cell.current.trackChange = false;
					Draw.LayerChevron(num, ref texOut.guiExpanded);
				}

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
			Cell.EmptyLinePx(2);

			if (texOut.guiExpanded == num)
			using (Cell.Line)
			{
				Cell.EmptyRowPx(2);

				using (Cell.Row)
				{
					using (Cell.LinePx(0))
						using (Cell.Padded(1,0,0,0))
					{
						//using (Cell.LineStd) layer.Opacity = Draw.Field(layer.Opacity, "Opacity");

						using (Cell.LineStd) 
						{
							Draw.ObjectField(ref layer.prototype, "Layer");
							Cell.current.Expose(layer.id, "prototype", typeof(TerrainLayer));
						}

						if (layer.name == "Layer"  &&  layer.prototype != null)
							layer.name = layer.prototype.name;
					}
				
					if (layer.prototype != null)
					{
						Cell.EmptyLinePx(2);

						using (Cell.LineStd) 
							using (new Draw.FoldoutGroup(ref layer.guiProperties, "Properties"))
								if (layer.guiProperties)
						{
							//textures
							using (Cell.LineStd) 
							{
								Texture2D tex = layer.prototype.diffuseTexture;
								Draw.Field(ref tex, "Diffuse");
								if (Cell.current.valChanged)
								{
									if (layer.prototype.diffuseTexture.name == "WrColorPlaceholder2x2")
										GameObject.DestroyImmediate(layer.prototype.diffuseTexture); // removing temporary color texture if assigned
									layer.prototype.diffuseTexture = tex;
								}
							}

							using (Cell.LineStd) 
							{
								Texture2D tex = layer.prototype.normalMapTexture;
								Draw.Field(ref tex, "Normal");
								if (Cell.current.valChanged)
									layer.prototype.normalMapTexture = tex;
							}

							using (Cell.LineStd) 
							{
									Texture2D tex = layer.prototype.maskMapTexture;
									Draw.Field(ref tex, "Mask");
									if (Cell.current.valChanged)
										layer.prototype.maskMapTexture = tex;
							}

							//color (after texture)
							if (layer.prototype.diffuseTexture == null) 
							{
								layer.prototype.diffuseTexture = TextureExtensions.ColorTexture(2,2,layer.color);
								layer.prototype.diffuseTexture.name = "WrColorPlaceholder2x2";
							}

							if (layer.prototype.diffuseTexture.name == "WrColorPlaceholder2x2")
							{
								using (Cell.LineStd)
								{
									using (Cell.LineStd) Draw.Field(ref layer.color, "Color");
									if (Cell.current.valChanged) layer.prototype.diffuseTexture.Colorize(layer.color);
								}
							}


							using (Cell.LineStd) layer.prototype.specular = Draw.Field(layer.prototype.specular, "Specular");
							using (Cell.LineStd) layer.prototype.smoothness = Draw.Field(layer.prototype.smoothness, "Smooth");
							using (Cell.LineStd) layer.prototype.metallic = Draw.Field(layer.prototype.metallic, "Metallic");
							using (Cell.LineStd) layer.prototype.normalScale = Draw.Field(layer.prototype.normalScale, "N. Scale");
						}
				
						using (Cell.LineStd) 
							using (new Draw.FoldoutGroup(ref layer.guiTileSettings, "Tile Settings"))
								if (layer.guiTileSettings)
						{
							using (Cell.LineStd) layer.prototype.tileSize = Draw.Field(layer.prototype.tileSize, "Size");
							using (Cell.LineStd) layer.prototype.tileOffset = Draw.Field(layer.prototype.tileOffset, "Offset");
						}

						if (layer.guiTileSettings)
							Cell.EmptyLinePx(3);
					}

					
				}

				/*using (UI.FoldoutGroup(ref layer.guiRemapping, "Remapping", inspectorOffset:0, margins:0))
				if (layer.guiTileSettings)
				{
					using (Cell.LineStd)
					{
						Draw.Label("Red", cell:UI.Empty(Size.row));
						layer.prototype.diffuseRemapMin.x = Draw.Field(layer.prototype.diffuseRemapMin.x, cell:UI.Empty(Size.row));
					}
				}*/

				Cell.EmptyRowPx(2);
			}
		}


		[Draw.Editor(typeof(MatrixGenerators.CustomShaderOutput200))]
		public static void DrawCustomShaderOutput (MatrixGenerators.CustomShaderOutput200 gen) 
		{ 
			MatrixGenerators.CustomShaderOutput200 cso = (MatrixGenerators.CustomShaderOutput200)gen;
			string[] controlTextureNames = MatrixGenerators.CustomShaderOutput200.controlTextureNames;
			string[] controlTextureChannelNames = MatrixGenerators.CustomShaderOutput200.controlTextureNames;

			int texturesCount = MatrixGenerators.CustomShaderOutput200.controlTextureNames.Length;
			using (Cell.LineStd) Draw.Field(ref texturesCount, "Textures Count");
			if (texturesCount != MatrixGenerators.CustomShaderOutput200.controlTextureNames.Length)
				ArrayTools.Resize(ref controlTextureNames, texturesCount, i=> "_ControlTexture"+i);

			using (Cell.LineStd) Draw.Label("Texture Names:");
			for (int i=0; i<controlTextureNames.Length; i++)
				using (Cell.LinePx(20)) 
				{
					Cell.current.fieldWidth = 0.9f;
					Draw.Field(ref controlTextureNames[i], i+":");
				}

			if (controlTextureNames==null || controlTextureChannelNames.Length!=controlTextureNames.Length*4)
				controlTextureChannelNames = new string[controlTextureNames.Length*4];
			for (int i=0; i<controlTextureNames.Length; i++)
			{
				controlTextureChannelNames[i*4] = controlTextureNames[i] + " R";
				controlTextureChannelNames[i*4+1] = controlTextureNames[i] + " G";
				controlTextureChannelNames[i*4+2] = controlTextureNames[i] + " B";
				controlTextureChannelNames[i*4+3] = controlTextureNames[i] + " A";
			}

			//using (Cell.Line)
			//	DrawCustomMaterialWarning();

			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawCustomShaderLayer);
		}
		
		private static void DrawCustomShaderLayer (Generator tgen, int num)
		{
			CustomShaderOutput200 gen = (CustomShaderOutput200)tgen;
			CustomShaderOutput200.CustomShaderLayer layer = gen.layers[num];
			if (layer == null) return;

			using (Cell.LinePx(32))
			{
				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);
				
				Cell.EmptyRowPx(10);

				using (Cell.Row) Draw.PopupSelector(ref layer.channelNum, MatrixGenerators.CustomShaderOutput200.controlTextureNames, null);

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
		}


		[Draw.Editor(typeof(DirectTexturesOutput200))]
		public static void DrawDirectTexturesOutput (DirectTexturesOutput200 gen) 
		{ 
			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:false);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawDirectTexturesLayer);
		}
		
		private static void DrawDirectTexturesLayer (Generator tgen, int num)
		{
			DirectTexturesOutput200 gen = (DirectTexturesOutput200)tgen;
			DirectTexturesOutput200.DirectTexturesLayer layer = gen.layers[num];
			if (layer == null) return;

			Cell.EmptyLinePx(2);
			using (Cell.LineStd)
			{
				using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				
				Cell.EmptyRowPx(10);
					
				using (Cell.Row)
				{
					//Cell.current.trackChange = false;
					Draw.EditableLabel(ref layer.name);
				}

				using (Cell.RowPx(55))
				{
					//Cell.current.trackChange = false;
					Draw.PopupSelector(ref layer.channelNum, channelLabels);
				}

				Cell.EmptyRowPx(4);
				//using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
			Cell.EmptyLinePx(2);
		}

		private static readonly string[] channelLabels = new string[4]{ "Red", "Green", "Blue", "Alpha" };


		[Draw.Editor(typeof(DirectMatricesOutput200))]
		public static void DrawDirectMapsOutput (DirectMatricesOutput200 gen) 
		{ 
			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:false);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawDirectMatricesLayer);
		}
		
		private static void DrawDirectMatricesLayer (Generator tgen, int num)
		{
			DirectMatricesOutput200 gen = (DirectMatricesOutput200)tgen;
			DirectMatricesOutput200.DirectMatricesLayer layer = gen.layers[num];
			if (layer == null) return;

			using (Cell.LinePx(28))
			{
				using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				
				Cell.EmptyRowPx(10);
					
				using (Cell.Row)
				{
					//Cell.current.trackChange = false;
					Draw.EditableLabel(ref layer.name);
				}

				Cell.EmptyRowPx(4);
				//using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
			Cell.EmptyLinePx(2);
		}


		//public static void DrawMicroSplatLayer (LayersGenerator.Layer target, Graph graph, Generator gen, int num)
		//in Compatibility/Editor

		[Draw.Editor(typeof(Placeholders.InletOutletPlaceholder))]
		[Draw.Editor(typeof(Placeholders.InletPlaceholder))]
		[Draw.Editor(typeof(Placeholders.OutletPlaceholder))]
		[Draw.Editor(typeof(Placeholders.Placeholder))]
		public static void DrawPlaceholder (Placeholders.GenericPlaceholder placeholder)
		{
			using (Cell.LinePx(80))
				Draw.Helpbox ("Generator type not found. It might be a custom generator, or a generator from the package that has not been installed.");
		}


		/*[Draw.Editor(typeof(Placeholders.InletOutletPlaceholder), cat="Header")]
		public static void DrawPlaceholderHeader (Placeholders.GenericPlaceholder placeholder)
		{
			using (Cell.LinePx(0))
			{
				using (Cell.Row)
				{
					foreach (IInlet<object> inlet in placeholder.inlets)
					{
						if (inlet == null) continue;
						using (Cell.LineStd)
						{
							using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(inlet, placeholder);
							Cell.EmptyRowPx(8);
							//using (Cell.Row) Draw.Label(fn.inlets[i].Name);
						}
					}
				}
			}
		}*/


		#region Warnings

			/*public static void DrawCustomMaterialWarning ()
			{
				Terrains.TerrainSettings settings = GraphWindow.current.mapMagic.terrainSettings;
				if (settings.materialType != Terrain.MaterialType.Custom)
				{
					using (Cell.LinePx(56))
					{
						//Cell.current.margins = new Padding(4);

						GUIStyle backStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/Foldout");
						Draw.Element(backStyle);
						Draw.Element(backStyle);

						using (Cell.Row) Draw.Label("Material Type \nis not switched \nto Custom.");

						using (Cell.RowPx(30))
							if (Draw.Button("Fix"))
							{
								settings.materialType = Terrain.MaterialType.Custom;
								GraphWindow.current.mapMagic.ApplyTerrainSettings();

								GraphWindow.current.mapMagic.ClearAllNodes();
								GraphWindow.current.mapMagic.StartGenerate();
							}
					}
					Cell.EmptyLinePx(5);
				}
			}*/


			public static void DrawMegaSplatShaderNameWarning ()
			{
				if (GraphWindow.current.mapMagic == null  ||  !(GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)) return;

				Terrains.TerrainSettings settings = mapMagicObject.terrainSettings;
				{
					using (Cell.LinePx(70))
					{
						//Cell.current.margins = new Padding(4);

						GUIStyle backStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/Foldout");
						Draw.Element(backStyle);
						Draw.Element(backStyle);

						using (Cell.Row) Draw.Label("No MegaSplat material \nis assigned as \nCustom Material in \nTerrain Settings");

						using (Cell.RowPx(30))
							if (Draw.Button("Fix"))
							{
								Shader shader = ReflectionExtensions.CallStaticMethodFrom("Assembly-CSharp-Editor", "SplatArrayShaderGUI", "NewShader", null) as Shader;
								settings.material = new Material(shader);
								settings.material.EnableKeyword("_TERRAIN");

								mapMagicObject.ApplyTerrainSettings();

								GraphWindow.current.RefreshMapMagic();
							}
					}
					Cell.EmptyLinePx(5);
				}
			}


			public static void DrawMegaSplatAssignedTextureArraysWarning ()
			{
				if (GraphWindow.current.mapMagic == null  ||  !(GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)) return;

				Terrains.TerrainSettings settings = mapMagicObject.terrainSettings;
				{
					using (Cell.LinePx(70))
					{
						//Cell.current.margins = new Padding(4);

						GUIStyle backStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/Foldout");
						Draw.Element(backStyle);
						Draw.Element(backStyle);

						using (Cell.Row) Draw.Label("Material has \nno Albedo/Height \nTexture Array \nassigned");
						
						using (Cell.RowPx(30))
							if (Draw.Button("Fix"))
							{
								Shader shader = ReflectionExtensions.CallStaticMethodFrom("Assembly-CSharp-Editor", "SplatArrayShaderGUI", "NewShader", null) as Shader;
								settings.material = new Material(shader);
								settings.material.EnableKeyword("_TERRAIN");

								mapMagicObject.ApplyTerrainSettings();

								GraphWindow.current.RefreshMapMagic();
							}
					}
					Cell.EmptyLinePx(5);
				}
			}


			public static void UpdateMaterial ()
			{
				if (GraphWindow.current.mapMagic == null  ||  !(GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)) return;

				Renderer renderer = mapMagicObject.gameObject.GetComponent<Renderer>();
				if (renderer == null) return;

				if (mapMagicObject.terrainSettings.material != renderer.sharedMaterial)
				{
					mapMagicObject.terrainSettings.material = renderer.sharedMaterial;
					mapMagicObject.ApplyTerrainSettings();
				}
			}
		#endregion
	}
}