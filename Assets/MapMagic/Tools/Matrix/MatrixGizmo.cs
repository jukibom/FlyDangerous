using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.GUI;


namespace Den.Tools.Matrices
{
	public class MatrixTextureGizmo : DebugGizmos.IGizmo
	{
		private Mesh mesh;
		private Texture2D texture;
		private byte[] bytes;
		private Material material;
		private Matrix4x4 tfm;

		private static readonly Vector3[] verts = new Vector3[] { new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(1,0,0), new Vector3(1,0,1) };
		private static readonly Vector2[] uvs = new Vector2[] { new Vector3(0,0), new Vector3(0,1), new Vector3(1,0), new Vector3(1,1)  };
		private static readonly int[] tris = new int[] { 1, 2, 0,  2, 1, 3 };

		public void SetMatrix (Matrix matrix, bool centerCell=false, FilterMode filterMode=FilterMode.Point)
		{
			//generating preview texture 
			if (texture == null || texture.width != matrix.rect.size.x || texture.height != matrix.rect.size.z)
				texture = new Texture2D(matrix.rect.size.x, matrix.rect.size.z, TextureFormat.RFloat, false, true);
			texture.filterMode = filterMode;

			matrix.ExportTextureRaw(texture);

			if (mesh==null) mesh = new Mesh();
			mesh.Clear();

			if (centerCell)
			{
				Vector2 pixelSize = new Vector2(1f/matrix.rect.size.x, 1f/matrix.rect.size.z);

				mesh.vertices = verts;

				Vector2[] muvs = uvs.Copy();
				for (int i=0; i<muvs.Length; i++)
					muvs[i] -= muvs[i]*pixelSize - pixelSize/2;
				mesh.uv = muvs;

				mesh.triangles = tris;
			}
			else
			{
				mesh.vertices = verts;
				mesh.uv = uvs;
				mesh.triangles = tris;
			}

			//material
			if (material==null) material = new Material(Shader.Find("Hidden/MapMagic/TexturePreview"));
			material.SetTexture("_MainTex", texture);
			material.SetInt("_Margins", 0);
		}


		public void SetOffsetSize (Vector2D worldOffset, Vector2D worldSize)
		{
			tfm = Matrix4x4.TRS((Vector3)worldOffset, Quaternion.identity, (Vector3)worldSize);
		}


		public void SetMatrixWorld (MatrixWorld matrix, bool centerCell=false, FilterMode filterMode=FilterMode.Point)
		{
			SetMatrix(matrix, centerCell:centerCell, filterMode:filterMode);
			SetOffsetSize((Vector2D)matrix.worldPos, (Vector2D)matrix.worldSize);
		}


		public void Draw () => Draw(colorize:false, relief:false, min:0, max:1, parent:null);
		public void Draw (
			bool colorize=false, bool relief=false, 
			float min=0, float max=1,
			Transform parent=null)
		{
			if (material==null || mesh==null) return;

			material.SetFloat("_Colorize", colorize ? 1 : 0);
			material.SetFloat("_Relief", relief ? 1 : 0);
			material.SetFloat("_MinValue", min);
			material.SetFloat("_MaxValue", max);

			material.SetPass(0);
			Graphics.DrawMeshNow(mesh, tfm);
		}


		public static void DrawNow (Matrix matrix, Vector2D worldOffset, Vector2D worldSize)
		{
			MatrixTextureGizmo gizmo = new MatrixTextureGizmo();
			gizmo.SetMatrix(matrix);
			gizmo.SetOffsetSize(worldOffset, worldSize);
			gizmo.Draw();
		}

		public Color Color { get{return Color.white;} set{} }

	}


	public class MatrixHeightGizmo
	{
		private Mesh mesh;
		//private Vector3[] vertices;
		//private Vector2[] uv;
		//private int[] tris;
		private Material material;
		private Matrix4x4 tfm;

		public enum ZMode { Occluded, Overlay, Both };


