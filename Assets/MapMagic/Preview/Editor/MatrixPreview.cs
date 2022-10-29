using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Den.Tools;
using Den.Tools.Matrices;
//using Den.Tools.Segs;
using Den.Tools.Tasks;
using Den.Tools.GUI;
using Den.Tools.Matrices.Window;

using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Terrains;

using MapMagic.Nodes.GUI;

namespace MapMagic.Previews
{
	public class MatrixPreview : IPreview
	{
		public bool colorized = true;
		public bool relief = true;

		public MatrixWorld matrix = null;  //to pass to thread
		public CoordRect activeRect;
		private int margins;
		private byte[] bytes = null;  //to pass from thread to apply
		public Texture2D tex = null;  //used by Preview.height
		
		public PreviewStage Stage { get; set; } = PreviewStage.Blank;

		private static Material guiMat;
		private static Material terrainMat;

		public BaseMatrixWindow Window { get; set; } 

		private Terrain terrain;
		private Terrain draftTerrain;
		public Terrain Terrain => terrain;


		#region Generate

			public void Clear ()
			{
				Stage = PreviewStage.Blank;
				Window?.Repaint();
			}

			public void SetObject (IOutlet<object> outlet, TileData data) 
			{ 
				if (data != null)
					SetObject((MatrixWorld)data.ReadOutletProduct(outlet), data.area); 
			}


			public void SetObject (MatrixWorld matrix, Area area)
			{
				this.matrix = matrix;
				this.activeRect = area.active.rect;
				this.margins = area.Margins;

				Stage = PreviewStage.Generating;

				ThreadManager.Enqueue(ExecuteInThread, priority:-1000);
			}


			public void ExecuteInThread ()
			{
				if (matrix == null) { Stage=PreviewStage.Blank; return; }

				bytes = new byte[matrix.rect.Count*2];
				matrix.ExportRaw16(bytes, matrix.rect.offset, matrix.rect.size);

				CoroutineManager.Enqueue(ApplyInMain, priority:-1000);
			}


			public void ApplyInMain ()
			{
				if (bytes==null) return;

				int textureSize = (int)Mathf.Sqrt(bytes.Length / 2);

				if (tex==null || tex.width != textureSize || tex.height != textureSize)
					tex = new Texture2D(textureSize, textureSize, TextureFormat.R16, false, true);

				tex.LoadRawTextureData(bytes);
				tex.Apply();

				Stage = PreviewStage.Ready;
				Window?.Repaint();
			}

		#endregion

		#region Terrain

			public void ToTerrain (Terrain terrain, Terrain draftTerrain)
			{
				this.terrain = terrain;
				this.draftTerrain = draftTerrain;

				terrain.drawHeightmap = false;
				if (draftTerrain != null) draftTerrain.drawHeightmap = false;

				Terrain substituteTerrain = GetSubstituteTerrain(terrain);
				if (substituteTerrain == null) substituteTerrain = CreateSubstituteTerrain(terrain);

				substituteTerrain.terrainData = terrain.terrainData; //in case was previewing other terrain

				if (terrainMat == null) 
				{
					Shader shader;
					#if UNITY_2019_2_OR_NEWER
					Material defaultTerrainMat = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset?.defaultTerrainMaterial;
					if (defaultTerrainMat != null  &&  defaultTerrainMat.shader.name.Contains("Universal Render Pipeline"))
						shader = Shader.Find("MapMagic/TerrainPreviewURP");
					else if (defaultTerrainMat != null  &&  defaultTerrainMat.shader.name.Contains("HDRP"))
						shader = Shader.Find("MapMagic/TerrainPreviewHDRP"); 
					else
					#endif
						shader = Shader.Find("MapMagic/TerrainPreview");

					terrainMat = new Material(shader);
				}
				#if !UNITY_2019_2_OR_NEWER
				substituteTerrain.materialType = Terrain.MaterialType.Custom;
				#endif

				substituteTerrain.materialTemplate = terrainMat; 

				substituteTerrain.basemapDistance = 100000; //disabling base map since SRP preview shader does not support it

				terrainMat.SetTexture("_Preview", tex); 
				terrainMat.SetInt("_Margins", margins);
			}


