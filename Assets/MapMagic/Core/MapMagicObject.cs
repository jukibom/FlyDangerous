
using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Den.Tools;

using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Terrains;
using MapMagic.Locks;

namespace MapMagic.Core 
{
	public interface IMapMagic
	{
		Graph Graph {get;}
		void Refresh ();
		void Refresh (Generator gen1);
		void Refresh (Generator gen1, Generator gen2);
		void Refresh (bool main, bool draft);
		void Clear (Generator gen);
		void ClearAll ();
		void StartGenerate (bool main=true, bool draft=true);
		float GetProgress ();
		bool IsGenerating ();
		bool ContainsGraph (Graph graph);
		Globals Globals {get;}
	}

	[SelectionBase]
	[ExecuteInEditMode] //to call onEnable, then it will subscribe to editor update
	[HelpURL("https://gitlab.com/denispahunov/mapmagic/wikis/home")]
	[DisallowMultipleComponent]
	public class MapMagicObject : MonoBehaviour, IMapMagic, ISerializationCallbackReceiver
	{
		public static readonly SemVer version = new SemVer(2,1,11); 

		//graph
		public Graph graph;
		public Graph Graph => graph;
		public bool guiOpenObjectSelector = false;  //forcing opening of object selector if mm was created with "Load" flag

		//terrains grid
		public bool guiTiles = true;
		public TerrainTileManager tiles = new TerrainTileManager() { allowMove=true };
		public Vector2D tileSize = new Vector2D(1000,1000);  //IDEA: move to tiles manager?

		[SerializeField] Coord previewCoord;
		[SerializeField] bool previewAssigned;

		public enum Resolution { _33=33, _65=65, _129=129, _257=257, _513=513, _1025=1025, _2049=2049 };
		public Resolution tileResolution = Resolution._513;
		public int tileMargins = 16;

		public bool draftsInEditor = true;
		public bool draftsInPlaymode = true;
		public Resolution draftResolution = Resolution._65;
		public int draftMargins = 2;
		
		//locks
		public bool guiLocks = false;
		public Lock[] locks = new Lock[0];

		public bool guiInfiniteTerrains = false;
		public bool hideFarTerrains = true;
		public int mainRange = 1;
		public int DraftRange => tiles.generateRange;

		//terrain settings
		public TerrainSettings terrainSettings = new TerrainSettings();  //pixel error, shadows, trees, grass, etc.

		//outputs settings
		public Globals globals = new Globals();
		public Globals Globals => globals;

		//mapmagic settings
		public bool guiSettings = false;
		public bool guiTileSettings = false;
		public bool guiOutputsSettings = false;
		public bool guiExposedVariables = false;
		public bool guiTerrainSettings = false;
		public bool guiDraftSettings = false;
		public bool guiTreesGrassSettings = false;
		public bool instantGenerate = true;
		public bool saveIntermediate = true;
		public int heightWeldMargins = 5;
		public int splatsWeldMargins = 2;
		public bool guiHideWireframe = false;
		public bool guiThreads = false;

		[NonSerialized] public bool guiDraggingField = false;  //DragDrop.group=="DragField", stored here since DragDrop is editor class

		public bool applyColliders = true;
		public bool setDirty; //registering change for undo. Inverting this value if Unity Undo does not see a change, but actually there is one (for example when saving data)

		//world shift
		public bool shift = false;
		public int shiftThreshold = 4000;
		public int shiftExcludeLayers = 0;

		public static bool isPlaying = true;
		//EditorApplication.isPlayingOrWillChangePlaymode does not work in thread
		//switched via SetIsPlaying

		public void OnEnable ()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= EditorUpdate; //just in case OnDisabled was not called somehow
			UnityEditor.EditorApplication.update += EditorUpdate;	

			isPlaying = UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
			UnityEditor.EditorApplication.playModeStateChanged -= SetIsPlaying;
			UnityEditor.EditorApplication.playModeStateChanged += SetIsPlaying;
			#endif