		public void SetMatrix (Matrix matrix, bool faceted=false)// bool centerCell=true)
		//centerCell: offsetting half-pixel to make verts be placed at grid cells centers
		{
			(Vector3[] verts, Vector2[] uvs, int[] tris) = MakePlane(matrix.rect.size.x-1); 
				//MakePlane takes resolution as a number of planes, not vertices

			for (int x=0; x<matrix.rect.size.x; x++)
				for (int z=0; z<matrix.rect.size.z; z++)
				{
					int pos = z*matrix.rect.size.x + x;
					verts[pos].y = matrix.arr[pos];
				}

			
			/*if (centerCell)
			{
				OffsetScale(verts,
					new Vector2D(1f/matrix.rect.size.x * 0.5f, 1f/matrix.rect.size.z * 0.5f),
					new Vector2D((float)(matrix.rect.size.x-1)/matrix.rect.size.x, (float)(matrix.rect.size.z-1)/matrix.rect.size.z) );
			}*/

			if (faceted)
				SplitFaceted(ref verts, ref tris);

			//mesh
			if (mesh == null) { mesh = new Mesh(); mesh.MarkDynamic(); }
			
			mesh.vertices = verts;
//			mesh.uv = uv;
			mesh.triangles = tris;
			mesh.RecalculateNormals();

			//material
			if (material==null) 
			{
				material = new Material(Shader.Find("Standard"));
				material.SetColor("_Color", Color.gray);
			}
		}


		public void SetMatrixWorld (MatrixWorld matrix, bool faceted=false)//, bool centerCell=true)
		{
			SetMatrix(matrix, faceted);//, centerCell);
			SetOffsetSize(matrix.worldPos, matrix.worldSize);
		}


		private static (Vector3[] verts, Vector2[] uvs, int[] tris) MakePlane (int resolution) 
		/// Fills verts,tris,uv with a plane data within 0-1 coordinate
		{
			float step = 1f / resolution;
		
			Vector3[] verts = new Vector3[(resolution+1)*(resolution+1)];
			Vector2[] uv = new Vector2[verts.Length];
			int[] tris = new int[resolution*resolution*2*3];

			int vertCounter = 0;
			int triCounter = 0;
			for (float x=0; x<1.00001f; x+=step) //including max
				for (float z=0; z<1.00001f; z+=step)
			{
				verts[vertCounter] = new Vector3(z, 0, x);
				uv[vertCounter] = new Vector2(z, x);
			
				if (x>0.00001f && z>0.00001f)
				{
					tris[triCounter] = vertCounter-(resolution+1);		tris[triCounter+1] = vertCounter-resolution-2;	tris[triCounter+2] = vertCounter-1;	
					tris[triCounter+3] = vertCounter-1;					tris[triCounter+4] = vertCounter;				tris[triCounter+5] = vertCounter-(resolution+1);	
					triCounter += 6;
				}

				vertCounter++;
			}

			return (verts, uv, tris);
		}


		private void OffsetScale (Vector3[] verts, Vector2D offset, Vector2D scale)
		{
			for (int i=0; i<verts.Length; i++)
				verts[i] = verts[i]*scale + offset;
		}


		private void SplitFaceted (ref Vector3[] verts, ref int[] tris)
		/// tris count won't change, marking it as ref for company
		{
			Vector3[] newVerts = new Vector3[(tris.Length/6)*4];

			int vertCounter = 0;
			for (int t=0; t<tris.Length; t+=6)
			{
				newVerts[vertCounter] = verts[tris[t]];
				newVerts[vertCounter+1] = verts[tris[t+1]];
				newVerts[vertCounter+2] = verts[tris[t+2]];
				newVerts[vertCounter+3] = verts[tris[t+4]];

				tris[t] = vertCounter;		tris[t+1] = vertCounter+1;		tris[t+2] = vertCounter+2;
				tris[t+3] = vertCounter+2;	tris[t+4] = vertCounter+3;		tris[t+5] = vertCounter;

				vertCounter+=4;
			}

			verts = newVerts;
		}



		public void SetOffsetSize (Vector3 worldOffset, Vector3 worldSize)
		{
			tfm = Matrix4x4.TRS(worldOffset, Quaternion.identity, worldSize);
		}


		public void Draw (
			Material material=null,
			Transform parent=null)
		{
			Material currMat = material ?? this.material;
			currMat.SetPass(0);
			Graphics.DrawMeshNow(mesh, tfm);
		}


		public static void DrawNow (Matrix matrix, Vector3 worldOffset, Vector3 worldSize)
		{
			MatrixHeightGizmo gizmo = new MatrixHeightGizmo();
			gizmo.SetMatrix(matrix);
			gizmo.SetOffsetSize(worldOffset, worldSize);
			gizmo.Draw();
		}
	}
}
