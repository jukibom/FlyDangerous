using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;

namespace Den.Tools.GUI
{
	public class UI
	{
		public static UI current;

		public Cell rootCell;
		public List<(Action action, int order)> afterLayouts = new List<(Action,int)>();
		public List<(Action action, int order)> afterDraws = new List<(Action,int)>();
		public bool layout;

		public ScrollZoom scrollZoom = null;
		public StylesCache styles = null;
		public TexturesCache textures = new TexturesCache();
		public CellObjs cellObjs = new CellObjs();
		public Undo undo = null;

		public EditorWindow editorWindow;

		public Rect subWindowRect;   
		public Vector2 viewRectMax = new Vector2(int.MaxValue, int.MaxValue); //window rect in internal coordinates (to optimize cells)
		public Vector2 viewRectMin = new Vector2(-int.MaxValue/2, -int.MaxValue/2); //min is 0 when no scrollzoom, but when scrolled it change it's value
		public Vector2 mousePos; //mouse position in internal coordinates
		public Vector2 prevMousePos;
		public int mouseButton = -1; //shortcut for Event.current.button (0-left, 1-right, 2-middle)
		public float ViewRectHeight { get{ return viewRectMax.y-viewRectMin.y; } }

		public bool optimizeEvents = false;
		public bool optimizeElements = false; //skips cell if it has no child cells
		public bool optimizeCells = false; //skips cell if it has child cells. Experimental!

		public bool hardwareMouse = false;

		public bool isInspector = false; //to draw foldout
		public Vector2 scrollBarPos; //for windows with the scrollbar

		public Cell delayedCell;  //to change values only on Enter
		public float delayedFloat;
		public int delayedInt;

		public static bool FieldLostFocus =>  //no way to find out if field lost focus, so using cases where it can do so
			//(UI.current != null  &&  UI.current.editorWindow != EditorWindow.focusedWindow)  ||  //will reset cell in UI first if enabled. Kinda complicated.
			(Event.current.rawType != EventType.Layout && //somehow called from UI on layout
				(Event.current.keyCode == KeyCode.Return  ||  Event.current.keyCode == KeyCode.KeypadEnter  ||  Event.current.keyCode == KeyCode.Tab))  ||  //these might be called when event is Used
			Event.current.rawType == EventType.MouseDown  ||  Event.current.rawType == EventType.MouseUp  ||
			Event.current.rawType == EventType.MouseDrag;


		public float DpiScaleFactor 
		{get{
			if (editorWindow==null) return 1;

			float factor = Screen.width / editorWindow.position.width;
			return ((int)(float)(factor * 4f + 0.5f)) / 4f; //rounding to 0.25f
		}}

		public enum RectSelector { Standard, Padded, Full }

		public static bool MouseUp  
		//Working synonym for EventType.MouseUp
		{get{
			if (Event.current.rawType == EventType.MouseUp) return true;
			
			//MouseUp is not called when mouse leaved window
			//but MouseLeaveWindow does - when releasing mouse
			//docs say that MouseLeaveWindow is not fired when mouse pressed, but actually it's fired when mouse released (instead MouseUp)
			//wantsMouseEnterLeaveWindow should be set to true

			//#if !UNITY_EDITOR_OSX  //in older versions it worked fine in OSX, but now seems the same as Windows
			if (Event.current.rawType == EventType.MouseLeaveWindow) return true;  
			//#endif

			return false;
		}}


		public static UI ScrolledUI (float maxZoom=1, float minZoom=0.4375f)
		{
			return new UI {
				scrollZoom = new ScrollZoom() { allowScroll=true, allowZoom=true, maxZoom = maxZoom, minZoom = minZoom },
				optimizeEvents = true,
				optimizeElements = true };
		}


		#region Draw

