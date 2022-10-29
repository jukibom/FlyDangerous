using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
//using Den.Tools.Segs;
using Den.Tools.Splines;
using MapMagic.Core;  //used once to get tile size
using MapMagic.Products;
using MapMagic.Expose.GUI;
using MapMagic.Expose;

namespace MapMagic.Nodes.GUI
{
	public static partial class GeneratorDraw
	{
		public const int nodeWidth = 150;
		public const int miniWidth = 140;
		public const int headerHeight = 24;
		public const int inletOutletDragArea = 20;
		public static readonly RectOffset shadowBorders = new RectOffset(38,38,38,38);
		public static readonly RectOffset shadowOverflow = new RectOffset(32, 32, 32, 32);
		public static readonly RectOffset frameBorders = new RectOffset(1, 1, 1, 1);
		public static readonly RectOffset selectionBorders = new RectOffset(2, 2, 2, 2);
		public const int portalHeight = 26;

		private const float miniInletsScale = 0.875f / GraphWindow.miniZoom;
		private const float miniInletLineHeight = 12 / GraphWindow.miniZoom;
		private static GUIStyle miniNameStyle;
		private static GUIStyle miniInletStyle;


		public static void DrawGeneratorOrPortal (Generator gen, Graph graph, bool isMini=false, bool selected=false, bool activeLinks=true)
		{
			Type genType = gen.GetType();
			GeneratorMenuAttribute menuAtt = GeneratorDraw.GetMenuAttribute(genType);

			if (menuAtt.lookLikePortal)
				GeneratorDraw.DrawPortal(gen, graph, selected:selected, isMini:isMini);

			else
			{
				try 
				{ 
					if (!isMini) GeneratorDraw.DrawGenerator(gen, graph, selected:selected, menuAtt:menuAtt); 
					else GeneratorDraw.DrawMini(gen, graph, selected:selected, menuAtt:menuAtt);
				}
				catch (ExitGUIException)
					{ } //ignoring
				catch (Exception e) 
					{ Debug.LogError($"Draw Generator {genType} failed: \n" + e); }
			}
		}


		public static void DrawGenerator (Generator gen, Graph graph, bool selected=false, bool activeLinks=true, GeneratorMenuAttribute menuAtt=null)
		{
			if (UI.current.layout)
				UI.current.cellObjs.ForceAdd(gen, Cell.current, "Generator");
			
			if (menuAtt == null)
			{
				Type genType = gen.GetType();
				menuAtt = GetMenuAttribute(genType);
			}

			//background
			if (!UI.current.layout)
			{
				//shadow
				GUIStyle shadowStyle = UI.current.textures.GetElementStyle(selected ? "MapMagic/Node/SelectionShadow" : "MapMagic/Node/Shadow", 
					borders:shadowBorders,
					overflow:shadowOverflow);
				#if !MM_DEBUG
					Draw.Element(shadowStyle);
				#else
					if (graph.debugGraphBackground)
						Draw.Element(shadowStyle);
				#endif

				//frame
				GUIStyle frameStyle = UI.current.textures.GetElementStyle(selected ? "MapMagic/Node/SelectionFrame" : "MapMagic/Node/Frame", 
					borders:selected ? selectionBorders : frameBorders,
					overflow:selected ? selectionBorders : frameBorders);
				Draw.Element(frameStyle);

				//gray field color (background to all node, including the header)
				GUIStyle fieldBackStyle = UI.current.textures.GetElementStyle("MapMagic/Node/Background");
				Draw.Element(fieldBackStyle);
			}

			//header
//			using (Timer.Start("Header"))
			using (Cell.LinePx(24)) //(Cell.LinePx(0)))
			{ 
				DrawHeader(gen, menuAtt, activeLinks);
			}


			//field
			bool fieldDrawn = false;
//			using (Timer.Start("Field"))
			using (Cell.LinePx(0))
			{
				Cell.current.fieldWidth = 0.44f;

				using (Cell.LinePx(0))
				{
					Cell.EmptyRowPx(1);

					using (Cell.Row) 
					{
						if (!menuAtt.advancedOptions)
							fieldDrawn = CellExpose.ExposableClass(gen, gen.id);

						else
						{
							fieldDrawn = true;

							using (Cell.LinePx(0))
								CellExpose.ExposableClass(gen, gen.id);

							Cell.EmptyLinePx(2);
							using (Cell.LinePx(0))
								using (new Draw.FoldoutGroup(ref gen.guiAdvanced, "Advanced", padding:0))
									if (gen.guiAdvanced)
										CellExpose.ExposableClass(gen, gen.id, category:"Advanced");
						}
					}

					Cell.EmptyRowPx(1);
				}

				using (Cell.LinePx(0))
					fieldDrawn = Draw.Editor(gen) || fieldDrawn;

				if (Cell.current.valChanged)
					GraphWindow.current?.RefreshMapMagic(gen);
			}
			

			//debug
			#if MM_DEBUG
			if (graph.debugGenInfo)
			{
				DrawDebug(gen);
				fieldDrawn = true;
			}
			#endif

			//preview
			if (gen is IOutlet<object> &&  gen.guiPreview)
			{
				using (Cell.LinePx(nodeWidth))
					Previews.PreviewDraw.DrawPreview((IOutlet<object>)gen);
			}


			//footer
			float footerHeight = (gen is IOutlet<object>) ? 10 : 4;
			using (Cell.LinePx(footerHeight)) 
//				using (Timer.Start("Footer"))
			{
				Color color = menuAtt.color;
				if (!gen.enabled) color = DisableGeneratorColor(color);
				Texture2D footerTex = UI.current.textures.GetColorizedTexture(fieldDrawn ? "MapMagic/Node/Footer" : "MapMagic/Node/FooterNofield", color); 
				Draw.Texture(footerTex);

				if (gen is IOutlet<object>)
				{
					Cell.EmptyRow();

					Texture2D chevronIcon = UI.current.textures.GetTexture(gen.guiPreview ? "DPUI/Chevrons/TickUp" : "DPUI/Chevrons/TickDown");
					using (Cell.RowPx(100)) Draw.CheckButton(ref gen.guiPreview, chevronIcon, visible:false);

					Cell.EmptyRow();
				}
			}


			if (!UI.current.layout)
			{
				gen.guiSize.y = Cell.current.finalSize.y;
				gen.guiSize.x = nodeWidth;
			}
		}


		public static void DragGenerator (Generator gen, HashSet<Generator> otherSelected=null)
		/// Returns the position of generator while dragging
		{
			if (UI.current.layout) return;

			Cell cell = UI.current.cellObjs.GetCell(gen, "Generator");

			if (DragDrop.TryDrag(cell, UI.current.mousePos))
			{
				gen.guiPosition = MoveGenerator(cell, DragDrop.initialRect.position + DragDrop.totalDelta);

				//dragging other selected
				if (otherSelected!=null  &&  otherSelected.Contains(gen)) //if clicked one of selected
				{
					foreach (Generator ogen in otherSelected)
						if (ogen != gen) //to avoid moving twice
						{
							Cell ogenCell = UI.current.cellObjs.GetCell(ogen, "Generator");
							ogen.guiPosition = MoveGenerator(ogenCell, ogen.guiPosition + DragDrop.currentDelta);
						}
				}
			}

			DragDrop.TryRelease(cell);

			DragDrop.TryStart(cell, UI.current.mousePos, cell.InternalRect);
		}

