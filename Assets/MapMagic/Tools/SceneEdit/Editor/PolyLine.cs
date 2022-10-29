using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

using Den.Tools.GUI;

namespace Den.Tools.GUI
{
	public class PolyLine
	{
		public Mesh mesh;
		private Vector3[] vertices;
		private Vector3[] neigCoords;
		private Vector2[] uv;
		private Vector2[] uv2; //point num and length
		private int[] tris;
		public int pointsCount; //number of visible points, assigned on SetPoints

		private static Material lineMat;
		private static Texture2D lineTex;

		public enum ZMode { Occluded, Overlay, Both };

		public int MaxPoints { get{ return vertices.Length/4-1; }}

		public PolyLine (int maxPoints)
		{
			//SetMesh(maxPoints);
		}


		public void SetPoints (Vector3[] points, int numPoints=-1)
		/// Shapes polyline before drawing it
		{
			SetPointsThread(points, numPoints);
			SetPointsApply();
		}


		public void SetPointsThread (Vector3[] points, int numPoints=-1)
			{ SetPointsThread( new Vector3[][] { points }, numPoints); }

		public void SetPointsThread (Vector3[][] points, int numPoints=-1)
		/// Prepares points in thread. SetPointsApply should be called after
		{


			if (numPoints < 0)
			{
				numPoints = 0;
				for (int s=0; s<points.Length; s++)
					numPoints += points[s].Length;
			}

			if (numPoints<=1) 
				throw new System.Exception("Line points number is <= 1");

			pointsCount = numPoints;
			//if (mesh == null) SetMesh(numPoints);
			//if (numPoints >= mesh.vertexCount)
			//	throw new System.Exception("Line points number is more than maximum allowed");

			//preparing arrays
			if (vertices == null  ||  vertices.Length != numPoints*4) vertices = new Vector3[numPoints*4];
			if (neigCoords == null  ||  neigCoords.Length != numPoints*4) neigCoords = new Vector3[numPoints*4];
			if (uv == null  ||  uv.Length != numPoints*4) uv = new Vector2[numPoints*4];  //uvs are always the same, re-calculate them only if count changed
			if (uv2 == null  ||  uv2.Length != numPoints*4) uv2 = new Vector2[numPoints*4];

			//filling arrays
			int v = 0;
			for (int s=0; s<points.Length; s++)
			{
				Vector3[] line = points[s];
				float totalLength = 0;

				for (int p=0; p<points[s].Length; p++)
				{
					Vector3 vert = line[p];
					vertices[v] = vert;
					vertices[v+1] = vert;// + new Vector3(0,0,1f);
					vertices[v+2] = vert;// + new Vector3(1f,0,1f);
					vertices[v+3] = vert;// + new Vector3(1f,0,0);

					Vector3 prev = p!=0 ? line[p-1] : line[0]-line[1];
					neigCoords[v] = prev;
					neigCoords[v+1] = prev;

					Vector3 next = p!=line.Length-1 ? line[p+1] : line[line.Length-1];
					neigCoords[v+2] = next;
					neigCoords[v+3] = next;

					uv[v] = new Vector2(-1,-1);
					uv[v+1] = new Vector2(-1,1);
					uv[v+2] = new Vector2(1,1);
					uv[v+3] = new Vector2(1,-1);

					//float segLength = (points[i-1] - points[i]).magnitude;  //using X and Z axis only
					float segLength = p!=0 ?
						Mathf.Sqrt( (line[p-1].x - line[p].x)*(line[p-1].x - line[p].x) + (line[p-1].z - line[p].z)*(line[p-1].z - line[p].z) ) :
						0;
					totalLength += segLength;

					uv2[v].x = p;	uv2[v].y = totalLength;
					uv2[v+1].x = p; uv2[v+1].y = totalLength;
					uv2[v+2].x = p; uv2[v+2].y = totalLength;
					uv2[v+3].x = p; uv2[v+3].y = totalLength;

					v += 4;
				}
			}

			//tris
			int numTris = 0;
			for (int s=0; s<points.Length; s++)
				numTris += ( (points[s].Length-1)*4 + 2 )*3;

			if (tris == null  ||  tris.Length != numTris)
				tris = new int[numTris];

			v = 0;
			int t = 0;
			for (int s=0; s<points.Length; s++)
			{
				Vector3[] line = points[s];

				for (int p=0; p<points[s].Length; p++)
				{
					tris[t] = v;
					tris[t+1] = v+2;
					tris[t+2] = v+1;

					tris[t+3] = v+1;
					tris[t+4] = v+2;
					tris[t+5] = v+3;

					if (p != points[s].Length-1)
					{
						tris[t+6] = v+2;
						tris[t+7] = v+4;
						tris[t+8] = v+3;

						tris[t+9] = v+3;
						tris[t+10] = v+4;
						tris[t+11] = v+5;
					}

					v += 4;
					t += p!=points[s].Length-1 ? 12 : 6;
				}
			}
		}