			public void DrawInSubWindow (Action drawAction, int id, Rect rect)
			/// Draws in unity's BeginWindows group
			{
				this.subWindowRect = rect;

				//rect.position = new Vector2(0,0);

				UnityEngine.GUI.WindowFunction drawFn = tmp => Draw(drawAction, inInspector:false);

				//hack. GUILayout.Window will not be called when mouse is not in window rect, but we have to release drag somehow
				if (editorWindow != null  &&  !editorWindow.wantsMouseEnterLeaveWindow) 
					editorWindow.wantsMouseEnterLeaveWindow = true;

				if (MouseUp)
				{
					Event.current.mousePosition -= rect.position; //offseting release button mouse position since it counts window offset as 0
					Draw(drawAction, inInspector:false);
				}

				else
				{
					//placing 2 rects in _this_ window if GraphGUI was not called
					UnityEditor.EditorGUILayout.GetControlRect(GUILayout.Height(0));
					UnityEditor.EditorGUILayout.GetControlRect(GUILayout.Height(0));
				}

				//window
				GUILayout.Window(id, rect, drawFn, new GUIContent(), GUIStyle.none, 
					GUILayout.MaxHeight(rect.height), GUILayout.MinHeight(rect.height), 
					GUILayout.MaxWidth(rect.width), GUILayout.MinWidth(rect.width) );
			}


			public void Draw (Action drawAction, bool inInspector, Rect customRect=new Rect())
			/// If calling two Draw instances in one window one should have offsetAfterDraw enabled (otherwise will not mouseUp)
			/// inInspector - starts the rect after inspector content and adds height after to draw further components
			/// if not in inspector - then can draw in custom rect (instead of full window)
			{
				Profiler.BeginSample("DrawUI");

				UI.current = this;

				afterLayouts.Add((drawAction, 0));
				afterDraws.Add((drawAction, 0));

				editorWindow = GetActiveWindow();
				float dpiScaleFactor = DpiScaleFactor;
				//Vector2 screenSize = new Vector2(Screen.width, Screen.height) / DpiScaleFactor;
					//screen size not in pixels but in Unity's understanding. It will be scaled back to pixels then

				//if in scrollable window - enabling wants mouse leave
				//GUILayout.Window will not be called when mouse is not in window rect, but we have to release drag somehow
				if (!inInspector && scrollZoom != null && (mouseButton==2 || Event.current.alt))
					editorWindow.wantsMouseEnterLeaveWindow = true;

				//finding rect
				UnityEditor.EditorGUI.indentLevel = 0;
				Rect rect;
				if (inInspector) rect = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none); //EditorStyles.helpBox for padding
				else
				{
					if (customRect.width < 0.1f  &&  customRect.height < 0.1f)
						rect = new Rect(0,0,Screen.width/dpiScaleFactor, Screen.height/dpiScaleFactor);
					else
						rect = customRect;
				}
				subWindowRect = rect;
				this.isInspector = inInspector;

				//scroll/zoom
				if (scrollZoom != null)  //just because alt rotates 'scene view in graph'
				{
					#if MM_DEBUG
					if (!Event.current.alt)
					#endif
					{
						scrollZoom.Scroll();
						scrollZoom.Zoom();
					}
				}
				
				

				//styles
				if (styles == null) 
					styles = new StylesCache();
				styles.CheckInit();
				if (scrollZoom != null) 
					styles.Resize(scrollZoom.zoom);
			
				//mouse button
				if (Event.current.type == EventType.MouseDown)
					mouseButton = Event.current.button;
				else
					mouseButton = -1;

				//mouse pos
				prevMousePos = mousePos;
				#if UNITY_EDITOR_WIN
				if (hardwareMouse)
				{
					GetCursorPos(out Vector2Int intPos);
					mousePos = intPos - editorWindow.position.position;
				}
				else 
				#endif
					mousePos = Event.current.mousePosition;

				//internal rect
				if (scrollZoom != null)
				{
					viewRectMin = scrollZoom.ToInternal( new Vector2(0,0) ) - Vector2.one;
					viewRectMax = scrollZoom.ToInternal( new Vector2(Screen.width, Screen.height) ) + Vector2.one;
					mousePos = scrollZoom.ToInternal(mousePos);
				}
				else
				{
					viewRectMin = Vector2.zero;
					viewRectMax = new Vector2(Screen.width, Screen.height);
				}

				//root cell rect (hacky)
				Rect rootCellRect = inInspector ?
					rect :
					new Rect(0,0, Screen.width/dpiScaleFactor, Screen.height/dpiScaleFactor);

