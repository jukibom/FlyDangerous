using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public static class LayersEditor
	{
		const float stepAsideDist = 10; //the distance other layers step aside when dragging layer
		private static readonly RectOffset layerBackgroundOverflow = new RectOffset(0,0,0,1);

		static object dragLayerId = null; //the id of the system currently dragging. If null-nothing dragged. If not match - other layers sys is dragging
		static object dragReleasedId = null;
		static int dragNum = -1;
		static int dragTo = -1; //dragNum;


		public static void DrawLayers<T> (
			ref T[] layers, 
			Action<int> onDraw,
			Func<int,T> onCreate = null)
		{
			T[] newLayers = layers;

			DrawLayers(
				layers.Length,
				onDraw: onDraw,
				onAdd:n => ArrayTools.Insert(ref newLayers, n, onCreate!=null ? onCreate(n) : default ),
				onRemove: n => ArrayTools.RemoveAt(ref newLayers, n),
				onMove: (n,m) => ArrayTools.Switch(newLayers, n, m) );

			if (layers != newLayers)
				layers = newLayers;
		}


		public static void DrawLayers (
			int count,
			Action<int> onDraw,
			Action<int> onAdd = null,
			Action<int> onRemove = null, 
			Action<int,int> onMove = null)
		{
			//GUIStyle background = UI.current.textures.GetElementStyle(texturesFolder+"AddPanelEmpty");
			//Draw.Element(background);

			Cell layersCell = Cell.current;

			using (Cell.LinePx(0)) 
				DrawLayersThemselves(layersCell, count, onDraw);

			using (Cell.LinePx(20)) 
				DrawAddRemove(layersCell, onAdd, onRemove, onMove);
		}

		public static void DrawLayersThemselves (object id, int count, Action<int> onDraw)
		/// Draws layers with the default backgrounds
		{
			DrawLayersThemselves(id, count, onDraw,
				midBackground: UI.current.textures.GetElementStyle("DPUI/Layers/Mid", overflow:layerBackgroundOverflow),
				topBackground: UI.current.textures.GetElementStyle("DPUI/Layers/Top", overflow:layerBackgroundOverflow),
				botBackground: UI.current.textures.GetElementStyle("DPUI/Layers/Bot", overflow:layerBackgroundOverflow),
				dragBackground: UI.current.textures.GetElementStyle("DPUI/Layers/Dragged", overflow:layerBackgroundOverflow) );
		}


		public static void DrawLayersThemselves (object id, int count, Action<int> onDraw, bool roundTop=true, bool roundBottom=true)
		/// Draws layers with the default backgrounds
		{
			DrawLayersThemselves(id, count, onDraw,
				midBackground: UI.current.textures.GetElementStyle("DPUI/Layers/Mid", overflow:layerBackgroundOverflow),
				topBackground: roundTop ? 
					UI.current.textures.GetElementStyle("DPUI/Layers/Top", overflow:layerBackgroundOverflow) :
					UI.current.textures.GetElementStyle("DPUI/Layers/Mid", overflow:layerBackgroundOverflow),
				botBackground: roundBottom ?
					UI.current.textures.GetElementStyle("DPUI/Layers/Bot", overflow:layerBackgroundOverflow) :
					UI.current.textures.GetElementStyle("DPUI/Layers/BotSquare", overflow:layerBackgroundOverflow),
				dragBackground: UI.current.textures.GetElementStyle("DPUI/Layers/Dragged", overflow:layerBackgroundOverflow) );
		}


		public static void DrawLayersThemselves (
			object id,		//any equitable object that send in dragdrop. Should be the same as AddRemove button id. Usually the parent cell
			int count,
			Action<int> onDraw,
			GUIStyle dragBackground=null, GUIStyle topBackground=null, GUIStyle botBackground=null, GUIStyle midBackground=null)
		{
			Cell cell = Cell.current;

			if (cell.subCounter != 0)
				throw new Exception("Using non-empty cell for layers");


			//space to insert dragged cell (stepAside)
			if (!UI.current.layout  &&  dragLayerId==id)
			{
				dragTo = FindDragTo();
				if (dragNum>=0) StepAside(dragNum, dragTo);
			}


			//clearing all drag data after the full frame circle (i.e. before it was assigned with drag)
			if (!UI.current.layout)
			{
				if (dragLayerId == id)
				{
					dragLayerId = null;
					dragNum = -1;
				}
				if (dragReleasedId == id) 
				{
					dragReleasedId = null;
					dragNum = -1;
				}
			}


			//drawing layers
			for (int i=0; i<count; i++)
				using (Cell.LinePx(0))
			{
				DragLayer(id, i);

				if (dragLayerId!=id || i!=dragNum)
					DrawLayer(i, count, onDraw, topBackground, botBackground, midBackground);

				if (dragLayerId==id && i==dragNum)
					DrawDraggedLayer(i, count, onDraw, dragBackground ?? midBackground);
			}
		}


		private static int FindDragTo ()
		/// Finding where the layer is dragged using current cursor position
		{
			if (UI.current.layout) return -1; 

			Cell cell = Cell.current;
			int count = cell.subCells.Count;

			int to = dragNum;

			if (UI.current.mousePos.y < cell.worldPosition.y) return 0;
			else if (UI.current.mousePos.y > cell.worldPosition.y+cell.finalSize.y) return count-1;
			else
			{
				int num = 0; //using counter to skip dragged field
				for (int i=0; i<count; i++)
				{
					if (i == dragNum) continue; //cell is dragged - pos always within this cell

					Cell layerCell = cell.subCells[i];

					double start = layerCell.worldPosition.y;
					double mid = layerCell.worldPosition.y + layerCell.finalSize.y/2;
					double end = layerCell.worldPosition.y + layerCell.finalSize.y;

					if (i==0 && UI.current.mousePos.y <= mid) return 0;
					if (UI.current.mousePos.y > mid) to = num+1;

				//	if (UI.mousePos.y >= start-stepAsideDist  &&  UI.mousePos.y < mid) { dragTo = num; break; }
				//	if (UI.mousePos.y >= mid  &&  UI.mousePos.y < end+stepAsideDist) { dragTo = num+1; break; }

					num++;
				}
			}

			return to;
		}


		private static void StepAside (int dragNum, int dragTo)
		/// Re-layouts cells creating an empty space. Shifting from dragTo to dragNum or vice versa
		{
			if (UI.current.layout) return;
			if (dragNum<0 || dragTo<0) return; //happens when something went wrong with dragging

			Cell cell = Cell.current;
			int count = cell.subCells.Count;

			for (int i=Mathf.Min(dragNum,dragTo); i<count; i++)
			{
				Cell layerCell = cell.subCells[i];

				if (i>=dragTo && i<dragNum)
					layerCell.worldPosition.y += stepAsideDist;

				if (i<=dragTo && i>dragNum)
					layerCell.worldPosition.y -= stepAsideDist;

				layerCell.CalculateSubRects(); //re-layout cell
			}
		}


		private static void DragLayer (object id, int i)
		{
			if (UI.current.layout) return;

			if (DragDrop.TryDrag((id,i), UI.current.mousePos))
			{
				Cell.current.worldPosition = DragDrop.initialRect.position + DragDrop.totalDelta;
				Cell.current.CalculateSubRects(); //re-layout cell

				dragLayerId = id;
				dragNum = i;
			}

			if (DragDrop.TryRelease((id,i), UI.current.mousePos))
			{ 
				dragReleasedId = id; 
				dragNum = i;
			}

			if (DragDrop.TryStart((id,i), UI.current.mousePos, Cell.current.InternalRect))
			{
				dragLayerId = id;
				dragNum = i;
			}
		}


		private static void DrawLayer (int i, int count, Action<int> onDraw, 
			GUIStyle topBackground=null, GUIStyle botBackground=null, GUIStyle midBackground=null)
		{
			//background
			if (!UI.current.layout  &&  UI.current.textures != null)
			{
				GUIStyle style = null;
				if (i==0) style = topBackground ?? midBackground;
				else if (i==count-1) style = botBackground ?? midBackground; 
				else style = midBackground;

				if (style!=null) 
					Draw.Element(style); 
			}

			//contents
			onDraw(i);
		}


		private static void DrawDraggedLayer (int i, int count, Action<int> onDraw, GUIStyle dragBackground)
		{
			//drawing dragged in standard order
			if (!UI.current.layout)
			{
				//drag
				Cell.current.worldPosition = DragDrop.initialRect.position + DragDrop.totalDelta;
				//Cell.current.finalSize.x = cell.finalSize.x;
				Cell.current.CalculateSubRects(); //re-layout cell
					
				//background
				if (!UI.current.layout  &&  UI.current.textures != null  &&  dragBackground != null)
					Draw.Element(dragBackground); 
			}
					
			onDraw(i);


			//and enqueue to draw once again
			Rect dragCellRect = Cell.current.InternalRect;
			int num = i; //to closure


			UI.current.DrawAfter(()=>
				{
					using (Cell.Custom(dragCellRect))
					{
						Cell.current.InternalRect = dragCellRect;
						Cell.current.CalculateSubRects(); //re-layout cell

						//background
						if (!UI.current.layout  &&  UI.current.textures != null  &&  dragBackground != null)
						Draw.Element(dragBackground);
					
						//contents
						onDraw(num);
					}
				}, 1);
		}


		public static void DrawAddRemove (
			object id,						 //the same object as in DrawLayers
			Action<int> onAdd = null,
			Action<int> onRemove = null, 
			Action<int,int> onMove = null)
		{
			//add/remove
			bool draggedOnRemoveCell = false;

			Cell.EmptyRow();

			using (Cell.RowPx(70))
			{
				//Cell.current.worldPosition.y = cell.worldPosition.y + cell.finalSize.y - Cell.lineHeight;

				if (dragLayerId != id) //add when drag is disabled
				{
					Texture2D buttonIcon = UI.current.textures.GetTexture("DPUI/Layers/Add");
					if (Draw.Button(buttonIcon, visible:false))
					{
						UI.current.MarkChanged();
						onAdd(0);

						Event.current.Use(); //gui structure changed. Not necessary since button is used
					}
				}

				else //displaying remove when drag is enabled
				{
					Rect cellRect = Cell.current.InternalRect;
					draggedOnRemoveCell = cellRect.Contains(UI.current.mousePos);

					UI.current.DrawAfter(()=>
					{
						using (Cell.Custom(cellRect))
						{
							Cell.current.InternalRect = cellRect;

							Draw.Icon( draggedOnRemoveCell ?
								UI.current.textures.GetTexture("DPUI/Layers/RemoveBright") :
								UI.current.textures.GetTexture("DPUI/Layers/RemoveDark") );
						}
					}, 2);
				}
			}

			Cell.EmptyRowPx(2);

			//releasing drag
			if (dragReleasedId==id && dragNum>=0 && dragTo>=0)
			{
				if (draggedOnRemoveCell)
				{
					UI.current.MarkChanged();
					onRemove(dragNum); //!invert ? dragNum : count-1-dragNum;

					Event.current.Use(); //to remove extra child in flush
				}

				else if (onMove != null && dragNum!=dragTo)  //switch is the last - it conflicts with remove
				{
					UI.current.MarkChanged();  
					onMove(dragNum, dragTo); //!invert ? dragNum : count-1-dragNum,  //!invert ? dragTo : count-1-dragTo);
				}

				//using dragged object
				dragReleasedId = null; dragNum = -1;

				//clearing after-draw (or will try to draw removed layer)
				UI.current.ClearDrawAfter();
			}
		}


		public static void DrawAddRemove (
			object id,						
			string label,
			Action<int> onAdd = null,
			Action<int> onRemove = null, 
			Action<int,int> onMove = null)
		{
			Cell.EmptyLine();
			using (Cell.LinePx(18)) Draw.Label(label); //placing in the center of cell to match the button font
			Cell.EmptyLine();

			using (Cell.Full)
				DrawAddRemove(id, onAdd, onRemove, onMove);
		}
	}
}
