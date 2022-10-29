using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Den.Tools 
{


	[System.Serializable]
	public class Edges<T>
	{
		public CoordRect rect;
		public T[] array;

		public Edges () { }

		public Edges (CoordRect rect)
		{
			this.rect = rect;
			this.array = new T[(rect.size.x+rect.size.z)*2];
		}

		public T this[int x, int z] 
		{
			get 
			{ 
				if (x<rect.offset.x || x>= rect.offset.x+rect.size.x) throw new Exception("Edges: x:" + x + " is out of range:" + rect.offset.x + "-" + (rect.offset.x+rect.size.x));
				if (z<rect.offset.z || z>= rect.offset.z+rect.size.z) throw new Exception("Edges: z:" + z + " is out of range:" + rect.offset.z + "-" + (rect.offset.z+rect.size.z));

				if (z==rect.offset.z) return array[x-rect.offset.x];
				if (z==rect.offset.z+rect.size.z-1) return array[rect.size.x + x-rect.offset.x];
				if (x==rect.offset.x) return array[rect.size.x*2 + z-rect.offset.z];
				if (x==rect.offset.x+rect.size.x-1) return array[rect.size.x*2 + rect.size.z + z-rect.offset.z];

				throw new Exception("Edges: improper x:" + x + " and z:" + z + " while rect is:" + rect);
			}
			set 
			{ 
				if (x<rect.offset.x || x>= rect.offset.x+rect.size.x) throw new Exception("Edges: x:" + x + " is out of range:" + rect.offset.x + "-" + (rect.offset.x+rect.size.x));
				if (z<rect.offset.z || z>= rect.offset.z+rect.size.z) throw new Exception("Edges: z:" + z + " is out of range:" + rect.offset.z + "-" + (rect.offset.z+rect.size.z));

				if (z==rect.offset.z) { array[x-rect.offset.x] = value; return; }
				if (z==rect.offset.z+rect.size.z-1) { array[rect.size.x + x-rect.offset.x] = value; return; }
				if (x==rect.offset.x) { array[rect.size.x*2 + z-rect.offset.z] = value; return; }
				if (x==rect.offset.x+rect.size.x-1) { array[rect.size.x*2 + rect.size.z + z-rect.offset.z] = value; return; }

				throw new Exception("Edges: improper x:" + x + " and z:" + z + " while rect is:" + rect);
			}
		}
	}



	[System.Serializable]
	public class FloatEdges : Edges<float>
	{
		public FloatEdges () { }

		public FloatEdges (CoordRect rect)
		{
			this.rect = rect;
			this.array = new float[(rect.size.x+rect.size.z)*2];
		}

		public void ReadFloats2D (float[,] heights2D)
		{
			int sizeX = heights2D.GetLength(1);
			int sizeZ = heights2D.GetLength(0);
			if (rect.size.x > sizeX || rect.size.z > sizeZ)
				throw new Exception("Float[" + sizeX + "," + sizeZ +"] is smaller than the edges rect " + rect);

			for (int x=0; x<rect.size.x; x++)
			{
				//this[rect.offset.x+x, rect.offset.z] = heights2D[0, x];
				//this[rect.offset.x+x, rect.offset.z+sizeZ-1] = heights2D[sizeZ-1, x];

				array[x] = heights2D[0, x];
				array[rect.size.x + x] = heights2D[sizeZ-1, x];
			}

			for (int z=0; z<rect.size.z; z++)
			{
				//this[rect.offset.x, rect.offset.z+z] = heights2D[z, 0];
				//this[rect.offset.x+sizeX-1, rect.offset.z+z] = heights2D[z, sizeX-1];

				array[rect.size.x*2 + z] = heights2D[z, 0];
				array[rect.size.x*2 + rect.size.z + z] = heights2D[z, sizeX-1];
			}
		}

		public void ReadDelta2D (float[,] heights2D)
		/// Reads not the edge values, but the delta between edges and 2nd pixel from edge
		{
			int sizeX = heights2D.GetLength(1);
			int sizeZ = heights2D.GetLength(0);
			if (rect.size.x > sizeX || rect.size.z > sizeZ)
				throw new Exception("Float[" + sizeX + "," + sizeZ +"] is smaller than the edges rect " + rect);

			for (int x=0; x<rect.size.x; x++)
			{
				array[x] = heights2D[0,x] - heights2D[1,x];
				array[rect.size.x + x] = heights2D[sizeZ-1,x] - heights2D[sizeZ-2,x];
			}

			for (int z=0; z<rect.size.z; z++)
			{
				array[rect.size.x*2 + z] = heights2D[z,0] - heights2D[z,1];
				array[rect.size.x*2 + rect.size.z + z] = heights2D[z, sizeX-1] - heights2D[z, sizeX-2];
			}
		}

		public void ReadFloats3D (float[,,] splats2D, int ch)
		{
			int sizeX = splats2D.GetLength(1);
			int sizeZ = splats2D.GetLength(0);
			if (rect.size.x > sizeX || rect.size.z > sizeZ)
				throw new Exception("Float[" + sizeX + "," + sizeZ +"] is smaller than the edges rect " + rect);

			for (int x=0; x<rect.size.x; x++)
			{
				array[x] = splats2D[0, x, ch];
				array[rect.size.x + x] = splats2D[sizeZ-1, x, ch];
			}

			for (int z=0; z<rect.size.z; z++)
			{
				array[rect.size.x*2 + z] = splats2D[z, 0, ch];
				array[rect.size.x*2 + rect.size.z + z] = splats2D[z, sizeX-1, ch];
			}
		}

		public void ReadCornersFloats2D (float[,] heights2D)
		{
			int sizeX = heights2D.GetLength(1);
			int sizeZ = heights2D.GetLength(0);
			if (rect.size.x > sizeX || rect.size.z > sizeZ)
				throw new Exception("Float[" + sizeX + "," + sizeZ +"] is smaller than the edges rect " + rect);

			array[0] = heights2D[0, 0];
			array[rect.size.x] = heights2D[sizeZ-1, 0];
			array[rect.size.x-1] = heights2D[0, sizeX-1];
			array[rect.size.x*2-1] = heights2D[sizeZ-1, sizeX-1];
		}



		public static void Weld_z (FloatEdges edges, float[,] heights2D)
		{
			for (int x=0; x<edges.rect.size.x; x++)
			{
				float edgeVal = edges.array[edges.rect.size.x + x];
				heights2D[0, x] = edgeVal;
			}
		}

		public static void Weld_Z (FloatEdges edges, float[,] heights2D)
		{
			for (int x=0; x<edges.rect.size.x; x++)
			{
				float edgeVal = edges.array[x]; 
				heights2D[edges.rect.size.z-1, x] = edgeVal;
			}
		}

		public static void Weld_x (FloatEdges edges, float[,] heights2D)
		{
			for (int z=0; z<edges.rect.size.z; z++)
			{
				float edgeVal = edges.array[edges.rect.size.x*2 + edges.rect.size.z + z];
				heights2D[z, 0] = edgeVal;
			}
		}

		public static void Weld_X (FloatEdges edges, float[,] heights2D)
		{
			for (int z=0; z<edges.rect.size.z; z++)
			{
				float edgeVal = edges.array[edges.rect.size.x*2 + z];
				heights2D[z,edges.rect.size.x-1] = edgeVal;
			}
		}

		public static void Weld (FloatEdges edges, Coord direction, float[,] heights2D)
		{
			if (direction.x == 1) Weld_X(edges, heights2D);
			else if (direction.x == -1) Weld_x(edges, heights2D);
			else if (direction.z == 1) Weld_Z(edges, heights2D);
			else if (direction.z == -1) Weld_z(edges, heights2D);
		}


		///Same weld using 3d array
		public static void Weld_z (FloatEdges edges, float[,,] splats3D, int ch)
		{
			for (int x=0; x<edges.rect.size.x; x++)
			{
				float edgeVal = edges.array[edges.rect.size.x + x];
				splats3D[0, x, ch] = edgeVal;
			}
		}

		public static void Weld_Z (FloatEdges edges, float[,,] splats3D, int ch)
		{
			for (int x=0; x<edges.rect.size.x; x++)
			{
				float edgeVal = edges.array[x]; 
				splats3D[edges.rect.size.z-1, x, ch] = edgeVal;
			}
		}

		public static void Weld_x (FloatEdges edges, float[,,] splats3D, int ch)
		{
			for (int z=0; z<edges.rect.size.z; z++)
			{
				float edgeVal = edges.array[edges.rect.size.x*2 + edges.rect.size.z + z];
				splats3D[z, 0, ch] = edgeVal;
			}
		}

		public static void Weld_X (FloatEdges edges, float[,,] splats3D, int ch)
		{
			for (int z=0; z<edges.rect.size.z; z++)
			{
				float edgeVal = edges.array[edges.rect.size.x*2 + z];
				splats3D[z,edges.rect.size.x-1, ch] = edgeVal;
			}
		}

		public static void Weld (FloatEdges edges, Coord direction, float[,,] splats3D, int ch)
		{
			if (direction.x == 1) Weld_X(edges, splats3D, ch);
			else if (direction.x == -1) Weld_x(edges, splats3D, ch);
			else if (direction.z == 1) Weld_Z(edges, splats3D, ch);
			else if (direction.z == -1) Weld_z(edges, splats3D, ch);
		}



		///Welds preserving normals and using margins
		public static void Weld_z (FloatEdges edges, FloatEdges deltas, float[,] heights2D, int margins=10)
		{
			for (int x=0; x<edges.rect.size.x; x++)
			{
				float edgeVal = edges.array[edges.rect.size.x + x];
				float deltaVal = deltas.array[edges.rect.size.x + x];
				heights2D[0, x] = edgeVal;
					float innerDelta = (edgeVal + deltaVal) - heights2D[1,x];
				heights2D[1,x] = edgeVal + deltaVal;

				//if (x>margins && x<edges.rect.size.x-margins-1)
					for (int i=0; i<margins; i++)
						heights2D[2+i,x] += innerDelta * (1 - 1f*i/margins);
			}
		}

		public static void Weld_Z (FloatEdges edges, FloatEdges deltas, float[,] heights2D, int margins=10)
		{
			for (int x=0; x<edges.rect.size.x; x++)
			{
				float edgeVal = edges.array[x]; 
				float deltaVal = deltas.array[x]; 
				heights2D[edges.rect.size.z-1, x] = edgeVal;
					float innerDelta = (edgeVal + deltaVal) - heights2D[edges.rect.size.z-2, x];
				heights2D[edges.rect.size.z-2, x] = edgeVal + deltaVal;

				//if (x>margins && x<edges.rect.size.x-margins-1)
					for (int i=0; i<margins; i++)
						heights2D[edges.rect.size.z-3-i, x] += innerDelta * (1 - 1f*i/margins);
			}
		}

		public static void Weld_x (FloatEdges edges, FloatEdges deltas, float[,] heights2D, int margins=10)
		{
			for (int z=0; z<edges.rect.size.z; z++)
			{
				float edgeVal = edges.array[edges.rect.size.x*2 + edges.rect.size.z + z];
				float deltaVal = deltas.array[edges.rect.size.x*2 + edges.rect.size.z + z];
				heights2D[z, 0] = edgeVal;
					float innerDelta = (edgeVal + deltaVal) - heights2D[z, 1];
				heights2D[z, 1] = edgeVal + deltaVal;

				//if (z>margins && z<edges.rect.size.z-margins-1)
					for (int i=0; i<margins; i++)
						heights2D[z, 2+i] += innerDelta * (1 - 1f*i/margins);
			}
		}

		public static void Weld_X (FloatEdges edges, FloatEdges deltas, float[,] heights2D, int margins=10)
		{
			for (int z=0; z<edges.rect.size.z; z++)
			{
				float edgeVal = edges.array[edges.rect.size.x*2 + z];
				float deltaVal = deltas.array[edges.rect.size.x*2 + z];
				heights2D[z,edges.rect.size.x-1] = edgeVal;
					float innerDelta = (edgeVal + deltaVal) - heights2D[z,edges.rect.size.x-2];
				heights2D[z,edges.rect.size.x-2] = edgeVal + deltaVal;

				//if (z>margins && z<edges.rect.size.z-margins-1)
					for (int i=0; i<margins; i++)
						heights2D[z,edges.rect.size.x-3-i] += innerDelta * (1 - 1f*i/margins);
			}
		}
		
		public static void Weld (FloatEdges edges, FloatEdges deltas, Coord direction, float[,] heights2D, int margins=10)
		{
			if (direction.x == 1) Weld_X(edges, deltas, heights2D, margins);
			else if (direction.x == -1) Weld_x(edges, deltas, heights2D, margins);
			else if (direction.z == 1) Weld_Z(edges, deltas, heights2D, margins);
			else if (direction.z == -1) Weld_z(edges, deltas, heights2D, margins);
		}


	}



}
