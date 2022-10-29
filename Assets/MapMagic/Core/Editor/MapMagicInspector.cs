
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;

using MapMagic.Nodes;
using MapMagic.Terrains;
using MapMagic.Locks;

using MapMagic.Nodes.GUI; //to open up editor window
using MapMagic.Terrains.GUI; //pin draw

namespace MapMagic.Core.GUI 
{
	[CustomEditor(typeof(MapMagicObject))]
	public class MapMagicInspector : Editor
	{
		public static MapMagicInspector current; //assigned on draw and removed after

		public MapMagicObject mapMagic; //aka target

		UI ui = new UI();

		public int backgroundHeight = 0; //to draw type background
		public int oldSelected = 0; //to repaint gui with new background if new type was selected


		public PinDraw.SelectionMode selectionMode = PinDraw.SelectionMode.none;
		public bool saveDraft = false;

		public enum Pots { _64=64, _128=128, _256=256, _512=512, _1024=1024, _2048=2048, _4096=4096 };

		bool guiAbout = false;

		private RectOffset pinButtonsOverflow;

		public static  Action<MapMagicObject, Vector3> OnClusterExported; //called in cluster module (not available yet)


		[RuntimeInitializeOnLoadMethod, UnityEditor.InitializeOnLoadMethod] 
		static void Subscribe ()
		{
			TerrainTile.OnLodSwitched += (TerrainTile t,bool m, bool d) => SceneView.RepaintAll();
		}


		public void OnEnable ()
		{
			EditorHacks.SetIconForObject(target, TexturesCache.LoadTextureAtPath("MapMagic/Icons/Window"));
		}


		//when selected
		public void OnSceneGUI ()
		{	
			//foreach (TerrainTile tile in mapMagic.tiles.All())
			//	EditorUtility.SetSelectedRenderState(tile.ActiveTerrain.GetComponent<Terrain>(), EditorSelectedRenderState.Hidden);

			current = this;
			if (mapMagic == null) mapMagic = (MapMagicObject)target;
			if (!mapMagic.enabled) return;

			FrameDraw.DrawSceneGUI(mapMagic);

			if (mapMagic.guiTiles)
				PinDraw.DrawSceneGUI(mapMagic, ref selectionMode, saveDraft);

			if (mapMagic.guiLocks)
				LockDraw.DrawSceneGUI(mapMagic); 

			current = null;
		}


		public override void  OnInspectorGUI ()
		{
			current = this;
			mapMagic = (MapMagicObject)target;

			//waiting for object selector close
			if (Event.current.type == EventType.ExecuteCommand && 
				Event.current.commandName == "ObjectSelectorUpdated" && 
				ScriptableAssetExtensions.GetObjectSelectorId() == 12345)
					mapMagic.graph = (Graph)ScriptableAssetExtensions.GetObjectSelectorObject();

			if (ui.undo == null)
				ui.undo = new Den.Tools.GUI.Undo {
					undoObject = mapMagic,
					undoName = "MapMagic Inspector Value"
				};

			ui.Draw(DrawGUI, inInspector:true);

			current = null;
		}

