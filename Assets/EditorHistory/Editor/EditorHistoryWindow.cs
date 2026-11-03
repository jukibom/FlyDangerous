using UnityEngine;
using UnityEditor;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Toolbars;
#endif

namespace BedtimeCore.EditorHistory
{
	internal class EditorHistoryWindow : EditorWindow
	{
		[SerializeField]
		private Vector2 _scroll;
		private bool _clickedHistoryEntry;
		private bool _isModal;
		
		private GUIStyle _scrollbarStyle;
		private GUIStyle _listStyle;
		private GUIStyle _navStyle;
		private GUIContent _popoutButtonContent;

		private const int ENTRY_HEIGHT = 16;
		private const string TOOLBAR_BUTTON_ICON = "d_UnityEditor.AnimationWindow";
		private const string POPOUT_ICON = "d_ScaleTool On";
		private const string TOOLBAR_BUTTON_TITLE = "Selection History";

#if UNITY_6000_0_OR_NEWER
		// Unity 6 main toolbar via the supported API (Toolbar/MainToolbar).
		// The previous reflection-based ToolbarExtender injection is flagged as
		// an "unsupported method" by Unity 6 and hidden by default.
		[MainToolbarElement("Bedtime/EditorHistory/SelectionHistory", defaultDockPosition = MainToolbarDockPosition.Right)]
		public static MainToolbarElement SelectionHistoryToolbarButton()
		{
			var icon = EditorGUIUtility.IconContent(TOOLBAR_BUTTON_ICON).image as Texture2D;
			var content = new MainToolbarContent(icon, TOOLBAR_BUTTON_TITLE);
			return new MainToolbarButton(content, ShowModalWindowAtCursor);
		}
#endif

		[MenuItem("Window/General/Selection History")]
		private static void ShowFromMenu()
		{
			var window = EditorWindow.GetWindow<EditorHistoryWindow>(false, TOOLBAR_BUTTON_TITLE, true);
			window._scroll = Vector2.up * int.MaxValue;
		}

		private static void ShowModalWindowAtCursor()
		{
			var window = CreateInstance<EditorHistoryWindow>();
			var size = new Vector2(200, 300);
			var mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
			var screenMouse = GUIUtility.GUIToScreenPoint(mouse);
			var pos = new Rect(screenMouse.x - size.x, screenMouse.y - 24, size.x, size.y);
			window._scroll = Vector2.up * int.MaxValue;
			window._isModal = true;
			window.ShowAsDropDown(pos, size);
		}
		
		private void ShowPopoutWindow()
		{
			var window = CreateInstance<EditorHistoryWindow>();
			var rect = position;
			rect.size = rect.size * 1.2f;
			window.Show();
			window.position = rect;
		}
		
		private void OnEnable()
		{
			_listStyle = null;
			_popoutButtonContent = EditorGUIUtility.TrTextContentWithIcon(string.Empty, "Pop Out", POPOUT_ICON);
			titleContent = EditorGUIUtility.TrTextContentWithIcon(TOOLBAR_BUTTON_TITLE, TOOLBAR_BUTTON_ICON);
			EditorHistory.OnHistoryUpdated += OnHistoryChanged;
			Selection.selectionChanged += Repaint;
		}

		private void OnDisable()
		{
			EditorHistory.OnHistoryUpdated -= OnHistoryChanged;
			Selection.selectionChanged -= Repaint;
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			DrawModalToolbar();
			DrawList();
			EditorGUILayout.EndVertical();
		}

		private void DrawModalToolbar()
		{
			if (!_isModal)
			{
				return;
			}
			
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.Label(TOOLBAR_BUTTON_TITLE, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(_popoutButtonContent, EditorStyles.toolbarButton))
			{
				ShowPopoutWindow();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void OnHistoryChanged(int location)
		{
			if (!_clickedHistoryEntry)
			{
				_scroll.y = (location * ENTRY_HEIGHT) - ENTRY_HEIGHT;
			}
			Repaint();
		}

		private void DrawList()
		{
			var selectedColor = new Color(.6f, .6f, .6f, 1f);
			var orgBGColor = GUI.backgroundColor;

			_scroll = GUILayout.BeginScrollView(_scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
			EditorGUILayout.BeginVertical();
			for (int i = 0; i < EditorHistory.HistoryObjects.Count; i++)
			{
				var entry = EditorHistory.HistoryObjects[i];
				if (!entry.Exists)
				{
					GUI.enabled = false;
				}

				if (i == EditorHistory.Location)
				{
					GUI.backgroundColor = selectedColor;
				}
				
				if (GUILayout.Button(entry.GUIContent, ListStyle))
				{
					_clickedHistoryEntry = true;
					EditorHistory.SetSelection(i);
					_clickedHistoryEntry = false;
				}
				GUI.backgroundColor = orgBGColor;
				GUI.enabled = true;
			}
			EditorGUILayout.EndVertical();
			GUILayout.EndScrollView();
		}

		private GUIStyle ListStyle
		{
			get
			{
				if (_listStyle != null)
				{
					return _listStyle;
				}

				_listStyle = new GUIStyle(EditorStyles.toolbarButton)
				{
					alignment = TextAnchor.MiddleLeft, 
					fixedHeight = ENTRY_HEIGHT,
				};
				return _listStyle;
			}
		}
	}
}