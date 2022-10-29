using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Den.Tools.Matrices;
using System.Runtime.InteropServices;

namespace Den.Tools 
{
	public static class Erosion
	{
		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 

			//il2cpp doesn't see Matrix2D<int>
			[Serializable, StructLayout (LayoutKind.Sequential)]
			public class MatrixInt
			{
				public CoordRect rect; //never assign it's size manually, use ChangeRect
				public int count;
				public int pos;
				public int[] arr;

				public static implicit operator MatrixInt (Matrix2D<int> src)
					{ return new MatrixInt(src); }

				public  MatrixInt (Matrix2D<int> src)
					{ rect=src.rect; count=src.count; pos=src.pos; arr=src.arr; }
			}

		#else

			public struct Cross
			{
				//public float xz; public float z; public float Xz;
				//public float x; public float c; public float X;
				//public float xZ; public float Z; public float XZ;

				public float[] vals;

				public static MooreCross Zero () { return new MooreCross() { vals = new float[5] }; }

				public Cross (float[] m, int i, int sizeX)
				{
					//xz = m[i-1-sizeX];	z = m[i-sizeX];		Xz = m[i+1-sizeX];
					//x = m[i-1];			c = m[i];			X = m[i+1];
					//xZ = m[i-1+sizeX];	Z = m[i+sizeX];		XZ = m[i+1+sizeX]; 

					vals = new float[]
					{
										m[i-sizeX],		
						m[i-1],			m[i],			m[i+1],
										m[i+sizeX],	
					};
				}

				public Cross (float val) { vals = new float[5]; for (int i=0; i<5; i++) vals[i] = val; }

				public void SetToMatrix (float[] m, int i, int sizeX)
				{
												m[i-sizeX] = vals[0];
						m[i-1] = vals[1];		m[i] = vals[2];			m[i+1] = vals[3];
												m[i+sizeX] = vals[4];	
				}

				public void AddToMatrix (float[] m, int i, int sizeX)
				{
												m[i-sizeX] += vals[0];
						m[i-1] += vals[1];		m[i] += vals[2];			m[i+1] += vals[3];
												m[i+sizeX] += vals[4];	
				}

				public static Cross operator + (Cross c1, Cross c2) { for (int i=0; i<5; i++) c1.vals[i] += c2.vals[i]; return c1; }
				public static Cross operator + (Cross c1, float f) { for (int i=0; i<5; i++) c1.vals[i] += f; return c1; }
				public static Cross operator - (float f, Cross c) { for (int i=0; i<5; i++) c.vals[i] = f-c.vals[i]; return c; }
				public static Cross operator - (Cross c1, float f) { for (int i=0; i<5; i++) c1.vals[i] -=f; return c1; }
				public static Cross operator - (Cross c1, Cross c2) { for (int i=0; i<5; i++) c1.vals[i] -= c2.vals[i]; return c1; }
				public static Cross operator / (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = c.vals[i]/f; return c; }
				public static Cross operator * (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = c.vals[i]*f; return c; }
				public static Cross operator * (float f, Cross c) { for (int i=0; i<5; i++) c.vals[i] = c.vals[i]*f; return c; }

				public float Min () { float min=2000000000; for (int i=0; i<5; i++) if (vals[i]<min) min = vals[i]; return min; }
				public float MinSides () { float min=2000000000; for (int i=0; i<5; i++) { if (i==2) continue; if (vals[i]<min) min = vals[i];} return min; }
				public float MaxSides () { float max=-2000000000; for (int i=0; i<5; i++) { if (i==2) continue; if (vals[i]>max) max = vals[i];} return max; }
				public float Sum () { float sum=0; for (int i=0; i<5; i++) sum += vals[i]; return sum; }
				public float SumSides () { float sum=0; for (int i=0; i<5; i++) { if (i==2) continue; sum += vals[i];} return sum; }
				public float Avg () { return Sum() / 5f; }
				public float AvgSides () { return  SumSides() / 4f; }

