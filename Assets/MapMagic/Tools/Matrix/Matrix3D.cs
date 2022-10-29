using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Den.Tools 
{
	[System.Serializable]
	public struct CoordDir
	{
		public int x; public int y; public int z; 
		public byte dir; //0-5 sides, 6 is itself, 7 is non-existing CoordDir
		
		static readonly public byte[] oppositeDir = {1,0,3,2,5,4};

		static readonly public int[] dirToPosX = {0, 0, 1,-1, 0, 0};
		static readonly public int[] dirToPosY = {1,-1, 0, 0, 0, 0};
		static readonly public int[] dirToPosZ = {0, 0, 0, 0, 1,-1};
		

		public CoordDir opposite 
			{get{ return new CoordDir(x+dirToPosX[dir], y+CoordDir.dirToPosY[dir], z+CoordDir.dirToPosZ[dir], oppositeDir[dir]);}}

		public CoordDir (bool empty) {this.x=0; this.y=0; this.z=0; this.dir=0; if (!empty) dir=7; } 
		public static CoordDir empty {get{ return new CoordDir(0,0,0,7); }}
		public CoordDir (int x, int y, int z) {this.x=x; this.y=y; this.z=z; this.dir=7; }
		public CoordDir (int x, int y, int z, byte d) {this.x=x; this.y=y; this.z=z; this.dir=d; }
		public CoordDir (CoordDir c, byte d) {this.x=c.x; this.y=c.y; this.z=c.z; this.dir=d; }
		
		public bool exists {get{return dir!=7;}}
		public Vector3 center { get{ return new Vector3(x+0.5f,y+0.5f,z+0.5f); } }
		public Vector3 pos { get{ return new Vector3(x,y,z); } }
		
		public static CoordDir operator + (CoordDir a, CoordDir b) { return new CoordDir(a.x+b.x, a.y+b.y, a.z+b.z); }
		public static CoordDir operator + (CoordDir a, int i) { return new CoordDir(a.x+i, a.y+i, a.z+i); }
		public static CoordDir operator - (CoordDir a, CoordDir b) { return new CoordDir(a.x-b.x, a.y-b.y, a.z-b.z); }
		public static CoordDir operator - (CoordDir a, int i) { return new CoordDir(a.x-i, a.y-i, a.z-i); }
		public static CoordDir operator ++ (CoordDir a) { return new CoordDir(a.x+1, a.y+1, a.z+1); }
		public static CoordDir operator * (CoordDir a, int s) { return  new CoordDir(a.x*s, a.y*s, a.z*s); }
		public static CoordDir operator * (CoordDir a, CoordDir b) { return  new CoordDir(a.x*b.x, a.y*b.y, a.z*b.z); }
		public static CoordDir operator / (CoordDir a, int s) { return  new CoordDir(a.x/s, a.y/s, a.z/s); }
		public static CoordDir operator / (CoordDir a, CoordDir b) { return  new CoordDir(a.x/b.x, a.y/b.y, a.z/b.z); }
		
		public static bool operator == (CoordDir a, CoordDir b) { return (a.x==b.x && a.y==b.y && a.z==b.z && a.dir==b.dir); }
		public static bool operator != (CoordDir a, CoordDir b) { return !(a.x==b.x && a.y==b.y && a.z==b.z && a.dir==b.dir); }
		public override bool Equals(object obj) { return base.Equals(obj); }
		public override int GetHashCode() {return ((y & 0xFFFFFFF) << 32)  |  ((x & 0xFFFF) << 16)  |  (z & 0xFFFF); }

		public static bool operator > (CoordDir a, CoordDir b) { return (a.x>b.x || a.z>b.z); } //do not compare y's
		public static bool operator >= (CoordDir a, CoordDir b) { return (a.x>=b.x || a.z>=b.z); }
		public static bool operator < (CoordDir a, CoordDir b) { return (a.x<b.x || a.z<b.z); }
		public static bool operator <= (CoordDir a, CoordDir b) { return (a.x<=b.x || a.z<=b.z); }


		public static CoordDir Min (CoordDir[] coords)
		{
			CoordDir min = new CoordDir(int.MaxValue, int.MaxValue, int.MaxValue,7);
			for (int i=0; i<coords.Length; i++)
			{
				if (coords[i].x < min.x) min.x = coords[i].x;
				if (coords[i].y < min.y) min.y = coords[i].y;
				if (coords[i].z < min.z) min.z = coords[i].z;
			}
			return min;
		}

		public static CoordDir Max (CoordDir[] coords)
		{
			CoordDir max = new CoordDir(int.MinValue, int.MinValue, int.MinValue,7);
			for (int i=0; i<coords.Length; i++)
			{
				if (coords[i].x > max.x) max.x = coords[i].x;
				if (coords[i].y > max.y) max.y = coords[i].y;
				if (coords[i].z > max.z) max.z = coords[i].z;
			}
			return max;
		}

		public CoordDir GetChunkCoord (CoordDir worldCoord, int chunkSize) //gets chunk CoordDirinates using wholeterrain unit CoordDir
		{
			return new CoordDir
				(
					worldCoord.x>=0 ? (int)(worldCoord.x/chunkSize) : (int)((worldCoord.x+1)/chunkSize)-1,
					0,
					worldCoord.z>=0 ? (int)(worldCoord.z/chunkSize) : (int)((worldCoord.z+1)/chunkSize)-1
					);
		}
		
		public int BlockMagnitude2 { get { return Mathf.Abs(x)+Mathf.Abs(z); } }
		public int BlockMagnitude3 { get { return Mathf.Abs(x)+Mathf.Abs(y)+Mathf.Abs(z); } }

		public override string ToString() { return "x:"+x+" y:"+y+" z:"+z+" dir:"+dir; }

		public Vector3 vector3 { get { return new Vector3(x,y,z); }}
		public Vector3 vector3centered { get { return new Vector3(x+0.5f,y+0.5f,z+0.5f); }}

		public Coord coord { get{ return new Coord(x,z); }}

		public static byte NormalToDir (Vector3 normal)
		{
			float absX = normal.x>0? normal.x : -normal.x;
			float absY = normal.y>0? normal.y : -normal.y;
			float absZ = normal.z>0? normal.z : -normal.z;

			if (absY>absX && absY>absZ)
			{
				if (normal.y>0) return 0;
				else return 1;
			}

			else if (absX>absY && absX>absZ)
			{
				if (normal.x>0) return 2;
				else return 3;
			}

			else if (absZ>absY && absZ>absX)
			{
				if (normal.z>0) return 4;
				else return 5;
			}

			else return 7; //not exists
		}

		#region Neighbours

			public static readonly CoordDir[] neigsLut = new CoordDir[] {
				//planar block			  //this block		//concave corner
				//dir0
				new CoordDir(0, 0, 1, 0),	new CoordDir(0,0,0,4),  new CoordDir(0, 1, 1, 5),	
				new CoordDir(1, 0, 0, 0),	new CoordDir(0,0,0,2),  new CoordDir(1, 1, 0, 3), 
				new CoordDir(0, 0,-1, 0),	new CoordDir(0,0,0,5),  new CoordDir(0, 1,-1, 4), 
				new CoordDir(-1,0, 0, 0),	new CoordDir(0,0,0,3),  new CoordDir(-1,1, 0, 2), 
				//dir1
				new CoordDir(0, 0, 1, 1),	new CoordDir(0,0,0,4),  new CoordDir(0,-1, 1, 5), 
				new CoordDir(-1,0, 0, 1),	new CoordDir(0,0,0,3),  new CoordDir(-1,-1,0, 2), 
				new CoordDir(0, 0,-1, 1),	new CoordDir(0,0,0,5),  new CoordDir(0,-1,-1, 4), 
				new CoordDir(1, 0, 0, 1),	new CoordDir(0,0,0,2),  new CoordDir(1,-1, 0, 3), 
				//dir2
				new CoordDir(0, 1, 0, 2),	new CoordDir(0,0,0,0),  new CoordDir(1, 1, 0, 1), 
				new CoordDir(0, 0, 1, 2),	new CoordDir(0,0,0,4),  new CoordDir(1, 0, 1, 5), 
				new CoordDir(0,-1, 0, 2),	new CoordDir(0,0,0,1),  new CoordDir(1,-1, 0, 0), 
				new CoordDir(0, 0,-1, 2),	new CoordDir(0,0,0,5),  new CoordDir(1, 0,-1, 4), 
				//dir3
				new CoordDir(0, 1, 0, 3),	new CoordDir(0,0,0,0),  new CoordDir(-1, 1, 0, 1), 
				new CoordDir(0, 0,-1, 3),	new CoordDir(0,0,0,5),  new CoordDir(-1, 0,-1, 4), 
				new CoordDir(0,-1, 0, 3),	new CoordDir(0,0,0,1),  new CoordDir(-1,-1, 0, 0), 
				new CoordDir(0, 0, 1, 3),	new CoordDir(0,0,0,4),  new CoordDir(-1, 0, 1, 5), 
				//dir4
				new CoordDir(1, 0, 0, 4),	new CoordDir(0,0,0,2),  new CoordDir(1, 0, 1, 3), 
				new CoordDir(0, 1, 0, 4),	new CoordDir(0,0,0,0),  new CoordDir(0, 1, 1, 1), 
				new CoordDir(-1,0, 0, 4),	new CoordDir(0,0,0,3),  new CoordDir(-1,0, 1, 2), 
				new CoordDir(0,-1, 0, 4),	new CoordDir(0,0,0,1),  new CoordDir(0,-1, 1, 0), 
				//dir5
				new CoordDir(1, 0, 0, 5),	new CoordDir(0,0,0,2),  new CoordDir(1, 0,-1, 3), 
				new CoordDir(0,-1, 0, 5),	new CoordDir(0,0,0,1),  new CoordDir(0,-1,-1, 0), 
				new CoordDir(-1,0, 0, 5),	new CoordDir(0,0,0,3),  new CoordDir(-1,0,-1, 2), 
				new CoordDir(0, 1, 0, 5),	new CoordDir(0,0,0,0),  new CoordDir(0, 1,-1, 1) 
			};

		#endregion

	}

	[System.Serializable]
	public struct CoordCube
	{
		public CoordDir offset;
		public CoordDir size;

		public CoordCube (CoordDir offset, CoordDir size) { this.offset = offset; this.size = size; }
		public CoordCube (int offsetX, int offsetY, int offsetZ, int sizeX, int sizeY, int sizeZ) { this.offset = new CoordDir(offsetX,offsetY,offsetZ,7); this.size = new CoordDir(sizeX,sizeY,sizeZ,7);  }

		public CoordDir Max { get { return offset+size; } set { offset = value-size; } }
		public CoordDir Min { get { return offset; } set { offset = value; } }
		public CoordDir Center { get { return offset + size/2; } } 

		public static bool operator == (CoordCube c1, CoordCube c2) { return c1.offset.x==c2.offset.x && c1.offset.y==c2.offset.y && c1.offset.z==c2.offset.z && c1.size.x==c2.size.x && c1.size.y==c2.size.y && c1.size.z==c2.size.z; }
		public static bool operator != (CoordCube c1, CoordCube c2) { return c1.offset.x!=c2.offset.x || c1.offset.y!=c2.offset.y || c1.offset.z!=c2.offset.z || c1.size.x!=c2.size.x || c1.size.y!=c2.size.y || c1.size.z!=c2.size.z; }
		public override bool Equals(object obj) { return base.Equals(obj); }
		public override int GetHashCode() {return offset.GetHashCode()^size.GetHashCode(); }

		public static CoordCube operator * (CoordCube c, int s) { return  new CoordCube(c.offset*s, c.size*s); }
		public static CoordCube operator / (CoordCube c, int s) { return  new CoordCube(c.offset/s, c.size/s); }

		public int this[int x, int y, int z] { get { return (z-offset.z)*size.x*size.y + (y-offset.y)*size.x + x - offset.x; }}
		public int this[CoordDir c] { get { return (c.z-offset.z)*size.x*size.y + (c.y-offset.y)*size.x + c.x - offset.x; }}

		public bool Contains (CoordDir c)
		{
			return  c.x>=offset.x && c.x<offset.x+size.x && 
					c.y>=offset.y && c.y<offset.y+size.y && 
					c.z>=offset.z && c.z<offset.z+size.z ;			
		}

		public bool Contains (int x, int y, int z)
		{
			return  x>=offset.x && x<offset.x+size.x && 
					y>=offset.y && y<offset.y+size.y && 
					z>=offset.z && z<offset.z+size.z ;			
		}

		public int GetPos (int x, int y, int z)
		{
			#if WDEBUG
			if (x<offset.x || x>=offset.x+size.x) throw new System.ArgumentOutOfRangeException("x", "Index Out Of Range (" + offset.x + "-" + (offset.x+size.x) +"): " + x);
			if (y<offset.y || y>=offset.y+size.y) throw new System.ArgumentOutOfRangeException("y", "Index Out Of Range (" + offset.y + "-" + (offset.y+size.y) +"): " + y);
			if (z<offset.z || z>=offset.z+size.z) throw new System.ArgumentOutOfRangeException("z", "Index Out Of Range (" + offset.z + "-" + (offset.z+size.z) +"): " + z);
			#endif
					
			return (z-offset.z)*size.x*size.y + (y-offset.y)*size.x + x - offset.x;
		}

		public int GetPos (CoordDir c)
		{
			#if WDEBUG
			if (c.x<offset.x || c.x>=offset.x+size.x) throw new System.ArgumentOutOfRangeException("x", "Index Out Of Range (" + offset.x + "-" + (offset.x+size.x) +"): " + c.x);
			if (c.y<offset.y || c.y>=offset.y+size.y) throw new System.ArgumentOutOfRangeException("y", "Index Out Of Range (" + offset.y + "-" + (offset.y+size.y) +"): " + c.y);
			if (c.z<offset.z || c.z>=offset.z+size.z) throw new System.ArgumentOutOfRangeException("z", "Index Out Of Range (" + offset.z + "-" + (offset.z+size.z) +"): " + c.z);
			#endif
					
			return (c.z-offset.z)*size.x*size.y + (c.y-offset.y)*size.x + c.x - offset.x;
		}

		public void Encapsulate (CoordDir coord)
		/// Resizes this rect so that coord is included
		{
			if (coord.x < offset.x) { size.x += offset.x-coord.x; offset.x = coord.x; }
			if (coord.x >= offset.x+size.x) { size.x = coord.x-offset.x+1; }

			if (coord.y < offset.y) { size.y += offset.y-coord.y; offset.y = coord.y; }
			if (coord.y >= offset.y+size.y) { size.y = coord.y-offset.y+1; }

			if (coord.z < offset.z) { size.z += offset.z-coord.z; offset.z = coord.z; }
			if (coord.z >= offset.z+size.z) { size.z = coord.z-offset.z+1; }
		}

		public CoordRect rect {get{ return new CoordRect(offset.coord, size.coord); }}
	}

	[System.Serializable]
	public class Matrix3D<T>
	{
		public CoordCube cube;
		public T[] array;
		public int count;
		
		/*public int sizeX;
		public int sizeY;
		public int sizeZ;
		
		public int offsetX;
		public int offsetY;
		public int offsetZ;*/
		
		//public int pos;
		
		public T this[int x, int y, int z] 
		{ 
			get { 
				#if WDEBUG
				if (x<cube.offset.x || x>=cube.offset.x+cube.size.x) throw new System.ArgumentOutOfRangeException("x", "Index Out Of Range (" + cube.offset.x + "-" + (cube.offset.x+cube.size.x) +"): " + x);
				if (y<cube.offset.y || y>=cube.offset.y+cube.size.y) throw new System.ArgumentOutOfRangeException("y", "Index Out Of Range (" + cube.offset.y + "-" + (cube.offset.y+cube.size.y) +"): " + y);
				if (z<cube.offset.z || z>=cube.offset.z+cube.size.z) throw new System.ArgumentOutOfRangeException("z", "Index Out Of Range (" + cube.offset.z + "-" + (cube.offset.z+cube.size.z) +"): " + z);
				#endif
					
				return array[(z-cube.offset.z)*cube.size.x*cube.size.y + (y-cube.offset.y)*cube.size.x + x - cube.offset.x]; }
			
			set { 
				#if WDEBUG
				if (x<cube.offset.x || x>=cube.offset.x+cube.size.x) throw new System.ArgumentOutOfRangeException("x", "Index Out Of Range (" + cube.offset.x + "-" + (cube.offset.x+cube.size.x) +"): " + x);
				if (y<cube.offset.y || y>=cube.offset.y+cube.size.y) throw new System.ArgumentOutOfRangeException("y", "Index Out Of Range (" + cube.offset.y + "-" + (cube.offset.y+cube.size.y) +"): " + y);
				if (z<cube.offset.z || z>=cube.offset.z+cube.size.z) throw new System.ArgumentOutOfRangeException("z", "Index Out Of Range (" + cube.offset.z + "-" + (cube.offset.z+cube.size.z) +"): " + z);
				#endif
				
				array[(z-cube.offset.z)*cube.size.x*cube.size.y + (y-cube.offset.y)*cube.size.x + x - cube.offset.x] = value; }
		}

		public T this[CoordDir c] 
		{ 
			get { 
				#if WDEBUG
				if (c.x<cube.offset.x || c.x>=cube.offset.x+cube.size.x) throw new System.ArgumentOutOfRangeException("c", "Index Out Of Range (" + cube.offset.x + "-" + cube.offset.x+cube.size.x +"): " + c.x);
				if (c.y<cube.offset.y || c.y>=cube.offset.y+cube.size.y) throw new System.ArgumentOutOfRangeException("c", "Index Out Of Range (" + cube.offset.y + "-" + cube.offset.y+cube.size.y +"): " + c.y);
				if (c.z<cube.offset.z || c.z>=cube.offset.z+cube.size.z) throw new System.ArgumentOutOfRangeException("c", "Index Out Of Range (" + cube.offset.z + "-" + cube.offset.z+cube.size.z +"): " + c.z);
				#endif
					
				return array[(c.z-cube.offset.z)*cube.size.x*cube.size.y + (c.y-cube.offset.y)*cube.size.x + c.x - cube.offset.x]; }
			
			set { 
				#if WDEBUG
				if (c.x<cube.offset.x || c.x>=cube.offset.x+cube.size.x) throw new System.ArgumentOutOfRangeException("x", "Index Out Of Range (" + cube.offset.x + "-" + cube.offset.x+cube.size.x +"): " + c.x);
				if (c.y<cube.offset.y || c.y>=cube.offset.y+cube.size.y) throw new System.ArgumentOutOfRangeException("y", "Index Out Of Range (" + cube.offset.y + "-" + cube.offset.y+cube.size.y +"): " + c.y);
				if (c.z<cube.offset.z || c.z>=cube.offset.z+cube.size.z) throw new System.ArgumentOutOfRangeException("z", "Index Out Of Range (" + cube.offset.z + "-" + cube.offset.z+cube.size.z +"): " + c.z);
				#endif
				
				array[(c.z-cube.offset.z)*cube.size.x*cube.size.y + (c.y-cube.offset.y)*cube.size.x + c.x - cube.offset.x] = value; }
		}
			/*
				try {array[(z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX] = value; }
				catch(System.Exception ex) { Debug.Log("offsetX:" + offsetX + " sizeX:" + sizeX + 
					"offsetY:" + offsetY + " sizeY:" + sizeY + 
					"offsetZ:" + offsetZ + " sizeZ:" + sizeZ +
					"   coords:" + x + ", " + y + ", " + z); throw; }
				}
			/*
			get 
			{ 
				if (x-offsetX < 0) Debug.LogError("Value of x (" + x.ToString() + ") is less then offset (" + offsetX.ToString() + ")" );
				if (y-offsetY < 0) Debug.LogError("Value of y (" + y.ToString() + ") is less then offset (" + offsetY.ToString() + ")" );
				if (z-offsetZ < 0) Debug.LogError("Value of z (" + z.ToString() + ") is less then offset (" + offsetZ.ToString() + ")" );
				if (x-offsetX >= sizeX) Debug.LogError("Value of x (" + x.ToString() + ") is equal or more then size (" + sizeX.ToString() + ") + offset(" + offsetX.ToString() + ")" );
				if (y-offsetY >= sizeY) Debug.LogError("Value of y (" + y.ToString() + ") is equal or more then size (" + sizeY.ToString() + ") + offset(" + offsetY.ToString() + ")" );
				if (z-offsetZ >= sizeZ) Debug.LogError("Value of z (" + z.ToString() + ") is equal or more then size (" + sizeZ.ToString() + ") + offset(" + offsetZ.ToString() + ")" );
				return array[(z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX]; 
			}
			*/
		
		/*public int GetPos (int x,int y,int z) { return (z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX; } 
		public void SetPos (int x, int y, int z) { pos = (z-offsetZ)*sizeX*sizeY + (y-offsetY)*sizeX + x - offsetX; } 
			//if (pos>=array.Length) Debug.Log("pos:" + x + " " + y + " " + z + " offset:" + offsetX + " " + offsetY + " " + offsetZ + " size:" + sizeX + " " + sizeY + " " + " " + sizeZ + " offsetSize:" + (sizeX+offsetX) + " " + (sizeY+offsetY) + " " + (sizeZ+offsetZ));}
		public void MovePos (int x, int y, int z) { pos += z*sizeX*sizeY + y*sizeX + x; }
		public void MovePosNextY () { pos += sizeX; }
		public void MovePosPrevY () { pos -= sizeX; }
		
		public T current { get { return array[pos]; } 			set { array[pos] = value; } }
		public T nextX { get { return array[pos+1]; } 			set { array[pos+1] = value; }  }
		public T prevX { get { return array[pos-1]; } 			set { array[pos-1] = value; }  }
		public T nextY { get { return array[pos+sizeX]; }		set { array[pos+sizeX] = value; }  }
		public T prevY { get { return array[pos-sizeX]; } 		set { array[pos-sizeX] = value; }  }
		public T nextZ { get { return array[pos+sizeX*sizeY]; } set { array[pos+sizeX*sizeY] = value; }  }
		public T prevZ { get { return array[pos-sizeX*sizeY]; } set { array[pos-sizeX*sizeY] = value; }  }
		public T nextXnextY { get { return array[pos+1+sizeX]; } set { array[pos+1+sizeX] = value; } }
		public T prevXnextY { get { return array[pos-1+sizeX]; } set { array[pos-1+sizeX] = value; } }
		public T nextZnextY { get { return array[pos+sizeX*sizeY+sizeX]; } set { array[pos+sizeX*sizeY+sizeX] = value; } }
		public T prevZnextY { get { return array[pos-sizeX*sizeY+sizeX]; } set { array[pos-sizeX*sizeY+sizeX] = value; } }
		public T nextXprevY { get { return array[pos+1-sizeX]; } set { array[pos+1-sizeX] = value; } }
		public T prevXprevY { get { return array[pos-1-sizeX]; } set { array[pos-1-sizeX] = value; } }
		public T nextZprevY { get { return array[pos+sizeX*sizeY-sizeX]; } set { array[pos+sizeX*sizeY-sizeX] = value; } }
		public T prevZprevY { get { return array[pos-sizeX*sizeY-sizeX]; } set { array[pos-sizeX*sizeY-sizeX] = value; } }*/
		
		/*
		public T SafeGet (int x, int y, int z)
		{
			x = Mathf.Clamp(x, 0,sizeX);
			y = Mathf.Clamp(y, 0,sizeY);
			z = Mathf.Clamp(z, 0,sizeZ);
			return array[z*sizeX*sizeY + y*sizeX + x];
		}
		*/

		public Matrix3D () { }
		
		public Matrix3D (CoordCube cube, T[] array=null)
		{
			this.cube = cube;
			count = cube.size.x * cube.size.y * cube.size.z;

			if (array != null && array.Length<count) Debug.LogError("Array length: " + array.Length + " is lower then matrix capacity: " + count);
			if (array != null && array.Length>=count) this.array = array;
			else this.array = new T[count];
		}

		public Matrix3D (CoordDir offset, CoordDir size, T[] array=null)
		{
			cube = new CoordCube(offset,size);
			count = cube.size.x * cube.size.y * cube.size.z;

			if (array != null && array.Length<count) Debug.LogError("Array length: " + array.Length + " is lower then matrix capacity: " + count);
			if (array != null && array.Length>=count) this.array = array;
			else this.array = new T[count];
		}

		public Matrix3D (int x, int y, int z, T[] array=null)
		{
			this.cube = new CoordCube(0,0,0,x,y,z);
			count = cube.size.x * cube.size.y * cube.size.z;

			if (array != null && array.Length<count) Debug.LogError("Array length: " + array.Length + " is lower then matrix capacity: " + count);
			if (array != null && array.Length>=count) this.array = array;
			else this.array = new T[count];
		}

		public Matrix3D (int ox, int oy, int oz, int sx, int sy, int sz, T[] array=null)
		{
			this.cube = new CoordCube(ox,oy,oz,sx,sy,sz);
			count = cube.size.x * cube.size.y * cube.size.z;

			if (array != null && array.Length<count) Debug.LogError("Array length: " + array.Length + " is lower then matrix capacity: " + count);
			if (array != null && array.Length>=count) this.array = array;
			else this.array = new T[count];
		}

		public void Fill(T def)
		{
			for(int i=0;i<array.Length;i++) array[i] = def;
		}

		public Matrix3D<T> Copy ()
		{
			Matrix3D<T> newMatrix = new Matrix3D<T>(cube);
			for (int i=0; i<array.Length; i++) newMatrix.array[i] = array[i];
			return newMatrix;
		}

		public Matrix2D<T> Matrix2 
		{get{ 
			#if WDEBUG
			if (cube.size.y!=1) Debug.LogError("Trying to convert non-flat Matrix3 to Matrix2");
			#endif
			return new Matrix2D<T>( new CoordRect(cube.offset.x, cube.offset.z, cube.size.x, cube.size.z), array);
		}}

		public static void CopyData (Matrix3D<T> src, Matrix3D<T> dst)
		{
			for (int x=dst.cube.offset.x; x<dst.cube.offset.x+dst.cube.size.x; x++)
				for (int y=dst.cube.offset.y; y<dst.cube.offset.y+dst.cube.size.y; y++)
					for (int z=dst.cube.offset.z; z<dst.cube.offset.z+dst.cube.size.z; z++)
					{
						if (src.cube.Contains(x,y,z))
							dst[x,y,z] = src[x,y,z];
					}
		}

		/*public void InsertLayer (int x, int z, int start, int depth, T val)
		{
			int min = offsetY; if (start>min) min=start;
			int max = offsetY+sizeY; if (start+depth<max) max=start+depth;

			int i = (z-offsetZ)*sizeX*sizeY + (min-offsetY)*sizeX + x - offsetX;

			for (int y=min; y<max; y++)
			{
				array[i] = val;
				i += sizeX;
			}
		}

		public static Matrix3<T> Rescale (Matrix3<T> src, int offsetX, int offsetY, int offsetZ, int sizeX, int sizeY, int sizeZ)
		{
			Matrix3<T> dst = new Matrix3<T>(sizeX, sizeY, sizeZ);
			dst.offsetX = offsetX; dst.offsetY = offsetY; dst.offsetZ = offsetZ;

			for (int x=dst.offsetX; x<dst.offsetX+dst.sizeX; x++)
			{
				if (x<src.offsetX || x>=src.offsetX+src.sizeX) continue;
				
				for (int y=dst.offsetY; y<dst.offsetY+dst.sizeY; y++)
				{
					if (y<src.offsetY || y>=src.offsetY+src.sizeY) continue;
					
					for (int z=dst.offsetZ; z<dst.offsetZ+dst.sizeZ; z++)
					{
						if (z<src.offsetZ || z>=src.offsetZ+src.sizeZ) continue;
						
						//dst[x,y,z] = src[x,y,z];
						dst.array[(z-dst.offsetZ)*dst.sizeX*dst.sizeY + (y-dst.offsetY)*dst.sizeX + x - dst.offsetX] = src.array[(z-src.offsetZ)*src.sizeX*src.sizeY + (y-src.offsetY)*src.sizeX + x - src.offsetX];
					}
				}
			}
			return dst;
		}*/
	}
	
/*	[System.Serializable]
	public struct Matrix4<T>
	{
		public T[] array; //must be private
		
		public int sizeX;
		public int sizeY;
		public int sizeZ;
		public int sizeW;

		public int stepZ;
		public int stepW;
		
		public int offsetX;
		public int offsetY;
		public int offsetZ;
		public int offsetW;
		
		public int pos;
		
		public T this[int x, int y, int z, int w] 
		{
			get { return array[(w-offsetW)*stepW + (z-offsetZ)*stepZ + (y-offsetY)*sizeX + x - offsetX]; }
			set { array[(w-offsetW)*stepW + (z-offsetZ)*stepZ + (y-offsetY)*sizeX + x - offsetX] = value; }
		}
		
		public bool CheckInRange (int x, int y, int z, int w)
		{
			return (x-offsetX >= 0 && x-offsetX < sizeX &&
			        y-offsetY >= 0 && y-offsetY < sizeY &&
			        z-offsetZ >= 0 && z-offsetZ < sizeZ &&
			        w-offsetW >= 0 && w-offsetW < sizeW);
		}
		
		public bool CheckInRange (int x, int y, int z)
		{
			return (x-offsetX >= 0 && x-offsetX < sizeX &&
			        y-offsetY >= 0 && y-offsetY < sizeY &&
			        z-offsetZ >= 0 && z-offsetZ < sizeZ);
		}
		
		public void SetPos (int x, int y, int z, int w) { pos = (w-offsetW)*stepW + (z-offsetZ)*stepZ + (y-offsetY)*sizeX + x - offsetX; }
		public void MovePos (int x, int y, int z, int w) { pos += stepW + z*stepZ + y*sizeX + x; }
		public void MovePosNextY ()  { pos += sizeX; }
		
		public T current { get { return array[pos]; } 			set { array[pos] = value; } }
		public T nextX { get { return array[pos+1]; } 			set { array[pos+1] = value; }  }
		public T prevX { get { return array[pos-1]; } 			set { array[pos-1] = value; }  }
		public T nextY { get { return array[pos+sizeX]; }		set { array[pos+sizeX] = value; }  }
		public T prevY { get { return array[pos-sizeX]; } 		set { array[pos-sizeX] = value; }  }
		public T nextZ { get { return array[pos+stepZ]; } set { array[pos+stepZ] = value; }  }
		public T prevZ { get { return array[pos-stepZ]; } set { array[pos-stepZ] = value; }  }
		public T nextW { get { return array[pos+stepZ]; } set { array[pos+stepW] = value; }  }
		public T prevW { get { return array[pos-stepZ]; } set { array[pos-stepW] = value; }  }
		public T nextXnextY { get { return array[pos+1+sizeX]; } set { array[pos+1+sizeX] = value; } }
		public T prevXnextY { get { return array[pos-1+sizeX]; } set { array[pos-1+sizeX] = value; } }
		public T nextZnextY { get { return array[pos+stepZ+sizeX]; } set { array[pos+stepZ+sizeX] = value; } }
		public T prevZnextY { get { return array[pos-stepZ+sizeX]; } set { array[pos-stepZ+sizeX] = value; } }
		
		public Matrix4 (int x, int y, int z, int w)
		{
			array = new T[w*x*y*z];
			sizeX = x;
			sizeY = y;
			sizeZ = z;
			sizeW = w;
			offsetX = 0;
			offsetY = 0;
			offsetZ = 0;
			offsetW = 0;
			pos = 0;
			stepW = sizeX*sizeY*sizeZ;
			stepZ = sizeX*sizeY;
		}
		
		public void Reset (T def)
		{
			if (array == null) array = new T[0];
			for (int i=0; i<array.Length; i++) array[i] = def;
		}
	}*/
	

}


