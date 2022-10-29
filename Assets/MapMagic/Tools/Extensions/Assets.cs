using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; //to copy properties
using UnityEngine.SceneManagement;

namespace Den.Tools
{
	static public class AssetsExtensions
	{
		public static string GUID (this UnityEngine.Object obj)
		{
			#if UNITY_EDITOR
			string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
			if (path==null || path.Length==0) return "";
			string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
			if (guid==null || guid.Length==0) return ""; //should not return null
			return guid;
			#else
			Debug.LogError("GUID does not work in build");
			return "";
			#endif
		}

		#if UNITY_EDITOR
		public static UnityEditor.AssetImporter GetImporter (this UnityEngine.Object obj)
		{
			string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
			if (path==null || path.Length==0) return null;
			return UnityEditor.AssetImporter.GetAtPath(path);
		}
		#endif

		#if UNITY_EDITOR
		public static UnityEditor.AssetImporter GetImporter (string guid)
		{
			
			string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
			if (path==null || path.Length==0) return null;
			return UnityEditor.AssetImporter.GetAtPath(path);
			
		}
		#endif

		public static T GUIDtoObj<T> (this string guid) where T: UnityEngine.Object
		{
			#if UNITY_EDITOR
			if (guid==null || guid.Length==0) return null;
			string sourcePath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
			if (sourcePath.Length==0) return null;
			return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(sourcePath);
			#else
			Debug.LogError("GUIDtObj does not work in build");
			return null;
			#endif	
		}


		public static string[] GetUserData (this UnityEngine.Object obj, string param) 
		{
			#if UNITY_EDITOR
			UnityEditor.AssetImporter importer = obj.GetImporter();
			if (importer == null) return null;
			return GetUserData(importer, param);
			#else
			Debug.LogError("GetUserData does not work in build");
			return null;
			#endif	
		}

		public static string[] GetUserData (string guid, string param) 
		{
			#if UNITY_EDITOR
			UnityEditor.AssetImporter importer = GetImporter(guid);
			if (importer == null) return null;
			return GetUserData(importer, param);
			#else
			Debug.LogError("GetUserData does not work in build");
			return null;
			#endif	
		}

		#if UNITY_EDITOR
		public static string[] GetUserData (this UnityEditor.AssetImporter importer, string param)
		{
			string userData = importer.userData;
			if (userData==null) return null;
			if (userData.Length==0) return new string[0];

			string[] userDataSplit = userData.Split('\n', ';');
			for (int i=0; i<userDataSplit.Length; i++)
			{
				if (userDataSplit[i].StartsWith(param + ":"))
				{
					userDataSplit[i] = userDataSplit[i].Remove(0, param.Length+1);
					return userDataSplit[i].Split(',');
				}
			}

			return new string[0];
		}
		#endif

		public static void SetUserData (this UnityEngine.Object obj, string param, string[] data, bool reload = false)
		{
			#if UNITY_EDITOR
			UnityEditor.AssetImporter importer = obj.GetImporter();
			if (importer == null) return;
			SetUserData(importer, param, data, reload);
			#else
			Debug.LogError("SetUserData does not work in build");
			#endif
		}

		public static void SetUserData (string guid, string param, string[] data, bool reload = false)
		{
			#if UNITY_EDITOR
			UnityEditor.AssetImporter importer = GetImporter(guid);
			if (importer == null) return;
			SetUserData(importer, param, data, reload);
			#else
			Debug.LogError("SetUserData does not work in build");
			#endif
		}

