using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Den.Tools.Matrices;

namespace Den.Tools
{
	public static class TextureExtensions
	{
			public static bool IsReadable (this Texture2D tex)
			{
				try
				{
					tex.GetPixel(0,0);
					return true;
				}
				catch
				{
					return false;
				}
			}

			public static bool IsLinear (this Texture tex)
			{
				#if UNITY_EDITOR
/*				//this works for saved texture, but not for texture2darray:
				//string path = UnityEditor.AssetDatabase.GetAssetPath(tex);
				//UnityEditor.AssetImporter importer = UnityEditor.TextureImporter.GetAtPath(path);
				//Debug.Log(importer.GetType());
				//UnityEditor.TextureImporter teximporter = (UnityEditor.TextureImporter)importer;
				//return teximporter.sRGBTexture;
				
				System.Reflection.Assembly a = typeof(UnityEditor.EditorWindow).Assembly;
				System.Type t = a.GetType("UnityEditor.TextureUtil");
				string s = (string)t.GetMethod("GetTextureColorSpaceString").Invoke(null, new object[] { tex });
				return s=="Linear";
				//you know the better way to get texture array color space?
				//let me know mail@denispahunov.ru
*/				
				return false;

				#else
				return false;
				#endif
			}


		
			public static Texture2D ReadableClone (this Texture2D tex)
			{
				Texture2D readTex = new Texture2D(tex.width, tex.height, tex.format, true, linear:tex.IsLinear());

				Graphics.CopyTexture(tex,readTex);

				readTex.Apply(updateMipmaps:false);
				return readTex;
			}

			public static Texture2D UncompressedClone (this Texture2D tex)
			/// Just using UnityEditor.EditorUtility.CompressTexture will not convert compressed to compressed (DXT5 to DXT1, for example)
			/// Texture should be readable
			{
				int mipmapCount = tex.mipmapCount;
				Texture2D resultTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, mipmapCount!=1, linear:tex.IsLinear());
				for (int m=0; m<mipmapCount; m++)
				{
					Color[] colors = tex.GetPixels(m);
					resultTex.SetPixels(colors,m);
				}
				resultTex.Apply(updateMipmaps:false);
				return resultTex;
			}

			static public Texture2D ResizedClone (this Texture2D tex, int newWidth, int newHeight)
			/// Texture should be be readable and not compressed
			{
				Texture2D newTex = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, true, linear:tex.IsLinear()); //has to be ARGB32 to set pixels
				newTex.name = tex.name;

				Color[] pixels = tex.GetPixels();
				pixels = pixels.ResizeColorArray(tex.width, tex.height, newWidth, newHeight);
				newTex.SetPixels(pixels);