		public static Vector2 MoveGenerator (Cell cell, Vector2 moveTo)
		/// Internal move function for DragGenerator
		/// Returns where was generator actually moved
		{
			GraphWindow.current.graphUI.undo.Record();

			if (Event.current.control)
			{
				moveTo.x = (int)(float)(moveTo.x / 32 + 0.5f) * 32;
				moveTo.y = (int)(float)(moveTo.y / 32 - 0.5f) * 32;
			}

			float dpiFactor = UI.current.DpiScaleFactor;
			float zoom = UI.current.scrollZoom != null ? UI.current.scrollZoom.zoom : 1;

			Vector2 roundVal = new Vector2(  //0.5002 prevents cells un-align for the reason I don't remember
				moveTo.x > 0  ?  0.5002f  :  -0.5002f,
				moveTo.y > 0  ?  0.5002f  :  -0.5002f );

			moveTo.x = (int)(float)(moveTo.x*dpiFactor*zoom + roundVal.x) / (dpiFactor*zoom);  
			moveTo.y = (int)(float)(moveTo.y*dpiFactor*zoom + roundVal.y) / (dpiFactor*zoom);

			//gen.guiPosition = moveTo;

			cell.worldPosition = moveTo;
			cell.CalculateSubRects(); //re-layout cell

			return moveTo;
		}


		public static void DrawHeader  (Generator gen, GeneratorMenuAttribute menuAtt, bool activeLinks=true)
		{
			Color color = menuAtt.color;
			if (!gen.enabled) color = DisableGeneratorColor(color);
			Texture2D headerTex = UI.current.textures.GetColorizedTexture("MapMagic/Node/Header", color); 
			//GUIStyle headBackStyle = UI.current.textures.GetElementStyle(headerTex);//, borders:new RectOffset(40,40,3,0));
			Draw.Texture(headerTex);

			//label & inline inlets/outlets
			using (Cell.LinePx(24))
			{
				if (gen is IInlet<object> inletGen)
				{
					using (Cell.RowPx(0)) 
						DrawInlet(inletGen, gen, addCellObj:activeLinks);
					
					Cell.EmptyRowPx(10);
					using (Cell.Row) Draw.Label(menuAtt.nameUpper, style:UI.current.styles.bigLabel);

					if (menuAtt.nameWidth < nodeWidth-24 -10 -(gen is IOutlet<object> ? 10 : 0))
						using (Cell.RowPx(24)) Draw.Icon(menuAtt.icon, scale:0.5f);
				}

				else
				{
					using (Cell.Row) Draw.Label(menuAtt.nameUpper, style:UI.current.styles.bigLabel);

					if (menuAtt.nameWidth < nodeWidth-24)
						using (Cell.RowPx(24)) Draw.Icon(menuAtt.icon, scale:0.5f); 
				}

				if (gen is IOutlet<object> outletGen)
				{
					Cell.EmptyRowPx(10);
					using (Cell.RowPx(0)) 
						DrawOutlet(outletGen, addCellObj:activeLinks);
				}
			}

			//multi inlets/outlets
			if (gen is IMultiInlet  ||  gen is IMultiOutlet)
				using (Cell.LinePx(0))
				{
					using (Cell.Row)
					{
						if (gen is IMultiInlet multiInGen)
						{
							ValAttribute[] inletVals = GetInletVals(menuAtt.type);

							foreach (ValAttribute val in inletVals)
								using (Cell.LineStd)
								{
									using (Cell.RowPx(0))
									{
										IInlet<object> inlet = (IInlet<object>)val.field.GetValue(gen);
										DrawInlet(inlet, gen, addCellObj:activeLinks); 
									}
									Cell.EmptyRowPx(10);
									using (Cell.Row) Draw.Label(val.name); 
								}
						}
					}

					using (Cell.Row)
					{
						if (gen is IMultiOutlet multiOutGen)
						{
							ValAttribute[] outletVals = GetOutletVals(menuAtt.type);

							foreach (ValAttribute val in outletVals)
								using (Cell.LineStd)
								{							
									using (Cell.Row) Draw.Label(val.name, UI.current.styles.rightLabel);
									Cell.EmptyRowPx(12);

									using (Cell.RowPx(0))
									{
										IOutlet<object> outlet = (IOutlet<object>)val.field.GetValue(gen);
										DrawOutlet(outlet, addCellObj:activeLinks); 
									}
								}
						}

					}
				}

			//custom header editor
			using (Cell.LinePx(0))
				Draw.Editor(gen, cat:"Header"); 
		}


		public static void DrawLayersAddRemove<T>  (Generator gen, ref T[] layers, bool inversed=false, bool unlinkBackground=false) where T : class, new() =>
			layers = DrawLayersAddRemove (gen, layers, inversed);

		public static T[] DrawLayersAddRemove<T> (Generator gen, T[] layers, bool inversed=false, bool unlinkBackground=false) where T : class, new()
		{
			float backCol = StylesCache.isPro ? 0.25f : 0.66f; 
			Draw.Rect( new Color(backCol, backCol, backCol) );

			Cell layersCell = Cell.Parent;

			using (Cell.LinePx(20)) 
				LayersEditor.DrawAddRemove(layersCell, "Layers", 
					onAdd: n => AddLayer<T>(gen, ref layers, inversed, n),
					onRemove: n => RemoveLayer<T>(gen, ref layers, inversed, n, unlinkBackground),
					onMove: (f,t) => MoveLayer<T>(gen, ref layers, inversed, f, t, unlinkBackground) );

			if (Cell.current.valChanged  &&  GraphWindow.current.mapMagic!=null)
				GraphWindow.current?.RefreshMapMagic(gen);

			return layers;
		}

		public static void DrawLayersThemselves<T> (Generator gen, T[] layers, bool inversed=false, Action<Generator,int> layerEditor=null) where T : class
		{
			float backCol = StylesCache.isPro ? 0.25f : 0.66f; 
			Draw.Rect( new Color(backCol, backCol, backCol) );

			Cell layersCell = Cell.Parent;

			using (Cell.LinePx(0)) 
				using (Cell.Padded(-1,-1,0,0))
					LayersEditor.DrawLayersThemselves(layersCell, layers.Length, 
						onDraw: n => DrawLayer<T>(gen, ref layers, inversed, n, layerEditor), 
						roundBottom:false);

			//if (Cell.current.valChanged  &&  GraphWindow.current.mapMagic!=null)
			//	GraphWindow.RefreshMapMagic(gen);
			//Already called from DrawGenerator (field)
		}

		private static void DrawLayer<T> (Generator gen, ref T[] layers, bool inversed, int num, Action<Generator,int> layerEditor) where T : class
		{
			if (inversed) num = layers.Length-1 - num; //inversed num

			UI.current.cellObjs.ForceAdd(layers[num], Cell.current, "Layer");

			Draw.Class(layers[num]);
			Draw.Editor(layers[num], new object[] {num, gen} );
			layerEditor?.Invoke(gen, num);
		}

