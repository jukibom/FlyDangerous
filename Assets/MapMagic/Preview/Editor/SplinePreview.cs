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
//using Den.Tools.Segs;
using Den.Tools.Splines;
using Den.Tools.Tasks;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Terrains;

using MapMagic.Nodes.GUI;

namespace MapMagic.Previews
{
	public class SplinePreview : IPreview
	{
		[NonSerialized] public SplineSys splineSys = null;  //to pass to thread
		[NonSerialized] public PolyLine polyLine;  //in thread and in apply

		[NonSerialized] public Mesh nodesMesh;
		[NonSerialized] public Vector3[] nodesMeshVerts;
		[NonSerialized] public Vector2[] nodesMeshUvs;
		[NonSerialized] public int[] nodesMeshTris;

		public Vector2D worldPos;
		public Vector2D worldSize;
		public float worldHeight;

		public PreviewStage Stage { get; set; } = PreviewStage.Blank;
		public BaseMatrixWindow Window { get; set; } 
		public Terrain Terrain { get; set; }

		private static Material heightTexMat;
		private static Material lineMat;
		private static Material terrainLineMat;
		private static Material terrainNodesMat;
		private static Texture2D lineTex;

		public static readonly Color guiGizmoColor = new Color(1f,0.5f,0,1);

		#region Generate

			public void Clear ()
			{
				Stage = PreviewStage.Blank;
				Window?.Repaint();
			}

			public void SetObject (IOutlet<object> outlet, TileData data) 
			{ 
				if (data == null) return;

				this.splineSys = (SplineSys)data.ReadOutletProduct(outlet); 
				this.worldPos = data.area.active.worldPos;    
				this.worldSize = data.area.active.worldSize;
				this.worldHeight = data.globals.height;

				Stage = PreviewStage.Generating;
				ThreadManager.Enqueue(ExecuteInThread, priority:-1000);
			}


			public void ExecuteInThread ()
			{
				if (splineSys == null) { Stage = PreviewStage.Blank; return; }

				//auto-line
				/*Vector3[][] nodes = new Vector3[splineSys.splines.Length][];
				for (int s=0; s<splineSys.splines.Length; s++)
					nodes[s] = splineSys.splines[s].nodes;

				if (polyLine == null) polyLine = new PolyLine(0);
				polyLine.SetPointsThread(nodes);*/
				
				//line
				Vector3[][] points = splineSys.GetAllPoints(resPerUnit:0.2f, minRes:3, maxRes:20);
				
				for (int l=0; l<points.Length; l++)
					for (int p=0; p<points[l].Length; p++)
						points[l][p] = new Vector3(points[l][p].x/worldSize.x, points[l][p].y/worldHeight, points[l][p].z/worldSize.z);

				if (polyLine == null) polyLine = new PolyLine(0);
				polyLine.SetPointsThread(points); 

				//nodes
				PrepareNodesMeshArrays();

				CoroutineManager.Enqueue(ApplyInMain, priority:-1000);
			}


			private void PrepareNodesMeshArrays ()
			{
				/*auto-line
				int nodesNum = 0;
				for (int s=0; s<splineSys.splines.Length; s++)
					nodesNum += splineSys.splines[s].nodes.Length;
				
				//vertices
				if (nodesMeshVerts == null  ||  nodesMeshVerts.Length != nodesNum*4)
					nodesMeshVerts = new Vector3[nodesNum*4];

				int i = 0;
				for (int s=0; s<splineSys.splines.Length; s++)
					for (int n=0; n<splineSys.splines[s].nodes.Length; n++)
					{
						Vector3 pos = splineSys.splines[s].nodes[n];
						pos = new Vector3(pos.x/worldSize.x, pos.y/worldHeight, pos.z/worldSize.z);

						for (int v=0; v<4; v++)
							nodesMeshVerts[i*4+v] = pos;
						i++;
					} */

				int nodesNum = splineSys.NodesCount;

				//vertices
				if (nodesMeshVerts == null  ||  nodesMeshVerts.Length != nodesNum*4)
					nodesMeshVerts = new Vector3[nodesNum*4];

				int i = 0;
				for (int l=0; l<splineSys.lines.Length; l++)
					for (int n=0; n<splineSys.lines[l].NodesCount; n++)
					{
						Vector3 pos = splineSys.lines[l].GetNodePos(n);
						pos = new Vector3(pos.x/worldSize.x, pos.y/worldHeight, pos.z/worldSize.z);

						for (int v=0; v<4; v++)
							nodesMeshVerts[i*4+v] = pos;
						i++;
					}

				//uvs
				if (nodesMeshUvs == null  ||  nodesMeshUvs.Length != nodesNum*4)
				{
					nodesMeshUvs = new Vector2[nodesNum*4];
					
					for (i=0; i<nodesNum; i++)
					{
						int v = i*4;
						nodesMeshUvs[v] = new Vector2(-1,-1);
						nodesMeshUvs[v+1] = new Vector2(-1,1);
						nodesMeshUvs[v+2] = new Vector2(1,1);
						nodesMeshUvs[v+3] = new Vector2(1,-1);
					}
				}

				//tris
				if (nodesMeshTris == null  ||  nodesMeshTris.Length != nodesNum*6)
				{
					nodesMeshTris = new int[nodesNum*6];

					for (i=0; i<nodesNum; i++)
					{
						int v = i*4;
						int t = i*6;

						nodesMeshTris[t] = v+0;
						nodesMeshTris[t+1] = v+2;
						nodesMeshTris[t+2] = v+3;

						nodesMeshTris[t+3] = v+0;
						nodesMeshTris[t+4] = v+1;
						nodesMeshTris[t+5] = v+2;
					}
				}

			}