			if (terrainSettings.material == null)
				terrainSettings.material = DefaultTerrainMaterial();

			//generating all tiles that were not generated previously
			StopGenerate();
			StartGenerateNonReady(); //executing in update, otherwise will not find obj pool
		}


		public void OnDisable ()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= EditorUpdate;	
			#endif
		}

		public void EditorUpdate ()
		{
			#if UNITY_EDITOR
			if (!isPlaying) Update();
			#endif
		}

		public void Update () 
		{ 
			tiles.Update((Vector3)tileSize, pinned:tiles.pinned, holder:this, distsOnly:!isPlaying); //distsOnly: only updating distance priority in editor
			
			Den.Tools.Tasks.CoroutineManager.Update();
		}

		#if UNITY_EDITOR
		public static void SetIsPlaying (UnityEditor.PlayModeStateChange m) 
			{ isPlaying = m==UnityEditor.PlayModeStateChange.EnteredPlayMode; }
		#endif

		public void ApplyTileSettings ()
		{
			StopGenerate();

			foreach (TerrainTile tile in tiles.Tiles())
				tile.Resize();
			
			ClearAll();

			if (instantGenerate)
				StartGenerate();
		}


		public void ApplyTerrainSettings ()
		{
			foreach (TerrainTile tile in tiles.All())
			{
				if (tile.main != null) terrainSettings.ApplyAll(tile.main.terrain);
				if (tile.draft != null) { terrainSettings.ApplyAll(tile.draft.terrain); tile.draft.terrain.groupingID = -1; }
			}
		}

		public Material DefaultTerrainMaterial ()
		{
			#if UNITY_2019_2_OR_NEWER
			Shader shader = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset?.defaultTerrainMaterial?.shader;
			#else
			Shader shader = null;
			#endif
		
			if (shader == null) shader = Shader.Find("HDRP/TerrainLit");
			if (shader == null) shader = Shader.Find("Nature/Terrain/Standard");
			if (shader == null) shader = Shader.Find("Lightweight Render Pipeline/Terrain/Lit");

			return new Material(shader);
		}

		#region Preview

			public TerrainTile PreviewTile
			{get{
					TerrainTile previewTile = null;
					if (previewAssigned) previewTile = tiles[previewCoord];
					if (previewTile == null) previewTile = tiles.ClosestMain();
					return previewTile;
			}}

			public void AssignPreviewTile (TerrainTile tile)
			{
				previewCoord = tile.coord;
				previewAssigned = true;
			}

			public void ClearPreviewTile () => previewAssigned = false;

			public TileData PreviewData => PreviewTile?.main.data;
			public Terrain PreviewTerrain => PreviewTile?.main?.terrain;

			//these ones ignore the automatic closes tile:
			public TerrainTile AssignedPreviewTile => tiles[previewCoord];
			public TileData AssignedPreviewData => AssignedPreviewTile?.main.data;
			public Terrain AssignedPreviewTerrain => AssignedPreviewTile?.main?.terrain;

		#endregion

		#region IMapMagic

			public void Refresh () 
			/// Makes current mapMagic to re-generate all
			{
				if (graph == null) return;
				graph.changeVersion++;

				ClearAll();

				if (instantGenerate)
					StartGenerate();
			}

			public void Refresh (Generator gen) => Refresh(gen, null);
			public void Refresh (Generator gen1, Generator gen2)
			/// Makes MM to reset only selected generators and generate
			/// This will not check if these generators are really contained in graph, so check it beforehand. Not a big deal though mm will generate 
			{
				if (graph == null) return;

				if (gen1!=null) Clear(gen1);
				if (gen2!=null) Clear(gen2);

				if (instantGenerate)
					StartGenerate();
			}

			public void Refresh (bool main, bool draft) 
			/// Makes current mapMagic to re-generate drafts or mains only
			{
				if (graph == null) return;

				if (instantGenerate)
					StartGenerate(main, draft);
			}

			public bool ContainsGraph (Graph graph)
			/// Does MM has graph assigned in any way (as a root graph or biome) 
			{
				if (this.graph == null) return false;
				if (this.graph.generators == null) return false; //avoiding breaking all if something went wrong on loading
				if (this.graph == graph  ||  this.graph.ContainsSubGraph(graph, recursively:true)) return true;
				return false;
			}

			public void Clear (Generator gen)
			{
				//clearing datas (no matter if this node is used or not)
				foreach (TerrainTile tile in tiles.All())
				{
					if (tile.main?.data!=null) 
					{
						graph.ClearGenerator(gen, tile.main.data);
						graph.ClearChanged(tile.main.data);
					}
					if (tile.draft?.data!=null) 
					{
						graph.ClearGenerator(gen, tile.draft.data);
						graph.ClearChanged(tile.draft.data);
					}
				}
			}

			public void ClearAll ()
			{
				foreach (TerrainTile tile in tiles.All())
				{
					tile.StopGenerate(); //this will reset tile tasks

					tile.main?.data?.Clear(inSubs:true);
					tile.draft?.data?.Clear(inSubs:true); 
				}
			}

			[Obsolete] public void Purge (OutputGenerator outGen, bool main=true, bool draft=true)
			{
				foreach (TerrainTile tile in tiles.Tiles())
				{
					if (tile.main?.data != null) outGen.ClearApplied(tile.main.data, tile.main.terrain);
					if (tile.draft?.data != null) outGen.ClearApplied(tile.draft.data, tile.draft.terrain);
				}
			}

			public void ResetTerrains ()
			{
				foreach (TerrainTile tile in tiles.Tiles())
					tile.ResetTerrain();
			}

			public void GenerateTerrain (TerrainTile tile)
			/// Used by: Tile.OnChange
			{
				if (instantGenerate)
					tile.StartGenerate(graph);
			}

			public void StartGenerate (bool main=true, bool draft=true)
			/// Start generating all tiles (if the specified lod is enabled)
			{
				if (graph == null)
					throw new Exception("MapMagic: Graph data is not assigned");

				if (draft || main)
					foreach (TerrainTile tile in tiles.All())
						tile.StartGenerate(graph, main, draft);  //enqueue all of chunks before starting generate
			}


			public void StartGenerate (TerrainTile tile, bool generateMain=true, bool generateLod=true)
			/// Used by: Tile.OnChange
			{
				if (instantGenerate)
					tile.StartGenerate(graph, generateMain:generateMain, generateLod:generateLod);
			}


			public void StartGenerateNonReady ()
			/// Start generating all tiles if they are not already generated
			{
				if (graph == null)
					throw new Exception("MapMagic: Graph data is not assigned");

				foreach (TerrainTile tile in tiles.All())
					if (!tile.Ready)
						tile.StartGenerate(graph);
			}

			public void StopGenerate ()
			{
				if (graph != null)
					foreach (TerrainTile tile in tiles.All())
						tile.StopGenerate();
			}

			public void SwitchLods ()
			{
				foreach (TerrainTile tile in tiles.All())
					tile.SwitchLod();
			}

			public void EnableEditorDrafts (bool enabled)
			{
				foreach (TerrainTile tile in tiles.All())
				{
					if (enabled && tile.draft==null) tile.draft = new TerrainTile.DetailLevel(tile, isDraft:true);
					if (!enabled && tile.draft!=null) { tile.draft.Remove(); tile.draft = null; }
				}
			}


			public bool IsGenerating ()
			/// Finds out if MapMagic is currently generating or applying terrains. 
			/// Much faster than GetProgress, could be called every frame
			{
				//if (!Den.Tools.Tasks.CoroutineManager.IsWorking && !Den.Tools.Tasks.ThreadManager.IsWorking) return false;
				//else return true;
				//might be doing other routine operations (will be added later)

				foreach (TerrainTile tile in tiles.All())
					if (tile.IsGenerating) 
						return true;

				return false;
			}


			public float GetProgress ()
			/// Returns minimum and maximum of the generated tiles (excluding previews), in percent 0-1
			{
				float generateComplexity = graph.GetGenerateComplexity();
				float applyComplexity = graph.GetApplyComplexity();

				float totalComplexity = 0;
				float totalComplete = 0;

				foreach (TerrainTile tile in tiles.All())
				{
					(float complete, float complexity) tileProgress = tile.GetProgress(graph, generateComplexity, applyComplexity);

					totalComplete += tileProgress.complete;
					totalComplexity += tileProgress.complexity;
				}

				return totalComplete / totalComplexity;

			}

		#endregion


		#region Serialization
			
			public bool serializedMultithreading = true;
			public int serializedMaxThreads = 3; 
			public bool serializedAutoMaxThreads = true;
			public float serializedMaxApplyTime = 10;

			public virtual void OnBeforeSerialize () 
			{
				serializedMultithreading = Den.Tools.Tasks.ThreadManager.useMultithreading;
				serializedMaxThreads = Den.Tools.Tasks.ThreadManager.maxThreads;
				serializedAutoMaxThreads = Den.Tools.Tasks.ThreadManager.autoMaxThreads;
				serializedMaxApplyTime = Den.Tools.Tasks.CoroutineManager.timePerFrame;
			}

			public virtual void OnAfterDeserialize ()  
			{  
				Den.Tools.Tasks.ThreadManager.useMultithreading = serializedMultithreading;
				Den.Tools.Tasks.ThreadManager.maxThreads = serializedMaxThreads;
				Den.Tools.Tasks.ThreadManager.autoMaxThreads = serializedAutoMaxThreads;
				Den.Tools.Tasks.CoroutineManager.timePerFrame = serializedMaxApplyTime;
			}
		#endregion

	}


	[Serializable]
	public class Globals
	{
		public Globals Clone() => this.MemberwiseClone() as Globals;

		public float height = 250;
		public Nodes.MatrixGenerators.HeightOutput200.Interpolation heightInterpolation = Nodes.MatrixGenerators.HeightOutput200.Interpolation.None;
		public int heightSplit = 129;
		#if UNITY_2019_1_OR_NEWER
		public Nodes.MatrixGenerators.HeightOutput200.ApplyType heightMainApply = Nodes.MatrixGenerators.HeightOutput200.ApplyType.TextureToHeightmap;
		#else
		public Nodes.MatrixGenerators.HeightOutput200.ApplyType heightMainApply = Nodes.MatrixGenerators.HeightOutput200.ApplyType.SetHeightsDelayLOD;
		#endif
		public Nodes.MatrixGenerators.HeightOutput200.ApplyType heightDraftApply = Nodes.MatrixGenerators.HeightOutput200.ApplyType.SetHeights;
		public int grassResDownscale = 1;
		public int grassResPerPatch = 16;
		public int objectsNumPerFrame = 500;

		//public Material microSplatMaterial; //using terrain mat instead

		public string[] customControlTextureNames = new string[] { "_ControlTexture1" };
        public UnityEngine.Object ctsProfile; 
		public UnityEngine.Object microSplatPropData;
		public bool microSplatTerrainDescriptor = false;
		public enum MicroSplatApplyType { Textures, Splats, Both }; //not flag for editor selection
		public MicroSplatApplyType microSplatApplyType = MicroSplatApplyType.Textures;
		public bool useCustomControlTextures = false;
		public bool microSplatNormals = false;
		public UnityEngine.Object megaSplatTexList;
		public UnityEngine.Object vegetationPackage;
		public bool assignComponent; //assign ms/ms/cts component to terrain

		public UnityEngine.Object vegetationSystem;
		public bool vegetationSystemCopy = true;
		public string sourceMultiPackageVsProTagName; //for compatibility with MultiPackage
	}
}