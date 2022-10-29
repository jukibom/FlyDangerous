//using System; //conflicts with UnityEngine.Object
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools.Matrices
{
	//[System.Serializable]
	public partial class MatrixSet
	{
		public CoordRect rect;  //storing for 0 length

		public Vector3 worldPos;
		public Vector3 worldSize;
		public Vector3 WorldMax => worldPos+worldSize;
		public Vector2D PixelSize => new Vector2D(worldSize.x/(rect.size.x-1), worldSize.z/(rect.size.z-1));

		private DictionaryOrdered<Prototype,Matrix> prototypesMatrices = new DictionaryOrdered<Prototype,Matrix>();

		public enum PrototypeType { Texture, Prefab, TerrainLayer };

		public MatrixSet (CoordRect rect, Vector3 worldPos, Vector3 worldSize)
		{
			this.rect = rect;
			this.worldPos = worldPos;
			this.worldSize = worldSize;
		}

		public MatrixSet (CoordRect rect, Vector2D worldPos, Vector2D worldSize, float height)
		{
			this.rect = rect;
			this.worldPos = new Vector3(worldPos.x, 0, worldPos.z);
			this.worldSize = new Vector3(worldSize.x, height, worldSize.z);
		}

		public MatrixSet (MatrixSet src)
		{
			rect = src.rect;
			worldPos = src.worldPos;
			worldSize = src.worldSize;

			foreach (var kvp in src.prototypesMatrices)
				prototypesMatrices.Add(kvp.Key, new Matrix(kvp.Value));
		}

		public MatrixSet (CoordRect rect, Vector3 worldPos, Vector3 worldSize, ICollection<Prototype> prototypes)
		{
			this.rect = rect;
			this.worldPos = worldPos;
			this.worldSize = worldSize;

			foreach (Prototype prototype in prototypes)
				prototypesMatrices.Add(prototype, new Matrix(rect));
		}


		public Matrix this[Prototype prototype] 
		{ 
			get{
				if (prototypesMatrices.TryGetValue(prototype, out Matrix matrix))
					return matrix;
				else
					return null;
			}

			set{
				if (value.rect != rect)
					throw new System.Exception("Trying to add a matrix of different resolution");

				if (prototypesMatrices.ContainsKey(prototype))
					prototypesMatrices[prototype] = value;
				else
					prototypesMatrices.Add(prototype, value);
			}
		}

		public Matrix this[int num]
		{
			get => prototypesMatrices[num];
			set => prototypesMatrices[num] = value;
		}


		public Matrix GetMatrixByNum (int num) => prototypesMatrices[num];
		public void SetMatrixByNum (int num, Matrix value) => prototypesMatrices[num] = value;

		public Prototype GetPrototypeByNum (int num) => prototypesMatrices.GetKeyByNum(num);

		public int Count => prototypesMatrices.Count;


		public ICollection<Prototype> Prototypes => prototypesMatrices.Keys;
		public ICollection<Matrix> Matrices => prototypesMatrices.Values;

		public IEnumerable<T> PrototypesOfType<T> () where T: class
		{
			foreach (object obj in prototypesMatrices.Keys)
				if (obj is T tobj) yield return tobj;
		}

		public bool TryGetValue (Prototype prototype, out Matrix matrix) => prototypesMatrices.TryGetValue(prototype, out matrix);
		public bool TryGetValue (System.Predicate<object> predicate, out Matrix matrix) 
		{
			foreach (var kvp in prototypesMatrices)
				if (predicate(kvp.Key))
				{
					matrix = kvp.Value;
					return true;
				}

			matrix = null;
			return false;
		}

		public void SetOffset (Coord newOffset)
		{
			rect.offset = newOffset;
			foreach (Matrix im in prototypesMatrices.Values)
				im.rect.offset = newOffset;
		}

		public void Append (Matrix matrix, Prototype prototype, bool normalized=true)
		///Adds new channel (if no prototype in dict) or appends existing one (if protoptye already added)
		///InvMultiplies all other channels making result normalized
		{
			if (normalized)
			{
				foreach (Matrix im in prototypesMatrices.Values)
					im.MultiplyInv(matrix);
			}

			if (prototypesMatrices.TryGetValue(prototype, out Matrix m))
				m.Add(matrix);
			else
			{
				m = new Matrix(matrix);
				prototypesMatrices.Add(prototype,m);
			}
		}


		public void SyncPrototypes (ICollection<Prototype> prototypes)
		///Adds prototypes from list if they are not added and creates empty matrices for them
		{
			foreach (Prototype prototype in prototypes)
				if (!prototypesMatrices.Contains(prototype))
				{
					Matrix matrix = new Matrix(rect);
					prototypesMatrices.Add(prototype, matrix);
				}
		}


		public void Resize (CoordRect dstRect, bool interpolate=false)
		///Resizes all the matrices and changes rect size
		///Trying to use fast resize when possible
		///Mainly for splats import/export
		{
			DictionaryOrdered<Prototype,Matrix> resizedProtMatrices = new DictionaryOrdered<Prototype,Matrix>();

			foreach (var kvp in prototypesMatrices)
			{
				Matrix src = kvp.Value;
				Matrix dst = new Matrix(dstRect);
					
				ResizeMatrix(src, dst, interpolate);

				resizedProtMatrices.Add(kvp.Key, dst);
			}

			prototypesMatrices = resizedProtMatrices;
			rect = dstRect;
		}

		public static void Resize (MatrixSet src, MatrixSet dst, bool interpolate=false)
		/// instant resise doesn't actually changes src
		{
			foreach (var kvp in src.prototypesMatrices)
			{
				Matrix srcMatrix = kvp.Value;

				Matrix dstMatrix;
				if (!dst.TryGetValue(kvp.Key, out dstMatrix)) //adding matrix if it's not in dst
				{
					dstMatrix = new Matrix(dst.rect);
					dst[kvp.Key] = dstMatrix;
				}
					
				ResizeMatrix(srcMatrix, dstMatrix, interpolate);
			}
		}

		private static void ResizeMatrix (Matrix src, Matrix dst, bool interpolate=false)
		{
			if (interpolate)
			{
				if (src.rect.size.x*2 == dst.rect.size.x)
					MatrixOps.UpscaleFast(src, dst);
				else if (src.rect.size.x == dst.rect.size.x*2)
					MatrixOps.DownscaleFast(src, dst);
				else
					MatrixOps.Resize(src, dst);
			}
			else
				MatrixOps.ResizeNearestNeighbor(src, dst);
		}


		public static void CopyIntersected (MatrixSet src, MatrixSet dst)
		/// Analog of Matrix.Copy / Matrix.Fill, but only works with a set
		/// This will also add new src matrix to dst
		{
			foreach (var kvp in src.prototypesMatrices)
			{
				Prototype prototype = kvp.Key;

				Matrix srcMatrix = kvp.Value;
				Matrix dstMatrix;

				if (!dst.prototypesMatrices.TryGetValue(prototype, out dstMatrix))
				{
					dstMatrix = new Matrix(dst.rect);
					dst.prototypesMatrices.Add(prototype, dstMatrix);
				}
				
				Matrix.CopyIntersected(srcMatrix, dstMatrix);
			}
		}

		public static void CopyResized (MatrixSet src, MatrixSet dst,
			Vector2D srcRectPos, Vector2D srcRectSize, Coord dstRectPos, Coord dstRectSize)
		///Resizes all the matrices and changes rect size
		///Trying to use fast resize when possible
		///Mainly for splats import/export
		{
			foreach (var kvp in src.prototypesMatrices)
			{
				Prototype prototype = kvp.Key;

				Matrix srcMatrix = kvp.Value;
				Matrix dstMatrix;

				if (!dst.prototypesMatrices.TryGetValue(prototype, out dstMatrix))
				{
					dstMatrix = new Matrix(dst.rect);
					dst.prototypesMatrices.Add(prototype, dstMatrix);
				}
				
				Matrix.CopyResized(srcMatrix, dstMatrix, srcRectPos, srcRectSize, dstRectPos, dstRectSize);

			}
		}
	}

}