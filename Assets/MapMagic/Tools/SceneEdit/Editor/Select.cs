using System;
using UnityEngine;
using UnityEditor;

namespace Den.Tools.SceneEdit
{
	public static class Select
	{
		public static bool isDragging; //mouse pressed and not yet released
		public static bool isFrame;  //true if dragging 5 pixels from the original
		public static bool justStarted;  //drag was started. isDragging is true
		public static bool justReleased;  //frame was released. Not fired if frame was not started. Both isDragging and isFrame are true

		public static Rect frameRect;
		public static Vector2 origFramePos; //can't use screenRect position (since mouse could be moved up left)

		public static void UpdateFrame ()
		{
			Event eventCurrent = Event.current;
			bool isAlt = eventCurrent.alt;

			//just pressed
			if (eventCurrent.type==EventType.MouseDown  &&  eventCurrent.button==0  &&  !isAlt)
			{
				justStarted = true;
				isDragging = true;
				origFramePos = eventCurrent.mousePosition;
				frameRect = new Rect(origFramePos, new Vector2(0,0));

				//eventCurrent.Use();
				if (UnityEditor.EditorWindow.focusedWindow != null) UnityEditor.EditorWindow.focusedWindow.Repaint(); 

				return;
			}

			//just released
			if (eventCurrent.rawType == EventType.MouseUp) //any button, any drag state
			{
				if (isFrame)
				{
					justReleased = true;

					//if (Event.current.isMouse) eventCurrent.Use();
					if (UnityEditor.EditorWindow.focusedWindow != null) UnityEditor.EditorWindow.focusedWindow.Repaint(); 

					//isDragging = false;  //disabling next frame
					//isFrame = false;
				}
				else
				{
					isDragging = false;
					isFrame = false;
				}


				return;
			}

			//frame after released - disabling both released and dragged
			if (justReleased)
			{
				isFrame = false;
				isDragging = false;
				justReleased = false;
			}

			//creating frame if dragged 5 pixels from origin
			if (isDragging  &&  !isFrame  &&  !isAlt)
			{
				if ((eventCurrent.mousePosition - origFramePos).sqrMagnitude > 250)
					isFrame = true;
			}


			if (isFrame  &&  !eventCurrent.alt)
			{
				Vector2 max = Vector2.Max(eventCurrent.mousePosition, origFramePos);
				Vector2 min = Vector2.Min(eventCurrent.mousePosition, origFramePos);
				frameRect = new Rect(min, max-min);

				DrawSelectionFrame (frameRect);

				//eventCurrent.Use();
				if (UnityEditor.EditorWindow.focusedWindow != null) UnityEditor.EditorWindow.focusedWindow.Repaint(); 

				return;
			}

			//making frame equal to mouse cursor if not dragging
			if (!isDragging)
				frameRect = new Rect(eventCurrent.mousePosition, Vector2.zero);
		}

		public static void CancelFrame ()
		{
			isFrame = false;
			isDragging = false;
			justStarted = false;
			justReleased = false;

			frameRect = new Rect(0,0,0,0);
			origFramePos = new Vector2(0,0);
		}


		static readonly Color offsetSizeRectColor = new Color(0.7f,0.8f, 0.9f, 1); 
		static readonly Color offsetSizeRectColorTransparent = new Color(0.3f,0.5f, 0.9f,0.125f); 

		public static void DrawSelectionFrame (Rect rect)
		{
			Vector3[] steps = new Vector3[] {
				new Vector3 (rect.position.x, rect.position.y, 0), 
				new Vector3 (rect.position.x+rect.width, rect.position.y, 0), 
				new Vector3 (rect.position.x+rect.width, rect.position.y+rect.height, 0), 
				new Vector3 (rect.position.x, rect.position.y+rect.height, 0),
				new Vector3 (rect.position.x, rect.position.y, 0) };

			for (int i=0; i<steps.Length; i++)
			{
				Ray worldRay = HandleUtility.GUIPointToWorldRay(steps[i]);
				Vector3 worldPos = worldRay.origin + worldRay.direction*100;
				steps[i] = worldPos;
			}

			//EditorGUI.DrawRect(rect, new Color(1,0,0,1));
			//UnityEngine.GUI.Button(new Rect(10,10,100,100), "Test");

			Color hColor = Handles.color;
			Handles.color = new Color(1, 1, 1, 1);

			UnityEditor.Handles.DrawSolidRectangleWithOutline(steps, offsetSizeRectColorTransparent, offsetSizeRectColor);

			Handles.color = hColor;
		}


		public static bool objSelected;

		public static void CheckSelected (ref bool origSelected, ref bool dispSelected, Predicate<Rect> isInFrame)
		/// Selects an object with by his screenRect area. Both by click and by frame
		/// Frame should be Updated beforehand
		/// origSelected is the "real" selection, dispSelected is for displaying as selected
		{
			dispSelected = origSelected;

			Event eventCurrent = Event.current;
			EventType eventType = eventCurrent.type;
			Vector2 mousePos = eventCurrent.mousePosition;

			if (eventCurrent.button != 0  ||  eventCurrent.alt) return;

			bool add = eventCurrent.shift;
			bool remove = eventCurrent.control;

			//direct click
			if (eventType == EventType.MouseDown) 
			{
				bool clicked = isInFrame(new Rect(mousePos,Vector2.zero)); //screenRect.Contains(mousePos);

				if (!add && !remove)
					origSelected = clicked;

				if (add)
					origSelected = origSelected || clicked;

				if (remove)
					if (clicked) origSelected = false;

				dispSelected = origSelected;

				if (clicked)
				//	eventCurrent.Use(); //to avoid clicking next node underneath 
					objSelected = true; //using this instead of Use, otherwise further objects will not be deselected
			}
			else
				objSelected = false;

			//selection frame
			if (isFrame)
			{
				bool inFrame = isInFrame(Select.frameRect); //Select.frameRect.Contains(screenRect.center);

				if (!add && !remove)
				{
					if (inFrame) dispSelected = true;
					else dispSelected = false;
				}
				
				if (add  &&  inFrame)
					dispSelected = true;

				if (remove  &&  inFrame)
					dispSelected = false;

				//setting real selection on release
				if (justReleased)
					origSelected = dispSelected;
					
			}
		}


		public static void CheckSelected (Rect screenRect, ref bool origSelected, ref bool dispSelected)
		/// Simplified CheckSelected if screen rect is already known
		{
			CheckSelected(ref origSelected, ref dispSelected, fr => IsInFrame(screenRect, fr));
		}


		public static void CheckSelected (Vector3 worldPos, float screenExt, ref bool origSelected, ref bool dispSelected)
		/// Same CheckSelected, but using world position instead of screen rect
		{
			CheckSelected(ref origSelected, ref dispSelected, fr => {
				Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
				Rect screenRect = new Rect(screenPos.x-screenExt, screenPos.y-screenExt, screenExt*2, screenExt*2);
				return IsInFrame(screenRect, fr);
			});
		}


		private static bool IsInFrame (Rect screenRect, Rect frameRect)
		{
			//if just click with no frame
			if (frameRect.width<0.1f && frameRect.height<0.1f) 
				return screenRect.Contains(frameRect.position); //if click is within screen rect

			//if dragging fame
			else
				return frameRect.Contains(screenRect.center); //if screen rect center is within frame
		}

	}
}
