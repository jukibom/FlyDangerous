using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

namespace Den.Tools
{
	public static class ScriptableAssetExtensions
	{
		public static T SaveAsset<T> (this T asset, string savePath=null, string filename="Data", string type="asset", string caption="Save Data as Unity Asset") where T : UnityEngine.Object
		{
			#if UNITY_EDITOR
			if (savePath==null) savePath = UnityEditor.EditorUtility.SaveFilePanel(
				caption,
				"Assets",
				filename, 
				type);
			if (savePath!=null && savePath.Length!=0)
			{
				savePath = savePath.Replace(Application.dataPath, "Assets");

				UnityEditor.AssetDatabase.CreateAsset(asset, savePath);
				if (asset is ISerializationCallbackReceiver) ((ISerializationCallbackReceiver)asset).OnBeforeSerialize();
				UnityEditor.AssetDatabase.SaveAssets();

				return asset;
			}
			#endif

			return null;
		} 

		public static void SaveRawBytes (this byte[] bytes, string savePath=null, string filename="Data", string type="asset")
		{
			#if UNITY_EDITOR
			if (savePath==null) savePath = UnityEditor.EditorUtility.SaveFilePanel(
				"Save Data as Unity Asset",
				"Assets",
				filename, 
				type);
			if (savePath!=null && savePath.Length!=0)
			{
				savePath = savePath.Replace(Application.dataPath, "Assets");
				System.IO.File.WriteAllBytes(savePath, bytes);
			}
			#endif
		}

		public static void SaveTexture (this Texture2D tex, string savePath=null, string filename="Texutre", string type="png", string caption="Save Texture as PNG")
		{
			#if UNITY_EDITOR
			if (savePath==null) savePath = UnityEditor.EditorUtility.SaveFilePanel(
				caption, "Assets", filename, type);

			if (savePath!=null && savePath.Length!=0)
				tex.SaveAsPNG(savePath);
			#endif
		} 

		public static T ReleaseAsset<T> (this T asset, string savePath=null) where T : ScriptableObject, ISerializationCallbackReceiver
		{
			#if UNITY_EDITOR
			asset = ScriptableObject.Instantiate<T>(asset); 
			#endif

			return asset;
		}

		public static T LoadAsset<T> (string label="Load Unity Asset", string[] filters=null) where T : UnityEngine.Object
		{
			#if UNITY_EDITOR
			if (filters == null)
			{
				if (typeof(T).IsSubclassOf(typeof(Texture))) filters = new string[] { "Textures", "PSD,TIFF,TIF,JPG,TGA,PNG,GIF,BMP,IFF,PICT" };
				if (typeof(T) == typeof(Transform) || typeof(T) == typeof(Mesh)) filters = new string[] { "Meshes", "FBX,DAE,3DS,DXF,OBJ,SKP" };
				if (typeof(T) == typeof(TerrainData)) filters = new string[] { "TerrainData", "ASSET" };
			}
			ArrayTools.Add(ref filters, "All files");
			ArrayTools.Add(ref filters, "*");

			string path= UnityEditor.EditorUtility.OpenFilePanelWithFilters(label, "Assets", filters);
			if (path!=null && path.Length!=0)
			{
				path = path.Replace(Application.dataPath, "Assets");
				T asset = (T)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T));
				return asset;
			}
			#endif
			return null;
		}


		//object selector wrapper
		static Type objectSelectorType;

		public static object GetObjectSelector ()
		{
			if (objectSelectorType == null) objectSelectorType = Type.GetType("UnityEditor.ObjectSelector,UnityEditor");
			
			PropertyInfo getProperty = objectSelectorType.GetProperty("get", BindingFlags.Public | BindingFlags.Static);
			object objSelector = getProperty.GetValue(null,null);

			return objSelector;
		}

		public static int GetObjectSelectorId ()
		{
			if (objectSelectorType == null) objectSelectorType = Type.GetType("UnityEditor.ObjectSelector,UnityEditor");
			object objectSelector = GetObjectSelector();
			
			FieldInfo idField = objectSelectorType.GetField("objectSelectorID", BindingFlags.Instance | BindingFlags.NonPublic);

			return (int)idField.GetValue(objectSelector);
		}

		public static object GetObjectSelectorObject ()
		{
			if (objectSelectorType == null) objectSelectorType = Type.GetType("UnityEditor.ObjectSelector,UnityEditor");
			object objectSelector = GetObjectSelector();

			MethodInfo objectMethod =  objectSelectorType.GetMethod("GetCurrentObject", BindingFlags.Static | BindingFlags.Public);
			Debug.Log(objectMethod.Invoke(null,null));
			return objectMethod.Invoke(null,null);

			//Debug.Log(objectMethod.Invoke(objectSelector, new object[0]));

			//return objectMethod.Invoke(objectSelector, new object[0]);
		}

		public static void ShowObjectSelector (Type objType, int id=12345, bool allowSceneObjects=false, Action<UnityEngine.Object> onClosed=null, Action<UnityEngine.Object> onUpdated=null)
		{
			#if UNITY_EDITOR

			if (objectSelectorType == null) objectSelectorType = Type.GetType("UnityEditor.ObjectSelector,UnityEditor");
			object objectSelector = GetObjectSelector();

			MethodInfo showMethod = objectSelectorType.GetMethod(
				"Show", 
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
				null,
				CallingConventions.Any,
				new Type[] { typeof(Type), typeof(UnityEditor.SerializedProperty), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>) }, 
				null);
			showMethod.Invoke(objectSelector, new object[]{objType, null, allowSceneObjects, null, onClosed, onUpdated});  //(obj, objType, property, allowSceneObjects);

			FieldInfo idField = objectSelectorType.GetField("objectSelectorID", BindingFlags.Instance | BindingFlags.NonPublic);
			idField.SetValue(objectSelector,id);

			#endif
		}



		public static class ObjectSelectorWrapper
		{
			#if UNITY_EDITOR

			private static System.Type T;
			private static bool oldState = false;
			static ObjectSelectorWrapper()
			{
				T = System.Type.GetType("UnityEditor.ObjectSelector,UnityEditor");
			}
			
		/*	private static UnityEditor.EditorWindow Get()
			{
				PropertyInfo P = T.GetProperty("get", BindingFlags.Public | BindingFlags.Static);
				return P.GetValue(null,null) as UnityEditor.EditorWindow;
			}

			public static void ShowSelector(System.Type aRequiredType)
			{
				MethodInfo ShowMethod = T.GetMethod("Show",BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				ShowMethod.Invoke(Get (), new object[]{null,aRequiredType,null, true});
			}
			public static T GetSelectedObject<T>() where T : UnityEngine.Object
			{
				MethodInfo GetCurrentObjectMethod =  T.GetMethod("GetCurrentObject",BindingFlags.Static | BindingFlags.Public);
				return GetCurrentObjectMethod.Invoke(null,null) as T;
			}*/

			public static bool isVisible
			{
				get 
				{
					PropertyInfo P = T.GetProperty("isVisible", BindingFlags.Public | BindingFlags.Static);
					return (bool)P.GetValue(null,null);
				}
			}
			public static bool HasJustBeenClosed()
			{
				bool visible = isVisible;
				if (visible != oldState && visible == false)
				{
					oldState = false;
					return true;
				}
				oldState = visible;
				return false;
			}

			#endif
		}


		
	}
}