			public void ClearTerrain ()
			{
				if (terrain == null) return;

				terrain.drawHeightmap = true;
				if (draftTerrain != null) draftTerrain.drawHeightmap = true;

				Terrain substituteTerrain = GetSubstituteTerrain(terrain);

				if (substituteTerrain != null)
				{
					Material previewMaterial = substituteTerrain.materialTemplate;
					if (previewMaterial != null) GameObject.DestroyImmediate(previewMaterial);

					GameObject.DestroyImmediate(substituteTerrain.gameObject);
				}

				terrain = null;
			}


			private static Terrain GetSubstituteTerrain (Terrain terrain)
			{
				Transform tileTfm = terrain.transform.parent;
				Transform previewTfm = tileTfm.Find("Preview Terrain");
				if (previewTfm == null) return null;
				return previewTfm.GetComponent<Terrain>();
			}


			private static Terrain CreateSubstituteTerrain (Terrain terrain)
			{
				GameObject go = new GameObject();  
				go.name = "Preview Terrain";
				go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;   
				go.tag = "EditorOnly"; 
				go.transform.parent = terrain.transform.parent;
				go.transform.localPosition = Vector3.zero;
				Terrain previewTerrain = go.AddComponent<Terrain>();
				previewTerrain.terrainData = terrain.terrainData; //it will be assigned once more after in case using of existing terrain

			//	StopPreviewOnLoad stopScript = go.AddComponent<StopPreviewOnLoad>();
			//	stopScript.active = true; 

				previewTerrain.drawInstanced = terrain.drawInstanced; //set only if terrain data assigned
				previewTerrain.heightmapPixelError = terrain.heightmapPixelError;
				previewTerrain.drawTreesAndFoliage = false;
				#if UNITY_2019_1_OR_NEWER
				previewTerrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				#else
				previewTerrain.castShadows = false;
				#endif

				return previewTerrain;
			}


			[RuntimeInitializeOnLoadMethod, UnityEditor.InitializeOnLoadMethod] 
			static void ClearTerrainsOnLoad ()
			{
				Terrain[] terrains = Resources.FindObjectsOfTypeAll<Terrain>(); //will return HideFlags.DontSave objects
				for (int i=terrains.Length-1; i>=0; i--) 
				{
					Terrain terrain = terrains[i];
					if (AssetDatabase.Contains(terrain) || AssetDatabase.Contains(terrain.gameObject)) continue;
						//FindObjectsOfTypeAll will return assets too

					//removing preview terrain (if left after script re-compile)
					if (terrain.gameObject.name == "Preview Terrain"  &&
						terrain.transform.parent != null  &&  
						terrain.transform.parent.GetComponent<TerrainTile>() != null)
							{ GameObject.DestroyImmediate(terrain.gameObject); continue; }

					//enabling drawHeightmap to all MM terrains
					if (!terrain.drawHeightmap  &&  
						terrain.transform.parent != null  &&  
						terrain.transform.parent.GetComponent<TerrainTile>() != null)
							terrain.drawHeightmap = true;  
				}
			}

		#endregion

		#region Window

			public BaseMatrixWindow CreateWindow ()
			{
				PreviewWindow window = ScriptableObject.CreateInstance<PreviewWindow>();

				window.plugins = new IPlugin[] { 
					new StatsPlugin(), 
					new ViewPlugin(),
					new PixelPlugin(),
					new SlicePlugin(),
					new ExportPlugin() {margins=margins} };
				window.preview = this;
				window.colorize = true;
				window.relief = true;
				window.name = "Map Preview";

				return window;
			}

			[System.Serializable]
			public class PreviewWindow : BaseMatrixWindow, IPreviewWindow
			{ 
				public IPreview Preview { get{return preview;} set{preview = value as MatrixPreview;} }
				public MatrixPreview preview;

				public override Matrix Matrix => preview?.matrix;
				public override Texture2D PreviewTexture => preview.tex;

				public ulong SerializedGenId 
				{ 
					get{ return serializedGenId; }
					set{ serializedGenId = value; }
				}
				public ulong serializedGenId; 


				protected override void DrawPreview ()
				{
					base.DrawPreview();

					PreviewDraw.DrawGenerateMarkInWindow(preview.Stage, MatrixRect.center);
				}
			}

		#endregion

		#region GUI

			public void DrawInGraph ()
			{
				if (tex == null) Draw.Rect(PreviewDraw.BackgroundColor);
				else 
				{
					Draw.MatrixPreviewTexture(tex, colorized, relief, margins:margins);
					Draw.MatrixPreviewReliefSwitch(ref colorized, ref relief);
				}
			}

		#endregion
	}
}
