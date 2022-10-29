using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Expose;
using MapMagic.Products;
using MapMagic.Previews;
using MapMagic.Nodes.GUI;

namespace MapMagic.Expose.GUI
{
	public static class OverrideInspector
	{
		public static void DrawLayeredOverride (Graph graph)
		/// Drawing override in layers-style (can add, remove or switch)
		/// For graph defaults
		{
			Override ovd = graph.defaults;

			using (Cell.LinePx(0))
				LayersEditor.DrawLayers(
					ovd.Count,
					onDraw:n => DrawLayer(ovd, n),
					onAdd:n => NewOverrideWindow.ShowWindow(ovd,n),
					onRemove:n => ovd.RemoveAt(n),
					onMove:(n1,n2) => ovd.Switch(n1,n2) );
			
			using (Cell.LinePx(20))
			{
				using (Cell.Row)
					if (Draw.Button("Add All Exposed"))
						graph.defaults.AddAllExposed(graph.exposed, graph);

				using (Cell.Row)
					if (Draw.Button("Remove Unused"))
						graph.defaults.RemoveAllUnused(graph.exposed);
			}
		}


		public static void DrawLayer (Override ovd, int num)
		{
			(string name, Type type, object obj) = ovd.GetOverrideAt(num);

			Cell.EmptyLinePx(2);
			using (Cell.LineStd)
			{
				Cell.EmptyRowPx(2);

				using (Cell.RowPx(20)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Layer"));
				using (Cell.Row) 
				{
					obj = Draw.UniversalField(obj, type, name);

					if (Cell.current.valChanged)
					{
						ovd.SetOverrideAt(num, name, type, obj);
						RefreshMapMagic(name);
					}
				}

				Cell.EmptyRowPx(2);
			}
			Cell.EmptyLinePx(2);
		}


		public static void DrawStaticOverride (Override ovd)
		/// For inspector use only: will make it re-generate on change, so don't use in function
		{
			for (int i=0; i<ovd.Count; i++)
			{
				(string name, Type type, object obj) = ovd.GetOverrideAt(i);

				using (Cell.LineStd)
				{
					obj = Draw.UniversalField(obj, type, name);

					if (Cell.current.valChanged)
					{
						ovd.SetOverrideAt(i, name, type, obj);
						RefreshMapMagic(name);
					}
				}
			}
		}


		public static void RefreshMapMagic (string reference)
		/// Makes MM generate if override changed
		/// Automaticaly finds current mm and it's graph
		{
			IMapMagic mm = GraphWindow.current.mapMagic;
			if (mm == null)
				return;

			Graph mmGraph = mm.Graph;
			if (mmGraph == null)
				return;
			
			foreach (IUnit unit in mmGraph.exposed.UnitsByReference(mmGraph, reference))
				if (unit is Generator gen)
					mm.Clear(gen);

			GraphWindow.current?.RefreshMapMagic(null); 
				//will clear no generator - needed genscleared before
		}
	}


	public class NewOverrideWindow : EditorWindow
	{
		public Override ovd;
		private UI ui = new UI();

		public int num;
		private string varName = "";
		private Type varType = typeof(float);

		private static Type[] types = new Type[] {	
			typeof(float), typeof(int), typeof(bool), typeof(Vector2D), typeof(Vector3),
			typeof(Texture2D), typeof(TerrainLayer)  };
		private static string[] typeNames = new string[] {  //to avoid gathering names for drop-down
			"Float", "Int", "Boolean", "Vector2D", "Vector3",
			"Texture", "Terrain Layer" };

		//public override void OnGUI(Rect rect) => ui.Draw(DrawParams);
		//public override Vector2 GetWindowSize() =>  new Vector2(200, 150);
		public void OnGUI () => ui.Draw(DrawParams, inInspector:false);

		private void DrawParams () 
		{
			using (Cell.Padded(5,5,5,5))
			{
				using (Cell.LineStd) Draw.Field(ref varName, "Name");
				using (Cell.LineStd) Draw.PopupSelector(ref varType, types, typeNames, "Type");

				Cell.EmptyLine();

				using (Cell.LinePx(22)) 
				{
					Cell.EmptyRow();

					using (Cell.RowPx(70)) 
					{
						Cell.current.disabled = name.Length!=0; //could not be pressed while expression is not valid

						if (Draw.Button("OK"))
						{
							ovd.Add(varName, varType, varType.IsValueType ? Activator.CreateInstance(varType) : null);
							
							Close();
							GraphWindow.current?.RefreshMapMagic();
							Extensions.GetInspectorWindow()?.Repaint();
						}
					}

					Cell.EmptyRowPx(10);

					using (Cell.RowPx(70)) 
						if (Draw.Button("Cancel"))
							Close();
				}
			}
		}

		public static void ShowWindow (Override ovd, int num)
		{
			Vector2 curWinPos = focusedWindow!=null ? focusedWindow.position.position : new Vector2(0,0);
			Vector2 mousePos = Event.current.mousePosition + curWinPos; //before opening window

			NewOverrideWindow window = ScriptableObject.CreateInstance(typeof(NewOverrideWindow)) as NewOverrideWindow;

			window.ovd = ovd;
			window.num = num;
			window.titleContent = new GUIContent("New Variable");
			window.ShowUtility();

			Vector2 windowSize = new Vector2(200, 100);
			window.position = new Rect(
				new Vector2(Screen.currentResolution.width, Screen.currentResolution.height)/2 - windowSize/2,
				windowSize);
		}
	}
}