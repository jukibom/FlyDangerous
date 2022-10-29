using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools
{
	public static class TextureArrayTools 
	{

		public static void SetTexture (this Texture2DArray dstArr, Texture2D src, int dstCh, bool apply=true)
		{
			//Debug.Log("Setting Texture " + src.name + " " + System.IO.Path.GetFileName(UnityEditor.AssetDatabase.GetAssetPath(src)) + " " + src.imageContentsHash);

			if (dstArr.depth <= dstCh) throw new System.IndexOutOfRangeException("Trying to set channel (" + dstCh + ") >= depth (" + dstArr.depth + ")");

			//quick case if size and format match
			if (src.width == dstArr.width  &&  src.height == dstArr.height  &&  src.format == dstArr.format)
			{
				Graphics.CopyTexture(src,0,  dstArr,dstCh);
				if (apply) dstArr.Apply(updateMipmaps:false);
				return;
			}

			if (!src.IsReadable()) src = src.ReadableClone();  //texture should be readable to uncompress
			if (src.format.IsCompressed()) src = src.UncompressedClone();  

			if (src.width != dstArr.width  ||  src.height != dstArr.height)
				src = src.ResizedClone(dstArr.width, dstArr.height);
			
			#if UNITY_EDITOR
				
			#if UNITY_2018_3_OR_NEWER
			UnityEditor.EditorUtility.CompressTexture(src, dstArr.format, 100);  //de-compress and compress to change the format
			#else
			UnityEditor.EditorUtility.CompressTexture(src, dstArr.format, TextureCompressionQuality.Best);  //de-compress and compress to change the format
			#endif

			#else
			if (dstArr.format.IsCompressed()) src.Compress(true);
			#endif
			src.Apply(updateMipmaps:false);

			Graphics.CopyTexture(src,0,  dstArr,dstCh);
			if (apply) dstArr.Apply(updateMipmaps:false);
		}

		public static void SetTextureAlpha (this Texture2DArray dstArr, Texture2D src, Texture2D alpha, int dstCh, bool apply=true)
		/// Sets RGB to src RGB, and A to desaturated alpha RGB
		{
			if (alpha == null) { dstArr.SetTexture(src, dstCh, apply:apply); return; } //shortcut if alpha is null
			
			if (!src.IsReadable()) src = src.ReadableClone();
			if (src.format.IsCompressed()) src = src.UncompressedClone(); 
			if (src.width != dstArr.width  ||  src.height != dstArr.height) src = src.ResizedClone(dstArr.width, dstArr.height);

			if (!alpha.IsReadable()) alpha = alpha.ReadableClone();
			//if (!alpha.format.IsCompressed()) alpha = alpha.UncompressedClone();  //no need to change alpha format, we will read pixels anyways
			if (alpha.width != dstArr.width  ||  alpha.height != dstArr.height) alpha = alpha.ResizedClone(dstArr.width, dstArr.height);


			Texture2D tmp = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true, src.IsLinear());
			int mipmapCount = src.mipmapCount;
			for (int m=0; m<mipmapCount; m++)
			{
				Color[] srcColors = src.GetPixels(m);
				Color[] alphaColors = alpha.GetPixels(m);
				
				for (int i=0; i<srcColors.Length; i++)
					srcColors[i] = new Color(srcColors[i].r, srcColors[i].g, srcColors[i].b, alphaColors[i].r*0.3f + alphaColors[i].r*0.6f + alphaColors[i].r*0.1f);
				tmp.SetPixels(srcColors,m);
			}

			tmp.Apply(updateMipmaps:false);
			dstArr.SetTexture(tmp, dstCh, apply:apply);
		}

		public static Texture2D GetTexture (this Texture2DArray srcArr, int srcCh, bool readable=true)
		{
			Texture2D tex = new Texture2D(srcArr.width, srcArr.height, srcArr.format, true, linear:srcArr.IsLinear());
			Graphics.CopyTexture(srcArr,srcCh,  tex,0);
			tex.Apply(updateMipmaps:false, makeNoLongerReadable:!readable);
			return tex;
		}


		public static Texture2D[] GetTextures (this Texture2DArray srcArr)
		{
			Texture2D[] result = new Texture2D[srcArr.depth];

			for (int ch=0; ch<srcArr.depth; ch++)
				result[ch] = GetTexture(srcArr, ch);

			return result;
		}


		public static Color GetPixel (this Texture2DArray srcArr, int x, int y, int ch)
		/// Debug purpose only
		{
			Texture2D tmp = srcArr.GetTexture(ch);
			return tmp.GetPixel(x,y);
		}

		public static void FillTexture (this Texture2DArray srcArr, Texture2D dst, int srcCh)
		{
			if (srcArr.depth <= srcCh) throw new System.IndexOutOfRangeException("Trying to get channel (" + srcCh + ") >= depth (" + srcArr.depth + ")");

			//format and size match
			if (srcArr.format == dst.format  &&  srcArr.width == dst.width  &&  srcArr.height == dst.height)
			{
				Graphics.CopyTexture(srcArr,srcCh,  dst,0);
				dst.Apply(updateMipmaps:false);
				return;
			}

			Texture2D tmpTex = srcArr.GetTexture(srcCh, readable:true);
			if (tmpTex.format.IsCompressed()) tmpTex = tmpTex.UncompressedClone();  //uncompress to change format

			if (tmpTex.width != dst.width  ||  tmpTex.height != dst.height)
				tmpTex = tmpTex.ResizedClone(dst.width, dst.height);

			#if UNITY_EDITOR

			#if UNITY_2018_3_OR_NEWER
			UnityEditor.EditorUtility.CompressTexture(tmpTex, dst.format, 100);  //de-compress and compress to change the format
			#else
			UnityEditor.EditorUtility.CompressTexture(tmpTex, dst.format, TextureCompressionQuality.Best);  //de-compress and compress to change the format
			#endif


			#else
			if (dst.format.IsCompressed()) tmpTex.Compress(true);
			#endif
			tmpTex.Apply(updateMipmaps:false);

			Graphics.CopyTexture(tmpTex, dst);
			dst.Apply(updateMipmaps:false);
		}


		public static void CopyTexture (Texture2DArray srcArr, int srcCh,  Texture2DArray dstArr, int dstCh) { CopyTextures(srcArr, srcCh, dstArr, dstCh, 1); }

		public static void CopyTextures (Texture2DArray srcArr, Texture2DArray dstArr, int length) { CopyTextures(srcArr, 0, dstArr, 0, length); }
		public static void CopyTextures (Texture2DArray srcArr, int srcIndex, Texture2DArray dstArr, int dstIndex, int length)
		/// Bulk textures copy from index to index
		{
			//format and size match
			if (srcArr.format == dstArr.format  &&  srcArr.width == dstArr.width  &&  srcArr.height == dstArr.height)
			{
				for (int i=0; i<length; i++)
					Graphics.CopyTexture(srcArr,srcIndex+i,  dstArr,dstIndex+i);
				dstArr.Apply(updateMipmaps:false);
				return;
			}

			Texture2D tmpTex = new Texture2D(dstArr.width, dstArr.height, dstArr.format, true, linear:srcArr.IsLinear());
			for (int i=0; i<length; i++)
			{
				srcArr.FillTexture(tmpTex, srcIndex+i);
				Graphics.CopyTexture(tmpTex,0, dstArr,dstIndex+i);
			}

			dstArr.Apply(updateMipmaps:false);
		}


		public static void Add (ref Texture2DArray texArr, Texture2D tex) { var newArr = Add(texArr, tex); Rewrite(ref texArr, newArr); }
		public static Texture2DArray Add (Texture2DArray texArr, Texture2D tex)
		{
			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth+1, texArr.format, true, linear:texArr.IsLinear());
			newArr.name = texArr.name;
			
			CopyTextures(texArr, newArr, texArr.depth);

			newArr.SetTexture(tex, texArr.depth, apply:false);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}


		static public void Insert (ref Texture2DArray texArr, int pos, Texture2D tex) { var newArr = Insert(texArr, pos, tex); Rewrite(ref texArr, newArr); }
		static public Texture2DArray Insert (Texture2DArray texArr, int pos, Texture2D tex)
		{
			bool linear = texArr.IsLinear();
			if (texArr==null || texArr.depth==0) 
			{ 
				texArr = new Texture2DArray(tex.width, tex.height, 1, texArr.format, true, linear:linear);
				texArr.filterMode = FilterMode.Trilinear;
				texArr.SetTexture(tex, 0, apply:false);
				return texArr;
			}

			if (pos > texArr.depth || pos < 0) pos = texArr.depth;
				
			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth+1, texArr.format, true, linear:linear);
			newArr.name = texArr.name;
			
			if (pos != 0) CopyTextures(texArr, newArr, pos);
			if (pos != texArr.depth) CopyTextures(texArr, pos, newArr, pos+1, texArr.depth-pos);

			if (tex!=null) newArr.SetTexture(tex, pos, apply:false);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}


		static public Texture2DArray InsertRange (Texture2DArray texArr, int pos, Texture2DArray addArr)
		{
			//if (texArr==null || texArr.depth==0) { return addArr; }
			if (pos > texArr.depth || pos < 0) pos = texArr.depth;
				
			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth+addArr.depth, texArr.format, true, linear:texArr.IsLinear());
			newArr.name = texArr.name;

			if (pos != 0) CopyTextures(texArr, newArr, pos);
			CopyTextures(addArr, 0, newArr, pos, addArr.depth);
			if (pos != texArr.depth) CopyTextures(texArr, pos, newArr, pos+addArr.depth, texArr.depth-pos);
			
			newArr.Apply(updateMipmaps:false);
			return newArr;
		}
		
		static public void Switch (ref Texture2DArray texArr, int num1, int num2) { Switch(texArr, num1, num2); Rewrite(ref texArr, texArr); }
		static public void Switch (this Texture2DArray texArr, int num1, int num2)
		{
			if (num1<0 || num1>=texArr.depth || num2<0 || num2 >=texArr.depth) return;
				
			Texture2D temp = texArr.GetTexture(num1);
			CopyTexture(texArr,num2,  texArr,num1);
			texArr.SetTexture(temp,num2);
		}


		static public void Clear (this Texture2DArray texArr, int chNum)
		/// Fills channel with blank without removing it
		{	
			Texture2D temp = new Texture2D(texArr.width, texArr.height, texArr.format, true, linear:texArr.IsLinear());
			texArr.SetTexture(temp,chNum);
		}


		static public void RemoveAt (ref Texture2DArray texArr, int num) { var newArr = RemoveAt(texArr, num); Rewrite(ref texArr, newArr); }
		static public Texture2DArray RemoveAt (Texture2DArray texArr, int num)
		{
			if (num >= texArr.depth || num < 0) return texArr;

			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth-1, texArr.format, true, linear:texArr.IsLinear());
			newArr.name = texArr.name;

			if (num!=0) CopyTextures(texArr, newArr, num);
			if (num!=texArr.depth) CopyTextures(texArr, num+1, newArr, num, newArr.depth-num);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}


		static public void ChangeCount (ref Texture2DArray texArr, int newSize) { var newArr = ChangeCount(texArr, newSize); Rewrite(ref texArr, newArr); }
		static public Texture2DArray ChangeCount (Texture2DArray texArr, int newSize)
		{
			//if (texArr.depth == newSize) return texArr;

			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, newSize, texArr.format, true, linear:texArr.IsLinear());
			newArr.name = texArr.name;
					
			int min = newSize<texArr.depth? newSize : texArr.depth;
			CopyTextures(texArr, newArr, min);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}


		static public Texture2DArray ResizedClone (this Texture2DArray texArr, int newWidth, int newHeight)
		{
			Texture2DArray newArr = new Texture2DArray(newWidth, newHeight, texArr.depth, texArr.format, true, linear:texArr.IsLinear());
			newArr.name = texArr.name;

			for (int i=0; i<texArr.depth; i++)
				CopyTexture(texArr,i, newArr,i);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}


		static public Texture2DArray FormattedClone (this Texture2DArray texArr, TextureFormat format)
		{
			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth, format, true, linear:texArr.IsLinear());
			newArr.name = texArr.name;

			int depth = texArr.depth;
			for (int i=0; i<depth; i++)
				CopyTexture(texArr,i, newArr,i);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}

		static public Texture2DArray LinearClone (this Texture2DArray texArr, bool linear)
		{
			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth, texArr.format, true, linear:linear);
			newArr.name = texArr.name;

			int depth = texArr.depth;
			for (int i=0; i<depth; i++)
				CopyTexture(texArr,i, newArr,i);

			newArr.Apply(updateMipmaps:false);
			return newArr;
		}

		static public Texture2DArray WritableClone (this Texture2DArray texArr)
		{
			Texture2DArray newArr = new Texture2DArray(texArr.width, texArr.height, texArr.depth, texArr.format, true, linear:texArr.IsLinear());
			for (int i=0; i<texArr.depth; i++)
			{
				CopyTexture(texArr,i, newArr,i);
			}

			newArr.Apply(updateMipmaps:true);
			return newArr;
		}


		public static int GetMipMapCount (this Texture2DArray texArr)
		{
			for (int i=0; i<100; i++)
			{
				try { texArr.GetPixels(0,i); }
				catch { return i; }
			}
			return -1;
		}

		public static bool IsReadWrite (this Texture2DArray texArr)
		{
			try { texArr.SetPixels(null,0); }
			catch { return false; }
			return true;
		}


		public static void Rewrite (ref Texture2DArray texArr, Texture2DArray newArr)
		{
			#if UNITY_EDITOR
			bool isSelected = UnityEditor.Selection.activeGameObject == texArr;

			UnityEditor.AssetImporter texImporter = texArr.GetImporter();
			if (texImporter == null) return;

			//string userData = texImporter.userData; //do not owerwrite userdata
			UnityEditor.EditorUtility.CopySerialized(newArr, texArr);
			UnityEditor.EditorUtility.SetDirty(texArr);   
			UnityEditor.AssetDatabase.SaveAssets(); 

			if (isSelected) UnityEditor.Selection.activeObject = texArr;

			
			//texArr = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
			//texArr.GetImporter().userData = userData;
			#endif
		}


		#region GUID operations

		/*public static Texture2D GetSource (this Texture2DArray texArr, int num, bool isAlpha=false)
		{
			#if UNITY_EDITOR
			if (!UnityEditor.AssetDatabase.Contains(texArr)) 
				{ Debug.Log("Texture Array is not an asset: could not get source tex"); return null; }

			string[] sourceGuids = texArr.GetUserData(isAlpha? "TexArr_alphaLayers" : "TexArr_sourceLayers");

			if (num < sourceGuids.Length)
				return sourceGuids[num].GUIDtoObj<Texture2D>();
			else 
				return null;
			#else
			return null;
			#endif
		}  

		public static void SetSource (this Texture2DArray texArr, Texture2D src, int num, bool isAlpha=false, bool reload = true)
		{
			#if UNITY_EDITOR


			//checking if user data could be saved
			if (!UnityEditor.AssetDatabase.Contains(texArr)) 
				{ Debug.Log("Texture Array is not an asset: could not set source tex"); return; }
			if (src != null  &&  !UnityEditor.AssetDatabase.Contains(src)) 
				{ Debug.Log("Texture is not an asset: could not set source"); return; }


			string arrGuid = texArr.GUID();
			string[] sourceGuids = texArr.GetUserData(isAlpha? "TexArr_alphaLayers" : "TexArr_sourceLayers");


			//removing link to arr in previous tex
			Texture2D prevTex = texArr.GetSource(num, isAlpha);
			if (prevTex != null)
			{
				string prevTexGuid = sourceGuids[num];
				int usedCount = sourceGuids.FindCount(prevTexGuid);
				if (usedCount == 1) //if used once (>0) and only once (<2)
				{
					string[] prevTexData = prevTex.GetUserData(isAlpha? "TexArr_textureArray_asAlpha": "TexArr_textureArray_asSource");
					ArrayTools.RemoveAll(ref prevTexData, arrGuid);
					prevTex.SetUserData(isAlpha? "TexArr_textureArray_asAlpha": "TexArr_textureArray_asSource", prevTexData, reload:false);
				}
			}


			//assigning link to arr in new tex
			if (src != null)
			{
				string[] srcData = src.GetUserData(isAlpha? "TexArr_textureArray_asAlpha": "TexArr_textureArray_asSource");
				if (!srcData.Contains(arrGuid)) 
					ArrayTools.Add(ref srcData, arrGuid);
				src.SetUserData(isAlpha? "TexArr_textureArray_asAlpha": "TexArr_textureArray_asSource", srcData, reload:false);
			}

			sourceGuids[num] = src.GUID();
			texArr.SetUserData(isAlpha? "TexArr_alphaLayers" : "TexArr_sourceLayers", sourceGuids);

			if (reload) UnityEditor.AssetDatabase.Refresh();
			#endif
		}



		public static void ClearSources (this Texture2DArray texArr, bool isAlpha=false, bool reload = true)
		/// Clear user data and unlink source textures
		{
			#if UNITY_EDITOR
			
			if (!UnityEditor.AssetDatabase.Contains(texArr)) 
				{ Debug.Log("Texture Array is not an asset: could not set sources count"); return; }

			//unlinking previous sources
			string[] sourceGuids = texArr.GetUserData(isAlpha? "TexArr_alphaLayers" : "TexArr_sourceLayers");
			for (int i=0; i<sourceGuids.Length; i++) texArr.SetSource(null, i, isAlpha, reload:false);

			texArr.SetUserData(isAlpha? "TexArr_alphaLayers" : "TexArr_sourceLayers", new string[0]);
			if (reload) UnityEditor.AssetDatabase.Refresh();

			#endif
		}

		public static void ResizeSources (this Texture2DArray texArr, int count, bool isAlpha=false, bool reload=true)
		/// This will erase all sources
		{
			#if UNITY_EDITOR
			
			if (!UnityEditor.AssetDatabase.Contains(texArr)) 
				{ Debug.Log("Texture Array is not an asset: could not set sources count"); return; }

			texArr.ClearSources(isAlpha, reload:false);

			//creating new
			string[] sourceGuids = new string[count];
			for (int i=0; i<sourceGuids.Length; i++) sourceGuids[i] = "";

			texArr.SetUserData(isAlpha? "TexArr_alphaLayers" : "TexArr_sourceLayers", sourceGuids);
			if (reload) UnityEditor.AssetDatabase.Refresh();

			#endif
		}*/

		#endregion

	}

}