				public static Cross ClampMax (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = Mathf.Max(f,c.vals[i]); return c; }
				public static Cross ClampMin (Cross c, float f) { for (int i=0; i<5; i++) c.vals[i] = Mathf.Min(f,c.vals[i]); return c; }

				public static Cross Pour (Cross height, float liquid)
				{
					//if (liquid < 0.0000001f) return new Cross(0);

					//initial avg scatter
					float sum = height.Sum() + liquid;
					float avg = sum / 5;

					Cross pour = new Cross(avg);
					pour -= height;
					pour = ClampMax(pour, 0);
					//now liquids sum is larger than original

					//lowering all of the liquid cells
					int liquidCellsCount = 0;
					float currentLiquidSum = 0;
					for (int i=0; i<5; i++)
					{
						float val = pour.vals[i];
						if (val > 0.0001f) liquidCellsCount++;
						currentLiquidSum += val;
					}
					if (liquidCellsCount == 0) return pour; //should not happen
					float lowerAmount = (pour.Sum() - liquid) / liquidCellsCount;
					pour = pour - lowerAmount;
					pour = ClampMax(pour, 0);

					//in most cases now the delta is 0, but sometimes it's still needs to be adjusted
					if (Mathf.Abs(pour.Sum() - liquid) > 0.00001f)
					{
						if (Mathf.Abs(pour.Sum()) < 0.000001f) return new Cross(0); //this is 100% needed
						float factor = liquid / pour.Sum();
						pour *= factor;
					}

					return pour;
				}
			}
			public struct MooreCross
			{
				//public float xz; public float z; public float Xz;
				//public float x; public float c; public float X;
				//public float xZ; public float Z; public float XZ;

				public float[] vals;

				public static MooreCross Zero () { return new MooreCross() { vals = new float[9] }; }

				public MooreCross (float[] m, int i, int sizeX)
				{
					//xz = m[i-1-sizeX];	z = m[i-sizeX];		Xz = m[i+1-sizeX];
					//x = m[i-1];			c = m[i];			X = m[i+1];
					//xZ = m[i-1+sizeX];	Z = m[i+sizeX];		XZ = m[i+1+sizeX]; 

					vals = new float[]
					{
						m[i-1-sizeX],	m[i-sizeX],		m[i+1-sizeX],
						m[i-1],			m[i],			m[i+1],
						m[i-1+sizeX],	m[i+sizeX],		m[i+1+sizeX]
					};
				}

				public void AddToMatrix (float[] m, int i, int sizeX)
				{
						m[i-1-sizeX] += vals[0];	m[i-sizeX] += vals[1];		m[i+1-sizeX] += vals[2];
						m[i-1] += vals[3];			m[i] += vals[4];			m[i+1] += vals[5];
						m[i-1+sizeX] += vals[6];	m[i+sizeX] += vals[7];		m[i+1+sizeX] += vals[8];
				}

				public void SetToMatrix (float[] m, int i, int sizeX)
				{
						m[i-1-sizeX] = vals[0];	m[i-sizeX] = vals[1];		m[i+1-sizeX] = vals[2];
						m[i-1] = vals[3];			m[i] = vals[4];			m[i+1] = vals[5];
						m[i-1+sizeX] = vals[6];	m[i+sizeX] = vals[7];		m[i+1+sizeX] = vals[8];
				}

				public static MooreCross operator + (MooreCross c1, MooreCross c2) { for (int i=0; i<9; i++) c1.vals[i] += c2.vals[i]; return c1; }
				public static MooreCross operator - (float f, MooreCross c) { for (int i=0; i<9; i++) c.vals[i] = f-c.vals[i]; return c; }
				public static MooreCross operator / (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = c.vals[i]/f; return c; }
				public static MooreCross operator * (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = c.vals[i]*f; return c; }
				public static MooreCross operator * (float f, MooreCross c) { for (int i=0; i<9; i++) c.vals[i] = c.vals[i]*f; return c; }

