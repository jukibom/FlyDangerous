using System;
using System.Reflection;
using System.Collections.Generic;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public sealed class SpecialEditorAttribute : Attribute
	{
		public string className;
		public string actionName;

		public SpecialEditorAttribute (string className, string actionName) { this.className=className; this.actionName=actionName; }
		public SpecialEditorAttribute (string className, string actionName, string cat) { this.className=className; this.actionName=actionName; }

		[System.NonSerialized] private static readonly Dictionary<Type,Delegate> actionsCache = new Dictionary<Type,Delegate>();


		[System.NonSerialized] private static readonly Dictionary<Type, Delegate> delegateCaches = new Dictionary<Type, Delegate>();
		[System.NonSerialized] private static readonly Dictionary<Type, MethodInfo> methodsCaches = new Dictionary<Type, MethodInfo>();

		private static MethodInfo GetEditorMethod (Type type)
		/// Using original (non-editor) type get gui action delegate
		{
			if (methodsCaches.TryGetValue(type, out MethodInfo editorMethod)) return editorMethod;

			SpecialEditorAttribute customEditorAttribute = Attribute.GetCustomAttribute(type, typeof(SpecialEditorAttribute)) as SpecialEditorAttribute;
			if (customEditorAttribute != null)
			{
				Type editorType = GetEditorType(type, customEditorAttribute.className);
				if (editorType != null)
				{
					editorMethod = editorType.GetMethod(customEditorAttribute.actionName);
					if (editorMethod == null)
						throw new Exception("Could not find method " + customEditorAttribute.actionName + " in " + customEditorAttribute.className);
				}
			}

			methodsCaches.Add(type, editorMethod);
			return editorMethod;
		}

		public static void Draw2 (object obj, Type nullObjType=null)
		/// Type argument for in case of object == null
		/// If object is null using nullObjType to determine it's type
		{
			Type type = obj!=null ? obj.GetType() : nullObjType; //note that using direct object type when it's not null
			
			MethodInfo guiMethod = GetEditorMethod(type);
			if (guiMethod == null) return;

			Action<object> guiAction = Delegate.CreateDelegate(typeof(Action<object>), guiMethod) as Action<object>;
			guiAction(obj);
		}


		public static void Draw<TO> (TO obj)
		{
			Type type = obj.GetType();
			SpecialEditorAttribute customEditorAttribute = Attribute.GetCustomAttribute(type, typeof(SpecialEditorAttribute)) as SpecialEditorAttribute;
			if (customEditorAttribute == null) return;

			Type editorType = GetEditorType(type, customEditorAttribute.className);
			MethodInfo editorMethod = editorType.GetMethod(customEditorAttribute.actionName);
			if (editorMethod == null)
				throw new Exception("Could not find method " + customEditorAttribute.actionName + " in " + customEditorAttribute.className);

			Action<TO> editorAction;
			if (actionsCache.ContainsKey(type)) editorAction = actionsCache[type] as Action<TO>;
			else
			{
				ParameterInfo[] args = editorMethod.GetParameters();
				if (args.Length != 1)
					throw new Exception("Special Editor: Number of method arguments (" + args.Length + ") doesn't match called count (1)");
				if (args[0].ParameterType != typeof(TO))
					throw new Exception("Special Editor: Arguments don't match: \n" + 
						"\t" + args[0].ParameterType + " vs " + typeof(TO) );

				editorAction = Delegate.CreateDelegate(typeof(Action<TO>), editorMethod) as Action<TO>;
				actionsCache.Add(type,editorAction);
			}
			editorAction(obj);
		}

		public static void Draw<TO, T1> (TO obj, T1 t1)
		{
			Type type = obj.GetType();
			SpecialEditorAttribute customEditorAttribute = Attribute.GetCustomAttribute(type, typeof(SpecialEditorAttribute)) as SpecialEditorAttribute;
			if (customEditorAttribute == null) return;

			Type editorType = GetEditorType(type, customEditorAttribute.className);
			MethodInfo editorMethod = editorType.GetMethod(customEditorAttribute.actionName);
			if (editorMethod == null)
				throw new Exception("Could not find method " + customEditorAttribute.actionName + " in " + customEditorAttribute.className);

			Action<TO,T1> editorAction;
			if (actionsCache.ContainsKey(type)) editorAction = actionsCache[type] as Action<TO,T1>;
			else
			{
				ParameterInfo[] args = editorMethod.GetParameters();
				if (args.Length != 2)
					throw new Exception("Special Editor: Number of method arguments (" + args.Length + ") doesn't match called count (2)");
				if (args[0].ParameterType != typeof(TO)  ||  args[1].ParameterType != typeof(T1) )
					throw new Exception("Special Editor: Arguments don't match: \n" + 
						"\t" + args[0].ParameterType + " vs " + typeof(TO) + "\n" +
						"\t" + args[1].ParameterType + " vs " + typeof(T1) );

				editorAction = Delegate.CreateDelegate(typeof(Action<TO,T1>), editorMethod) as Action<TO,T1>;
				actionsCache.Add(type,editorAction);
			}
			editorAction(obj, t1);
		}


		public static void Draw<TO, T1,T2> (TO obj, T1 t1, T2 t2)
		{
			Type type = obj.GetType();
			SpecialEditorAttribute customEditorAttribute = Attribute.GetCustomAttribute(type, typeof(SpecialEditorAttribute)) as SpecialEditorAttribute;
			if (customEditorAttribute == null) return;

			Type editorType = GetEditorType(type, customEditorAttribute.className);
			MethodInfo editorMethod = editorType.GetMethod(customEditorAttribute.actionName);
			if (editorMethod == null)
				throw new Exception("Special Editor: Could not find method " + customEditorAttribute.actionName + " in " + customEditorAttribute.className);

			Action<TO,T1,T2> editorAction;
			if (actionsCache.ContainsKey(type)) editorAction = actionsCache[type] as Action<TO,T1,T2>;
			else
			{
				ParameterInfo[] args = editorMethod.GetParameters();
				if (args.Length != 3)
					throw new Exception("Special Editor: Number of method arguments (" + args.Length + ") doesn't match called count (3)");
				if (args[0].ParameterType != typeof(TO)  ||  args[1].ParameterType != typeof(T1)  ||  args[2].ParameterType != typeof(T2))
					throw new Exception("Special Editor: Arguments don't match: \n" + 
						"\t" + args[0].ParameterType + " vs " + typeof(TO) + "\n" +
						"\t" + args[1].ParameterType + " vs " + typeof(T1) + "\n" +
						"\t" + args[2].ParameterType + " vs " + typeof(T2)	);

				editorAction = Delegate.CreateDelegate(typeof(Action<TO,T1,T2>), editorMethod) as Action<TO,T1,T2>;
				actionsCache.Add(type,editorAction);
			}
			editorAction(obj, t1, t2);
		}


		public static void Draw<TO, T1,T2,T3> (TO obj, T1 t1, T2 t2, T3 t3)
		{
			Type type = obj.GetType();
			SpecialEditorAttribute customEditorAttribute = Attribute.GetCustomAttribute(type, typeof(SpecialEditorAttribute)) as SpecialEditorAttribute;
			if (customEditorAttribute == null) return;

			Type editorType = GetEditorType(type, customEditorAttribute.className);
			MethodInfo editorMethod = editorType.GetMethod(customEditorAttribute.actionName);
			if (editorMethod == null)
				throw new Exception("Special Editor: Could not find method " + customEditorAttribute.actionName + " in " + customEditorAttribute.className);

			Action<TO,T1,T2,T3> editorAction;
			if (actionsCache.ContainsKey(type)) editorAction = actionsCache[type] as Action<TO,T1,T2,T3>;
			else
			{
				ParameterInfo[] args = editorMethod.GetParameters();
				if (args.Length != 4)
					throw new Exception("Special Editor: Number of method arguments (" + args.Length + ") doesn't match called count (4)");
				if (args[0].ParameterType != typeof(TO)  ||  args[1].ParameterType != typeof(T1)  ||  args[2].ParameterType != typeof(T2) ||  args[3].ParameterType != typeof(T3))
					throw new Exception("Special Editor: Arguments don't match: \n" + 
						"\t" + args[0].ParameterType + " vs " + typeof(TO) + "\n" +
						"\t" + args[1].ParameterType + " vs " + typeof(T1) + "\n" +
						"\t" + args[2].ParameterType + " vs " + typeof(T2) + "\n" +
						"\t" + args[3].ParameterType + " vs " + typeof(T3)	);

				editorAction = Delegate.CreateDelegate(typeof(Action<TO,T1,T2,T3>), editorMethod) as Action<TO,T1,T2,T3>;
				actionsCache.Add(type,editorAction);
			}
			editorAction(obj, t1, t2, t3);
		}


		public static Type GetEditorType (Type baseType, string editorTypeName)
		{
			Type editorType = null;
			editorType = Type.GetType(editorTypeName);
			if (editorType != null) return editorType;

			//finding type in it'a assembly
			Assembly ass = baseType.Assembly; //There are only two hard things in Computer Science: cache invalidation and naming things
			editorType = ass.GetType(editorTypeName);
			if (editorType != null) return editorType;
			
			//finding type in it's editor assembly
			Assembly editorAss = null;
			string assName = ass.GetName().Name;
			string editorAssName = assName + "Editor";
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (a.GetName().Name == editorAssName)
					{ editorAss = a; break; }
			}
			if (editorAss != null)
			{
				editorType = editorAss.GetType(editorTypeName);
				if (editorType != null) return editorType;
			}

			//finding type without namespace
			if (editorType == null)
			{
				Type[] assTypes = ass.GetTypes();

				for (int i=0; i<assTypes.Length; i++)
				{
					string typeName = assTypes[i].Name;
					int lastIndexOfDot = typeName.LastIndexOf('.');
					if (lastIndexOfDot >= 0)
						typeName = typeName.Substring(typeName.LastIndexOf('.'), typeName.Length);
					if (typeName == editorTypeName) 
						{ editorType = assTypes[i]; break; }
				}
			}
			if (editorType != null) return editorType;

			//finding type without namespace in editor assembly
			if (editorType == null && editorAss != null)
			{
				Type[] assTypes = editorAss.GetTypes();

				for (int i=0; i<assTypes.Length; i++)
				{
					string typeName = assTypes[i].Name;
					int lastIndexOfDot = typeName.LastIndexOf('.');
					if (lastIndexOfDot >= 0)
						typeName = typeName.Substring(typeName.LastIndexOf('.'), typeName.Length);
					if (typeName == editorTypeName) 
						{ editorType = assTypes[i]; break; }
				}
			}
			return editorType;
		}
	}
}
