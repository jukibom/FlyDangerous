using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Products;
using MapMagic.Previews; 

namespace MapMagic.Nodes.GUI
{
	//[EditoWindowTitle(title = "MapMagic Graph")]  //it's internal Unity stuff
	public class GraphWindow : EditorWindow
	{
		public static GraphWindow current;  //assigned each gui draw (and nulled after)

		//public List<Graph> graphs = new List<Graph>();
		//public Graph CurrentGraph { get{ if (graphs.Count==0) return null; else return graphs[graphs.Count-1]; }}
		//public Graph RootGraph { get{ if (graphs.Count==0) return null; else return graphs[0]; }}

		public Graph graph;
		public List<Graph> parentGraphs;   //the ones on pressing "up level" button
											//we can have the same function in two biomes. Where should we exit on pressing "up level"?
											//automatically created on opening window, though

		private bool drawAddRemoveButton = true;  //turning off Add/Remove on opening popup with it, and re-enabling once the graph window is focused again

		public static Dictionary<Graph,Vector3> graphsScrollZooms = new Dictionary<Graph, Vector3>();
		//to remember the stored graphs scroll/zoom to switch between graphs
		//public for snapshots

		public IMapMagic mapMagic;

		private static UnityEngine.SceneManagement.Scene prevSceneLoaded; //to search for mapmagic only when scene, selection or root objects count changes
		private static int prevRootObjsCount;
		private static UnityEngine.Object prevObjSelected;

		public bool MapMagicRelevant =>
			mapMagic != null  &&  
			mapMagic.Graph != null  &&
			(mapMagic.Graph == current.graph  ||  mapMagic.Graph.ContainsSubGraph(current.graph, recursively:true));

		public void UpdateRelatedMapMagic () => mapMagic = FindRelatedMapMagic(graph); //mostly for public calls, GraphWindow itself can use mm=FindRelated frewfrw
		   
		public static IMapMagic FindRelatedMapMagic (Graph graph)
		{
			if (Selection.activeObject is IMapMagic imm)
				if (imm.ContainsGraph(graph)) return imm;
			//doesn't work with MM object, but leaving here just in case

			//looking in selection
			if (Selection.activeObject is GameObject selectedGameObj)
			{
				MapMagicObject mmo = selectedGameObj.GetComponent<MapMagicObject>();
				if (mmo != null && mmo.ContainsGraph(graph)) return mmo;
				//we can't assign ClusterAsset same way! Add code for it
			}

			//looking in all objects
			UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
			if (scene != prevSceneLoaded || scene.rootCount != prevRootObjsCount || Selection.activeGameObject != prevObjSelected)
			{
				MapMagicObject[] allMM = GameObject.FindObjectsOfType<MapMagicObject>();
				for (int m=0; m<allMM.Length; m++)
					if (allMM[m].ContainsGraph(graph)) return allMM[m];

				prevSceneLoaded = scene;
				prevRootObjsCount = scene.rootCount;
				prevObjSelected = Selection.activeGameObject;
			}

			return null;
		}

		public static IMapMagic RelatedMapMagic 
		{get{
			if (current == null  ||  current.mapMagic == null  ||  current.mapMagic.Graph == null) return null;
			if (current.mapMagic.Graph != current.graph  &&  !current.mapMagic.Graph.ContainsSubGraph(current.graph, recursively:true)) return null;
			return current.mapMagic;
		}}

		public void RefreshMapMagic () => RefreshMapMagic(true, null, null); 
		public void RefreshMapMagic (Generator gen) => RefreshMapMagic(false, gen, null);
		public void RefreshMapMagic (Generator gen1, Generator gen2) => RefreshMapMagic(false, gen1, gen2);
		private void RefreshMapMagic (bool all, Generator gen1, Generator gen2)
		/// makes current mapMagic to generate
		/// if gen not specified forcing re-generate
		{
			graph.changeVersion++;
			
			if (MapMagicRelevant)  //TODO: test without relevancy check
			{
				if (all) RelatedMapMagic?.Refresh();
				else RelatedMapMagic?.Refresh(gen1, gen2);
			}

			EditorUtility.SetDirty(current.graph);

			OnGraphChanged?.Invoke(current.graph);
		}

		public static Action<Graph> OnGraphChanged;


		public static void RecordCompleteUndo ()
		{
			current.graphUI.undo.Record(completeUndo:true);
		}
		//the usual undo is recorded on valChange via gui

		const int toolbarSize = 20;

		public UI graphUI = UI.ScrolledUI(maxZoom:1, minZoom:0.375f);  //public for snapshot
		UI toolbarUI = new UI();
		UI dragUI = new UI();
		UI miniSelectedUI = new UI();

		public bool IsMini => graphUI.scrollZoom.zoom < 0.4f; //minimum full-version zoom is 0.4375. Next step (0.375) switches to mini
		public const float miniZoom = 0.375f;

		bool wasGenerating = false; //to update final frame when generate is finished
		
		private static Vector2 addDragTo = new Vector2(Screen.width-50,20);
		private static Vector2 AddDragDefault {get{ return new Vector2(Screen.width-50,20); }}
		private const int addDragSize = 34;
		private const int addDragOffset = 20; //the offset from screen corner
		private static readonly object addDragId = new object();