		private static void AddLayer<T> (Generator gen, ref T[] layers, bool inversed, int num) where T : class, new()
		{
			num = inversed ? layers.Length : 0;

			T layer = new T(); 
			//(T)Activator.CreateInstance(layerType); //object layer = new T(); // layGen.CreateLayer();

			if (layer is IUnit unitLayer) { unitLayer.SetGen(gen); unitLayer.Id = Id.Generate(); }
			//if (layer is IInlet<object> inletLayer) { inletLayer.SetGen(gen); inletLayer.Id = gen.NewLayerId; }
			//if (layer is IOutlet<object> outletLayer) { outletLayer.SetGen(gen); outletLayer.Id = gen.NewLayerId; }
			//inlets and outlets are already IUnit

			ArrayTools.Insert(ref layers, num, layer);
		}

		private static void RemoveLayer<T> (Generator gen, ref T[] layers, bool inversed, int num, bool unlinkBackground) where T : class
		///IDEA: To graph or generator since it uses not much of gui stuff (but usesGraphWindow.current.graph)
		{
			if (inversed) num = layers.Length-1 - num;

			if (layers[num] is IOutlet<object> outlet) GraphWindow.current.graph.UnlinkOutlet(outlet);
			if (layers[num] is IInlet<object> inlet) GraphWindow.current.graph.UnlinkInlet(inlet);
			
			if (layers[num] is IUnit unitLayer)
				GraphWindow.current.graph.exposed.Remove(unitLayer.Id);

			ArrayTools.RemoveAt(ref layers, num);

			//unlinking background inlet (we could remove tthe background layer and now here's the new one)
			if (unlinkBackground  &&  layers[0] is IInlet<object> nInlet)
				GraphWindow.current.graph.UnlinkInlet(nInlet);
		}

		private static void MoveLayer<T> (Generator gen, ref T[] layers, bool inversed, int from, int to, bool unlinkBackground) where T : class
		{
			if (inversed) 
			{
				from = layers.Length-1 - from;
				to = layers.Length-1 - to;
			}

			ArrayTools.Move(layers, from, to);
				
			//if (layGen.Expanded == from) layGen.Expanded = to;
			//if (layGen.Expanded == to) layGen.Expanded = from;

			//unlinking background inlet
			if (unlinkBackground  &&  layers[0] is IInlet<object> nInlet)
				GraphWindow.current.graph.UnlinkInlet(nInlet);
		}


		private static void DrawDebug (Generator gen)
		{
			using (Cell.LineStd) Draw.Label("Id: " + Id.ToString(gen.id));
			using (Cell.LineStd) Draw.Label(gen.id.ToString());


			TileData previewData = null;
			if (GraphWindow.current.mapMagic is MapMagicObject mapMagicObject) previewData = mapMagicObject?.PreviewData;

			if (previewData != null)
			{
				Graph rootGraph = GraphWindow.current.mapMagic.Graph;

				foreach ((Graph subGraph, TileData subData) in Graph.SubGraphsDatas(rootGraph,previewData))
				{
					if (!subGraph.ContainsGenerator(gen))
						continue;

					Cell.EmptyLinePx(5);
					using (Cell.LineStd) Draw.Toggle(subData.IsReady(gen), "Ready");
					if (gen is ICustomComplexity)
						using (Cell.LineStd) Draw.DualLabel("Progress", subData.GetProgress((ICustomComplexity)gen).ToString());
					if (gen is IOutlet<object> outlet)
					{
						object product = subData.ReadOutletProduct(outlet);
						string hashString = "null";
						if (product != null)
						{
							int hashCode = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(product);
							hashString = Convert.ToBase64String( BitConverter.GetBytes(hashCode) );
						}
						using (Cell.LineStd) Draw.DualLabel("Product", hashString);
					}
				}
			}
			using (Cell.LineStd) Draw.DualLabel("Time Draft", gen.draftTime.ToString("0.00") + "ms");
			using (Cell.LineStd) Draw.DualLabel("Time Main", gen.mainTime.ToString("0.00") + "ms");
		}


		public static void SelectGenerators (HashSet<Generator> selection, bool shiftForSingleSelect=false)
		/// Using cell lut as a list of all generators
		/// shiftForSingleSelect selects sinhgle gens only when shift is pressed. Otherwise selected by click
		{
			//one by one
			bool genClicked = false;
			if ((Event.current.shift || !shiftForSingleSelect)  &&  Event.current.type == EventType.MouseDown  &&  Event.current.button == 0)
			{
				//returning if clicked to already selected generator (to move several generators it in mini)
				if (!shiftForSingleSelect)
				{
					foreach (Cell cell in UI.current.cellObjs.GetAllCells("Generator"))
						if (cell.Contains(UI.current.mousePos))
						{
							Generator gen = UI.current.cellObjs.GetObject<Generator>(cell, "Generator");
							if (selection.Contains(gen))
								return;
						}
				}

				//selecting
				foreach (Cell cell in UI.current.cellObjs.GetAllCells("Generator"))
					if (cell.Contains(UI.current.mousePos))
					{
						Generator gen = UI.current.cellObjs.GetObject<Generator>(cell, "Generator");

						//if (selection.Contains(gen)) selection.Remove(gen);
						//else selection.Add(gen);
						selection.Clear();
						selection.Add(gen);

						genClicked = true;
						break;
					}
			}

			//selection frame
			if (!genClicked)
			{
				if (DragDrop.TryDrag("NodeSelectionFrame"))
				{
					GUIStyle frameStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/SelectionFrame");

					Rect rect = new Rect(DragDrop.initialMousePos, UI.current.mousePos-DragDrop.initialMousePos);
					rect = rect.TurnNonNegative();

					Draw.Element(rect, frameStyle);
				}

				if (DragDrop.TryRelease("NodeSelectionFrame")  &&  !UI.current.layout)
				{
					selection.Clear();

					Rect frameRect = new Rect(DragDrop.initialMousePos, UI.current.mousePos-DragDrop.initialMousePos);
					frameRect = frameRect.TurnNonNegative();

					foreach (Cell cell in UI.current.cellObjs.GetAllCells("Generator")) //(var kvp in genCellLut.d1)
					{
						Generator gen = UI.current.cellObjs.GetObject<Generator>(cell, "Generator");

						Rect genRect = new Rect(gen.guiPosition, cell.finalSize);
						if (frameRect.Contains(genRect))
							{ if (!selection.Contains(gen)) selection.Add(gen); }
						//else
						//	{ if (selection.Contains(gen)) selection.Remove(gen); }
						//clearing selection anyways
					}

					genClicked = true;
				}

				if (Event.current.shift  &&  Event.current.type == EventType.MouseDown   &&  Event.current.button == 0)
					DragDrop.ForceStart("NodeSelectionFrame", UI.current.mousePos, new Rect(0,0,0,0));
			}

			if (genClicked)
				UI.RemoveFocusOnControl();
		}

