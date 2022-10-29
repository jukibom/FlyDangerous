using System;
/// Matrix Stripe Operations

using System.Runtime.InteropServices;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Tests")]

namespace Den.Tools.Matrices {
	public static class MatrixOps
	/// All the operations that work with neighbor pixels
	/// The ones that require Stripes
	{
		[Serializable, StructLayout (LayoutKind.Sequential)] //to pass to native
		public class Stripe
		{
			public int length; //in case array is longer (for re-use)
			public float[] arr;

			public Stripe (float[] arr) { this.arr = arr; length = arr.Length; }
			public Stripe (int length) { this.length = length; arr = new float[length]; }
			public Stripe (Stripe stripe) { this.arr = new float[stripe.arr.Length]; Array.Copy(stripe.arr, arr, stripe.arr.Length); length = stripe.length; }

			public void Expand (int newCount) { length = newCount; Array.Resize(ref arr, length); }
			public void Fill (float val) { for (int i=0; i<arr.Length; i++) arr[i] = val; }

			public static void Copy (Stripe src, Stripe dst) { Array.Copy(src.arr, dst.arr, src.length); dst.length = src.length; }
			public static void Swap (Stripe s1, Stripe s2) { float[] t=s1.arr; s1.arr=s2.arr; s2.arr=t; }
		}


		#region Stripe Readings/Writings

			public static void ReadLine (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int ix=0; ix<stripe.length; ix++)
					stripe.arr[ix] = matrix.arr[start+ix];
				//Array.Copy(this.arr, start, stripe.arr, 0, stripe.length);
			}