		private Vector2 addButtonDragOffset;

		public HashSet<Generator> selected = new HashSet<Generator>();

		private long lastFrameTime;


		public void OnEnable () 
		{
			//redrawing previews
			//Preview.OnRefreshed += p => Repaint();

			#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= OnSceneGUI;
			SceneView.duringSceneGui += OnSceneGUI;
			#else
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			#endif

			ScrollZoomOnOpen(); //focusing after script re-compile

			selected.Clear(); //removing selection from previous graph
		}

		public void OnDisable () 
		{
			#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= OnSceneGUI;
			#else
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			#endif

			 UnityEditor.Tools.hidden = false; //in case gizmo node is turned on
		}

		public void OnInspectorUpdate () 
		{
			current = this;

			//updating gauge
			if (mapMagic == null) return;
			bool isGenerating = mapMagic.IsGenerating()  &&  mapMagic.ContainsGraph(graph);
			if (wasGenerating) { Repaint(); wasGenerating=false; } //1 frame delay after generate is finished
			if (isGenerating) { Repaint(); wasGenerating=true; }
		}


		private void OnGUI()
		{
			current = this;

			mapMagic = FindRelatedMapMagic(graph);

			if (graph==null || graph.generators==null) return;

			if (mapMagic==null) Den.Tools.Tasks.CoroutineManager.Update(); //updating coroutine if no mm assigned (to display brush previews)

			//fps timer
			#if MM_DEBUG
			long frameStart = System.Diagnostics.Stopwatch.GetTimestamp();
			#endif

			//undo
			if (graphUI.undo == null) 
			{
				graphUI.undo = new Den.Tools.GUI.Undo() { undoObject = graph , undoName = "MapMagic Graph Change" };
				graphUI.undo.undoAction = GraphWindow.current.RefreshMapMagic;
			}
			graphUI.undo.undoObject = graph;


			//mini with selection
			if (selected.Count == 1  &&  IsMini)
			{
				Generator selectedGen = selected.Any();

				Rect selectedRect = new Rect(PlaceByAnchor(graph.guiMiniAnchor,graph.guiMiniPos,selectedGen.guiSize), selectedGen.guiSize);
				selectedRect.position = selectedRect.position+new Vector2(0,toolbarSize);

				//skipping drawing graph if clicked somewhere in generator
				bool clickedToGen = Event.current.isMouse  &&  selectedRect.Contains(Event.current.mousePosition);
				
				using (new UnityEngine.GUI.ClipScope(new Rect(0, toolbarSize, Screen.width, Screen.height-toolbarSize)))
				{
					if (clickedToGen) EditorGUI.BeginDisabledGroup(true); //disabling other controls when clicking selected mini gen
					graphUI.Draw(DrawGraph, inInspector:false);
					if (clickedToGen) EditorGUI.EndDisabledGroup();

					miniSelectedUI.Draw(DrawMiniSelected, inInspector:false);
				}

				//using (new UnityEngine.GUI.ClipScope(new Rect(20, 20+toolbarSize, selectedGen.guiSize.x, selectedGen.guiSize.y)))
				//	miniSelectedUI.Draw(DrawMiniSelected, inInspector:false);
			}

			//standard graph/mini
			else
				using (new UnityEngine.GUI.ClipScope(new Rect(0, toolbarSize, Screen.width, Screen.height-toolbarSize)))
					graphUI.Draw(DrawGraph, inInspector:false, customRect:new Rect(0, toolbarSize, Screen.width, Screen.height-toolbarSize));

			//toolbar
			using (new UnityEngine.GUI.ClipScope(new Rect(0,0, Screen.width, toolbarSize)))
				toolbarUI.Draw(DrawToolbar, inInspector:false);

			//storing graph pivot to focus it on load
			Vector3 scrollZoom = graphUI.scrollZoom.GetWindowCenter(position.size);
			scrollZoom.z = graphUI.scrollZoom.zoom;
			if (graphsScrollZooms.ContainsKey(graph)) graphsScrollZooms[graph] = scrollZoom;
			else graphsScrollZooms.Add(graph, scrollZoom);

			//preventing switching to main while dragging field (for MMobject only)
			if (mapMagic != null  &&  mapMagic is MapMagicObject mapMagicObject)
			{
				bool newForceDrafts = DragDrop.obj!=null && (DragDrop.group=="DragField" || DragDrop.group=="DragCurve" || DragDrop.group=="DragLevels"); 
				if (!newForceDrafts  &&  mapMagicObject.guiDraggingField)
				{
					mapMagicObject.guiDraggingField = newForceDrafts;
					mapMagicObject.SwitchLods();
				}
				mapMagicObject.guiDraggingField = newForceDrafts;
			}

			//showing fps
			#if MM_DEBUG
			if (graph.debugGraphFps)
			{
				long frameEnd = System.Diagnostics.Stopwatch.GetTimestamp();
				float timeDelta = 1f * (frameEnd-frameStart) / System.Diagnostics.Stopwatch.Frequency;
				float fps = 1f / timeDelta;
				EditorGUI.LabelField(new Rect(10, toolbarSize+10, 70, 18), "FPS:" + fps.ToString("0.0"));
			}
			#endif

			//moving scene view
			#if MM_DEBUG
			if (graph.drawInSceneView)
				MoveSceneView();
			#endif
		}