		public static void DeselectGenerators (HashSet<Generator> selection)
		{
			//if non-shift clicked any other place rather than selected node - deselecting
			if (!UI.current.layout  &&  
				selection.Count != 0  &&  
				Event.current.type == EventType.MouseDown  &&  
				Event.current.button == 0  &&  
				!Event.current.shift)
				{
					//deselecting if dragging anything else but generator
					if (DragDrop.obj != null  &&  DragDrop.obj is Cell cell  &&  UI.current.cellObjs.ContainsCell(cell, "Generator"))// genCellLut.d1.ContainsKey(cell))
						return;

					selection.Clear();
					UI.RemoveFocusOnControl();
					UI.current.editorWindow?.Repaint();
				}
		}


		public static void DrawInlet (IInlet<object> inlet, Generator gen, float scale=1, bool addCellObj=true)
		/// Drawing in a 0-pixel of a cell
		{
			Cell inletCell;

			Graph graph = GraphWindow.current.graph;

			Color color = GetLinkColor(inlet);

			//drawing
			using (inletCell = Cell.Center(0, 0))
			{
				if (UI.current.layout  &&  addCellObj) //adding on layout to draw links before generators
					UI.current.cellObjs.ForceAdd(inlet, Cell.current, "Inlet");

				if (!UI.current.layout)
				{
					IOutlet<object> outlet = graph.GetLink(inlet);

					Texture2D icon = UI.current.textures.GetColorizedTexture("MapMagic/InletOutlet", color);
					Draw.Icon(icon, scale:0.5f*scale);
				}
			}

			//dragging
			if (!UI.current.layout) 
			{
				IOutlet<object> outlet = null;

				if (DragDrop.TryDrag(inletCell, UI.current.mousePos))
				{
					Cell outletCell = null;

					foreach (Cell cell in UI.current.cellObjs.GetAllCells("Outlet"))
						if (cell.Contains(UI.current.mousePos, (int)(inletOutletDragArea/2 * scale))) //outlet cell size is 0, using 10 pixel padding
						{
							outletCell = cell;
							outlet = UI.current.cellObjs.GetObject<IOutlet<object>>(cell, "Outlet");
						}

					
					color.a = 0.5f;	

					if (outlet != null) 
					{
						if (!graph.CheckLinkValidity(outlet, inlet)) color = Color.red;
						DrawLink(StartCellLinkpos(outletCell), EndCellLinkpos(inletCell), color);
					}
					else 
						DrawLink(UI.current.mousePos, EndCellLinkpos(inletCell), color);
				}

				if (DragDrop.TryRelease(inletCell, UI.current.mousePos))
				{
					GraphWindow.RecordCompleteUndo();

					if (outlet == null)
						graph.UnlinkInlet(inlet);
					else if (graph.CheckLinkValidity(outlet,inlet))
						graph.Link(inlet, outlet);

					GraphWindow.current?.RefreshMapMagic(gen, outlet?.Gen);
				}

				Rect inletRect = new Rect(
					inletCell.worldPosition.x - inletOutletDragArea/2*scale,
					inletCell.worldPosition.y - inletOutletDragArea/2*scale,
					inletCell.finalSize.x + inletOutletDragArea*scale,
					inletCell.finalSize.x + inletOutletDragArea*scale);
				DragDrop.TryStart(inletCell, UI.current.mousePos, inletRect);
			}
		}


		public static void DrawOutlet (IOutlet<object> outlet, float scale=1, bool addCellObj=true)
		/// Requires 0-width cell, draws outlet in the center
		{
			//if (UI.current.layout) return;
			//no optimize!

			Graph graph = GraphWindow.current.graph;

			Color color = GetLinkColor(outlet);

			//drawing
			Cell outletCell;
			using (outletCell = Cell.Center(0,0))
			{
				if (UI.current.layout && addCellObj)
					UI.current.cellObjs.ForceAdd(outlet, Cell.current, "Outlet");

				if (!UI.current.layout)
				{
					Texture2D icon = UI.current.textures.GetColorizedTexture("MapMagic/InletOutlet", color);
					Draw.Icon(icon, scale:0.5f*scale);
				}
			}

			//dragging
			if (!UI.current.layout) 
			{
				IInlet<object> inlet = null;

				if (DragDrop.TryDrag(outletCell, UI.current.mousePos))
				{
					Cell inletCell = null;

					foreach (Cell cell in UI.current.cellObjs.GetAllCells("Inlet"))
						if (cell.Contains(UI.current.mousePos, (int)(inletOutletDragArea/2 * scale))) //inlet cell size is 0, using 10 pixel padding
						{
							inlet = UI.current.cellObjs.GetObject<IInlet<object>>(cell, "Inlet");
							inletCell = cell;
						}

					color.a = 0.5f;

					if (inlet != null) 
					{
						if (!graph.CheckLinkValidity(outlet,inlet)) color = Color.red;
						DrawLink(StartCellLinkpos(outletCell), EndCellLinkpos(inletCell), color);
					}
					else DrawLink(StartCellLinkpos(outletCell), UI.current.mousePos, color);
				}

				if (DragDrop.TryRelease(outletCell, UI.current.mousePos))
				{
					GraphWindow.RecordCompleteUndo();

					if (inlet != null  &&  graph.CheckLinkValidity(outlet,inlet))
						graph.Link(inlet, outlet);

					GraphWindow.current?.RefreshMapMagic(outlet.Gen);
				}

				Rect outletRect = new Rect(
					outletCell.worldPosition.x - inletOutletDragArea/2*scale,
					outletCell.worldPosition.y - inletOutletDragArea/2*scale,
					outletCell.finalSize.x + inletOutletDragArea*scale,
					outletCell.finalSize.x + inletOutletDragArea*scale);
				DragDrop.TryStart(outletCell, UI.current.mousePos, outletRect);
			}
		}