		public void DrawGUI ()
		{
			Cell.EmptyLinePx(4);

			//graph
			using (Cell.LinePx(20))
			{
				using (Cell.RowPx(50)) Draw.Label("Graph");
				using (Cell.Row) 
				{
					Graph oldGraph = mapMagic.graph;
					Draw.ObjectField(ref mapMagic.graph);

					if (Cell.current.valChanged && mapMagic.graph != null)
					{
						GraphWindow.current.UpdateRelatedMapMagic();
						GraphWindow.current.Repaint();
						mapMagic.Refresh();
					}
				}

				using (Cell.RowPx(22))
				{
					Cell.current.disabled = mapMagic.graph==null || mapMagic.graph.Equals(null);
					Texture2D openIcon = UI.current.textures.GetTexture("DPUI/Icons/FolderOpen");
					if (Draw.Button(icon:openIcon, iconScale:0.5f, visible:false))
						GraphWindow.Show(mapMagic.graph);
				}
			}



			//graph empty warning
			if (mapMagic.graph==null || mapMagic.graph.Equals(null))
				using (Cell.LinePx(80))
				{
					using (Cell.LinePx(50)) Draw.Label("MapMagic graph is not assigned. \nEither create a new one or \nselect already existing graph");
						
					using (Cell.LinePx(20))
					{
						using (Cell.Row) if (Draw.Button("Create Empty")) 
						{
							Graph graph = Graph.Create();
							graph.OnBeforeSerialize();
							graph = ScriptableAssetExtensions.SaveAsset(graph, filename:"MapMagic Graph", caption:"Save MapMagic Graph");
							mapMagic.graph = graph;
							mapMagic.tiles.Pin( new Coord(0,0), false, mapMagic );
						}

						using (Cell.Row) if (Draw.Button("Create Template")) 
						{
							Graph graph = GraphTemplates.CreateTemplate();
							graph.OnBeforeSerialize();
							graph = ScriptableAssetExtensions.SaveAsset(graph, filename:"MapMagic Graph", caption:"Save MapMagic Graph");
							mapMagic.graph = graph;
							mapMagic.tiles.Pin( new Coord(0,0), false, mapMagic );
						}

						using (Cell.Row) if (Draw.Button("Select")) 
						{
							//showing object selector
							ScriptableAssetExtensions.ShowObjectSelector(typeof(Graph), 12345, false);
						}
					}

					return;
				}

			//graph not loaded
			else if (mapMagic.graph.generators == null)
			{
				using (Cell.LineStd) Draw.Label("Could not load graph");
				using (Cell.LineStd) Draw.Label("See console for details");

				return;
			}

			//seed
			/*using (Cell.LinePx(20)))
			{
				Draw.Label("Seed", cell:UI.Empty(Size.RowPixels(50)));
				Draw.Field(ref mapMagic.seed, cell:UI.Empty(Size.row));
				UI.Empty(Size.RowPixels(68));
			}*/

			//generate
			Cell.EmptyLinePx(4);

			using (Cell.LinePx(30))
			{
				//if (pinButtonsOverflow == null) pinButtonsOverflow = new RectOffset(0,0,1,2);
				GUIStyle style = UI.current.textures.GetElementStyle("MapMagic/PinButtons/Top", "MapMagic/PinButtons/Top_pressed");  

				using (Cell.Padded(0,0,-2,-1))
					if (Draw.Button("", style:style)) 
				{
					//graphUI.undo.Record(completeUndo:true); //won't record changed terrain data
					foreach (Terrain terrain in mapMagic.tiles.AllActiveTerrains())
						UnityEditor.Undo.RegisterFullObjectHierarchyUndo(terrain.terrainData, "RefreshAll");
					EditorUtility.SetDirty(mapMagic);

					mapMagic.ClearAll();
					mapMagic.StartGenerate();
				}

				Draw.Label("Generate", style:UI.current.styles.middleCenterLabel);
				using (Cell.RowPx(30)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/RefreshAll"), scale:0.5f);
			}

			using (Cell.LinePx(22))
			{
				GUIStyle style = UI.current.textures.GetElementStyle("MapMagic/PinButtons/Bottom", "MapMagic/PinButtons/Bottom_pressed");

				using (Cell.Padded(0,0,-2,-1))
					if (Draw.Button("", style:style)) 
				{
					mapMagic.StartGenerate();
				}
					
				Draw.Label("Generate Changed", style:UI.current.styles.middleCenterLabel);
				using (Cell.RowPx(30)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Refresh"), scale:0.5f);
			}

			//Tiles
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiTiles, "Tiles", isLeft:true))
					if (mapMagic.guiTiles)
						PinDraw.DrawInspectorGUI(mapMagic, ref selectionMode, ref saveDraft);
				
			//Locks
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiLocks, "Locks", isLeft:true))
					if (mapMagic.guiLocks)
						LockDraw.DrawInspectorGUI(mapMagic);