			public void ApplyInMain ()
			{
				if (polyLine == null) return;

				polyLine.SetPointsApply();

				if (nodesMesh == null) { nodesMesh = new Mesh(); nodesMesh.MarkDynamic(); }

				if (nodesMeshVerts.Length != nodesMesh.vertexCount) 
				{
					if (nodesMeshVerts.Length < nodesMesh.vertexCount) //resetting uv and tris to avoid "Mesh.vertices is too small" error
					{
						nodesMesh.uv = new Vector2[0];
						nodesMesh.triangles = new int[0];
					}
					nodesMesh.vertices = nodesMeshVerts;
					nodesMesh.uv = nodesMeshUvs;
					nodesMesh.triangles = nodesMeshTris;
				}
				else
					nodesMesh.vertices = nodesMeshVerts; //just changing verts

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
				if (polyLine == null || Terrain == null) return;

				Vector3 position = Terrain.transform.position;
				Vector3 size = Terrain.terrainData.size;

				Matrix4x4 prs = Matrix4x4.TRS(
					position, 
					Quaternion.identity,  
					size );

				//line mesh
				if (lineTex == null) lineTex = Resources.Load("DPUI/PolyLineTex") as Texture2D; 
				if (terrainLineMat == null) 
				{
					terrainLineMat = new Material( Shader.Find("Hidden/DPLayout/PolyLine") ); 

					terrainLineMat.SetTexture("_MainTex", lineTex);
					terrainLineMat.SetColor("_Color", guiGizmoColor);
					terrainLineMat.SetFloat("_Width", 5);
					terrainLineMat.SetFloat("_Offset", 0.01f);
					//terrainLineMat.SetInt("_ZTest", 1);

					#if MM_DOC
					terrainLineMat.SetFloat("_Width", 10);
					#endif
				}

				terrainLineMat.SetPass(0);
				Graphics.DrawMeshNow(polyLine.mesh, prs);

				//nodes mesh
				if (terrainNodesMat == null) 
				{
					terrainNodesMat = new Material( Shader.Find("Hidden/DPLayout/PolyLineBillboards") ); 
					terrainNodesMat.SetColor("_Color", guiGizmoColor);
					terrainNodesMat.SetFloat("_Size", 4);
					terrainNodesMat.SetFloat("_Offset", 0.02f);
					//terrainNodesMat.SetInt("_ZTest", 1);

					#if MM_DOC
					terrainNodesMat.SetFloat("_Size", 6); //4
					#endif
				}

				terrainNodesMat.SetPass(0);
				Graphics.DrawMeshNow(nodesMesh, prs);
			}

		#endregion

		#region Window

			public BaseMatrixWindow CreateWindow ()
			{
				SplinePreviewWindow window = ScriptableObject.CreateInstance<SplinePreviewWindow>();

				window.plugins = new IPlugin[] { 
					new ViewPlugin() };
				window.preview = this;
				window.colorize = false;
				window.relief = true;
				window.name = "Spline Preview";

				return window;
			}

			public class SplinePreviewWindow : BaseMatrixWindow, IPreviewWindow
			{
				public IPreview Preview { get{return preview;} set{preview = value as SplinePreview;} }
				public SplinePreview preview;

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

					if (preview.polyLine?.mesh == null) 
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
						if (lineTex == null) lineTex = Resources.Load("DPUI/PolyLineTex") as Texture2D; 
						if (lineMat == null) 
						{
							lineMat = new Material( Shader.Find("Hidden/DPLayout/PolyLine") ); 

							lineMat.SetTexture("_MainTex", lineTex);
							lineMat.SetColor("_Color", guiGizmoColor);
							lineMat.SetFloat("_Width", 1.5f);
							//lineMat.SetFloat("_Offset", offset);
							//lineMat.SetFloat("_NumPoints", numPoints-1);
							//lineMat.SetFloat("_Dotted", dottedSpace);
						}

						Draw.Mesh(preview.polyLine.mesh, lineMat, clip:false);
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
				if (splineSys == null  ||  polyLine == null  ||  polyLine.mesh == null) { Draw.Rect(PreviewDraw.BackgroundColor); return; }

				//height background
				Texture2D heightTex = PreviewManager.heightOutputPreview!=null ? PreviewManager.heightOutputPreview.tex : UI.current.textures.GetBlankTexture(0);
				Draw.MatrixPreviewTexture(heightTex, colorize:false, relief:true);

				//preview itself
				if (Event.current.type != EventType.Repaint) return;

				if (lineTex == null) lineTex = Resources.Load("DPUI/PolyLineTex") as Texture2D; 
				if (lineMat == null) 
				{
					lineMat = new Material( Shader.Find("Hidden/DPLayout/PolyLine") ); 

					lineMat.SetTexture("_MainTex", lineTex);
					lineMat.SetColor("_Color", guiGizmoColor);
					lineMat.SetFloat("_Width", 1.5f);
					//lineMat.SetFloat("_Offset", offset);
					//lineMat.SetFloat("_NumPoints", numPoints-1);
					//lineMat.SetFloat("_Dotted", dottedSpace);
				}

				Draw.Mesh(polyLine.mesh, lineMat);

				//DrawBeizer();
			}


		#endregion
	}
}
