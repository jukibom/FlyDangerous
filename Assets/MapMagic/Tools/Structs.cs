using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Den.Tools 
{
	[System.Serializable]
	[StructLayout (LayoutKind.Sequential)] //to pass to native
	public struct Coord
	{
		public int x;
		public int z;

		public int this[int c] { get => c==0 ? x : z;  set { if (x==0) x=value; else z=value; } }

		public static bool operator > (Coord c1, Coord c2) { return c1.x>c2.x && c1.z>c2.z; }
		public static bool operator < (Coord c1, Coord c2) { return c1.x<c2.x && c1.z<c2.z; }
		public static bool operator == (Coord c1, Coord c2) { return c1.x==c2.x && c1.z==c2.z; }
		public static bool operator != (Coord c1, Coord c2) { return c1.x!=c2.x || c1.z!=c2.z; }
		public static Coord operator + (Coord c, int s) { return  new Coord(c.x+s, c.z+s); }
		public static Coord operator + (Coord c1, Coord c2) { return  new Coord(c1.x+c2.x, c1.z+c2.z); }
		public static Coord operator - (Coord c) { return  new Coord(-c.x, -c.z); }
		public static Coord operator - (Coord c, int s) { return  new Coord(c.x-s, c.z-s); }
		public static Coord operator - (Coord c1, Coord c2) { return  new Coord(c1.x-c2.x, c1.z-c2.z); }
		public static Coord operator * (Coord c, int s) { return  new Coord(c.x*s, c.z*s); }
		public static Vector2 operator * (Coord c, Vector2 s) { return  new Vector2(c.x*s.x, c.z*s.y); }
		public static Vector3 operator * (Coord c, Vector3 s) { return  new Vector3(c.x*s.x, s.y, c.z*s.z); }
		public static Coord operator * (Coord c1, Coord c2) { return  new Coord((int)(c1.x*c2.x), (int)(c1.z*c2.z)); }
		public static Coord operator * (Coord c, float s) { return  new Coord((int)(c.x*s), (int)(c.z*s)); }
		public static Coord operator / (Coord c, int s) { return  new Coord(c.x/s, c.z/s); }
		public static Coord operator / (Coord c, float s) { return  new Coord((int)(c.x/s), (int)(c.z/s)); }

		public override bool Equals(object obj) { if (obj is Coord co) return co.x==x && co.z==z; return false; }
		public override int GetHashCode() {return x*10000000 + z;}

		public int Minimal {get{ return x<z ? x : z; } }
		public int Maximal {get{ return x>z ? x : z; } }
		public int SqrMagnitude {get{ return x*x + z*z; } }
		public float Magnitude {get{ return Mathf.Sqrt(x*x + z*z); } }

		public Vector4 vector4 {get{ return new Vector4(x,0,z,0); } }
		public Vector3 vector3 {get{ return new Vector3(x,0,z); } }
		public Vector2 vector2 {get{ return new Vector2(x,z); } }
		public Vector2D vector2d {get{ return new Vector2D(x,z); } }

		public static explicit operator Coord(Vector2D v) => new Coord((int)v.x, (int)v.z); //no flooring, handling negative values as casting to int
		public static explicit operator Vector2D(Coord c) => new Vector2D(c.x, c.z);
		public static explicit operator Vector3(Coord c) => new Vector3(c.x, 0, c.z);
		public static explicit operator Coord(int i) => new Coord(i, i);
		
		public static Coord zero {get{ return new Coord(0,0); }}

		public Coord (int x, int z) { this.x=x; this.z=z; }
		public Coord (int x) { this.x=x; this.z=x; }

		#region Cell Operations

			public static Coord PickCell (int ix, int iz, int cellRes)
			{
				int x = ix/cellRes;
				if (ix<0 && ix!=x*cellRes) x--;

				int z = iz/cellRes;
				if (iz<0 && iz!=z*cellRes) z--;
				
				return new Coord(x,z);
			}

			public static Coord PickCell (Coord c, int cellRes) { return PickCell(c.x, c.z, cellRes); }

			public static Coord PickCellByPos (float fx, float fz, float cellSize=1)
			{
				int x = (int)(fx/cellSize);
				if (fx<0 && fx!=x*cellSize) x--;

				int z = (int)(fz/cellSize);
				if (fz<0 && fz!=z*cellSize) z--;
				
				return new Coord (x,z);
			}

			public static Coord PickCellByPos (Vector3 v, float cellSize=1) { return PickCellByPos(v.x, v.z, cellSize); }


		#endregion

		#region Rounding

			//use v/tileSize instead of special tileSize methods
			
			public static Coord Floor (Vector3 v)
			{
				if (v.x<0) v.x--;  if (v.z<0) v.z--;
				return new Coord((int)(float)v.x, (int)(float)v.z);
			}

			public static Coord Floor (Vector2D v)
			{
				if (v.x<0) v.x--;  if (v.z<0) v.z--;
				return new Coord((int)(float)v.x, (int)(float)v.z);
			}

			public static Coord Floor (float x, float z)
			{
				if (x<0) x--;  if (z<0) z--;
				return new Coord((int)(float)x, (int)(float)z);
			}

			public static Coord Ceil (Vector2D v)
			/// Ceil is NOT Floor(v+1):
			/// Math.Ceiling(-2f)=-2,  Math.Floor(-2f+1)=-1,  Math.Floor(-2f)+1=-1
			{
				if (v.x<0) v.x--;  if (v.z<0) v.z--;
				return new Coord((int)(float)(v.x + 1f), (int)(float)(v.z + 1f));
			}

			public static Coord Ceil (float x, float z)
			{
				if (x<0) x--;  if (z<0) z--;
				return new Coord((int)(float)(x + 1f), (int)(float)(z + 1f));
			}

			public static Coord Round (Vector3 v)
			{
				if (v.x<0) v.x--;  if (v.z<0) v.z--;
				return new Coord((int)(float)(v.x + 0.5f), (int)(float)(v.z + 0.5f));
			}

			public static Coord Round (Vector2D v)
			{
				if (v.x<0) v.x--;  if (v.z<0) v.z--;
				return new Coord((int)(float)(v.x + 0.5f), (int)(float)(v.z + 0.5f));
			}

			public static Coord Round (float x, float z)
			{
				if (x<0) x--;  if (z<0) z--;
				return new Coord((int)(float)(x + 0.5f), (int)(float)(z + 0.5f));
			}



		#endregion

		public void Clamp (int sizeX, int sizeZ)
		{
			if (x > sizeX) x = sizeX;
			if (z > sizeZ) z = sizeZ;
		}
			
		public void ClampPositive ()
			{ x = Mathf.Max(0,x); z = Mathf.Max(0,z); }

		public void ClampByRect (CoordRect rect)
		// Closest coordinate within rect
		{ 
			if (x<rect.offset.x) x = rect.offset.x; if (x>=rect.offset.x+rect.size.x) x = rect.offset.x+rect.size.x-1;
			if (z<rect.offset.z) z = rect.offset.z; if (z>=rect.offset.z+rect.size.z) z = rect.offset.z+rect.size.z-1;
		}

		static public Coord Min (Coord c1, Coord c2) 
		{ 
			//return new Coord(Mathf.Min(c1.x,c2.x), Mathf.Min(c1.z,c2.z)); 
			int minX = c1.x<c2.x? c1.x : c2.x;
			int minZ = c1.z<c2.z? c1.z : c2.z;
			return new Coord(minX, minZ);
		}
		static public Coord Max (Coord c1, Coord c2) 
		{ 
			//return new Coord(Mathf.Max(c1.x,c2.x), Mathf.Max(c1.z,c2.z));
			int maxX = c1.x>c2.x? c1.x : c2.x;
			int maxZ = c1.z>c2.z? c1.z : c2.z;
			return new Coord(maxX, maxZ);

		}

		public Coord BaseFloor (int cellSize) //tested
		{
			return new Coord(
				x>=0 ? x/cellSize : (x+1)/cellSize-1,
				z>=0 ? z/cellSize : (z+1)/cellSize-1 );
		}


		public override string ToString()
		{
			return (base.ToString() + " x:" + x + " z:" + z);
		}


		public static float Distance (Coord c1, Coord c2)
		/// Standard Euclidean distance
		{
			int distX = c1.x - c2.x; //if (distX < 0) distX = -distX; //there should be a reason I've added this. Don't really remember
			int distZ = c1.z - c2.z; //if (distZ < 0) distZ = -distZ;
			return Mathf.Sqrt(distX*distX + distZ*distZ);
		}

		public static float DistanceSq (Coord c1, Coord c2)
		{
			int distX = c1.x - c2.x; if (distX < 0) distX = -distX;
			int distZ = c1.z - c2.z; if (distZ < 0) distZ = -distZ;
			return distX*distX + distZ*distZ;
		}

		public static int DistanceAxisAligned (Coord c1, Coord c2)
		/// Chebyshev Max distance
		{
			int distX = c1.x - c2.x; if (distX < 0) distX = -distX;
			int distZ = c1.z - c2.z; if (distZ < 0) distZ = -distZ;
			return distX>distZ? distX : distZ;
		}

		public static int DistanceManhattan (Coord c1, Coord c2)
		{
			int distX = c1.x - c2.x; if (distX < 0) distX = -distX;
			int distZ = c1.z - c2.z; if (distZ < 0) distZ = -distZ;
			return distX+distZ;
		}

		//TODO: test
		public static int DistanceAxisAligned (Coord c, CoordRect rect) //NOT manhattan dist. offset and size are instead of UnityEngine.Rect
		{
			//finding x distance
			int distPosX = rect.offset.x - c.x;
			int distNegX = c.x - rect.offset.x - rect.size.x;
			
			int distX;
			if (distPosX >= 0) distX = distPosX;
			else if (distNegX >= 0) distX = distNegX;
			else distX = 0;

			//finding z distance
			int distPosZ = rect.offset.z - c.z;
			int distNegZ = c.z - rect.offset.z - rect.size.z;
			
			int distZ;
			if (distPosZ >= 0) distZ = distPosZ;
			else if (distNegZ >= 0) distZ = distNegZ;
			else distZ = 0;

			//returning the maximum(!) distance 
			if (distX > distZ) return distX;
			else return distZ;
		}

		public static float DistanceAxisPriority (Coord c1, Coord c2)
		/// Whole number is an axis (Chebyshev) max dist, and remains is a remoteness from axis. Useful to set priority
		{
			int distX = c1.x - c2.x; if (distX < 0) distX = -distX;
			int distZ = c1.z - c2.z; if (distZ < 0) distZ = -distZ;

			int max = distX>distZ? distX : distZ;
			int min = distX<distZ? distX : distZ;

			return max + 1f*min/(max+1);
		}


		public IEnumerable<Coord> DistanceStep (int i, int dist) //4+4 terrains, no need to use separetely
		{
			yield return new Coord(x-i, z-dist);
			yield return new Coord(x-dist, z+i);
			yield return new Coord(x+i, z+dist);
			yield return new Coord(x+dist, z-i);

			yield return new Coord(x+i+1, z-dist);
			yield return new Coord(x-dist, z-i-1);
			yield return new Coord(x-i-1, z+dist);
			yield return new Coord(x+dist, z+i+1);
		}

		public IEnumerable<Coord> DistancePerimeter (int dist) //a circular square border sorted by distance
		{
			for (int i=0; i<dist; i++)
				foreach (Coord c in DistanceStep(i,dist)) yield return c;
		}

		public IEnumerable<Coord> DistanceArea (int maxDist)
		{
			yield return this;
			for (int i=0; i<maxDist; i++)
				foreach (Coord c in DistancePerimeter(i)) yield return c;
		}

		public IEnumerable<Coord> DistanceArea (CoordRect rect) //same as distance are, but clamped by rect
		{
			int maxDist = Mathf.Max(x-rect.offset.x, rect.Max.x-x, z-rect.offset.z, rect.Max.z-z) + 1;

			if (rect.Contains(this)) yield return this;
			for (int i=0; i<maxDist; i++)
				foreach (Coord c in DistancePerimeter(i)) 
					if (rect.Contains(c)) yield return c;
		}

		public static IEnumerable<Coord> MultiDistanceArea (Coord[] coords, int maxDist)
		{
			if (coords.Length==0) yield break;

			for (int c=0; c<coords.Length; c++) yield return coords[c];
			
			for (int dist=0; dist<maxDist; dist++)
				for (int i=0; i<dist; i++)
					for (int c=0; c<coords.Length; c++)
						foreach (Coord c2 in coords[c].DistanceStep(i,dist)) yield return c2;
		}

		public Vector3 ToVector3 (float cellSize) { return new Vector3(x*cellSize, 0, z*cellSize); }
		public Vector2 ToVector2 (float cellSize) { return new Vector2(x*cellSize, z*cellSize); }
		public Rect ToRect (float cellSize) { return new Rect(x*cellSize, z*cellSize, cellSize, cellSize); }
		public CoordRect ToCoordRect (int cellSize) { return new CoordRect(x*cellSize, z*cellSize, cellSize, cellSize); }

		public float GetFalloff (Vector2D center, float radius, float hardness, int smooth=1)
		/// Gets current pixel's falloff percent for stamps
		/// Smooth is iterational
		{
			float distSq = (x-center.x)*(x-center.x) + (z-center.z)*(z-center.z);
			if (distSq > radius*radius) return 0;
			if (distSq < radius*hardness * radius*hardness) return 1;

			float dist = Mathf.Sqrt(distSq);

			float hardRadius = radius*hardness;
			float fallof = 1 - (dist - hardRadius) / (radius - hardRadius);  //remaining dist / transition, inversed

			for (int s=0; s<smooth; s++)
				fallof = 3*fallof*fallof - 2*fallof*fallof*fallof;
			
			return fallof;
		}

		public float GetInterpolatedPercent (Vector2D pos)
		/// Gets influence of the pos on current coord
		/// Used instead of GetFallof when brush size is lower than one pixel
		{
			float xp = pos.x - x;  if (xp<0) xp = -xp; if (xp>1) return 0;
			float zp = pos.z - z;  if (zp<0) zp = -zp; if (zp>1) return 0;
			return (1-xp)*(1-zp);
		}

		public float GetInterpolatedFalloff (Vector2D center, float radius, float hardness, int smooth=1)
		/// Automatically switches between GetFalloff and GetInterpolatedPercent to get a neat right falloff of this coord
		{
			if (radius > 2) 
				return GetFalloff(center, radius, hardness, smooth);

			else if (radius > 1) return
				GetFalloff(center, radius, hardness, smooth) * ((radius-1)) +
				GetInterpolatedPercent(center) * (2-radius);

			else 
				return GetInterpolatedPercent(center) * radius;
		}

		//serialization
		public string Encode () { return "x=" + x + " z=" + z; }
		public void Decode (string[] lineMembers) { x=(int)lineMembers[2].Parse(typeof(int)); z=(int)lineMembers[3].Parse(typeof(int)); }
	}
	


	[System.Serializable]
	[StructLayout (LayoutKind.Sequential)] //to pass to native
	public struct CoordRect
	{
		public Coord offset;
		public Coord size;

		public enum TileMode { Clamp=0, Tile=1, PingPong=2 } //see Tile region

		//public int radius; //not related with size, because a clamped CoordRect should have non-changed radius

		public CoordRect (Coord offset, Coord size) { this.offset = offset; this.size = size; }
		public CoordRect (int offsetX, int offsetZ, int sizeX, int sizeZ) { this.offset = new Coord(offsetX,offsetZ); this.size = new Coord(sizeX,sizeZ);  }
		public CoordRect (float offsetX, float offsetZ, float sizeX, float sizeZ) { this.offset = new Coord((int)offsetX,(int)offsetZ); this.size = new Coord((int)sizeX,(int)sizeZ);  }
		public CoordRect (Rect r) { offset = new Coord((int)r.x, (int)r.y); size = new Coord((int)r.width, (int)r.height); }
		public CoordRect (Coord center, int radius) { this.offset = center-radius; this.size = new Coord(radius*2,radius*2); }
		public CoordRect (Vector2D center, float radius) 
		{ 
			Coord centerCoord = Coord.Round(center);
			int radiusInt = (int)(radius+1);
			this.offset = centerCoord-radiusInt;
			this.size = new Coord(radiusInt + 1 + radiusInt); //1 for the dimensions of the centerCoord tile
		}

		public Coord Max { get { return offset+size; } set { size = value-offset; } }
		public int MaxX { get { return offset.x+size.x; } set { size.x = value-offset.x; } }
		public int MaxZ { get { return offset.z+size.z; } set { size.z = value-offset.z; } }
		public Coord Min { get { return offset; } set { offset = value; } }
		public Coord Center { get { return offset + size/2; } } 
		public Vector3 CenterVector3 { get { return new Vector3(offset.x + size.x/2f, 0, offset.z + size.z/2f); } } 
		public int Count { get {return size.x*size.z;} }


		public override bool Equals(object obj) { return base.Equals(obj); }
		public override int GetHashCode() {return offset.x*100000000 + offset.z*1000000 + size.x*1000+size.z;}

		public int GetPos (Coord c) { return (c.z-offset.z)*size.x + c.x - offset.x; }
		public int GetPos (int x, int z) { return (z-offset.z)*size.x + x - offset.x; }

		public Coord GetCoord (int pos)
		{
			int z = pos/size.x + offset.z;
			int x = pos - (z-offset.z)*size.x + offset.x;
			return new Coord(x,z);
		}

		public static bool operator > (CoordRect c1, CoordRect c2) { return c1.size>c2.size; }
		public static bool operator < (CoordRect c1, CoordRect c2) { return c1.size<c2.size; }
		public static bool operator == (CoordRect c1, CoordRect c2) { return c1.offset==c2.offset && c1.size==c2.size; }
		public static bool operator != (CoordRect c1, CoordRect c2) { return c1.offset!=c2.offset || c1.size!=c2.size; }
		public static CoordRect operator * (CoordRect c, int s) { return  new CoordRect(c.offset*s, c.size*s); }
		public static CoordRect operator * (CoordRect c, float s) { return  new CoordRect(c.offset*s, c.size*s); }
		public static CoordRect operator / (CoordRect c, int s) { return  new CoordRect(c.offset/s, c.size/s); }

		public Vector4 vector4 {get{ return new Vector4(offset.x,offset.z,size.x,size.z); } }

		public static explicit operator CoordRect(Vector4 vec) => new CoordRect((int)vec.x, (int)vec.y, (int)vec.z, (int)vec.w);
		public static explicit operator Vector4(CoordRect cr) => new Vector4(cr.offset.x, cr.offset.z, cr.size.x, cr.size.z);
		public static explicit operator CoordRect(Rect r) => new CoordRect((int)r.x, (int)r.y, (int)r.width, (int)r.height);
		public static explicit operator Rect(CoordRect cr) => new Rect(cr.offset.x, cr.offset.z, cr.size.x, cr.size.z);

		public void Expand (int v) { offset.x-=v; offset.z-=v; size.x+=v*2; size.z+=v*2; }
		public CoordRect Expanded (int v) { return new CoordRect(offset.x-v, offset.z-v, size.x+v*2, size.z+v*2); }
		public void Contract (int v) { offset.x+=v; offset.z+=v; size.x-=v*2; size.z-=v*2; }
		public CoordRect Contracted (int v) { return new CoordRect(offset.x+v, offset.z+v, size.x-v*2, size.z-v*2); }

		public void Clamp (Coord min, Coord max)
		{
			Coord oldMax = Max;
			offset = Coord.Max(min, offset);
			size = Coord.Min(max-offset, oldMax-offset);
			size.ClampPositive();
		}

		public void ClampLine (ref Coord from, ref Coord to)
		/// changes from and to so that they are within rect
		{
			if (this.Contains(from) && this.Contains(to)) return;

			Vector2 dir = (from-to).vector2.normalized;
		}

		public static CoordRect Intersected (CoordRect c1, CoordRect c2) 
		{ 
			c1.Clamp(c2.Min, c2.Max); 
			return c1; 
		}

		public static CoordRect Intersected (CoordRect c1, CoordRect c2, CoordRect c3) 
		/// Finds the minimum intersection of 3 rects
		{ 
			c1.Clamp(c2.Min, c2.Max); 
			c1.Clamp(c3.Min, c3.Max);
			return c1; 
		}

		public static bool IsIntersecting (CoordRect c1, CoordRect c2) 
		{ 
			if (c2.Contains(c1.offset.x, c1.offset.z) || c2.Contains(c1.offset.x+c1.size.x, c1.offset.z) || c2.Contains(c1.offset.x, c1.offset.z+c1.size.z) || c2.Contains(c1.offset.x+c1.size.x, c1.offset.z+c1.size.z)) return true;
			if (c1.Contains(c2.offset.x, c2.offset.z) || c1.Contains(c2.offset.x+c2.size.x, c2.offset.z) || c1.Contains(c2.offset.x, c2.offset.z+c1.size.z) || c1.Contains(c2.offset.x+c2.size.x, c2.offset.z+c2.size.z)) return true;

			return false;
		}

		public static CoordRect Combined (CoordRect rect1, CoordRect rect2)
		{
			Coord min = Coord.Min(rect1.offset, rect2.offset);
			Coord max = Coord.Max(rect1.Max, rect2.Max);
			return new CoordRect(min, max-min);
		}

		public static CoordRect Combined (CoordRect[] rects)
		{
			Coord min=new Coord(2000000000, 2000000000); Coord max=new Coord(-2000000000, -2000000000); 
			for (int i=0; i<rects.Length; i++)
			{
				if (rects[i].offset.x < min.x) min.x = rects[i].offset.x;
				if (rects[i].offset.z < min.z) min.z = rects[i].offset.z;
				if (rects[i].offset.x + rects[i].size.x > max.x) max.x = rects[i].offset.x + rects[i].size.x;
				if (rects[i].offset.z + rects[i].size.z > max.z) max.z = rects[i].offset.z + rects[i].size.z;
			}
			return new CoordRect(min, max-min);
		}

		public void Encapsulate (Coord coord)
		/// Resizes this rect so that coord is included
		{
			if (coord.x < offset.x) { size.x += offset.x-coord.x; offset.x = coord.x; }
			if (coord.x > offset.x+size.x) { size.x = coord.x-offset.x; }

			if (coord.z < offset.z) { size.z += offset.z-coord.z; offset.z = coord.z; }
			if (coord.z > offset.z+size.z) { size.z = coord.z-offset.z; }
		}


		public static CoordRect WorldToPixel (Vector2D worldPos, Vector2D worldSize, Vector2D pixelSize, bool inclusive=true)
		/// Converts world rect to 0-aligned pixel/grid rect
		/// Can convert to pixels and cells as well
		{
			if (pixelSize.x == 0  ||  pixelSize.z == 0) 
				throw new Exception("Cell size is zero");

			Coord min; Coord max;
			if (inclusive)
			{
				min = new Coord(
					Mathf.FloorToInt(worldPos.x/pixelSize.x),
					Mathf.FloorToInt(worldPos.z/pixelSize.z) );
				max = new Coord(
					Mathf.CeilToInt((worldPos.x+worldSize.x)/pixelSize.x),
					Mathf.CeilToInt((worldPos.z+worldSize.z)/pixelSize.z) );
			}
			else
			{
				min = new Coord(
					Mathf.CeilToInt(worldPos.x/pixelSize.x),
					Mathf.CeilToInt(worldPos.z/pixelSize.z) );
				max = new Coord(
					Mathf.FloorToInt((worldPos.x+worldSize.x)/pixelSize.x),
					Mathf.FloorToInt((worldPos.z+worldSize.z)/pixelSize.z) );
			}
			
			return new CoordRect(min, max-min);
		}



		#region Contains

		public bool Contains (Coord coord)
			/// Checking if coord is within coordrect
			{ 
				return (coord.x >= offset.x && coord.x < offset.x + size.x && 
						coord.z >= offset.z && coord.z < offset.z + size.z); 
			}

			public bool Contains (int x, int z)
			{ 
				return (x- offset.x >= 0 && x- offset.x < size.x && 
						z- offset.z >= 0 && z- offset.z < size.z); 
			}

			public bool Contains (float x, float z)
			{
				return (x- offset.x >= 0 && x- offset.x < size.x && 
					z- offset.z >= 0 && z- offset.z < size.z); 
			}

			public bool Contains (Vector2 pos)
			{ 
				return (pos.x- offset.x >= 0 && pos.x- offset.x < size.x && 
						pos.y- offset.z >= 0 && pos.y- offset.z < size.z); 
			}

			public bool Contains (Vector3 pos)
			{ 
				return (pos.x- offset.x >= 0 && pos.x- offset.x < size.x && 
						pos.z- offset.z >= 0 && pos.z- offset.z < size.z); 
			}

			public bool Contains (float x, float z, float margins)
			/// Contracts the coordrect by margins and checks contains
			{
				return (x- offset.x >= margins && x- offset.x < size.x-margins &&
						z- offset.z >= margins && z- offset.z < size.z-margins);
			}

			public bool Contains (CoordRect r) //tested
			{
				return  r.offset.x >= offset.x && r.offset.x+r.size.x <= offset.x+size.x &&
						r.offset.z >= offset.z && r.offset.z+r.size.z <= offset.z+size.z;
			}

			public bool ContainsOrIntersects (CoordRect r) //tested
			{
				return  r.offset.x > offset.x-r.size.x && r.offset.x+r.size.x < offset.x+size.x+r.size.x &&
						r.offset.z > offset.z-r.size.z && r.offset.z+r.size.z < offset.z+size.z+r.size.z;
			}

		#endregion


		#region Tiling

			public Coord Tile (Coord coord, TileMode tileMode)
			/// Returns the corresponding coord within the matrix rect
			{
				//transferring to zero-based coord
				coord.x -= offset.x;
				coord.z -= offset.z;

				switch (tileMode)
				{
					//case TileMode.Once:
					//	if (coord.x < 0 || coord.x >= size.x) coord.x = -1;
					//	if (coord.z < 0 || coord.z >= size.z) coord.z = -1;
					//	break;

					case TileMode.Clamp:
						if (coord.x < 0) coord.x = 0; 
						if (coord.x >= size.x) coord.x = size.x - 1;
						if (coord.z < 0) coord.z = 0; 
						if (coord.z >= size.z) coord.z = size.z - 1;
						break;

					case TileMode.Tile:
						coord.x = coord.x % size.x; 
						if (coord.x < 0) coord.x= size.x + coord.x;
						coord.z = coord.z % size.z; 
						if (coord.z < 0) coord.z= size.z + coord.z;
						break;

					case TileMode.PingPong:
						coord.x = coord.x % (size.x*2); 
						if (coord.x < 0) coord.x = size.x*2 + coord.x; 
						if (coord.x >= size.x) coord.x = size.x*2 - coord.x - 1;

						coord.z = coord.z % (size.z*2); 
						if (coord.z<0) coord.z=size.z*2 + coord.z; 
						if (coord.z>=size.z) coord.z = size.z*2 - coord.z - 1;
						break;
				}
				

				coord.x += offset.x;
				coord.z += offset.z;

				return coord;
			}


			public void Tile (ref Coord coord, TileMode tileMode)
			/// Returns the corresponding coord within the matrix rect
			{
				//transferring to zero-based coord
				coord.x -= offset.x;
				coord.z -= offset.z;

				switch (tileMode)
				{
					//case TileMode.Once:
					//	if (coord.x < 0 || coord.x >= size.x) coord.x = -1;
					//	if (coord.z < 0 || coord.z >= size.z) coord.z = -1;
					//	break;

					case TileMode.Clamp:
						if (coord.x < 0) coord.x = 0; 
						if (coord.x >= size.x) coord.x = size.x - 1;
						if (coord.z < 0) coord.z = 0; 
						if (coord.z >= size.z) coord.z = size.z - 1;
						break;

					case TileMode.Tile:
						coord.x = coord.x % size.x; 
						if (coord.x < 0) coord.x= size.x + coord.x;
						coord.z = coord.z % size.z; 
						if (coord.z < 0) coord.z= size.z + coord.z;
						break;

					case TileMode.PingPong:
						coord.x = coord.x % (size.x*2); 
						if (coord.x < 0) coord.x = size.x*2 + coord.x; 
						if (coord.x >= size.x) coord.x = size.x*2 - coord.x - 1;

						coord.z = coord.z % (size.z*2); 
						if (coord.z<0) coord.z=size.z*2 + coord.z; 
						if (coord.z>=size.z) coord.z = size.z*2 - coord.z - 1;
						break;
				}
				

				coord.x += offset.x;
				coord.z += offset.z;
			}



			/*public Vector2 Tile (Vector2 vec, TileMode tileMode)
			/// Returns the corresponding coord within the matrix rect
			{
				switch (tileMode)
				{
					case TileMode.Clamp | TileMode.Once: return TileClamp(vec);
					case TileMode.Tile: return TileRepeat(vec);
					case TileMode.PingPong: return TilePingPong(vec);
					default: return vec;
				}
			}

			private Coord TileClamp (Coord coord)
			{
				if (coord.x < offset.x) coord.x = offset.x; 
				if (coord.x >= offset.x + size.x) coord.x = offset.x + size.x - 1;

				if (coord.z < offset.z) coord.z = offset.z; 
				if (coord.z >= offset.z + size.z) coord.z = offset.z + size.z - 1;

				return coord;
			}

			private Vector2 TileClamp (Vector2 vec)
			{
				if (vec.x < offset.x) vec.x = offset.x; 
				if (vec.x >= offset.x + size.x) vec.x = offset.x + size.x - 1;

				if (vec.y < offset.z) vec.y = offset.z; 
				if (vec.y >= offset.z + size.z) vec.y = offset.z + size.z - 1;

				return vec;
			}

			private Coord TileRepeat (Coord coord)
			{
				coord.x -= offset.x;
				coord.x = coord.x % size.x; 
				if (coord.x < 0) coord.x= size.x + coord.x;
				coord.x += offset.x;

				coord.z -= offset.z;
				coord.z = coord.z % size.z; 
				if (coord.z < 0) coord.z= size.z + coord.z;
				coord.z += offset.z;

				return coord;
			}

			private Vector2 TileRepeat (Vector2 vec)
			{
				vec.x -= offset.x;
				vec.x = vec.x % size.x; 
				if (vec.x < 0) vec.x= size.x + vec.x;
				vec.x += offset.x;

				vec.y -= offset.z;
				vec.y = vec.y % size.z; 
				if (vec.y < 0) vec.y= size.z + vec.y;
				vec.y += offset.z;

				return vec;
			}

			private Coord TilePingPong (Coord coord)
			{
				coord.x -= offset.x;
				coord.x = coord.x % (size.x*2); 
				if (coord.x < 0) coord.x = size.x*2 + coord.x; 
				if (coord.x >= size.x) coord.x = size.x*2 - coord.x - 1;
				coord.x += offset.x;

				coord.z -= offset.z;
				coord.z = coord.z % (size.z*2); 
				if (coord.z<0) coord.z=size.z*2 + coord.z; 
				if (coord.z>=size.z) coord.z = size.z*2 - coord.z - 1;
				coord.z += offset.z;

				return coord + offset;
			}

			private Vector2 TilePingPong (Vector2 vec)
			{
				vec.x -= offset.x;
				vec.x = vec.x % (size.x*2); 
				if (vec.x < 0) vec.x = size.x*2 + vec.x; 
				if (vec.x >= size.x) vec.x = size.x*2 - vec.x - 1;
				vec.x += offset.x;

				vec.y -= offset.z;
				vec.y = vec.y % (size.z*2); 
				if (vec.y<0) vec.y=size.z*2 + vec.y; 
				if (vec.y>=size.z) vec.y = size.z*2 - vec.y - 1;
				vec.y += offset.z;

				return vec;
			}*/

		#endregion

		public override string ToString()
		{
			return (base.ToString() + ": offsetX:" + offset.x + " offsetZ:" + offset.z + " sizeX:" + size.x + " sizeZ:" + size.z);
		}

		public void DrawGizmo ()
		{
			#if UNITY_EDITOR
			Vector3 s = size.ToVector3(1);
			Vector3 o = offset.ToVector3(1);
			Gizmos.DrawWireCube(o + s/2, s);
			#endif
		}


		#region Obsolete

			[Obsolete]
			public static CoordRect PickIntersectingCells (CoordRect rect, int cellRes) 
			{
				int rectMaxX = rect.offset.x+rect.size.x;
				int rectMaxZ = rect.offset.z+rect.size.z;
				
				int minX = rect.offset.x/cellRes; if (rect.offset.x<0 && rect.offset.x%cellRes!=0) minX--;
				int minZ = rect.offset.z/cellRes; if (rect.offset.z<0 && rect.offset.z%cellRes!=0) minZ--; 
				int maxX = rectMaxX/cellRes; if (rectMaxX>=0 && rectMaxX%cellRes!=0) maxX++;
				int maxZ = rectMaxZ/cellRes; if (rectMaxZ>=0 && rectMaxZ%cellRes!=0) maxZ++;

				return new CoordRect (minX, minZ, maxX-minX, maxZ-minZ);
			}
			//public static CoordRect PickIntersectingCells (Coord center, int range, int cellRes=1) { return PickIntersectingCells( new CoordRect(center-range, center+range), cellRes); } //TODO: test, might be broken when cellSize = 1

			[Obsolete]
			public static CoordRect PickIntersectingCellsByPos (float rectMinX, float rectMinZ, float rectMaxX, float rectMaxZ, float cellSize)
			{
				int minX = (int)(rectMinX/cellSize); if (rectMinX<0 && rectMinX!=minX*cellSize) minX--;
				int minZ = (int)(rectMinZ/cellSize); if (rectMinZ<0 && rectMinZ!=minZ*cellSize) minZ--;
				int maxX = (int)(rectMaxX/cellSize); if (rectMaxX>=0 && rectMaxX!=maxX*cellSize) maxX++;
				int maxZ = (int)(rectMaxZ/cellSize); if (rectMaxZ>=0 && rectMaxZ!=maxZ*cellSize) maxZ++;

				return new CoordRect (minX, minZ, maxX-minX, maxZ-minZ);
			}
			[Obsolete] public static CoordRect PickIntersectingCellsByPos (Vector3 pos, float range, float cellSize=1) { return PickIntersectingCellsByPos (pos.x-range, pos.z-range, pos.x+range, pos.z+range, cellSize); }
			[Obsolete] public static CoordRect PickIntersectingCellsByPos (Rect rect, float cellSize=1) { return PickIntersectingCellsByPos (rect.position.x, rect.position.y, rect.position.x+rect.size.x, rect.position.y+rect.size.y, cellSize); }


			[Obsolete]
			public CoordRect MapSized (int resolution)
			{
				CoordRect tem = new CoordRect( 
					Mathf.RoundToInt( offset.x / (1f * size.x / resolution)  ),
					Mathf.RoundToInt( offset.z / (1f * size.z / resolution) ),
					resolution,
					resolution );
				return tem;
			}


			[Obsolete]
			public int GetPos (float x, float z)
			///Rounds coordinates and gets value. To get interpolated value use GetInterpolated
			{
				int ix = (int)(x+0.5f); if (x<1) ix--;
				int iz = (int)(z+0.5f); if (z<1) iz--;
				return (iz-offset.z)*size.x + ix - offset.x; 
			}


			//serialization
			[Obsolete] public string Encode () { return "offsetX=" + offset.x + " offsetZ=" + offset.z + " sizeX=" + size.x + " sizeZ=" + size.z; }
			[Obsolete] public void Decode (string[] lineMembers) 
			{ 
				offset.x=(int)lineMembers[2].Parse(typeof(int)); offset.z=(int)lineMembers[3].Parse(typeof(int));
				size.x=(int)lineMembers[4].Parse(typeof(int)); size.z=(int)lineMembers[5].Parse(typeof(int)); 
			}


			/*public Vector2 ToWorldspace (Coord coord, Rect worldRect)
			{
				return new Vector2 ( 1f*(coord.x-offset.x)/size.x * worldRect.width + worldRect.x, 
									 1f*(coord.z-offset.z)/size.z * worldRect.height + worldRect.y);  //percentCoord*worldWidth + worldOffset
			}

			public Coord ToLocalspace (Vector2 pos, Rect worldRect)
			{
				return new Coord ( (int) ((pos.x-worldRect.x)/worldRect.width * size.x + offset.x),
								   (int) ((pos.y-worldRect.y)/worldRect.height * size.z + offset.z) ); //percentPos*size + offset
			}*/

			
			/*public IEnumerable<Coord> Cells (int cellSize) //coordinates of the cells inside this rect
			{
				//transforming to cell-space
				Coord min = offset/cellSize;
				Coord max = (Max-1)/cellSize + 1;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					yield return new Coord(x,z);
				}
			}*/

		#endregion
	}


	[System.Serializable]
	[StructLayout (LayoutKind.Sequential)] //to pass to native
	public struct Coord3D
	{
		public int x;
		public int y;
		public int z;

		public static bool operator > (Coord3D c1, Coord3D c2) { return c1.x>c2.x && c1.y>c2.y && c1.z>c2.z; }
		public static bool operator < (Coord3D c1, Coord3D c2) { return c1.x<c2.x && c1.y<c2.y && c1.z<c2.z; }
		public static bool operator == (Coord3D c1, Coord3D c2) { return c1.x==c2.x && c1.y==c2.y && c1.z==c2.z; }
		public static bool operator != (Coord3D c1, Coord3D c2) { return c1.x!=c2.x || c1.y!=c2.y || c1.z!=c2.z; }
		public static Coord3D operator + (Coord3D c, int s) { return  new Coord3D(c.x+s, c.y+s, c.z+s); }
		public static Coord3D operator + (Coord3D c1, Coord3D c2) { return  new Coord3D(c1.x+c2.x, c1.y+c2.y, c1.z+c2.z); }
		public static Coord3D operator - (Coord3D c) { return  new Coord3D(-c.x, -c.y, -c.z); }
		public static Coord3D operator - (Coord3D c, int s) { return  new Coord3D(c.x-s, c.y-s, c.z-s); }
		public static Coord3D operator - (Coord3D c1, Coord3D c2) { return  new Coord3D(c1.x-c2.x, c1.y-c2.y, c1.z-c2.z); }
		public static Coord3D operator * (Coord3D c, int s) { return  new Coord3D(c.x*s, c.y*s, c.z*s); }
		public static Vector3 operator * (Coord3D c, Vector3 s) { return  new Vector3(c.x*s.x, c.y*s.y, c.z*s.z); }
		public static Coord3D operator * (Coord3D c1, Coord3D c2) { return  new Coord3D(c1.x*c2.x, c1.y*c2.y, c1.z*c2.z); }
		public static Coord3D operator * (Coord3D c, float s) { return  new Coord3D((int)(c.x*s), (int)(c.y*s), (int)(c.z*s)); }
		public static Coord3D operator / (Coord3D c, int s) { return  new Coord3D(c.x/s, c.y/s, c.z/s); }
		public static Coord3D operator / (Coord3D c, float s) { return new Coord3D((int)(c.x/s), (int)(c.y/s), (int)(c.z/s)); }

		public static readonly Coord3D up = new Coord3D(0,1,0); //TODO: could be changed externally
		public static readonly Coord3D down = new Coord3D(0,-1,0);
		public static readonly Coord3D front = new Coord3D(0,0,1);
		public static readonly Coord3D back = new Coord3D(0,0,-1);
		public static readonly Coord3D left = new Coord3D(-1,0,0);
		public static readonly Coord3D right = new Coord3D(1,0,0);

		public override bool Equals(object obj) { if (obj is Coord3D co) return co.x==x && co.y==y && co.z==z; return false; }
		public override int GetHashCode() {return x*1000000 + y*1000 + z;}

		public int Minimal {get{ int xz = x<z ? x : z; return xz<y ? xz : y; } }
		public int Maximal {get{ int xz = x>z ? x : z; return xz>y ? xz : y; } }
		public int SqrMagnitude {get{ return x*x + y*y + z*z; } }
		public float Magnitude {get{ return Mathf.Sqrt(x*x + y*y + z*z); } }

		public static explicit operator Coord3D(Vector3 v) => new Coord3D((int)v.x, (int)v.y, (int)v.z); //no flooring, handling negative values as casting to int
		public static explicit operator Vector3(Coord3D c) => new Vector3(c.x, c.y, c.z);
		
		public static Coord3D zero {get{ return new Coord3D(0,0,0); }}

		public Coord3D (int x, int y, int z) { this.x=x; this.y=y; this.z=z; }

		public override string ToString() => $"{base.ToString()} x:{x}, y{y}, z:{z}";
	}


	[Serializable]
	[StructLayout (LayoutKind.Sequential)] //to pass to native
	public struct Vector2D : IEquatable<Vector2D> 
	{
		public float x;
		public float z;

		public const float kEpsilon = 1E-05F;
		public const float kEpsilonNormalSqrt = 1E-15F;

		public float this[int c] { get => c==0 ? x : z;  set { if (x==0) x=value; else z=value; } }

		public Vector2D (float x, float z) { this.x=x; this.z=z; }
		public Vector2D (float v) { this.x=v; this.z=v; }

		public static readonly Vector2D zero = new Vector2D(0,0);
		public static readonly Vector2D one = new Vector2D(1,1);

		public static Vector2D Zero { get; } = new Vector2D(0f, 0f);
		public static Vector2D One { get; } = new Vector2D(1f, 1f);
		public static Vector2D PositiveInfinity { get; } = new Vector2D(float.PositiveInfinity, float.PositiveInfinity);
		public static Vector2D NegativeInfinity { get; } = new Vector2D(float.NegativeInfinity, float.NegativeInfinity);

		public float SqrMagnitude => x*x + z*z;
		public float Magnitude => (float)Math.Sqrt((double)(x*x + z*z));
		public Vector2D Normalized 
		{get{ 
			float m = (float)Math.Sqrt((double)(x*x + z*z)); 
			return m>1E-05f ? new Vector2D(x/m, z/m) : new Vector2D(0,0);
		}}

		public void ClampPositive ()
		{
			if (x<0) x=0;
			if (z<0) z=0;
		}

		public static float Dot (Vector2D lhs, Vector2D rhs) 
		{ 
			return lhs.x * rhs.x + lhs.z * rhs.z; 
		}

		public static float Distance (Vector2D a, Vector2D b)
		{
			float num = a.x - b.x;
			float num2 = a.z - b.z;
			return (float)Math.Sqrt((double)(num * num + num2 * num2));
		}

		public static Vector2D Lerp (Vector2D a, Vector2D b, float t)
		{
			if (t>1) t=1; 
			if (t<0) t=0;
			return new Vector2D(a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t);
		}

		public static Vector2D Min (Vector2D lhs, Vector2D rhs)
		{
			return new Vector2D(lhs.x<rhs.x ? lhs.x : rhs.x, lhs.z<rhs.z ? lhs.z : rhs.z);
		}

		public static Vector2D Max (Vector2D lhs, Vector2D rhs)
		{
			return new Vector2D(lhs.x>rhs.x ? lhs.x : rhs.x, lhs.z>rhs.z ? lhs.z : rhs.z);
		}

		public static Vector2D Normalize (Vector3 v)
		{
			float m = (float)Math.Sqrt((double)(v.x*v.x + v.z*v.z)); 
			return m>1E-05f ? new Vector2D(v.x/m, v.z/m) : new Vector2D(0,0);
		}

		public void Normalize ()
		{
			float m = (float)Math.Sqrt((double)(x*x + z*z)); 
			if (m>1E-05f) {x=x/m; z=z/m;}
			else {x=0; z=0;}
		}

		public override int GetHashCode () { return x.GetHashCode() ^ z.GetHashCode() << 2; }
		public bool Equals (Vector2D other) { return x == other.x && z == other.z; }
		public override bool Equals (object other)
		{
			if (!(other is Vector2D otherV)) return false;
			return x == otherV.x && z == otherV.z;
		}

		public static (Vector2D,Vector2D) Intersected (Vector2D pos1, Vector2D size1, Vector2D pos2, Vector2D size2)
		/// Copy of CoordRect intersection
		{
			Vector2D pos3 = Vector2D.Max(pos1, pos2);
			Vector2D size3 = Vector2D.Min(pos1+size1, pos2+size2) - pos3;
			size3.ClampPositive();
			return (pos3, size3);
		}

		public static bool Intersects (Vector2D pos1, Vector2D size1, Vector2D pos2, Vector2D size2)
		/// Finds if two world rects intersecting
		{
			Vector2D pos = Vector2D.Max(pos1, pos2);
			Vector2D size = Vector2D.Min(pos1+size1, pos2+size2) - pos;

			if (size.x > 0 && size.z > 0)
				return true;
			else
				return false;
		}

		public static bool Contains (Vector2D pos, Vector2D size, Vector2D pos2)
		/// Checks if pos2 within pos-size rect
		{
			return (pos2.x>pos.x && pos2.x<pos.x+size.x &&
					pos2.z>pos.z && pos2.z<pos.z+size.z);
		}


		public override string ToString() { return string.Format("({0:F1}, {1:F1})", x, z); }

		public static Vector2D operator + (Vector2D a, Vector2D b) => new Vector2D(a.x + b.x, a.z + b.z);
		public static Vector2D operator + (Vector2D a, float b) => new Vector2D(a.x + b, a.z + b);
		public static Vector2D operator - (Vector2D a, Vector2D b) => new Vector2D(a.x - b.x, a.z - b.z);
		public static Vector2D operator - (Vector2D a, float b) => new Vector2D(a.x-b, a.z-b);
		public static Vector2D operator * (Vector2D a, Vector2D b) => new Vector2D(a.x * b.x, a.z * b.z);
		public static Vector2D operator / (Vector2D a, Vector2D b) => new Vector2D(a.x / b.x, a.z / b.z);
		public static Vector2D operator - (Vector2D a) => new Vector2D(0f - a.x, 0f - a.z);
		public static Vector2D operator * (Vector2D a, float d) => new Vector2D(a.x * d, a.z * d);
		public static Vector2D operator * (float d, Vector2D a) => new Vector2D(a.x * d, a.z * d);
		public static Vector2D operator / (Vector2D a, float d) => new Vector2D(a.x / d, a.z / d);
		public static Vector2D operator / (float d, Vector2D a) => new Vector2D(d / a.x, d / a.z);

		public static Vector3 operator * (Vector3 a, Vector2D b) => new Vector3(a.x*b.x, a.y, a.z*b.z);
		public static Vector3 operator / (Vector3 a, Vector2D b) => new Vector3(a.x/b.x, a.y, a.z/b.z);
		public static Vector3 operator + (Vector3 a, Vector2D b) => new Vector3(a.x+b.x, a.y, a.z+b.z);
		public static Vector3 operator - (Vector3 a, Vector2D b) => new Vector3(a.x-b.x, a.y, a.z-b.z);
		public static Vector3 operator * (Vector2D a, Vector3 b) => new Vector3(a.x*b.x, b.y, a.z*b.z);
		public static Vector3 operator / (Vector2D a, Vector3 b) => new Vector3(a.x/b.x, b.y, a.z/b.z);
		public static Vector3 operator + (Vector2D a, Vector3 b) => new Vector3(a.x+b.x, b.y, a.z+b.z);
		public static Vector3 operator - (Vector2D a, Vector3 b) => new Vector3(a.x-b.x, b.y, a.z-b.z);

		public static bool operator == (Vector2D lhs, Vector2D rhs)
		{
			float num = lhs.x - rhs.x;
			float num2 = lhs.z - rhs.z;
			return num * num + num2 * num2 < 9.99999944E-11f;
		}
		public static bool operator != (Vector2D lhs, Vector2D rhs) => !(lhs == rhs);

		public static explicit operator Vector2D(Vector3 v) => new Vector2D(v.x, v.z);
		public static explicit operator Vector3(Vector2D v) => new Vector3(v.x, 0f, v.z);
		public static explicit operator Vector2D(Vector2 v) => new Vector2D(v.x, v.y);
		public static explicit operator Vector2(Vector2D v) => new Vector3(v.x, v.z);
		public static explicit operator Vector2D(float v) => new Vector2D(v, v);

		public Coord RoundToCoord () { return new Coord( (int)(float)(x<0 ? x-1 : x + 0.5f), (int)(float)(z<0 ? z-1 : z + 0.5f) ); }
	}


	[System.Serializable]
	public struct PosRect
	{
		public Vector3 offset;
		public Vector3 size;


	}




	[System.Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct StructArray 
	{ 
		[FieldOffset(0)] public byte b0;  
		[FieldOffset(1)] public byte b1;  
		[FieldOffset(2)] public byte b2;  
		[FieldOffset(3)] public byte b3;  
		[FieldOffset(4)] public byte b4;  
		[FieldOffset(5)] public byte b5;  
		[FieldOffset(6)] public byte b6;  
		[FieldOffset(7)] public byte b7; 
		[FieldOffset(0)] private long l;

		public const int length = 8;
			
		public byte this[int n]
		{
			get { return (byte)((l>>n*8) & 0b_1111_1111); }
			set 
			{ 
				l &= ~(((long)0b_1111_1111) << n*8); //erasing previous val
				l |= ((long)value)<<n*8;  //writing new one
			}
		}

		public float GetFloat (int n)
		{
			long val = (l>>n*8) & 0b_1111_1111;
			return val / 255f;
		}

		public void SetFloat (int n, float f)
		{
			long val = (int)(f*255);
			l &= ~(((long)0b_1111_1111) << n*8);
			l |= val<<n*8;
		}
	}



	public struct StructArrayExtended<T>
	///Stores first 4 T as a struct, and others as ref array
	{ 
		public T i0;  
		public T i1;  
		public T i2;  
		public T i3;  

		public int count;  
		private T[] others;


		public StructArrayExtended (int capacity)
		{
			i0=default; i1=default; i2=default; i3=default;
			others = null;
			count=0;
			Capacity = capacity;
		}


		public T this[int n]
		{
			get 
			{ 
				switch (n)
				{
					case 0: return i0;
					case 1: return i1;
					case 2: return i2;
					case 3: return i3;
					default: return others[n-4];
				}
			}
			set 
			{ 
				switch (n)
				{
					case 0: i0 = value; break;
					case 1: i1 = value; break;
					case 2: i2 = value; break;
					case 3: i3 = value; break;
					default: others[n-4] = value; break;
				}
			}
		}


		public int Capacity
		{
			get{ return 4 + (others!=null ? others.Length : 0); }
		
			set{
				if (value <= 4)
				{
					if (others!=null)
						others = null;
				}

				else
				{
					if (others==null) others = new T[value-4];
					else ArrayTools.Resize(ref others, value-4);
				}
			}
		}


		public void Add (T item)
		{	
			switch (count)
			{
				case 0: i0 = item; break;
				case 1: i1 = item; break;
				case 2: i2 = item; break;
				case 3: i3 = item; break;
				default:
					if (others == null) others = new T[4];
					if (others.Length <= count-4) ArrayTools.Resize(ref others, others.Length*2);
					others[count-4] = item;
					break;
			}
			count++;
		}


		public void AddRange (StructArrayExtended<T> other)
		{
			for (int i=0; i<other.count; i++)
				this.Add(other[i]);
		}


		public int FindIndex (T item)
		{
			if (i0.Equals(item)) return 0;
			if (i1.Equals(item)) return 1;
			if (i2.Equals(item)) return 2;
			if (i3.Equals(item)) return 3;

			if (others != null)
				for (int i=0; i<others.Length; i++)
					if (others[i].Equals(item)) return i+4;

			return -1;
		}
	}


	[System.Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct SemVer
	{
		public enum PRT { Release=4, RC=3, Beta=2, Alpha=1 };

		[FieldOffset(0)] public byte major;
		[FieldOffset(1)] public byte minor;
		[FieldOffset(2)] public byte prt;
		[FieldOffset(3)] public byte patch;

		[FieldOffset(0)] private int hash; //could be negative
		[FieldOffset(0)] private uint summary;
		

		public SemVer (byte major, byte minor, PRT prt, byte patch)
		{
			this.hash = 0;
			this.summary = 0;

			this.major = major;
			this.minor = minor;
			this.prt = (byte)prt;
			this.patch = patch;
		}

		public SemVer (byte major, byte minor, byte patch) : this (major, minor, PRT.Release, patch) { }

		public string PRTtoString 
		{get{
			switch (prt)
			{
				default: return "";
				case 3: return "RC";
				case 2: return "B";
				case 1: return "A";
			}
		}}

		public override string ToString () => $"{major}.{minor}.{PRTtoString}{patch}";

		public bool Equals (SemVer obj) => summary == obj.summary;
		public override bool Equals (object obj) => summary == ((SemVer)obj).summary;
		public override int GetHashCode () => hash;

		public static bool operator == (SemVer v1, SemVer v2) => v1.summary == v2.summary;
		public static bool operator != (SemVer v1, SemVer v2) => v1.summary != v2.summary;
		public static bool operator < (SemVer v1, SemVer v2) => v1.summary < v2.summary;
		public static bool operator > (SemVer v1, SemVer v2) => v1.summary > v2.summary;
		public static bool operator <= (SemVer v1, SemVer v2) => v1.summary <= v2.summary;
		public static bool operator >= (SemVer v1, SemVer v2) => v1.summary >= v2.summary;
	}
}