				//preparing shaders
				Shader.SetGlobalVector("_ScreenRect", new Vector4(rect.x, rect.y, Screen.width, Screen.height) );
				Shader.SetGlobalVector("_ScreenParams", new Vector4(Screen.width, Screen.height, 1f/Screen.width, 1f/Screen.height) );
				Shader.SetGlobalVector("_InternalRect", new Vector4(viewRectMin.x, viewRectMin.y, viewRectMax.x-viewRectMin.x, viewRectMax.y-viewRectMin.y) );

				//clearing active cell stack in case previous gui was failed to finish (or color picker clicked)
				if (Cell.activeStack.Count != 0)
				{
					Cell.activeStack.Clear();
					//Debug.Log("Trying to start UI with non-empty active stack");  
				}
			
			
				//drawing
				if (!optimizeEvents || !SkipEvent())
				//using (Timer.Start("Draw GUI"))
				{
					layout = true;
					//using (Timer.Start("Draw pre-layout"))
						using (Cell.Root(ref rootCell, rootCellRect))
						{
							for (int i=0; i<afterLayouts.Count; i++) //count could be increased while iterating
								afterLayouts[i].action();
						}

					//using (Timer.Start("CalculateMinContentsSize"))
						rootCell.CalculateMinContentsSize();

					//using (Timer.Start("CalculateRootRects"))
						rootCell.CalculateRootRects();

					layout = false;
					//using (Timer.Start("Draw final"))
						using (Cell.Root(ref rootCell, rootCellRect))
						{
							for (int i=0; i<afterDraws.Count; i++) //count could be increased while iterating
								afterDraws[i].action();
						}

					UI.current = null;
				}

				DragDrop.ResetTempObjs();

				//resetting afterdraw actions
				afterLayouts.Clear();
				afterDraws.Clear();

				cellObjs.Clear();

				//setting inspector/window rect
				if (inInspector)
				{
					float inspectorHeight = rootCell!=null ? (float)rootCell.finalSize.y : 0;
					inspectorHeight -= 20; //Unity leaves empty space for some reason
					Rect wholeRect = UnityEditor.EditorGUILayout.GetControlRect(GUILayout.Height(inspectorHeight));

					UnityEngine.GUI.Button(wholeRect, "", GUIStyle.none); 
					//drawing any control on all the field, otherwise OnMouseUp won't be called when mouse left the window
					//known issue: whole rect doesnt cover all for some reason
				}

				//clearing delayed cell on field lost focus
				if (delayedCell!=null  &&  FieldLostFocus) 
					delayedCell = null;

				if (Event.current.keyCode == KeyCode.Escape)
					delayedCell = null;

				//disabling field right-click (copy/paste) when opened right-click menu
				if (Event.current.isMouse && 
					Event.current.type == EventType.MouseUp  && 
					Event.current.button == 1  &&
					EditorWindow.focusedWindow != null &&
					EditorWindow.focusedWindow.GetType() == typeof(UnityEditor.PopupWindow))
						Event.current.Use();

				Profiler.EndSample();
			}

			public void DrawAfter (Action action, int layer=1)
			{
				if (layout)
				{
					afterLayouts.Add( (action,layer) );
					afterLayouts.Sort( (a,b) => -a.order + b.order );
				}
				else
				{
					
					afterDraws.Add( (action,layer) );
					afterDraws.Sort( (a,b) => a.order - b.order );
				}

				//Debug.Log("Layouts add " + afterLayouts.Count);
				//Debug.Log("Draws add " + afterDraws.Count);
			}

			public void ClearDrawAfter ()
			{
				if (layout)
					afterLayouts.Clear();
				else
					afterDraws.Clear();
			}

		#endregion


		#region Helpers

			public static bool SkipEvent ()
			/// Should this event be skipped?
			{
				bool skipEvent = false;

				if (Event.current.type == EventType.Layout  ||  Event.current.type == EventType.Used) skipEvent = true; //skip all layouts
				if (Event.current.type == EventType.MouseDrag) //skip all mouse drags (except when dragging text selection cursor in field)
				{
					if (!UnityEditor.EditorGUIUtility.editingTextField) skipEvent = true;
					if (UnityEngine.GUI.GetNameOfFocusedControl() == "Temp") skipEvent = true; 
				}
				if (Event.current.rawType == EventType.MouseUp) skipEvent = false;

				return skipEvent;
			}


