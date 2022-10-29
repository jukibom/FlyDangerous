using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public static class DragDrop
	{
		public static object obj; //drag object
		public static string group; //and id. Drag if both obj and id match. Used in DragField, could be used in Layers and ResizeRect

		public static Rect initialRect; //rect provided with CheckStart
		public static Vector2 initialMousePos;
		public static Vector2 prevMousePos;

		public static Vector2 totalDelta; //drag pos relative to initial position
		public static Vector2 currentDelta;

		public static object releasedObj;
		public static object startedObj;

		//execute order:
		// - TryDrag
		// - TryRelease
		// - TryStart - always the last
		// int the end of frame - ResetOnUse

		public static void Drag (object obj)
		{
			
		}

		public static void ForceStart (object obj, Vector2 mousePos, Rect rect)
		{
			DragDrop.obj = obj;
			startedObj = obj;

			initialMousePos = mousePos;
			initialRect = rect;
			prevMousePos = mousePos;
			totalDelta = new Vector2();
			currentDelta = new Vector2();

			UI.current.editorWindow?.Repaint(); 
		}

		public static bool TryStart (object obj, Vector2 mousePos, Rect rect)
		{
			if (UI.current.layout) return false; //dragging could be started only in repaint (how we get rect otherwise?)

			if (Event.current.type==EventType.MouseDown  )
			if ( Event.current.button==0  &&  rect.Contains(mousePos))
			{
				ForceStart(obj, mousePos, rect);
				return true;
			}
			return false;
		}
		public static bool TryStart (object obj, int id, Vector2 mousePos, Rect rect)  { return TryStart(new DragObj(obj,id), mousePos, rect); } 
		public static bool TryStart (object obj, int id, Rect rect)  { return TryStart(new DragObj(obj,id), rect); }
		public static bool TryStart (object obj, Rect rect) { return TryStart(obj, Event.current.mousePosition, rect); }
		   


		public static bool TryDrag (object obj, Vector2 mousePos)
		/// Done before each gui layout. Change the static position values (does not move any cell or something)
		/// Done both in !UI.layout and non-repaint
		{
			if (DragDrop.obj==null  ||  !DragDrop.obj.Equals(obj)) 
				return false;

			else
			{
				totalDelta = mousePos - initialMousePos;
				currentDelta = mousePos - prevMousePos;

				prevMousePos = mousePos;

				UI.current.editorWindow?.Repaint(); 

				return true;
			}
		}
		public static bool TryDrag (object obj, int id, Vector2 mousePos)  { return TryDrag(new DragObj(obj,id), mousePos); }
		public static bool TryDrag (object obj) { return TryDrag(obj, Event.current.mousePosition); }


		public static bool TryRelease (object obj, Vector2 mousePos) 
		//mousePos is actually not used, made for uniformity
		//if not working in window - check for the OnGUI mouse hack 
		{
			if (UI.MouseUp && !UI.current.layout) 
			//if (Event.current.rawType == EventType.MouseUp && !UI.current.layout) 
			{
				if (DragDrop.obj==null  ||  !DragDrop.obj.Equals(obj)) 
					return false;

				else
				{
					releasedObj = DragDrop.obj;
					DragDrop.obj = null;
					DragDrop.group = null;

					UI.current.editorWindow?.Repaint();  

					return true;
				}
			}

			return false;
		}
		public static bool TryRelease (object obj, int id, Vector2 mousePos)  { return TryRelease(new DragObj(obj,id), mousePos); } 
		public static bool TryRelease (object obj) { return TryRelease(obj, Event.current.mousePosition); }


		public static bool IsDragging (object obj)
		///Doesn't perform drag, just check obj equality
			{ return DragDrop.obj!=null  &&  DragDrop.obj.Equals(obj); }
		public static bool IsDragging (object obj, int id) { return IsDragging(new DragObj(obj,id)); }
		public static bool IsDragging () { return DragDrop.obj!=null; }

		public static bool IsReleased (object obj) { return releasedObj!=null  &&  releasedObj.Equals(obj); }
		public static bool IsReleased () { return releasedObj!=null; }

		public static bool IsStarted (object obj)
			{ return startedObj!=null  &&  startedObj.Equals(obj); }
		public static bool IsStarted () => startedObj!=null;

		public static void ResetTempObjs ()
		///Called after every gui update
		{
			//resetting drag/drop on event use
			if (Event.current.type == EventType.Used  &&  Event.current.rawType != EventType.MouseUp)
				obj = null;

			releasedObj = null;
			startedObj = null;
		}


		public static void ResetOnUse ()
		{
			//resetting drag/drop on event use
			if (Event.current.type == EventType.Used  &&  Event.current.rawType != EventType.MouseUp)
				obj = null;
		}

		public struct DragObj 
		///Temporary object to be used on drag if performing several drags per cell/object
		{ 
			public object obj;
			public int id;

			public DragObj (object o, int i) { obj=o; id=i; }
		}


		public static bool ResizeRect (
			object obj,
			Vector2 mousePos,
			ref Rect rect,
			int border=6,
			Vector2 minSize = new Vector2())
		{
			bool resized = false;


			Rect leftRect = new Rect( rect.x - border/2, rect.y + border, border, rect.height-border*2 );
			
			Rect zoomedRect = UI.current.scrollZoom==null ? leftRect : UI.current.scrollZoom.ToScreen(leftRect.position, leftRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeHorizontal);
			
			ObjId obj1 = new ObjId (obj, 1);
			if (TryDrag(obj1, mousePos))
			{ 
				rect.x = (initialRect).position.x + totalDelta.x;  
				rect.width = (initialRect).width - totalDelta.x; 
				if (rect.width<minSize.x)  { rect.x = (initialRect).xMax - minSize.x; rect.width = minSize.x; }
				resized = true; 
			}
			if (TryRelease(obj1, mousePos))
				{ resized = true; }
			if (TryStart(obj1, mousePos, leftRect))
				{ initialRect = rect; resized = true; } //re-assigning initial rect to full one, not left rect


			Rect rightRect = new Rect( rect.x - border/2 + rect.width, rect.y + border, border, rect.height-border*2 );

			zoomedRect = UI.current.scrollZoom==null ? rightRect : UI.current.scrollZoom.ToScreen(rightRect.position, rightRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeHorizontal);

			ObjId obj2 = new ObjId (obj, 2);
			if (TryDrag(obj2, mousePos))
			{ 
				rect.width = initialRect.width + totalDelta.x; 
				if (rect.width<minSize.x)  { rect.width = minSize.x; }
				resized = true; 
			}
			if (TryRelease(obj2, mousePos))
				{ resized = true; }
			if (TryStart(obj2, mousePos, rightRect))
				{ initialRect = rect; resized = true; } 


			Rect topRect = new Rect( rect.x + border, rect.y - border/2, rect.width-border*2, border );

			zoomedRect = UI.current.scrollZoom==null ? topRect : UI.current.scrollZoom.ToScreen(topRect.position, topRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeVertical);

			ObjId obj3 = new ObjId (obj, 3);
			if (TryDrag(obj3, mousePos))
			{ 
				rect.y = initialRect.position.y + totalDelta.y;  
				rect.height = initialRect.height - totalDelta.y; 
				if (rect.height<minSize.y)  { rect.y = initialRect.yMax - minSize.y; rect.height = minSize.y; }
				resized = true; 
			}
			if (TryRelease(obj3, mousePos))
				{ resized = true; }
			if (TryStart(obj3, mousePos, topRect))
				{ initialRect = rect; resized = true; } 


			Rect bottomRect = new Rect( rect.x + border, rect.y - border/2 + rect.height, rect.width-border*2, border );

			zoomedRect = UI.current.scrollZoom==null ? bottomRect : UI.current.scrollZoom.ToScreen(bottomRect.position, bottomRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeVertical);

			ObjId obj4 = new ObjId (obj, 4);
			if (TryDrag(obj4, mousePos))
			{ 
				rect.height = initialRect.height + totalDelta.y; 
				if (rect.height<minSize.y)  { rect.height = minSize.y; }
				resized = true; 
			}
			if (TryRelease(obj4, mousePos))
				{ resized = true; }
			if (TryStart(obj4, mousePos, bottomRect))
				{ initialRect = rect; resized = true; } 


			Rect topLeftRect = new Rect( rect.x-border, rect.y-border, border*2, border*2);

			zoomedRect = UI.current.scrollZoom==null ? topLeftRect : UI.current.scrollZoom.ToScreen(topLeftRect.position, topLeftRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeUpLeft);

			ObjId obj5 = new ObjId (obj, 5);
			if (TryDrag(obj5, mousePos))
			{ 
				rect.position = initialRect.position + totalDelta;  
				rect.size = initialRect.size - totalDelta; 
				if (rect.width<minSize.x)  { rect.x = initialRect.xMax - minSize.x; rect.width = minSize.x; }
				if (rect.height<minSize.y)  { rect.y = initialRect.yMax - minSize.y; rect.height = minSize.y; }
				resized = true; 
			}
			if (TryRelease(obj5, mousePos))
				{ resized = true; }
			if (TryStart(obj5, mousePos, topLeftRect))
				{ initialRect = rect; resized = true; } 


			Rect topRightRect = new Rect( rect.x-border + rect.width, rect.y-border, border*2, border*2);

			zoomedRect = UI.current.scrollZoom==null ? topRightRect : UI.current.scrollZoom.ToScreen(topRightRect.position, topRightRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeUpRight);

			ObjId obj6 = new ObjId (obj, 6);
			if (TryDrag(obj6, mousePos))
			{ 
				rect.position = initialRect.position + new Vector2(0,totalDelta.y);  
				rect.size = initialRect.size + new Vector2(totalDelta.x, -totalDelta.y);  
				if (rect.width<minSize.x)  { rect.width = minSize.x; }
				if (rect.height<minSize.y)  { rect.y = initialRect.yMax - minSize.y; rect.height = minSize.y; }
				resized = true; 
			}
			if (TryRelease(obj6, mousePos))
				{ resized = true; }
			if (TryStart(obj6, mousePos, topRightRect))
				{ initialRect = rect; resized = true; } 


			Rect bottomLeftRect = new Rect( rect.x-border, rect.y-border + rect.height, border*2, border*2);

			zoomedRect = UI.current.scrollZoom==null ? bottomLeftRect : UI.current.scrollZoom.ToScreen(bottomLeftRect.position, bottomLeftRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect,  UnityEditor.MouseCursor.ResizeUpRight);

			ObjId obj7 = new ObjId (obj, 7);
			if (TryDrag(obj7, mousePos))
			{ 
				rect.position = initialRect.position + new Vector2(totalDelta.x,0);  
				rect.size = initialRect.size + new Vector2(-totalDelta.x, totalDelta.y); 
				if (rect.width<minSize.x)  { rect.x = initialRect.xMax - minSize.x; rect.width = minSize.x; }
				if (rect.height<minSize.y)  { rect.height = minSize.y; }
				resized = true; 
			}
			if (TryRelease(obj7, mousePos))
				{ resized = true; }
			if (TryStart(obj7, mousePos, bottomLeftRect))
				{ initialRect = rect; resized = true; } 


			Rect bottomRightRect = new Rect( rect.x-border + rect.width, rect.y-border + rect.height, border*2, border*2);

			zoomedRect = UI.current.scrollZoom==null ? bottomRightRect : UI.current.scrollZoom.ToScreen(bottomRightRect.position, bottomRightRect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect (zoomedRect, UnityEditor.MouseCursor.ResizeUpLeft);

			ObjId obj8 = new ObjId (obj, 8);
			if (TryDrag(obj8, mousePos))
			{ 
				rect.size = initialRect.size + totalDelta;  
				if (rect.width<minSize.x)  { rect.width = minSize.x; }
				if (rect.height<minSize.y)  { rect.height = minSize.y; }
				resized = true; 
			}
			if (TryRelease(obj8, mousePos))
				{ resized = true; }
			if (TryStart(obj8, mousePos, bottomRightRect))
				{ initialRect = rect; resized = true; } 

			return resized;
		}

		
		private struct ObjId { public object i1; public int i2; public ObjId (object o, int i) {i1=o; i2=i;} } 
		//will be used in ResizeCell instead TupleSet <object, int>
	}
}