		private void DrawGraph ()
		{
			bool isMini = IsMini;

			//background
			float gridColor = !StylesCache.isPro ? 0.45f : 0.12f;
			float gridBackgroundColor = !StylesCache.isPro ? 0.5f : 0.15f;

			#if MM_DEBUG
				if (!graph.debugGraphBackground)
				{
					gridColor = graph.debugGraphBackColor;
					gridBackgroundColor = graph.debugGraphBackColor;
				}
			#endif

			Draw.StaticGrid(
				displayRect: new Rect(0, 0, Screen.width, Screen.height-toolbarSize),
				cellSize:32,
				color:new Color(gridColor,gridColor,gridColor), 
				background:new Color(gridBackgroundColor,gridBackgroundColor,gridBackgroundColor),
				fadeWithZoom:true);

			#if MM_DEBUG
				if (graph.drawInSceneView)
				{
					using (Cell.Full)
						DrawSceneView();
				}
			#endif

			//drawing groups
			foreach (Group group in graph.groups)
				using (Cell.Custom(group.guiPos.x, group.guiPos.y, group.guiSize.x, group.guiSize.y)) 
				{
					GroupDraw.DragGroup(group, graph.generators);
					GroupDraw.DrawGroup(group, isMini:isMini);
				}


			//dragging nodes
			foreach (Generator gen in graph.generators)
				GeneratorDraw.DragGenerator(gen, selected);


			//drawing links
			//using (Timer.Start("Links"))
			if (!UI.current.layout)
			{
				List<(IInlet<object> inlet, IOutlet<object> outlet)> linksToRemove = null;
				foreach (var kvp in graph.links)
				{
					IInlet<object> inlet = kvp.Key;
					IOutlet<object> outlet = kvp.Value;

					Cell outletCell = UI.current.cellObjs.GetCell(outlet, "Outlet");
					Cell inletCell = UI.current.cellObjs.GetCell(inlet, "Inlet");

					if (outletCell == null || inletCell == null)
					{
						Debug.LogError("Could not find a cell for inlet/outlet. Removing link");
						if (linksToRemove == null) linksToRemove = new List<(IInlet<object> inlet, IOutlet<object> outlet)>();
						linksToRemove.Add((inlet,outlet));
						continue;
					}

					GeneratorDraw.DrawLink(
						GeneratorDraw.StartCellLinkpos(outletCell),
						GeneratorDraw.EndCellLinkpos(inletCell), 
						GeneratorDraw.GetLinkColor(inlet),
						width:!isMini ? 4f : 6f );
				}

				if (linksToRemove != null)
					foreach ((IInlet<object> inlet, IOutlet<object> outlet) in linksToRemove)
					{
						graph.UnlinkInlet(inlet);
						graph.UnlinkOutlet(outlet);
					}
			}

			//removing null generators (for test purpose)
			for (int n=graph.generators.Length-1; n>=0; n--)
			{
				if (graph.generators[n] == null)
					ArrayTools.RemoveAt(ref graph.generators, n);
			}

			//drawing generators
			//using (Timer.Start("Generators"))
			float nodeWidth = !isMini ? GeneratorDraw.nodeWidth : GeneratorDraw.miniWidth;
			foreach (Generator gen in graph.generators)
				using (Cell.Custom(gen.guiPosition.x, gen.guiPosition.y, nodeWidth, 0))
					GeneratorDraw.DrawGeneratorOrPortal(gen, graph, isMini:isMini, selected.Contains(gen));


			//de-selecting nodes (after dragging and drawing since using drag obj)
			if (!UI.current.layout)
			{
				GeneratorDraw.SelectGenerators(selected, shiftForSingleSelect:!isMini);
				GeneratorDraw.DeselectGenerators(selected); //and deselected always without shift
			}
				
			//add/remove button
			//using (Timer.Start("AddRemove"))
			using (Cell.Full)
				DragDrawAddRemove();

			//right click menu (should have access to cellObjs)
			if (!UI.current.layout  &&  Event.current.type == EventType.MouseDown  &&  Event.current.button == 1)
				RightClick.DrawRightClickItems(graphUI, graphUI.mousePos, graph);

			//create menu on space
			if (!UI.current.layout  &&  Event.current.type == EventType.KeyDown  &&  Event.current.keyCode == KeyCode.Space  && !Event.current.shift)
				CreateRightClick.DrawCreateItems(graphUI.mousePos, graph);

			//delete selected generators
			if (selected!=null  &&  selected.Count!=0  &&  Event.current.type==EventType.KeyDown  &&  Event.current.keyCode==KeyCode.Delete)
				GraphEditorActions.RemoveGenerators(graph, selected);
		}

