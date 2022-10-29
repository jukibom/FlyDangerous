using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.Matrices.Window;
using Den.Tools.Tasks;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Terrains;

using MapMagic.Nodes.GUI;

namespace MapMagic.Previews
{
	public class ObjectsPreview : IPreview
	{
		//temporary objects to pass to thread
		public TransitionsList trns = null; 
		public MatrixWorld heights = null;
		public Vector3 worldPos;
		public Vector3 worldSize;
		public int count; //number of obs within worldPos/worldSize rect

		//mesh data passing from thread to main
		private Vector3[] vertices; //relative coords from 0 to 1
		private Vector3[] directions; //Y-types directions to create a triangle in shader
		private int[] tris;

		private Mesh mesh;

		public bool relativeHeight = true;
		private static Material heightTexMat;
		private static Material meshMat;
		private static Material terrainMat; //same shader as meshMat, but has rect and heightmap assigned (and bigger size)

		public PreviewStage Stage { get; set; } = PreviewStage.Blank;
		public BaseMatrixWindow Window { get; set; } 
		public Terrain Terrain { get; set; }

		public static readonly Color guiGizmoColor = new Color(0.3f,1,0,1); 


		#region Generate

			public void Clear ()
			{
				Stage = PreviewStage.Blank;
				Window?.Repaint();
			}

			public void SetObject (IOutlet<object> outlet, TileData data) 
			{ 
				if (data != null)
					SetObject((TransitionsList)data.ReadOutletProduct(outlet), data.heights, data.area); 
			}


			public void SetObject (TransitionsList posTab, MatrixWorld heights, Area area)
			{
				this.trns = posTab;
				this.heights = heights;
				this.worldPos = (Vector3)area.active.worldPos;    
				this.worldSize = (Vector3)area.active.worldSize;

				Stage = PreviewStage.Generating;
				ThreadManager.Enqueue(ExecuteInThread, priority:-1000);
			}


			public void ExecuteInThread ()
			{
				if (trns == null) { Stage = PreviewStage.Blank; return; }

				count = trns.CountInRect((Vector2D)worldPos, (Vector2D)worldSize);

				vertices = new Vector3[trns.count * 6];
				directions = new Vector3[trns.count * 6];
				tris = new int[trns.count * 6];

				int i = 0;
				for (int t=0; t<trns.count; t++)
				{
					Vector3 pos = trns.arr[t].pos;
					pos -= worldPos;
					pos = new Vector3 (pos.x/worldSize.x, pos.y, pos.z/worldSize.z);
				//	if (heights != null) pos.y = pos.y/heights.worldSize.y;
					pos.y = 0;

					vertices[i] = vertices[i+1] = vertices[i+2] = vertices[i+3] = vertices[i+4] = vertices[i+5] = pos;

					directions[i]   = new Vector3(0, 2, 1); //background has Z of 1
					directions[i+1] = new Vector3(-4.5f, -7, 1);
					directions[i+2] = new Vector3(4.5f, -7, 1);

					directions[i+3] = new Vector3(0, 0, 0);
					directions[i+4] = new Vector3(-3f, -6, 0);
					directions[i+5] = new Vector3(3f, -6, 0);

					tris[i] = i;
					tris[i+1] = i+1;
					tris[i+2] = i+2;
					tris[i+3] = i+3;
					tris[i+4] = i+4;
					tris[i+5] = i+5;

					i+=6;
				}

				CoroutineManager.Enqueue(ApplyInMain, priority:-1000);
			}


			public void ApplyInMain ()
			{
				if (vertices==null || tris==null) return;

				if (mesh == null) { mesh = new Mesh(); mesh.MarkDynamic(); }
				if (vertices.Length < mesh.vertices.Length) mesh.triangles = new int[0]; //otherwise "The supplied vertex array has less vertices than are referenced by the triangles array."
				mesh.vertices = vertices;
				mesh.normals = directions;
				mesh.triangles = tris;

				Stage = PreviewStage.Ready;
				Window?.Repaint();
			}

		#endregion

		#region Terrain

			public void ToTerrain (Terrain terrain, Terrain draftTerrain)
			{
				#if UNITY_2019_1_OR_NEWER
				SceneView.duringSceneGui -= DrawTerrainPreview;
				SceneView.duringSceneGui += DrawTerrainPreview;
				#else
				SceneView.onSceneGUIDelegate -= DrawTerrainPreview;
				SceneView.onSceneGUIDelegate += DrawTerrainPreview;
				#endif

				Terrain = terrain;
			}


			public void ClearTerrain ()
			{
				#if UNITY_2019_1_OR_NEWER
				SceneView.duringSceneGui -= DrawTerrainPreview;
				#else
				SceneView.onSceneGUIDelegate -= DrawTerrainPreview;
				#endif

				Terrain = null;
			}

			public void DrawTerrainPreview (SceneView sceneView) => DrawTerrainPreview();
			public void DrawTerrainPreview ()
			{
				if (mesh == null || Terrain == null) return;

				if (terrainMat == null)
				{
					terrainMat = new Material( Shader.Find("MapMagic/ObjectPreview") );
					terrainMat.SetColor("_Color", ObjectsPreview.guiGizmoColor);
					terrainMat.SetColor("_BackColor", new Color(0,0,0,1));
					terrainMat.SetFloat("_Size", 1.42f);
					terrainMat.SetFloat("_Flip", 0);
				}

				if (PreviewManager.heightOutputPreview?.tex != null)
					terrainMat.SetTexture("_Heightmap", PreviewManager.heightOutputPreview.tex);

				Vector3 position = Terrain.transform.position;
				Vector3 size = Terrain.terrainData.size;

				Matrix4x4 prs = Matrix4x4.TRS(
					position, 
					Quaternion.identity,  
					size );

				if (terrainMat.HasProperty("_Rect"))
					terrainMat.SetVector("_Rect", new Vector4(position.x, position.y, size.x, size.y)); 
					//not _ClipRect, since mesh is drawn in 0-1 range clipping it in shader

				terrainMat.SetPass(0);
				Graphics.DrawMeshNow(mesh, prs); 
			}