				newTex.Apply(updateMipmaps:true);
				return newTex;
			}


			public static Color[] ResizeColorArray (this Color[] srcColors, int oldWidth, int oldHeight, int newWidth, int newHeight)
			/// Helper for GetPixelsResize
			{
				Color[] dstColors = new Color[newWidth*newHeight];

				Matrix src = new Matrix( new CoordRect(0,0,oldWidth, oldHeight) ); 
				Matrix dst = new Matrix( new CoordRect(0,0, newWidth, newHeight) ); 

				//reds
				for (int i=0; i<srcColors.Length; i++) 
					src.arr[i] = srcColors[i].r;

				MatrixOps.Resize(src, dst);

				for (int i=0; i<dstColors.Length; i++) 
					dstColors[i].r = dst.arr[i];

				//greens
				for (int i=0; i<srcColors.Length; i++) 
					src.arr[i] = srcColors[i].g;

				MatrixOps.Resize(src, dst);

				for (int i=0; i<dstColors.Length; i++) 
					dstColors[i].g = dst.arr[i];

				//blues
				//when I was arrested I was dressed in black they put me on a train and they took me back
				for (int i=0; i<srcColors.Length; i++) 
					src.arr[i] = srcColors[i].b;

				MatrixOps.Resize(src, dst);

				for (int i=0; i<dstColors.Length; i++) 
					dstColors[i].b = dst.arr[i];

				//alphas
				for (int i=0; i<srcColors.Length; i++) 
					src.arr[i] = srcColors[i].a;

				MatrixOps.Resize(src, dst);

				for (int i=0; i<dstColors.Length; i++) 
					dstColors[i].a = dst.arr[i];

				return dstColors;
			}


			public static Texture2D ColorTexture (int width, int height, Color color, bool linear=false)
			{
				Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true, linear:linear);
				result.Colorize(color);
				return result;
			}

			public static void Colorize (this Texture2D tex, Color color)
			{
				Color[] pixels = tex.GetPixels();
				for (int i=0;i<pixels.Length;i++) pixels[i] = color;
				tex.SetPixels(pixels);
				tex.Apply();
			}


			public static Texture2D Clone (this Texture2D src)
			{
				Texture2D dst = new Texture2D(src.width, src.height, src.format, src.mipmapCount!=1); 
				Graphics.CopyTexture(src, dst);
				dst.Apply(updateMipmaps:false);
				return dst;
				
			}


			public static void ClearAlpha (this Texture2D tex)
			{
				Color[] colors = tex.GetPixels();
				for (int i=0; i<colors.Length; i++) 
					colors[i].a = 1;  //clear alpha is white alpha
				tex.SetPixels(colors);
				tex.Apply();
			}

			public static void ApplyGamma (this Texture2D tex, float gamma=2.2f)
			{
				//IDEA: use raw pixel data
				float invVal = 1 / gamma;
				Color[] colors = tex.GetPixels();
				for (int i=0; i<colors.Length; i++)
					colors[i] = new Color(
						Mathf.Pow(colors[i].r, invVal),
						Mathf.Pow(colors[i].g, invVal),
						Mathf.Pow(colors[i].b, invVal),
						colors[i].a);
				tex.SetPixels(colors);
				tex.Apply();
			}


			public static void RestoreNormalmap (this Texture2D tex)
			{
				Color[] colors = tex.GetPixels();
				for (int i=0; i<colors.Length; i++)
				{
					Vector2 normXY = new Vector2(colors[i].g*2 - 1, colors[i].a*2 - 1);
					float normZ = Mathf.Sqrt(1 - Mathf.Clamp01( Vector3.Dot(normXY, normXY)));
					colors[i] = new Color(
						colors[i].g,
						colors[i].a,
						normZ/2 + 0.5f,
						1);
				}
				tex.SetPixels(colors);
				tex.Apply();
			}


			public static void Multiply (this Texture2D tex, Color color, bool multiplyAlpha=false)
			{
				Color[] colors = tex.GetPixels();
				for (int i=0; i<colors.Length; i++)
				{
					colors[i].r = colors[i].r * color.r; //(r*cr)*ca + r*(1-ca),
					colors[i].g = colors[i].g * color.g;
					colors[i].b = colors[i].b * color.b;
					if (multiplyAlpha)
						colors[i].a = colors[i].a * color.a;
				}
				tex.SetPixels(colors);
				tex.Apply();
			}


			public static void SaveAsPNG (this Texture2D origTex, string savePath, bool linear=false, bool normal=false)
			{
				Texture2D tex = origTex;

				if (!tex.IsReadable()) tex = tex.ReadableClone();
				if (tex.format.IsCompressed()) tex = tex.UncompressedClone();

				if (linear) 
				{
					if (tex==origTex) tex=tex.Clone();
					tex.ApplyGamma(0.4545454545454545f);
					tex.Apply(updateMipmaps:false);
				}

				if (normal) 
				{
					if (tex==origTex) tex=tex.Clone();
					tex.RestoreNormalmap();
					tex.Apply(updateMipmaps:false);
				}

				savePath = savePath.Replace(Application.dataPath, "Assets");
				System.IO.File.WriteAllBytes(savePath, tex.EncodeToPNG());

				#if UNITY_EDITOR
				UnityEditor.AssetDatabase.Refresh();
				#endif
			}

			public static Hash128 GetHash (this Texture2D tex)
			{
				#if UNITY_EDITOR

					#if UNITY_2017_3_OR_NEWER
					return tex.imageContentsHash;
					
					#else

					UnityEditor.SerializedProperty property = new UnityEditor.SerializedObject (tex).FindProperty ("m_ImageContentsHash");

					//by Broxxar 
					//https://answers.unity.com/questions/1249181/how-to-get-texture-image-contents-hash-property.html

					if (property.type != "Hash128") {
						throw new Exception("SerializedProperty does not represent a Hash128 struct.");
					}

					var bytes = new byte[4][];

					for (var i = 0; i < 4; i++) {
						bytes[i] = new byte[4];

						for (var j = 0; j < 4; j++) {
							property.Next(true);
							bytes[i][j] = (byte)property.intValue;
						}
					}

					var hash = new Hash128(
						BitConverter.ToUInt32(bytes[0], 0),
						BitConverter.ToUInt32(bytes[1], 0),
						BitConverter.ToUInt32(bytes[2], 0),
						BitConverter.ToUInt32(bytes[3], 0));

					return hash;
					#endif

				#else
				return new Hash128();
				#endif
			}


		#region Formats and Compression

			public static readonly HashSet<TextureFormat> uncompressedFormats = new HashSet<TextureFormat>(
				new TextureFormat[] {
					TextureFormat.Alpha8, TextureFormat.ARGB32, TextureFormat.ARGB4444, TextureFormat.R16, TextureFormat.R8, TextureFormat.RFloat, 
					TextureFormat.RG16, TextureFormat.RGB24, TextureFormat.RGB565, TextureFormat.RGB9e5Float, TextureFormat.RGBA32, TextureFormat.RGBA4444,
					TextureFormat.RGBAFloat, TextureFormat.RGBAHalf, TextureFormat.RGFloat, TextureFormat.RHalf } );

			public static bool IsCompressed (this TextureFormat format)
			{
				if (uncompressedFormats.Contains(format)) return false;
				else return true;
			}

			public enum TextureType { RGBA, RGB, Normal, Monochrome, MonochromeFloat, Manual };

			public static TextureFormat AutoFormat (TextureType type, bool compressed)
			{
				if (compressed)
				{
					//list of used texture formats: https://docs.unity3d.com/Manual/class-TextureImporterOverride.html
					//using high quality settings
					#if UNITY_EDITOR
					UnityEditor.BuildTarget buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
					switch (buildTarget)
					{
						case UnityEditor.BuildTarget.StandaloneWindows64:
						case UnityEditor.BuildTarget.StandaloneWindows:
						case UnityEditor.BuildTarget.WSAPlayer:
							UnityEngine.Rendering.GraphicsDeviceType[] deviceTypes = UnityEditor.PlayerSettings.GetGraphicsAPIs(buildTarget);
							for (int i=0; i<deviceTypes.Length; i++)
								if (deviceTypes[i] == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 ||
									deviceTypes[i] == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
									{
										if (type==TextureType.Normal) return TextureFormat.BC5;
										else if (type==TextureType.Monochrome) return TextureFormat.BC4;
										else if (type==TextureType.MonochromeFloat) return TextureFormat.BC6H;
										else return TextureFormat.BC7;
									}
							return TextureFormat.DXT5;
						#if UNITY_2017_3_OR_NEWER
						case UnityEditor.BuildTarget.StandaloneOSX:
						#endif
						//case UnityEditor.BuildTarget.StandaloneLinuxUniversal: 
						case UnityEditor.BuildTarget.PS4:
						case UnityEditor.BuildTarget.XboxOne:
							if (type==TextureType.Normal) return TextureFormat.BC5;
							else if (type==TextureType.Monochrome) return TextureFormat.BC4;
							else if (type==TextureType.MonochromeFloat) return TextureFormat.BC6H;
							else return TextureFormat.BC7;
						case UnityEditor.BuildTarget.Android: 
							if (type==TextureType.RGBA) return TextureFormat.ETC2_RGBA8;
							else return TextureFormat.ETC2_RGB;
						case UnityEditor.BuildTarget.iOS: return TextureFormat.PVRTC_RGBA4;
						case UnityEditor.BuildTarget.tvOS: return TextureFormat.ASTC_4x4;
						default: return TextureFormat.DXT5;
					}
					#else
					return TextureFormat.DXT5;
					#endif
				}
				else
				{
					switch (type)
					{
						case TextureType.RGB: return TextureFormat.RGB24;
						case TextureType.Normal: return TextureFormat.RG16;
						case TextureType.Monochrome: return TextureFormat.R8;
						case TextureType.MonochromeFloat: return TextureFormat.RFloat;
						default: return TextureFormat.RGBA32;
					}
				}
			}

		#endregion
	}
}