		private void DrawMiniSelected () 
		{
			if (selected.Count != 1)
				return;

			Generator gen = selected.Any();

			Vector2 cellSize = new Vector2(gen.guiSize.x, 0);
			Vector2 cellPos = PlaceByAnchor(graph.guiMiniAnchor, graph.guiMiniPos, gen.guiSize);

			using (Cell.Custom(cellPos, cellSize))
			{
				//dragging
				if (!UI.current.layout)
				{
					if (DragDrop.TryDrag(Cell.current, UI.current.mousePos))
					{
						Vector2 newPosition = GeneratorDraw.MoveGenerator(Cell.current, DragDrop.initialRect.position + DragDrop.totalDelta);
						(graph.guiMiniAnchor, graph.guiMiniPos) = GetAnchorPos(newPosition, gen.guiSize);
					}
					DragDrop.TryRelease(Cell.current);
					DragDrop.TryStart(Cell.current, UI.current.mousePos, Cell.current.InternalRect);
				}

				//shadow
				//GUIStyle shadowStyle = UI.current.textures.GetElementStyle("MapMagic/Node/ShadowMini", 
				//	borders:GeneratorDraw.shadowBorders,
				//	overflow:GeneratorDraw.shadowOverflow);
				//Draw.Element(shadowStyle);

				//drawing
				try { GeneratorDraw.DrawGenerator(gen, graph, selected:false, activeLinks:true); }
				catch (ExitGUIException)
					{ } //ignoring
				catch (Exception e) 
					{ Debug.LogError("Draw Graph Window failed: " + e); }

				//right click menu (should have access to cellObjs)
				if (!UI.current.layout  &&  Event.current.type == EventType.MouseDown  &&  Event.current.button == 1)
					RightClick.DrawRightClickItems(miniSelectedUI, miniSelectedUI.mousePos, graph);
			}
		}