		public static void DrawPortal (Generator gen, Graph graph, bool selected=false, bool isMini=false, string tooltip=null)
		{
			IPortalEnter<object> portalEnter = gen as IPortalEnter<object>;
			IPortalExit<object> portalExit = gen as IPortalExit<object>;
			IFnEnter<object> functionInput = gen as IFnEnter<object>;
			IFnExit<object> functionOutput = gen as IFnExit<object>;

			if (UI.current.layout)
				UI.current.cellObjs.ForceAdd(gen, Cell.current, "Generator");

			//background
			if (!UI.current.layout)
			{
				//shadow
				#if !MM_DOC
				GUIStyle shadowStyle = UI.current.textures.GetElementStyle(selected ? "MapMagic/Node/SelectionShadow" : "MapMagic/Node/Shadow", 
					borders:shadowBorders,
					overflow:shadowOverflow);
				Draw.Element(shadowStyle);
				#endif

				//frame
				GUIStyle frameStyle = UI.current.textures.GetElementStyle(selected ? "MapMagic/Node/SelectionFrame" : "MapMagic/Node/Frame", 
					borders:selected ? selectionBorders : frameBorders,
					overflow:selected ? selectionBorders : frameBorders);
				Draw.Element(frameStyle);

				//gray field color (background to all node, including the header)
				GUIStyle fieldBackStyle = UI.current.textures.GetElementStyle("MapMagic/Node/Background");
				Draw.Element(fieldBackStyle);
			}

			//header (and field)
			Color color = GeneratorDraw.GetGeneratorColor(gen);
			//Texture2D headerTex = UI.current.textures.GetColorizedTexture("MapMagic/Node/Header", color); 
			//GUIStyle headBackStyle = UI.current.textures.GetElementStyle(headerTex, borders:new RectOffset(40,40,3,3));
			//Draw.Element(headBackStyle);
			Texture2D headerTex = UI.current.textures.GetColorizedTexture("MapMagic/Node/Header", color); 
			Draw.Texture(headerTex);

			using (Cell.LinePx(portalHeight))
			{
				float inletScale = isMini ? miniInletsScale : 1;
				float nodeHeight = 24;	if (isMini) nodeHeight /= GraphWindow.miniZoom;
				float iconScale = 0.5f; if (isMini) iconScale /= GraphWindow.miniZoom;
				float iconSize = 20;	if (isMini) iconSize /= GraphWindow.miniZoom;
				float offsetSize = 8;	if (isMini) offsetSize /= GraphWindow.miniZoom;

				Cell.EmptyLineRel(1);
				using (Cell.LinePx(nodeHeight))
				{
						if (portalEnter != null)
						{
							//inlet
							using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(portalEnter, gen, scale:inletScale);

							//icon
							Cell.EmptyRowPx(offsetSize);
							Texture2D genIcon = UI.current.textures.GetTexture("GeneratorIcons/PortalIn");
							using (Cell.RowPx(iconSize)) Draw.Icon(genIcon, scale:iconScale);

							//label
							if (!isMini)
								using (Cell.Row) 
							{
								Color prevSelectionColor = UnityEngine.GUI.skin.settings.selectionColor;
								UnityEngine.GUI.skin.settings.selectionColor = new Color(0, 0.3555f, 0.78125f);
								portalEnter.Name = Draw.EditableLabel(portalEnter.Name, style:UI.current.styles.bigLabel);
								UnityEngine.GUI.skin.settings.selectionColor = prevSelectionColor;
							}

							Cell.EmptyRowPx(5);
						}

						else if (portalExit != null)
						{
							//icon
							Cell.EmptyRowPx(offsetSize/2);
							Texture2D genIcon = UI.current.textures.GetTexture("GeneratorIcons/PortalOut");
							using (Cell.RowPx(iconSize)) Draw.Icon(genIcon, scale:iconScale);
							//UI.Empty(Size.RowPixels(5));

							//label
							using (Cell.Row) //cell or empty cell if mini
								if (!isMini)
								 
							{
								string label = "(Empty)";
								if (portalExit.Enter != null)
									label = portalExit.Enter.Name;
								Draw.Label(label, style:UI.current.styles.bigLabel);
							}

							Texture2D chevronDown = UI.current.textures.GetTexture("DPUI/Chevrons/Down");
							using (Cell.RowPx(iconSize))
								if (Draw.Button(null, icon:chevronDown, iconScale:iconScale, visible:false))
									PortalSelectorPopup.DrawPortalSelector(graph, portalExit);

							Cell.EmptyRowPx(10);

							using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet((IOutlet<object>)gen, scale:inletScale);
						}

						else if (functionInput != null)
						{
							//icon
							Cell.EmptyRowPx(4);
							Texture2D genIcon = UI.current.textures.GetTexture("GeneratorIcons/FunctionIn");
							using (Cell.RowPx(iconSize)) Draw.Icon(genIcon, scale:iconScale);
							//UI.Empty(Size.RowPixels(5));

							//label
							using (Cell.Row) 
								if (!isMini)
							{
								Color prevSelectionColor = UnityEngine.GUI.skin.settings.selectionColor;
								UnityEngine.GUI.skin.settings.selectionColor = new Color(0, 0.3555f, 0.78125f);
								functionInput.Name = Draw.EditableLabel(functionInput.Name, style:UI.current.styles.bigLabel);
								UnityEngine.GUI.skin.settings.selectionColor = prevSelectionColor;
							}

							Cell.EmptyRowPx(10);

							using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet((IOutlet<object>)gen, scale:inletScale);
						}

						else if (functionOutput != null)
						{
							//inlet
							using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(functionOutput, gen, scale:inletScale);

							//icon
							Cell.EmptyRowPx(offsetSize);
							Texture2D genIcon = UI.current.textures.GetTexture("GeneratorIcons/FunctionOut");
							using (Cell.RowPx(iconSize)) Draw.Icon(genIcon, scale:iconScale);
							//UI.Empty(Size.RowPixels(5));

							//label
							using (Cell.Row)
								if (!isMini)
							{
								Color prevSelectionColor = UnityEngine.GUI.skin.settings.selectionColor;
								UnityEngine.GUI.skin.settings.selectionColor = new Color(0, 0.3555f, 0.78125f);
								functionOutput.Name = Draw.EditableLabel(functionOutput.Name, style:UI.current.styles.bigLabel);
								UnityEngine.GUI.skin.settings.selectionColor = prevSelectionColor;
							}

							Cell.EmptyRowPx(5);
						}
				}
				Cell.EmptyLineRel(1);
			}

			#if MM_DEBUG
			if (graph.debugGenInfo)
				DrawDebug(gen);
			#endif

			if (!UI.current.layout)
			{
				gen.guiSize.y = Cell.current.finalSize.y;
				gen.guiSize.x = nodeWidth;
			}
		}