				public static MooreCross ClampMax (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = Mathf.Max(f,c.vals[i]); return c; }
				public static MooreCross ClampMin (MooreCross c, float f) { for (int i=0; i<9; i++) c.vals[i] = Mathf.Min(f,c.vals[i]); return c; }
				
				public float Sum () { float sum=0; for (int i=0; i<9; i++) sum += vals[i]; return sum; }
			}
		#endif

		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetOrder")]
			public static extern void SetOrder(Matrix refm, MatrixInt order);
		#else
			public static void SetOrder (Matrix refMatrix, Matrix2D<int> order)
			{
				int length = refMatrix.count;
				for (int i=0; i<length; i++) order.arr[i] = i;
				float[] refHeights = new float[length];
				Array.Copy(refMatrix.arr, refHeights, length);
				Array.Sort(refHeights, order.arr);

			//	int[] orderCopy = new int[refArray.Length];
			//	Array.Copy(orderArray, orderCopy, length);
			//	for (int i=0; i<orderCopy.Length; i++) orderArray[i] = orderCopy[orderCopy.Length-1-i];

			}
		#endif


		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MaskBorders")]
			public static extern void MaskBorders(MatrixInt order);
		#else
			public static void MaskBorders (Matrix2D<int> order)
			{
				for (int j=0; j<order.count; j++)
				{
					int pos = order.arr[j];

					int x = pos / order.rect.size.x;
					int z = pos % order.rect.size.x;

					if (x==0 || z==0 || x==order.rect.size.x-1 || z==order.rect.size.z-1) order.arr[j] = -1;
				}
			}
		#endif


		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateTorrents")]
			public static extern void CreateTorrents(Matrix heights, MatrixInt order, Matrix torrents);
		#else
			public static void CreateTorrents (Matrix heights, Matrix2D<int> order, Matrix torrents)
			{
					CoordRect rect = heights.rect;
					for (int i=0; i<heights.count; i++) torrents.arr[i] = 1; //casting initial rain
					
					for (int j=heights.count-1; j>=0; j--)
					{
						//finding column ordered by height
						int pos = order.arr[j];
						if (pos<0) continue;


						MooreCross height = new MooreCross(heights.arr, pos, rect.size.x);
						MooreCross torrent = new MooreCross(torrents.arr, pos, rect.size.x); //moore
						if (torrent.vals[4] > 200000000) torrent.vals[4] = 200000000;

						MooreCross delta = height.vals[4] - height;
						delta = MooreCross.ClampMax(delta, 0);

						MooreCross percents = MooreCross.Zero();
						float sum = delta.Sum();
						if (sum>0.00001f) percents = delta / sum;

						MooreCross newTorrent = percents*torrent.vals[4];
						newTorrent.AddToMatrix(torrents.arr, pos, rect.size.x);
					}
			}
		#endif


		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Erode")]
			public static extern void Erode(Matrix heights, Matrix torrents, Matrix mudflow, MatrixInt order,
				float erosionDurability = 0.9f, float erosionAmount = 1, float sedimentAmount = 0.5f);
		#else
			public static void Erode (Matrix heights, Matrix torrents, Matrix mudflow, Matrix2D<int> order,
				float erosionDurability=0.9f, float erosionAmount=1f, float sedimentAmount=0.5f)
			{
					CoordRect rect = heights.rect;
					for (int i=0; i<mudflow.count; i++) mudflow.arr[i] = 0;

					for (int j=heights.count-1; j>=0; j--)
					{
						//finding column ordered by height
						int pos = order.arr[j];
						if (pos<0) continue;


						Cross height = new Cross(heights.arr, pos, rect.size.x);
						float h_min = height.Min();

						//getting height values
//						float[] m = heights; int i=pos; int sizeX = rect.size.x;
//						float h = m[i]; float hx = m[i-1]; float hX = m[i+1]; float hz = m[i-sizeX]; float hZ = m[i+sizeX];

						//height minimum
//						float h_min = h;
//						if (hx<h_min) h_min=hx; if (hX<h_min) h_min=hX; if (hz<h_min) h_min=hz; if (hZ<h_min) h_min=hZ;


						//erosion line
						float erodeLine = (heights.arr[pos] + h_min)/2f; //halfway between current and maximum height
						if (heights.arr[pos] < erodeLine) continue;

						//raising soil
						float raised = heights.arr[pos] - erodeLine;
						float maxRaised = raised*(torrents.arr[pos]-1) * (1-erosionDurability);
						if (raised > maxRaised) raised = maxRaised;
						raised *= erosionAmount;

						//saving arrays
						heights.arr[pos] -= raised;
						mudflow.arr[pos] += raised * sedimentAmount;
						//if (erosion != null) erosion.array[pos] += raised; //and writing to ref
					}
					
					//for (int i=0; i<heights.Length; i++) 
					//	if (float.IsNaN(heights[i])) Debug.Log("NaN"); 
			}
		#endif


		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
			[DllImport ("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "TransferSettleMudflow")]
			public static extern void TransferSettleMudflow(Matrix heights, Matrix mudflow, Matrix sediments, MatrixInt order, int erosionFluidityIterations = 3);
		#else
			public static void TransferSettleMudflow(Matrix heights, Matrix mudflow, Matrix sediments, Matrix2D<int> order, int erosionFluidityIterations = 3)
			{
				TransferMudflow (heights, mudflow, sediments, order, erosionFluidityIterations);
				SettleMudflow (heights, mudflow, order, ruffle:1f);
			}
		
			public static void TransferMudflow (Matrix heights, Matrix mudflow, Matrix sediments, Matrix2D<int> order, int erosionFluidityIterations=3)
			{
				CoordRect rect = heights.rect;
				for (int i=0; i<sediments.count; i++) sediments.arr[i] = 0;

				#region Settling sediment

					for (int l=0; l<erosionFluidityIterations; l++)
					for (int j=heights.count-1; j>=0; j--)
					{				
						//finding column ordered by height
						int pos = order.arr[j];
						if (pos<0) continue;

						Cross height = new Cross(heights.arr, pos, rect.size.x);
						Cross sediment = new Cross(mudflow.arr, pos, rect.size.x);

						float sedimentSum = sediment.Sum();
						if (sedimentSum < 0.00001f) continue;

						Cross pour = Cross.Pour(height, sedimentSum);

						pour.SetToMatrix(mudflow.arr, pos, rect.size.x);
						if (sediments != null) pour.AddToMatrix(sediments.arr, pos, rect.size.x);
				}

				//for (int i=0; i<heights.Length; i++) 
				//	if (float.IsNaN(heights[i])) Debug.Log("NaN");
				
				#endregion
			}

			public static void SettleMudflow (Matrix heights, Matrix mudflow, Matrix2D<int> order, float ruffle=0.1f)
			{
				
				
				//int seed = 12345;
				for(int j=heights.count-1; j>=0; j--) 
				{
					//writing heights
					heights.arr[j] += mudflow.arr[j];
					
					/*seed = 214013*seed + 2531011; 
					float random = ((seed>>16)&0x7FFF) / 32768f;

					int pos = order[j];
					if (pos<0) continue;

					//float[] m = heights; int sizeX = rect.size.x;
					//float h = m[pos]; float hx = m[pos-1]; float hX = m[pos+1]; float hz = m[pos-sizeX]; float hZ = m[pos+sizeX];
					Cross height = new Cross(heights, pos, rect.size.x);

					//smoothing sediments a bit
					float s = mudflow[pos];
					if (s > 0.0001f)
					{
						float smooth = s/2f; if (smooth > 0.75f) smooth = 0.75f; 
						heights[pos] = heights[pos]*(1-smooth) + height.AvgSides()*smooth;
					}

					else
					{
						float maxHeight = height.MaxSides();
						float minHeight = height.MinSides();
						float randomHeight = random*(maxHeight-minHeight) + minHeight;
					//	heights[pos] = heights[pos]*(1-ruffle) + randomHeight*ruffle;
					}*/
				}
			}
		#endif

	}//class
}//namespace
