using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public class TexturesCache
	{
		private Dictionary<string,Texture2D> textures = new Dictionary<string,Texture2D>();
		private Dictionary<string, Dictionary<uint,Texture2D>> colorTextures = new Dictionary<string, Dictionary<uint, Texture2D>>();
		private Dictionary<uint, Texture2D> blankTextures = new Dictionary<uint, Texture2D>();
		private Dictionary<Texture2D,GUIStyle> styles = new Dictionary<Texture2D,GUIStyle>();

		private Dictionary<string,Material> materials = new Dictionary<string, Material>();
		
		public bool forcePro = false;
		public bool forceLight = false;


		public Texture2D GetTexture (string textureName)
		{
			if (textures.TryGetValue(textureName, out Texture2D tex)) return tex;
			else
			{
				tex = LoadTextureAtPath(textureName); 
				textures.Add(textureName, tex);
				return tex;
			}
		}

		public Texture2D GetTexture (string texturePath, string textureName, bool forceLight=false, bool forcePro=false)
		{
			if (textures.TryGetValue(textureName, out Texture2D tex)) return tex;
			else
			{
				tex = LoadTextureAtPath(texturePath, forceLight:forceLight, forcePro:forcePro); 
				textures.Add(textureName, tex);
				return tex;
			}
		}

		private uint ColorHash (Color color)
		/// Using manual color hash since color in dictionary (compare) creates garbage for some reason
		{
			uint r = (uint)(color.r*255);
			uint g = (uint)(color.g*255);
			uint b = (uint)(color.b*255);
			uint a = (uint)(color.a*255);

			return r<<24 | g<<16 | b<<8 | a;
		}

		public Texture2D GetColorizedTexture (string textureName, Color color)
		{
			if (!colorTextures.TryGetValue(textureName, out var dict))
			{
				dict = new Dictionary<uint, Texture2D>();
				colorTextures.Add(textureName, dict);
			}

			uint colorHash = ColorHash(color);

			if (dict.ContainsKey(colorHash)  &&  !(dict[colorHash]==null || dict[colorHash].Equals(null)))  //texture is removed on scene change
				return dict[colorHash];
		
			//else
			{
				Texture2D source = GetTexture(textureName);
				Texture2D clone = source.Clone();
				clone.Multiply(color, multiplyAlpha:false);

				if (!dict.ContainsKey(colorHash)) dict.Add(colorHash, clone);
				else dict[colorHash] = clone;

				return clone;
			}

			
		}


		public Texture2D GetBlankTexture (float color) 
			{ return GetBlankTexture( new Color(color, color, color)); }


		public Texture2D GetBlankTexture (Color color) 
		{
			uint colorHash = ColorHash(color);

			blankTextures.TryGetValue(colorHash, out Texture2D tex); //if(TryGet) //texture could be removed, tryGet will return true and out aka "null"
			if (tex != null) return tex;
			else
			{
				tex = new Texture2D(4,4);
				Color[] colors = tex.GetPixels();
				for (int i=0; i<colors.Length; i++) colors[i] = color; //new Color(0.0f, 0.0f, 0.0f, 1);
				tex.SetPixels(colors);
				tex.Apply(true, true);

				if (!blankTextures.ContainsKey(colorHash)) blankTextures.Add(colorHash, tex);
				else blankTextures[colorHash] = tex;
				return tex;
			}
		}


		public static Texture2D LoadTextureAtPath (string textureName, bool forceLight=false, bool forcePro=false)
		/// Loads texture dealing with pro name and folders
		{
			Texture2D texture;

			bool isPro = StylesCache.isPro;
			if (forceLight) isPro = false;
			if (forcePro) isPro = true;

			if (!isPro)
			{
				texture = Resources.Load(textureName) as Texture2D;
				//if (texture == null) texture = Resources.Load(proName) as Texture2D;
				//if (texture == null) texture = Resources.Load(proFolderName) as Texture2D;
			}
			else
			{
				string proName = textureName + "_pro";
			
				int sepIndex = textureName.LastIndexOf('/'); if (sepIndex < 0) sepIndex = 0;
				string folderName = textureName.Substring(0,sepIndex);
				string fileName = textureName.Substring(sepIndex+1);
				string proFolderName = folderName + "/pro/" + fileName;

				texture = Resources.Load(proName) as Texture2D;
				if (texture == null) texture = Resources.Load(proFolderName) as Texture2D;
				if (texture == null) texture = Resources.Load(textureName) as Texture2D;
			}

			#if UNITY_EDITOR
			if (texture == null && !UnityEditor.BuildPipeline.isBuildingPlayer) //hack since there are no resources during build
				throw new Exception("Could not find texture " + textureName); 
			#else
			if (texture == null)
				throw new Exception("Could not find texture " + textureName); 
			#endif
	   
			return texture;
		}


		public GUIStyle GetElementStyle (string textureName, RectOffset borders=null, RectOffset overflow=null)
		{
			Texture2D tex = GetTexture(textureName); 
			return GetElementStyle(tex, null, borders, overflow);
		}

		public GUIStyle GetElementStyle (string textureName, string onTextureName, RectOffset borders=null, RectOffset overflow=null)
		{
			Texture2D tex = GetTexture(textureName); 
			Texture2D onTex = GetTexture(onTextureName);
			return GetElementStyle(tex, onTex, borders, overflow);
		}

		public GUIStyle GetElementStyle (Texture2D tex, Texture2D onTex=null, RectOffset borders=null, RectOffset overflow=null)
		{
			if (styles.TryGetValue(tex, out GUIStyle style)) return style;
			else
			{
				style = new GUIStyle();
				style.normal.background = tex;
				style.active.background = style.onActive.background = style.onNormal.background = style.onFocused.background = style.focused.background = onTex ?? tex;

				if (borders == null) style.border = new RectOffset(tex.width/2, tex.width/2, tex.height/2, tex.height/2);
				else style.border = borders;
				//if (borders != null) style.border = borders;

				if (overflow != null) style.overflow = overflow;

				styles.Add(tex, style);
				return style;
			}
		}

		public Material GetMaterial (string shaderName)
		{
			if (materials.ContainsKey(shaderName))
			{
				Material mat = materials[shaderName];
				if (mat==null) //destroys material on scene close
				{
					mat = new Material( Shader.Find(shaderName) ); 
					materials[shaderName] = mat;
				}
				return mat;
			}
			
			else
			{
				Material mat = new Material( Shader.Find(shaderName) ); 
				materials.Add(shaderName,mat);
				return mat;
			}
		}

		public void SetMaterial (Material mat)
        {
			string shaderName = mat.shader.name;
			if (materials.ContainsKey(shaderName)) materials[shaderName] = mat;
			else materials.Add(shaderName, mat);
		}

	}
}