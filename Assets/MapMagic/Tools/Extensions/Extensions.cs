using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; //to copy properties


namespace Den.Tools
{
	static public class Extensions
	{


		public static void RemoveChildren (this Transform tfm)
		{
			for (int i=tfm.childCount-1; i>=0; i--)
			{
				Transform child = tfm.GetChild(i);
				
				#if UNITY_EDITOR
				if (!UnityEditor.EditorApplication.isPlaying)
					GameObject.DestroyImmediate(child.gameObject);
				else
				#endif
					GameObject.Destroy(child.gameObject);
			}
		}

		public static Transform FindChildRecursive (this Transform tfm, string name)
		{
			int numChildren = tfm.childCount;

			for (int i=0; i<numChildren; i++)
				if (tfm.GetChild(i).name == name) return tfm.GetChild(i);

			for (int i=0; i<numChildren; i++)
			{
				Transform result = tfm.GetChild(i).FindChildRecursive(name);
				if (result != null) return result;
			}

			return null;
		}





		public static void ToggleDisplayWireframe (this Transform tfm, bool show)
		{
			#if UNITY_EDITOR
			#if !UNITY_5_5_OR_NEWER
				UnityEditor.EditorUtility.SetSelectedWireframeHidden(tfm.GetComponent<Renderer>(), !show);
				int childCount = tfm.childCount;
				for (int c=0; c<childCount; c++) tfm.GetChild(c).ToggleDisplayWireframe(show);
			#else
				UnityEditor.EditorUtility.SetSelectedRenderState(tfm.GetComponent<Renderer>(), show? UnityEditor.EditorSelectedRenderState.Highlight : UnityEditor.EditorSelectedRenderState.Hidden);
				int childCount = tfm.childCount;
				for (int c=0; c<childCount; c++) tfm.GetChild(c).ToggleDisplayWireframe(show);
			#endif
			#endif
		}

		public static int ToInt (this Coord coord)
		{
			int absX = coord.x<0? -coord.x : coord.x; 
			int absZ = coord.z<0? -coord.z : coord.z;

			return ((coord.z<0? 1000000000 : 0) + absX*30000 + absZ) * (coord.x<0? -1 : 1);
		}

		public static Coord ToCoord (this int hash)
		{
			int absHash = hash<0? -hash : hash;
			int sign = (absHash/1000000000)*1000000000;

			int absX = (absHash - sign)/30000;
			int absZ = absHash - sign - absX*30000;

			return new Coord(hash<0? -absX : absX, sign==0? absZ : -absZ);
		}

		public static TValue[] ToArray<TKey,TValue> (this Dictionary<TKey,TValue>.ValueCollection values)
		{
			TValue[] arr = new TValue[values.Count];
			values.CopyTo(arr, 0);
			return arr;
		}

		public static TKey[] ToArray<TKey,TValue> (this Dictionary<TKey,TValue>.KeyCollection keys)
		{
			TKey[] arr = new TKey[keys.Count];
			keys.CopyTo(arr, 0);
			return arr;
		}

		public static T[] ToArray<T> (this ICollection<T> col)
		{
			T[] arr = new T[col.Count];
			col.CopyTo(arr, 0);
			return arr;
		}

		public static T[] ToArray<T> (this HashSet<T> vals)
		{
			T[] arr = new T[vals.Count];
			vals.CopyTo(arr, 0);
			return arr;
		}

		public static void AddRange<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey[] keys, TValue[] values)
		{
			for (int i=0; i<keys.Length; i++)
				dict.Add(keys[i], values[i]);
		}