			public static void WriteLine (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int ix=0; ix<stripe.length; ix++)
					matrix.arr[start+ix] = stripe.arr[ix];
				//Array.Copy(stripe.arr, 0, this.arr, start, stripe.length);
			}

			public static void MaxLine (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int ix=0; ix<stripe.length; ix++)
				{
					float matrixVal = matrix.arr[start+ix];
					float lineVal = stripe.arr[ix];
					if (lineVal>matrixVal) matrix.arr[start+ix] = lineVal;	
				}
			}

			public static void MinLine (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int ix=0; ix<stripe.length; ix++)
				{
					float matrixVal = matrix.arr[start+ix];
					float lineVal = stripe.arr[ix];
					if (lineVal<matrixVal) matrix.arr[start+ix] = lineVal;	
				}
			}

			public static void AddLine (Stripe stripe, Matrix matrix, int x, int z, float opacity = 1) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int ix=0; ix<stripe.length; ix++)
					matrix.arr[start+ix] += stripe.arr[ix]*opacity;
			}

			public static void ReadRow (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
					stripe.arr[iz] = matrix.arr[start + iz*matrix.rect.size.x];
			}

			public static void WriteRow (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
					matrix.arr[start + iz*matrix.rect.size.x] = stripe.arr[iz];
			}

			public static void WriteRow (Stripe stripe, Matrix matrix, int x, int z, float[] mask) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
					matrix.arr[start + iz*matrix.rect.size.x] = stripe.arr[iz] * mask[iz];
			}

			public static void MaxRow (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
				{
					float matrixVal = matrix.arr[start+iz*matrix.rect.size.x];
					float lineVal = stripe.arr[iz];
					if (lineVal>matrixVal) matrix.arr[start+iz*matrix.rect.size.x] = lineVal;
				}
			}

			public static void MinRow (Stripe stripe, Matrix matrix, int x, int z) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
				{
					float matrixVal = matrix.arr[start+iz*matrix.rect.size.x];
					float lineVal = stripe.arr[iz];
					if (lineVal<matrixVal) matrix.arr[start+iz*matrix.rect.size.x] = lineVal;
				}
			}

			public static void AddRow (Stripe stripe, Matrix matrix, int x, int z, float opacity = 1) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
					matrix.arr[start + iz*matrix.rect.size.x] += stripe.arr[iz] * opacity;
			}

			public static void OverlayRow (Stripe stripe, Matrix matrix, int x, int z, float opacity = 1) 
			//Uses stripe values with range -1 - 1
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
				{
					float valA = matrix.arr[start + iz*matrix.rect.size.x];
					float valB = stripe.arr[iz];

					valB *= opacity;

					matrix.arr[start + iz*matrix.rect.size.x] = 2*valA*valB + valA + valB;

					//for range 0-1 just in case
					//if (a > 0.5f) b = 1 - 2*(1-a)*(1-b);
					//else b = 2*a*b;
				}
			}

			public static void MixRow (Matrix dst, Matrix matrix, Matrix matrixMask, Stripe stripe, Stripe stripeMask, int x, int z)
			/// Commutative operation, doesn't matter if row applied to mask or vice versa
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
				{
					int pos = start + iz*matrix.rect.size.x;
					float sum = matrixMask.arr[pos] + stripeMask.arr[iz];

					dst.arr[pos] = sum>0 ?
						(matrix.arr[pos]*matrixMask.arr[pos] + stripe.arr[iz]*stripeMask.arr[iz]) / sum :
						matrix.arr[pos] + stripe.arr[iz];
				}
			}

			public static void MaxRow (Matrix dst, Matrix matrix, Matrix matrixMask, Stripe stripe, Stripe stripeMask, int x, int z)
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;
				for (int iz=0; iz<stripe.length; iz++)
				{
					int pos = start + iz*matrix.rect.size.x;
					dst.arr[pos] = matrixMask.arr[pos] > stripeMask.arr[iz] ?
						matrix.arr[pos] :
						stripe.arr[iz];
				}
			}

			public static void ReadDiagonal (Stripe stripe, Matrix matrix, int x, int z, float stepX = 1, float stepZ = 1) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;

				//clamping stripe length to matrix borders
				float xLength = matrix.rect.size.x + matrix.rect.size.z; 
				if (stepX > 0.001f) xLength = (matrix.rect.offset.x+matrix.rect.size.x - x) / stepX;
				if (stepX < -0.001f) xLength =  (x - matrix.rect.offset.x) / (-stepX);

				float zLength = matrix.rect.size.x + matrix.rect.size.z; 
				if (stepZ > 0.001f) zLength = (matrix.rect.offset.z+matrix.rect.size.z - z) / stepZ;
				if (stepZ < -0.001f) zLength = (z - matrix.rect.offset.z) / (-stepZ);
				
				stripe.length = (int)((xLength<zLength ? xLength : zLength) - 0.5f); //if no 0.5 will exceed the matrix border (checked)

				//reading
				for (int i=0; i<stripe.length; i++)
				{
					int posX = (int)(i*stepX + 0.5f); // if (posX+x<matrix.rect.offset.x  || posX+x>=matrix.rect.offset.x+matrix.rect.size.x) continue;
					int posZ = (int)(i*stepZ + 0.5f); // if (posZ+z<matrix.rect.offset.z  || posZ+z>=matrix.rect.offset.z+matrix.rect.size.z) continue;

					stripe.arr[i] = matrix.arr[start + posX + posZ*matrix.rect.size.x];
				}
			}

			public static void WriteDiagonal (Stripe stripe, Matrix matrix, int x, int z, float stepX = 1, float stepZ = 1) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;

				for (int i=0; i<stripe.length; i++)
				{
					int posX = (int)(i*stepX + 0.5f);  //if (posX+x<matrix.rect.offset.x  || posX+x>=matrix.rect.offset.x+matrix.rect.size.x) continue;
					int posZ = (int)(i*stepZ + 0.5f);  //if (posZ+z<matrix.rect.offset.z  || posZ+z>=matrix.rect.offset.z+matrix.rect.size.z) continue;

					matrix.arr[start + posX + posZ*matrix.rect.size.x] = stripe.arr[i];
				}
			}

			public static void MaxDiagonal (Stripe stripe, Matrix matrix, int x, int z, float stepX = 1, float stepZ = 1) 
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  x;

				for (int i=0; i<stripe.length; i++)
				{
					int posX = (int)(i*stepX + 0.5f);  //if (posX+x<matrix.rect.offset.x  || posX+x>=matrix.rect.offset.x+matrix.rect.size.x) continue;
					int posZ = (int)(i*stepZ + 0.5f); // if (posZ+z<matrix.rect.offset.z  || posZ+z>=matrix.rect.offset.z+matrix.rect.size.z) continue;

					float matrixVal = matrix.arr[start + posX + posZ*matrix.rect.size.x];
					float lineVal = stripe.arr[i];

					if (lineVal>matrixVal) 
						matrix.arr[start + posX + posZ*matrix.rect.size.x] = lineVal;
				}
			}

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ReadInclined")]
			public static extern void ReadInclined(Stripe stripe, Matrix matrix, Vector2 start, Vector2 step);
			#else

			public static void ReadInclined (Stripe stripe, Matrix matrix, Vector2 start, Vector2 step) 
			{
				for (int i=0; i<stripe.length; i++)
				{
					float x = start.x + step.x*i;
					float z = start.y + step.y*i;

					if (x<0) x--;  int ix = (int)(float)(x + 0.5f);
					if (z<0) z--;  int iz = (int)(float)(z + 0.5f);

					if (ix<matrix.rect.offset.x || ix>=matrix.rect.offset.x+matrix.rect.size.x ||
						iz<matrix.rect.offset.z || iz>=matrix.rect.offset.z+matrix.rect.size.z )
							stripe.arr[i] = -0.00001f; //-Mathf.Epsilon;
					else 
					{
						int pos = (iz-matrix.rect.offset.z)*matrix.rect.size.x + ix - matrix.rect.offset.x;
						stripe.arr[i] = matrix.arr[pos];
					}
				}
			}
			#endif

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_WriteInclined")]
			public static extern void WriteInclined(Stripe stripe, Matrix matrix, Vector2 start, Vector2 step);
			#else

			public static void WriteInclined (Stripe stripe, Matrix matrix, Vector2 start, Vector2 step) 
			{
				for (int i=0; i<stripe.length; i++)
				{
					float x = start.x + step.x*i;
					float z = start.y + step.y*i;

					if (x<0) x--;  int ix = (int)(float)(x + 0.5f);
					if (z<0) z--;  int iz = (int)(float)(z + 0.5f);

					if (ix<matrix.rect.offset.x || ix>=matrix.rect.offset.x+matrix.rect.size.x ||
						iz<matrix.rect.offset.z || iz>=matrix.rect.offset.z+matrix.rect.size.z )
							continue;
					else 
					{
						int pos = (iz-matrix.rect.offset.z)*matrix.rect.size.x + ix - matrix.rect.offset.x;
						matrix.arr[pos] = stripe.arr[i];
					}
				}
			}
			#endif

			public static void ReadStrip (Stripe stripe, Matrix matrix, Vector2 start, Vector2 end) 
			{
				Vector2 delta = end-start;
				Vector2 direction = delta.normalized;

				//making any of the step components equal to 1
				Vector2 posDir = new Vector2 (
					(direction.x>0 ? direction.x : -direction.x),
					(direction.y>0 ? direction.y : -direction.y) );
				float max = posDir.x>posDir.y ? posDir.x : posDir.y;
				Vector2 step = direction / max; 

				Vector2 absDelta = new Vector2(delta.x>0 ? delta.x : -delta.x, delta.y>0 ? delta.y : -delta.y);
				int numSteps = (int)(absDelta.x>absDelta.y ? absDelta.x : absDelta.y) + 1;
				stripe.length = numSteps < stripe.length ? numSteps : stripe.length;

				ReadInclined(stripe, matrix, start, step);
			}


			public static void WriteStrip (Stripe stripe, Matrix matrix, Vector2 start, Vector2 end) 
			{
				Vector2 delta = end-start;
				Vector2 direction = delta.normalized;

				//making any of the step components equal to 1
				Vector2 posDir = new Vector2 (
					(direction.x>0 ? direction.x : -direction.x),
					(direction.y>0 ? direction.y : -direction.y) );
				float max = posDir.x>posDir.y ? posDir.x : posDir.y;
				Vector2 step = direction / max; 

				int numSteps = (int)(delta.x>delta.y ? delta.x : delta.y) + 1;
				stripe.length = numSteps < stripe.length ? numSteps : stripe.length;

				WriteInclined(stripe, matrix, start, step);
			}


			public static void ReadSquare (Stripe stripe, Matrix matrix, Coord center, int radius)
			/// Same as circular, but in form of square. 4 lines one-by-one. Useful for blurs and spreads
			{
				int side = radius*2 + 1;
				stripe.length = side*4;

				//resetting line
				for (int i=0; i<side*4; i++)
					stripe.arr[i] = - Mathf.Epsilon;

				Coord min = center-radius;
				Coord max = center+radius;

				Coord rectMin = matrix.rect.offset;
				Coord rectMax = matrix.rect.offset + matrix.rect.size;

				int start = (min.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x;	//matrix[min.x, min.z]
				if (min.z-1 >= rectMin.z  &&  min.z-1 < rectMax.z)
					for (int x=0; x<side; x++)
						if (x+min.x >= rectMin.x  &&  x+min.x < rectMax.x)
							stripe.arr[x] = matrix.arr[start+x];
				
				start = (min.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x;	//matrix[max.x, min.z]
				if (max.x >= rectMin.x  &&  max.x < rectMax.x)
					for (int z=0; z<side; z++)
						if (z+min.z >= rectMin.z  &&  z+min.z < rectMax.z)
							stripe.arr[z+side] = matrix.arr[start + z*matrix.rect.size.x];

				start = (max.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x;	//matrix[max.x-1, max.z]
				if (max.z >= rectMin.z  &&  max.z < rectMax.z)
				for (int x=0; x<side; x++)
					if (max.x-x >= rectMin.x  &&  max.x-x < rectMax.x)
						stripe.arr[x+side*2] = matrix.arr[start -x];

				start = (max.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x;	//matrix[min.x-1, max.z-1]
				if (min.x-1 >= rectMin.x  &&  min.x < rectMax.x)
				for (int z=0; z<side; z++)
					if (max.z-1-z >= rectMin.z  &&  max.z-1-z < rectMax.z)
						stripe.arr[z+side*3] = matrix.arr[start - z*matrix.rect.size.x]; //matrix[min.x, max.z-1-z]

				//closing
				stripe.arr[0] = stripe.arr[stripe.length-1];
			}


			public static void WriteSquare (Stripe stripe, Matrix matrix, Coord center, int radius)
			/// Same as circular, but in form of square. 4 lines one-by-one. Useful for blurs and spreads
			/// Line length should be radius*8 + 4 corners
			{
				int side = radius*2 + 1;
				stripe.length = side*4;

				Coord min = center-radius;
				Coord max = center+radius;

				Coord rectMin = matrix.rect.offset;
				Coord rectMax = matrix.rect.offset + matrix.rect.size;

				int start = (min.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x;	//matrix[min.x, min.z]
				if (min.z-1 >= rectMin.z  &&  min.z-1 < rectMax.z)
					for (int x=0; x<side; x++)
						if (x+min.x >= rectMin.x  &&  x+min.x < rectMax.x)
							matrix.arr[start+x] = stripe.arr[x];
				
				start = (min.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x;	//matrix[max.x, min.z]
				if (max.x >= rectMin.x  &&  max.x < rectMax.x)
					for (int z=0; z<side; z++)
						if (z+min.z >= rectMin.z  &&  z+min.z < rectMax.z)
							matrix.arr[start + z*matrix.rect.size.x] = stripe.arr[z+side];

				start = (max.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x;	//matrix[max.x-1, max.z]
				if (max.z >= rectMin.z  &&  max.z < rectMax.z)
				for (int x=0; x<side; x++)
					if (max.x-x >= rectMin.x  &&  max.x-x < rectMax.x)
						matrix.arr[start -x] = stripe.arr[x+side*2];

				start = (max.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x;	//matrix[min.x-1, max.z-1]
				if (min.x >= rectMin.x  &&  min.x < rectMax.x)
				for (int z=0; z<side; z++)
					if (max.z-1-z >= rectMin.z  &&  max.z-1-z < rectMax.z)
						matrix.arr[start - z*matrix.rect.size.x] = stripe.arr[z+side*3]; //matrix[min.x, max.z-1-z]
			}


			public static void FlipVertical (Matrix src, Matrix dst)
			{
				Coord min = src.rect.Min; Coord max = src.rect.Max;
				Stripe stripe = new Stripe(src.rect.size.x);
				for (int z=min.z; z<max.z; z++)
				{
					ReadLine(stripe, src, src.rect.offset.x, z);
					WriteLine(stripe, dst, dst.rect.offset.x, max.z-(z-min.z)-1);
				}
			}

		#endregion


		#region Free Resize

			public static void Resize (Matrix src, CoordRect dstRect)
			{
				Matrix dst = new Matrix(dstRect);
				Resize(src, dst);
				src.rect=dst.rect; src.arr=dst.arr;
			}


			public static void Resize (Matrix src, Matrix dst, Matrix tmp=null)
			/// Writing dst with new sized src contents
			/// Using cubic filtration for upsizing and linear for downsizing
			/// Using temp array for downsizing both dimensions (tmp should be src size at least vertically)
			{
				Coord savedDstOffset = dst.rect.offset;
				dst.rect.offset = src.rect.offset;

				Coord srcSize = src.rect.size;
				Coord dstSize = dst.rect.size;

				Stripe srcStripe = new Stripe( Mathf.Max(srcSize.x, srcSize.z, dst.rect.size.x, dst.rect.size.z) ); //just the longest dimension
				Stripe dstStripe = new Stripe( srcStripe.length );

				Coord min = src.rect.Min; Coord srcMax = src.rect.Max; Coord dstMax = dst.rect.Max;

				//shrinking both dimensions (using temporary array)
				if (dstSize.x < srcSize.x  &&  dstSize.z < srcSize.z)
				{
					if (tmp==null)
						tmp = new Matrix(src.rect.offset.x, src.rect.offset.z, dstSize.x, srcSize.z);

					DownsizeHorizontally(src, tmp, srcStripe, dstStripe);
					DownsizeVertically(tmp, dst, srcStripe, dstStripe);
				}

				//expanding both dimensions
				else if (dstSize.x > srcSize.x  &&  dstSize.z > srcSize.z)
				{
					Coord tmpSize = new Coord(dstSize.x, srcSize.z); //dst horizontally, src vertically
					tmp = new Matrix(src.rect.offset, tmpSize, dst.arr); //using dst array!

					UpsizeHorizontally(src, tmp, srcStripe, dstStripe);
					UpsizeVertically(tmp, dst, srcStripe, dstStripe); //note tmp and dst arrays are same
				}

				//expanding horizontally, shrinking vertically
				else if (dstSize.x > srcSize.x  &&  dstSize.z < srcSize.z)
				{
					Coord tmpSize = new Coord(srcSize.x, dstSize.z);
					tmp = new Matrix(src.rect.offset, tmpSize, dst.arr); //using dst array!

					DownsizeVertically(src, tmp, srcStripe, dstStripe);
					UpsizeHorizontally(tmp, dst, srcStripe, dstStripe); //note tmp and dst arrays are same
				}

				//expanding vertically, shrinking horizontally
				else if (dstSize.x < srcSize.x  &&  dstSize.z > srcSize.z)
				{
					Coord tmpSize = new Coord(dstSize.x, srcSize.z);
					tmp = new Matrix(src.rect.offset, tmpSize, dst.arr); //using dst array!

					DownsizeHorizontally(src, tmp, srcStripe, dstStripe);
					UpsizeVertically(tmp, dst, srcStripe, dstStripe); //note tmp and dst arrays are same
				}

				else if (dstSize.x > srcSize.x  &&  dstSize.z == srcSize.z)
					UpsizeHorizontally(src, dst, srcStripe, dstStripe);

				else if (dstSize.x < srcSize.x  &&  dstSize.z == srcSize.z)
					DownsizeHorizontally(src, dst, srcStripe, dstStripe);

				else if (dstSize.x == srcSize.x  &&  dstSize.z > srcSize.z)
					UpsizeVertically(src, dst, srcStripe, dstStripe);

				else if (dstSize.x == srcSize.x  &&  dstSize.z < srcSize.z)
					DownsizeVertically(src, dst, srcStripe, dstStripe);

				else
					dst.Fill(src); //if size match just copying array

				dst.rect.offset = savedDstOffset;
			}


			
			public static void Upsize (Matrix src, Matrix dst)			
			/// Expanding both dimensions
			{
				if (dst.rect.size.x < src.rect.size.x  ||  dst.rect.size.z < src.rect.size.z)
					throw new Exception($"Couldn't upsize: src {src.rect.ToString()} is less than dst {dst.rect.ToString()}");
				
				Stripe srcStripe = new Stripe( Mathf.Max(src.rect.size.x, src.rect.size.z, dst.rect.size.x, dst.rect.size.z) ); //just the longest dimension
				Stripe dstStripe = new Stripe( srcStripe.length );

				Coord tmpSize = new Coord(dst.rect.size.x, src.rect.size.z); //dst horizontally, src vertically
				Matrix tmp = new Matrix(src.rect.offset, tmpSize, dst.arr); //using dst array!

				UpsizeHorizontally(src, tmp, srcStripe, dstStripe, 0, src.rect.size.x);
				UpsizeVertically(tmp, dst, srcStripe, dstStripe, 0, src.rect.size.z); //note tmp and dst arrays are same
			}


			public static void Upsize (Matrix src, Vector2D srcOffset, Vector2D srcSize, Matrix dst)			
			/// Custom float-rect for src matrix to begin/end reading on half-pixel
			/// For Import node rescale
			{
				if (dst.rect.size.x < src.rect.size.x  ||  dst.rect.size.z < src.rect.size.z)
					throw new Exception($"Couldn't upsize: src {src.rect.ToString()} is less than dst {dst.rect.ToString()}");
				
				Stripe srcStripe = new Stripe( Mathf.Max(src.rect.size.x, src.rect.size.z, dst.rect.size.x, dst.rect.size.z) ); //just the longest dimension
				Stripe dstStripe = new Stripe( srcStripe.length );

				Coord tmpSize = new Coord(dst.rect.size.x, src.rect.size.z); //dst horizontally, src vertically
				Matrix tmp = new Matrix(src.rect.offset, tmpSize, dst.arr); //using dst array!

				UpsizeHorizontally(src, tmp, srcStripe, dstStripe, srcOffset.x-src.rect.offset.x, srcSize.x);
				UpsizeVertically(tmp, dst, srcStripe, dstStripe, srcOffset.z-src.rect.offset.z, srcSize.z); //note tmp and dst arrays are same
			}


			public static void Downsize (Matrix src, Matrix dst, Matrix tmp=null)			
			// Shrinking both dimensions (using temporary array)
			{
				if (dst.rect.size.x > src.rect.size.x  ||  dst.rect.size.z > src.rect.size.z)
					throw new Exception($"Couldn't downsize: src {src.rect.ToString()} is more than dst {dst.rect.ToString()}");
							
				Stripe srcStripe = new Stripe( Mathf.Max(src.rect.size.x, src.rect.size.z, dst.rect.size.x, dst.rect.size.z) ); //just the longest dimension
				Stripe dstStripe = new Stripe( srcStripe.length );

				if (tmp==null)
					tmp = new Matrix(src.rect.offset.x, src.rect.offset.z, dst.rect.size.x, src.rect.size.z);

				DownsizeHorizontally(src, tmp, srcStripe, dstStripe);
				DownsizeVertically(tmp, dst, srcStripe, dstStripe);
			}


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_DownsizeHorizontally")]
				private static extern void DownsizeHorizontally (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe);
			#else
				private static void DownsizeHorizontally (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe)
				{
					srcStripe.length = src.rect.size.x;
					dstStripe.length = dst.rect.size.x;

					for (int z=0; z<src.rect.size.z; z++) //should match dst.rect.size.z
					{
						ReadLine(srcStripe, src, src.rect.offset.x, z+src.rect.offset.z);
						ResampleStripeLinear(srcStripe, dstStripe);
						//ResampleStripeCubic(srcStripe, dstStripe);
						WriteLine(dstStripe, dst, dst.rect.offset.x, z+dst.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_DownsizeVertically")]
				private static extern void DownsizeVertically (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe);
			#else
				private static void DownsizeVertically (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe)
				{
					srcStripe.length = src.rect.size.z;
					dstStripe.length = dst.rect.size.z;

					for (int x=0; x<dst.rect.size.x; x++) //should match src.rect.size.x
					{
						ReadRow(srcStripe, src, x+src.rect.offset.x, src.rect.offset.z);
						ResampleStripeLinear(srcStripe, dstStripe);
						//ResampleStripeCubic(srcStripe, dstStripe);
						WriteRow(dstStripe, dst, x+dst.rect.offset.x, dst.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_UpsizeHorizontally")]
				private static extern void UpsizeHorizontally (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe, float srcOffset=0, float srcLength=0);
			#else
				private static void UpsizeHorizontally (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe, float srcOffset=0, float srcLength=0)
				/// srcOffset is zero-based (pixels offset from start of the stripe)
				{
					srcStripe.length = src.rect.size.x;
					dstStripe.length = dst.rect.size.x;

					for (int z=src.rect.size.z-1; z>=0; z--) //from end to start since in some cases we would be using the same array with different rects
					{
						ReadLine(srcStripe, src, src.rect.offset.x, z+src.rect.offset.z);
						ResampleStripeCubic(srcStripe, dstStripe, srcOffset, srcLength);
						WriteLine(dstStripe, dst, dst.rect.offset.x, z+dst.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_UpsizeVertically")]
				private static extern void UpsizeVertically (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe, float srcOffset=0, float srcLength=0);
			#else
				private static void UpsizeVertically (Matrix src, Matrix dst, Stripe srcStripe, Stripe dstStripe, float srcOffset=0, float srcLength=0)
				/// srcOffset is zero-based (pixels offset from start of the stripe)
				{
					srcStripe.length = src.rect.size.z;
					dstStripe.length = dst.rect.size.z;

					for (int x=src.rect.size.x-1; x>=0; x--)
					{
						ReadRow(srcStripe, src, x+src.rect.offset.x, src.rect.offset.z);
						ResampleStripeCubic(srcStripe, dstStripe, srcOffset, srcLength);
						WriteRow(dstStripe, dst, x+dst.rect.offset.x, dst.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleStripeCubic")]
				private static extern void ResampleStripeCubic (Stripe src, Stripe dst, float srcOffset=0, float srcLength=0);
				/// Scales the stripe filling dst with interpolated values. Cubic for upscale
			#else
				private static void ResampleStripeCubic (Stripe src, Stripe dst, float srcOffset=0, float srcLength=0)
				/// Scales the stripe filling dst with interpolated values. Cubic for upscale
				{
					if (srcLength<1) srcLength = src.length;

					for (int dstX=0; dstX<dst.length; dstX++)
					{
						float srcX = (float)dstX * srcLength / dst.length  + srcOffset;

						int px = (int)srcX;	if (px<0) px=0;
						int nx = px+1;		if (nx>src.length-1) nx = src.length-1;
						int ppx = px-1;		if (ppx<0) ppx = 0;
						int nnx = nx+1;		if (nnx>src.length-1) nnx = src.length-1;
						int pppx = ppx-1;	if (pppx<0) pppx = 0;
						int nnnx = nnx+1;	if (nnnx>src.length-1) nnnx = src.length-1;


						float vp = src.arr[px]; float vpp = src.arr[ppx]; float vppp = src.arr[pppx];
						float vn = src.arr[nx]; float vnn = src.arr[nnx]; float vnnn = src.arr[nnnx];

						float p = srcX-px;
						float ip = 1f-p;

						float dpp = vpp - (vppp+vp-vpp*2) * 0.25f;
						float dp = vp - (vpp+vn-vp*2) * 0.25f;
						float dn = vn - (vnn+vp-vn*2) * 0.25f;
						float dnn = vnn - (vnnn+vn-vnn*2) * 0.25f;

						float tp = (dn-dpp)*0.5f;
						float tn = (dp-dnn)*0.5f;

						float l = vp*ip + vn*p; //linear filtration

						float cp = 
							(vp + tp*p) * ip + 
							l * p;

						float cn = l * ip + 
							(vn + tn*ip) * p;

						dst.arr[dstX] =  cp*ip + cn*p;

						//dst.arr[dstX] = vp + 0.5f * p * (vn - vpp + p*(2.0f*vpp - 5.0f*vp + 4.0f*vn - vnn + p*(3.0f*(vp - vn) + vnn - vpp)));
							//standard quadratic filtration (here for test purpose)

						if (dst.arr[dstX] > 1) dst.arr[dstX] = 1;
						if (dst.arr[dstX] < 0) dst.arr[dstX] = 0;
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleStripeQuadratic")]
				private static extern void ResampleStripeQuadratic (Stripe src, Stripe dst);
				/// Scales the stripe filling dst with interpolated values. Faster than cubic, but can result in "grid" look because of the tangents
			#else
				private static void ResampleStripeQuadratic (Stripe src, Stripe dst)
				/// Scales the stripe filling dst with interpolated values. Faster than cubic, but can result in "grid" look because of the tangents
				{
					for (int dstX=0; dstX<dst.length; dstX++)
					{
						float srcX = (1.0f*dstX / dst.length) * src.length;

						int px = (int)srcX;	if (px<0) px=0;
						int nx = px+1;		if (nx>src.length-1) nx = src.length-1;
						int ppx = px-1;		if (ppx<0) ppx = 0;
						int nnx = nx+1;		if (nnx>src.length-1) nnx = src.length-1;

						float vp = src.arr[px]; float vpp = src.arr[ppx];
						float vn = src.arr[nx]; float vnn = src.arr[nnx];

						float p = srcX-px;
						float ip = 1-p;

						float tp = (vn-vpp)*0.5f;
						float tn = (vp-vnn)*0.5f;

						float l = vp*ip + vn*p; //linear filtration

						float cp = 
							(vp + tp*p) * ip + 
							l * p;

						float cn = l * ip + 
							(vn + tn*ip) * p;

						dst.arr[dstX] =  cp*ip + cn*p;

						//dst.arr[dstX] = vp + 0.5f * p * (vn - vpp + p*(2.0f*vpp - 5.0f*vp + 4.0f*vn - vnn + p*(3.0f*(vp - vn) + vnn - vpp)));
							//standard quadratic filtration (here for test purpose)

						if (dst.arr[dstX] > 1) dst.arr[dstX] = 1;
						if (dst.arr[dstX] < 0) dst.arr[dstX] = 0;
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleStripeLinear")]
				private static extern void ResampleStripeLinear (Stripe src, Stripe dst);
				/// Scales the line filling dst with ALL src interpolated values. Linear for downscale
			#else
				private static void ResampleStripeLinear (Stripe src, Stripe dst)
				/// Scales the line filling dst with interpolated values. Linear for downscale
				{
					float dstToSrc = (float)src.length / (float)dst.length;

					for (int dstX=0; dstX<dst.length; dstX++)
					{
						int srcStartX = (int)((dstX-1) * dstToSrc - 1);
						if (srcStartX < 0) srcStartX = 0;

						int srcEndX = (int)((dstX+1) * dstToSrc + 2);
						if (srcEndX >= src.length) srcEndX = src.length-1;

						float val = 0;
						float sum = 0;

						for (int srcX = srcStartX; srcX <= srcEndX; srcX++)
						{
							float refX = srcX / dstToSrc; //converting back to dst gauge
							
							float percent = (refX - dstX) * dstToSrc;
							if (percent > 1) percent = 1;
							if (percent < -1) percent = -1;
							if (percent < 0) percent = -percent;
							percent = 1-percent;

							val += src.arr[srcX] * percent;
							sum += percent;
						}

						dst.arr[dstX] = sum!=0 ? val/sum : 0;	
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResizeNearestNeighbor")]
				public static extern void ResizeNearestNeighbor (Matrix src, Matrix dst);
			#else
				public static void ResizeNearestNeighbor (Matrix src, Matrix dst)
				/// For upsizing and downsizing. Does not actually use Stripe, so might be moved to Matrix
				{
					float dstToSrcX = (float)src.rect.size.x / (float)dst.rect.size.x;
					float dstToSrcZ = (float)src.rect.size.z / (float)dst.rect.size.z;

					for (int x=0; x<dst.rect.size.x; x++)
						for (int z=0; z<dst.rect.size.z; z++)
						{
							int dstPos = z*dst.rect.size.x + x;

							int srcX = (int)((x+0.5f)*dstToSrcX);
							int srcZ = (int)((z+0.5f)*dstToSrcZ);
							int srcPos = srcZ*src.rect.size.x + srcX;

							dst.arr[dstPos] = src.arr[srcPos];
						}
				}
			#endif

		#endregion


		#region ResizeFast

			public static void ResizeFast (Matrix src, Matrix dst)
			/// When dstRect is 2x, 4x, etc larger or smaller
			/// Using ResampleStripeDownFast instead of ResampleStripeCubic/Linear
			{
				if (dst.rect.size.x == src.rect.size.x*2 || dst.rect.size.z == src.rect.size.z*2)
					UpscaleFast(src, dst);

				else if (dst.rect.size.x*2 == src.rect.size.x || dst.rect.size.z*2 == src.rect.size.z)
					DownscaleFast(src, dst);

				else
					throw new Exception("Matrix ResizeFast: rect size mismatch: src:" + src.rect.size.ToString() + " dst:" + dst.rect.size.ToString());
			}

			
			public static Matrix[] GenerateMips (Matrix src, int count=-1, float blur=0, float multiply=1)
			//
			{
				if (count < 0) 
					count = (int)Mathf.Log(src.rect.size.x, 2) - 1;

				CoordRect rect = src.rect;
				Matrix[] mips = new Matrix[count];
				Matrix mat = src;
				Matrix mip;

				Matrix tmp = new Matrix( new CoordRect(rect.offset.x, rect.offset.z, rect.size.x/2, rect.size.z) );

				Stripe srcStripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) ); //just the longest dimension
				Stripe dstStripe = new Stripe( srcStripe.length );

				for (int m=0; m<count; m++)
				{
					mip = new Matrix( new CoordRect(mat.rect.offset.x, mat.rect.offset.z, mat.rect.size.x/2, mat.rect.size.z/2) );

					DownscaleFast(mat, mip, tmp, srcStripe, dstStripe);

					if (blur > 0.0001f)
						GaussianBlur(mip, blur);

					if (multiply != 1)
						mip.Multiply(multiply);

					mips[m] = mip;
					mat = mip;
				}

				return mips;
			}


			public static Matrix TestMips (Matrix[] mips)
			/// Blends all mipmaps in one matrix
			{
				int width = 0;
				for (int m=0; m<mips.Length; m++)
					width += mips[m].rect.size.x;

				Matrix matrix = new Matrix( new CoordRect(0,0,width, mips[0].rect.size.x) );
				matrix.Fill(-1);

				width = 0;
				for (int m=0; m<mips.Length; m++)
				{
					Matrix mip = mips[m];
					CoordRect mipRect = mip.rect;

					for (int x=0; x<mipRect.size.x; x++)
						for (int z=0; z<mipRect.size.z; z++)
						{
							int mipPos = z*mipRect.size.x + x;
							int matPos = z*matrix.rect.size.x + x + width;
							
							matrix.arr[matPos] = mip.arr[mipPos];
						}

					width += mipRect.size.x;
				}

				return matrix;
			}


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_UpscaleFast")]
				public static extern void UpscaleFast (Matrix src, Matrix dst);
			#else
				public static void UpscaleFast (Matrix src, Matrix dst)
				/// Only for cases when dst rect size is exactly twice bigger than src
				/// Does not require temp matrix
				{
					if (dst.rect.size.x != src.rect.size.x*2 || dst.rect.size.z != src.rect.size.z*2)
						throw new Exception("Matrix Upscale Fast: rect size mismatch: src:" + src.rect.size.ToString() + " dst:" + dst.rect.size.ToString());

					Stripe srcStripe = new Stripe( dst.rect.size.Maximal );
					Stripe dstStripe = new Stripe( srcStripe.length );

					srcStripe.length = src.rect.size.x;
					dstStripe.length = dst.rect.size.x;
					for (int z=0; z<src.rect.size.z; z++)
					{
						ReadLine(srcStripe, src, src.rect.offset.x, z + src.rect.offset.z);
						ResampleStripeUpFast(srcStripe, dstStripe);
						WriteLine(dstStripe, dst, dst.rect.offset.x, z + dst.rect.offset.z);
					}

					srcStripe.length = src.rect.size.z;
					dstStripe.length = dst.rect.size.z;
					for (int x=dst.rect.size.x-1; x>=0; x--) //inverse order to re-use the same array
					{
						ReadRow(srcStripe, dst, x + dst.rect.offset.x, dst.rect.offset.z);
						ResampleStripeUpFast(srcStripe, dstStripe);
						WriteRow(dstStripe, dst, x + dst.rect.offset.x, dst.rect.offset.z);
					}
				}
			#endif

			
			public static void DownscaleFast (Matrix src, Matrix dst) =>  DownscaleFast (src, dst, null, null, null);

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)

				public static void DownscaleFast (Matrix src, Matrix dst, Matrix tmp=null, Stripe srcStripe=null, Stripe dstStripe=null)
				{
					if (tmp == null)
						tmp = new Matrix( new CoordRect(src.rect.offset.x, src.rect.offset.z, src.rect.size.x/2, src.rect.size.z) );

					if (srcStripe==null) srcStripe = new Stripe( src.rect.size.Maximal );
					if (dstStripe==null) dstStripe = new Stripe( srcStripe.length );

					DownscaleFastTmp(src, dst, tmp, srcStripe, dstStripe);
				}

				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_DownscaleFastTmp")]
				private static extern void DownscaleFastTmp (Matrix src, Matrix dst, Matrix tmp, Stripe srcStripe, Stripe dstStripe);
			#else
				public static void DownscaleFast (Matrix src, Matrix dst, Matrix tmp=null, Stripe srcStripe=null, Stripe dstStripe=null)
				/// Only for cases when dst rect size is exactly twice smaller than src
				{
					//if (dst.rect.size.x*2 != src.rect.size.x || dst.rect.size.z*2 != src.rect.size.z)
					//	throw new Exception("Matrix Downscale Fast: rect size mismatch: src:" + src.rect.size.ToString() + " dst:" + dst.rect.size.ToString());

					if (tmp == null)
						tmp = new Matrix( new CoordRect(src.rect.offset.x, src.rect.offset.z, src.rect.size.x/2, src.rect.size.z) );

					if (srcStripe==null) srcStripe = new Stripe( src.rect.size.Maximal );
					if (dstStripe==null) dstStripe = new Stripe( srcStripe.length );

					srcStripe.length = src.rect.size.x;
					dstStripe.length = dst.rect.size.x;
					for (int z=0; z<src.rect.size.z; z++)
					{
						ReadLine(srcStripe, src, src.rect.offset.x, z + src.rect.offset.z);
						ResampleStripeDownFast(srcStripe, dstStripe);
						WriteLine(dstStripe, tmp, tmp.rect.offset.x, z + tmp.rect.offset.z);
					}

					srcStripe.length = src.rect.size.z;
					dstStripe.length = dst.rect.size.z;
					for (int x=0; x<dst.rect.size.x; x++)
					{
						ReadRow(srcStripe, tmp, x + tmp.rect.offset.x, tmp.rect.offset.z);
						ResampleStripeDownFast(srcStripe, dstStripe);
						WriteRow(dstStripe, dst, x + dst.rect.offset.x, dst.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleStripeDownFast")]
				private static extern void ResampleStripeDownFast (Stripe src, Stripe dst);
			#else
				private static void ResampleStripeDownFast (Stripe src, Stripe dst)
				/// Like linear, works faster, but requires dst.size = src.size/2
				{
					for (int dstX=1; dstX<dst.length-1; dstX++)
					{
						//dst.arr[dstX] = src.arr[dstX*2]*0.5f + src.arr[dstX*2-1]*0.25f + src.arr[dstX*2+1]*0.25f;
						dst.arr[dstX] = src.arr[dstX*2]*0.5f + src.arr[dstX*2+1]*0.5f;
							//surprizingly this way it generates no offset
					}

					dst.arr[0] = src.arr[0]*0.75f + src.arr[1]*0.25f;
					dst.arr[dst.length-1] = src.arr[src.length-1]*0.75f + src.arr[src.length-2]*0.25f;
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleStripeUpFast")]
				private static extern void ResampleStripeUpFast (Stripe src, Stripe dst);
			#else
				private static void ResampleStripeUpFast (Stripe src, Stripe dst)
				/// Like cubic, works faster, but requires dst.size = src.size*2
				{
					for (int srcX=0; srcX<src.length-1; srcX++)
					{
						dst.arr[srcX*2] = src.arr[srcX]*0.5f + src.arr[srcX+1]*0.5f;
						dst.arr[srcX*2+1] = src.arr[srcX+1];  //placing original value to pixel+1, this will downscale lossless with NN
					}

					if (src.length*2 < dst.length) //duplicating the last pixel if dst is 1-pixel larger than src*2
						dst.arr[dst.length-1] = src.arr[src.length-1];

					dst.arr[src.length*2-1] = src.arr[src.length-1]; //*0.5f + src.arr[src.length-2]*0.5f;
					dst.arr[src.length*2-2] = src.arr[src.length-1]*0.5f + src.arr[src.length-2]*0.5f;
				}
			#endif

		#endregion


		#region GaussianBlur

			public static void GaussianBlur (Matrix matrix, float blur)
				{ GaussianBlur(matrix, matrix, blur); }

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_GaussianBlur")]
				public static extern void GaussianBlur (Matrix src, Matrix dst, float blur);
			#else
				public static void GaussianBlur (Matrix src, Matrix dst, float blur)
				/// Blur value is the number of iterations
				{
					CoordRect rect = src.rect;
					Coord min = rect.Min; Coord max = rect.Max;

					Stripe stripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );

					stripe.length = rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, rect.offset.x, z);
						BlurStripe(stripe, blur);
						WriteLine(stripe, dst, rect.offset.x, z);
					}

					stripe.length = rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, dst, x, rect.offset.z);
						BlurStripe(stripe, blur);
						WriteRow(stripe, dst, x, rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_BlurStripe")]
				public static extern void BlurStripe (Stripe src, float blur);
			#else
				internal static void BlurStripe (Stripe src, float blur)
				{
					int iterations = (int)blur;

					//iteration blur
					for (int i=0; i<iterations; i++) 
						BlurIteration(src, 1);

					//last iteration - percentage
					float percent = blur - iterations;	
					if (percent > 0.0001f)
						BlurIteration(src, percent);
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_BlurIteration")]
				public static extern void BlurIteration (Stripe src, float blur);
			#else
				internal static void BlurIteration (Stripe src, float blur)
				{
					float qBlur = blur*0.25f;
					float hBlur = 1 - qBlur*2;

					float vp = src.arr[0];
					float vx = src.arr[1];
					float vn;

					for (int x=1; x<src.length-1; x++)
					{
						vn = src.arr[x+1];

						src.arr[x] = vp*qBlur + vx*hBlur + vn*qBlur;

						vp = vx;
						vx = vn;
					}

					src.arr[0] = src.arr[0]*hBlur + src.arr[1]*qBlur*2;
					src.arr[src.length-1] = src.arr[src.length-1]*hBlur + src.arr[src.length-2]*qBlur*2;
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_BlurIteration05")]
				public static extern void BlurIteration (Stripe src);
			#else
				internal static void BlurIteration (Stripe src)
				/// Surprisingly worse performance rather than read array. I expected less bounds check, but...
				{
					float vp = src.arr[0];
					float vx = src.arr[1];
					float vn;

					for (int x=1; x<src.length-1; x++)
					{
						vn = src.arr[x+1];

						src.arr[x] = (vp + vx*2 + vn - 0.000000000000001f)*0.25f;

						vp = vx;
						vx = vn;
					}

					src.arr[0] = (src.arr[1] + src.arr[0] + src.arr[0] -0.000000000000001f) / 3;
					src.arr[src.length-1] = (src.arr[src.length-2] + src.arr[src.length-1] + src.arr[src.length-1] -0.000000000000001f) / 3;
				}
			#endif


			/*internal static unsafe void BlurIteration_Unsafe (Stripe src)
			/// Same performance as the standard one. Expected array bounds check, but...
			{
				float vp = src.arr[0];
				float vx = src.arr[1];
				float vn;

				fixed (float* arrPtr = src.arr)
					for (int x=1; x<src.arr.Length-1; x++)
				{
					vn = arrPtr[x+1];

					arrPtr[x] = (vp + vx*2 + vn - 0.000000000000001f)*0.25f;

					vp = vx;
					vx = vn;
				}

				src.arr[0] = (src.arr[1] + src.arr[0] + src.arr[0]) / 3;
				src.arr[src.arr.Length-1] = (src.arr[src.arr.Length-2] + src.arr[src.arr.Length-1] + src.arr[src.arr.Length-1]) / 3;
			}*/


			/*public static void BlurIteration_Wrong (Stripe src) // Reads the changed array
			{
				for (int x=1; x<src.arr.Length-1; x++)
				{
					src.arr[x] = (
						src.arr[x-1] + 
						src.arr[x]*2 +
						src.arr[x+1] -0.000000000000001f)*0.25f;
				}

				src.arr[0] = (src.arr[1] + src.arr[0] + src.arr[0]) / 3;
				src.arr[src.arr.Length-1] = (src.arr[src.arr.Length-2] + src.arr[src.arr.Length-1] + src.arr[src.arr.Length-1]) / 3;
			}*/

		#endregion


		#region Downsample Blur

			public static void DownsampleBlur (Matrix matrix, int downsample, float blur)
				{ DownsampleBlur(matrix, matrix, downsample, blur); }

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_DownsampleBlur")]
				public static extern void DownsampleBlur(Matrix src, Matrix dst, int downsample, float blur);
			#else
				public static void DownsampleBlur (Matrix src, Matrix dst, int downsample, float blur)
				/// Blurs matrix by downscaling each line and then upscaling it back
				/// Downsample 1 means no re-scaling (however resample stripe cubic produces artifacts in this case, so standard blur instead)
				{
					int downsamplePot = (int)Mathf.Pow(2,downsample-1);
					CoordRect rect = src.rect;
					Coord min = rect.Min; Coord max = rect.Max;
	
					Stripe hiStripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );
					Stripe loStripe = new Stripe( hiStripe.length / downsample);

					hiStripe.length = rect.size.x;
					loStripe.length = hiStripe.length / downsample;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(hiStripe, src, rect.offset.x, z);
					
						ResampleStripeLinear(hiStripe, loStripe);
						BlurStripe(loStripe, blur);
						ResampleStripeCubic(loStripe, hiStripe);

						WriteLine(hiStripe, dst, rect.offset.x, z);
					}

					hiStripe.length = rect.size.z;
					loStripe.length = hiStripe.length / downsample;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(hiStripe, dst, x, rect.offset.z);

						ResampleStripeLinear(hiStripe, loStripe);
						BlurStripe(loStripe, blur);
						ResampleStripeCubic(loStripe, hiStripe);

						WriteRow(hiStripe, dst, x, rect.offset.z);
					}
				}
			#endif

		#endregion


		#region Overblur Mipped

			public static void OverblurMipped (Matrix matrix, float downsample, float escalate=4, float blur=1)
				{ OverblurMipped(matrix, matrix, downsample, escalate, blur); }

			public static void OverblurMipped (Matrix src, Matrix dst, float downsample, float escalate=4, float blur=1)
			/// Takes the zero-centered (-1 - +1) map and overblurs it maintaining the middle (0) value
			/// Useful to blur cavity maps
			/// Escalate increases the contrast of each next mipmap
			{
				int iDownsample = (int)downsample + 1; //not ceiltoint (in case of 3.0 ceil returns 3)!
				Matrix[] mips = GenerateMips(src, iDownsample, blur:1, multiply:2f);

				if (dst!=src) dst.Fill(src);
				ArrayTools.Insert(ref mips, 0, dst);

				//lowering last mip contrast to gradually switch between downsamples
				float lastMipFactor = downsample - (int)downsample;
				mips[mips.Length-1].Multiply(lastMipFactor);

				Matrix tmp = new Matrix( new CoordRect(src.rect.offset.x, src.rect.offset.z, src.rect.size.x, src.rect.size.z/2) );
				Stripe srcStripe = new Stripe( Mathf.Max(src.rect.size.x, src.rect.size.z) ); //just the longest dimension
				Stripe dstStripe = new Stripe( srcStripe.length );

				for (int i=mips.Length-2; i>=0; i--)
					OverblurMippedIteration(mips[i+1], mips[i], tmp, srcStripe, dstStripe, escalate);
			}

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_OverblurMippedIteration")]
				public static extern void OverblurMippedIteration (Matrix mip, Matrix mat, Matrix tmp, Stripe srcStripe, Stripe dstStripe, float escalate);
			#else
				private static void OverblurMippedIteration (Matrix mip, Matrix mat, Matrix tmp, Stripe srcStripe, Stripe dstStripe, float escalate)
				{
					srcStripe.length = mip.rect.size.x;
					dstStripe.length = mat.rect.size.x;
					for (int z=mip.rect.offset.z; z<mip.rect.offset.z+mip.rect.size.z; z++)
					{
						ReadLine(srcStripe, mip, mip.rect.offset.x, z);
						ResampleStripeUpFast(srcStripe, dstStripe);
						WriteLine(dstStripe, tmp, mat.rect.offset.x, z);
					}

					srcStripe.length = mip.rect.size.z;
					dstStripe.length = mat.rect.size.z;
					for (int x=mat.rect.offset.x; x<mat.rect.offset.x+mat.rect.size.x; x++)
					{
						ReadRow(srcStripe, tmp, x, mat.rect.offset.z);
						ResampleStripeUpFast(srcStripe, dstStripe);
						OverlayRow(dstStripe, mat, x, mat.rect.offset.z, escalate);
					}
				}
			#endif

		#endregion


		#region Normals/Delta

			//normals formula:
			//new Vector3(
			//	(prevXHeight-nextXHeight)*height, 
			//	pixelSize*2, 
			//	(prevZHeight-nextZHeight)*height).
			//normalized;

			public static (Matrix r, Matrix g, Matrix b) NormalsSet (Matrix src, float pixelSize, float height)
			/// Generates 3 channels normal map from heightmap
			/// This one creates new matrices
			{
				Matrix r = new Matrix(src.rect);
				Matrix g = new Matrix(src.rect);
				Matrix b = new Matrix(src.rect);

				NormalsSet(src, r, g, b, pixelSize, height);

				return (r,g,b);
			}


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_NormalsSet")]
				public static extern void NormalsSet (Matrix src, Matrix normX, Matrix normZ, Matrix normBlue, float pixelSize, float height);
				/// Generates full set of normal map
				/// This one uses prepared matrices
			#else
				public static void NormalsSet (Matrix src, Matrix normX, Matrix normZ, Matrix normBlue, float pixelSize, float height)
				/// Generates full set of normal map
				/// This one uses prepared matrices
				{
					Coord min = src.rect.Min; Coord max = src.rect.Max;

					Stripe stripe = new Stripe(src.rect.size.Maximal);

					stripe.length = src.rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, src.rect.offset.x, z);
						NormalsStripe(stripe, height);
						WriteLine(stripe, normX, src.rect.offset.x, z);
					}

					stripe.length = src.rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, src, x, src.rect.offset.z);
						NormalsStripe(stripe, height);
						WriteRow(stripe, normZ, x, src.rect.offset.z); 
					}

					NormalizeSet(normX, normZ, normBlue, pixelSize);
				}

				private static void NormalizeSet (Matrix normX, Matrix normZ, Matrix normBlue, float pixelSize)
				{
					Coord min = normX.rect.Min; Coord max = normX.rect.Max;

					for (int i=0; i<normX.count; i++)
					{
						float nx = normX.arr[i];
						float nz = normZ.arr[i];
						float blue = pixelSize*2;

						float length = Mathf.Sqrt(nx*nx + blue*blue + nz*nz);

						normX.arr[i] = (nx/length + 1)/2;
						normZ.arr[i] = (nz/length + 1)/2;
						//if (normBlue != null) //can't observe it in c++
							normBlue.arr[i] = blue/length;
					}
				}

				private static void NormalsStripe (Stripe stripe, float height)
				{
					if (stripe.length < 3) return;

					float prevHeight = stripe.arr[0];
					float currHeight = stripe.arr[1];
					for (int x=1; x<stripe.length-2; x++)
					{
						float nextHeight = stripe.arr[x+1];

						stripe.arr[x] = (prevHeight-nextHeight)*height;

						prevHeight = currHeight;
						currHeight = nextHeight;
					}

					stripe.arr[0] = stripe.arr[1];
					stripe.arr[stripe.length-1] = stripe.arr[stripe.length-2];
				}
			#endif


			public static void NormalsDir (Matrix src, Matrix dst, Vector3 dir, float pixelSize, float height, float intensity=1, float wrapping=1)
			/// Generates dot product (lightened) image from heightmap
			/// Generates 3-channel normals and applies light to them
			{
				wrapping /= 2; //maximum wrapping is reached at level 0.5

				Matrix normX = new Matrix(src.rect);
				Matrix normZ = new Matrix(src.rect);
				Matrix normBlue = dst; 
				normBlue.Fill(0);

				NormalsSet(src, normX, normZ, normBlue, pixelSize, height);

				SetToDir(normX, normZ, normBlue, dst, dir.x, dir.y, dir.z, intensity, wrapping);
			}

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SetToDir")]
				public static extern void  SetToDir (Matrix normX, Matrix normZ, Matrix normBlue, Matrix dst, float dirX, float dirY, float dirZ, float intensity=1, float wrapping=1);
				/// Dot-lighting 3-channel normals set depending on direction
			#else
				private static void SetToDir (Matrix normX, Matrix normZ, Matrix normBlue, Matrix dst, float dirX, float dirY, float dirZ, float intensity=1, float wrapping=1)
				/// Dot-lighting 3-channel normals set depending on direction
				{
					Vector3 dir = new Vector3(dirX, dirY, dirZ);
					Vector3 normal = new Vector3();

					for (int i=0; i<dst.count; i++)
					{
						normal.x = normX.arr[i]*2 - 1;
						normal.y = normBlue.arr[i]*2 - 1;
						normal.z = normZ.arr[i]*2 - 1;

						//float val = Vector3.Dot(dir, normal);
						float val = dir.x*normal.x + dir.y*normal.y + dir.z*normal.z; //to use the same as c++ code

						val = val*(1-wrapping) + wrapping/2;
						val *= intensity;

						if (val < 0) val = 0;
						if (val > 1) val = 1;
						dst.arr[i] = val;
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_Delta")]
				public static extern void Delta(Matrix src, Matrix dst);
			#else
				public static void Delta (Matrix src, Matrix dst)
				/// Finds the maximum delta (both pos and neg) for the neighbor pixel
				{
					Coord min = src.rect.Min; Coord max = src.rect.Max;

					Stripe stripe = new Stripe( Mathf.Max(src.rect.size.x, src.rect.size.z) );

					stripe.length = src.rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, src.rect.offset.x, z);
						DeltaStripe(stripe);
						WriteLine(stripe, dst, src.rect.offset.x, z);
					}

					stripe.length = src.rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, src, x, src.rect.offset.z);
						DeltaStripe(stripe);
						MaxRow(stripe, dst, x, src.rect.offset.z);
					}
				}

				private static void DeltaStripe (Stripe stripe)
				{
					float prev = stripe.arr[0];
					float curr = stripe.arr[1];

					for (int x=1; x<stripe.length-1; x++)
					{
						//float prev = arr[x-1];
						//float curr = arr[x];
						float next = stripe.arr[x+1];
						
						float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
						float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
						float delta = prevDelta>nextDelta? prevDelta : nextDelta; 
					
						stripe.arr[x] = delta;

						prev = curr;
						curr = next;
					}

					stripe.arr[0] = stripe.arr[1];
					stripe.arr[stripe.length-1] = stripe.arr[stripe.length-2];
				}
			#endif


		#endregion


		#region Select

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_Silhouette")]
				public static extern void Silhouette (Matrix src, Matrix dst, float level, bool antialiasing=true);
			#else
				public static void Silhouette (Matrix src, Matrix dst, float level, bool antialiasing=true)
				/// Makes the pixels crossing level 1, others - 0
				{
					CoordRect rect = src.rect;
					Coord min = rect.Min; Coord max = rect.Max;

					Stripe stripe = new Stripe( Mathf.Max(src.rect.size.x, src.rect.size.z) );

					stripe.length = rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, rect.offset.x, z);
						SilhouetteStripe(stripe, level, antialiasing);
						WriteLine(stripe, dst, rect.offset.x, z);
					}

					stripe.length = rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, src, x, rect.offset.z);
						SilhouetteStripe(stripe, level, antialiasing);
						MaxRow(stripe, dst, x, rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SilhouetteStripe")]
				public static extern void SilhouetteStripe (Stripe stripe, float level, bool antiAliasing);
			#else
				private static void SilhouetteStripe (Stripe stripe, float level, bool antiAliasing)
				{
					float prevVal = stripe.arr[0];
					for (int x=0; x<stripe.length-1; x++)
					{
						float nextVal = stripe.arr[x+1];

						float prevSelect = 0;
						float nextSelect = 0;
						float percent = -1;

						if (prevVal < level  &&  nextVal >= level) //growing
						{
							float prevAbs = -(prevVal - level);
							float nextAbs = nextVal - level;
							percent = prevAbs / (prevAbs+nextAbs);
						}

						if (prevVal > level  &&  nextVal <= level) //lowering
						{
							float prevAbs = prevVal - level;
							float nextAbs = -(nextVal - level);
							percent = prevAbs / (prevAbs+nextAbs);
						}

						if (percent >= 0) //if growing or lowering
						{
							//prevSelect = 1-percent;
							//nextSelect = percent;
							//making closest pixel always 1, the other one 0-1

							if (percent < 0.5f)
							{ 
								prevSelect = 1; 
								if (antiAliasing) nextSelect = percent*2; 
							}

							else
							{ 
								nextSelect = 1; 
								if (antiAliasing) prevSelect = (1-percent)*2; 
							}
						}

						if (prevSelect > stripe.arr[x]) stripe.arr[x] = prevSelect;
						stripe.arr[x+1] = nextSelect;

						prevVal = nextVal;
					}

					
				}
			#endif

		#endregion


		#region Cavity

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_Cavity")]
				public static extern void Cavity(Matrix src, Matrix dst);
			#else
				public static void Cavity (Matrix src, Matrix dst)
				/// Creates a map of curvatures - white for concave pixels and black for convex
				{
					CoordRect rect = src.rect;
					Coord min = rect.Min; Coord max = rect.Max;

					Stripe stripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );

					stripe.length = rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, rect.offset.x, z);
						CavityStripe(stripe);
						AddLine(stripe, dst, rect.offset.x, z, 0.5f);
					}

					stripe.length = rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, src, x, rect.offset.z);
						CavityStripe(stripe);
						AddRow(stripe, dst, x, rect.offset.z, 0.5f); //apply row additively (with mid-point 0.5)
					}
				}

				internal static void CavityStripe (Stripe stripe)
				{
					float prev = stripe.arr[0];
					float curr = stripe.arr[1];

					for (int x=1; x<stripe.length-1; x++)
					{
						float next = stripe.arr[x+1];

						//float val = curr - (next + prev)/2;
						//float sign = val>0 ? 1 : -1;
						//val = (val*val*sign)*intensity*1000; 
						//val = (val+1) / 2;
						float avg = (next + prev)/2;
						float val = avg - curr;

						stripe.arr[x] = val;

						prev = curr;
						curr = next;
					}
					stripe.arr[0] = stripe.arr[1];
					stripe.arr[stripe.length-1] = stripe.arr[stripe.length-2];
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_OverSpread")]
				public static extern void OverSpread(Matrix src, Matrix dst, float multiply);
			#else
				public static void OverSpread (Matrix src, Matrix dst, float multiply)
				/// Doesn't work yet
				{
					CoordRect rect = src.rect;
					Coord min = rect.Min; Coord max = rect.Max;

					Stripe stripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );

					Matrix pos = new Matrix(src);
					for (int i=0; i<pos.arr.Length; i++)
						pos.arr[i] = (pos.arr[i] - 0.5f)*2;

					for (int i=0; i<5; i++)
					{
						stripe.length = rect.size.x;
						for (int z=min.z; z<max.z; z++)
						{
							ReadLine(stripe, pos, rect.offset.x, z);
							SpreadMultiply(stripe, multiply);
							WriteLine(stripe, pos, rect.offset.x, z);
						}

						stripe.length = rect.size.z;
						for (int x=min.x; x<max.x; x++)
						{
							ReadRow(stripe, pos, x, rect.offset.z);
							SpreadMultiply(stripe, multiply);
							WriteRow(stripe, pos, x, rect.offset.z); //apply row additively (with mid-point 0.5)
						}
					}

					dst.arr = pos.arr;
				}


				public static Matrix OverSpread (this Matrix src, float multiply)
				{
					Matrix dst = new Matrix(src.rect);
					OverSpread(src, dst, multiply);
					return dst;
				}


				private static void Invert (Stripe src, Stripe dst)
				{
					for (int i=0; i<src.arr.Length; i++)
						dst.arr[i] = -src.arr[i];
				}
			#endif


		#endregion


		#region Spread

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)  //when native checked: editor always, build when not unsupported platform
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SpreadLinear")]
				public static extern void SpreadLinear (Matrix src, Matrix dst, float subtract=0.01f, bool diagonals=false, bool quarters=false, bool bulb=false);
			#else

				public static void SpreadLinear (Matrix src, Matrix dst, float subtract=0.01f, bool diagonals=false, bool quarters=false, bool bulb=false)
				/// Smears all white values all over the matrix
				/// Each new pixel is multiplied with multiply and get subtract subtracted
				/// Will overwrite the gray values with the bigger spread values, so couldn't be used as padding
				{
					if (src==dst)
						throw new Exception("MatrixOps: same matrix is used as src and dst at the same time");

				//	if (bulb) subtract *= 2; //bulb is bigger than original, so increasing subtract

					Coord min = src.rect.Min; Coord max = src.rect.Max;

					Stripe stripe = new Stripe(src.rect.size.x + src.rect.size.z); //+ to process diagonals

					//spreading two dimensions independently
					stripe.length = src.rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, src.rect.offset.x, z);
						SpreadLinearLeft(stripe, subtract, regardSmallerValues:1);
						SpreadLinearRight(stripe, subtract, regardSmallerValues:1);
						MaxLine(stripe, dst, src.rect.offset.x, z);
					}

					stripe.length = src.rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, src, x, src.rect.offset.z);
						SpreadLinearLeft(stripe, subtract, regardSmallerValues:1);
						SpreadLinearRight(stripe, subtract, regardSmallerValues:1);
						MaxRow(stripe, dst, x, dst.rect.offset.z); 
					}

					//connecting crosses lines
					if (!diagonals)
					{
						stripe.length = src.rect.size.x;
						for (int z=min.z; z<max.z; z++)
						{
							ReadLine(stripe, dst, src.rect.offset.x, z);
							SpreadLinearLeft(stripe, subtract, regardSmallerValues:0);
							SpreadLinearRight(stripe, subtract, regardSmallerValues:0);
							MaxLine(stripe, dst, src.rect.offset.x, z);
						}

						stripe.length = src.rect.size.z;
						for (int x=min.x; x<max.x; x++)
						{
							ReadRow(stripe, dst, x, src.rect.offset.z);
							SpreadLinearLeft(stripe, subtract, regardSmallerValues:0);
							SpreadLinearRight(stripe, subtract, regardSmallerValues:0);
							MaxRow(stripe, dst, x, dst.rect.offset.z);
						}
					}
				
					else //diagonals
					{
						if (quarters)
						{
							float step = 0.4142f;  // sin(radians(22.5)) / cos(radians(22.5))
							float factor = 1.082387f;  // (1,step).magnitude;

							for (int z=max.z-1; z>=min.z; z-=3)
							{
								ReadDiagonal(stripe, dst, min.x, z, step, 1);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, min.x, z, step, 1);
							}
							for (int x=min.x; x<max.x; x++)
							{
								ReadDiagonal(stripe, dst, x, min.z, step, 1);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, x, min.z, step, 1);
							}

							for (int x=min.x; x<max.x; x+=3)
							{
								ReadDiagonal(stripe, dst, x, min.z, -1, step);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, x, min.z, -1, step);
							}
							for (int z=min.z; z<max.z; z++)
							{
								ReadDiagonal(stripe, dst, max.x-1, z, -1, step);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, max.x-1, z, -1, step);
							}

							for (int x=max.x-1; x>=min.x; x--)
							{
								ReadDiagonal(stripe, dst, x, max.z-1, step, -1);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, x, max.z-1, step, -1);
							}
							for (int z=max.z-1; z>=min.z; z-=3)
							{
								ReadDiagonal(stripe, dst, min.x, z, step, -1);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, min.x, z, step, -1);
							}

							for (int z=min.z; z<max.z; z++)
							{
								ReadDiagonal(stripe, dst, max.x-1, z, -1, -step);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, max.x-1, z, -1, -step);
							}
							for (int x=max.x-1; x>=min.x; x-=3)
							{
								ReadDiagonal(stripe, dst, x, max.z-1, -1, -step);
								SpreadLinearLeft(stripe, subtract*factor, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*factor, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, x, max.z-1, -1, -step);
							}
						}

					
						for (int z=max.z-1; z>=min.z; z--)  //quarters leave empty pixels since they apply 1to3, so using diagonals after
						{
							int maxX = z==min.z ? max.x : min.x+1;
							for (int x=min.x; x<maxX; x++)
							{
								ReadDiagonal(stripe, dst, x, z, 1, 1);
								SpreadLinearLeft(stripe, subtract*1.4142f, regardSmallerValues:0);  //sqrt(2)
								SpreadLinearRight(stripe, subtract*1.4142f, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, x, z, 1, 1);
							}
						}

						for (int x=min.x; x<max.x; x++)
						{
							int maxZ = x==max.x-1 ? max.z : min.z+1;
							for (int z=min.z; z<maxZ; z++)
							{
								ReadDiagonal(stripe, dst, x, z, -1, 1);
								SpreadLinearLeft(stripe, subtract*1.41421f, regardSmallerValues:0);
								SpreadLinearRight(stripe, subtract*1.41421f, regardSmallerValues:0);
								MaxDiagonal(stripe, dst, x, z, -1, 1);
							}
						}
					}

					//bulb
					//simulates pseudo-round behaviour on edges
					if (bulb)
					{
						//applying bulb function to enter bulb mode
						for (int i=0; i<dst.count; i++)
							dst.arr[i] = (float)(Math.Sqrt(2*dst.arr[i] - dst.arr[i]*dst.arr[i])); //*0.5f + dst.arr[i]*0.5f;

						//spreading two dimensions
						stripe.length = src.rect.size.z;
						for (int x=min.x; x<max.x; x++)
						{
							ReadRow(stripe, dst, x, src.rect.offset.z);
							SpreadLinearLeft(stripe, subtract);
							SpreadLinearRight(stripe, subtract);
							MaxRow(stripe, dst, x, dst.rect.offset.z);
						}


						stripe.length = src.rect.size.x;
						for (int z=min.z; z<max.z; z++)
						{
							ReadLine(stripe, dst, src.rect.offset.x, z);
							SpreadLinearLeft(stripe, subtract);
							SpreadLinearRight(stripe, subtract);
							MaxLine(stripe, dst, src.rect.offset.x, z);
						}

						//applying bulb inverse and some magic to 'linearize' bulb
						for (int i=0; i<dst.count; i++)
						{
							float val = dst.arr[i];
							val = (float)(1 - Math.Sqrt(1-val*val));
							val = (float)(1 - Math.Sqrt(1-val*val));

							val = (val-0.5f)*2;
							if (val<0) val = 0;

							dst.arr[i] = val;
						}
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SpreadLinearRight")]
				public static extern void SpreadLinearRight(Stripe stripe, float subtract = 0.01f, float regardSmallerValues = 1);
			#else

				private static void SpreadLinearRight (Stripe stripe, float subtract=0.01f, float regardSmallerValues=1)
				{
					if (stripe.length == 0) return; //diagonals

					float prevVal = stripe.arr[0];
					for (int x=1; x<stripe.length; x++)
					{
						float currVal = stripe.arr[x];

						if (prevVal > currVal)
						{
							float newVal = currVal;

							newVal = prevVal - subtract + (currVal/prevVal)*subtract*regardSmallerValues;
							if (newVal<0) newVal =0;

							if (newVal > currVal)
							{
								stripe.arr[x] = newVal;
								currVal = newVal;
							}
						}

						prevVal = currVal;
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SpreadLinearLeft")]
				public static extern void SpreadLinearLeft(Stripe stripe, float subtract = 0.01f, float regardSmallerValues = 1);
			#else

				private static void SpreadLinearLeft (Stripe stripe, float subtract=0.01f, float regardSmallerValues=1)
				{
					if (stripe.length < 3) return; //diagonals

					float prevVal = stripe.arr[stripe.length-1];
					for (int x=stripe.length-2; x>=0; x--)
					{
						float currVal = stripe.arr[x];

						if (prevVal > currVal)
						{
							float newVal = currVal;

							newVal = prevVal - subtract + (currVal/prevVal)*subtract*regardSmallerValues;
							if (newVal<0) newVal =0;

							if (newVal > currVal)
							{
								stripe.arr[x] = newVal;
								currVal = newVal;
							}
						}

						prevVal = currVal;
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SpreadMultiply")]
				public static extern void SpreadMultiply(Stripe stripe, float multiply = 1.0f, float regardSmallerValues = 1);
			#else

			private static void SpreadMultiply (Stripe stripe, float multiply=1f, float regardSmallerValues=1)
			{
				SpreadMultiplyRight(stripe, multiply, regardSmallerValues);
				SpreadMultiplyLeft(stripe, multiply, regardSmallerValues);
			}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SpreadMultiplyLeft")]
				public static extern void SpreadMultiplyLeft(Stripe stripe, float multiply = 1.0f, float regardSmallerValues = 1);
			#else

			private static void SpreadMultiplyLeft (Stripe stripe, float multiply=1f, float regardSmallerValues=1)
			{
				float prevVal = stripe.arr[stripe.length-1];
				for (int x=stripe.length-2; x>=0; x--)
				{
					float currVal = stripe.arr[x];

					if (prevVal > currVal)
					{
						currVal = prevVal*multiply + currVal*(1-multiply)*regardSmallerValues;
						stripe.arr[x] = currVal;
					}

					prevVal = currVal;
				}
			}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_SpreadMultiplyRight")]
				public static extern void SpreadMultiplyRight(Stripe stripe, float multiply = 1.0f, float regardSmallerValues = 1);
			#else

				private static void SpreadMultiplyRight (Stripe stripe, float multiply=1f, float regardSmallerValues=1)
				{
					float prevVal = stripe.arr[0];
					for (int x=1; x<stripe.length-1; x++)
					{
						float currVal = stripe.arr[x];

						if (prevVal > currVal)
						{
							currVal = prevVal*multiply + currVal*(1-multiply)*regardSmallerValues;
							stripe.arr[x] = currVal;
						}

						prevVal = currVal;
					}
				}
			#endif

		#endregion


		#region Padding

			public static void PaddingMipped (Matrix src, Matrix mask, Matrix dst, int mipsCount=-1, float mipContrast=2, float mipOnePxPadding=0.5f)
			/// Fills all of the unmasked areas with extended src values
			/// Supports mask AA (note that transparent mask values should be fully filled in src, like the opaque ones)
			{
				if (src==dst)
					throw new Exception("MatrixOps: same matrix is used as src and dst at the same time");

				//downscaling 
				//pretty similar to GenerateMips
				if (mipsCount < 0) 
					mipsCount = (int)Mathf.Log(src.rect.size.x, 2) - 1;

				Matrix[] mips = new Matrix[mipsCount]; 
				Matrix[] maskMips = new Matrix[mipsCount];

				Matrix mat = src; Matrix matMask = mask;
				Matrix mip; Matrix mipMask;

				CoordRect rect = src.rect;
				Matrix tmp = new Matrix( new CoordRect(rect.offset.x, rect.offset.z, rect.size.x/2, rect.size.z) );
				Matrix tmpMask = new Matrix( new CoordRect(rect.offset.x, rect.offset.z, rect.size.x/2, rect.size.z) );

				for (int i=0; i<mipsCount; i++)
				{
					mip = new Matrix( new CoordRect(mat.rect.offset.x, mat.rect.offset.z, mat.rect.size.x/2, mat.rect.size.z/2) );
					mipMask = new Matrix(mip.rect);

					DownscaleMaskedFast(mat, matMask, mip, mipMask, tmp, tmpMask);
					mipMask.Multiply(mipContrast);
					mipMask.Clamp01();
					PaddingOnePixel(mip, mipMask, mipOnePxPadding*(1-1f*i/mipsCount));
					
					mips[i] = mip;	maskMips[i] = mipMask;
					mat = mip;		matMask = mipMask;
				}

				ArrayTools.Insert(ref mips, 0, src);
				ArrayTools.Insert(ref maskMips, 0, mask);

				//Matrix maskMipsTest = TestMips(maskMips);
				//maskMipsTest.ToWindow("Mask Mips");

				//Matrix mipsTest = TestMips(mips);
				//mipsTest.ToWindow("Mips Before");

				//upscaling
				for (int m=mips.Length-2; m>=0; m--)
				{
					Matrix prevMip = mips[m+1];
					tmp = m!=0 ? new Matrix(mips[m].rect) : dst; //last iteration mixing to dst
					tmpMask = new Matrix(mips[m].rect);

					GaussianBlur(prevMip, 0.5f);
					GaussianBlur(prevMip, 0.5f);
					MatrixOps.UpscaleMaskedFast(prevMip, maskMips[m+1], tmp, tmpMask);
					tmp.Mix(mips[m], maskMips[m]);

					mips[m] = tmp;
					maskMips[m] = tmpMask;
				}
			}

			
			public static void PaddingOnePixel (Matrix matrix, Matrix mask, float intensity=1)
				{ PaddingOnePixel(matrix, mask, matrix, mask, intensity); }


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PaddingOnePixel")]
				public static extern void PaddingOnePixel(Matrix src, Matrix srcMask, Matrix dst, Matrix dstMask, float intensity = 1);
			#else
				public static void PaddingOnePixel (Matrix src, Matrix srcMask, Matrix dst, Matrix dstMask, float intensity=1)
				/// Pads one pixel only in all directions
				/// Supports AA
				{
					Stripe maskStripe = new Stripe(Mathf.Max(src.rect.size.x, src.rect.size.z));
					Stripe stripe = new Stripe(maskStripe.length);

					Coord min = src.rect.Min; Coord max = src.rect.Max;

					maskStripe.length = maskStripe.length =  src.rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, src, src.rect.offset.x, z);	
						ReadLine(maskStripe, srcMask, srcMask.rect.offset.x, z);
						
						PadStripeOnePixel(stripe, maskStripe, intensity, toLeft:false);
						PadStripeOnePixel(stripe, maskStripe, intensity, toLeft:true);

						WriteLine(stripe, dst, src.rect.offset.x, z);
						WriteLine(maskStripe, dstMask, srcMask.rect.offset.x, z);
					}

					maskStripe.length =  maskStripe.length = src.rect.size.x;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, dst, x, src.rect.offset.z);
						ReadRow(maskStripe, dstMask, x, srcMask.rect.offset.z);

						PadStripeOnePixel(stripe, maskStripe, intensity, toLeft:false);
						PadStripeOnePixel(stripe, maskStripe, intensity, toLeft:true);

						WriteRow(stripe, dst, x, src.rect.offset.z);
						WriteRow(maskStripe, dstMask, x, srcMask.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PadStripeOnePixel")]
				public static extern void PadStripeOnePixel(Stripe stripe, Stripe mask, float intensity = 1, bool toLeft = false);
			#else
				private static void PadStripeOnePixel (Stripe stripe, Stripe mask, float intensity=1, bool toLeft=false)
				/// Moves image one pixel left, blending it with the previous one the way it's done in photoshop
				{
					float prev = stripe.arr[ !toLeft ? 0 : stripe.length-1 ];
					float prevMask = mask.arr[ !toLeft ? 0 : stripe.length-1 ];

					for (int x= !toLeft ? 0 : stripe.length-1;
						!toLeft ? x<stripe.length : x>=0;
						x += !toLeft ? +1 : -1)
					{
						float val = stripe.arr[x];
						float maskVal = mask.arr[x];

						float maskSum = maskVal+prevMask; if (maskSum==0) maskSum = 1;
						float modifiedVal = (val*maskVal + prev*prevMask) /maskSum;
						stripe.arr[x] = val*maskVal +  //should maintain original value with original mask anyways
									   modifiedVal*(1-maskVal);

						float newMaskVal = maskVal + prevMask*(1-maskVal);
						mask.arr[x] = maskVal*(1-intensity) + newMaskVal*intensity; //intensity is applied only to mask

						prev = val;
						prevMask = maskVal;
					}
				}
			#endif


			public static (Matrix[],Matrix[]) GenerateMaskedMips (Matrix src, Matrix mask, int count=-1, float blur=0)
			/// Like GenerateMips, but ignores black (lower than minVal) values
			{
				if (count < 0) 
					count = (int)Mathf.Log(src.rect.size.x, 2) - 1;

				Matrix[] mips = new Matrix[count]; Matrix[] mipsMask = new Matrix[count];
				Matrix mat = src; Matrix matMask = mask;
				Matrix mip; Matrix mipMask;

				CoordRect rect = src.rect;
				Matrix tmp = new Matrix( new CoordRect(rect.offset.x, rect.offset.z, rect.size.x/2, rect.size.z) );
				Matrix tmpMask = new Matrix( new CoordRect(rect.offset.x, rect.offset.z, rect.size.x/2, rect.size.z) );

				for (int i=0; i<count; i++)
				{
					mip = new Matrix( new CoordRect(mat.rect.offset.x, mat.rect.offset.z, mat.rect.size.x/2, mat.rect.size.z/2) );
					mipMask = new Matrix(mip.rect);

					DownscaleMaskedFast(mat, matMask, mip, mipMask, tmp, tmpMask);

					mips[i] = mip;	mipsMask[i] = mipMask;
					mat = mip;		matMask = mipMask;
				}

				return (mips,mipsMask);
			}


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_DownscaleMaskedFast")]
				public static extern void DownscaleMaskedFast(Matrix src, Matrix srcMask, Matrix dst, Matrix dstMask, Matrix tmp, Matrix tmpMask);
			#else
				private static void DownscaleMaskedFast (Matrix src, Matrix srcMask, Matrix dst, Matrix dstMask, Matrix tmp, Matrix tmpMask)
				{
					CoordRect rect = src.rect;

					Stripe srcStripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) ); //just the longest dimension
					Stripe dstStripe = new Stripe( srcStripe.length );
					Stripe srcMaskStripe = new Stripe( srcStripe.length );
					Stripe dstMaskStripe = new Stripe( srcStripe.length );

					srcStripe.length = srcMaskStripe.length = src.rect.size.x;
					dstStripe.length = dstMaskStripe.length = dst.rect.size.x;
					for (int z=src.rect.offset.z; z<src.rect.offset.z+src.rect.size.z; z++)
					{
						ReadLine(srcStripe, src, src.rect.offset.x, z);
						ReadLine(srcMaskStripe, srcMask, src.rect.offset.x, z);
						
						ResampleMaskedStripeDownFast(srcStripe, srcMaskStripe, dstStripe);
						ResampleStripeDownFast(srcMaskStripe, dstMaskStripe);

						WriteLine(dstStripe, tmp, src.rect.offset.x, z);
						WriteLine(dstMaskStripe, tmpMask, src.rect.offset.x, z);
					}

					srcStripe.length = srcMaskStripe.length = src.rect.size.z;
					dstStripe.length = dstMaskStripe.length = dst.rect.size.z;
					for (int x=dst.rect.offset.x; x<dst.rect.offset.x+dst.rect.size.x; x++)
					{
						ReadRow(srcStripe, tmp, x, src.rect.offset.z);
						ReadRow(srcMaskStripe, tmpMask, x, src.rect.offset.z);
						
						ResampleMaskedStripeDownFast(srcStripe, srcMaskStripe, dstStripe);
						ResampleStripeDownFast(srcMaskStripe, dstMaskStripe);

						WriteRow(dstStripe, dst, x, src.rect.offset.z);
						WriteRow(dstMaskStripe, dstMask, x, src.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleAutomaskedStripeDownFast")]
				public static extern void ResampleAutomaskedStripeDownFast(Stripe src, Stripe dst, float minVal);
			#else
				private static void ResampleAutomaskedStripeDownFast (Stripe src, Stripe dst, float minVal)
				/// Like ResampleStripeDownFast, but ignores black (lower than minVal) values
				{
					float sum; int num;

					for (int dstX=1; dstX<dst.length-1; dstX++)
					{
						sum = 0; num = 0;

						if (src.arr[dstX*2] > minVal) { sum += src.arr[dstX*2] * 2; num += 2; } //TODO: compare in longs
						if (src.arr[dstX*2-1] > minVal) { sum += src.arr[dstX*2-1]; num ++; }
						if (src.arr[dstX*2+1] > minVal) { sum += src.arr[dstX*2+1]; num ++; }

						dst.arr[dstX] = num>0 ? sum/num : 0;
					}

					sum = 0; num = 0;
					if (src.arr[0] > minVal) { sum += src.arr[0]*3; num += 3; }
					if (src.arr[1] > minVal) { sum += src.arr[1]; num ++; }
					dst.arr[0] = num>0 ? sum/num : 0;

					sum = 0; num = 0;
					if (src.arr[src.length-1] > minVal) { sum += src.arr[src.length-1]*3; num += 3; }
					if (src.arr[src.length-2] > minVal) { sum += src.arr[src.length-2]; num ++; }
					dst.arr[dst.length-1] = num>0 ? sum/num : 0;
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleMaskedStripeDownFast")]
				public static extern void ResampleMaskedStripeDownFast(Stripe src, Stripe mask, Stripe dst);
			#else
				private static void ResampleMaskedStripeDownFast (Stripe src, Stripe mask, Stripe dst)
				/// Like ResampleAutomaskedStripeDownFast, but uses mask instead minVal
				{
					float sum; float num;

					for (int dstX=1; dstX<dst.length-1; dstX++)
					{
						sum = 0; num = 0;

						sum += src.arr[dstX*2] * mask.arr[dstX*2] * 2;  num += mask.arr[dstX*2] * 2;
						sum += src.arr[dstX*2-1] * mask.arr[dstX*2-1];  num += mask.arr[dstX*2-1];
						sum += src.arr[dstX*2+1] * mask.arr[dstX*2+1];  num += mask.arr[dstX*2+1];

						dst.arr[dstX] = num>0 ? sum/num : 0;
					}

					sum = 0; num = 0;
					sum += src.arr[0]*mask.arr[0]*3;  num += mask.arr[0]*3;
					sum += src.arr[1]*mask.arr[1];	 num += mask.arr[1];
					dst.arr[0] = num>0 ? sum/num : 0;

					sum = 0; num = 0;
					sum += src.arr[src.length-1]*mask.arr[src.length-1]*3; num += mask.arr[src.length-1]*3;
					sum += src.arr[src.length-2]*mask.arr[src.length-2];   num += mask.arr[src.length-2];
					dst.arr[dst.length-1] = num>0 ? sum/num : 0;
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_UpscaleMaskedFast")]
				public static extern void UpscaleMaskedFast(Matrix src, Matrix srcMask, Matrix dst, Matrix dstMask);
			#else
				public static void UpscaleMaskedFast (Matrix src, Matrix srcMask, Matrix dst, Matrix dstMask)
				{
					//if (dst.rect.size.x/2 != src.rect.size.x || dst.rect.size.z/2 != src.rect.size.z)
					//	throw new Exception("Matrix Upscale Fast: rect size mismatch: src:" + src.rect.size.ToString() + " dst:" + dst.rect.size.ToString());

					Stripe srcStripe = new Stripe( dst.rect.size.Maximal );
					Stripe srcMaskStripe = new Stripe(srcStripe.length);
					Stripe dstStripe = new Stripe( srcStripe.length );
					Stripe dstMaskStripe = new Stripe(srcStripe.length);

					srcStripe.length = srcMaskStripe.length = src.rect.size.x;
					dstStripe.length = dstMaskStripe.length = dst.rect.size.x;
					for (int z=src.rect.offset.z; z<src.rect.offset.z+src.rect.size.z; z++)
					{
						ReadLine(srcStripe, src, src.rect.offset.x, z);
						ReadLine(srcMaskStripe, srcMask, srcMask.rect.offset.x, z);

						ResampleMaskedStripeUpFast(srcStripe, srcMaskStripe, dstStripe);
						ResampleStripeUpFast(srcMaskStripe, dstMaskStripe);

						WriteLine(dstStripe, dst, dst.rect.offset.x, z);
						WriteLine(dstMaskStripe, dstMask, dst.rect.offset.x, z);
					}

					srcStripe.length = srcMaskStripe.length = src.rect.size.z;
					dstStripe.length = dstMaskStripe.length = dst.rect.size.z;
					for (int x=dst.rect.offset.x; x<dst.rect.offset.x+dst.rect.size.x; x++)
					{
						ReadRow(srcStripe, dst, x, dst.rect.offset.z);
						ReadRow(srcMaskStripe, dstMask, x, dst.rect.offset.z);

						ResampleMaskedStripeUpFast(srcStripe, srcMaskStripe, dstStripe);
						ResampleStripeUpFast(srcMaskStripe, dstMaskStripe);

						WriteRow(dstStripe, dst, x, src.rect.offset.z);
						WriteRow(dstMaskStripe, dstMask, x, src.rect.offset.z);
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleAutomaskedStripeUpFast")]
				public static extern void ResampleAutomaskedStripeUpFast(Stripe src, Stripe dst, float minVal);
			#else
				private static void ResampleAutomaskedStripeUpFast (Stripe src, Stripe dst, float minVal)
				{
					float sum = 0; int num = 0;

					for (int srcX=0; srcX<src.length-1; srcX++)
					{
						dst.arr[srcX*2] = src.arr[srcX];

						sum = 0; num = 0;
						if (src.arr[srcX] > minVal) { sum += src.arr[srcX]; num += 1; }
						if (src.arr[srcX+1] > minVal) { sum += src.arr[srcX+1]; num += 1; }
						dst.arr[srcX*2+1] = num>0 ? sum/num : 0;
					}

					dst.arr[dst.length-1] = src.arr[src.length-1];

					sum = 0; num = 0;
					if (src.arr[src.length-1] > minVal) { sum += src.arr[src.length-1]; num += 1; }
					if (src.arr[src.length-2] > minVal) { sum += src.arr[src.length-2]; num += 1; }
					dst.arr[dst.length-2] = num>0 ? sum/num : 0;
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_ResampleMaskedStripeUpFast")]
				public static extern void ResampleMaskedStripeUpFast(Stripe src, Stripe mask, Stripe dst);
			#else
				private static void ResampleMaskedStripeUpFast (Stripe src, Stripe mask, Stripe dst)
				{
					float sum = 0; float num = 0;

					for (int srcX=0; srcX<src.length-1; srcX++)
					{
						dst.arr[srcX*2] = src.arr[srcX];

						sum = 0; num = 0;
						sum += src.arr[srcX]*mask.arr[srcX]; num += mask.arr[srcX];
						sum += src.arr[srcX+1]*mask.arr[srcX+1]; num += mask.arr[srcX+1];
						dst.arr[srcX*2+1] = num>0 ? sum/num : 0;
					}

					if (src.length*2 < dst.length) //duplicating the last pixel if dst is 1-pixel larger than src*2
						dst.arr[dst.length-1] = src.arr[src.length-1];

					dst.arr[src.length*2-1] = src.arr[src.length-1];

					sum = 0; num = 0;
					sum += src.arr[src.length-1]*mask.arr[src.length-1]; num += mask.arr[src.length-1];
					sum += src.arr[src.length-2]*mask.arr[src.length-2]; num += mask.arr[src.length-2];

					if (num==0)
						num=0;

					dst.arr[src.length*2-2] = num>0 ? sum/num : 0;
				}
			#endif

		#endregion


		#region Outdated Padding
		
			public static void PaddingSpread (Matrix srcDst, Matrix mask)
				{ PaddingSpread(srcDst, srcDst, mask, mask); }

			public static void PaddingSpread (Matrix src, Matrix dst, Matrix srcMask, Matrix dstMask, float horFade=0.8f, float vertFade=0.8f)
			/// Previous experiments with padding. Slow and do not give an effect of PaddingMips.
			{
				Stripe leftStripe = new Stripe(Mathf.Max(src.rect.size.x, src.rect.size.z));
				Stripe leftMask = new Stripe(Mathf.Max(src.rect.size.x, src.rect.size.z));

				Coord min = src.rect.Min; Coord max = src.rect.Max;


				leftStripe.length = leftMask.length = src.rect.size.x;
				for (int z=min.z; z<max.z; z++)
				{
					ReadLine(leftStripe, src, src.rect.offset.x, z);	 //Stripe.Copy(leftStripe, rightStripe);
					ReadLine(leftMask, srcMask, srcMask.rect.offset.x, z);

					PadStripe(leftStripe, leftMask, fade:vertFade, toLeft:false);
					PadStripe(leftStripe, leftMask, fade:vertFade, toLeft:true);

					WriteLine(leftStripe, dst, dst.rect.offset.x, z);
					WriteLine(leftMask, dstMask, dstMask.rect.offset.x, z);
				}

				leftStripe.length = leftMask.length = src.rect.size.z;
				for (int x=min.x; x<max.x; x++)
				{
					ReadRow(leftStripe, dst, x, src.rect.offset.z);
					ReadRow(leftMask, dstMask, x, src.rect.offset.z);

					for (int i=0; i<leftMask.length; i++)
						leftMask.arr[i] *= leftMask.arr[i];

					PadStripe(leftStripe, leftMask, fade:vertFade, toLeft:false);
					PadStripe(leftStripe, leftMask, fade:vertFade, toLeft:true);

					WriteRow(leftStripe, dst, x, dst.rect.offset.z);
					WriteRow(leftMask, dstMask, x, dstMask.rect.offset.z);
				}
			}


			private static void PadStripe (Stripe stripe, Stripe mask, float fade=0.9f, bool toLeft=false)
			/// For mask: spreading with linear lowering
			/// For stripe: if new (spreaded) mask value > curr mask value - setting prev
			/// prev = stripe*mask + prev*(1-mask)
			{
				float prev = stripe.arr[ !toLeft ? 0 : stripe.arr.Length-1 ];
				float prevMask = mask.arr[ !toLeft ? 0 : stripe.arr.Length-1 ];

				for (int x= !toLeft ? 0 : stripe.arr.Length-1;
					!toLeft ? x<stripe.arr.Length : x>=0;
					x += !toLeft ? +1 : -1)
				{
					float val = stripe.arr[x];
					float maskVal = mask.arr[x];

					float modifiedVal = (val*maskVal + prev*prevMask) / ((maskVal+prevMask != 0) ? maskVal+prevMask : 1);
					val = val*maskVal + modifiedVal*(1-maskVal);

					//maskVal = maskVal > prevMask ? maskVal : prevMask;  
					maskVal = (long)(maskVal * 0x3FFFFFFFFFFFFFFE) > (long)(prevMask * 0x3FFFFFFFFFFFFFFE) ? maskVal : prevMask;  //direct float comparison is slower

					prev = val;
					prevMask = maskVal*fade;

					stripe.arr[x] = val;
					mask.arr[x] = maskVal;
				}
			}


			private static void PadStripeLeftWithBlur (Stripe stripe, float minVal=0, int blur=30)
			/// Not used, stored just in case
			/// Continues last value if current is less minVal, in 'blur' pixels blending it to blurred (average) value
			{
				float prevDefinedVal = stripe.arr[stripe.length-1];
				float prevDefinedValBlurred = stripe.arr[stripe.length-1];
				int undefinedCount = 0;

				float invBlurStrength = 1f / blur;
				float blurStrength = 1 - invBlurStrength;

				for (int x=stripe.length-2; x>=0; x--)
				{
					float val = stripe.arr[x];
					if (val > minVal) 
					{
						prevDefinedVal = val;

						prevDefinedValBlurred = undefinedCount == 0 ? 
							prevDefinedValBlurred*blurStrength + val*invBlurStrength :
							prevDefinedVal; //resetting blurred on new defined pixel

						undefinedCount = 0;
					}
					else
					{
						if (undefinedCount > blur)
							val = prevDefinedValBlurred;
						else
						{
							float blurPercent = 1f*undefinedCount / blur;
							val = prevDefinedVal*(1-blurPercent) + prevDefinedValBlurred*blurPercent;
						}

						stripe.arr[x] = val;

						undefinedCount++;
					}
				}
			}


			public static void StripeToMask (Stripe stripe, Stripe mask, float minVal=0)
			/// Sets mask values to 1 if stripe > minVal, and to 0 if stripe less minVal
			/// Reads stripe, writes mask
			{
				for (int x=0; x<stripe.length; x++)
					mask.arr[x] = stripe.arr[x] > minVal ? 1 : 0;
			}


			public static void BlendStripes (Stripe leftStripe, Stripe rightStripe, Stripe leftMask, Stripe rightMask)
			/// Blending two stripes together using their mask values (more mask - more weight)
			/// Writing result to leftStripe. 
			/// Btw merging masks and writing in leftMask
			{
				for (int x=0; x<leftStripe.length; x++)
				{
					float leftMaskVal =  leftMask.arr[x];
					float rightMaskVal =  rightMask.arr[x];

					float sum = leftMaskVal + rightMaskVal;
					leftStripe.arr[x] = 
						sum > 0 ?
						(leftStripe.arr[x] * leftMaskVal  +  rightStripe.arr[x] * rightMaskVal) / sum :
						0;
					leftMask.arr[x] = sum / 2;
				}
			}


			public static void MaxStripes (Stripe leftStripe, Stripe rightStripe, Stripe leftMask, Stripe rightMask)
			/// Blending two stripes together using their mask values (more mask - more weight)
			/// Writing result to leftStripe. 
			/// Btw merging masks and writing in leftMask
			{
				for (int x=0; x<leftStripe.length; x++)
				{
					float leftMaskVal =  leftMask.arr[x];
					float rightMaskVal =  rightMask.arr[x];

					float sum = leftMaskVal + rightMaskVal;
					leftStripe.arr[x] = leftMask.arr[x] > rightMask.arr[x] ?
						leftStripe.arr[x] :
						rightStripe.arr[x];
					leftMask.arr[x] = (leftMask.arr[x]+rightMask.arr[x]) / 2;
				}
			}


			public static void PaddingBac (Matrix matrix, Matrix mask, float sharpness=0.25f, int iterations=2)
			/// Fills non-masked areas with edge color
			/// Does not support initial mask antialiasing
			{
				float multiply = 1-sharpness; //multiply controls sharpness

				Stripe leftMask = new Stripe(Mathf.Max(matrix.rect.size.x, matrix.rect.size.z));
				Stripe rightMask = new Stripe(leftMask.length);
				Stripe leftStripe = new Stripe(leftMask.length);
				Stripe rightStripe = new Stripe(leftMask.length);

				Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;

				for (int i=0; i<iterations; i++)
				{

					leftMask.length = matrix.rect.size.x;
					rightMask.length = leftMask.length;
					leftStripe.length = leftMask.length;
					rightStripe.length = leftMask.length;
					for (int z=min.z; z<max.z; z++)
					{
						//reading lines
						ReadLine(leftStripe, matrix, matrix.rect.offset.x, z);
						ReadLine(leftMask, mask, matrix.rect.offset.x, z);
						Stripe.Copy(leftStripe, rightStripe);
						Stripe.Copy(leftMask, rightMask);

						//spreading
						PadLeft(leftStripe, leftMask, multiply);
						PadRight(rightStripe, rightMask, multiply);

						//assembling back to leftStripe
						for (int x=0; x<leftStripe.length; x++)
						{
							float sum = leftMask.arr[x] + rightMask.arr[x];
							leftStripe.arr[x] = 
								sum > 0 ?
								(leftStripe.arr[x] * leftMask.arr[x]  +  rightStripe.arr[x] * rightMask.arr[x]) / sum :
								0;
							leftMask.arr[x] = sum / 2;
						}

						WriteLine(leftStripe, matrix, matrix.rect.offset.x, z);
						WriteLine(leftMask, mask, mask.rect.offset.x, z);
					}

					leftMask.length = matrix.rect.size.z;
					rightMask.length = leftMask.length;
					leftStripe.length = leftMask.length;
					rightStripe.length = leftMask.length;

					for (int x=min.x; x<max.x; x++)
					{
						//reading lines
						ReadRow(leftStripe, matrix, x, matrix.rect.offset.z);
						ReadRow(leftMask, mask, x, matrix.rect.offset.z);
						Stripe.Copy(leftStripe, rightStripe);
						Stripe.Copy(leftMask, rightMask);

						//spreading
						PadLeft(leftStripe, leftMask, multiply);
						PadRight(rightStripe, rightMask, multiply);

						//applying to dst
						for (int z=0; z<leftStripe.length; z++)
						{
							float sum = leftMask.arr[z] + rightMask.arr[z];
							leftStripe.arr[z] = 
								sum > 0 ? 
								(leftStripe.arr[z] * leftMask.arr[z]  +  rightStripe.arr[z] * rightMask.arr[z]) / sum :
								0;

							leftMask.arr[z] = sum / 2;
						}

						WriteRow(leftStripe, matrix, x, matrix.rect.offset.z);
						WriteRow(leftMask, mask, x, mask.rect.offset.z);
					}

				}
			}


			private static void PadLeft (Stripe stripe, Stripe mask, float multiply=0.9f)
			{
				float prevVal = stripe.arr[stripe.length-1];
				float prevMask = mask.arr[stripe.length-1];

				for (int x=stripe.length-2; x>=0; x--)
				{
					float currMask = mask.arr[x];
					float currVal = stripe.arr[x];

					float maskSum = currMask+prevMask;
					currVal = maskSum > 0 ?
						(currVal*currMask + prevVal*prevMask) / maskSum :
						0;
					stripe.arr[x] = currVal;

					if (prevMask > currMask)
					{
						currMask = prevMask*multiply; // + currVal*(1-multiply)*regardSmallerValues;
						mask.arr[x] = currMask;
					}

					prevVal = currVal;
					prevMask = currMask;
				}
			}


			private static void PadRight (Stripe stripe, Stripe mask, float multiply=0.9f)
			{
				float prevVal = stripe.arr[0];
				float prevMask = mask.arr[0];

				for (int x=1; x<stripe.length-1; x++)
				{
					float currMask = mask.arr[x];
					float currVal = stripe.arr[x];

					float maskSum = currMask+prevMask;
					currVal = maskSum > 0 ?
						(currVal*currMask + prevVal*prevMask) / maskSum :
						0;
					stripe.arr[x] = currVal;

					if (prevMask > currMask)
					{
						currMask = prevMask*multiply; // + currVal*(1-multiply)*regardSmallerValues;
						mask.arr[x] = currMask;
					}

					prevVal = currVal;
					prevMask = currMask;
				}
			}

		#endregion


		#region Outdated Padding (Predict)

			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 

				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PredictPadding")]
				public static extern void PredictPadding(Matrix src, Matrix dst, float expandEdge = 0.1f, int expandPixels = 50);

				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PadStripe")]
				public static extern void PadStripe(Stripe leftStripe, Stripe rightStripe, Stripe leftMask, Stripe rightMask, float expandEdge = 0.1f, int expandPixels = 50);

				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PredictPadStripeLeft")]
				public static extern void PredictPadStripeLeft(Stripe stripe, float expandEdge = 0.1f, int expandPixels = 50);

				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PredictPadStripeRight")]
				public static extern void PredictPadStripeRight(Stripe stripe, float expandEdge = 0.1f, int expandPixels = 50);

				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PadStripeLeft")]
				public static extern void PadStripeLeft(Stripe stripe);
	
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_PadStripeRight")]
				public static extern void PadStripeRight(Stripe stripe);

			#else

			public static void PredictPadding (Matrix src, Matrix dst, float expandEdge=0.1f, int expandPixels=50)
			/// Fills gaps in matrix. Uses negative values as a mask
			{
				CoordRect rect = src.rect;
				Coord min = rect.Min; Coord max = rect.Max;

				Stripe leftStripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );
				Stripe rightStripe = new Stripe(leftStripe.length);
				Stripe leftMask = new Stripe(leftStripe.length);
				Stripe rightMask = new Stripe(leftStripe.length);

				for (int i=0; i<5; i++)
				{
					leftStripe.length = rect.size.x;		rightStripe.length = rect.size.x;
					leftMask.length = rect.size.x;			rightMask.length = rect.size.x;

					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(leftStripe, src, rect.offset.x, z);
						PadStripe(leftStripe, rightStripe, leftMask, rightMask, expandEdge, expandPixels);
						WriteLine(leftStripe, dst, rect.offset.x, z);
					}

					/*stripe.length = rect.size.z;
					maskStripe.length = rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(src, stripe, x, rect.offset.z);
						mask.ReadRow(maskStripe, x, rect.offset.z);
						PadStripe(stripe, maskStripe);
						dst.WriteRow(stripe, x, rect.offset.z); //apply row additively (with mid-point 0.5)
					}*/
				}
			}


			internal static void PadStripe (Stripe leftStripe, Stripe rightStripe, Stripe leftMask, Stripe rightMask, float expandEdge=0.1f, int expandPixels=50)
			/// Processed values are stored in leftStripe
			{
				//reading left stripe
				for (int x=0; x<leftStripe.length; x++)
				{
					leftMask.arr[x] = rightMask.arr[x] = leftStripe.arr[x] > 0 ? 1 : 0; 
					rightStripe.arr[x] = leftStripe.arr[x];
				}

				//spreading masks
				SpreadMultiplyLeft(leftMask, multiply:0.9f);
				SpreadMultiplyRight(rightMask, multiply:0.9f);

				//padding stripes
				PadStripeLeft(leftStripe);
				PadStripeRight(rightStripe);

				//applying masks
				for (int x=0; x<leftStripe.length; x++)
				{
					float lm = leftMask.arr[x];
					float rm = rightMask.arr[x];
					float sum = lm+rm;
					if (sum != 0) { lm/=sum; rm/=sum; }

					float p;
					if (lm > rm)
					{
						p = lm;
						leftStripe.arr[x] = leftStripe.arr[x]*(1-p) + rightStripe.arr[x]*p;
					}
					else
					{
						p = rm;
						leftStripe.arr[x] = leftStripe.arr[x]*p + rightStripe.arr[x]*(1-p);
					}

//					leftStripe.arr[x] = leftMask.arr[x];
				}
			}


			internal static void PredictPadStripeLeft (Stripe stripe, float expandEdge=0.1f, int expandPixels=50)
			{
				//finding stripe start
				int s;
				for (s=0; s<stripe.length; s++)
					if (stripe.arr[s] >= 0) break;
				if (s>=stripe.length-1) return;

				float pv = stripe.arr[s];

				int empty = 0;
				//float avg = stripe.arr[0];
				float vector = 0;

				for (int x=s; x<stripe.length; x++)
				{
					float v = stripe.arr[x];
					//float m = maskStripe.arr[x];

					if (v < 0) 
					{
						empty++;

						if (empty < expandPixels)
							pv += vector*(1f-empty/expandPixels);

						stripe.arr[x] = pv;
					}
					else 
					{ 
						empty = 0;
						vector = vector*(1-expandEdge) + (v-pv)*expandEdge; 
						//avg = avg*0.9f + v*0.1f;

						//stripe.arr[x] = v; 
						pv = v; 
					}
				}
			}

			internal static void PredictPadStripeRight (Stripe stripe, float expandEdge=0.1f, int expandPixels=50)
			{
				float pv = stripe.arr[stripe.length-1];

				int empty = 0;
				float vector = 0;

				for (int x=stripe.length-1; x>0; x--)
				{
					float v = stripe.arr[x];

					if (v < 0) 
					{
						empty++;

						if (empty < expandPixels)
							pv += vector*(1f-empty/expandPixels);

						stripe.arr[x] = pv;
					}
					else 
					{ 
						empty = 0;
						vector = vector*(1-expandEdge) + (v-pv)*expandEdge; 
						//avg = avg*0.9f + v*0.1f;

						stripe.arr[x] = v; 
						pv = v; 
					}
				}
			}

			internal static void PadStripeLeft (Stripe stripe)
			{
				float pv = stripe.arr[0];

				for (int x=0; x<stripe.length; x++)
				{
					float v = stripe.arr[x];

					if (v < 0) 
						stripe.arr[x] = pv;
					else  
						pv = v; 
				}
			}

			internal static void PadStripeRight (Stripe stripe)
			{
				float pv = stripe.arr[0];

				for (int x=stripe.length-1; x>0; x--)
				{
					float v = stripe.arr[x];

					if (v < 0) 
						stripe.arr[x] = pv;
					else  
						pv = v; 
				}
			}

			#endif

		#endregion


		#region Lock Padding

			public static void ExtendCircular (this Matrix matrix, Coord center, int radius, int extendRange, int expandPixels=0)
			{
				//resetting area out of radius
				RemoveOuter(matrix, center, radius);

				//creating radial lines
				int numLines = Mathf.CeilToInt( Mathf.PI * radius ); //using only the half of the needed lines
				float angleStep = Mathf.PI * 2 / (numLines-1); //in radians

				Stripe stripe = new Stripe(extendRange*2);
				Stripe maskStripe = new Stripe(extendRange*2);
				for (int i=0; i<extendRange; i++)
					maskStripe.arr[i] = 1;

				for (int i=0; i<numLines; i++)
				{
					float angle = i*angleStep;
					angle -= Mathf.PI*3f / 4f;
					Vector2 direction = new Vector2( Mathf.Sin(angle), Mathf.Cos(angle) ).normalized;

					//making any of the step components equal to 1
					Vector2 posDir = new Vector2 (
						(direction.x>0 ? direction.x : -direction.x),
						(direction.y>0 ? direction.y : -direction.y) );
					float max = posDir.x>posDir.y ? posDir.x : posDir.y;
					Vector2 step = direction / max; 
					int predictStart = (int)(extendRange * max);

					//finding proper start so that stripe middle be on the edge
					Vector2 start = center.vector2 + direction*radius;
					start -= step*extendRange;

					ReadInclined(stripe, matrix, start, step);
					PredictPadStripeLeft(stripe, expandPixels:expandPixels);
					WriteInclined(stripe, matrix, start, step);
				}

				//creating a diagonal stripe for squares
				{
					Vector2 dir =  new Vector2(-1,-1);
					Vector2 start = center.vector2 + dir.normalized*radius;
					stripe = new Stripe(extendRange*2); //new Stripe( Mathf.Max(matrix.rect.size.x, matrix.rect.size.z) / 2);

					ReadInclined(stripe, matrix, start, dir);
					PredictPadStripeLeft(stripe, expandPixels:expandPixels);
					WriteInclined(stripe, matrix, start, dir);
				}

				//filling gaps between lines
				Stripe leftStripe = new Stripe((radius+extendRange+1)*2*4 + 1);		leftStripe.Fill(-1); //otherwise will partly fill with 0 from the end
				Stripe rightStripe = new Stripe(leftStripe.length);					rightStripe.Fill(-1);
				Stripe leftMask = new Stripe(leftStripe.length);
				Stripe rightMask = new Stripe(leftStripe.length);

				for (int i=(int)(radius*0.7f); i<radius+extendRange; i++)
				{
					ReadSquare(leftStripe, matrix, center, i);
					PadStripe(leftStripe, rightStripe, leftMask, rightMask, expandEdge:0, expandPixels:0);
					WriteSquare(leftStripe, matrix, center, i);
				}

				//blurring
				BlurCircular(matrix, center, radius + extendRange, extendRange);
				DownsampleBlurCircular(matrix, center, radius + (int)(extendRange*0.05f), (int)(extendRange*0.95f+1), 2, 4);
			}


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_RemoveOuter")]
				public static extern void RemoveOuter(Matrix matrix, Coord coord, int radius);
			#else
				private static void RemoveOuter (this Matrix matrix, Coord coord, int radius)
				/// Fill the outer part of the matrix with negative values so that FillGaps could be used 
				{
					Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
					int radiusSq = radius*radius;

					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
						{
							int pos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
						
							float dist = (x-coord.x)*(x-coord.x) + (z-coord.z)*(z-coord.z);
							if (dist > radiusSq) { matrix.arr[pos] = -1; continue; }
						}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_BlurCircular")]
				public static extern void BlurCircular(Matrix matrix, Coord coord, int radius, int extendRange);
			#else
				private static void BlurCircular (this Matrix matrix, Coord coord, int radius, int extendRange)
				/// Maybe blur standard and then mask circular?
				{
					CoordRect rect = matrix.rect;
					Coord min = rect.Min; Coord max = rect.Max;
					int radiusSq = radius*radius;
					int extendRangeSq = extendRange*extendRange;
	
					Stripe stripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );

					stripe.length = rect.size.x;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(stripe, matrix, rect.offset.x, z);
						BlurIteration(stripe, 1.5f);
						//BlurIteration(stripe, 1.5f);
						for (int x=min.x; x<max.x; x++)
						{
							int stripePos = x-matrix.rect.offset.x;
							int matrixPos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;

							float distSq = (x-coord.x)*(x-coord.x) + (z-coord.z)*(z-coord.z);
							if (distSq > radiusSq)
								matrix.arr[matrixPos] = stripe.arr[stripePos];
						}
					}

					stripe.length = rect.size.z;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(stripe, matrix, x, rect.offset.z);
						BlurIteration(stripe, 1.5f);
						//BlurIteration(stripe, 1.5f);
						for (int z=min.z; z<max.z; z++)
						{
							int stripePos = z-matrix.rect.offset.z;
							int matrixPos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;

							float distSq = (x-coord.x)*(x-coord.x) + (z-coord.z)*(z-coord.z);
							if (distSq > radiusSq)
								matrix.arr[matrixPos] = stripe.arr[stripePos];
						}
					}
				}
			#endif


			#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
				[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MatrixOps_DownsampleBlurCircular")]
				public static extern void DownsampleBlurCircular(Matrix matrix, Coord coord, int radius, int extendRange, int downsample, float blur);
			#else
				private static void DownsampleBlurCircular (this Matrix matrix, Coord coord, int radius, int extendRange, int downsample, float blur)
				{
					int maxDownsample = (int)Mathf.Log(matrix.rect.size.x, 2) - 1;
					if (downsample > maxDownsample) downsample = maxDownsample;
					if (downsample == 0) return;

					CoordRect rect = matrix.rect;
					Coord min = rect.Min; Coord max = rect.Max;
					int radiusSq = radius*radius;
					int extendRangeSq = extendRange*extendRange;
	
					Stripe hiStripe = new Stripe( Mathf.Max(rect.size.x, rect.size.z) );
					Stripe srcStripe = new Stripe(hiStripe.length);
					Stripe loStripe = new Stripe( hiStripe.length / 2);

					hiStripe.length = rect.size.x;
					loStripe.length = hiStripe.length / 2;
					for (int z=min.z; z<max.z; z++)
					{
						ReadLine(hiStripe, matrix, rect.offset.x, z);

						ResampleStripeDownFast(hiStripe, loStripe);
						BlurStripe(loStripe, blur);
						ResampleStripeUpFast(loStripe, hiStripe);

						for (int x=min.x; x<max.x; x++)
						{
							int stripePos = x-matrix.rect.offset.x;
							int matrixPos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;

							float distSq = (x-coord.x)*(x-coord.x) + (z-coord.z)*(z-coord.z);
							if (distSq > radiusSq)
							{
								float p = 1 - (distSq-radiusSq) / extendRangeSq;
								if (p>1) p = 1; if (p<0) p = 0;
								p *= p;
								matrix.arr[matrixPos] = matrix.arr[matrixPos]*p + hiStripe.arr[stripePos]*(1-p);
							}
						}
					}


					hiStripe.length = rect.size.z;
					loStripe.length = hiStripe.length / 2;
					for (int x=min.x; x<max.x; x++)
					{
						ReadRow(hiStripe, matrix, x, rect.offset.z);

						ResampleStripeDownFast(hiStripe, loStripe);
						BlurStripe(loStripe, blur);
						ResampleStripeUpFast(loStripe, hiStripe);

						for (int z=min.z; z<max.z; z++)
						{
							int stripePos = z-matrix.rect.offset.z;
							int matrixPos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;

							float distSq = (x-coord.x)*(x-coord.x) + (z-coord.z)*(z-coord.z);
							if (distSq > radiusSq)
							{
								float p = 1 - (distSq-radiusSq) / extendRangeSq;
								if (p>1) p = 1; if (p<0) p = 0;
								p *= p;
								matrix.arr[matrixPos] = matrix.arr[matrixPos]*p + hiStripe.arr[stripePos]*(1-p);
							}
						}
					}

					/*hiStripe.length = rect.size.z;
					loStripe.length = hiStripe.length / downsample;
					for (int x=min.x; x<max.x; x++)
					{
						matrix.ReadRow(hiStripe, x, rect.offset.z);

						ResampleStripeLinear(hiStripe, loStripe);
						BlurStripe(loStripe, blur);
						ResampleStripeCubic(loStripe, hiStripe);

						matrix.WriteRow(hiStripe, x, rect.offset.z);
					}*/
				}
			#endif

		#endregion
	}
}
