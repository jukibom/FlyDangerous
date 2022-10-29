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
	public class FloatMatrix : Matrix2D<float>
	{
		public float GetInterpolatedValue (Vector2 pos) //for upscaling - gets value in-between two points
		{
			int x = Mathf.FloorToInt(pos.x); int z = Mathf.FloorToInt(pos.y);
			float xPercent = pos.x-x; float zPercent = pos.y-z;

			//if (!rect.CheckInRange(x+1,z+1)) return 0;

			float val1 = this[x,z];
			float val2 = this[x+1,z];
			float val3 = val1*(1-xPercent) + val2*xPercent;

			float val4 = this[x,z+1];
			float val5 = this[x+1,z+1];
			float val6 = val4*(1-xPercent) + val5*xPercent;
			
			return val3*(1-zPercent) + val6*zPercent;
		}

		public float GetAveragedValue (int x, int z, int steps) //for downscaling
		{
			float sum = 0;
			int div = 0;
			for (int ix=0; ix<steps; ix++)
				for (int iz=0; iz<steps; iz++)
			{
				if (x+ix >= rect.offset.x+rect.size.x) continue;
				if (z+iz >= rect.offset.z+rect.size.z) continue;
				sum += this[x+ix, z+iz];
				div++;
			}
			return sum / div;
		}

		#region Overriding constructors and clone
			
			public FloatMatrix () { arr = new float[0]; rect = new CoordRect(0,0,0,0); count = 0; } //for serializer

			public FloatMatrix (int offsetX, int offsetZ, int sizeX, int sizeZ, float[] array=null)
			{
				this.rect = new CoordRect(offsetX, offsetZ, sizeX, sizeZ);;
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.arr = array;
				else this.arr = new float[count];
			}

			public FloatMatrix (CoordRect rect, float[] array=null)
			{
				this.rect = rect;
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.arr = array;
				else this.arr = new float[count];
			}

			public FloatMatrix (Coord offset, Coord size, float[] array=null)
			{
				rect = new CoordRect(offset, size);
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.arr = array;
				else this.arr = new float[count];
			}

			public FloatMatrix (FloatMatrix src)
			{
				rect = src.rect;
				count = src.count;

				arr = new float[src.arr.Length];
				Array.Copy(src.arr, arr, arr.Length);
			}

			public override object Clone () //IClonable
			{ 
				FloatMatrix result = new FloatMatrix(rect);
			
				//copy params
				result.rect = rect;
				result.count = count;
			
				//copy array
				if (result.arr.Length != arr.Length) result.arr = new float[arr.Length];
				Array.Copy(arr, result.arr, arr.Length);

				return result;
			} 


			[Obsolete] public FloatMatrix Copy (FloatMatrix result=null)
			{
				if (result==null) result = new FloatMatrix(rect);
			
				//copy params
				result.rect = rect;
				result.count = count;
			
				//copy array
				if (result.arr.Length != arr.Length) result.arr = new float[arr.Length];
				Array.Copy(arr, result.arr, arr.Length);

				return result;
			}

			public FloatMatrix CopyRegion (CoordRect region, FloatMatrix result=null) //SOON: optimize
			{
				if (result==null) result = new FloatMatrix(region);
			
				//copy params
				result.rect = region;
				result.count = region.Count;
			
				//copy array
				Coord min = region.Min; Coord max = region.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						if (x<rect.offset.x || x>rect.offset.x+rect.size.x ||
							z<rect.offset.z || z>rect.offset.z+rect.size.z) continue;
						result[x,z] = this[x,z];
					}

				return result;
			}

		#endregion

		#region Texture (obsolete)

			[Obsolete]
			public Texture2D ToTexture ()
			{ 
				Texture2D texture = new Texture2D(rect.size.x, rect.size.z);
				WriteIntersectingTexture(texture, rect.offset.x, rect.offset.z);
				return texture;
			}

			[Obsolete]
			public void WriteIntersectingTexture (Texture2D texture, int textureOffsetX, int textureOffsetZ, float rangeMin=0, float rangeMax=1)
			///Filling texture using both matrix and texture offsets. Filling only intersecting rect/texture are, leaving other unchanged.
			{
				//TODO: use LoadRawTextureData
				
				CoordRect textureRect = new CoordRect(textureOffsetX, textureOffsetZ, texture.width, texture.height);
				CoordRect intersect = CoordRect.Intersected(rect, textureRect);

				Color[] colors = new Color[intersect.size.x*intersect.size.z];

				Coord min = intersect.Min; Coord max = intersect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float val = this[x,z]; //TODO: direct
					val -= rangeMin;
					val /= rangeMax-rangeMin;

					colors[(z-min.z)*(max.x-min.x) + (x-min.x)] = new Color(val, val, val); //TODO: r should not be == r and ==b, there should be 1 byte diff
				}
			
				texture.SetPixels(
					intersect.offset.x-textureOffsetX,
					intersect.offset.z-textureOffsetZ,
					intersect.size.x,
					intersect.size.z,
					colors);
				texture.Apply();
			}

			[Obsolete]
			public void WriteTextureInterpolated (Texture2D texture, CoordRect textureRect, CoordRect.TileMode wrap=CoordRect.TileMode.Clamp, float rangeMin=0, float rangeMax=1)
			{
				float pixelSizeX = 1f * textureRect.size.x / texture.width;
				float pixelSizeZ = 1f * textureRect.size.z / texture.height;

				Rect pixelTextureRect = new Rect(0, 0, texture.width, texture.height);
				Rect pixelMatrixRect = new Rect(
					(textureRect.offset.x - rect.offset.x) / pixelSizeX,
					(textureRect.offset.z - rect.offset.z) / pixelSizeZ,
					rect.size.x/pixelSizeX, 
					rect.size.z/pixelSizeZ);

				Rect pixelIntersection = CoordinatesExtensions.Intersect(pixelTextureRect, pixelMatrixRect);

				CoordRect intersect = new CoordRect(
					Mathf.CeilToInt(pixelIntersection.x),
					Mathf.CeilToInt(pixelIntersection.y),
					Mathf.FloorToInt(pixelIntersection.width),
					Mathf.FloorToInt(pixelIntersection.height) );

				Color[] colors = new Color[intersect.size.x*intersect.size.z];

				Coord min = intersect.Min; Coord max = intersect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float wx = x*pixelSizeX - textureRect.offset.x + rect.offset.x*2;
					float wz = z*pixelSizeZ - textureRect.offset.z + rect.offset.z*2;

					//float val = this[x,z]; //TODO: direct
					float val = GetInterpolated(wx, wz);
					val -= rangeMin;
					val /= rangeMax-rangeMin;

					//val = 1;

					colors[(z-min.z)*(max.x-min.x) + (x-min.x)] = new Color(val, val, val); //TODO: r should not be == r and ==b, there should be 1 byte diff
				}

				texture.SetPixels(intersect.offset.x, intersect.offset.z, intersect.size.x, intersect.size.z, colors);
				texture.Apply();
			}

			[Obsolete]
			public Texture2D SimpleToTexture (Texture2D texture=null, Color[] colors=null, float rangeMin=0, float rangeMax=1, string savePath=null)
			{
				if (texture == null) texture = new Texture2D(rect.size.x, rect.size.z);
				if (texture.width != rect.size.x || texture.height != rect.size.z) texture.Resize(rect.size.x, rect.size.z);
				if (colors == null || colors.Length != rect.size.x*rect.size.z) colors = new Color[rect.size.x*rect.size.z];

				for (int i=0; i<count; i++) 
				{
					float val = arr[i];
					val -= rangeMin;
					val /= rangeMax-rangeMin;
					colors[i] = new Color(val, val, val);
				}
			
				texture.SetPixels(colors);
				texture.Apply();
				return texture;
			}

			[Obsolete]
			public static FloatMatrix SimpleFromTexture (Texture2D texture)
			{
				Color[] colors = texture.GetPixels();
				FloatMatrix matrix = new FloatMatrix(0,0, texture.width, texture.height);
				for (int i=0; i<colors.Length; i++)
					matrix.arr[i] = colors[i].r;
				return matrix;
			}


		#endregion


		#region Resize (obsolete)

			[Obsolete]
			public FloatMatrix ResizeOld (CoordRect newRect, FloatMatrix result=null)
			{
				if (result==null) result = new FloatMatrix(newRect);
				else result.ChangeRect(newRect);

				Coord min = result.rect.Min; Coord max = result.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float percentX = 1f*(x-result.rect.offset.x)/result.rect.size.x; float origX = percentX*this.rect.size.x + this.rect.offset.x; 
					float percentZ = 1f*(z-result.rect.offset.z)/result.rect.size.z; float origZ = percentZ*this.rect.size.z + this.rect.offset.z; 
					result[x,z] = this.GetInterpolated(origX, origZ);
				}

				return result;
			}

			
			public enum Interpolation { None, Linear, Bicubic } //TODO: Biquadratic
			
			[Obsolete]
			public float GetInterpolatedOld (float x, float z, CoordRect.TileMode wrap=CoordRect.TileMode.Clamp)
			{
				//skipping value if it is out of bounds
				if (wrap==CoordRect.TileMode.Clamp)
				{
					if (x<rect.offset.x || x>=rect.offset.x+rect.size.x || z<rect.offset.z || z>=rect.offset.z+rect.size.z) 
						return 0;
				}

				//neig coords
				int px = (int)x; if (x<0) px--; //because (int)-2.5 gives -2, should be -3 
				int nx = px+1;

				int pz = (int)z; if (z<0) pz--; 
				int nz = pz+1;

				//local coordinates (without offset)
				int lpx = px-rect.offset.x; int lnx = nx-rect.offset.x;
				int lpz = pz-rect.offset.z; int lnz = nz-rect.offset.z;

				//wrapping coordinates
				if (wrap==CoordRect.TileMode.Clamp)
				{
					if (lpx<0) lpx=0; if (lpx>=rect.size.x) lpx=rect.size.x-1;
					if (lnx<0) lnx=0; if (lnx>=rect.size.x) lnx=rect.size.x-1;
					if (lpz<0) lpz=0; if (lpz>=rect.size.z) lpz=rect.size.z-1;
					if (lnz<0) lnz=0; if (lnz>=rect.size.z) lnz=rect.size.z-1;
				}
				else if (wrap==CoordRect.TileMode.Tile)
				{
					lpx = lpx % rect.size.x; if (lpx<0) lpx=rect.size.x+lpx;
					lpz = lpz % rect.size.z; if (lpz<0) lpz=rect.size.z+lpz;
					lnx = lnx % rect.size.x; if (lnx<0) lnx=rect.size.x+lnx;
					lnz = lnz % rect.size.z; if (lnz<0) lnz=rect.size.z+lnz;
				}
				else if (wrap==CoordRect.TileMode.PingPong)
				{
					lpx = lpx % (rect.size.x*2); if (lpx<0) lpx=rect.size.x*2 + lpx; if (lpx>=rect.size.x) lpx = rect.size.x*2 - lpx - 1;
					lpz = lpz % (rect.size.z*2); if (lpz<0) lpz=rect.size.z*2 + lpz; if (lpz>=rect.size.z) lpz = rect.size.z*2 - lpz - 1;
					lnx = lnx % (rect.size.x*2); if (lnx<0) lnx=rect.size.x*2 + lnx; if (lnx>=rect.size.x) lnx = rect.size.x*2 - lnx - 1;
					lnz = lnz % (rect.size.z*2); if (lnz<0) lnz=rect.size.z*2 + lnz; if (lnz>=rect.size.z) lnz = rect.size.z*2 - lnz - 1;
				}

				//reading values
				float val_pxpz = arr[lpz*rect.size.x + lpx];
				float val_nxpz = arr[lpz*rect.size.x + lnx]; //array[pos_fxfz + 1]; //do not use fast calculations as they are not bounds safe
				float val_pxnz = arr[lnz*rect.size.x + lpx]; //array[pos_fxfz + rect.size.z];
				float val_nxnz = arr[lnz*rect.size.x + lnx]; //array[pos_fxfz + rect.size.z + 1];

				float percentX = x-px;
				float percentZ = z-pz;

				float val_fz = val_pxpz*(1-percentX) + val_nxpz*percentX;
				float val_cz = val_pxnz*(1-percentX) + val_nxnz*percentX;
				float val = val_fz*(1-percentZ) + val_cz*percentZ;

				return val;
			}


			[Obsolete]
			public float GetInterpolatedBicubicOld (float x, float z, CoordRect.TileMode wrap=CoordRect.TileMode.Clamp, float smoothness=0.33f)
			/// Sorry for the unfolded code, it's editor method that does not support inlining
			{
				//skipping value if it is out of bounds
				//if (wrap==CoordRect.TileMode.Once)
				//{
				//	if (x<rect.offset.x || x>=rect.offset.x+rect.size.x || z<rect.offset.z || z>=rect.offset.z+rect.size.z) 
				//		return 0;
				//}

				//neig coords
				int px = (int)x; if (x<0) px--; //because (int)-2.5 gives -2, should be -3 
				int nx = px+1;
				int ppx = px-1;
				int nnx = nx+1;

				int pz = (int)z; if (z<0) pz--; 
				int nz = pz+1;
				int ppz = pz-1;
				int nnz = nz+1;

				float percentX = x-px;
				float percentZ = z-pz;
				float invPercentX = 1-percentX;
				float invPercentZ = 1-percentZ;
				float sqPercentX = 3*percentX*percentX - 2*percentX*percentX*percentX;
				float sqPercentZ = 3*percentZ*percentZ - 2*percentZ*percentZ*percentZ;

				//local coordinates (without offset)
				px = px-rect.offset.x; nx = nx-rect.offset.x;
				pz = pz-rect.offset.z; nz = nz-rect.offset.z;
				ppx = ppx-rect.offset.x; nnx = nnx-rect.offset.x;
				ppz = ppz-rect.offset.z; nnz = nnz-rect.offset.z;

				//wrapping coordinates
				if (wrap==CoordRect.TileMode.Clamp)
				{
					if (px<0) px=0; if (px>=rect.size.x) px=rect.size.x-1;
					if (nx<0) nx=0; if (nx>=rect.size.x) nx=rect.size.x-1;
					if (pz<0) pz=0; if (pz>=rect.size.z) pz=rect.size.z-1;
					if (nz<0) nz=0; if (nz>=rect.size.z) nz=rect.size.z-1;
					
					if (ppx<0) ppx=0; if (ppx>=rect.size.x) ppx=rect.size.x-1;
					if (nnx<0) nnx=0; if (nnx>=rect.size.x) nnx=rect.size.x-1;
					if (ppz<0) ppz=0; if (ppz>=rect.size.z) ppz=rect.size.z-1;
					if (nnz<0) nnz=0; if (nnz>=rect.size.z) nnz=rect.size.z-1;
				}


				float p = arr[pz*rect.size.x + ppx];
				float pp = arr[ppz*rect.size.x + ppx];
				float n = arr[nz*rect.size.x + ppx];
				float nn = arr[nnz*rect.size.x + ppx];
				float plvz = (2*p - pp*percentZ + n*percentZ) * 0.5f;
				float nlvz = (2*n - nn*invPercentZ + p*invPercentZ  )*0.5f;
				float ppvz = plvz*(1-sqPercentZ) + nlvz*(sqPercentZ);

				p = arr[pz*rect.size.x + px];
				pp = arr[ppz*rect.size.x + px];
				n = arr[nz*rect.size.x + px];
				nn = arr[nnz*rect.size.x + px];
				plvz = (2*p - pp*percentZ + n*percentZ) * 0.5f;
				nlvz = (2*n - nn*invPercentZ + p*invPercentZ  )*0.5f;
				float pvz = plvz*(1-sqPercentZ) + nlvz*(sqPercentZ);

				p = arr[pz*rect.size.x + nx];
				pp = arr[ppz*rect.size.x + nx];
				n = arr[nz*rect.size.x + nx];
				nn = arr[nnz*rect.size.x + nx];
				plvz = (2*p - pp*percentZ + n*percentZ) * 0.5f;
				nlvz = (2*n - nn*invPercentZ + p*invPercentZ  )*0.5f;
				float nvz = plvz*(1-sqPercentZ) + nlvz*(sqPercentZ);

				p = arr[pz*rect.size.x + nnx];
				pp = arr[ppz*rect.size.x + nnx];
				n = arr[nz*rect.size.x + nnx];
				nn = arr[nnz*rect.size.x + nnx];
				plvz = (2*p - pp*percentZ + n*percentZ) * 0.5f;
				nlvz = (2*n - nn*invPercentZ + p*invPercentZ  )*0.5f;
				float nnvz = plvz*(1-sqPercentZ) + nlvz*(sqPercentZ);

				plvz = (2*pvz - ppvz*percentX + nvz*percentX) * 0.5f;
				nlvz = (2*nvz - nnvz*invPercentX + pvz*invPercentX  )*0.5f;
				return plvz*(1-sqPercentX) + nlvz*(sqPercentX);
			}

			//public float GetInterpolatedTiled (float x, float z)



		#endregion

		#region Blur (obsolete)

			[Obsolete]
			public void Spread (float strength=0.5f, int iterations=4, FloatMatrix copy=null)
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int j=0; j<count; j++) arr[j] = Mathf.Clamp(arr[j],-1,1);

				if (copy==null) copy = Copy(null);
				else for (int j=0; j<count; j++) copy.arr[j] = arr[j];

				for (int i=0; i<iterations; i++)
				{
					float prev = 0;

					for (int x=min.x; x<max.x; x++)
					{
						prev = this[x,min.z]; SetPos(x,min.z); for (int z=min.z+1; z<max.z; z++) { prev = (prev+arr[pos])/2; arr[pos] = prev; pos += rect.size.x; }
						prev = this[x,max.z-1]; SetPos(x,max.z-1); for (int z=max.z-2; z>=min.z; z--) { prev = (prev+arr[pos])/2; arr[pos] = prev; pos -= rect.size.x; }
					}

					for (int z=min.z; z<max.z; z++)
					{
						prev = this[min.x,z]; SetPos(min.x,z); for (int x=min.x+1; x<max.x; x++) { prev = (prev+arr[pos])/2; arr[pos] = prev; pos += 1; }
						prev = this[max.x-1,z]; SetPos(max.x-1,z); for (int x=max.x-2; x>=min.x; x--) { prev = (prev+arr[pos])/2; arr[pos] = prev; pos -= 1; }
					}
				}

				for (int j=0; j<count; j++) arr[j] = copy.arr[j] + arr[j]*2*strength;

				float factor = Mathf.Sqrt(iterations);
				for (int j=0; j<count; j++) arr[j] /= factor;
			}


			[Obsolete]
			public void Spread (System.Func<float,float,float> spreadFn=null, int iterations=4)
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int i=0; i<iterations; i++)
				{
					float prev = 0;

					for (int x=min.x; x<max.x; x++)
					{
						prev = this[x,min.z]; SetPos(x,min.z); for (int z=min.z+1; z<max.z; z++) { prev = spreadFn(prev,arr[pos]); arr[pos] = prev; pos += rect.size.x; }
						prev = this[x,max.z-1]; SetPos(x,max.z-1); for (int z=max.z-2; z>=min.z; z--) { prev = spreadFn(prev,arr[pos]); arr[pos] = prev; pos -= rect.size.x; }
					}

					for (int z=min.z; z<max.z; z++)
					{
						prev = this[min.x,z]; SetPos(min.x,z); for (int x=min.x+1; x<max.x; x++) { prev = spreadFn(prev,arr[pos]); arr[pos] = prev; pos += 1; }
						prev = this[max.x-1,z]; SetPos(max.x-1,z); for (int x=max.x-2; x>=min.x; x--) { prev = spreadFn(prev,arr[pos]); arr[pos] = prev; pos -= 1; }
					}
				}
			}

			[Obsolete]
			public void SimpleBlur (int iterations, float strength)
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int iteration=0; iteration<iterations; iteration++)
				{
					for (int z=min.z; z<max.z; z++)
					{
						float prev = this[min.x,z];
						for (int x=min.x+1; x<max.x-1; x++)
						{
							int i = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
							float curr = arr[i];
							float next = arr[i+1];

							float val = (prev+next)/2*strength + curr*(1-strength);
							arr[i] = val;
							prev = val;
						}
					}

					for (int x=min.x; x<max.x; x++)
					{
						float prev = this[x,min.z];
						for (int z=min.z+1; z<max.z-1; z++)
						{
							int i = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
							float curr = arr[i];
							float next = arr[i+rect.size.x];

							float val = (prev+next)/2*strength + curr*(1-strength);
							arr[i] = val;
							prev = val;
						}
					}
				}
			}

			[Obsolete]
			public void Blur (System.Func<float,float,float,float> blurFn=null, float intensity=0.666f, bool additive=false, bool takemax=false, bool horizontal=true, bool vertical=true, FloatMatrix reference=null)
			{
				if (reference==null) reference = this;
				Coord min = rect.Min; Coord max = rect.Max;

				if (horizontal)
				for (int z=min.z; z<max.z; z++)
				{
					int pos = (z-rect.offset.z)*rect.size.x + min.x - rect.offset.x;

					float prev = reference[min.x,z];
					float curr = prev;
					float next = prev;

					float blurred = 0;

					for (int x=min.x; x<max.x; x++) 
					{
						prev = curr; //reference[x-1,z];
						curr = next; //reference[x,z]; 
						if (x<max.x-1) next = reference.arr[pos+1]; //reference[x+1,z];

						//blurring
						if (blurFn==null) blurred = (prev+next)/2f;
						else blurred = blurFn(prev, curr, next);
						blurred = curr*(1-intensity) + blurred*intensity;
						
						//filling
						if (additive) arr[pos] += blurred;
						else arr[pos] = blurred;

						pos++;
					}
				}

				if (vertical)
				for (int x=min.x; x<max.x; x++)
				{
					int pos = (min.z-rect.offset.z)*rect.size.x + x - rect.offset.x;
				
					float next = reference[x,min.z];
					float curr = next;
					float prev = next;

					float blurred = next;

					for (int z=min.z; z<max.z; z++) 
					{
						prev = curr; //reference[x-1,z];
						curr = next; //reference[x,z]; 
						if (z<max.z-1) next = reference.arr[pos+rect.size.x]; //reference[x+1,z];

						//blurring
						if (blurFn==null) blurred = (prev+next)/2f;
						else blurred = blurFn(prev, curr, next);
						blurred = curr*(1-intensity) + blurred*intensity;
						
						//filling
						if (additive) arr[pos] += blurred;
						else if (takemax) { if (blurred > arr[pos]) arr[pos] = blurred; }
						else arr[pos] = blurred;

						pos+=rect.size.x;
					}
				}
			}

			[Obsolete]
			public void LossBlur (int step=2, bool horizontal=true, bool vertical=true, FloatMatrix reference=null)
			{
				if (reference==null) reference = this;
				Coord min = rect.Min; Coord max = rect.Max;
				int stepShift = step + step/2;

				if (horizontal)
				for (int z=min.z; z<max.z; z++)
				{
					SetPos(min.x, z);
				
					float sum = 0;
					int div = 0;
				
					float avg = this.arr[pos];
					float oldAvg = this.arr[pos];

					for (int x=min.x; x<max.x+stepShift; x++) 
					{
						//gathering
						if (x < max.x) sum += reference.arr[pos];
						div ++;
						if (x%step == 0) 
						{
							oldAvg=avg; 
							if (x < max.x) avg=sum/div; 
							sum=0; div=0;
						}

						//filling
						if (x-stepShift >= min.x)
						{
							float percent = 1f*(x%step)/step;
							if (percent<0) percent += 1; //for negative x
							this.arr[pos-stepShift] = avg*percent + oldAvg*(1-percent);
						}

						pos += 1;
					}
				}

				if (vertical)
				for (int x=min.x; x<max.x; x++)
				{
					SetPos(x, min.z);
				
					float sum = 0;
					int div = 0;
				
					float avg = this.arr[pos];
					float oldAvg = this.arr[pos];

					for (int z=min.z; z<max.z+stepShift; z++) 
					{
						//gathering
						if (z < max.z) sum += reference.arr[pos];
						div ++;
						if (z%step == 0) 
						{
							oldAvg=avg; 
							if (z < max.z) avg=sum/div; 
							sum=0; div=0;
						}

						//filling
						if (z-stepShift >= min.z)
						{
							float percent = 1f*(z%step)/step;
							if (percent<0) percent += 1;
							this.arr[pos-stepShift*rect.size.x] = avg*percent + oldAvg*(1-percent);
						}

						pos += rect.size.x;
					}
				}
			}

			#region Outdated
		/*public void OverBlur (int iterations=20)
		{
			Matrix blurred = this.Clone(null);

			for (int i=1; i<=iterations; i++)
			{
				if (i==1 || i==2) blurred.Blur(step:1);
				else if (i==3) { blurred.Blur(step:1); blurred.Blur(step:1); }
				else blurred.Blur(step:i-2); //i:4, step:2

				for (int p=0; p<count; p++) 
				{
					float b = blurred.array[p] * i;
					float a = array[p];

					array[p] = a + b + a*b;
				}
			}
		}*/

		/*public void LossBlur (System.Func<float,float,float,float> blurFn=null, //prev, curr, next = output
			float intensity=0.666f, int step=1, Matrix reference=null, bool horizontal=true, bool vertical=true)
		{
			Coord min = rect.Min; Coord max = rect.Max;

			if (reference==null) reference = this;
			int lastX = max.x-1;
			int lastZ = max.z-1;

			if (horizontal)
			for (int z=min.z; z<=lastZ; z++)
			{
				float next = reference[min.x,z];
				float curr = next;
				float prev = next;

				float blurred = next;
				float lastBlurred = next;

				for (int x=min.x+step; x<=lastX; x+=step) 
				{
					//blurring
					if (blurFn==null) blurred = (prev+next)/2f;
					else blurred = blurFn(prev, curr, next);
					blurred = curr*(1-intensity) + blurred*intensity;

					//shifting values
					prev = curr; //this[x,z];
					curr = next; //this[x+step,z];
					try { next = reference[x+step*2,z]; } //this[x+step*2,z];
					catch { next = reference[lastX,z]; }

					//filling between-steps distance
					if (step==1) this[x,z] = blurred;
					else for (int i=0; i<step; i++) 
					{
						float percent = 1f * i / step;
						this[x-step+i,z] = blurred*percent + lastBlurred*(1-percent);
					}
					lastBlurred = blurred;
				}
			}

			if (vertical)
			for (int x=min.x; x<=lastX; x++)
			{
				float next = reference[x,min.z];
				float curr = next;
				float prev = next;

				float blurred = next;
				float lastBlurred = next;

				for (int z=min.z+step; z<=lastZ; z+=step) 
				{
					//blurring
					if (blurFn==null) blurred = (prev+next)/2f;
					else blurred = blurFn(prev, curr, next);
					blurred = curr*(1-intensity) + blurred*intensity;

					//shifting values
					prev = curr;
					curr = next;
					try { next = reference[x,z+step*2]; }
					catch { next = reference[x,lastZ]; }

					//filling between-steps distance
					if (step==1) this[x,z] = blurred;
					else for (int i=0; i<step; i++) 
					{
						float percent = 1f * i / step;
						this[x,z-step+i] = blurred*percent + lastBlurred*(1-percent);
					}
					lastBlurred = blurred;
				}
			}
		}*/
		#endregion

		#endregion


		#region Other

			static public void BlendLayers (FloatMatrix[] matrices, float[] opacity=null) 
			/// Changes splatmaps in photoshop layered style so their summary value does not exceed 1
			{
				//finding any existing matrix
				int anyMatrixNum = -1;
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null) { anyMatrixNum = i; break; }
				if (anyMatrixNum == -1) { Debug.LogError("No matrices were found to blend " + matrices.Length); return; }

				//finding rect
				CoordRect rect = matrices[anyMatrixNum].rect;

				//checking rect size
				#if WDEBUG
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null && matrices[i].rect!=rect) { Debug.LogError("Matrix rect mismatch " + rect + " " + matrices[i].rect); return; }
				#endif

				int rectCount = rect.Count;
				for (int pos=0; pos<rectCount; pos++)
				{
					float left = 1;
					for (int i=matrices.Length-1; i>=0; i--) //layer 0 is background, layer Length-1 is the top one
					{
						if (matrices[i] == null) continue;
						
						float val = matrices[i].arr[pos];

						if (opacity != null) val *= opacity[i];

						val = val * left;
						matrices[i].arr[pos] = val;
						left -= val;

						if (left < 0) break;

						
						/*float overly = sum + val - 1; 
						if (overly < 0) overly = 0; //faster then calling Math.Clamp
						if (overly > 1) overly = 1;

						matrices[i].array[pos] = val - overly;
						sum += val - overly;*/
					}
				}
			}

			static public void NormalizeLayers (FloatMatrix[] matrices, float[] opacity) 
			/// Changes splatmaps so their summary value does not exceed 1
			{
				//finding any existing matrix
				int anyMatrixNum = -1;
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null) { anyMatrixNum = i; break; }
				if (anyMatrixNum == -1) { Debug.LogError("No matrices were found to blend " + matrices.Length); return; }

				//finding rect
				CoordRect rect = matrices[anyMatrixNum].rect;

				//checking rect size
				#if WDEBUG
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null && matrices[i].rect!=rect) { Debug.LogError("Matrix rect mismatch " + rect + " " + matrices[i].rect); return; }
				#endif


				int rectCount = rect.Count;
				for (int pos=0; pos<rectCount; pos++)
				{
					for (int i=0; i<matrices.Length; i++) matrices[i].arr[pos] *= opacity[i];

					float sum = 0;
					for (int i=0; i<matrices.Length; i++) sum += matrices[i].arr[pos];
					if (sum > 1f) for (int i=0; i<matrices.Length; i++) matrices[i].arr[pos] /= sum;
				}
			}


			static public void Blend (FloatMatrix src, FloatMatrix dst, float factor)
			{
				if (dst.rect != src.rect) Debug.LogError("Matrix Blend: maps have different sizes");
				
				for (int i=0; i<dst.count; i++)
				{
					dst.arr[i] = dst.arr[i]*factor + src.arr[i]*(1-factor);
				}
			}

			static public void Mask (FloatMatrix src, FloatMatrix dst, FloatMatrix mask) //changes dst, not src
			{
				if (src != null &&
					(dst.rect != src.rect || dst.rect != mask.rect)) Debug.LogError("Matrix Mask: maps have different sizes");
				
				for (int i=0; i<dst.count; i++)
				{
					float percent = mask.arr[i];
					if (percent > 1 || percent < 0) continue;

					dst.arr[i] = dst.arr[i]*percent + (src==null? 0:src.arr[i]*(1-percent));
				}
			}

			static public void SafeBorders (FloatMatrix src, FloatMatrix dst, int safeBorders) //changes dst, not src
			{
				Coord min = dst.rect.Min; Coord max = dst.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					int distFromBorder = Mathf.Min( Mathf.Min(x-min.x,max.x-x), Mathf.Min(z-min.z,max.z-z) );
					float percent = 1f*distFromBorder / safeBorders;
					if (percent > 1) continue;

					dst[x,z] = dst[x,z]*percent + (src==null? 0:src[x,z]*(1-percent));
				}
			}

		#endregion


		#region Histogram

			public float[] Histogram (int resolution, float max=1, bool normalize=true)
			/// Evaluates all pixels in matrix and returns an array of pixels count by their value. 
			{
				float[] quants = new float[resolution];

				for (int i=0; i<count; i++)
				{
					float val = arr[i];
					float percent = val / max;
					int num = (int)(percent*resolution);
					if (num==resolution) num--; //this could happen when val==max

					quants[num]++;
				}

				if (normalize)
				{
					float maxQuant = 0;

					for (int i=0; i<resolution; i++)
						if (quants[i] > maxQuant) maxQuant = quants[i];

					for (int i=0; i<resolution; i++)
						quants[i] /= maxQuant;
				}

				return quants;
			}

			
			public float[] Slice (Coord coord, int length, bool vertical)
			/// A single line (or row) of a matrix to show a vertical slice
			{
				if (vertical) return SliceVertical(coord, length);
				else return SliceHorizontal(coord, length);
			}


			public float[] SliceHorizontal (Coord coord, int length)
			{
				if (coord.z < rect.offset.z || coord.z >= rect.offset.z+rect.size.z) return new float[0];

				if (coord.x < rect.offset.x) { length -= rect.offset.x-coord.x; coord.x = rect.offset.x; }
				if (length <= 0) return new float[0];
				int max = coord.x + length;
				if (max >= rect.offset.x+rect.size.x) length = rect.offset.x+rect.size.x - coord.x;

				MatrixLine line = new MatrixLine(coord.x-rect.offset.x, length);
				line.ReadLine(this, coord.z);
				return line.arr;
			}


			public float[] SliceVertical (Coord coord, int length)
			{
				if (coord.x < rect.offset.x || coord.x >= rect.offset.x+rect.size.x) return new float[0];

				if (coord.z < rect.offset.z) { length -= rect.offset.z-coord.z; coord.x = rect.offset.z; }
				if (length <= 0) return new float[0];
				int max = coord.z + length;
				if (max >= rect.offset.z+rect.size.z) length = rect.offset.z+rect.size.z - coord.z;

				MatrixLine row = new MatrixLine(coord.z-rect.offset.z, length);
				row.ReadLine(this, coord.x);
				return row.arr;
			}


			public static byte[] HistogramToTextureBytes (float[] quants, int height, byte empty=0, byte top=255, byte filled=128)
			/// Converts an array from Histogram to texture bytes
			{
				int width = quants.Length;
				byte[] bytes = new byte[width * height];

				for (int x=0; x<width; x++)
				{
					int max = (int)(quants[x] * height);
					if (max==height) max--;

					for (int z=0; z<height; z++)
					{
						byte val = empty;
						if (z==max) val=top;
						else if (z<max) val=filled;

						bytes[z*width + x] = val; 
					}
				}

				return bytes;
			}

			public static Texture2D HistogramToTextureR8 (float[] quants, int height, byte empty=0, byte top=255, byte filled=128)
			/// Converts an array from Histogram to texture (format R8)
			{
				byte[] bytes = HistogramToTextureBytes(quants, height, empty, top, filled);
				
				Texture2D tex = new Texture2D(quants.Length, height, TextureFormat.R8, false, linear:true);
				tex.LoadRawTextureData(bytes);
				tex.Apply(updateMipmaps:false);

				return tex;
			}

			[Obsolete("Use static method instead")] public byte[] HistogramTexture (int width, int height, byte empty=0, byte top=255, byte filled=128)
			{
				float[] quants = Histogram(width);
				byte[] bytes = HistogramToTextureBytes(quants, height, empty, top, filled);
				return bytes;
			}

		#endregion


		#region Import

			public void ImportTexture (Texture2D tex, int channel, bool useRaw=true) { ImportTexture(tex, rect.offset, channel, useRaw); }
			public void ImportTexture (Texture2D tex, Coord texOffset, int channel, bool useRaw=true)
			{
				Coord texSize = new Coord(tex.width, tex.height);

				//raw bytes
				TextureFormat format = tex.format;
				if (useRaw && (format==TextureFormat.RGBA32 || format==TextureFormat.ARGB32 || format==TextureFormat.RGB24 || format==TextureFormat.R8 || format==TextureFormat.R16))
				{
					byte[] bytes = tex.GetRawTextureData();
					switch(format)
					{
						case TextureFormat.RGBA32: ImportRaw(bytes, texOffset, texSize, channel, 4); break;
						case TextureFormat.ARGB32: channel++; if (channel == 5) channel = 0; ImportRaw(bytes, texOffset, texSize, channel, 4); break;
						case TextureFormat.RGB24: ImportRaw(bytes, texOffset, texSize, channel, 3); break;
						case TextureFormat.R8: ImportRaw(bytes, texOffset, texSize, 0, 1); break;
						case TextureFormat.R16: ImportRaw16(bytes, texOffset, texSize); break;
					}
				}

				//colors
				else
				{
					CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(texOffset,texSize)); //to get array smaller than the whole texture

					Color[] colors = tex.GetPixels(intersection.offset.x-texOffset.x, intersection.offset.z-texOffset.z, intersection.size.x, intersection.size.z);
					ImportColors(colors, intersection.offset, intersection.size, channel);
				}

				tex.Apply();
			}


			public void ImportColors (Color[] colors, int width, int height, int channel) { ImportColors(colors, rect.offset, new Coord(width,height), channel); }
			public void ImportColors (Color[] colors, Coord colorsSize, int channel) { ImportColors(colors, rect.offset, colorsSize, channel); }
			public void ImportColors (Color[] colors, Coord colorsOffset, Coord colorsSize,  int channel)
			{
				if (colors.Length != colorsSize.x*colorsSize.z)
					throw new Exception("Array count does not match texture dimensions");

				CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(colorsOffset, colorsSize));
				Coord min = intersection.Min; Coord max = intersection.Max;
				
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int colorsPos = (z-colorsOffset.z)*colorsSize.x + x - colorsOffset.x;

						float val;
						switch (channel)
						{
							case 0: val = colors[colorsPos].r; break;
							case 1: val = colors[colorsPos].g; break;
							case 2: val = colors[colorsPos].b; break;
							case 3: val = colors[colorsPos].a; break;
							default: val = colors[colorsPos].r; break; //(colors[colorsPos].r + colors[colorsPos].g + colors[colorsPos].b + colors[colorsPos].a); break;
						}

						arr[matrixPos] = val;
					}
			}


			public void ImportRaw (byte[] bytes, int width, int height, int start, int step) { ImportRaw(bytes, new Coord(width,height), rect.offset, start, step); }
			public void ImportRaw (byte[] bytes, Coord bytesSize, int start, int step) { ImportRaw(bytes, bytesSize, rect.offset, start, step); }
			public void ImportRaw (byte[] bytes, Coord bytesOffset, Coord bytesSize, int start, int step)
			{
				if (bytes.Length != bytesSize.x*bytesSize.z*step &&  
					(bytes.Length < bytesSize.x*bytesSize.z*step*1.3f || bytes.Length > bytesSize.x*bytesSize.z*step*1.3666f)) //in case of mipmap information
						throw new Exception("Array count does not match texture dimensions");

				CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(bytesOffset, bytesSize));
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int bytesPos = (z-bytesOffset.z)*bytesSize.x + x - bytesOffset.x;
						bytesPos = bytesPos * step + start;

						float val = bytes[bytesPos] / 255f;  //matrix has the range 0-1 _inclusive_, it could be 1, so using 255
						arr[matrixPos] = val;
					}
			}


			public void ImportRaw16 (byte[] bytes, int width, int height) { ImportRaw16(bytes, new Coord(width,height), rect.offset); }
			public void ImportRaw16 (byte[] bytes, Coord texSize) { ImportRaw16(bytes, texSize, rect.offset); }
			public void ImportRaw16 (byte[] bytes, Coord texOffset, Coord texSize)
			{
				if (texSize.x*texSize.z*2 != bytes.Length)
					throw new Exception("Array count does not match texture dimensions");

				CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(texOffset, texSize));
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int bytesPos = (z-texOffset.z)*texSize.x + x - texOffset.x;
						bytesPos *= 2; 

						float val = (bytes[bytesPos+1]*255f + bytes[bytesPos]) / 65025f;
						arr[matrixPos] = val;
					}
			}


			public void ImportHeights (float[,] heights) { ImportHeights(heights, rect.offset); }
			public void ImportHeights (float[,] heights, Coord heightsOffset)
			{
				Coord heightsSize = new Coord(heights.GetLength(1), heights.GetLength(0));  //x and z swapped
				CoordRect heightsRect = new CoordRect(heightsOffset, heightsSize);
				
				CoordRect intersection = CoordRect.Intersected(rect, heightsRect);
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int heightsPosZ = x - heightsRect.offset.x;
						int heightsPosX = z - heightsRect.offset.z;

						arr[matrixPos] = heights[heightsPosX, heightsPosZ];
					}
			}


			public void ImportSplats (float[,,] splats, int channel) { ImportSplats(splats, rect.offset, channel); }
			public void ImportSplats (float[,,] splats, Coord splatsOffset, int channel)
			{
				Coord splatsSize = new Coord(splats.GetLength(1), splats.GetLength(0));  //x and z swapped
				CoordRect splatsRect = new CoordRect(splatsOffset, splatsSize);
				
				CoordRect intersection = CoordRect.Intersected(rect, splatsRect);
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int heightsPosZ = x - splatsRect.offset.x;
						int heightsPosX = z - splatsRect.offset.z;

						arr[matrixPos] = splats[heightsPosX, heightsPosZ, channel];
					}
			}


			public void ImportData (TerrainData data, int channel=-1) { ImportData (data, rect.offset, channel=-1); }
			public void ImportData (TerrainData data, Coord dataOffset, int channel=-1)
			/// Partial terrain data (loading only the part intersecting with matrix). Do not work in thread!
			/// If channel is -1 getting height
			{
				int resolution = channel==-1 ?  data.heightmapResolution : data.alphamapResolution;

				Coord dataSize = new Coord(resolution, resolution);
				CoordRect dataIntersection = CoordRect.Intersected(rect, new CoordRect(dataOffset, dataSize));
				if (dataIntersection.size.x==0 || dataIntersection.size.z==0) return;

				if (channel == -1)
				{
					float[,] heights = data.GetHeights(dataIntersection.offset.x-dataOffset.x, dataIntersection.offset.z-dataOffset.z, dataIntersection.size.x, dataIntersection.size.z);
					ImportHeights(heights, dataIntersection.offset);
				}

				else
				{
					float[,,] splats = data.GetAlphamaps(dataIntersection.offset.x-dataOffset.x, dataIntersection.offset.z-dataOffset.z, dataIntersection.size.x, dataIntersection.size.z);
					ImportSplats(splats, dataIntersection.offset, channel);
				}
			}

		#endregion


		#region Export

			public void ExportTexture (Texture2D tex, int channel, bool useRaw=true) { ExportTexture(tex, rect.offset, channel, useRaw); }
			public void ExportTexture (Texture2D tex, Coord texOffset, int channel, bool useRaw=true)
			{
				Coord texSize = new Coord(tex.width, tex.height);

				//raw bytes
				TextureFormat format = tex.format;
				if (useRaw && (format==TextureFormat.RGBA32 || format==TextureFormat.ARGB32 || format==TextureFormat.RGB24 || format==TextureFormat.R8 || format==TextureFormat.R16))
				{
					byte[] bytes = tex.GetRawTextureData();
					switch(format)
					{
						case TextureFormat.RGBA32: 
							//bytes = new byte[texSize.x*texSize.z*4]; 
							ExportRaw(bytes, texOffset, texSize, channel, 4); break;

						case TextureFormat.ARGB32: 
							//bytes = new byte[texSize.x*texSize.z*4]; 
							channel++; if (channel == 5) channel = 0; 
							ExportRaw(bytes, texOffset, texSize, channel, 4); break;

						case TextureFormat.RGB24: 
							//bytes = new byte[texSize.x*texSize.z*3]; 
							ExportRaw(bytes, texOffset, texSize, channel, 3); break;

						case TextureFormat.R8: 
							//bytes = new byte[texSize.x*texSize.z]; 
							ExportRaw(bytes, texOffset, texSize, 0, 1); break;

						case TextureFormat.R16: 
							//bytes = new byte[texSize.x*texSize.z*2]; 
							ExportRaw16(bytes, texOffset, texSize); break;
					}

					tex.LoadRawTextureData(bytes);
				}

				//colors
				else
				{
					CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(texOffset,texSize)); //to get array smaller than the whole texture

					//Color[] colors = tex.GetPixels(intersection.offset.x-texOffset.x, intersection.offset.z-texOffset.z, intersection.size.x, intersection.size.z);
					Color[] colors = new Color[intersection.size.x * intersection.size.z];
					ExportColors(colors, intersection.offset, intersection.size, channel);
					tex.SetPixels(intersection.offset.x-texOffset.x, intersection.offset.z-texOffset.z, intersection.size.x, intersection.size.z, colors);
				}

				tex.Apply();
			}


			public void ExportColors (Color[] colors, int width, int height, int channel) { ExportColors(colors, rect.offset, new Coord(width,height), channel); }
			public void ExportColors (Color[] colors, Coord colorsSize, int channel) { ExportColors(colors, rect.offset, colorsSize, channel); }
			public void ExportColors (Color[] colors, Coord colorsOffset, Coord colorsSize,  int channel)
			{
				if (colors.Length != colorsSize.x*colorsSize.z)
					throw new Exception("Array count does not match texture dimensions");

				CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(colorsOffset, colorsSize));
				Coord min = intersection.Min; Coord max = intersection.Max;
				
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int colorsPos = (z-colorsOffset.z)*colorsSize.x + x - colorsOffset.x;

						float val = arr[matrixPos];
						switch (channel)
						{
							case 0: colors[colorsPos].r = val; break;
							case 1: colors[colorsPos].g = val; break;
							case 2: colors[colorsPos].b = val; break;
							case 3: colors[colorsPos].a = val; break;
							default: colors[colorsPos].r = val; break; //(colors[colorsPos].r + colors[colorsPos].g + colors[colorsPos].b + colors[colorsPos].a); break;
						}
					}
			}


			public void ExportRaw (byte[] bytes, int width, int height, int start, int step) { ExportRaw(bytes, new Coord(width,height), rect.offset, start, step); }
			public void ExportRaw (byte[] bytes, Coord bytesSize, int start, int step) { ExportRaw(bytes, bytesSize, rect.offset, start, step); }
			public void ExportRaw (byte[] bytes, Coord bytesOffset, Coord bytesSize, int start, int step)
			{
				if (bytes.Length != bytesSize.x*bytesSize.z*step &&  
					(bytes.Length < bytesSize.x*bytesSize.z*step*1.3f || bytes.Length > bytesSize.x*bytesSize.z*step*1.3666f)) //in case of mipmap information
						throw new Exception("Array count does not match texture dimensions");

				CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(bytesOffset, bytesSize));
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int bytesPos = (z-bytesOffset.z)*bytesSize.x + x - bytesOffset.x;
						bytesPos = bytesPos * step + start;

						float val = arr[matrixPos];
						bytes[bytesPos] = (byte)(val * 255f); //matrix has the range 0-1 _inclusive_, it could be 1
					}
			}


			public void ExportRaw16 (byte[] bytes, int width, int height) { ExportRaw16(bytes, new Coord(width,height), rect.offset); }
			public void ExportRaw16 (byte[] bytes, Coord texSize) { ExportRaw16(bytes, texSize, rect.offset); }
			public void ExportRaw16 (byte[] bytes, Coord texOffset, Coord texSize)
			{
				if (texSize.x*texSize.z*2 != bytes.Length)
					throw new Exception("Array count does not match texture dimensions");

				CoordRect intersection = CoordRect.Intersected(rect, new CoordRect(texOffset, texSize));
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int bytesPos = (z-texOffset.z)*texSize.x + x - texOffset.x;
						bytesPos *= 2; 

						float val = arr[matrixPos]; //this[x+regionRect.offset.x, z+regionRect.offset.z];

						int intVal = (int)(val*65025);
						byte hb = (byte)(intVal/255f);
						bytes[bytesPos+1] = hb;  //TODO: test if the same will work on macs with non-inverted byte order
						bytes[bytesPos] = (byte)(intVal-hb*255);
					}
			}


			public void ExportHeights (float[,] heights) { ExportHeights(heights, rect.offset); }
			public void ExportHeights (float[,] heights, Coord heightsOffset)
			{
				Coord heightsSize = new Coord(heights.GetLength(1), heights.GetLength(0));  //x and z swapped
				CoordRect heightsRect = new CoordRect(heightsOffset, heightsSize);
				
				CoordRect intersection = CoordRect.Intersected(rect, heightsRect);
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int heightsPosZ = x - heightsRect.offset.x;
						int heightsPosX = z - heightsRect.offset.z;

						float val = arr[matrixPos];
						heights[heightsPosX, heightsPosZ] = val;
					}
			}


			public void ExportSplats (float[,,] splats, int channel) { ExportSplats(splats, rect.offset, channel); }
			public void ExportSplats (float[,,] splats, Coord splatsOffset, int channel)
			{
				Coord splatsSize = new Coord(splats.GetLength(1), splats.GetLength(0));  //x and z swapped
				CoordRect splatsRect = new CoordRect(splatsOffset, splatsSize);
				
				CoordRect intersection = CoordRect.Intersected(rect, splatsRect);
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int matrixPos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int heightsPosZ = x - splatsRect.offset.x;
						int heightsPosX = z - splatsRect.offset.z;

						float val = arr[matrixPos];
						splats[heightsPosX, heightsPosZ, channel] = val;
					}
			}


			//partial terrain data (loading only the part intersecting with matrix). Do not work in thread!
			public void ExportData (TerrainData data) { ExportData (data, rect.offset, -1); }
			public void ExportData (TerrainData data, int channel) { ExportData (data, rect.offset, channel); }
			public void ExportData (TerrainData data, Coord dataOffset, int channel) //if channel is -1 getting height
			{
				int resolution = channel==-1 ?  data.heightmapResolution : data.alphamapResolution;

				Coord dataSize = new Coord(resolution, resolution);
				CoordRect dataIntersection = CoordRect.Intersected(rect, new CoordRect(dataOffset, dataSize));
				if (dataIntersection.size.x==0 || dataIntersection.size.z==0) return;

				if (channel == -1)
				{
					float[,] heights = new float[dataIntersection.size.z, dataIntersection.size.x];  //x and z swapped
					ExportHeights(heights, dataIntersection.offset);
					data.SetHeights(dataIntersection.offset.x-dataOffset.x, dataIntersection.offset.z-dataOffset.z, heights);  //while get/set has the right order
				}

				else
				{
					float[,,] splats = data.GetAlphamaps(dataIntersection.offset.x-dataOffset.x, dataIntersection.offset.z-dataOffset.z, dataIntersection.size.x, dataIntersection.size.z);
					ExportSplats(splats, dataIntersection.offset, channel);
					data.SetAlphamaps(dataIntersection.offset.x-dataOffset.x, dataIntersection.offset.z-dataOffset.z, splats);
				}
			}

		#endregion


		#region Stamp

			public void Stamp (float centerX, float centerZ, float radius, float hardness, FloatMatrix stamp)
			/// Applies stamp to this matrix, blending it in a smooth circular way using radius and hardness
			/// All values are in pixels and using matrix offset
			/// Center does not need to be the real center, it's just used to calculate fallof
			/// Hardness is the percent (0-1) of the stamp that has 100% fallof
			/// Invert fills all matrix except the center area
			/// Tested
			{
				CoordRect intersection = CoordRect.Intersected(rect, stamp.rect);
				Coord min = intersection.Min; Coord max = intersection.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						float dist = Mathf.Sqrt((x-centerX)*(x-centerX) + (z-centerZ)*(z-centerZ));
						if (dist > radius) continue;

						int pos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						int stampPos = (z-stamp.rect.offset.z)*stamp.rect.size.x + x - stamp.rect.offset.x;

						if (dist < radius*hardness) { arr[pos] = stamp.arr[stampPos]; continue; }

						float fallof = (radius - dist) / (radius - radius*hardness); //linear yet
						if (fallof>1) fallof = 1; if (fallof<0) fallof = 0;
						fallof = 3*fallof*fallof - 2*fallof*fallof*fallof;

						arr[pos] = arr[pos]*(1-fallof) + stamp.arr[stampPos]*fallof;
					}
			}

			public void Stamp (float centerX, float centerZ, float radius, float hardness, float value)
			/// Applies value stamp to this matrix. Same as above, but using uniform value
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						float dist = Mathf.Sqrt((x-centerX)*(x-centerX) + (z-centerZ)*(z-centerZ));
						if (dist > radius) continue;

						int pos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;

						if (dist < radius*hardness) { arr[pos] = value; continue; }

						float fallof = (radius - dist) / (radius - radius*hardness); //linear
						if (fallof>1) fallof = 1; if (fallof<0) fallof = 0;
						fallof = 3*fallof*fallof - 2*fallof*fallof*fallof;

						arr[pos] = arr[pos]*(1-fallof) + value*fallof;
					}
			}

			public void StampInverted (float centerX, float centerZ, float radius, float hardness, float value)
			/// Applies value to all of the matrix except the area in the center. Changes from the original version by it's "continiue" optimizations
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						int pos = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
						
						float dist = Mathf.Sqrt((x-centerX)*(x-centerX) + (z-centerZ)*(z-centerZ));
						if (dist > radius) { arr[pos] = value; continue; }

						if (dist < radius*hardness) continue; //differs from the original one

						float fallof = (radius - dist) / (radius - radius*hardness); //linear
						if (fallof>1) fallof = 1; if (fallof<0) fallof = 0;
						fallof = 3*fallof*fallof - 2*fallof*fallof*fallof;

						arr[pos] = arr[pos]*fallof + value*(1-fallof); //differs here too
					}
			}

		#endregion


		#region Arithmetic (per-pixel operation that does not involve neighbor pixels)

			public void Mix (FloatMatrix m, float opacity=1) 
			{ 
				float invOpacity = 1-opacity;
				for (int i=0; i<count; i++) 
					arr[i] = m.arr[i]*opacity + arr[i]*invOpacity; 
			}

			public void Add (FloatMatrix add, float opacity=1) 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] += add.arr[i] * opacity; 
			}

			public void Add (FloatMatrix add, FloatMatrix mask, float opcaity=1) 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] += add.arr[i] * mask.arr[i] * opcaity; 
			}

			public void Add (float add) 
			{
				for (int i=0; i<count; i++) 
					arr[i] += add; 
			}

			public void Subtract (FloatMatrix m, float opacity=1) 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] -= m.arr[i] * opacity; 
			}

			public void InvSubtract (FloatMatrix m, float opacity=1) 
			/// subtracting this matrix from m
			{ 
				for (int i=0; i<count; i++) 
					arr[i] = m.arr[i]*opacity - arr[i]; 
			}

			public void Multiply (FloatMatrix m, float opacity=1) 
			{
				float invOpacity = 1-opacity;
				for (int i=0; i<count; i++) 
					arr[i] *= m.arr[i]*opacity + invOpacity; 
			}

			public void Multiply (float m) 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] *= m; 
			}

			public void Sqrt () 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] = Mathf.Sqrt(arr[i]); 
			}

			public void Fallof () 
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = arr[i];
					arr[i] = 3*val*val - 2*val*val*val;
				}
			}

			public void Contrast (float m)
			/// Mid-matrix (value 0.5) multiply
			{
				for (int i=0; i<count; i++) 
				{
					float val = arr[i]*2 -1;
					val *= m;
					arr[i] = (val+1) / 2;
				}
			}

			public void Divide (FloatMatrix m, float opacity=1) 
			{ 
				float invOpacity = 1-opacity;
				for (int i=0; i<count; i++) 
					arr[i] *= opacity/m.arr[i] + invOpacity; 
			}

			public void Difference (FloatMatrix m, float opacity=1) 
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = arr[i] - m.arr[i]*opacity;
					if (val < 0) val = -val;
					arr[i] = val;
				}
			}

			public void Overlay (FloatMatrix m, float opacity=1)
			{
				for (int i=0; i<count; i++) 
				{
					float a = arr[i];
					float b = m.arr[i];

					b = b*opacity + (0.5f - opacity/2); //enhancing contrast via levels

					if (a > 0.5f) b = 1 - 2*(1-a)*(1-b);
					else b = 2*a*b;
					
					arr[i] = b;// b*opacity + a*(1-opacity); //the same
				}
			}

			public void HardLight (FloatMatrix m, float opacity=1)
			/// Same as overlay but estimating b>0.5
			{
				for (int i=0; i<count; i++) 
				{
					float a = arr[i];
					float b = m.arr[i];

					if (b > 0.5f) b = 1 - 2*(1-a)*(1-b);
					else b = 2*a*b; 

					arr[i] = b*opacity + a*(1-opacity);
				}
			}

			public void SoftLight (FloatMatrix m, float opacity=1)
			{
				for (int i=0; i<count; i++) 
				{
					float a = arr[i];
					float b = m.arr[i];
					b = (1-2*b)*a*a + 2*b*a;
					arr[i] = b*opacity + a*(1-opacity);
				}
			}

			public void Max (FloatMatrix m, float opacity=1) 
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = m.arr[i]>arr[i] ? m.arr[i] : arr[i];
					arr[i] = val*opacity + arr[i]*(1-opacity);
				}
			}

			public void Min (FloatMatrix m, float opacity=1) 
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = m.arr[i]<arr[i] ? m.arr[i] : arr[i];
					arr[i] = val*opacity + arr[i]*(1-opacity);
				}
			}

			public new void Fill(float val) 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] = val; 
			}

			public void Fill (FloatMatrix m)
			{
				m.arr.CopyTo(arr,0);
			}

			public void Invert() 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] = -arr[i]; 
			}

			public void InvertOne() 
			{ 
				for (int i=0; i<count; i++) 
					arr[i] = 1-arr[i]; 
			}

			public void SelectRange (float min0, float min1, float max0, float max1)
			/// Fill all values within min1-max0 with 1, while min0-1 and max0-1 are filled with blended
			{
				for (int i=0; i<arr.Length; i++)
				{
					float delta = arr[i];
				
					//if (steepness.x<0.0001f) array[i] = 1-(delta-max0)/(max1-max0);
					//else
					{
						float minVal = (delta-min0)/(min1-min0);
						float maxVal = 1-(delta-max0)/(max1-max0);
						float val = minVal>maxVal? maxVal : minVal;
						if (val<0) val=0; if (val>1) val=1;

						arr[i] = val;
					}
				}
			}

			public void Clamp01 ()
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = arr[i];
					if (val > 1) arr[i] = 1;
					else if (val < 0) arr[i] = 0;
				}
			}

			public void ToRange01 ()
			{ 
				for (int i=0; i<count; i++) 
					arr[i] = (arr[i]+1) / 2;
			}

			public float MaxValue () 
			{ 
				float max=-20000000; 
				for (int i=0; i<count; i++) 
				{
					float val = arr[i];
					if (val > max) max = val;
				}
				return max; 
			}

			public float MinValue () 
			{ 
				float min=20000000; 
				for (int i=0; i<count; i++) 
				{
					float val = arr[i];
					if (val < min) min = val;
				}
				return min; 
			}

			public virtual bool IsEmpty () 
			/// Better than MinValue since it can quit if matrix is not empty
			{ 
				for (int i=0; i<count; i++) 
					if (arr[i] > 0.0001f) return false; 
				return true; 
			}

			public virtual bool IsEmpty (float delta) 
			{ 
				for (int i=0; i<count; i++) 
					if (arr[i] > delta) return false; 
				return true; 
			}

			public void BlackWhite (float mid)
			/// Sets all values bigger than mid to white (1), and those lower to black (0)
			{
				for (int i=0; i<count; i++) 
				{
					float val = arr[i];
					if (val > mid) arr[i] = 1;
					else arr[i] = 0;
				}
			}

			public void Terrace (float[] terraces, float steepness, float intensity)
			{
				for (int i=0; i<count; i++)
				{
					float val = arr[i];
					if (val > 0.999f) continue;	//do nothing with values that are out of range

					int terrNum = 0;		
					for (int t=0; t<terraces.Length-1; t++)
					{
						if (terraces[terrNum+1] > val || terrNum+1 == terraces.Length) break;
						terrNum++;
					}

					//kinda curve evaluation
					float delta = terraces[terrNum+1] - terraces[terrNum];
					float relativePos = (val - terraces[terrNum]) / delta;

					float percent = 3*relativePos*relativePos - 2*relativePos*relativePos*relativePos;

					percent = (percent-0.5f)*2;
					bool minus = percent<0; percent = Mathf.Abs(percent);

					percent = Mathf.Pow(percent,1f-steepness);

					if (minus) percent = -percent;
					percent = percent/2 + 0.5f;

					arr[i] = (terraces[terrNum]*(1-percent) + terraces[terrNum+1]*percent)*intensity + arr[i]*(1-intensity);
				}
			}

			public void Levels (Vector2 min, Vector2 max, float gamma)
			{
				for (int i=0; i<count; i++)
				{
					float val = arr[i];

					if (val < min.x) { arr[i] = 0; continue; }
					if (val > max.x) { arr[i] = 1; continue; }

					val = 1 * ( ( val - min.x ) / ( max.x - min.x ) );

					if (gamma != 1) // (gamma>1.00001f || gamma<0.9999f)
					{
						if (gamma<1) val = Mathf.Pow(val, gamma);
						else val = Mathf.Pow(val, 1/(2-gamma));
					}

					arr[i] = val;
				}
			}


			public void ReadMatrix (FloatMatrix src, CoordRect.TileMode tileMode = CoordRect.TileMode.Clamp)
			{
				Coord min = rect.Min; Coord max = rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						Coord tiledCoord = src.rect.Tile(new Coord(x,z), tileMode);
						this[x,z] = src[tiledCoord];
					}
			}



			/*public Matrix FastDownscaled (int ratio)
			{
				CoordRect downRect = new CoordRect( rect.offset, new Coord(rect.size.x/ratio, rect.size.z/ratio) );
				Matrix downMatrix = new Matrix(downRect);

				Coord min = downRect.Min; Coord max = downRect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					{
						float avgVal = 0;
						for (int sx=0; sx<ratio; sx++)
							for (int sz=0; sz<ratio; sz++)
								avgVal += arr[(z*ratio + sz) * rect.size.x  +  (x*ratio + sx)]; //this[x*ratio + sx, z*ratio + sz];
						avgVal /= ratio*ratio;

						downMatrix[x,z] = avgVal;
					}

				return downMatrix;
			}*/

		#endregion


		#region Simple Conversions

			/// No offset is used in all the Simple Conversions
			// TODO: use arrRect

			public void ReadArray (float[,] arr2D)
			{
				int maxX = rect.size.x; if (arr2D.GetLength(1) < maxX) maxX = arr2D.GetLength(1);
				int maxZ = rect.size.z; if (arr2D.GetLength(0) < maxZ) maxZ = arr2D.GetLength(0);

				for (int x=0; x<maxX; x++)
					for (int z=0; z<maxZ; z++)
						arr[z*rect.size.x + x] = arr2D[z,x];
			}

			public void ApplyArray (float[,] arr2D)
			{
				int maxX = rect.size.x; if (arr2D.GetLength(1) < maxX) maxX = arr2D.GetLength(1);
				int maxZ = rect.size.z; if (arr2D.GetLength(0) < maxZ) maxZ = arr2D.GetLength(0);

				for (int x=0; x<maxX; x++)
					for (int z=0; z<maxZ; z++)
						arr2D[z,x] = arr[z*rect.size.x + x];
			}

		#endregion


		#region Blend Algorithms

			public enum BlendAlgorithm {
				mix=0, 
				add=1, 
				subtract=2, 
				multiply=3, 
				divide=4, 
				difference=5, 
				min=6, 
				max=7, 
				overlay=8, 
				hardLight=9, 
				softLight=10} 
			
			//not using const arrays or dictionaries for native compatibility

			public void Blend (FloatMatrix m, BlendAlgorithm algorithm, float opacity=1)
			{
				switch (algorithm)
				{
					case BlendAlgorithm.mix: default: Mix(m, opacity); break;
					case BlendAlgorithm.add: Add(m, opacity); break;
					case BlendAlgorithm.subtract: Subtract(m, opacity); break;
					case BlendAlgorithm.multiply: Multiply(m, opacity); break;
					case BlendAlgorithm.divide: Divide(m, opacity); break;
					case BlendAlgorithm.difference: Difference(m, opacity); break;
					case BlendAlgorithm.min: Min(m, opacity); break;
					case BlendAlgorithm.max: Max(m, opacity); break;
					case BlendAlgorithm.overlay: Overlay(m, opacity); break;
					case BlendAlgorithm.hardLight: HardLight(m, opacity); break;
					case BlendAlgorithm.softLight: SoftLight(m, opacity); break;
				}
			}

			public static System.Func<float,float,float> GetBlendAlgorithm (BlendAlgorithm algorithm)
			{
				switch (algorithm)
				{
					case BlendAlgorithm.mix: return delegate (float a, float b) { return b; };
					case BlendAlgorithm.add: return delegate (float a, float b) { return a+b; };
					case BlendAlgorithm.subtract: return delegate (float a, float b) { return a-b; };
					case BlendAlgorithm.multiply: return delegate (float a, float b) { return a*b; };
					case BlendAlgorithm.divide: return delegate (float a, float b) { return a/b; };
					case BlendAlgorithm.difference: return delegate (float a, float b) { return Mathf.Abs(a-b); };
					case BlendAlgorithm.min: return delegate (float a, float b) { return Mathf.Min(a,b); };
					case BlendAlgorithm.max: return delegate (float a, float b) { return Mathf.Max(a,b); };
					case BlendAlgorithm.overlay: return delegate (float a, float b) 
					{
						if (a > 0.5f) return 1 - 2*(1-a)*(1-b);
						else return 2*a*b; 
					}; 
					case BlendAlgorithm.hardLight: return delegate (float a, float b) 
					{
							if (b > 0.5f) return 1 - 2*(1-a)*(1-b);
							else return 2*a*b; 
					};
					case BlendAlgorithm.softLight: return delegate (float a, float b) { return (1-2*b)*a*a + 2*b*a; };
					default: return delegate (float a, float b) { return b; };
				}
			}

		#endregion


		#region Convert

			public void WriteTexture (Texture2D texture, CoordRect regionRect = new CoordRect())
			/// Converts matrix to texture (with rescale if needed). Will crop matrix to regionRect (if specified) before convert
			{
				//rescaling if needed
				FloatMatrix src = this;
				if (texture.width != regionRect.size.x || texture.height != regionRect.size.z)
					src = Resized(new Coord(texture.width, texture.height), regionRect);

				Color[] colors = new Color[texture.width * texture.height];
				for (int i=0; i<colors.Length; i++)
				{
					float val = src.arr[i];
					colors[i] = new Color(val, val, val, 1);
				}
				
				texture.SetPixels(colors);
				texture.Apply();
			}


			public byte[] ToRawBytes (CoordRect regionRect = new CoordRect())
			{
				if (regionRect.size.x==0 && regionRect.size.z==0)
					regionRect = rect;
				Coord min = regionRect.Min; Coord max = regionRect.Max;

				byte[] bytes = new byte[regionRect.size.x * regionRect.size.z * 2];

				int bytePos = 0;
				for (int z=0; z<regionRect.size.z; z++)
				{
					int matrixRowStart = (z+regionRect.offset.z-rect.offset.z)*rect.size.x + regionRect.offset.x-rect.offset.x;
					for (int x=0; x<regionRect.size.x; x++)
					{
						int matrixPos = matrixRowStart + x;
						float val = arr[matrixPos]; //this[x+regionRect.offset.x, z+regionRect.offset.z];
						if (val > 1) val = 1;

						int intVal = (int)(val*65025);
						byte hb = (byte)(intVal/255f);
						bytes[bytePos+1] = hb;  //TODO: test if the same will work on macs with non-inverted byte order
						bytes[bytePos] = (byte)(intVal-hb*255);
						bytePos+=2;
					}
				}

				return bytes;
			}


			public void ReadRawBytes (byte[] bytes, CoordRect regionRect = new CoordRect())
			{
				if (regionRect.size.x==0 && regionRect.size.z==0)
					regionRect = rect;
				Coord min = regionRect.Min; Coord max = regionRect.Max;

				int bytePos = 0;
				for (int z=0; z<regionRect.size.z; z++)
				{
					int matrixRowStart = (z+regionRect.offset.z-rect.offset.z)*rect.size.x + regionRect.offset.x-rect.offset.x;
					for (int x=0; x<regionRect.size.x; x++)
					{
						float val = (bytes[bytePos+1]*256f + bytes[bytePos]) / 65025f;
						bytePos+=2;

						arr[matrixRowStart + x] = val;
					}
				}
			}


		#endregion


		#region Line Operations (operate with neighbor pixel per-line/row)

			public FloatMatrix Resized (CoordRect newRect, CoordRect regionRect = new CoordRect())
			{
				FloatMatrix newMatrix = Resized(newRect.size, regionRect);
				newMatrix.rect.offset = newRect.offset;
				return newMatrix;
			}

			public FloatMatrix Resized (Coord newSize, CoordRect regionRect = new CoordRect())
			/// Returns the new matrix of given dstRect size. Will read only srcRect if specified. Downscaling linear, upscaling bicubic.
			{
				FloatMatrix dst;

				if (regionRect.size.x == newSize.x  &&  regionRect.size.z == newSize.z)  // returning clone if both sizes match
					dst = CopyRegion(regionRect);

				else if (rect.size.z == newSize.z) //if vertical size match
					dst = ResizedHorizontally(newSize.x, regionRect);

				else if (rect.size.x == newSize.x) //if horizontal size match
					dst = ResizedVertically(newSize.z, regionRect);

				else
				{
					FloatMatrix intermediate = ResizedHorizontally(newSize.x, regionRect);
					dst = intermediate.ResizedVertically(newSize.z);
				}

				return dst;
			}

			public FloatMatrix ResizedHorizontally (int newWidth, CoordRect regionRect = new CoordRect())
			/// Will resize matrix only horizontally. Offset will not change.
			{
				if (regionRect.size.x==0 && regionRect.size.z==0)
					regionRect = rect;
				Coord min = regionRect.Min; Coord max = regionRect.Max;

				FloatMatrix result = new FloatMatrix( new CoordRect(regionRect.offset.x, regionRect.offset.z, newWidth, regionRect.size.z) );

				MatrixLine src = new MatrixLine(regionRect.offset.x, regionRect.size.x);
				MatrixLine dst = new MatrixLine(regionRect.offset.x, newWidth);

				for (int z=min.z; z<max.z; z++)
				{
					src.ReadLine(this, z); //srcStart.x);

					if (dst.length > src.length) MatrixLine.ResampleCubic(src,dst);
					else MatrixLine.ResampleLinear(src,dst);  

					dst.WriteLine(result, z);
				}

				return result;
			}

			
			public FloatMatrix ResizedVertically (int newHeight, CoordRect regionRect = new CoordRect())
			/// Will resize matrix only horizontally
			{
				if (regionRect.size.x==0 && regionRect.size.z==0)
					regionRect = rect;
				Coord min = regionRect.Min; Coord max = regionRect.Max;

				FloatMatrix result = new FloatMatrix( new CoordRect(regionRect.offset.x, regionRect.offset.z, regionRect.size.x, newHeight) );

				MatrixLine src = new MatrixLine(regionRect.offset.z, rect.size.z);
				MatrixLine dst = new MatrixLine(regionRect.offset.z, newHeight);

				for (int x=min.x; x<max.x; x++)
				{
					src.ReadRow(this, x);

					if (dst.length > src.length) MatrixLine.ResampleCubic(src,dst);
					else MatrixLine.ResampleLinear(src,dst);

					dst.WriteRow(result, x);
				}

				return result;
			}


			public FloatMatrix FastDownscaled (int ratio, CoordRect regionRect = new CoordRect())
			{
				if (regionRect.size.x==0 && regionRect.size.z==0)
					regionRect = rect;
				Coord min = regionRect.Min; Coord max = regionRect.Max;

				CoordRect intermediateRect = new CoordRect( regionRect.offset, new Coord(regionRect.size.x/ratio, regionRect.size.z) );
				FloatMatrix intermediate = new FloatMatrix(intermediateRect);

				MatrixLine srcH = new MatrixLine(regionRect.offset.x, regionRect.size.x);
				MatrixLine dstH = new MatrixLine(regionRect.offset.x, intermediateRect.size.x);
				for (int z=min.z; z<max.z; z++)
				{
					srcH.ReadLine(this, z);
					MatrixLine.DownsampleFast(srcH,dstH,ratio);  
					dstH.WriteLine(intermediate, z);
				}

				
				CoordRect downRect = new CoordRect( regionRect.offset, new Coord(regionRect.size.x/ratio, regionRect.size.z/ratio) );
				FloatMatrix downMatrix = new FloatMatrix(downRect);

				MatrixLine srcV = new MatrixLine(regionRect.offset.z, intermediateRect.size.z);
				MatrixLine dstV = new MatrixLine(regionRect.offset.z, downRect.size.z);
				for (int x=min.x; x<min.x+intermediateRect.size.x; x++)
				{
					srcV.ReadRow(intermediate, x);
					MatrixLine.DownsampleFast(srcV,dstV,ratio);
					dstV.WriteRow(downMatrix, x);
				}

				return downMatrix;
			}

			public FloatMatrix Cavity (float intensity)
			{
				FloatMatrix dstMatrix = new FloatMatrix(rect);
				Coord min = rect.Min; Coord max = rect.Max;

				MatrixLine line = new MatrixLine(rect.offset.x, rect.size.x);
				for (int z=min.z; z<max.z; z++)
				{
					line.ReadLine(this,z);
					line.Cavity(intensity);
					line.WriteLine(dstMatrix, z);
				}
				
				line = new MatrixLine(rect.offset.z, rect.size.z);
				for (int x=min.x; x<max.x; x++)
				{
					line.ReadRow(this, x);
					line.Cavity(intensity);
					
					//apply row additively (with mid-point 0.5)
					line.Add(-0.5f);
					line.AppendRow(dstMatrix, x);
				}

				return dstMatrix;
			}

			public FloatMatrix NormalRelief (float horizontalHeight=1, float verticalHeight=1)
			{
				FloatMatrix dstMatrix = new FloatMatrix(rect);
				Coord min = rect.Min; Coord max = rect.Max;

				MatrixLine line = new MatrixLine(rect.offset.x, rect.size.x);
				for (int z=min.z; z<max.z; z++)
				{
					line.ReadLine(this,z);
					line.Normal(horizontalHeight);
					line.WriteLine(dstMatrix, z);
				}
				  
				line = new MatrixLine(rect.offset.z, rect.size.z);
				for (int x=min.x; x<max.x; x++)
				{
					line.ReadRow(this, x);
					line.Normal(verticalHeight);
					line.AppendRow(dstMatrix, x);
				}

				dstMatrix.Multiply(0.5f); //each line has range 0-1, and they are combined additively

				return dstMatrix;
			}


			public FloatMatrix Delta ()
			/// For each pixel it evaluates 4 neighbors and sets maximum value delta
			{
				FloatMatrix dstMatrix = new FloatMatrix(rect);
				Coord min = rect.Min; Coord max = rect.Max;

				MatrixLine line = new MatrixLine(rect.offset.x, rect.size.x);
				for (int z=min.z; z<max.z; z++)
				{
					line.ReadLine(this,z);
					line.Delta();
					line.WriteLine(dstMatrix, z);
				}

				MatrixLine src = new MatrixLine(rect.offset.z, rect.size.z);
				MatrixLine dst = new MatrixLine(rect.offset.z, rect.size.z);
				for (int x=min.x; x<max.x; x++)
				{
					src.ReadRow(this, x);
					src.Delta();

					dst.ReadRow(dstMatrix, x); //reading dst row to compare with horizontal delta
					src.Max(dst);

					src.WriteRow(dstMatrix, x);
				}

				return dstMatrix;
			}

			public void Spread (int iterations, float subtract=0.01f, float multiply=1f)
			{
				FloatMatrix dstMatrix = new FloatMatrix(this);
				Coord min = rect.Min; Coord max = rect.Max;

				for (int i=0; i<iterations; i++)
				{
					dstMatrix.Mix(this, 0.5f);

					MatrixLine line = new MatrixLine(rect.offset.x, rect.size.x);
					for (int z=min.z; z<max.z; z++)
					{
						line.ReadLine(dstMatrix, z);
						line.Spread(subtract, multiply);
						line.WriteLine(dstMatrix, z);
					}

					//dstMatrix.GaussianBlur(1);

					line = new MatrixLine(rect.offset.z, rect.size.z);
					for (int x=min.x; x<max.x; x++)
					{
						line.ReadRow(dstMatrix, x);
						line.Spread(subtract, multiply);
						line.WriteRow(dstMatrix, x);
					}

					//dstMatrix.GaussianBlur(1);
				}

				arr = dstMatrix.arr;
			}


			public void GaussianBlur (float blur)
			{
				//float[] arr = new float[rect.size.x];
				//float[] tmp = new float[rect.size.x];
				Coord min = rect.Min; Coord max = rect.Max;

				MatrixLine line = new MatrixLine(rect.offset.x, rect.size.x);
				float[] temp = new float[rect.size.x];

				for (int z=min.z; z<max.z; z++)
				{
					line.ReadLine(this, z);
					line.GaussianBlur(temp,blur);
					line.WriteLine(this, z);
				}

				line = new MatrixLine(rect.offset.z, rect.size.z);
				temp = new float[rect.size.z];

				for (int x=min.x; x<max.x; x++)
				{
					line.ReadRow(this, x);
					line.GaussianBlur(temp,blur);
					line.WriteRow(this, x);
				}
			}


			public void DownsampleBlur (int downsample, float blur)
			{
				int downsamplePot = (int)Mathf.Pow(2,downsample-1);
				Coord min = rect.Min; Coord max = rect.Max;
	
				MatrixLine hiLine = new MatrixLine(rect.offset.x, rect.size.x);
				MatrixLine loLine = new MatrixLine(rect.offset.x, rect.size.x / downsample);
				float[] tmp = new float[rect.size.x / downsample];

				for (int z=min.z; z<max.z; z++)
				{
					hiLine.ReadLine(this, z);

					MatrixLine.ResampleLinear(hiLine, loLine);
					loLine.GaussianBlur(tmp, blur);
					MatrixLine.ResampleCubic(loLine, hiLine);

					hiLine.WriteLine(this, z);
				}

				hiLine = new MatrixLine(rect.offset.z, rect.size.z);
				loLine = new MatrixLine(rect.offset.z, rect.size.z / downsample);
				tmp = new float[rect.size.z / downsample];

				for (int x=min.x; x<max.x; x++)
				{
					hiLine.ReadRow(this, x);

					MatrixLine.ResampleLinear(hiLine, loLine);
					loLine.GaussianBlur(tmp, blur);
					MatrixLine.ResampleCubic(loLine, hiLine);

					hiLine.WriteRow(this, x);
				}
			}

			public void DownsampleOverblur (int downsample, float blur=1, float fallof=2)
			{
				FloatMatrix mip = this;
				Clamp01();

				for (int i=0; i<downsample; i++)
				{
					if (mip.rect.size.x/2 == 0) break; //already at the lowest level
					
					mip = mip.Resized( new Coord( mip.rect.size.x/2, mip.rect.size.z/2) ); //downscaling to mip matrix
					mip.GaussianBlur(blur);
					mip.Contrast(fallof);

					FloatMatrix blurred = mip.Resized( this.rect.size );

					//float contrast = i*fallof + sharpness;

					for (int a=0; a<arr.Length; a++)
					{
						float valA = arr[a];
						float valB = blurred.arr[a];

						//apply overlay
						if (valA<0.5f) valA = 2*valA*valB;
						else valA = 1 - 2*(1-valA)*(1-valB);

						//clamp 0-1 preventing over-value in next iterations
						if (valA > 1) valA = 1;
						if (valB < 0) valB = 0;

						arr[a] = valA;
					}
				}
			}


			public void ExtendCircular (Coord center, int radius, int extendRange, int predictEvaluateRange)
			{
				//resetting area out of radius
				StampInverted(center.x, center.z, radius, 1, -Mathf.Epsilon);

				//creating radial lines
				int numLines = Mathf.CeilToInt( Mathf.PI * radius ); //using only the half of the needed lines
				float angleStep = Mathf.PI * 2 / numLines; //in radians

				MatrixLine line = new MatrixLine(0, Mathf.CeilToInt(extendRange + predictEvaluateRange + 1));

				for (int i=0; i<numLines; i++)
				{
					float angle = i*angleStep;
					Vector2 direction = new Vector2( Mathf.Sin(angle), Mathf.Cos(angle) );
					Vector2 start = center.vector2 + direction*(radius-predictEvaluateRange);

					//making any of the step components equal to 1
					Vector2 posDir = new Vector2 (
						(direction.x>0 ? direction.x : -direction.x),
						(direction.y>0 ? direction.y : -direction.y) );
					float max = posDir.x>posDir.y ? posDir.x : posDir.y;
					Vector2 step = direction / max; 
					int predictStart = (int)(predictEvaluateRange * max);
					
					line.ReadInclined(this, start, step);
					line.PredictExtend(predictStart);
					line.WriteInclined(this, start, step);
				}

				//filling gaps between lines
				line = new MatrixLine(0, (int)(radius+extendRange+1)*2*4);

				for (int i=(int)(radius*0.7f); i<radius+extendRange; i++)
				{
					line.ReadSquare(this, center, i);
					line.FillGaps(0, i*2*4);
					line.WriteSquare(this, center, i);
				}

				//blurring circular
				/*float[] tmp = new float[line.length];
				Line loLine = new Line(0, line.length);

				for (int i=(int)radius; i<radius+extendRange; i++)
				{
					line.ReadSquare(this, cCenter, i);
					
					loLine.length = line.length / 16;

					//Line.DownsampleFast(line, loLine, 16);
					for (int j=0; j<16; j++)
						line.GaussianBlur(tmp, 1f);
					//Line.ResampleCubic(loLine, line);

					line.WriteSquare(this, cCenter, i);
				}*/
			}


			public void SpreadBlurCircular (Coord center, float radius, int blur=2)
			/// Spread-blurs the circular stamp stroke at the center position (does not need to at matrix center) after the radius value
			{
				SpreadBlurCircularCorner(center, radius, blur:blur);
				SpreadBlurCircularCorner(center, radius, blur:blur, bottom:true);
				SpreadBlurCircularCorner(center, radius, blur:blur, left:true);
				SpreadBlurCircularCorner(center, radius, blur:blur, left:true, bottom:true);
			}


			private void SpreadBlurCircularCorner (Coord center, float radius, int blur=2, bool left=false, bool bottom=false)
			/// Circular spread blur iteration, it takes one corner relatively to center only
			{
				FloatMatrix src = (FloatMatrix)Clone();

				//the maximum possible rect (includes center)
				CoordRect maxRect = rect;
				maxRect.Encapsulate(center);

				//upper left corner
				CoordRect cornerRect = new CoordRect (center.x, center.z, maxRect.size.x, maxRect.size.z); //creating a maximum corner rect, then intersecting it with real matrix
				if (left) cornerRect.offset.x -= maxRect.size.x;
				if (bottom) cornerRect.offset.z -= maxRect.size.z;
				cornerRect = CoordRect.Intersected(rect, cornerRect);

				if (cornerRect.size.x ==0 || cornerRect.size.z == 0) return;

				Coord min = cornerRect.Min; Coord max = cornerRect.Max;

				Vector2 gradientStart = (center.vector2 - min.vector2) / radius; //the value at which the rect's gradient start (from 0 to 1)
				Vector2 gradientEnd = (center.vector2 - max.vector2) / radius;
				if (!left) { gradientStart.x = - gradientStart.x;  gradientEnd.x = - gradientEnd.x; }
				if (!bottom) { gradientStart.y = - gradientStart.y;  gradientEnd.y = - gradientEnd.y; }

				MatrixLine currLine = new MatrixLine(cornerRect.offset.x, cornerRect.size.x);
				MatrixLine prevLine = new MatrixLine(cornerRect.offset.x, cornerRect.size.x);
				MatrixLine mask = new MatrixLine(cornerRect.offset.x, cornerRect.size.x);


				if (bottom) prevLine.ReadLine(src, min.z);
				else		prevLine.ReadLine(src, max.z-1);

				for (int iz=0; iz<cornerRect.size.z; iz++)
				{
					int z = bottom ? max.z-iz-1 : min.z+iz;

					currLine.ReadLine(src, z);
					MatrixLine.SpreadBlur(ref currLine, ref prevLine, blur);

					float gradientPercentZ = 1f * (z-min.z) / (max.z - min.z);
					float gradientZ = gradientStart.y*(1-gradientPercentZ) + gradientEnd.y*gradientPercentZ;
					mask.Gradient((gradientStart.x + (1-gradientZ))*0.5f, (gradientEnd.x + (1-gradientZ))*0.5f);
					//mask.WriteLine(this, z); // to test

					prevLine.WriteLine(this, z, mask);  //writing prev line since lines are swapped now
				}

				currLine = new MatrixLine(cornerRect.offset.z, cornerRect.size.z);
				prevLine = new MatrixLine(cornerRect.offset.z, cornerRect.size.z);
				mask = new MatrixLine(cornerRect.offset.z, cornerRect.size.z);

				if (left) prevLine.ReadRow(src, min.x);
				else	  prevLine.ReadRow(src, max.x-1);

				//for (int x=max.x-1; x>=min.x; x--)
				for (int ix=0; ix<cornerRect.size.x; ix++)
				{
					int x = left ? max.x-ix-1 : min.x+ix;
					
					currLine.ReadRow(src, x);
					MatrixLine.SpreadBlur(ref currLine, ref prevLine, blur);

					float gradientPercentX = 1f * (x-min.x) / (max.x - min.x);
					float gradientX = gradientStart.x*(1-gradientPercentX) + gradientEnd.x*gradientPercentX;
					mask.Gradient((gradientStart.y + (1-gradientX))*0.5f, (gradientEnd.y + (1-gradientX))*0.5f);
					//mask.WriteRow(this, x); // to test

					prevLine.AppendRow(this, x, mask);  //writing prev line since lines are swapped now
				}
			}

		#endregion


		#region Native (under construction)

			[DllImport ("NativePlugins", EntryPoint = "MatrixResize")]
			public static extern void Resize (FloatMatrix src, FloatMatrix dst);

			[DllImport ("NativePlugins", EntryPoint = "MatrixFastDownscale")]
			public static extern void FastDownscale (FloatMatrix src, FloatMatrix dst, int ratio);

			/*public void ReadMatrix (Matrix srcMatrix, 
				CoordRect srcRect = new CoordRect(), 
				CoordRect.TileMode tileMode = CoordRect.TileMode.Clamp,
				Interpolation upscaleInterpolation = Interpolation.Bicubic,
				Interpolation downscaleInterpolation = Interpolation.Linear)
			/// Copies matrix with resize. If rect is defined then copies only the given rect.
			{
				if (srcRect.size.x == 0 && srcRect.size.z == 0 && srcRect.offset.x == 0 && srcRect.offset.z == 0)
					srcRect = srcMatrix.rect;

				Interpolation interpolation = Interpolation.None;
				if (rect.size == srcRect.size) interpolation = Interpolation.None;
				else if (rect.size.x > srcRect.size.x || rect.size.z > srcRect.size.z) interpolation = upscaleInterpolation;
				else interpolation = downscaleInterpolation;

				Coord min = rect.Min; Coord max = rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					double percentX = 1.0 * (x-min.x) / rect.size.x;
					double percentZ = 1.0 * (z-min.z) / rect.size.z;

					float srcX = (float)(percentX*srcRect.size.x) + srcRect.offset.x;
					float srcZ = (float)(percentZ*srcRect.size.z) + srcRect.offset.z;

					switch (interpolation)
					{
						case Interpolation.Linear: this[x,z] = srcMatrix.GetInterpolated(srcX, srcZ, tileMode:tileMode); break;
						case Interpolation.Bicubic: this[x,z] = srcMatrix.GetInterpolatedBicubic(srcX, srcZ, tileMode:tileMode); break;
						default: this[x,z] = srcMatrix.GetTiled((int)srcX, (int)srcZ, tileMode); break;
					}
				}
			}*/

		#endregion


		#region Native Per-pixel (outdated?)

			public float GetInterpolated (float x, float z, CoordRect.TileMode tileMode = CoordRect.TileMode.Clamp)
			{
				//neig coords
				int px = (int)x; if (x<0) px--; //because (int)-2.5 gives -2, should be -3 
				int nx = px+1;

				int pz = (int)z; if (z<0) pz--; 
				int nz = pz+1;

				//blending percent between pixels
				float percentX = x-px;
				float percentZ = z-pz;

				//reading values
				float val_pxpz = GetTiled(px,pz, tileMode);
				float val_nxpz = GetTiled(nx,pz, tileMode);
				float val_fz = val_pxpz*(1-percentX) + val_nxpz*percentX;

				float val_pxnz = GetTiled(px,nz, tileMode);
				float val_nxnz = GetTiled(nx,nz, tileMode);
				float val_cz = val_pxnz*(1-percentX) + val_nxnz*percentX;

				float val = val_fz*(1-percentZ) + val_cz*percentZ;

				return val;
			}


			public void AddInterpolated (float x, float z, float val)
			{
				//neig coords
				int px = (int)x; if (x<0) px--; //because (int)-2.5 gives -2, should be -3 
				int nx = px+1;

				int pz = (int)z; if (z<0) pz--; 
				int nz = pz+1;

				//blending percent between pixels
				float percentX = x-px;
				float percentZ = z-pz;

				//writing values
				int pos = (pz-rect.offset.z)*rect.size.x + px - rect.offset.x;
				arr[pos]				+=	val *	(1-percentX) *  (1-percentZ);
				arr[pos+1]				+=	val *	percentX *		(1-percentZ);
				arr[pos+rect.size.x]	+=	val *	(1-percentX) *  percentZ;
				arr[pos+rect.size.x+1]	+=	val *	percentX *  percentZ;
			}


			public float GetInterpolatedBicubic (float x, float z, CoordRect.TileMode tileMode = CoordRect.TileMode.Clamp)
			{
				//neig coords - z axis
				int p = (int)z; if (z<0) z--; //because (int)-2.5 gives -2, should be -3 
				int n = p+1;
				int pp = p-1;
				int nn = n+1;

				//blending percent
				float percent = z-p;

				//reading values
				float vp = GetInterpolateCubic (x, p, tileMode);
				float vpp = GetInterpolateCubic (x, pp, tileMode);
				float vn = GetInterpolateCubic (x, n, tileMode);
				float vnn = GetInterpolateCubic (x, nn, tileMode);

				return vp + 0.5f * percent * (vn - vpp + percent*(2.0f*vpp - 5.0f*vp + 4.0f*vn - vnn + percent*(3.0f*(vp - vn) + vnn - vpp)));
			}

			public float GetInterpolateCubic (float x, int z, CoordRect.TileMode tileMode = CoordRect.TileMode.Clamp)
			/// Gets interpolated result using a horizontal level only
			{
				//neig coords - x axis
				int p = (int)x; if (x<0) p--; //because (int)-2.5 gives -2, should be -3 
				int n = p+1;
				int pp = p-1;
				int nn = n+1;

				//blending percent
				float percent = x-p;

				//reading values
				float vp = GetTiled(p,z,tileMode);
				float vpp = GetTiled(pp,z,tileMode);
				float vn = GetTiled(n,z,tileMode);
				float vnn = GetTiled(nn,z,tileMode);

				return vp + 0.5f * percent * (vn - vpp + percent*(2.0f*vpp - 5.0f*vp + 4.0f*vn - vnn + percent*(3.0f*(vp - vn) + vnn - vpp)));
			}


			public float GetTiled (Coord coord, CoordRect.TileMode tileMode)
			/// Returns the usual value if coord is in rect, handles tiling if it is not
			{
				if (rect.Contains(coord))
					return this[coord];

				Coord tiledCoord = rect.Tile(coord, tileMode);
				return this[tiledCoord];
			}

			public float GetTiled (int x, int z, CoordRect.TileMode tileMode) { return GetTiled(new Coord(x,z), tileMode); }


			public void AddLine (Vector3 start, Vector3 end, float valStart, float valEnd, bool antialised=false)
			{
				float rectSizeX = Mathf.Abs(end.x-start.x);
				float rectSizeZ = Mathf.Abs(end.z-start.z);

				int length = (int)(rectSizeX>rectSizeZ ? rectSizeX : rectSizeZ) + 1;

				float stepX = (end.x-start.x) / length;
				float stepZ = (end.z-start.z) / length;

				if (rectSizeX > rectSizeZ)
				{ 
					stepZ /= Mathf.Abs(stepX);
					stepX = stepX>0 ? 1 : -1;
				}
				else
				{
					stepX /= Mathf.Abs(stepZ);
					stepZ = stepZ>0 ? 1 : -1;
				}

				for (int i=0; i<length; i++)
				{
					float x = start.x + stepX*i;
					float z = start.z + stepZ*i;

					int ix = (int)(float)x;  if (x<0) ix--;  
					int iz = (int)(float)z;  if (z<0) iz--;  

					if (ix<rect.offset.x || ix>=rect.offset.x+rect.size.x ||
						iz<rect.offset.z || iz>=rect.offset.z+rect.size.z )
							continue;

					int pos = (iz-rect.offset.z)*rect.size.x + ix - rect.offset.x;
					float percent = 1f*i/length;
					float val = valStart*(1-percent) + valEnd+percent;

					arr[pos] = val;

					if (antialised)
					{
						if (rectSizeX > rectSizeZ)
						{
							arr[pos-rect.size.x] = val*(1-(z-iz));
							arr[pos+rect.size.x] = val*(z-iz);
						}
						else
						{
							arr[pos-1] = val*(1-(x-ix));
							arr[pos+1] = val*(x-ix);
						}
					}
				}
			}

		#endregion
	}

}//namespace