		#endregion

		#region Window

			public BaseMatrixWindow CreateWindow ()
			{
				ObjectsPreviewWindow window = ScriptableObject.CreateInstance<ObjectsPreviewWindow>();

				window.plugins = new IPlugin[] { 
					new ViewPlugin() };
				window.preview = this;
				window.colorize = false;
				window.relief = true;
				window.name = "Object Preview";

				return window;
			}

			public class ObjectsPreviewWindow : BaseMatrixWindow, IPreviewWindow
			{
				public IPreview Preview { get{return preview;} set{preview = value as ObjectsPreview;} }
				public ObjectsPreview preview;

				public override Matrix Matrix => PreviewManager.heightOutputPreview?.matrix;
				public override Texture2D PreviewTexture => PreviewManager.heightOutputPreview?.tex;

				public ulong SerializedGenId 
				{ 
					get{ return serializedGenId; }
					set{ serializedGenId = value; }
				}
				public ulong serializedGenId; 


				protected override void DrawPreview ()
				{
					base.DrawPreview();

					if (preview.mesh == null) 
					{
						PreviewDraw.DrawGenerateMarkInWindow(PreviewStage.Blank, Vector2.zero);
						return;
					}

					CoordRect matrixRect = PreviewManager.heightOutputPreview?.matrix!=null ? 
						PreviewManager.heightOutputPreview.matrix.rect : 
						new CoordRect(0,0,0,0);
					CoordRect activeRect = PreviewManager.heightOutputPreview?.matrix!=null ? 
						PreviewManager.heightOutputPreview.activeRect : 
						new CoordRect(0,0,0,0);

					//height background
					using (Cell.Custom( ToMatrixRect(matrixRect) ))
					{
						
						Texture2D heightTex = PreviewManager.heightOutputPreview!=null ? PreviewManager.heightOutputPreview.tex : UI.current.textures.GetBlankTexture(0);
						Draw.MatrixPreviewTexture(heightTex, colorize:false, relief:true);
					}

					//preview itself
					using (Cell.Custom( ToMatrixRect(activeRect) ))
					{
						if (meshMat == null)
						{
							meshMat = new Material( Shader.Find("MapMagic/ObjectPreview") );
							meshMat = UI.current.textures.GetMaterial("MapMagic/ObjectPreview");
							meshMat.SetColor("_Color", ObjectsPreview.guiGizmoColor);
							meshMat.SetColor("_BackColor", new Color(0,0,0,1));
							meshMat.SetFloat("_Size", 1.01f);
							meshMat.SetFloat("_Flip", 1);
						}

						Draw.Mesh(preview.mesh, meshMat, clip:false);
					}

					PreviewDraw.DrawGenerateMarkInWindow(preview.Stage, MatrixRect.center);
					if (PreviewManager.heightOutputPreview!=null) 
						PreviewDraw.DrawGenerateMarkInWindow(PreviewManager.heightOutputPreview.Stage, MatrixRect.center);
				}
			}

		#endregion

		#region GUI

			public void DrawInGraph ()
			{
				if (mesh == null) { Draw.Rect(PreviewDraw.BackgroundColor); return; }

				//height background
				Texture2D heightTex = PreviewManager.heightOutputPreview!=null ? PreviewManager.heightOutputPreview.tex : UI.current.textures.GetBlankTexture(0);
				Draw.MatrixPreviewTexture(heightTex, colorize:false, relief:true);

				//preview itself
				if (meshMat == null)
				{
					meshMat = new Material( Shader.Find("MapMagic/ObjectPreview") );
					meshMat = UI.current.textures.GetMaterial("MapMagic/ObjectPreview");
					meshMat.SetColor("_Color", ObjectsPreview.guiGizmoColor);
					meshMat.SetColor("_BackColor", new Color(0,0,0,1));
					meshMat.SetFloat("_Size", 1.01f);
					meshMat.SetFloat("_Flip", 1);
					meshMat.SetFloat("_Offset", 0);
				}

				Draw.Mesh(mesh, meshMat);

				//objects count
				using (Cell.Full)
				{
					Cell.EmptyLine();
					using (Cell.LineStd) Draw.BackgroundRightLabel(count.ToString(), style:UI.current.styles.whiteLabel, rightOffset:4);
				}

				//use heightmap
				/*using (Cell.Full)
				{
					Cell.EmptyRow();
					using (Cell.RowPx(12))
					{
						Cell.EmptyLinePx(3);
						using (Cell.LinePx(12))
						{
							Texture2D icon;
							if (relativeHeight) icon = UI.current.textures.GetTexture("DPUI/TexCh/Heightmap");
							else icon = UI.current.textures.GetTexture("DPUI/TexCh/Flat");

							if (Draw.Button(icon, visible:false)) relativeHeight = !relativeHeight;
						}
						Cell.EmptyLine();
					}
					Cell.EmptyRowPx(3);
				}*/
			}

		#endregion
	}
}
