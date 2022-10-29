using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Den.Tools
{
	public static class EditorHacks
	{
		public static void SetIconForObject (UnityEngine.Object obj, Texture2D icon)
		{
			var flags = System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
			var argTypes = new System.Type[]{typeof(UnityEngine.Object), typeof(Texture2D)};
			var methodInfo = typeof(EditorGUIUtility).GetMethod("SetIconForObject", flags, null, argTypes, null);

			var args = new object[] { obj, icon };
			methodInfo?.Invoke(null, args);
		}


		public static Assembly EditorAssembly
			{ get{ return Assembly.GetAssembly(typeof(Editor)); } }

		public static Type GetEditorType (string typeName) //type name with the namespace (UnityEditor.ObjectListArea)
			{ return EditorAssembly?.GetType(typeName); }


		public static void SubscribeToListIconDrawCallback (Action<Rect,string,bool> action)
		{
			Type type = GetEditorType("UnityEditor.ObjectListArea");  
			EventInfo eventInfo = type.GetEvent("postAssetIconDrawCallback", BindingFlags.Static | BindingFlags.NonPublic); //postAssetLabelDrawCallback could also be handy

			Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, action.Target, action.Method);
			
			//eventInfo.AddEventHandler(null, handler);
			var addMethod = eventInfo.GetAddMethod(true);
			addMethod.Invoke(null, new[] {handler});
		}

		public static void SubscribeToTreeIconDrawCallback (Action<Rect,string> action)
		{
			Type type = GetEditorType("UnityEditor.AssetsTreeViewGUI");  
			EventInfo eventInfo = type.GetEvent("postAssetIconDrawCallback", BindingFlags.Static | BindingFlags.NonPublic);  

			Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, action.Target, action.Method);
			
			var addMethod = eventInfo.GetAddMethod(true);
			addMethod.Invoke(null, new[] {handler});
		}


		public static void SubscribeToLabelDrawCallback (Func<Rect,string,bool, bool> func)
		// func return true if drawing occured (space will be redistributed if false)
		{
			Type type = GetEditorType("UnityEditor.ObjectListArea");   
			EventInfo eventInfo = type.GetEvent("postAssetLabelDrawCallback", BindingFlags.Static | BindingFlags.NonPublic);

			Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, func.Target, func.Method);
			
			//eventInfo.AddEventHandler(null, handler);
			var addMethod = eventInfo.GetAddMethod(true);
			addMethod.Invoke(null, new[] {handler});
		}


		//[RuntimeInitializeOnLoadMethod, UnityEditor.InitializeOnLoadMethod] 
		//static void Subscribe()
		//	{ SceneView.duringSceneGui += DragGraphToScene; }
	}
}