		public static T DrawGlobalVar<T> (T val, string label)
		{
			using (Cell.RowRel(1-Cell.current.fieldWidth)) 
			{
				Cell.EmptyRowPx(3);
				using (Cell.RowPx(9)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Linked"));
				using (Cell.Row) Draw.Label(label);

				if (val != null  &&  val is float)
					val = (T)(object)Draw.DragValue((float)(object)val);
				if (val != null  &&  val is int)
					val = (T)(object)Draw.DragValue((int)(object)val);
			}
			
			using (Cell.RowRel(Cell.current.fieldWidth))
				val = (T)Draw.UniversalField(val, typeof(T)); 

			return val;
		}
		public static void DrawGlobalVar<T> (ref T val, string label) => val = DrawGlobalVar<T>(val, label);


		public static void SubGraph (IBiome biome, ref Graph subGraph, bool refreshOnGraphChange=true)
		{
			using (Cell.Row)
			{
				Graph prevSubGraph = subGraph;
				Draw.ObjectField(ref subGraph); 

				if (Cell.current.valChanged)
				{
					if (SubGraphDependenceError(biome, subGraph))
					{
						Debug.Log($"Graph {subGraph} contains this biome (directly or in sub-graphs). Could not be assigned since it will create dependency loop.");
						subGraph = prevSubGraph;
						return;
					}

					//if (biome is Biomes.BaseFunctionGenerator fn)
					//	if (biome.SubGraph != null)
					//		fn.ovd = new Expose.Override(subGraph.defaults);
					//requires an access to module. Moved to fn editor

					if (refreshOnGraphChange)
						GraphWindow.current?.RefreshMapMagic();	
				}
			}

			Texture2D openIcon = UI.current.textures.GetTexture("DPUI/Icons/FolderOpen");
				using (Cell.RowPx(22))
					if (Draw.Button(icon:openIcon, iconScale:0.5f, visible:false) && subGraph!=null)
					{
						Graph subGraphClosure = subGraph;
						UI.current.DrawAfter( ()=> GraphWindow.current.OpenBiome(subGraphClosure) );
					}
		}

		private static bool SubGraphDependenceError (IBiome biome, Graph subGraph)
		/// True if subGraph contains biome (even in sub-sub grpahs)
		{
			if (subGraph == null)
				return false;

			Generator gen = ((IUnit)biome).Gen;

			if (subGraph.ContainsGenerator(gen))
				return true;

			foreach (Graph subSubGraph in subGraph.SubGraphs(recursively:true))
				if (subSubGraph.ContainsGenerator(gen))
					return true;

			return false;
		}


		public static void DrawMini (Generator gen, Graph graph, bool selected=false, GeneratorMenuAttribute menuAtt=null)
		{
			if (UI.current.layout)
				UI.current.cellObjs.ForceAdd(gen, Cell.current, "Generator");

			if (menuAtt == null)
			{
				Type genType = gen.GetType();
				menuAtt = GetMenuAttribute(genType);
			}

			//background
			if (!UI.current.layout)
			{
				//shadow
				GUIStyle shadowStyle = UI.current.textures.GetElementStyle(selected ? "MapMagic/Node/SelectionShadow" : "MapMagic/Node/Shadow", 
					borders:shadowBorders,
					overflow:shadowOverflow);
				#if !MM_DEBUG
					Draw.Element(shadowStyle);
				#else
					if (graph.debugGraphBackground)
						Draw.Element(shadowStyle);
				#endif

				//frame
				GUIStyle frameStyle = UI.current.textures.GetElementStyle(selected ? "MapMagic/Node/SelectionFrame" : "MapMagic/Node/Frame", 
					borders:selected ? selectionBorders : frameBorders,
					overflow:selected ? selectionBorders : frameBorders);
				Draw.Element(frameStyle);

				//gray field color (borders in this case)
				GUIStyle fieldBackStyle = UI.current.textures.GetElementStyle("MapMagic/Node/Background");
				Draw.Element(fieldBackStyle);

				//header color
				Color color = menuAtt.color;
				if (!gen.enabled) color = DisableGeneratorColor(color);
				Texture2D headerTex = UI.current.textures.GetColorizedTexture("MapMagic/Node/Header", color); 
				//GUIStyle headBackStyle = UI.current.textures.GetElementStyle(headerTex);//, borders:new RectOffset(40,40,3,0));
				Draw.Texture(headerTex);
			}

			Cell.EmptyLinePx(4/GraphWindow.miniZoom);

			//icon/single inlets/single outlets
			using (Cell.LinePx(0))
			{
				if (gen is IInlet<object> inletGen)
				{
					using (Cell.RowPx(0)) 
						DrawInlet(inletGen, gen, miniInletsScale);
				}

				using (Cell.Row)
					using (Cell.LinePx(16f/GraphWindow.miniZoom)) Draw.Icon(menuAtt.icon, scale:0.5f/GraphWindow.miniZoom);

				if (gen is IOutlet<object> outletGen)
				{
					using (Cell.RowPx(0)) 
						DrawOutlet(outletGen, miniInletsScale);
				}
			}

			//name
			using (Cell.LinePx(16/GraphWindow.miniZoom))
			{
				if (miniNameStyle == null)
				{
					miniNameStyle = new GUIStyle(UI.current.styles.bigLabel);
					miniNameStyle.fontSize = (int)(miniNameStyle.fontSize * 0.6f / GraphWindow.miniZoom);
					miniNameStyle.alignment = TextAnchor.LowerCenter;
				}

				Draw.Label(menuAtt.nameUpper, style:miniNameStyle);
			}

			//layers
			if (miniInletStyle == null)
			{
				miniInletStyle = new GUIStyle(UI.current.styles.label);
				miniInletStyle.fontSize = (int)(miniInletStyle.fontSize*0.8f / GraphWindow.miniZoom);
			}

			if (gen is IMultiLayer mlayGen)
			{
				IList<IUnit> layers = mlayGen.Layers;
				int layersCount = layers.Count;

				//draing inlets that are not layers for Whittaker
				if (gen is IMultiInlet multiInGen)
					foreach (IInlet<object> inlet in multiInGen.Inlets())
						if (!(inlet is IOutlet<object>))
							using (Cell.LinePx(miniInletLineHeight))
								using (Cell.RowPx(0)) 
									using (Cell.LinePx(miniInletLineHeight)) DrawInlet(inlet, gen, miniInletsScale); 

				//drawing layers
				for (int i=0; i<layersCount; i++)
				{
					int ii = !mlayGen.Inversed ?  i  :  layersCount-1 - i;
					IUnit layer = layers[ii];

					if (layer is IInlet<object>  ||  layer is IOutlet<object>)
						using (Cell.LinePx(miniInletLineHeight))
						{
							if (layer is IInlet<object> inlet)
								if (!(ii==0 && mlayGen.HideFirst))
									using (Cell.RowPx(0)) DrawInlet(inlet, gen, miniInletsScale);
								
							Cell.EmptyRowPx(14);

							string name = null;
							if (layer is IBiome biome  &&  biome.SubGraph != null) name = biome.SubGraph.name;
							if (layer is MatrixGenerators.BaseTextureLayer texLayer) name = texLayer.name;

							using (Cell.Row)
							{
								if (name != null)
									Draw.Label(name, miniInletStyle);
							}

							Cell.EmptyRowPx(14);

							if (layer is IOutlet<object> outlet)
								using (Cell.RowPx(0)) DrawOutlet(outlet, miniInletsScale); 
						}
				}
			}

			//multi inlets/outlets
			else if (gen is IMultiInlet  ||  gen is IMultiOutlet)
				using (Cell.LinePx(0))
				{
					//inlets
					using (Cell.RowPx(0))
						if (gen is IMultiInlet multiInGen)
						{
							foreach (IInlet<object> inlet in multiInGen.Inlets())
								using (Cell.LinePx(miniInletLineHeight)) DrawInlet(inlet, gen, miniInletsScale); 
						}

					Cell.EmptyRowPx(14);

					//names
					using (Cell.Row)
						if (gen is IMultiInlet multiInGen)
						{
							ValAttribute[] inletVals = GetInletVals(menuAtt.type);

							foreach (ValAttribute val in inletVals)
								using (Cell.LinePx(miniInletLineHeight)) Draw.Label(val.name, miniInletStyle); 
						}

					Cell.EmptyRowPx(16);

					//outlets
					using (Cell.RowPx(0))
						if (gen is IMultiOutlet multiOutGen)
						{
							foreach (IOutlet<object> outlet in multiOutGen.Outlets())
								using (Cell.LinePx(miniInletLineHeight)) DrawOutlet(outlet, miniInletsScale); 
						}
				}

			//custom header editor
			using (Cell.LinePx(0))
				Draw.Editor(gen, cat:"Header"); 
		}


		

		#region Links

			public static void DrawLink (Vector2 startPos, Vector2 endPos, Color color, bool dotted=false, int density=10, float width=4f) 
			{
				if (UI.current.layout) return;
				//if (UI.current.optimizeElements && 
				//	(end.x<Cell.current.worldPosition.x || end.y<Cell.current.worldPosition.y))// ||
					//start.x>Cell.current.worldPosition.x+Cell.current.finalSize.x || start.y>Cell.current.worldPosition.y+Cell.current.finalSize.y)) 
				//		return;



				float distance = (endPos-startPos).magnitude;

				Vector2 startTan = new Vector2(startPos.x + distance/4, startPos.y);
				Vector2 endTan = new Vector2(endPos.x-distance/4, endPos.y);



				//Mathf.Min(startPos, endPos, startTangent, endTangent)
				float minX = startPos.x<endPos.x ? startPos.x : endPos.x;  minX=minX<startTan.x ? minX : startTan.x;  minX=minX<endTan.x ? minX : endTan.x;  
				float maxX = startPos.x>endPos.x ? startPos.x : endPos.x;  maxX=maxX>startTan.x ? maxX : startTan.x;  maxX=maxX>endTan.x ? maxX : endTan.x;
				float minY = startPos.y<endPos.y ? startPos.y : endPos.y;  minY=minY<startTan.y ? minY : startTan.y;  minY=minY<endTan.y ? minY : endTan.y;  
				float maxY = startPos.y>endPos.y ? startPos.y : endPos.y;  maxY=maxY>startTan.y ? maxY : startTan.y;  maxY=maxY>endTan.y ? maxY : endTan.y;

				if (UI.current.optimizeElements && !UI.current.IsInWindow(minX, maxX, minY, maxY)) return;

				startPos = UI.current.scrollZoom.ToScreen(startPos);
				endPos = UI.current.scrollZoom.ToScreen(endPos);
				startTan = UI.current.scrollZoom.ToScreen(startTan);
				endTan = UI.current.scrollZoom.ToScreen(endTan);

				Texture2D splineTex = UI.current.textures.GetTexture("DPUI/SplineTex");

				float curWidth = width*UI.current.scrollZoom.zoom+2f;

				//20 skin
				if (!dotted)
					#if !MM_DOC
					UnityEditor.Handles.DrawBezier(startPos, endPos, startTan, endTan, color, splineTex, curWidth);
					#else
					UnityEditor.Handles.DrawBezier(startPos, endPos, startTan, endTan, color, UI.current.textures.GetTexture("DPUI/White"), width/1.5f);
					#endif

				//manual spline
				else
				{
					Vector3[] splinePoints = new Vector3[2];
				
					int steps = (int)(distance / 10);

					for (int i=0; i<steps; i+=2)
					{
						float p = 1f*i/steps;
						float ip = 1f-p;
						splinePoints[0] = ip*ip*ip*startPos + 3*p*ip*ip*startTan + 3*p*p*ip*endTan + p*p*p*endPos;

						p = 1f*(i+1)/steps;
						ip = 1f-p;
						splinePoints[1] = ip*ip*ip*startPos + 3*p*ip*ip*startTan + 3*p*p*ip*endTan + p*p*p*endPos;

						UnityEditor.Handles.color = color;
						UnityEditor.Handles.DrawAAPolyLine(splineTex, curWidth, splinePoints);
					}
				}
			}

			public static Vector2 StartCellLinkpos (Cell cell) { return cell.InternalCenter+new Vector2(5,0); }
			public static Vector2 EndCellLinkpos (Cell cell) { return cell.InternalCenter+new Vector2(-5,0); }

			public static float DistToLink (Vector2 pos, IOutlet<object> outlet, IInlet<object> inlet)
			{
				Cell outletCell = UI.current.cellObjs.GetCell(outlet, "Outlet");
				Cell inletCell = UI.current.cellObjs.GetCell(inlet, "Inlet");
				if (outletCell==null || inletCell==null) 
					return float.MaxValue;

				Vector2 linkStart = StartCellLinkpos(outletCell);
				Vector2 linkEnd = EndCellLinkpos(inletCell);

				return DistToLink(pos, linkStart, linkEnd);
			}


			public static float DistToLink (Vector2 pos, Vector2 startPos, Vector2 endPos)
			{
				float distance = (endPos-startPos).magnitude;

				Vector2 startTan = new Vector2(startPos.x + distance/4, startPos.y);
				Vector2 endTan = new Vector2(endPos.x-distance/4, endPos.y);

				return UnityEditor.HandleUtility.DistancePointBezier(pos, startPos, endPos, startTan, endTan);
			}

			/*public static void MinDistToLink (Vector2 pos, out float minDist, out IInlet<object> minInlet)
			{
				minDist = int.MaxValue;
				minInlet = null;
				foreach (var kvp in linkLinesLut.d1)
				{
					(Vector2,Vector2) link = kvp.Key;
					float dist = DistToLink(pos, link.Item1, link.Item2);
					if (dist < minDist) 
					{
						minDist = dist;
						minInlet = kvp.Value;
					}
				}
			}*/




		#endregion


		#region Helpers

			private static readonly Dictionary<Type,GeneratorMenuAttribute> cachedAttributes = new Dictionary<Type, GeneratorMenuAttribute>();

			public static GeneratorMenuAttribute GetMenuAttribute (Type genType)
			{
				if (cachedAttributes.TryGetValue(genType, out GeneratorMenuAttribute att)) return att;

				Attribute[] atts = Attribute.GetCustomAttributes(genType); 
			
				GeneratorMenuAttribute menuAtt = null;
				for (int i=0; i<atts.Length; i++)
					if (atts[i] is GeneratorMenuAttribute) menuAtt = (GeneratorMenuAttribute)atts[i];

				if (menuAtt != null)
				{
					menuAtt.nameUpper = menuAtt.name.ToUpper();
					menuAtt.nameWidth = UI.current.styles.bigLabel.CalcSize( new GUIContent(menuAtt.nameUpper) ).x;
					menuAtt.icon = UI.current.textures.GetTexture(menuAtt.iconName ?? "GeneratorIcons/Generator");
					menuAtt.type = genType;
					menuAtt.color = menuAtt.colorType != null ?
						GetGeneratorColor( menuAtt.colorType) :
						GetGeneratorColor( Generator.GetGenericType(genType) );
				}
				//else
				//	throw new Exception("Could not find attribute for " + genType.ToString());
				// there are still some generators without attribute. Like "MyGenerator"

				cachedAttributes.Add(genType, menuAtt);
				return menuAtt;
			}


			private static readonly Dictionary<Type,ValAttribute[]> cachedInletVals = new Dictionary<Type, ValAttribute[]>();

			public static ValAttribute[] GetInletVals (Type genType)
			{
				if (cachedInletVals.TryGetValue(genType, out ValAttribute[] inletValsArr)) return inletValsArr;

				else
				{
					List<ValAttribute> inletVals = new List<ValAttribute>();

					ValAttribute[] allAttributes = ValAttribute.GetAttributes(genType);
					for (int v=0; v<allAttributes.Length; v++)
						if (allAttributes[v].type != null  &&  (typeof(IInlet<object>)).IsAssignableFrom(allAttributes[v].type)) //if subclass of Inlet
							inletVals.Add(allAttributes[v]);

					inletValsArr = inletVals.ToArray();
					cachedInletVals.Add(genType, inletValsArr);
					return inletValsArr;
				}
			}


			private static readonly Dictionary<Type,ValAttribute[]> cachedOutletVals = new Dictionary<Type, ValAttribute[]>();

			public static ValAttribute[] GetOutletVals (Type genType)
			{
				if (cachedOutletVals.TryGetValue(genType, out ValAttribute[] outletValsArr)) return outletValsArr;

				else
				{
					List<ValAttribute> outletVals = new List<ValAttribute>();

					ValAttribute[] allAttributes = ValAttribute.GetAttributes(genType);
					for (int v=0; v<allAttributes.Length; v++)
						if (allAttributes[v].type != null  &&  (typeof(IOutlet<object>)).IsAssignableFrom(allAttributes[v].type)) //if subclass of Inlet
							outletVals.Add(allAttributes[v]);

					outletValsArr = outletVals.ToArray();
					cachedOutletVals.Add(genType, outletValsArr);
					return outletValsArr;
				}
			}


			public static Color GetGeneratorColor (Generator gen) => GetGeneratorColor(Generator.GetGenericType(gen));
			public static Color GetGeneratorColor (Type genericType)
			{
				if (StylesCache.isPro) return GetGeneratorColorPro(genericType);

				Color color;

				if (genericType == typeof(MatrixWorld)) color = new Color(0.4f, 0.666f, 1f, 1f);
				else if (genericType == typeof(TransitionsList)) color = new Color(0.444f, 0.871f, 0.382f, 1f);
				else if (genericType == typeof(SplineSys)) color = new Color(1f, 0.6f, 0f, 1f);
				else if (genericType == typeof(Den.Tools.Splines.SplineSys)) color = new Color(1f, 0.6f, 0f, 1f);
				else if (genericType == typeof(IBiome)) color = new Color(0.45f, 0.55f, 0.56f, 1f);
				else if (genericType == typeof(MatrixSet)) color = new Color(0.3f, 0.444f, 0.75f, 1f);
				else color = new Color(0.65f, 0.65f, 0.65f, 1);

				return color;
			}

			private static Color GetGeneratorColorPro (Type genericType)
			{
				Color color;

				if (genericType == typeof(MatrixWorld)) color = new Color(0, 0.325f, 0.75f, 0.7f);
				else if (genericType == typeof(TransitionsList)) color = new Color(0.13f, 0.65f, 0, 0.65f);
				else if (genericType == typeof(SplineSys)) color = new Color(0.65f, 0.325f, 0f, 0.7f);
				else if (genericType == typeof(Den.Tools.Splines.SplineSys)) color = new Color(0.65f, 0.325f, 0f, 0.7f);
				else if (genericType == typeof(IBiome)) color = new Color(0.45f, 0.55f, 0.56f, 1f);
				else if (genericType == typeof(MatrixSet)) color = new Color(0, 0.2f, 0.5f, 0.7f);
				else color = new Color(0.65f, 0.65f, 0.65f, 1);

				return color;
			}

			private static Color DisableGeneratorColor (Color color)
			//returns the disabled color from original generator's color
			{
				return Color.Lerp(color, new Color(0.8f, 0.8f, 0.8f, 1), 0.75f);
			}

			public static Color GetLinkColor (IInlet<object> inlet) => GetLinkColor(Generator.GetGenericType(inlet));
			public static Color GetLinkColor (IOutlet<object> outlet) => GetLinkColor(Generator.GetGenericType(outlet));
			public static Color GetLinkColor (Type genericType)
			{
				bool isPro = StylesCache.isPro;

				if (genericType == typeof(MatrixWorld))
				{
					if (isPro) return new Color(0, 0.5f, 1f, 1f);
					else return new Color(0, 0.333f, 0.666f, 1f);
				}

				if (genericType == typeof(TransitionsList))
				{
					if (isPro) return new Color(0.2f, 1f, 0, 1f);
					else return new Color(0, 0.465f, 0, 1f);
				}

				if (genericType == typeof(SplineSys))
				{
					if (isPro) return new Color(1f, 0.5f, 0f, 1f);
					else return new Color(0.666f, 0.333f, 0, 1f);
				}

				if (genericType == typeof(Den.Tools.Splines.SplineSys))
				{
					if (isPro) return new Color(1f, 0.5f, 0f, 1f);
					else return new Color(0.666f, 0.333f, 0, 1f);
				}

				if (genericType == typeof(MatrixSet))
				{
					if (isPro) return new Color(0, 0.25f, 0.5f, 1f);
					else return new Color(0, 0.16f, 0.333f, 1f);
				}

				return Color.gray;
			}

		#endregion


		#region AutoPositioning

			public static bool FindPlace (ref Vector2 original, Vector2 range, Graph graph, int step=10, int margins=5)
			/// Tries to find the position that is not intersecting with other nodes. Return true if found (and changes ref)
			{
				//creating a list of generators within range
				List<Rect> gensInRange = new List<Rect>();
				Rect rangeRect = new Rect(
					original.x-range.x-nodeWidth/2-margins, 
					original.y-range.y-nodeWidth/4-margins, 
					range.x*2+nodeWidth+margins*2, 
					range.y*2+nodeWidth/2+margins*2);
				for (int g=0; g<graph.generators.Length; g++) 
				{
					if (!rangeRect.Intersects(graph.generators[g].guiPosition, graph.generators[g].guiSize)) continue;
					gensInRange.Add(new Rect(graph.generators[g].guiPosition, graph.generators[g].guiSize));
				}
				int gensInRangeCount = gensInRange.Count;

				Vector2 defSize = new Vector2(nodeWidth + margins*2, nodeWidth/2 + margins*2);

				//finding number of steps
				int stepsX = (int)(range.x/step) + 1;
				int stepsY = (int)(range.y/step) + 1;
				int stepsMax = Mathf.Max(stepsX, stepsY);

				//find the closest possible place (starting from center)
				Coord center = new Coord(0,0);
				foreach (Coord coord in center.DistanceArea(stepsMax))
				{
					int x = coord.x;
					int y = coord.z;

					if (x>stepsX || x<-stepsX || y>stepsY || y<-stepsY) continue;

					Vector2 newPos = new Vector2(original.x + x*step - margins, original.y + y*step - margins);
					//Vector2 pos = new Vector2(original.x + x*step, original.y + y*step);

					bool intersects = false;
					for (int g=0; g<gensInRangeCount; g++)
					{
						if (gensInRange[g].Intersects(newPos, defSize))
							{ intersects = true; break; }
					}

					if (!intersects)
					{
						//using (Cell.Custom( new Rect(pos, defSize)))
						//	Draw.Rect(Color.red);

						original = newPos + new Vector2(margins,margins);
						return true;
					}
				}

				return false;
			}


			public static void RelaxIteration (Generator gen, List<Generator> nearGens, List<IOutlet<object>> inletGens, List<Generator> outletGens)
			{
				
			}


			internal static void TestRelax (Graph graph)
			{
				Generator gen = null;
				for (int g=0; g<graph.generators.Length; g++)
					if ( new Rect(graph.generators[g].guiPosition, graph.generators[g].guiSize).Contains(UI.current.mousePos) )
						{ gen = graph.generators[g]; break; }

				if (gen != null)
				{
					using (Cell.Custom(  new Rect(gen.guiPosition, gen.guiSize) ))
						Draw.Rect(Color.red);
				}
			}


		#endregion
	}
}