			//Infinite Terrain
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiInfiniteTerrains, "Infinite Terrain (Playmode)", isLeft:true))
					if (mapMagic.guiInfiniteTerrains)
					{
						using (Cell.LineStd) Draw.ToggleLeft(ref mapMagic.tiles.generateInfinite, "Generate Infinite Terrain");

						using (Cell.LineStd) Draw.Field(ref mapMagic.mainRange, "Main Range");
						using (Cell.LineStd) 
						{
							if (!mapMagic.draftsInPlaymode) 
							{
								Cell.current.disabled = true;
								mapMagic.tiles.generateRange = mapMagic.mainRange;
							}

							Draw.Field(ref mapMagic.tiles.generateRange, "Drafts Range");
						}

						Cell.EmptyLinePx(4);
						using (Cell.LineStd) Draw.ToggleLeft(ref mapMagic.hideFarTerrains, "Hide Out-of-Range Terrains");
					
						Cell.EmptyLinePx(4);
						using (Cell.LineStd) Draw.Label("Generate Terrain Markers:");
						using (Cell.LineStd) Draw.Toggle(ref mapMagic.tiles.genAroundMainCam, "Around Main Camera");
						using (Cell.LineStd) 
						{
							using (Cell.RowRel(1-Cell.current.fieldWidth))
								Draw.Label("Around Objects Tagged");

							using (Cell.RowRel(Cell.current.fieldWidth))
							{
								using (Cell.RowPx(20))
									Draw.Toggle(ref mapMagic.tiles.genAroundObjsTag);

								using (Cell.Row)
									mapMagic.tiles.genAroundTag = Draw.Field(
										mapMagic.tiles.genAroundTag, 
										drawFn:(Rect rect, string oldVal) => { return EditorGUI.TagField(rect, (string)oldVal); } );
							}
						}
					}

			//Size and Resolution
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiTileSettings, "Tile Settings", isLeft:true))
					if (mapMagic.guiTileSettings)
					{
						using (Cell.LineStd) 
						{
							float newSize = Draw.Field(mapMagic.tileSize.x, "Size");
							if (Cell.current.valChanged)
							{
								//UnityEditor.Undo.RegisterFullObjectHierarchyUndo(mapMagic.gameObject, "MapMagic Tile Size");
								mapMagic.tileSize.z = newSize; 
								mapMagic.tileSize.x = newSize;
							}
						}

						Cell.EmptyLinePx(6);
						using (Cell.LineStd) 
						{
							MapMagicObject.Resolution prevResolution = mapMagic.tileResolution;
							Draw.Field(ref mapMagic.tileResolution, "Main Resolution");
							if (Cell.current.valChanged && mapMagic.locks.Length!=0)
							{
								if (!EditorUtility.DisplayDialog("Resolution Change", "This will remove all of the Lock custom terrain data. Are you sure you wish to continue?", "Change", "Cancel"))
									mapMagic.tileResolution = prevResolution;
							}
						}
						using (Cell.LineStd) Draw.Field(ref mapMagic.tileMargins, "Main Margins");

						Cell.EmptyLinePx(6);
						if (mapMagic.draftsInEditor || mapMagic.draftsInPlaymode)
						{
							using (Cell.LineStd) Draw.Field(ref mapMagic.draftResolution, "Draft Resolution");
							using (Cell.LineStd) Draw.Field(ref mapMagic.draftMargins, "Draft Margins");
						}

						Cell.EmptyLinePx(6);
						using (Cell.LineStd) Draw.Label("Use Draft (low-detail) Terrains in:");
						using (Cell.LineStd) 
						{
							Draw.ToggleLeft(ref mapMagic.draftsInEditor, "Editor");
							if (Cell.current.valChanged) mapMagic.EnableEditorDrafts(mapMagic.draftsInEditor);
						}
						using (Cell.LineStd) 
						{
							Draw.ToggleLeft(ref mapMagic.draftsInPlaymode, "Playmode");
							if (!mapMagic.draftsInPlaymode) mapMagic.tiles.generateRange = mapMagic.mainRange; //if just changed
						}

						if (Cell.current.valChanged)
						{
							if (mapMagic.tileMargins < 2) mapMagic.tileMargins = 2;  //min margins is 2 for now
							if (mapMagic.draftMargins < 2) mapMagic.draftMargins = 2;

							mapMagic.ApplyTileSettings();
						}
					}

			//Outputs settings
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiOutputsSettings, "Outputs Settings", isLeft:true))
					if (mapMagic.guiOutputsSettings)
					{
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("Height Output", style:UI.current.styles.boldLabel);

								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.height, "Height");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.heightInterpolation, "Interpolate");

								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("Apply Type");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.heightMainApply, "Main");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.heightDraftApply, "Draft");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.heightSplit, "Split Frame");

								Cell.EmptyLinePx(2);
							}
						}

						#if CTS_PRESENT
						Cell.EmptyLinePx(4);
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("CTS Output", style:UI.current.styles.boldLabel);
								using (Cell.LineStd) Draw.ObjectField(ref mapMagic.globals.ctsProfile, "CTS Profile");
								Cell.EmptyLinePx(2);
							}
						}
						#endif

						#if __MICROSPLAT__
						Cell.EmptyLinePx(4);
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("MicroSplat Output", style:UI.current.styles.boldLabel);

								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.ObjectField(ref mapMagic.globals.microSplatPropData, "Prop Data");
								using (Cell.LineStd) Draw.Toggle(ref mapMagic.globals.microSplatTerrainDescriptor, "Terrain Descriptor");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.microSplatApplyType, "Apply Type");
								using (Cell.LineStd) Draw.Toggle(ref mapMagic.globals.useCustomControlTextures, "Use Custom Textures");
								using (Cell.LineStd) Draw.Toggle(ref mapMagic.globals.assignComponent, "Set Component");
								using (Cell.LineStd) Draw.ObjectField(ref mapMagic.globals.megaSplatTexList, "TexList");

								Cell.EmptyLinePx(2);
							}
						}
						#endif

						#if __MEGASPLAT__
						Cell.EmptyLinePx(4);
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("MegaSplat Output", style:UI.current.styles.boldLabel);
								using (Cell.LineStd) Draw.ObjectField(ref mapMagic.globals.megaSplatTexList, "TexList");
								using (Cell.LineStd) Draw.Toggle(ref mapMagic.globals.assignComponent, "Set Component");
								Cell.EmptyLinePx(2);
							}
						}
						#endif

						#if VEGETATION_STUDIO_PRO
						Cell.EmptyLinePx(4);
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("VSPro Output", style:UI.current.styles.boldLabel);

								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.height, "Height");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.heightInterpolation, "Interpolate");

								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.ObjectField(ref mapMagic.globals.vegetationPackage, "Vegetation Package");
								using (Cell.LineStd) Draw.ObjectField(ref mapMagic.globals.vegetationSystem, "Vegetation system");
								using (Cell.LineStd) Draw.Toggle(ref mapMagic.globals.vegetationSystemCopy, "Copy System");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.sourceMultiPackageVsProTagName, "Source MultiPackage VSPro Tag Name");

								Cell.EmptyLinePx(2);
							}
						}
						#endif

						Cell.EmptyLinePx(4);
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("Grass Output", style:UI.current.styles.boldLabel);

								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.grassResDownscale, "Resolution Downscale");
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.grassResPerPatch, "Resolution per Patch");

								Cell.EmptyLinePx(2);
							}
						}

						Cell.EmptyLinePx(4);
						using (Cell.LinePx(0))
						{
							Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.Padded(2,2,0,0))
							{
								Cell.EmptyLinePx(2);
								using (Cell.LineStd) Draw.Label("Objects Output", style:UI.current.styles.boldLabel);
								using (Cell.LineStd) Draw.Field(ref mapMagic.globals.objectsNumPerFrame, "Num Per Frame");
								Cell.EmptyLinePx(2);
							}
						}
						
						if (Cell.current.valChanged)
						{
							mapMagic.ClearAll();
							if (mapMagic.instantGenerate)
								mapMagic.StartGenerate();
						}
					}


			//Exposed variables
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiExposedVariables, "Exposed Variables", isLeft:true))
					if (mapMagic.guiExposedVariables)
						Expose.GUI.OverrideInspector.DrawStaticOverride(mapMagic.graph.defaults);


			//Terrain Settings
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiTerrainSettings, "Terrain Properties", isLeft:true))
					if (mapMagic.guiTerrainSettings)
					{
						Cell.EmptyLinePx(6);
						using (Cell.LinePx(0))
						{
							using (Cell.LineStd)
							{
								using (Cell.RowRel(1-Cell.current.fieldWidth-0.08f)) Draw.Label("Auto Connect");
								using (Cell.RowRel(0.08f)) Draw.Toggle(ref mapMagic.terrainSettings.allowAutoConnect);
								using (Cell.RowRel(Cell.current.fieldWidth)) Draw.Field(ref mapMagic.terrainSettings.groupingID);
							}
							//using (Cell.LineStd) Draw.Field(ref mapMagic.terrainSettings.pixelError, "Pixel Error");
							using (Cell.LineStd)
							{
								using (Cell.RowRel(1-Cell.current.fieldWidth-0.08f)) Draw.Label("Base Map Distance");
								using (Cell.RowRel(0.08f)) Draw.Toggle(ref mapMagic.terrainSettings.showBaseMap);
								using (Cell.RowRel(Cell.current.fieldWidth)) Draw.Field(ref mapMagic.terrainSettings.baseMapDist);
							}
							using (Cell.LineStd) Draw.Field(ref mapMagic.terrainSettings.baseMapResolution, "Base Map Resolution");
							
							Draw.Class(mapMagic.terrainSettings, category:"Terrain");

							if (Cell.current.valChanged) mapMagic.ApplyTerrainSettings();
						}

						Cell.EmptyLinePx(6);
						using (Cell.LineStd) Draw.Field(ref mapMagic.terrainSettings.material, "Material Template");

						Cell.EmptyLinePx(6);
						using (Cell.LineStd)
						{
							Draw.ToggleLeft(ref mapMagic.guiHideWireframe, "Hide Selection Outline"); //
							if (Cell.current.valChanged) mapMagic.transform.ToggleDisplayWireframe(mapMagic.guiHideWireframe);
						}

						//copy
						Cell.EmptyLinePx(6);
						using (Cell.LineStd) Draw.ToggleLeft(ref mapMagic.terrainSettings.copyLayersTags, "Copy Layers to Terrains");
						using (Cell.LineStd) Draw.ToggleLeft(ref mapMagic.terrainSettings.copyLayersTags, "Copy Tags to Terrains");
						using (Cell.LineStd) Draw.ToggleLeft(ref mapMagic.terrainSettings.copyComponents, "Copy Components to Terrains");

						if (Cell.current.valChanged)
							mapMagic.ApplyTerrainSettings();
					}


			//Trees, Details and Grass Settings
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiTreesGrassSettings, "Trees, Details and Grass Properties", isLeft:true))
					if (mapMagic.guiTreesGrassSettings)
					{
						Draw.Class(mapMagic.terrainSettings, category:"Trees");
						Cell.EmptyLinePx(5);
						Draw.Class(mapMagic.terrainSettings, category:"Grass");
						Cell.EmptyLinePx(5);
						Draw.Class(mapMagic.terrainSettings, category:"WindTint");

						if (Cell.current.valChanged) mapMagic.ApplyTerrainSettings();
					}

			//Multithreading
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref mapMagic.guiThreads, "Multithreading", isLeft:true))
					if (mapMagic.guiThreads)
					{
						using (Cell.LineStd) Draw.Toggle(ref Den.Tools.Tasks.ThreadManager.useMultithreading, "Use Multithreading");
						using (Cell.LineStd) Draw.Toggle(ref Den.Tools.Tasks.ThreadManager.autoMaxThreads, "Auto Max Threads");
						using (Cell.LineStd) 
						{
							Cell.current.disabled = Den.Tools.Tasks.ThreadManager.autoMaxThreads;
							Draw.Field(ref Den.Tools.Tasks.ThreadManager.maxThreads, "Max Threads");
						}

						Cell.EmptyLinePx(5);

						using (Cell.LineStd) 
							Draw.Field(ref Den.Tools.Tasks.CoroutineManager.timePerFrame, "Apply Time per Frame");

						Cell.EmptyLinePx(5);

						using (Cell.LineStd) Draw.Toggle(ref mapMagic.instantGenerate, "Instant Generate");

						Cell.EmptyLinePx(5);

						using (Cell.LinePx(32)) Draw.Helpbox("Multitheading values are shared between all MapMagic objects in all scenes");

					}

			//About
			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref guiAbout, "About", isLeft:true))
					if (guiAbout)
					{
						using (Cell.Line)
							AboutWindow.DrawAbout();
					}
		}





		public bool GetDebug ()
		{
			#if UNITY_EDITOR
			UnityEditor.BuildTargetGroup buildGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
			string defineSymbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);

			return (defineSymbols.Contains("WDEBUG;") || defineSymbols.EndsWith("WDEBUG"));
			#else
			return false;
			#endif
		}

		public void SetDebug (object debug) { SetDebug((bool)debug); }
		public void SetDebug (bool debug)
		{
			#if UNITY_EDITOR
			UnityEditor.BuildTargetGroup buildGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
			string defineSymbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
			
			if (debug)
			{
				defineSymbols += (defineSymbols.Length!=0? ";" : "") + "WDEBUG";
			}
			else
			{
				defineSymbols = defineSymbols.Replace("WDEBUG",""); 
				defineSymbols = defineSymbols.Replace(";;", ";"); 
			}
			
			UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, defineSymbols);
			#endif
		}


		[MenuItem ("GameObject/3D Object/MapMagic")]
		public static MapMagicObject CreateMapMagic () { return CreateMapMagic(null); }
		
		public static MapMagicObject CreateMapMagic (Graph graph)
		{
			GameObject go = new GameObject();
			go.SetActive(false); //to avoid starting generate while graph not assigned
			go.name = "MapMagic";
			MapMagicObject mapMagic = go.AddComponent<MapMagicObject>();
			Selection.activeObject = mapMagic;

			mapMagic.graph = graph;
			go.SetActive(true);
			mapMagic.tiles.Pin( new Coord(0,0), false, mapMagic );

			//registering undo
			UnityEditor.Undo.RegisterCreatedObjectUndo (go, "MapMagic Create");
			EditorUtility.SetDirty(mapMagic);

//			Selection.activeGameObject = mapMagic.gameObject;
			//MapMagicWindow.Show(mapMagic.gens, mapMagic, asBiome:false);

			return mapMagic;
		}

	}//class

}//namespace