		#if UNITY_EDITOR
		public static void SetUserData (this UnityEditor.AssetImporter importer, string param, string[] data, bool reload=false)
		{
			char endline = '\n'; //';'
			
			string userData = importer.userData;
			string[] userDataSplit = userData.Split('\n', ';');

			//preparing new data line
			if (data == null) data = new string[0];
			string newDataString = param + ":" + data.ToStringMemberwise(separator:",");

			//param line number (-1 if not found)
			int numInSplit = -1;
			for (int i=0; i<userDataSplit.Length; i++)
				if (userDataSplit[i].StartsWith(param + ":"))
					numInSplit = i;
			
			//erasing empty data
			if (numInSplit >= 0 && data.Length == 0)
				ArrayTools.RemoveAt(ref userDataSplit, numInSplit);

			//replacing line
			if (numInSplit >= 0 && data.Length != 0)
				userDataSplit[numInSplit] = newDataString;

			//adding new line
			if (numInSplit == -1 && data.Length != 0)
				ArrayTools.Add(ref userDataSplit, newDataString);

			//to string
			string newUserData = "";
			for (int i=0; i<userDataSplit.Length; i++)
			{
				if (userDataSplit[i].Length == 0) continue;
				newUserData += userDataSplit[i];
				if (i!=userDataSplit.Length-1) newUserData += endline;
			}

			//writing
			if (newUserData != userData)
			{
				importer.userData = newUserData;

				UnityEditor.EditorUtility.SetDirty(importer);
				UnityEditor.AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
				if (reload) UnityEditor.AssetDatabase.Refresh();
			}
		}
		#endif


		public static void Reimport (this UnityEngine.Object obj)
		{
			#if UNITY_EDITOR
			string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
			if (path==null || path.Length==0) return;
			UnityEditor.AssetImporter importer = UnityEditor.AssetImporter.GetAtPath(path);
			importer.userData = importer.userData;
			UnityEditor.EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();
			#else
			Debug.LogError("Reimport does not work in build");
			#endif
		}


		public static int GetDirtyId (this Scene scene)
		{
			//Scene scene = SceneManager.GetActiveScene();
			if (dirtyIdProp == null)
				dirtyIdProp = typeof(Scene).GetProperty("dirtyID", BindingFlags.Instance | BindingFlags.NonPublic);
			return (int)dirtyIdProp.GetValue(scene, new object[0]);
		}
		private static PropertyInfo dirtyIdProp;
		
		public static int GetDirtyId ()
		/// Gets summary dirty id of all scenes
		{
			int dirtySum = 0;
			int scenesCount = SceneManager.sceneCount;
			for (int s=0; s<scenesCount; s++)
			{
				Scene scene = SceneManager.GetSceneAt(s);
				dirtySum += scene.GetDirtyId();
			}
			return dirtySum;
		}


		
		public static Texture2D GetNormalTexture (this Texture2D diffuse)
		/// Tries to load texture with the postfix _n or _normal and same name as this texture
		{
			#if UNITY_EDITOR
			string path = UnityEditor.AssetDatabase.GetAssetPath(diffuse);
			if (path == null)
				return null;

			//megasplat
			if (path.Contains("_diff."))
				path = path.Replace("_diff.", "_norm.");

			//microsplat
			else if (path.Contains("_albedo."))
				path = path.Replace("_albedo.", "_norm.");

			//cts
			else if (path.Contains("_A_Sm."))
				path = path.Replace("_A_Sm.", "_N.");

			else if (path.Contains("_A_Sm_Dry."))
				path = path.Replace("_A_Sm_Dry.", "_N.");

			//mapmagic
			else path = path.Replace(".", "_nrm.");

			return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			#else
			return null;
			#endif
		}


		public static Texture GetMainTexture (this GameObject gameObject) =>
			gameObject.GetComponent<Renderer>()?.material?.mainTexture;


		public static bool IsAsset (this GameObject obj)
		{
			#if UNITY_EDITOR
			return UnityEditor.AssetDatabase.Contains(obj);
			#else
			return false;
			#endif
		}

		public static bool IsNull (this UnityEngine.Object unityObj)
		/// Can check if unity obj is null in thread
		/// Just checking unityObj == null will throw an error if object was removed (available in main thread) 
		{
			//return (UnityEngine.Object)unityObj != (UnityEngine.Object)null;
			try { return unityObj == null; }
			catch (Exception) { return false; }
		}
	}
}