		public static void TryAdd<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key, TValue value) { if (!dict.ContainsKey(key)) dict.Add(key, value); }

		public static void ForceAdd<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key, TValue value)
		{
			if (dict.ContainsKey(key)) 
				dict[key] = value;
			else dict.Add(key, value);
		}

		public static void TryRemove<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key) { if (dict.ContainsKey(key)) dict.Remove(key); }
		
		public static void RemoveWhere<TKey,TValue> (this Dictionary<TKey,TValue> dict, Predicate<TKey> predicate)
		{
			List<TKey> keysToRemove = null;

			foreach (TKey key in dict.Keys)
				if (predicate(key))
				{
					if (keysToRemove == null) keysToRemove = new List<TKey>(); //create list only if there's something to remove
					keysToRemove.Add(key);
				}

			if (keysToRemove != null)
				foreach (TKey key in keysToRemove)
					dict.Remove(key);
		}

		public static TValue CheckGet<TKey,TValue> (this Dictionary<TKey,TValue> dict, TKey key)
		{
			if (dict.ContainsKey(key)) return dict[key];
			else return default(TValue);
		}
		public static TKey AnyKey<TKey,TValue> (this Dictionary<TKey,TValue> dict)
		{
			foreach (KeyValuePair<TKey,TValue> kvp in dict)
				return kvp.Key;
			return default(TKey);
		}
		public static TValue AnyValue<TKey,TValue> (this Dictionary<TKey,TValue> dict)
		{
			foreach (KeyValuePair<TKey,TValue> kvp in dict)
				return kvp.Value;
			return default(TValue);
		}
		public static T Any<T> (this HashSet<T> hashSet)
		{
			foreach (T val in hashSet)
				return val;
			return default(T);
		}

		public static void RemoveAfter<T> (this List<T> list, int num)
		{
			list.RemoveRange(num+1, list.Count-num-1);
		}

		public static T ElementOfType<T> (this IEnumerable arr) where T : class
		{
			foreach (object mem in arr)
				if (mem is T objMem)
					return objMem;
			return default;
		}

		public static bool MinDist<T1,T2> (this Dictionary<T1,T2> dict, Func<T1,float> distEvaluator, out float minDist, out T2 minValue)
		{
			bool minFound = false;
			minDist = int.MaxValue;
			minValue = default;
			foreach (var kvp in dict)
			{
				float dist = distEvaluator(kvp.Key);
				if (dist < minDist) 
				{
					minDist = dist;
					minFound = true;
					minValue = kvp.Value;
				}
			}
			return minFound;
		}

		public static void AddRange<T> (this HashSet<T> set, T[] objs) { for (int i=0; i<objs.Length; i++) set.Add(objs[i]); }
		public static void CheckAdd<T> (this HashSet<T> set, T obj) { if (!set.Contains(obj)) set.Add(obj); }
		public static void CheckRemove<T> (this HashSet<T> set, T obj) { if (set.Contains(obj)) set.Remove(obj); }
		public static void SetState<T> (this HashSet<T> set, T obj, bool state)
		{
			if (state && !set.Contains(obj)) set.Add(obj);
			if (!state && set.Contains(obj)) set.Remove(obj);
		}

		public static void Normalize (this float[,,] array, int pinnedLayer)
		{
			int maxX = array.GetLength(0); int maxZ = array.GetLength(1); int numLayers = array.GetLength(2);
			for (int x=0; x<maxX; x++)
				for (int z=0; z<maxZ; z++)
			{
				float othersSum = 0;

				for (int i=0; i<numLayers; i++)
				{
					if (i==pinnedLayer) continue;
					othersSum += array[x,z,i];
				}

				float pinnedValue = array[x,z,pinnedLayer];
				if (pinnedValue > 1) { pinnedValue = 1; array[x,z,pinnedLayer] = 1; }
				if (pinnedValue < 0) { pinnedValue = 0; array[x,z,pinnedLayer] = 0; }

				float othersTargetSum = 1 - pinnedValue;
				float factor = othersSum>0? othersTargetSum / othersSum : 0;

				for (int i=0; i<numLayers; i++)
				{
					if (i==pinnedLayer) continue;
					 array[x,z,i] *= factor;
				}
			}

		}

		public static void DrawDebug (this Vector3 pos, float range=1, Color color=new Color())
		{
			if (color.a<0.001f) color = Color.white;
			Debug.DrawLine(pos + new Vector3(-1,0,1)*range, pos + new Vector3(1,0,1)*range, color);
			Debug.DrawLine(pos + new Vector3(1,0,1)*range, pos + new Vector3(1,0,-1)*range, color);
			Debug.DrawLine(pos + new Vector3(1,0,-1)*range, pos + new Vector3(-1,0,-1)*range, color);
			Debug.DrawLine(pos + new Vector3(-1,0,-1)*range, pos + new Vector3(-1,0,1)*range, color);
		}

		public static void DrawDebug (this Rect rect, Color color=new Color())
		{
			if (color.a<0.001f) color = Color.white;
			Debug.DrawLine(	new Vector3(rect.x,0,rect.y),							new Vector3(rect.x+rect.width,0,rect.y),				color);
			Debug.DrawLine(	new Vector3(rect.x+rect.width,0,rect.y),				new Vector3(rect.x+rect.width,0,rect.y+rect.height),	color);
			Debug.DrawLine(	new Vector3(rect.x+rect.width,0,rect.y+rect.height),	new Vector3(rect.x,0,rect.y+rect.height),				color);
			Debug.DrawLine(	new Vector3(rect.x,0,rect.y+rect.height),				new Vector3(rect.x,0,rect.y),							color);
		}


		public static Transform AddChild (this Transform tfm, string name="", Vector3 offset=new Vector3())
		{
			GameObject go = new GameObject();
			go.name = name;
			go.transform.parent = tfm;
			go.transform.localPosition = offset;

			return go.transform;
		}

		public static T CreateObjectWithComponent<T> (string name="", Transform parent=null, Vector3 offset=new Vector3()) where T : MonoBehaviour
		{
			GameObject go = new GameObject();
			if (name != null) 
			if (parent != null) go.transform.parent = parent.transform;
			go.transform.localPosition = offset;
			
			return go.AddComponent<T>();
		}



		public static float EvaluateMultithreaded (this AnimationCurve curve, float time)
		{
			int keyCount = curve.keys.Length;
			
			if (time <= curve.keys[0].time) return curve.keys[0].value;
			if (time >= curve.keys[keyCount-1].time) return curve.keys[keyCount-1].value; 

			int keyNum = 0;
			for (int k=0; k<keyCount-1; k++)
			{
				if (curve.keys[keyNum+1].time > time) break;
				keyNum++;
			}
			
			float delta = curve.keys[keyNum+1].time - curve.keys[keyNum].time;
			float relativeTime = (time - curve.keys[keyNum].time) / delta;

			float timeSq = relativeTime * relativeTime;
			float timeCu = timeSq * relativeTime;
     
			float a = 2*timeCu - 3*timeSq + 1;
			float b = timeCu - 2*timeSq + relativeTime;
			float c = timeCu - timeSq;
			float d = -2*timeCu + 3*timeSq;

			return a*curve.keys[keyNum].value + b*curve.keys[keyNum].outTangent*delta + c*curve.keys[keyNum+1].inTangent*delta + d*curve.keys[keyNum+1].value;
		}

		public static bool IdenticalTo (this AnimationCurve c1, AnimationCurve c2)
		{
			if (c1==null || c2==null) return false;
			if (c1.keys.Length != c2.keys.Length) return false;
			
			int numKeys = c1.keys.Length;
			for (int k=0; k<numKeys; k++)
			{
				if (c1.keys[k].time != c2.keys[k].time ||
					c1.keys[k].value != c2.keys[k].value ||
					c1.keys[k].inTangent != c2.keys[k].inTangent ||
					c1.keys[k].outTangent != c2.keys[k].outTangent)
						return false;
			}

			return true;
		}

		public static Keyframe[] Copy (this Keyframe[] src)
		{
			Keyframe[] dst = new Keyframe[src.Length];
			for (int k=0; k<src.Length; k++) 
			{
				dst[k].value = src[k].value;
				dst[k].time = src[k].time;
				dst[k].inTangent = src[k].inTangent;
				dst[k].outTangent = src[k].outTangent;
			}
			return dst;
		}

		public static AnimationCurve Copy (this AnimationCurve src)
		{
			AnimationCurve dst = new AnimationCurve();
			dst.keys = src.keys.Copy();
			return dst;
		}





		public static object Parse (this string s, Type t)
		{
			//better than creating xml serializer each time. Reverse to "ToString" function

			if (s.Contains("=")) s = s.Remove(0, s.IndexOf('=')+1); //removing everything before =

			object r = null;
			if (t == typeof(float)) r = float.Parse(s);
			else if (t == typeof(int)) r = int.Parse(s);
			else if (t == typeof(bool)) r = bool.Parse(s);
			else if (t == typeof(string)) r = s;
			else if (t == typeof(byte)) r = byte.Parse(s);
			else if (t == typeof(short)) r = short.Parse(s);
			else if (t == typeof(long)) r = long.Parse(s);
			else if (t == typeof(double)) r = double.Parse(s);
			else if (t == typeof(char)) r = char.Parse(s);
			else if (t == typeof(decimal)) r = decimal.Parse(s);
			else if (t == typeof(sbyte)) r = sbyte.Parse(s);
			else if (t == typeof(uint)) r = uint.Parse(s);
			else if (t == typeof(ulong)) r = ulong.Parse(s);
			else return null;

			return r;
		}


		public static bool isPlaying
		{get{
			#if UNITY_EDITOR
				return UnityEditor.EditorApplication.isPlaying; //if not playing
			#else
				return true;
			#endif
		}}

		public static bool IsEditor ()
		{
			#if UNITY_EDITOR
				return 
					!UnityEditor.EditorApplication.isPlaying; //if not playing
					//(UnityEditor.EditorWindow.focusedWindow != null && UnityEditor.EditorWindow.focusedWindow.GetType() == System.Type.GetType("UnityEditor.GameView,UnityEditor")) //if game view is focused
					//UnityEditor.SceneView.lastActiveSceneView == UnityEditor.EditorWindow.focusedWindow; //if scene view is focused
			#else
				return false;
			#endif
		}

		public static bool IsSelected (Transform transform)
		{
			#if UNITY_EDITOR
				return UnityEditor.Selection.activeTransform == transform;
			#else
				return false;
			#endif
		}

		public static Camera GetMainCamera ()
		{
			if (IsEditor()) 
			{
				#if UNITY_EDITOR
				if (UnityEditor.SceneView.lastActiveSceneView==null) return null;
				else return UnityEditor.SceneView.lastActiveSceneView.camera;
				#else
				return null;
				#endif
			}
			else
			{
				Camera mainCam = Camera.main;
				if (mainCam==null) mainCam = GameObject.FindObjectOfType<Camera>(); //in case it was destroyed or something
				return mainCam;
			}
		}

		public static Vector3[] GetCamPoses (bool genAroundMainCam=true, string genAroundTag=null, Vector3[] camPoses=null)
		{
			if (IsEditor()) 
			{
				#if UNITY_EDITOR
				if (UnityEditor.SceneView.lastActiveSceneView==null || UnityEditor.SceneView.lastActiveSceneView.camera==null) return new Vector3[0]; //this happens right after script compile
				if (camPoses==null || camPoses.Length!=1) camPoses = new Vector3[1];
				camPoses[0] = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
				#else
				camPoses = new Vector3[1];
				#endif
			}
			else
			{
				//finding objects with tag
				GameObject[] taggedObjects = null;
				if (genAroundTag!=null && genAroundTag.Length!=0) taggedObjects = GameObject.FindGameObjectsWithTag(genAroundTag);

				//calculating cams array length and rescaling it
				int camPosesLength = 0;
				if (genAroundMainCam) camPosesLength++;
				if (taggedObjects !=null) camPosesLength += taggedObjects.Length;
				
				if (camPosesLength == 0) { Debug.LogError("No Main Camera to deploy"); return new Vector3[0]; }
				if (camPoses == null || camPosesLength != camPoses.Length) camPoses = new Vector3[camPosesLength];
				
				//filling cams array
				int counter = 0;
				if (genAroundMainCam) 
				{
					Camera mainCam = Camera.main;
					if (mainCam==null) mainCam = GameObject.FindObjectOfType<Camera>(); //in case it was destroyed or something
					camPoses[0] = mainCam.transform.position;
					counter++;
				}
				if (taggedObjects != null)
					for (int i=0; i<taggedObjects.Length; i++) camPoses[i+counter] = taggedObjects[i].transform.position;
			}

			return camPoses;		
		}

		public static Vector2 GetMousePosition ()
		{
			if (IsEditor()) 
			{
				#if UNITY_EDITOR
				UnityEditor.SceneView sceneview = UnityEditor.SceneView.lastActiveSceneView;
				if (sceneview==null || sceneview.camera==null || Event.current==null) return Vector2.zero;
				Vector2 mousePos = Event.current.mousePosition;
				mousePos = new Vector2(mousePos.x/sceneview.camera.pixelWidth, mousePos.y/sceneview.camera.pixelHeight);
				#if UNITY_5_4_OR_NEWER 	
				mousePos *= UnityEditor.EditorGUIUtility.pixelsPerPoint;
				#endif
				mousePos.y = 1 - mousePos.y;
				return mousePos;
				#else
				return Input.mousePosition;
				#endif
			}
			else return Input.mousePosition;
		}

		public static void GizmosDrawFrame (Vector3 center, Vector3 size, int resolution, float level = 30)
		{
			Vector3 offset = center-size/2;
			
			Vector3 prevP1=Vector3.zero; Vector3 prevP2=Vector3.zero;
			for (float x=0; x < size.x+0.0001f; x += 1f*size.x/resolution)
			{
				RaycastHit hit = new RaycastHit();

				Vector3 p1 = new Vector3(offset.x+x, 10000, offset.z);
				if (Physics.Raycast(new Ray(p1, Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y; 
				else if (Physics.Raycast(new Ray(p1+new Vector3(1,0,0), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p1+new Vector3(-1,0,0), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p1+new Vector3(0,0,1), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p1+new Vector3(0,0,-1), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else p1.y = level;
				if (x>0.0001f) Gizmos.DrawLine(prevP1, p1);
				prevP1 = p1;

				Vector3 p2 = new Vector3(offset.x+x, 10000, offset.z+size.z);
				if (Physics.Raycast(new Ray(p2, Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(1,0,0), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(-1,0,0), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(0,0,1), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(0,0,-1), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else p2.y = level;
				if (x>0.0001f) Gizmos.DrawLine(prevP2, p2);
				prevP2 = p2;
			}

			for (float z=0; z < size.z+0.0001f; z += 1f*size.z/resolution)
			{
				RaycastHit hit = new RaycastHit();

				Vector3 p1 = new Vector3(offset.x, 10000, offset.z+z);
				if (Physics.Raycast(new Ray(p1, Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y; 
				else if (Physics.Raycast(new Ray(p1+new Vector3(1,0,0), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p1+new Vector3(-1,0,0), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p1+new Vector3(0,0,1), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p1+new Vector3(0,0,-1), Vector3.down*20000), out hit, 20000)) p1.y = hit.point.y;
				else p1.y = level;
				if (z>0.0001f) Gizmos.DrawLine(prevP1, p1);
				prevP1 = p1;

				Vector3 p2 = new Vector3(offset.x+size.x, 10000, offset.z+z);
				if (Physics.Raycast(new Ray(p2, Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(1,0,0), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(-1,0,0), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(0,0,1), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else if (Physics.Raycast(new Ray(p2+new Vector3(0,0,-1), Vector3.down*20000), out hit, 20000)) p2.y = hit.point.y;
				else p2.y = level;
				if (z>0.0001f) Gizmos.DrawLine(prevP2, p2);
				prevP2 = p2;
			}
		}

		public static void Planar (this Mesh mesh, float size, int resolution)
		{
			float step = size / resolution;

			Vector3[] verts = new Vector3[(resolution+1)*(resolution+1)];
			Vector2[] uvs = new Vector2[verts.Length];
			int[] tris = new int[resolution*resolution*2*3];

			int vertCounter = 0;
			int triCounter = 0;
			for (float x=0; x<size+0.001f; x+=step) //including max
				for (float z=0; z<size+0.001f; z+=step)
			{
				verts[vertCounter] = new Vector3(x,0,z);
				uvs[vertCounter] = new Vector2(x/size, z/size);

				if (x>0.001f && z>0.001f)
				{
					tris[triCounter] = vertCounter-(resolution+1);		tris[triCounter+1] = vertCounter-1;					tris[triCounter+2] = vertCounter-resolution-2;
					tris[triCounter+3] = vertCounter-1;					tris[triCounter+4] = vertCounter-(resolution+1);	tris[triCounter+5] = vertCounter;
					triCounter += 6;
				}

				vertCounter++;
			}

			mesh.Clear();
			mesh.vertices = verts;
			mesh.uv = uvs;
			mesh.triangles = tris;
		}

		/* //use layout instead
		public static T Save<T> (this T data, string label="Save Data as Unity Asset", string fileName="Data.asset", UnityEngine.Object undoObj=null, string undoName="Save Data") where T : ScriptableObject, ICloneable
		{
			#if UNITY_EDITOR
			//finding path
			string path= UnityEditor.EditorUtility.SaveFilePanel(label, "Assets", fileName, "asset");
			if (path==null || path.Length==0) return data;

			//releasing data on re-save
			T newData = data;
			if (UnityEditor.AssetDatabase.Contains(data)) newData = (T)data.Clone();

			//saving
			path = path.Replace(Application.dataPath, "Assets");
			if (undoObj != null) UnityEditor.Undo.RecordObject(undoObj, undoName); //TODO: undo is actually not recordered because the new data is not assigned
			//undoObj.setDirty = !undoObj.setDirty;
			UnityEditor.AssetDatabase.CreateAsset(newData, path);
			if (undoObj != null) UnityEditor.EditorUtility.SetDirty(undoObj);

			return newData;
			#else
			return data;
			#endif
		}

		public static T LoadAsset<T> (string label="Load Unity Asset", string[] filters=null) where T : UnityEngine.Object
		{
			#if UNITY_EDITOR
			if (filters==null && typeof(T).IsSubclassOf(typeof(Texture))) filters = new string[] { "Textures", "PSD,TIFF,TIF,JPG,TGA,PNG,GIF,BMP,IFF,PICT" };
			ArrayTools.Add(ref filters, "All files");
			ArrayTools.Add(ref filters, "*");

			string path= UnityEditor.EditorUtility.OpenFilePanelWithFilters(label, "Assets", filters);
			if (path!=null && path.Length!=0)
			{
				path = path.Replace(Application.dataPath, "Assets");
				T asset = (T)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T));
				return asset;
			}
			return null;
			#endif
		}*/

		/*public static object Invoke (object target, string methodName, params object[] paramArray)
		{
			Type type = target.GetType();
			MethodInfo methodInfo = type.GetMethod(methodName);
			return methodInfo.Invoke(target, paramArray);
		}

		public static object StaticInvoke (string className, string methodName, params object[] paramArray)
		{
			Type type = Type.GetType(className);
			MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			return methodInfo.Invoke(null, new object[] { paramArray });
		}*/

		public static string LogBinary (this int src)
		{
			string result = "";
			for (int i=0; i<32; i++)
			{
				if (i%4==0) result=" "+result;
				result = (src & 0x1) + result;
				

				src = src >> 1;
			}
			return result;
		}

		public static string ToStringArray<T> (this T[] array)
		{
			string result = "";
			for (int i=0; i<array.Length; i++)
			{
				result += array[i].ToString();
				if (i!=array.Length-1) result += ",";
			}
			return result;
		}

		public static Color[] ToColors (this Vector4[] src)
		{
			Color[] dst = new Color[src.Length];
			for (int i=0; i<src.Length; i++)
				dst[i] = src[i];
			return dst;
		}

		public static Texture2D GetBiggestTexture (this Texture2D[] textures)
		/// Finds the biggest texture in array. Useful to create TextureArrays
		{
			int maxResolution = 0;
			int maxNum = -1;

			for (int i=0; i<textures.Length; i++)
			{
				if (textures[i]==null) continue;

				if (textures[i].width > maxResolution) { maxResolution = textures[i].width; maxNum = i; }
				if (textures[i].height > maxResolution) { maxResolution = textures[i].height; maxNum = i; }
			}

			if (maxNum >=0) return (textures[maxNum]);
			else return null;
		}

		/*public static string ToStringMemberwise<T> (this List<T> list)
		/// prints list as one, two, three
		{
			string result = "";
			for (int i=0; i<list.Count; i++)
			{
				result += list[i];
				if (i!=list.Count-1) result += ", ";
			}
			return result;
		}*/

		public static string ToStringMemberwise<T> (this IEnumerable list, Func<T,string> toStringFn=null)
		/// prints list as one, two, three
		{
			string result = "";
			foreach (T obj in list)
			{
				if (toStringFn==null) result += obj.ToString();
				else result += toStringFn(obj);
				result += ", ";
			}
			if (result.Length>=2) result = result.Substring(0, result.Length-2);
			return result;
		}


		public static string Nicify (this string camelCase)
		{
			string titleCase = System.Text.RegularExpressions.Regex.Replace(camelCase, @"(\B[A-Z])", @" $1"); //thanks https://bytes.com/topic/c-sharp/answers/277768-regex-convert-camelcase-into-title-case for regex
			return titleCase.Substring(0, 1).ToUpper() + titleCase.Substring(1);
		}



		public static void CheckSetInt (this Material mat, string name, int val) { if (mat.HasProperty(name)) mat.SetInt(name, val); }
		public static void CheckSetFloat (this Material mat, string name, float val) { if (mat.HasProperty(name)) mat.SetFloat(name, val); }
		public static void CheckSetTexture (this Material mat, string name, Texture tex) { if (mat.HasProperty(name)) mat.SetTexture(name, tex); }
		public static void CheckSetVector (this Material mat, string name, Vector4 val) { if (mat.HasProperty(name)) mat.SetVector(name, val); }
		public static void CheckSetColor (this Material mat, string name, Color val) { if (mat.HasProperty(name)) mat.SetColor(name, val); }


		public class WeakRefComparer<T> : IEqualityComparer<WeakReference<T>> where T : class
		{
			public bool Equals ( WeakReference<T> wr2, T t1) 
			{ 
				//if (!wr1.TryGetTarget(out T t1)) return false;
				if (!wr2.TryGetTarget(out T t2)) return false;
				return t1==t2;
			}

			public bool Equals (T t1, WeakReference<T> wr2) 
			{ 
				//if (!wr1.TryGetTarget(out T t1)) return false;
				if (!wr2.TryGetTarget(out T t2)) return false;
				return t1==t2;
			}

			public bool Equals (WeakReference<T> wr1, WeakReference<T> wr2) 
			{ 
				if (!wr1.TryGetTarget(out T t1)) return false;
				if (!wr2.TryGetTarget(out T t2)) return false;
				return t1==t2;
			}

			public int GetHashCode (WeakReference<T> obj) 
			{
				if (!obj.TryGetTarget(out T t)) return 0;
				return t.GetHashCode();
			}
		}

		public static int Max<T> (this IEnumerable<T> enumerable, Func<T,int> getNumFn)
		{
			int max = int.MinValue;
			foreach (T element in enumerable)
			{
				int num = getNumFn(element);
				if (num > max)
					max = num;
			}
			return max;
		}

		public static uint Max<T> (this IEnumerable<T> enumerable, Func<T,uint> getNumFn)
		{
			uint max = uint.MinValue;
			foreach (T element in enumerable)
			{
				uint num = getNumFn(element);
				if (num > max)
					max = num;
			}
			return max;
		}

		public static bool Contains<T> (this List<T> list, T val, out int index)
		/// Checks if list contains value and returns it's index
		{
			index = list.IndexOf(val);
			return index >= 0;
		}

		public static T GetInRange<T> (this List<T> list, int index) where T: class
		/// Gets object if it is within list range, null if not
		{
			if (index>0 && index<list.Count)
				return list[index];
			else 
				return null;
		}

		public static void ExtendSet<T> (this List<T> list, T val, int index)
		/// Sets val at index, resizing list if necessary
		{
			if (list.Count-1 < index)
				list.AddRange( new T[index-(list.Count-1)] );
			list[index] = val;
		}

		
		
		#if UNITY_EDITOR
		public static UnityEditor.EditorWindow GetInspectorWindow ()
		{
			var editorAsm = typeof(UnityEditor.Editor).Assembly;
			var inspWndType = editorAsm.GetType("UnityEditor.InspectorWindow");
			return UnityEditor.EditorWindow.GetWindow(inspWndType);
		}
		#endif

	}//extensions
}//namespace