		private void DrawToolbar () 
		{ 
			//using (Timer.Start("DrawToolbar"))

			using (Cell.LinePx(toolbarSize))
			{
				Draw.Button();

				//Graph graph = CurrentGraph;
				//Graph rootGraph = mapMagic.graph;

				//if (mapMagic != null  &&  mapMagic.graph!=graph  &&  mapMagic.graph!=rootGraph) mapMagic = null;

				UI.current.styles.Resize(0.9f);  //shrinking all font sizes

				Draw.Element(UI.current.styles.toolbar);

	
				//undefined graph
				if (graph==null)
				{
					using (Cell.RowPx(200)) Draw.Label("No graph selected to display. Select:");
					using (Cell.RowPx(100)) Draw.ObjectField(ref graph);
					return;
				}

				//if graph loaded corrupted
				if (graph.generators==null) 
				{
					using (Cell.RowPx(300)) Draw.Label("Graph is null. Check the console for the error on load.");

					using (Cell.RowPx(100))
						if (Draw.Button("Reload", style:UI.current.styles.toolbarButton)) graph.OnAfterDeserialize();

					using (Cell.RowPx(100))
					{
						if (Draw.Button("Reset", style:UI.current.styles.toolbarButton)) graph.generators = new Generator[0];
					}
					
					Cell.EmptyRowRel(1);

					return;
				}

				//root graph
				Graph rootGraph = null;
				if (parentGraphs != null  &&  parentGraphs.Count != 0) 
					rootGraph = parentGraphs[0];
					//this has nothing to do with currently assigned mm graph - we can view subGraphs with no mm in scene at all

				if (rootGraph != null)
				{
					Vector2 rootBtnSize = UnityEngine.GUI.skin.label.CalcSize( new GUIContent(rootGraph.name) );
					using (Cell.RowPx(rootBtnSize.x))
					{
						//Draw.Button(graph.name, style:UI.current.styles.toolbarButton, cell:rootBtnCell);
						Draw.Label(rootGraph.name);
							if (Draw.Button("", visible:false))
								EditorGUIUtility.PingObject(rootGraph);
					}
				
					using (Cell.RowPx(20)) Draw.Label(">>"); 
				}

				//this graph
				Vector2 graphBtnSize = UnityEngine.GUI.skin.label.CalcSize( new GUIContent(graph.name) );
				using (Cell.RowPx(graphBtnSize.x))
				{
					Draw.Label(graph.name);
					if (Draw.Button("", visible:false))
						EditorGUIUtility.PingObject(graph);
				}

				//up-level and tree
				using (Cell.RowPx(20))
				{
					if (Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/FolderTree"), iconScale:0.5f, visible:false))
						GraphTreePopup.DrawGraphTree(rootGraph!=null ? rootGraph : graph);
				}

				using (Cell.RowPx(20))
				{
					if (parentGraphs != null  &&  parentGraphs.Count != 0  && 
						Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/FolderUp"), iconScale:0.5f, visible:false))
					{
						graph = parentGraphs[parentGraphs.Count-1];
						parentGraphs.RemoveAt(parentGraphs.Count-1);
						ScrollZoomOnOpen();
						Repaint();
					}
				}

				Cell.EmptyRowRel(1); //switching to right corner

				//seed
				Cell.EmptyRowPx(5);
				using (Cell.RowPx(1)) Draw.ToolbarSeparator();

				using (Cell.RowPx(90))
				//	using (Cell.LinePx(toolbarSize-1))  //-1 just to place it nicely
				{
					#if UNITY_2019_1_OR_NEWER
					int newSeed;
					using (Cell.RowRel(0.4f)) Draw.Label("Seed:");
					using (Cell.RowRel(0.6f))
						using (Cell.Padded(1))
							newSeed = (int)Draw.Field(graph.random.Seed, style:UI.current.styles.toolbarField);
					#else
					Cell.current.fieldWidth = 0.6f;
					int newSeed = Draw.Field(graph.random.Seed, "Seed:");
					#endif
					if (newSeed != graph.random.Seed)
					{
						GraphWindow.RecordCompleteUndo();
						graph.random.Seed = newSeed;
						GraphWindow.current?.RefreshMapMagic();
					}
				}

				Cell.EmptyRowPx(2);


				//gauge
				using (Cell.RowPx(1)) Draw.ToolbarSeparator();

				using (Cell.RowPx(200))
					using (Cell.LinePx(toolbarSize-1)) //-1 to leave underscore under gauge
				{
					if (mapMagic != null)
					{
						bool isGenerating = mapMagic.IsGenerating();

						//background gauge
						if (isGenerating)
						{
							float progress = mapMagic.GetProgress();

							if (progress < 1 && progress != 0)
							{
								Texture2D backgroundTex = UI.current.textures.GetTexture("DPUI/ProgressBar/BackgroundBorderless");
								mapMagic.GetProgress();
								Draw.Texture(backgroundTex);

								Texture2D fillTex = UI.current.textures.GetBlankTexture(StylesCache.isPro ? Color.grey : Color.white);
								Color color = StylesCache.isPro ? new Color(0.24f, 0.37f, 0.58f) : new Color(0.44f, 0.574f, 0.773f);
								Draw.ProgressBarGauge(progress, fillTex, color);
							}

							//Repaint(); //doing it in OnInspectorUpdate
						}

						//refresh buttons
						using (Cell.RowPx(20))
							if (Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/RefreshAll"), iconScale:0.5f, visible:false))
							{
								//graphUI.undo.Record(completeUndo:true); //won't record changed terrain data
								if (mapMagic is MapMagicObject mapMagicObject)
								{
									foreach (Terrain terrain in mapMagicObject.tiles.AllActiveTerrains())
										UnityEditor.Undo.RegisterFullObjectHierarchyUndo(terrain.terrainData, "RefreshAll");
									EditorUtility.SetDirty(mapMagicObject);
								}

								GraphWindow.current.mapMagic.ClearAll();
								GraphWindow.current.mapMagic.StartGenerate();
							}

						using (Cell.RowPx(20))
							if (Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/Refresh"), iconScale:0.5f, visible:false))
							{
								GraphWindow.current.mapMagic.StartGenerate();
							}

						//ready mark
						if (!isGenerating)
						{
							Cell.EmptyRow();
							using (Cell.RowPx(40)) Draw.Label("Ready");
						}
					}

					else
						Draw.Label("Not Assigned to MapMagic Object");
				}

				using (Cell.RowPx(1)) Draw.ToolbarSeparator();

				//focus
				using (Cell.RowPx(20))
					if (Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/FocusSmall"), iconScale:0.5f, visible:false))
					{
						graphUI.scrollZoom.FocusWindowOn(GetNodesCenter(graph), position.size);
					}

				using (Cell.RowPx(20))
				{
					if (graphUI.scrollZoom.zoom < 0.999f)
					{
						if (Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/ZoomSmallPlus"), iconScale:0.5f, visible:false))
							graphUI.scrollZoom.Zoom(1f, position.size/2);
					}
					else
					{
						if (Draw.Button(null, icon:UI.current.textures.GetTexture("DPUI/Icons/ZoomSmallMinus"), iconScale:0.5f, visible:false))
							graphUI.scrollZoom.Zoom(miniZoom, position.size/2); 
					}
				}
			}
		}

		
		private void DrawSceneView ()
		{
			Rect windowRect = UI.current.editorWindow.position;
			SceneView sceneView = SceneView.lastActiveSceneView;

			//drawing
			if (sceneTex == null  ||  sceneTex.width != (int)windowRect.width  ||  sceneTex.height != (int)windowRect.height )
				sceneTex = new RenderTexture((int)windowRect.width, (int)windowRect.height, 24, RenderTextureFormat.ARGB32, 0);
			RenderTexture backTex = sceneView.camera.targetTexture;
			sceneView.camera.targetTexture = sceneTex;
			sceneView.camera.Render();
			sceneView.camera.targetTexture = backTex;

			using (Cell.Custom(
				0, 
				0, 
				UI.current.editorWindow.position.width, 
				UI.current.editorWindow.position.height))
			{
				Cell.current.MakeStatic();
				//Draw.Icon(sceneTex); 
				Draw.Texture(sceneTex);
			}

			//moving/rotating


			
		}
		private static RenderTexture sceneTex = null;

		private void MoveSceneView ()
		{
			SceneView sceneView = SceneView.lastActiveSceneView;

			if (Event.current.alt  &&  Event.current.button == 2)
			{
				Ray rayZero = sceneView.camera.ViewportPointToRay(Vector2.zero);
				Vector3 pointZero = rayZero.origin + rayZero.direction*sceneView.cameraDistance;
				
				Ray rayX = sceneView.camera.ViewportPointToRay(new Vector2(1,0));
				Vector3 pointX = rayX.origin + rayX.direction*sceneView.cameraDistance;

				Ray rayY = sceneView.camera.ViewportPointToRay(new Vector2(0,1));
				Vector3 pointY = rayY.origin + rayY.direction*sceneView.cameraDistance;

				Vector3 axisX = pointZero-pointX;
				Vector3 axisY = pointZero-pointY;

				Vector2 relativeDelta = Event.current.delta / new Vector2(Screen.width, Screen.height);
				relativeDelta.y = -relativeDelta.y;
				sceneView.pivot += axisX*relativeDelta.x + axisY*relativeDelta.y;

				//Debug.DrawLine(pointZero, pointX, Color.red);
				//unfortunately ViewportPointToRay uses scene view - and will return improper values if aspect is different

				Repaint();
			}

			if (Event.current.alt  &&  Event.current.button == 0  &&  !Event.current.isScrollWheel)
			{
				Vector3 rotation = sceneView.rotation.eulerAngles;
				rotation.y += Event.current.delta.x / 10;
				rotation.x += Event.current.delta.y / 10;
				sceneView.rotation = Quaternion.Euler(rotation);

				Repaint();
			}

			if (Event.current.alt  &&  Event.current.isScrollWheel) //undocumented!
			{
				//Vector3 camVec = sceneView.camera.transform.position - sceneView.pivot;
				//float camVecLength = camVec.magnitude;
				//camVecLength *= 1 + Event.current.delta.y*0.02f;

				float size = sceneView.size;
				size *= 1 + Event.current.delta.y*0.02f;

				sceneView.LookAtDirect(sceneView.pivot, sceneView.rotation, size);

				Repaint();
			}	
		}


		private static Vector2 GetNodesCenter (Graph graph)
		{
			//Graph graph = CurrentGraph;
			if (graph.generators.Length==0) return new Vector2(0,0);

			Vector2 min = graph.generators[0].guiPosition;
			Vector2 max = min + graph.generators[0].guiSize;

			for (int g=1; g<graph.generators.Length; g++)
			{
				Vector2 pos = graph.generators[g].guiPosition;
				min = Vector2.Min(pos, min);
				max = Vector2.Max(pos + graph.generators[g].guiSize, max);
			}

			return (min + max)/2;
		}


		private static (Vector2,Vector2) GetAnchorPos (Vector2 genPos, Vector2 genSize)
		{
			Vector2 genCenter = genPos + genSize/2;
			Vector2 anchor =  new Vector2(
				genCenter.x > Screen.width/2 ? 1 : 0,
				genCenter.y > Screen.height/2 ? 1 : 0 );

			Vector2 genCorner = genPos + genSize*anchor;
			Vector2 screenCorner = new Vector2(Screen.width, Screen.height)*anchor;
			Vector2 sign = -(anchor*2 - Vector2.one);
			
			Vector2 pos = (screenCorner + genCorner*sign)*sign;

			Vector2 absPos = pos*sign;
			if (absPos.x < 0) pos.x = 0;
			if (absPos.y < 0) pos.y = 0;

			return (anchor, pos);
		}


		private static Vector2 PlaceByAnchor  (Vector2 anchor, Vector2 pos, Vector2 size)
		{
			Vector2 screenCorner = new Vector2(Screen.width, Screen.height)*anchor;
			Vector2 genCorner = size*anchor;

			return screenCorner - genCorner + pos;
		}


		public void OnSceneGUI (SceneView sceneview)
		{
			if (graph==null || graph.generators==null) return; //if graph loaded corrupted

			bool hideDefaultToolGizmo = false; //if any of the nodes has it's gizmo enabled (to hide the default tool)

			for (int n=0; n<graph.generators.Length; n++)
				if (graph.generators[n] is ISceneGizmo)
				{
					ISceneGizmo gizmoNode = (ISceneGizmo)graph.generators[n];
					gizmoNode.DrawGizmo();
					if (gizmoNode.hideDefaultToolGizmo) hideDefaultToolGizmo = true;
				}
			
			if (hideDefaultToolGizmo) UnityEditor.Tools.hidden = true;
			else UnityEditor.Tools.hidden = false;
		}


		private void DragDrawAddRemove ()
		{
			int origButtonSize = 34; int origButtonOffset = 20;

			Vector2 buttonPos = new Vector2(
				UI.current.editorWindow.position.width - (origButtonSize + origButtonOffset)*UI.current.DpiScaleFactor,
				20*UI.current.DpiScaleFactor);
			Vector2 buttonSize = new Vector2(origButtonSize,origButtonSize) * UI.current.DpiScaleFactor;

			using (Cell.Custom(buttonPos,buttonSize))
			//later button pos could be overriden if dragging it
			{
				Cell.current.MakeStatic();


				//if dragging generator
				if (DragDrop.IsDragging()  &&  !DragDrop.IsStarted()  &&  DragDrop.obj is Cell  &&  UI.current.cellObjs.TryGetObject((Cell)DragDrop.obj, "Generator", out Generator draggedGen) )
				
				{
					if (Cell.current.Contains(UI.current.mousePos))
						Draw.Texture(UI.current.textures.GetTexture("MapMagic/Icons/NodeRemoveActive"));
					else
						Draw.Texture(UI.current.textures.GetTexture("MapMagic/Icons/NodeRemove"));
				}


				//if released generator on remove icon
				else if (DragDrop.IsReleased()  &&  
					DragDrop.releasedObj is Cell  &&  
					UI.current.cellObjs.TryGetObject((Cell)DragDrop.releasedObj, "Generator", out Generator releasedGen)  &&  
					Cell.current.Contains(UI.current.mousePos))
				{
					GraphEditorActions.RemoveGenerators(graph, selected, releasedGen); 
					GraphWindow.current?.RefreshMapMagic();
				}


				//if not dragging generator
				else
				{
					if (focusedWindow==this) drawAddRemoveButton = true;   //re-enabling when window is focused again after popup
					bool drawFrame = false;
					Color frameColor = new Color();

					//dragging button
					if (DragDrop.TryDrag(addDragId, UI.current.mousePos))
					{
						Cell.current.pixelOffset += DragDrop.totalDelta; //offsetting cell position with the mouse

						Draw.Texture(UI.current.textures.GetTexture("MapMagic/Icons/NodeAdd"));

						//if dragging near link, output or node
						Vector2 mousePos = graphUI.mousePos;
						//Vector2 mousePos = graphUI.scrollZoom.ToInternal(addDragTo + new Vector2(addDragSize/2,addDragSize/2)); //add button center

						object clickedObj = RightClick.ClickedOn(graphUI, mousePos);
				
						if (clickedObj != null  &&  !(clickedObj is Group))
						{
							drawFrame = true;
							frameColor = GeneratorDraw.GetLinkColor(Generator.GetGenericType(clickedObj.GetType()));
						}
					}

					//releasing button
					if (DragDrop.TryRelease(addDragId))
					{
						drawAddRemoveButton = false;

						Vector2 mousePos = graphUI.mousePos;
						//Vector2 mousePos = graphUI.scrollZoom.ToInternal(addDragTo + new Vector2(addDragSize/2,addDragSize/2)); //add button center

						RightClick.ClickedNear (graphUI, mousePos, 
							out Group clickedGroup, out Generator clickedGen, out IInlet<object> clickedLink, out IInlet<object> clickedInlet, out IOutlet<object> clickedOutlet, out RightClickExpose clickedField);

						if (clickedOutlet != null)
							CreateRightClick.DrawAppendItems(mousePos, graph, clickedOutlet);

						else if (clickedLink != null)
							CreateRightClick.DrawInsertItems(mousePos, graph, clickedLink);

						else
							CreateRightClick.DrawCreateItems(mousePos, graph);
					}

					//starting button drag
					DragDrop.TryStart(addDragId, UI.current.mousePos, Cell.current.InternalRect);

					//drawing button
					#if !MM_DOC
					if (drawAddRemoveButton) //don't show this button if right-click items are shown
						Draw.Texture(UI.current.textures.GetTexture("MapMagic/Icons/NodeAdd")); //using Texture since Icon is scaled with scrollzoom
					#endif

					if (drawFrame)
					{
						Texture2D frameTex = UI.current.textures.GetColorizedTexture("MapMagic/Icons/NodeAddRemoveFrame", frameColor);
						Draw.Texture(frameTex);
					}
				}
			}
		}

		#region Showing Window

			public static GraphWindow ShowInNewTab (Graph graph)
			{
				GraphWindow window = CreateInstance<GraphWindow>();

				window.OpenRoot(graph);

				ShowWindow(window, inTab:true);
				return window;
			}

			public static GraphWindow Show (Graph graph)
			{
				GraphWindow window = null;
				GraphWindow[] allWindows = Resources.FindObjectsOfTypeAll<GraphWindow>();

				//if opened as biome via focused graph window - opening as biome
				if (focusedWindow is GraphWindow focWin  &&  focWin.graph.ContainsSubGraph(graph))
				{
					focWin.OpenBiome(graph);
					return focWin;
				}

				//if opened only one window - using it (and trying to load mm biomes)
				if (window == null)
				{
					if (allWindows.Length == 1)  
					{
						window = allWindows[0];
						if (!window.TryOpenMapMagicBiome(graph))
							window.OpenRoot(graph);
					}
				}

				//if window with this graph currently opened - just focusing it
				if (window == null)
				{
					for (int w=0; w<allWindows.Length; w++)
						if (allWindows[w].graph == graph)
							window = allWindows[w];
				}

				//if the window with parent graph currently opened
				if (window == null)
				{
					for (int w=0; w<allWindows.Length; w++)
						if (allWindows[w].graph.ContainsSubGraph(graph))
						{
							window = allWindows[w];
							window.OpenBiome(graph);
						}
				}

				//if no window found after all - creating new tab (and trying to load mm biomes)
				if (window == null)
				{
					window = CreateInstance<GraphWindow>();
					if (!window.TryOpenMapMagicBiome(graph))
						window.OpenRoot(graph);
				}
					
				ShowWindow(window, inTab:false);
				return window;
			}


			public void OpenBiome (Graph graph)
			/// In this case we know for sure what window should be opened. No internal checks
			{
				if (parentGraphs == null) parentGraphs = new List<Graph>();
				parentGraphs.Add(this.graph);
				this.graph = graph;
				DragDrop.obj = null; //resetting dragDrop or it will move any generator to position of this one on open
				ScrollZoomOnOpen();
			}


			public void OpenBiome (Graph graph, Graph root)
			/// Opens graph as sub-sub-sub biome to root
			{
				parentGraphs = GetStepsToSubGraph(root, graph);
				this.graph = graph;
				DragDrop.obj = null;
				ScrollZoomOnOpen();
			}


			private bool TryOpenMapMagicBiome (Graph graph)
			/// Finds MapMagic object in scene and opens graph as mm biome with mm graph as a root
			/// Return false if it's wrong mm (or no mm at all)
			{
				mapMagic = FindRelatedMapMagic(graph);
				if (mapMagic == null) return false;

				parentGraphs = GetStepsToSubGraph(mapMagic.Graph, graph);
				this.graph = graph;

				DragDrop.obj = null;
				ScrollZoomOnOpen();

				return true;
			}


			private void OpenRoot (Graph graph)
			{
				this.graph = graph;
				parentGraphs = null;

				DragDrop.obj = null; //resetting dragDrop or it will move any generator to position of this one on open
				ScrollZoomOnOpen();
			}


			private static void ShowWindow (GraphWindow window, bool inTab=false)
			/// Opens the graph window. But it should be created and graph assigned first.
			{
				Texture2D icon = TexturesCache.LoadTextureAtPath("MapMagic/Icons/Window"); 
				window.titleContent = new GUIContent("MapMagic Graph", icon);

				if (inTab) window.ShowTab();
				else window.Show();
				window.Focus();
				window.Repaint();

				DragDrop.obj = null;
				window.ScrollZoomOnOpen(); //focusing after window has shown (since it needs window size)
			}


			private static GraphWindow FindReusableWindow (Graph graph)
			/// Finds the most appropriate window among all of all currently opened
			{
				GraphWindow[] allWindows = Resources.FindObjectsOfTypeAll<GraphWindow>();

				//if opened only one window - using it
				if (allWindows.Length == 1)  
					return allWindows[0];

				//if opening from currently active window
				if (focusedWindow is GraphWindow focWin)
					if (focWin.graph.ContainsSubGraph(graph))
						return focWin;
						
				//if window with this graph currently opened
				for (int w=0; w<allWindows.Length; w++)
					if (allWindows[w].graph == graph)
						return allWindows[w];

				//if the window with parent graph currently opened
				for (int w=0; w<allWindows.Length; w++)
					if (allWindows[w].graph.ContainsSubGraph(graph))
						return allWindows[w];

				return null;
			}


			private void ScrollZoomOnOpen ()
			///Finds a graph scroll and zoom from graphsScrollZooms and focuses on them. To switch between graphs
			///should be called each time new graph assigned
			{
				if (graph == null) return; 

				if (graphsScrollZooms.TryGetValue(graph, out Vector3 scrollZoom))
				{
					graphUI.scrollZoom.FocusWindowOn(new Vector2(scrollZoom.x, scrollZoom.y), position.size);
					graphUI.scrollZoom.zoom = scrollZoom.z;
				}

				else
					graphUI.scrollZoom.FocusWindowOn(GetNodesCenter(graph), position.size);
			}


			public static List<Graph> GetStepsToSubGraph (Graph rootGraph, Graph subGraph)
			/// returns List(this > biome > innerBiome)
			/// doesn't include the subgraph itself
			/// doesn't perform check if subGraph is contained within graph at all
			{
				List<Graph> steps = new List<Graph>();
				ContainsSubGraphSteps(rootGraph, subGraph, steps);
				steps.Reverse();
				return steps;
			}


			private static bool ContainsSubGraphSteps (Graph thisGraph, Graph subGraph, List<Graph> steps)
			/// Same as ContainsSubGraph, but using track list for GetStepsToSubGraph
			{
				if (thisGraph == subGraph)
					return true;

				foreach (Graph biomeSubGraph in thisGraph.SubGraphs())
					if (ContainsSubGraphSteps(biomeSubGraph, subGraph, steps))
					{
						steps.Add(thisGraph);
						return true;
					}
				
				return false;
			}


			[MenuItem ("Window/MapMagic/Editor")]
			public static void ShowEditor ()
			{
				MapMagicObject mm = FindObjectOfType<MapMagicObject>();
				Graph gens = mm!=null? mm.graph : null;
				GraphWindow.Show(mm?.graph);
			}

			[UnityEditor.Callbacks.OnOpenAsset(0)]
			public static bool ShowEditor (int instanceID, int line)
			{
				UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceID);
				if (obj is Nodes.Graph graph) 
				{ 
					if (graph.generators == null)
						graph.OnAfterDeserialize();
					if (graph.generators == null)
						throw new Exception("Error loading graph");

					if (UI.current != null) UI.current.DrawAfter( new Action( ()=>GraphWindow.Show(graph) ) ); //if opened via graph while drawing it - opening after draw
					else Show(graph); 
					return true; 
				}
				if (obj is MapMagicObject) { GraphWindow.Show(((MapMagicObject)obj).graph); return true; }
				return false;
			}

		#endregion
	}

}//namespace