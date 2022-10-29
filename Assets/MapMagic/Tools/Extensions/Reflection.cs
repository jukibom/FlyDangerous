using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; //to copy properties

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Den.Tools
{
	static public class ReflectionExtensions
	{
		public static object CallStaticMethodFrom (string assembly, string type, string method, params object[] parameters)
		/// Used for debug purposes only
		{
			// editor assembly is Assembly-CSharp-Editor (or typeof(CustomEditor).Assembly)
			// main is Assembly-CSharp

			Assembly a = Assembly.Load(assembly);
			Type t = a.GetType(type);
			return t.GetMethod(method).Invoke(null, parameters);
		}

		public static void GetPropertiesFrom<T1,T2> (this T1 dst, T2 src) where T1:class where T2:class
		{
			PropertyInfo[] srcProps = src.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
			PropertyInfo[] dstProps = src.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
         
			for (int sp=0; sp<srcProps.Length; sp++)
				for (int dp=0; dp<dstProps.Length; dp++)
			{
				if (srcProps[sp].Name==dstProps[dp].Name && dstProps[dp].CanWrite)
					dstProps[dp].SetValue(dst, srcProps[sp].GetValue(src, null), null);
			}
         }


		public static IEnumerable<FieldInfo> UsableFields (this Type type, bool nonPublic=false, bool includeStatic=false)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			if (nonPublic) flags = flags | BindingFlags.NonPublic; 
			if (includeStatic) flags = flags | BindingFlags.Static; 

			FieldInfo[] fields = type.GetFields(flags);
			for (int i=0; i<fields.Length; i++)
			{
				FieldInfo field = fields[i];
				if (field.IsLiteral) continue; //leaving constant fields blank
				if (field.FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
				if (field.IsNotSerialized) continue;

				yield return field;
			}
		}

		public static IEnumerable<PropertyInfo> UsableProperties (this Type type, bool nonPublic=false, bool skipItems=true)
		{
			BindingFlags flags;
			if (nonPublic) flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance; 
			else flags = BindingFlags.Public | BindingFlags.Instance; 

			PropertyInfo[] properties = type.GetProperties(flags);
			for (int i=0;i<properties.Length;i++) 
			{
				PropertyInfo prop = properties[i];
				if (!prop.CanWrite) continue;
				if (skipItems && prop.Name=="Item") continue; //ignoring this[x] 

				yield return prop;
			}
		}

		public static IEnumerable<MemberInfo> UsableMembers (this Type type, bool nonPublic=false, bool skipItems=true)
		{
			BindingFlags flags;
			if (nonPublic) flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance; 
			else flags = BindingFlags.Public | BindingFlags.Instance; 

			FieldInfo[] fields = type.GetFields(flags);
			for (int i=0; i<fields.Length; i++)
			{
				FieldInfo field = fields[i];
				if (field.IsLiteral) continue; //leaving constant fields blank
				if (field.FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
				if (field.IsNotSerialized) continue;

				yield return field;
			}

			PropertyInfo[] properties = type.GetProperties(flags);
			for (int i=0;i<properties.Length;i++) 
			{
				PropertyInfo prop = properties[i];
				if (!prop.CanWrite) continue;
				if (skipItems && prop.Name=="Item") continue; //ignoring this[x] 

				yield return prop;
			}
		}


		public static void PrintAllFields (this Type type, BindingFlags flags)
		{
			FieldInfo[] fields = type.GetFields();
			for (int i=0; i<fields.Length; i++) Debug.Log(fields[i].Name + ", field, " + flags.ToString());

			PropertyInfo[] props = type.GetProperties(flags);
			for (int i=0; i<props.Length; i++) Debug.Log(props[i].Name + ", property, " + flags.ToString());

			MethodInfo[] methods = type.GetMethods(flags);
			for (int i=0; i<methods.Length; i++) Debug.Log(methods[i].Name + ", method, " + flags.ToString());
		}

		public static void PrintAllFields (this Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			type.PrintAllFields(flags);

			flags = BindingFlags.NonPublic | BindingFlags.Instance;
			type.PrintAllFields(flags);

			flags = BindingFlags.Public | BindingFlags.Static;
			type.PrintAllFields(flags);

			flags = BindingFlags.NonPublic | BindingFlags.Static;
			type.PrintAllFields(flags);

			//type.PrintAllFields();
		}

		public static Component CopyComponent (Component src, GameObject go)
		{
			System.Type type = src.GetType();
			
			Component dst = go.GetComponent(src.GetType());
			if (dst==null) dst = go.AddComponent(type);

			foreach (FieldInfo field in type.UsableFields(nonPublic:true)) field.SetValue(dst, field.GetValue(src));
			foreach (PropertyInfo prop in type.UsableProperties(nonPublic:true))
			{
				if (prop.Name == "name") continue;
				try {prop.SetValue(dst, prop.GetValue(src, null), null); }
				catch { }
			}

			return dst;
		}

		/*public static List<Type> GetAllChildTypes (this Type type)
		{
			List<Type> result = new List<Type>();
		
			Assembly assembly = Assembly.GetAssembly(type);
			Type[] allTypes = assembly.GetTypes();
			for (int i=0; i<allTypes.Length; i++) 
				if (allTypes[i].IsSubclassOf(type)) result.Add(allTypes[i]); //nb: IsAssignableFrom will return derived classes

			return result;
		}*/

		[Obsolete] public static IEnumerable<Type> SubtypesEnumerable (this Type parent)
		{
			Assembly assembly = Assembly.GetAssembly(parent);  //Assembly[] assembleys = AppDomain.CurrentDomain.GetAssemblies();
			Type[] types = assembly.GetTypes();
			for (int t=0; t<types.Length; t++) 
			{
				Type type = types[t];
				if (type.IsSubclassOf(parent) && !type.IsInterface && !type.IsAbstract) yield return type;
			}
		}

		public static Type[] Subtypes (this Type parent, bool allAssemblies=false)
		{
			List<Type> children = new List<Type>();
		
			Assembly[] assembleys;
			if (allAssemblies) assembleys = AppDomain.CurrentDomain.GetAssemblies();
			else assembleys = new Assembly[] { Assembly.GetAssembly(parent) };

			foreach (Assembly assembly in assembleys)  
			{
				Type[] types = assembly.GetTypes();
				for (int t=0; t<types.Length; t++) 
				{
					Type type = types[t];
					if (type.IsInterface || type.IsAbstract) continue;

					if (type.IsSubclassOf(parent) || parent.IsAssignableFrom(type)) children.Add(type);
				}
			}
			return children.ToArray();
		}

		public static Type GetTerrainInspectorType ()
		{
			//System.Reflection.Assembly a = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
			//return a.GetType("TerrainInspector"); //does not work

			#if UNITY_EDITOR
			Type[] tmpTypes = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetTypes();
			for (int i=tmpTypes.Length-1; i>=0; i--) //from the end
			{
				if (tmpTypes[i].Name=="TerrainInspector") 
					return tmpTypes[i];
			}
			#endif

			return null;
		}

		public static object GetTerrainInspectorField (string fieldName, Type inspectorType=null)
		{
			if (inspectorType==null) inspectorType = GetTerrainInspectorType();
			
			object[] editors = Resources.FindObjectsOfTypeAll(inspectorType);
			for (int i=0; i<editors.Length; i++)
			{
				PropertyInfo toolProp = inspectorType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);	
				object field = toolProp.GetValue(editors[i], null);
				if (field != null) return field;
			}
			return null;
		}

		public static void SetTerrainInspectorField (string fieldName, object obj, Type inspectorType=null)
		{
			if (inspectorType==null) inspectorType = GetTerrainInspectorType();
			
			object[] editors = Resources.FindObjectsOfTypeAll(inspectorType);
			for (int i=0; i<editors.Length; i++)
			{
				PropertyInfo toolProp = inspectorType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);	

				toolProp.SetValue(editors[i], obj, null);

				//moving component up to refresh terrain tool state
				//UnityEditorInternal.ComponentUtility.MoveComponentUp(script); 
			}
		}

		public static int GetVersion<T> (this HashSet<T> hashSet)
		{
			FieldInfo field = typeof(HashSet<T>).GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);
			return (int)field.GetValue(hashSet);
		}

		public static int GetVersion<TK,TV> (this Dictionary<TK,TV> dict)
		{
			FieldInfo field = typeof(Dictionary<TK,TV>).GetField("version", BindingFlags.NonPublic | BindingFlags.Instance);
			return (int)field.GetValue(dict);
		}


		public static T GetAddComponent<T> (this GameObject go) where T:Component
		{
			T c = go.GetComponent<T>();
			if (c==null) c = go.AddComponent<T>();
			return c;
		}


		public static void ReflectionReset<T> (this T obj) 
		{
			Type type = obj.GetType();
			T empty = (T)Activator.CreateInstance(type);
			
			foreach (FieldInfo field in type.UsableFields(nonPublic:true)) field.SetValue(obj, field.GetValue(empty));
			foreach (PropertyInfo prop in type.UsableProperties(nonPublic:true)) prop.SetValue(obj, prop.GetValue(empty,null), null);
		}

	}
}