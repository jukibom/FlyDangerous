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

namespace MapMagic.Expose.GUI
{
	//[EditoWindowTitle(title = "MapMagic Graph")]  //it's internal Unity stuff
	[Serializable]
	public class ExposeWindow : EditorWindow
	{
		[SerializeField] private Graph graph;
		[SerializeField] private Exposed.Entry entry;

		[SerializeField] private UI ui = new UI();

		private static readonly Dictionary<int,string> chToName = new Dictionary<int, string> { {-1,"None"}, {0,"X"}, {1,"Y"}, {2,"Z"}, {3,"W"} };


		public void OnGUI () => ui.Draw(DrawParams, inInspector:false);

		private void DrawParams () 
		{
			if (entry==null || graph==null) //happens after re-compile
				Close();

			using (Cell.Padded(5,5,5,5))
			{
				Cell.current.fieldWidth = 0.7f;

				//using (Cell.LineStd) Draw.DualLabel("Generator", genName);
				using (Cell.LineStd) Draw.DualLabel("Node Id", entry.id.ToString());
				using (Cell.LineStd) Draw.DualLabel("Field", $"{entry.name.Nicify()} ({entry.type})");
				using (Cell.LineStd) Draw.DualLabel("Channel", chToName[entry.channel]);
				using (Cell.LineStd) Draw.DualLabel("Array Index", entry.arrIndex==-1 ? "N/A" : entry.arrIndex.ToString());
				
				Cell.EmptyLinePx(10);

				//expression field
				using (Cell.LineStd) 
					Draw.Label("Expression:");

				using (Cell.LineStd) 
					Draw.Field(ref entry.expression);

				//parsings/checking expression (every frame, since we can add or remove overrided variables from graph)
				string error = null;
				string warning = null;
				string result = null;
				string assign = null; //value name to assign override to graph with button

				if (entry.expression.Length == 0)
					result = "Non exposed";

				else
				{
					entry.calculator = Calculator.Parse(entry.expression);

					if (!entry.calculator.CheckValidity(out string anyError))
						error = anyError; //error assigned only if non-valid

					else
					{
						assign = entry.calculator.CheckOverrideAssign(graph.defaults);
						if (assign != null)
							warning = $"Graph variable '{assign}' is not assigned. \nUsing 0 instead.";
					/*	else  //currently type can't be wroong
						{
							warning = calculator.CheckOverrideType(graph.defaults);
							if (warning != null)
								warning = $"Graph variable '{warning}' has the wrong type. \nUsing 0 instead.";
						}*/

						Type assignedType = entry.channel>=0 ? typeof(float) : entry.type;
						result = $"All okay. Default result: {entry.calculator.Calculate(graph.defaults).Convert(assignedType)}";
					}
				}

				Cell.EmptyLinePx(10);

				//errors/warnings
				using (Cell.LinePx(46))
				{
					using (Cell.RowPx(20))
					{
						using (Cell.LinePx(16))
						{
							if (error != null) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Error"));
							else if (warning != null) Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Warning"));
							else Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Okay"));
						}
						Cell.EmptyLine();
					}

					using (Cell.Row) 
					{
						if (error != null) Draw.Label(error, style:UI.current.styles.topLabel);
						else if (warning != null) Draw.Label(warning, style:UI.current.styles.topLabel);
						else Draw.Label(result, style:UI.current.styles.topLabel);
					}

					//assign button
					if (assign != null)
						using (Cell.RowPx(60))
						{
							using (Cell.LinePx(22))
								if (Draw.Button("Assign"))
								{
									graph.defaults.AddExposed(entry,graph);
									OverrideInspector.RefreshMapMagic(entry.name);
								}

							Cell.EmptyLine();
						}
				}

				//Cell.EmptyLinePx(10);

				//okay/cancel
				using (Cell.LinePx(22)) 
				{
					Cell.EmptyRow();

					using (Cell.RowPx(70)) 
					{
						Cell.current.disabled = error!=null; //could not be pressed while expression is not valid

						if (Draw.Button("OK"))
						{
							if (entry.expression.Length == 0)
								graph.exposed.Remove(entry.id, entry.name, entry.channel, entry.arrIndex);
							
							else
							{
								//removing all channels if exposing vector
								if (entry.channel < 0)
								{
									graph.exposed.Remove(entry.id, entry.name, 0, entry.arrIndex);
									graph.exposed.Remove(entry.id, entry.name, 1, entry.arrIndex);
									graph.exposed.Remove(entry.id, entry.name, 2, entry.arrIndex);
									graph.exposed.Remove(entry.id, entry.name, 3, entry.arrIndex);
								}

								//removing base vector if exposing channel
								else
									graph.exposed.Remove(entry.id, entry.name, -1, entry.arrIndex);

								//exposing
								graph.exposed.Add(entry, overwrite:true);
							}

							Close();
							EditorWindow.focusedWindow?.Repaint();
						}
					}

					Cell.EmptyRowPx(10);

					using (Cell.RowPx(70)) 
						if (Draw.Button("Cancel"))
							Close();
				}
			}
		}


		private void DrawExpression ()
		/// Draws expression line with check string
		{
			
		}



		public void ParseCheckExpression ()
		{

		}

		public static void ShowWindow (Graph graph, ulong genId, string fieldName, Type fieldType, int channel, int arrIndex)
		{
			Vector2 mousePos = Event.current.mousePosition + UI.current.editorWindow.position.position; //before opening window

			ExposeWindow window = GetWindow<ExposeWindow>();
			if (window != null) window.Close(); //otherwise it will turn utility window in a tab
			window = ScriptableObject.CreateInstance(typeof(ExposeWindow)) as ExposeWindow;

			window.graph = graph;
			window.entry = graph.exposed[genId, fieldName, channel, arrIndex];
			if (window.entry == null  ||  window.entry.type != fieldType)
				window.entry = new Exposed.Entry(
					id: genId, 
					name: fieldName, 
					type: fieldType, 
					channel: channel,
					arrIndex: arrIndex,
					expression: "" );

			window.titleContent = new GUIContent("Expose " + fieldName.Nicify());
			window.ShowUtility();

			Vector2 windowSize = new Vector2(310, 220);
			window.position = new Rect(
				mousePos - windowSize/2,
				windowSize);
		}
	}

}//namespace