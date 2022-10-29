using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
//using Den.Tools.Segs;
using Den.Tools.Splines;
using MapMagic.Core;  //used once to get tile size
using MapMagic.Products;
using MapMagic.Expose.GUI;
using MapMagic.Expose;

namespace MapMagic.Nodes.GUI
{
	public static class CellExpose
	{
		public static void Expose (this Cell cell, ulong unitId, string fieldName, Type fieldType, int chNum=-1, int arrIndex=-1, bool genExposed=true)
		/// Adds right-click menu to cell and draw exposed field over standard one if cell is exposed
		/// Automatically does so for all vector cells (maintaining their channel numbers)
		/// Checks whether this field is exposed only if genExposed=true (genExposed is false on drawing class to optimize it)
		{
			//fast skipping (since it will be called in every field)
			if (!genExposed && UI.current.mouseButton!=1)
				return;

			//if exposed - overdrawing exposed fields
			bool fieldExposed = genExposed  &&  GraphWindow.current.graph.exposed.Contains(unitId, fieldName, chNum, arrIndex);

			if (fieldExposed && !UI.current.layout)
			{
				foreach (Cell subCell in cell.SubCellsRecursively(includeSelf:true))
				{
					subCell.inactive = true; //turning off controls like field drag
					//making all cells disable recursively since they won't make it AFTER deactivating them
					//could not be made inside special.HasFlag because flag is set only on repaint - when it's too late to make inactive

					if (subCell.special.HasFlag(Cell.Special.Field))
					{
						subCell.Activate();
						ExposedField(unitId, fieldName, fieldType, chNum, arrIndex);
						subCell.Dispose();
					}
				}
			}

			//adding to right-click menu
			if (UI.current.mouseButton==1)
				UI.current.cellObjs.ForceAdd(new RightClickExpose(unitId, fieldName, fieldType, chNum, arrIndex), cell, "Expose");

			
			//if vector - calling per-channel expose cell first
			if (cell.special.HasFlag(Cell.Special.Vector) && chNum < 0) 
			{
				foreach (Cell subCell in cell.SubCellsRecursively())
				{
					if (subCell.special.HasFlag(Cell.Special.VectorX))
						subCell.Expose(unitId, fieldName, fieldType, 0);

					if (subCell.special.HasFlag(Cell.Special.VectorY))
						subCell.Expose(unitId, fieldName, fieldType, 1);

					if (subCell.special.HasFlag(Cell.Special.VectorZ))
						subCell.Expose(unitId, fieldName, fieldType, 2);

					if (subCell.special.HasFlag(Cell.Special.VectorW))
						subCell.Expose(unitId, fieldName, fieldType, 3);
				}
			}
		}


		public static bool ExposableClass (object genObj, ulong genId, string category=null)
		// Synonym of Draw.Class but enhanced with expose
		/// True if drawn any field
		{
			Graph graph = GraphWindow.current.graph;
			bool genExposed = graph.exposed!=null ? graph.exposed.Contains(genId) : false;  //has this generator exposed values? 

			void ExposeCellAction (FieldInfo field, Cell cell)
			{
				if (genExposed || UI.current.mouseButton==1)
					cell.Expose(genId, field.Name, field.FieldType);
			}

			return Draw.Class(genObj, category, ExposeCellAction);
		}


		/// Drawing field of a value that is currently exposed
		private static void ExposedField (ulong genId, string fieldName, Type fieldType, int chNum, int arrIndex)
		{
			if (UI.current.layout) //will need field widt, and what's the point to do it in layout?
				return;

			Graph graph = GraphWindow.current.graph;

			float fieldWidth = Cell.current.finalSize.x; //we are not in layout
			
			string label = graph.exposed.GetExpression(genId, fieldName, channel:chNum, arrIndex:arrIndex);
			float labelWidth = UI.current.styles.label.CalcSize( new GUIContent(label) ).x;
			if (labelWidth > fieldWidth-20)
				label = "...";
			
			Draw.Label(label, UI.current.styles.field);

			Vector2 center = Cell.current.InternalCenter;
			Vector2 iconPos = new Vector2(center.x + fieldWidth/2 - 10, center.y);
			Draw.Icon(UI.current.textures.GetTexture("DPUI/Icons/Expose"), iconPos, scale:0.5f);

			//if (Draw.Button(visible:false))  //cell inactive
			if (UI.current.mouseButton==0 && Cell.current.Contains(UI.current.mousePos))
				ExposeWindow.ShowWindow(graph, genId, fieldName, fieldType, chNum, arrIndex);

			#if UNITY_EDITOR
//			Rect rect = Cell.current.GetRect(UI.current.scrollZoom);
//			UnityEditor.EditorGUIUtility.AddCursorRect(rect, UnityEditor.MouseCursor.Zoom);
			#endif
		}
	}
}