			public bool IsInWindow ()
			/// Finding if cell within a window by it's rect
			{
				Cell cell = Cell.current;

				float borders = 1;

				//Vector2 cellRectPos = cell.worldPosition;
				//Vector2 cellRectSize = cell.finalSize;

				float minX = cell.worldPosition.x;
				float maxX = cell.worldPosition.x + cell.finalSize.x;

				float minY = cell.worldPosition.y;
				float maxY = cell.worldPosition.y + cell.finalSize.y;

				if (maxX < viewRectMin.x - borders||
					maxY < viewRectMin.y - borders ||
					minX > viewRectMax.x + borders ||
					minY > viewRectMax.y + borders)
						return false;

				return true;
			}


			public bool IsInWindow (float minX, float maxX, float minY, float maxY)
			/// Finding if cell within a window by it's rect
			{
				Cell cell = Cell.current;

				float borders = 1;

				if (maxX < viewRectMin.x - borders||
					maxY < viewRectMin.y - borders ||
					minX > viewRectMax.x + borders ||
					minY > viewRectMax.y + borders)
						return false;

				return true;
			}


			public static void RemoveFocusOnControl ()
			/// GUI.FocusControl(null) is not reliable, so creating a temporary control and focusing on it
			{
				//UnityEngine.GUI.SetNextControlName("Temp");
				//UnityEditor.EditorGUI.FloatField(new Rect(-10,-10,0,0), 0);
				//UnityEngine.GUI.FocusControl("Temp");

				UnityEngine.GUI.FocusControl(null);
			}


			public static void RepaintAllWindows ()
			/// Usually called on undo
			{
				UnityEditor.EditorWindow[] windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
				foreach (UnityEditor.EditorWindow win in windows)
					win.Repaint();
			}


			public void MarkChanged (bool completeUndo=false)
			/// Writes undo and cell change. Should be called BEFORE actual change since writes undo
			{
				//write undo and dirty (got to know undo object to set it dirty)
				undo?.Record(completeUndo);

				//writing changed state in all active cells
				for (int i=Cell.activeStack.Count-1; i>=0; i--)
				{
					if (!Cell.activeStack[i].trackChange) break; //root cell should not recieve value change if non-tracked cell changed
					Cell.activeStack[i].valChanged = true;
				}

			}


			private static string[] GetPopupNames<T> (T[] objs, Func<T,string> nameFn, string none=null, string[] names=null)
			/// Generates names array for popups. Use 'none' to place it before other variants. Use 'names' to re-use array.
			{
				int arrLength = objs.Length;
				if (none != null) arrLength++;

				if (names == null || names.Length != arrLength)
					names = new string[arrLength];

				int c = 0;
				for (int i=0; i<arrLength; i++)
				{
					if (i==0 && none!=null) { names[0] = none; continue; }
					names[i] = nameFn(objs[c]);
					c++;
				}

				return names;
			}


			public static Texture2D GetBlankTex ()
			{
				Texture2D tex = new Texture2D(4,4);
				Color[] colors = tex.GetPixels();
				for (int i=0; i<colors.Length; i++) colors[i] = new Color(0,0,0,1);
				tex.SetPixels(colors);
				tex.Apply(true, true);
				return tex;
			}


			[DllImport("user32.dll")]
			public static extern bool GetCursorPos(out Vector2Int lpPoint);

			[DllImport("user32.dll")]
			public static extern bool SetCursorPos(int x, int y);


			public static EditorWindow GetActiveWindow ()
			{
				//HostView hostView = GUIView.current as HostView;
				//return hostView.actualView;

				Type guiViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView");
				PropertyInfo currentGuiViewProp = guiViewType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
				object currentGuiView = currentGuiViewProp.GetValue(guiViewType, null);
				if (currentGuiView == null) return null;

				Type hostViewType = currentGuiView.GetType(); //could be DockArea, which also has a actualView property
				//Type hostViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView");
				//if (currentGuiView.GetType() != hostViewType) return null;
				PropertyInfo actualViewProp = hostViewType.GetProperty("actualView", BindingFlags.Instance | BindingFlags.NonPublic);
				object activeView = actualViewProp.GetValue(currentGuiView);

				return activeView as EditorWindow;
			}

		#endregion
	}
}