		public void SetPointsApply ()
		/// Applies SetPointsThread
		{
			if (mesh == null) { mesh = new Mesh(); mesh.MarkDynamic(); }
			if (vertices.Length < mesh.vertices.Length) mesh.triangles = new int[0]; //otherwise "The supplied vertex array has less vertices than are referenced by the triangles array."

			mesh.vertices = vertices;
			mesh.normals = neigCoords;
			mesh.uv = uv;
			mesh.uv2 = uv2;
			mesh.triangles = tris;
		}


		public void DrawLine (Color color, float width, float dottedSpace=0, float offset=0.01f, ZMode zMode=ZMode.Occluded, Transform parent=null)
		/// Draws a line with the points previously set
		{
			if (mesh == null) //in some cases mesh turns null (when switching MicroSplat output to Both/Splats for some reason (!))
				SetPointsApply();
			
			int numPoints = pointsCount;
			if (numPoints < 0) numPoints = vertices.Length/4;

			if (lineMat == null) lineMat = new Material( Shader.Find("Hidden/DPLayout/PolyLine") ); 
			if (lineTex == null) lineTex = Resources.Load("DPUI/PolyLineTex") as Texture2D; 

			lineMat.SetTexture("_MainTex", lineTex);
			lineMat.SetColor("_Color", color);
			lineMat.SetFloat("_Width", width);
			lineMat.SetFloat("_Offset", offset);
			lineMat.SetFloat("_NumPoints", numPoints-1);
			lineMat.SetFloat("_Dotted", dottedSpace);
			lineMat.SetInt("_ZTest", zMode==ZMode.Occluded ? 2 : 0); //2 for LEqual

			lineMat.SetPass(0);
			Graphics.DrawMeshNow(mesh, parent==null ? Matrix4x4.identity : parent.localToWorldMatrix);
		}


		public void DrawLine (Vector3[] points, Color color, float width, float dottedSpace=0, float offset=0.01f, int numPoints=-1, ZMode zMode=ZMode.Both, Transform parent=null)
		/// Sets points and draws a line
		{
			if (numPoints < 0) numPoints = points.Length;

			SetPoints(points, numPoints);
			DrawLine(color, width, dottedSpace:dottedSpace, offset:offset, zMode:zMode, parent:parent);
		}


		public void DrawLine (Vector3[][] points, Color color, float width, float dottedSpace=0, float offset=0.01f, int numPoints=-1, ZMode zMode=ZMode.Both, Transform parent=null)
		/// Draws multiple lines
		{
			SetPointsThread(points, numPoints);
			SetPointsApply();
			DrawLine(color, width, dottedSpace:dottedSpace, offset:offset, zMode:zMode, parent:parent);
		}


		public static void InstantLine (Vector3[] points, Color color, float width, float dottedSpace=0, float offset=0.01f, int numPoints=-1, ZMode zMode=ZMode.Both)
		/// Creates a new polyline, sets points and draws it at once
		{
			if (numPoints < 0) numPoints = points.Length;

			PolyLine line = new PolyLine(numPoints);
			line.SetPoints(points, numPoints:numPoints);
			line.DrawLine(color, width, dottedSpace:dottedSpace,  offset:offset, zMode:zMode);
		}


		private static Mesh TestMesh ()
		{
			Mesh mesh = new Mesh();
			mesh.vertices = new Vector3[] { new Vector3(0,0,0), new Vector3(0,0,0.01f), new Vector3(10,0,0.01f), new Vector3(10,0,0) };
			mesh.normals = new Vector3[] { new Vector3(-10,0,0), new Vector3(-10,0,0), new Vector3(20,0,0), new Vector3(20,0,0) };
			mesh.uv = new Vector2[] { new Vector2(-1,-1), new Vector2(-1,1), new Vector2(1,1), new Vector2(1,-1) };
			mesh.uv2 = new Vector2[] { new Vector2(0,0), new Vector2(0,0), new Vector2(1,10), new Vector2(1,10) };
			mesh.triangles = new int[] { 0,2,1,1,2,3 };

			return mesh;
		}

		public static void DrawTest ()
		{
			Mesh mesh = TestMesh();

			if (lineMat==null) lineMat = new Material( Shader.Find("Hidden/DPLayout/PolyLine") ); 
			if (lineTex==null) lineTex = Resources.Load("DPUI/PolyLineTex") as Texture2D; 

			lineMat.SetTexture("_MainTex", lineTex);
			lineMat.SetFloat("_Width", 10);
			lineMat.SetInt("_ZTest", 0);
			lineMat.SetColor("_Color", Color.red);

			lineMat.SetPass(0);
			Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
		}
	}
}