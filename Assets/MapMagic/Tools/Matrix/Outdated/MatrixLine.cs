// Operations with "maps"
// Note that functions are not inlined in editor, so keeping all method unfolded

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Den.Tools 
{
	[Serializable, StructLayout (LayoutKind.Sequential)] //to pass to native
	public class MatrixLine
	/// A single matrix line (or row) to be used in fast native matrix operations
	{
		public int offset; //IDEA: use two coordinates in read/write line. IDEA: don't keep offset parameter. Line operations does not use it except read/write
		public int length; //for c++ compatibility and for re-use cases (when array is longer than length)
		public float[] arr;

		public MatrixLine (int offset, int length)
		{
			arr = new float[length];
			this.offset = offset;
			this.length = length;
		}

		/*public Line (int length)
		{
			arr = new float[length];
			this.length = length;
		}*/

		public MatrixLine (float[] arr, int offset, int length)
		{
			this.arr = arr;
			this.offset = offset;
			this.length = length;
		}

		#region Sampling

			public static void ResampleCubic (MatrixLine src, MatrixLine dst)
			/// Scales the line filling dst with interpolated values. Cubic for upscale
			{
				for (int x=0; x<dst.length; x++)
				{
					float percent = 1.0f * x / dst.length;
					float sx = percent * src.length;
					dst.arr[x] = src.CubicSample(sx);
				}
			}

			public static void ResampleLinear (MatrixLine src, MatrixLine dst)
			/// Scales the line filling dst with interpolated values. Linear for downscale
			{
				float radius = 1.0f * src.length / dst.length;

				for (int x=0; x<dst.length; x++)
				{
					float percent = 1.0f * x / dst.length;
					float sx = percent * src.length;
					dst.arr[x] = src.LinearSample(sx, radius);
				}
			}

			public static void DownsampleFast (MatrixLine src, MatrixLine dst, int ratio)
			/// Scales the line filling dst with interpolated values. Linear for downscale
			{
				for (int x=0; x<dst.length; x++)
				{
					float sumVal = 0;
					for (int ix=0; ix<ratio; ix++)
						sumVal += src.arr[x*ratio + ix];
					dst.arr[x] = sumVal / ratio;
				}
			}

			public float CubicSample (float x)
			/// Interpolated sampling for upscaling
			{
				int p = (int)x;		if (p<0) p=0;
				int n = p+1;		if (n>length-1) n = length-1;
				int pp = p-1;		if (pp<0) pp = 0;
				int nn = n+1;		if (nn>length-1) nn = length-1;

				float percent = x-p;

				float vp = arr[p]; float vpp = arr[pp];
				float vn = arr[n]; float vnn = arr[nn];

				return vp + 0.5f * percent * (vn - vpp + percent*(2.0f*vpp - 5.0f*vp + 4.0f*vn - vnn + percent*(3.0f*(vp - vn) + vnn - vpp)));
			}

			public float LinearSample (float x, float radius)
			/// Weighted radius sampling for downscaling
			{
				int ix = (int)x;
				int iRadius = (int)(radius+0.5f);

				float factorSum = 0;
				float valueSum = 0;

				for (int rx = ix-iRadius+1; rx < ix+iRadius+1; rx++)  
				//skipping edge vertices since their factor is 0
				//evaluating +1 point if x is between ix and ix+1
				{
					float dist = x - rx;
					if (dist < 0) dist = -dist;

					float factor = 1 - dist/radius;
					if (factor<0) factor = 0;

					int crx = rx;
					if (crx<0) crx = 0;
					if (crx>length-1) crx = length-1;

					factorSum += factor;
					valueSum += arr[crx] * factor;
				}

				return valueSum / factorSum;
			}

			public float AverageSample (int x)
			{
				int p = x-1;		if (p<0) p=0;
				int n = x+1;		if (n>length-1) n = length-1;

				float vp = arr[p];
				float vx = arr[x];
				float vn = arr[n];

				return vp*0.25f + vx*0.5f + vn*0.25f;
			}

		#endregion


		#region Operations

			public void Spread (float subtract=0.01f, float multiply=1f)
			/// Spreads higher values (whites) over the lower (darker) ones
			// Warning: spreading with multiply is WRONG! 
			// It will not steer the corner
			{
				//to right
				float prevVal = arr[0];
				for (int x=1; x<length-1; x++)
				{
					float val = arr[x];
					if (prevVal > val)
					{
						val = prevVal*multiply - subtract*(1-val/prevVal);
						if (val<0) val =0;

						arr[x] = val;
					}
					prevVal = val;
				}

				//to left
				prevVal = arr[length-1];
				for (int x=length-2; x>=0; x--)
				{
					float val = arr[x];
					if (prevVal > val)
					{
						val = prevVal*multiply - subtract*(1-val/prevVal);
						if (val<0) val =0;

						arr[x] = val;
					}
					prevVal = val;
				}
			}


			public void SpreadMultiply (float multiply=1f)
			{
				//to right
				float prevVal = arr[0];
				for (int x=1; x<length-1; x++)
				{
					float currVal = arr[x];

					if (prevVal > currVal)
					{
						currVal = currVal*(1-multiply) + prevVal*multiply;
						arr[x] = currVal;
					}

					prevVal = currVal;
				}

				//to left
				prevVal = arr[length-1];
				for (int x=length-2; x>=0; x--)
				{
					float currVal = arr[x];

					if (prevVal > currVal)
					{
						currVal = currVal*(1-multiply) + prevVal*multiply;
						arr[x] = currVal;
					}

					prevVal = currVal;
				}
			}

			public void Cavity (float intensity=1)
			{
				float prev = arr[0];
				float curr = arr[1];

				for (int x=1; x<length-1; x++)
				{
					//float prev = src.arr[x-1];
					//float curr = src.arr[x];
					float next = arr[x+1];

					float val = curr - (next + prev)/2;
					float sign = val>0 ? 1 : -1;
					val = (val*val*sign)*intensity*1000; 
					val = (val+1) / 2;
					arr[x] = val;

					prev = curr;
					curr = next;
				}
				arr[0] = arr[1];
				arr[length-1] = arr[length-2];
			}

				
			public void Delta ()
			{
				float prev = arr[0];
				float curr = arr[1];

				for (int x=1; x<length-1; x++)
				{
					//float prev = arr[x-1];
					//float curr = arr[x];
					float next = arr[x+1];
						
					float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
					float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
					float delta = prevDelta>nextDelta? prevDelta : nextDelta; 
					
					if (delta > arr[x]) arr[x] = delta;

					prev = curr;
					curr = next;
				}
				arr[0] = arr[1];
				arr[length-1] = arr[length-2];
			}


			public void Normal (float height)
			/// Will return normal direction in range -1, 1
			{
				float prev = arr[0];
				float curr = arr[1];

				for (int x=1; x<length-1; x++)
				{
					//float prev = arr[x-1];
					//float curr = arr[x];
					float next = arr[x+1];
						
					//Vector3(prev-next, height, 0)).normalized;
					float delta = prev-next;
					float magnitude = Mathf.Sqrt(delta*delta + height*height + delta*delta);
					float normal = delta*magnitude;
					normal = (normal+1) / 2;

					arr[x] = normal;

					prev = curr;
					curr = next;
				}
				arr[0] = arr[1];
				arr[length-1] = arr[length-2];
			}


			public void GaussianBlur (float[] tmp, float blur)
			{
				int iterations = (int)blur;

				MatrixLine src = new MatrixLine(arr, 0, length); //for switching arrays between iterations
				MatrixLine dst = new MatrixLine(tmp, 0, length);

				//iteration blur
				for (int i=0; i<iterations; i++)
				{
					for (int x=0; x<length; x++)
						dst.arr[x] = src.AverageSample(x);

					float[] t = src.arr;
					src.arr = dst.arr;
					dst.arr = t;
				}

				//last iteration - percentage
				float percent = blur - iterations;
				if (percent > 0.0001f)
				{
					for (int x=0; x<length; x++)
						dst.arr[x] = src.AverageSample(x)*percent + src.arr[x]*(1-percent);

					float[] t = src.arr;
					src.arr = dst.arr;
					dst.arr = t;
				}

				//copy values to arr for non-even iteration count
				for (int x=0; x<length; x++)
					dst.arr[x] = src.arr[x];
			}


			public void DownsampleBlur (int downsample, float blur, MatrixLine tmpDownsized, MatrixLine tmpBlur)
			/// both temp lines length is length/downsample
			{
				ResampleLinear(this, tmpDownsized);
				tmpDownsized.GaussianBlur(tmpBlur.arr, blur);
				ResampleCubic(tmpDownsized, this);
			}


			public static void SpreadBlur (ref MatrixLine curr, ref MatrixLine prev, int blur)
			{
				for (int x=0; x<curr.arr.Length; x++)
				{
					if (curr.arr[x] > 0.001f) continue;

					float val = 0;
					float sum = 0;

					if (prev.arr[x] > 0.001f)	 { val += prev.arr[x]; sum++; }

					for (int i=1; i<=blur; i++)
					{
						if (x-i >= 0  &&  prev.arr[x-i] > 0.001f)					{ val += prev.arr[x-i]; sum++; }
						if (x+i < prev.arr.Length-1  &&  prev.arr[x+i] > 0.001f)	{ val += prev.arr[x+i]; sum++; }
					}

					if (sum != 0)
						curr.arr[x] = val / sum;
				}

				//swapping lines
				MatrixLine tmp = prev;
				prev = curr;
				curr = tmp;
			}


			public void PredictExtend (int start)
			/// Averages the line behavior before start and continues it after start
			{
				//int max = start < length-start ? start : length-start;  //iterating both in two ways from start, whatever is less
				
				float negPrev = arr[0];
				float posPrev = arr[0];

				//finding the average pivot and vector
				float prevVals = 0; //the values starting from start and going deeper into array. And smoother
				float prevVector = 0;
				float prevSum = 0;
				for (int x=0; x<start; x++) 
				{
					float negCurr = arr[x];

					float weight = 1f; // 1f / (x+1);
					//weight = 1 - (1-weight)*(1-weight);

					prevVals += negCurr * weight;
					prevVector += (negCurr-negPrev) * weight;  //IDEA: if weight=1 avgVector is just a difference between first and last. No need to calc it per-i
					prevSum += weight;

					negPrev = negCurr;
				}

				float avgPivot = prevVals / prevSum;
				float avgVector = prevVector / prevSum;

				//shifting pivot to make it start according to avgVector
				avgPivot += avgVector * (start/2f);

				avgPivot = arr[start-1];

				//applying
				negPrev = arr[start];
				posPrev = arr[start];
				float pivotPrev = avgPivot;
				
				for (int x=start; x<arr.Length; x++) 
				{
					float pivotCurr = pivotPrev + avgVector;
					arr[x] = pivotCurr; //posCurr*(1-pivotBlend) + pivotCurr*pivotBlend;
					pivotPrev = pivotCurr;
				}
			}


			public void FillGaps (int start=0, int end=0)
			{
				if (start==0 && end==0) end = length;

				int gapSize = 0;
				float gapStartVal = 0;
				float gapEndVal = 0;

				//finding first start value
				int firstDefined = start;
				while (arr[firstDefined] < 0  &&  firstDefined < end)
					firstDefined++;
				gapStartVal = arr[firstDefined];

				//filling gaps
				for (int x=start; x<end; x++)
				{
					float val = arr[x];
					
					//if val is gap
					if (val < 0) gapSize ++;

					//if gap just ended
					else if (gapSize != 0)
					{
						gapEndVal = val;
						for (int ix=0; ix<gapSize; ix++)
						{
							float percent = 1f * (ix+1) / (gapSize+1);
							arr[x-gapSize+ix] = gapStartVal*(1-percent) + gapEndVal*percent;
						}
						gapSize = 0;
						gapStartVal = val;
					}

					//if no gap
					else
						gapStartVal = val;
				}

				//filling the remaining dist if ent is gap
				if (gapSize != 0)
					for (int ix=0; ix<gapSize; ix++)
						arr[end-gapSize+ix] = gapStartVal;
			}

			public void FillGapsOld (int start=0, int end=0, MatrixLine tmpFront=null, MatrixLine tmpBack=null, MatrixLine tmpFrontDist=null, MatrixLine tmpBackDist=null)
			{
				if (start==0 && end==0) end = length;

				if (tmpFront==null) tmpFront = new MatrixLine(offset, length);
				if (tmpBack==null) tmpBack = new MatrixLine(offset, length);
				if (tmpFrontDist==null) tmpFrontDist = new MatrixLine(offset, length);
				if (tmpBackDist==null) tmpBackDist = new MatrixLine(offset, length);

				//filling front line
				float prevVal = -1;
				float prevDist = 0;
				for (int x=start; x<end; x++)
				{
					float val = arr[x];
					if (val >= 0) 
					{
						prevVal = val;
						prevDist = 0;
					}

					tmpFront.arr[x] = prevVal;
					tmpFrontDist.arr[x] = prevDist;
					prevDist++;
				}

				//extended front line with delta vector - works fine but the result is so-so
				/*float prevVal = -1;
				float prevDist = 0;
				float prevDelta = 0;
				bool prevEnabled = false; //to avoid calculating delta on single pixels
				for (int x=0; x<length; x++)
				{
					float val = arr[x];
					if (val >= 0) 
					{
						prevDelta = val-prevVal;
						prevVal = val;
						prevDist = 0;

						if (!prevEnabled) 
						prevDelta = 0; //linear delta if prev pixel is a gap
						prevEnabled = true;
					}
					else
					{
						prevDist++;
						prevVal += prevDelta * (1f/prevDist);
					}

					tmpFront.arr[x] = prevVal;
					tmpFrontDist.arr[x] = prevDist;
				}*/

				//back line
				prevVal = -1;
				prevDist = 0;
				for (int x=end-1; x>=start; x--)
				{
					float val = arr[x];
					if (val >= 0) 
					{
						prevVal = val;
						prevDist = 0;
					}

					tmpBack.arr[x] = prevVal;
					tmpBackDist.arr[x] = prevDist;
					prevDist++;
				}

				//blending lines
				for (int x=start; x<end; x++)
				{
					//float val = arr[x];
					//if (val >= 0) continue;
					
					//float frontVal = tmpFront.arr[x];
					//float backVal = tmpBack.arr[x];

					float distSum = tmpFrontDist.arr[x] + tmpBackDist.arr[x];
					if (distSum == 0) continue;
					arr[x] = tmpFront.arr[x]*(tmpBackDist.arr[x]/distSum) + tmpBack.arr[x]*(tmpFrontDist.arr[x]/distSum); //note that front and back factors are inverted!
				}
			}

		#endregion


		#region Arithmetic

			public void Add (float f) { for (int i = 0; i<length; i++) arr[i] += f; }
			public void Fill (float f) { for (int i = 0; i<length; i++) arr[i] = f; }
			public void Max (MatrixLine l) 
			{ 
				for (int i = 0; i<length; i++)
				{
					float v1 = arr[i];
					float v2 = l.arr[i];
					arr[i] = v1>v2 ? v1 : v2;
				}
			}

			public void Gradient (float startVal, float endVal)
			{
				for (int i = 0; i<length; i++)
				{
					float percent = 1f * i / length;
					arr[i] = startVal*(1-percent) + endVal*percent;
				}
			}

			public void Invert () { for (int i = 0; i<length; i++) arr[i] = 1-arr[i]; }

			public float Average (bool skipNegative=false)
			{
				float sum = 0;
				int count = 0;
				for (int i = 0; i<length; i++) 
				{
					float val = arr[i];
					if (!skipNegative  ||  val>0) { sum += val; count++; }
				}
				if (count == 0) return -Mathf.Epsilon;
				else return sum / count;
			}

		#endregion


		#region Reading/Writing Matrix

			//All line readings are zero-based (no offset)

			public void ReadLine (FloatMatrix matrix, int z)
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  offset;
				for (int x=0; x<length; x++)
					arr[x] = matrix.arr[start+x];  //matrix[x+offset, z];
			}

			public void ReadRow (FloatMatrix matrix, int x)
			{
				int start = (offset-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
				for (int z=0; z<length; z++)
					arr[z] = matrix.arr[start + z*matrix.rect.size.x];  //matrix[x, z+offset];
			}
			
			public void WriteLine (FloatMatrix matrix, int z)
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x   +  offset;
				for (int x=0; x<length; x++)
					matrix.arr[start+x] = arr[x];  //matrix[x+offset, z];
			}

			public void WriteLine (FloatMatrix matrix, int z, MatrixLine mask)
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x   +  offset;
				for (int x=0; x<length; x++)
					matrix.arr[start+x] = arr[x] * mask.arr[x];  //matrix[x+offset, z];
					
			}

			public void AppendLine (FloatMatrix matrix, int z)
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x   +  offset;
				for (int x=0; x<length; x++)
					matrix.arr[start+x] += arr[x];  //matrix[x+offset, z];
			}

			public void AppendLine (FloatMatrix matrix, int z, MatrixLine mask)
			{
				int start = (z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x   +  offset;
				for (int x=0; x<length; x++)
					matrix.arr[start+x] += arr[x] * mask.arr[x];  //matrix[x+offset, z];
			}

			public void WriteRow (FloatMatrix matrix, int x)
			{
				int start = (offset-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
				for (int z=0; z<length; z++)
					matrix.arr[start + z*matrix.rect.size.x] = arr[z];
			}

			public void WriteRow (FloatMatrix matrix, int x, MatrixLine mask)
			{
				int start = (offset-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
				for (int z=0; z<length; z++)
					matrix.arr[start + z*matrix.rect.size.x] = arr[z] * mask.arr[z];
			}

			public void AppendRow (FloatMatrix matrix, int x)
			{
				int start = (offset-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
				for (int z=0; z<length; z++)
					matrix.arr[start + z*matrix.rect.size.x] += arr[z];
			}

			public void AppendRow (FloatMatrix matrix, int x, MatrixLine mask)
			{
				int start = (offset-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;
				for (int z=0; z<length; z++)
					matrix.arr[start + z*matrix.rect.size.x] += arr[z] * mask.arr[z];
			}


			//inclined lines
			public void ReadInclined (FloatMatrix matrix, Vector2 start, Vector2 step)
			{
				for (int i=0; i<length; i++)
				{
					float x = start.x + step.x*i;
					float z = start.y + step.y*i;

					if (x<0) x--;  int ix = (int)(float)(x + 0.5f);
					if (z<0) z--;  int iz = (int)(float)(z + 0.5f);

					if (ix<matrix.rect.offset.x || ix>=matrix.rect.offset.x+matrix.rect.size.x ||
						iz<matrix.rect.offset.z || iz>=matrix.rect.offset.z+matrix.rect.size.z )
							arr[i] = -Mathf.Epsilon;
					else 
					{
						int pos = (iz-matrix.rect.offset.z)*matrix.rect.size.x + ix - matrix.rect.offset.x;
						arr[i] = matrix.arr[pos];
					}
				}
			}

			public void WriteInclined (FloatMatrix matrix, Vector2 start, Vector2 step)
			{
				for (int i=0; i<length; i++)
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
						matrix.arr[pos] = arr[i];
					}
				}
			}


			public void ReadCircular (FloatMatrix matrix, Coord center, float radius)
			{
				int counter = 0;

				ReadArcX(matrix, center, radius, ref counter);
				ReadArcZ(matrix, center, radius, ref counter, reverse:true);

				ReadArcZ(matrix, center, radius, ref counter, flipZ:-1);
				ReadArcX(matrix, center, radius, ref counter, flipZ:-1, reverse:true);
			
				ReadArcX(matrix, center, radius, ref counter, flipX:-1, flipZ:-1);
				WriteArcZ(matrix, center, radius, ref counter, flipX:-1, flipZ:-1,  reverse:true);

				ReadArcZ(matrix, center, radius, ref counter, flipX:-1);
				ReadArcX(matrix, center, radius, ref counter, flipX:-1, reverse:true);
			}


			public void WriteCircular (FloatMatrix matrix, Coord center, float radius)
			{
				int counter = 0;

				WriteArcX(matrix, center, radius, ref counter);
				WriteArcZ(matrix, center, radius, ref counter, reverse:true);

				WriteArcZ(matrix, center, radius, ref counter, flipZ:-1);
				WriteArcX(matrix, center, radius, ref counter, flipZ:-1, reverse:true);
			
				WriteArcX(matrix, center, radius, ref counter, flipX:-1, flipZ:-1);
				WriteArcZ(matrix, center, radius, ref counter, flipX:-1, flipZ:-1,  reverse:true);

				WriteArcZ(matrix, center, radius, ref counter, flipX:-1);
				WriteArcX(matrix, center, radius, ref counter, flipX:-1, reverse:true);
			}

			private void ReadArcX (FloatMatrix matrix, Coord center, float radius, ref int counter, int flipX=1, int flipZ=1, bool reverse=false)
			{
				int radius45 = (int)(radius*0.7071068f + 1);

				for (int ix=0; ix<radius45; ix++)
				{
					int x = reverse ? radius45-ix : ix;

					float fz = Mathf.Sqrt(radius*radius - x*x);
					if (fz<0) fz--;  int z = (int)(float)(fz + 0.5f); //presuming here center is 0,0

					int cx = center.x + x*flipX;
					int cz = center.z + z*flipZ;

					int pos = (cz-matrix.rect.offset.z)*matrix.rect.size.x + cx - matrix.rect.offset.x;
					arr[counter] = matrix.arr[pos];
					counter++;
				}
			}

			private void ReadArcZ (FloatMatrix matrix, Coord center, float radius, ref int counter, int flipX=1, int flipZ=1, bool reverse=false)
			{
				int radius45 = (int)(radius*0.7071068f + 1);

				for (int iz=0; iz<radius45; iz++)
				{
					int z = reverse ? radius45-iz : iz;

					float fx = Mathf.Sqrt(radius*radius - z*z);
					if (fx<0) fx--;  int x = (int)(float)(fx + 0.5f);

					int cx = center.x + x*flipX;
					int cz = center.z + z*flipZ;

					int pos = (cz-matrix.rect.offset.z)*matrix.rect.size.x + cx - matrix.rect.offset.x;
					arr[counter] = matrix.arr[pos];
					counter++;
				}
			}


			private void WriteArcX (FloatMatrix matrix, Coord center, float radius, ref int counter, int flipX=1, int flipZ=1, bool reverse=false)
			{
				int radius45 = (int)(radius*0.7071068f + 1);

				for (int ix=0; ix<radius45; ix++)
				{
					int x = reverse ? radius45-ix : ix;

					float fz = Mathf.Sqrt(radius*radius - x*x);
					if (fz<0) fz--;  int z = (int)(float)(fz + 0.5f); //presuming here center is 0,0

					int cx = center.x + x*flipX;
					int cz = center.z + z*flipZ;

					int pos = (cz-matrix.rect.offset.z)*matrix.rect.size.x + cx - matrix.rect.offset.x;
					matrix.arr[pos] = arr[counter];
					counter++;
				}
			}

			private void WriteArcZ (FloatMatrix matrix, Coord center, float radius, ref int counter, int flipX=1, int flipZ=1, bool reverse=false)
			{
				int radius45 = (int)(radius*0.7071068f + 1);

				for (int iz=0; iz<radius45; iz++)
				{
					int z = reverse ? radius45-iz : iz;

					float fx = Mathf.Sqrt(radius*radius - z*z);
					if (fx<0) fx--;  int x = (int)(float)(fx + 0.5f);

					int cx = center.x + x*flipX;
					int cz = center.z + z*flipZ;

					int pos = (cz-matrix.rect.offset.z)*matrix.rect.size.x + cx - matrix.rect.offset.x;
					matrix.arr[pos] = arr[counter];
					counter++;
				}
			}


			public void ReadSquare (FloatMatrix matrix, Coord center, int radius)
			/// Same as circular, but in form of square. 4 lines one-by-one. Useful for blurs and spreads
			{
				int side = radius*2 + 1;

				//resetting line
				for (int i=0; i<side*4; i++)
					arr[i] = - Mathf.Epsilon;

				Coord min = center-radius;
				Coord max = center+radius;

				Coord rectMin = matrix.rect.offset;
				Coord rectMax = matrix.rect.offset + matrix.rect.size;

				int start = (min.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x;	//matrix[min.x, min.z]
				if (min.z-1 >= rectMin.z  &&  min.z-1 < rectMax.z)
					for (int x=0; x<side; x++)
						if (x+min.x >= rectMin.x  &&  x+min.x < rectMax.x)
							arr[x] = matrix.arr[start+x];
				
				start = (min.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x;	//matrix[max.x, min.z]
				if (max.x >= rectMin.x  &&  max.x < rectMax.x)
					for (int z=0; z<side; z++)
						if (z+min.z >= rectMin.z  &&  z+min.z < rectMax.z)
							arr[z+side] = matrix.arr[start + z*matrix.rect.size.x];

				start = (max.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x - 1;	//matrix[max.x-1, max.z]
				if (max.z >= rectMin.z  &&  max.z < rectMax.z)
				for (int x=0; x<side; x++)
					if (max.x-1-x >= rectMin.x  &&  max.x-1-x < rectMax.x)
						arr[x+side*2] = matrix.arr[start -x];

				start = (max.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x - 1;	//matrix[min.x-1, max.z-1]
				if (min.x-1 >= rectMin.x  &&  min.x-1 < rectMax.x)
				for (int z=0; z<side; z++)
					if (max.z-1-z >= rectMin.z  &&  max.z-1-z < rectMax.z)
						arr[z+side*3] = matrix.arr[start - z*matrix.rect.size.x]; //matrix[min.x, max.z-1-z]
			}


			public void WriteSquare (FloatMatrix matrix, Coord center, int radius)
			/// Same as circular, but in form of square. 4 lines one-by-one. Useful for blurs and spreads
			/// Line length should be radius*8 + 4 corners
			{
				int side = radius*2 + 1;

				Coord min = center-radius;
				Coord max = center+radius;

				Coord rectMin = matrix.rect.offset;
				Coord rectMax = matrix.rect.offset + matrix.rect.size;

				int start = (min.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x;	//matrix[min.x, min.z]
				if (min.z-1 >= rectMin.z  &&  min.z-1 < rectMax.z)
					for (int x=0; x<side; x++)
						if (x+min.x >= rectMin.x  &&  x+min.x < rectMax.x)
							matrix.arr[start+x] = arr[x];
				
				start = (min.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x;	//matrix[max.x, min.z]
				if (max.x >= rectMin.x  &&  max.x < rectMax.x)
					for (int z=0; z<side; z++)
						if (z+min.z >= rectMin.z  &&  z+min.z < rectMax.z)
							matrix.arr[start + z*matrix.rect.size.x] = arr[z+side];

				start = (max.z-matrix.rect.offset.z)*matrix.rect.size.x - matrix.rect.offset.x  +  max.x - 1;	//matrix[max.x-1, max.z]
				if (max.z >= rectMin.z  &&  max.z < rectMax.z)
				for (int x=0; x<side; x++)
					if (max.x-1-x >= rectMin.x  &&  max.x-1-x < rectMax.x)
						matrix.arr[start -x] = arr[x+side*2];

				start = (max.z-matrix.rect.offset.z-1)*matrix.rect.size.x - matrix.rect.offset.x  +  min.x - 1;	//matrix[min.x-1, max.z-1]
				if (min.x-1 >= rectMin.x  &&  min.x-1 < rectMax.x)
				for (int z=0; z<side; z++)
					if (max.z-1-z >= rectMin.z  &&  max.z-1-z < rectMax.z)
						matrix.arr[start - z*matrix.rect.size.x] = arr[z+side*3]; //matrix[min.x, max.z-1-z]
			}

		#endregion

	} //class

}//namespace