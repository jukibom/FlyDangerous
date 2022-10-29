using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Den.Tools 
{
	[Serializable, StructLayout (LayoutKind.Sequential)]
	public class Matrix2D<T>// : ICloneable
	{
		public CoordRect rect; //never assign it's size manually, use ChangeRect
		public int count;
		public int pos;
		public T[] arr;
		
		public const bool native = true;
		 //rect.size.x*rect.size.z, not a property for faster access

		#region Creation

			public Matrix2D () {}

			public Matrix2D (int x, int z, T[] array=null)
			{
				rect = new CoordRect(0,0,x,z);
				count = x*z;
				if (array != null && array.Length<count) Debug.LogError("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.arr = array;
				else this.arr = new T[count];
			}
		
			public Matrix2D (CoordRect rect, T[] array=null)
			{
				this.rect = rect;
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.arr = array;
				else this.arr = new T[count];
			}

			public Matrix2D (Coord offset, Coord size, T[] array=null)
			{
				rect = new CoordRect(offset, size);
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.arr = array;
				else this.arr = new T[count];
			}

			public Matrix2D (Matrix2D<T> src)
			{
				rect = src.rect;
				count = src.count;
				arr = new T[count];
				for (int i=0; i<arr.Length; i++)
					arr[i] = src.arr[i];
			}

		#endregion
		
		public T this[int x, int z] 
		{
			get { return arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; } //rect fn duplicated to increase performance
			set { arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x] = value; }
		}

		public T this[float x, float z] 
		///Floors coordinates and gets value. To get interpolated value use GetInterpolated
		{
			get{ 
				int ix = (int)(x); if (x<1) ix--;
				int iz = (int)(z); if (z<1) iz--;
				return arr[(iz-rect.offset.z)*rect.size.x + ix - rect.offset.x]; 
			}
			set{ 
				int ix = (int)(x); if (x<1) ix--;
				int iz = (int)(z); if (z<1) iz--;
				arr[(iz-rect.offset.z)*rect.size.x + ix - rect.offset.x] = value; 
			}
		}

		public T this[Coord c] 
		{
			get { return arr[(c.z-rect.offset.z)*rect.size.x + c.x - rect.offset.x]; }
			set { arr[(c.z-rect.offset.z)*rect.size.x + c.x - rect.offset.x] = value; }
		}

		public T CheckGet (int x, int z) 
		{ 
			if (x>=rect.offset.x && x<rect.offset.x+rect.size.x && z>=rect.offset.z && z<rect.offset.z+rect.size.z)
				return arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; 
			else return default(T);
		} 

		/*public T this[Vector3 pos]
		{
			get { return array[((int)pos.z-rect.offset.z)*rect.size.x + (int)pos.x - rect.offset.x]; }
			set { array[((int)pos.z-rect.offset.z)*rect.size.x + (int)pos.x - rect.offset.x] = value; }
		}*/

		public T this[Vector2 pos]
		{
			get{ 
				int posX = (int)(pos.x + 0.5f); if (pos.x < 0) posX--;
				int posZ = (int)(pos.y + 0.5f); if (pos.y < 0) posZ--;
				return arr[(posZ-rect.offset.z)*rect.size.x + posX - rect.offset.x]; 
			}
			set{
				int posX = (int)(pos.x + 0.5f); if (pos.x < 0) posX--;
				int posZ = (int)(pos.y + 0.5f); if (pos.y < 0) posZ--;
				arr[(posZ-rect.offset.z)*rect.size.x + posX - rect.offset.x] = value; 
			}
		}

		public int Pos (Coord coord) { return (coord.z-rect.offset.z)*rect.size.x + coord.x - rect.offset.x; }
		public int Pos (int posX, int posZ) { return (posZ-rect.offset.z)*rect.size.x + posX - rect.offset.x; }

		public Coord CoordByNum (int num) 
		{
			int z = num / rect.size.x;
			int x = num - z*rect.size.x;
			return new Coord(x+rect.offset.x, z+rect.offset.z);
		}

		public T GetRaw(int x, int z)
		/// Gets value without using an offset
		{
			return arr[z*rect.size.x + x]; //rect fn duplicated to increase performance
		}

		public void SetRaw (int x, int z, T value)
		/// Sets value without offset
		{
			arr[z*rect.size.x + x] = value;
		}

		public void Clear () { for (int i=0; i<arr.Length; i++) arr[i] = default(T); }

		public void ChangeRect (CoordRect newRect, bool forceNewArray=false) //will re-create array only if capacity changed
		{
			rect = newRect;
			count = newRect.size.x*newRect.size.z;

			if (arr.Length!=count || forceNewArray) arr = new T[count];
		}

		public virtual object Clone () { return Clone(null); } //separate fn for IClonable
		public Matrix2D<T> Clone (Matrix2D<T> result)
		{
			if (result==null) result = new Matrix2D<T>(rect);
			
			//copy params
			result.rect = rect;
			result.pos = pos;
			result.count = count;
			
			//copy array
			//result.array = (float[])array.Clone(); //no need to create it any time
			if (result.arr.Length != arr.Length) result.arr = new T[arr.Length];
			for (int i=0; i<arr.Length; i++)
				result.arr[i] = arr[i];

			return result;
		}

		public void Fill (T v) { for (int i=0; i<count; i++) arr[i] = v; }

		public void Fill (Matrix2D<T> m, bool removeBorders=false)
		{
			CoordRect intersection = CoordRect.Intersected(rect, m.rect);
			Coord min = intersection.Min; Coord max = intersection.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
					this[x,z] = m[x,z];
			if (removeBorders) RemoveBorders(intersection);
		}

		#region Quick Pos

			public void SetPos(int x, int z) { pos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x; }
			public void SetPos(int x, int z, int s) { pos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x  +  s*rect.size.x*rect.size.z; }

			public void MoveX() { pos++; }
			public void MoveZ() { pos += rect.size.x; }
			public void MovePrevX() { pos--; }
			public void MovePrevZ() { pos -= rect.size.x; }

			//public float current { get { return array[pos]; } set { array[pos] = value; } }
			/*public T nextX { get { return array[pos+1]; } set { array[pos+1] = value; } }
			public T prevX { get { return array[pos-1]; } set { array[pos-1] = value; } }
			public T nextZ { get { return array[pos+rect.size.x]; } set { array[pos+rect.size.x] = value; } }
			public T prevZ { get { return array[pos-rect.size.x]; } set { array[pos-rect.size.x] = value; } }
			public T nextXnextZ { get { return array[pos+rect.size.x+1]; } set { array[pos+rect.size.x+1] = value; } }
			public T prevXnextZ { get { return array[pos+rect.size.x-1]; } set { array[pos+rect.size.x-1] = value; } }
			public T nextXprevZ { get { return array[pos-rect.size.x+1]; } set { array[pos-rect.size.x+1] = value; } }
			public T prevXprevZ { get { return array[pos-rect.size.x-1]; } set { array[pos-rect.size.x-1] = value; } }*/

		#endregion

		#region OrderedFromCenter

			/*public Coord[] GetOrderedFromCenterCoords ()
			{
				Coord[] sortedByDistance = new Coord[array.Length];
				int i=0;
				Coord min = rect.Min; Coord max = rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						{ sortedByDistance[i] = new Coord(x,z); i++; }

				float[] distances = new float[array.Length];
				for (int z=0; z<rect.size.z; z++)
					for (int x=0; x<rect.size.x; x++)
						distances[z*rect.size.x + x] = (x-rect.size.x/2)*(x-rect.size.x/2) + (z-rect.size.z/2)*(z-rect.size.z/2); //Mathf.Max( Mathf.Abs(x-chunks.rect.size.x/2), Mathf.Abs(z-chunks.rect.size.z/2) );

				Extensions.ArrayQSort(sortedByDistance, distances);
				return sortedByDistance;
			}

			public IEnumerable<Coord> OrderedFromCenterCoord ()
			{
				Coord[] sortedByDistance = GetOrderedFromCenterCoords();
				for (int i=0; i<sortedByDistance.Length; i++)
					yield return sortedByDistance[i];
			}

			public IEnumerable<T> OrderedFromCenter ()
			{
				Coord[] sortedByDistance = GetOrderedFromCenterCoords();
				for (int i=0; i<sortedByDistance.Length; i++)
					yield return this[sortedByDistance[i]];
			}*/

		#endregion

		#region Borders

			public void RemoveBorders ()
			{
				Coord min = rect.Min; Coord last = rect.Max - 1;
			
				for (int x=min.x; x<=last.x; x++)
					{ SetPos(x,min.z); arr[pos] = arr[pos+rect.size.x]; }

				for (int x=min.x; x<=last.x; x++)
					{ SetPos(x,last.z); arr[pos] = arr[pos-rect.size.x]; }

				for (int z=min.z; z<=last.z; z++)
					{ SetPos(min.x,z); arr[pos] = arr[pos+1]; }

				for (int z=min.z; z<=last.z; z++)
					{ SetPos(last.x,z); arr[pos] = arr[pos-1]; }
			}

			public void RemoveBorders (int borderMinX, int borderMinZ, int borderMaxX, int borderMaxZ)
			{
				Coord min = rect.Min; Coord max = rect.Max;
			
				if (borderMinZ != 0)
				for (int x=min.x; x<max.x; x++)
				{
					T val = this[x, min.z+borderMinZ];
					for (int z=min.z; z<min.z+borderMinZ; z++) this[x,z] = val;
				}

				if (borderMaxZ != 0)
				for (int x=min.x; x<max.x; x++)
				{
					T val = this[x, max.z-borderMaxZ];
					for (int z=max.z-borderMaxZ; z<max.z; z++) this[x,z] = val;
				}

				if (borderMinX != 0)
				for (int z=min.z; z<max.z; z++)
				{
					T val = this[min.x+borderMinX, z];
					for (int x=min.x; x<min.x+borderMinX; x++) this[x,z] = val;
				}
				
				if (borderMaxX != 0)
				for (int z=min.z; z<max.z; z++)
				{
					T val = this[max.x-borderMaxX, z];
					for (int x=max.x-borderMaxX; x<max.x; x++) this[x,z] = val;
				}
			}

			public void RemoveBorders (CoordRect centerRect)
			{ 
				RemoveBorders(
					Mathf.Max(0,centerRect.offset.x-rect.offset.x), 
					Mathf.Max(0,centerRect.offset.z-rect.offset.z), 
					Mathf.Max(0,rect.Max.x-centerRect.Max.x+1), 
					Mathf.Max(0,rect.Max.z-centerRect.Max.z+1) ); 
			}

		#endregion
	}

}


