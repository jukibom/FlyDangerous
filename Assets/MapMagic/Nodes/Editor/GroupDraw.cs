using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;


namespace MapMagic.Nodes.GUI
{
	public static class GroupDraw
	{
		private static Generator[] draggedGroupNodes;
		private static Vector2[] initialGroupNodesPos;
		private static GUIStyle miniNameStyle;

		public static void DragGroup (Group group, Generator[] allGens=null)
		{
			//dragging
			if (!UI.current.layout)
			{
				if (DragDrop.TryDrag(group, UI.current.mousePos))
				{
					for (int i=0; i<draggedGroupNodes.Length; i++)
					{
						Generator gen = draggedGroupNodes[i];

						gen.guiPosition = initialGroupNodesPos[i] + DragDrop.totalDelta;

						//moving generators cells
						if (UI.current.cellObjs.TryGetCell(gen, "Generator", out Cell genCell))
						{
							genCell.worldPosition = gen.guiPosition;
							genCell.CalculateSubRects();
						}
					}

					group.guiPos = DragDrop.initialRect.position + DragDrop.totalDelta;
					Cell.current.worldPosition = group.guiPos;
					Cell.current.CalculateSubRects();
				}

				if (DragDrop.TryRelease(group, UI.current.mousePos))
				{
					draggedGroupNodes = null;
					initialGroupNodesPos = null;

					#if UNITY_EDITOR //this should be an editor script, but it doesnt mentioned anywhere
					UnityEditor.EditorUtility.SetDirty(GraphWindow.current.graph);
					#endif
				}

				if (DragDrop.TryStart(group, UI.current.mousePos, Cell.current.InternalRect))
				{
					draggedGroupNodes = GetContainedGenerators(group, allGens);
					
					initialGroupNodesPos = new Vector2[draggedGroupNodes.Length];
					for (int i=0; i<draggedGroupNodes.Length; i++)
						initialGroupNodesPos[i] = draggedGroupNodes[i].guiPosition;
				}

				Rect cellRect = Cell.current.InternalRect;
				if (DragDrop.ResizeRect(group, UI.current.mousePos, ref cellRect, minSize:new Vector2(100,100)))
				{
					group.guiPos = cellRect.position;
					group.guiSize = cellRect.size;

					Cell.current.InternalRect = cellRect;
					Cell.current.CalculateSubRects();
				}
			}
		}

		public static void DrawGroup (Group group, bool isMini=false)
		{
			float miniFactor = !isMini ? 1 : 0.5f/GraphWindow.miniZoom; //group controls are slightly smaller than in fullscreen

			//CellObject.SetObject(Cell.current, group);
			if (UI.current.layout)
				UI.current.cellObjs.ForceAdd(group, Cell.current, "Group");

			Texture2D tex = UI.current.textures.GetColorizedTexture("MapMagic/Group", group.color);
			GUIStyle style = UI.current.textures.GetElementStyle(tex);
			Draw.Element(style);

			Cell.EmptyRowPx(5*miniFactor);
			using (Cell.Row)
			{
				if (isMini && miniNameStyle == null)
				{
					miniNameStyle = new GUIStyle(UI.current.styles.bigLabel);
					miniNameStyle.fontSize = (int)(miniNameStyle.fontSize/GraphWindow.miniZoom*0.6f);
				}

				Cell.EmptyLinePx(5*miniFactor);
				GUIStyle labelStyle = !isMini ? UI.current.styles.bigLabel : miniNameStyle;
				using (Cell.LinePx(24*miniFactor)) Draw.EditableLabelRight(ref group.name, style:labelStyle);
				//using (Cell.LineStd)	Draw.EditableLabelRight(ref group.comment);
				Cell.EmptyLinePx(3*miniFactor);
				using (Cell.LineStd)
				{
					using (Cell.RowPx(0))
					{
						if (!isMini)
						{
							using (Cell.RowPx(20)) Draw.Icon(UI.current.textures.GetTexture("DPUI/Chevrons/Down"), scale:0.5f*miniFactor);
							using (Cell.RowPx(35)) Draw.Label("Color");
						}

						if (Draw.Button("", visible:false)) GroupRightClick.DrawGroupColorSelector(group);
					}
				}
			}
		}


		public static Generator[] GetContainedGenerators (Group group, Generator[] all)
		/// Removes dragged-off gens and adds new ones
		{
			List<Generator> generators = new List<Generator>();
			Rect rect = new Rect(group.guiPos, group.guiSize);
			for (int g=0; g<all.Length; g++)
			{
				if (!rect.Contains(all[g].guiPosition, all[g].guiSize)) continue;
				generators.Add(all[g]);
			}

			return generators.ToArray();
		}


		public static Generator[] GetContainedGenerators (Group group, Graph graph)
			{ return GetContainedGenerators(group, graph.generators); }


		public static void RemoveGroupContents (Group group, Graph graph)
		/// Called from editor. Removes the enlisted generators on group remove
		{
			GraphWindow.RecordCompleteUndo();

			Generator[] containedGens = GetContainedGenerators(group, graph.generators);

			for (int i=0; i<containedGens.Length; i++)
				graph.Remove(containedGens[i]);
		}
	}
}