#if UNITY_2022_2_OR_NEWER
#define UNITY_MOUSE_SHORTCUTS_SUPPORTED
using UnityEditor.ShortcutManagement;
#endif
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BedtimeCore.EditorHistory
{
    [InitializeOnLoad]
	public class EditorHistory
	{
        static EditorHistory()
		{
			#if !UNITY_MOUSE_SHORTCUTS_SUPPORTED
			EditorApplication.update += ReadInput;
			#endif
			EditorSceneManager.sceneOpened += (scene, _) => OnSceneOpened(scene);
			EditorSceneManager.sceneSaved += OnSceneSaved;
			EditorApplication.quitting += () => History.Save();

            EditorApplication.delayCall += () =>
            {
	            OnSceneOpened(SceneManager.GetActiveScene());
	            RegisterHistorySelector(new UnityObjectSelector());
	            AddObject(Selection.activeObject);
            };
		}

        public static void RegisterHistorySelector(IHistorySelector selector)
        {
            _historySelectors.Add(selector);
            selector.AddToHistory += AddObject;
        }

        private static bool IsNavigating { get; set; }

        private static readonly List<IHistorySelector> _historySelectors = new List<IHistorySelector>();

        internal static event Action<int> OnHistoryUpdated;

        internal static List<HistoryObject> HistoryObjects => History.HistoryObjects;

        internal static int Location
		{
			get => IsNavigating ? History.Location : HistoryObjects.Count - 1;

			private set
			{
				History.Location = ClampLocation(value);
				OnHistoryUpdated?.Invoke(Location);
			}
		}

		internal static void SetSelection(int location, bool setActive = true)
		{
			EditorWindow focus = EditorWindow.focusedWindow;
			if (Application.isPlaying && focus != null)
			{
				var focusType = focus.GetType();
				if (focusType.FullName == _gameViewTypeName)
				{
					return;
				}
			}

            if (HistoryObjects.Count <= 0)
            {
                return;
            }

            IsNavigating = setActive;
            _selectionWasSet = setActive;
            foreach (var historySelector in _historySelectors)
            {
                var obj = HistoryObjects[ClampLocation(location)];
                if (obj.Exists)
                {
                    var selection = obj.Selection;
                    if(historySelector.Select(selection))
                    {
                        historySelector.Select(selection);
                        break;
                    }
                }
            }
            Location = location;
        }

		private static void Navigate(NavigationDirection direction, int amount = 1)
		{
			if (!InternalEditorUtility.isApplicationActive)
			{
				return;
			}
			
			var totalAmount = amount * (direction == NavigationDirection.Forward ? 1 : -1);
            if(Mathf.Abs(totalAmount) > History.HistoryObjects.Count)
            {
                return;
            }
            
			if (!IsNavigating)
			{
				SetSelection(HistoryObjects.Count - 1);
			}
			else if (Location - ClampLocation(Location + totalAmount) != 0)
			{
				var target = HistoryObjects[ClampLocation(Location + totalAmount)];
				if (!target.Exists)
				{
					Navigate(direction, amount + 1);
				}
				else
				{
					SetSelection(Location + totalAmount);
				}
			}
		}

		private static int ClampLocation(int location)
		{
			location = location >= HistoryObjects.Count - 1 ? HistoryObjects.Count - 1 : location;
			location = location < 0 ? 0 : location;
			return location;
		}


		
		private static void ClearInFront()
		{
			for (int i = HistoryObjects.Count - 1; i > Location; i--)
			{
				HistoryObjects.RemoveAt(i);
			}
		}

		private static void AddObject(object obj)
		{
            IsNavigating = obj != null;
            
            if (_selectionWasSet)
            {
                _selectionWasSet = false;
                return;
            }
            
			if (obj == null)
			{
				SetSelection(HistoryObjects.Count - 1, false);
				return;
			}

			var entry = new HistoryObject(obj);
			ClearInFront();
			if (HistoryObjects.Count > 0 && HistoryObjects[ClampLocation(Location)].Selection == entry.Selection)
			{
				return;
			}

			HistoryObjects.Add(entry);
			Location = HistoryObjects.Count - 1;
		}

		private static void OnSceneOpened(Scene scene)
		{
			for (var i = 0; i < HistoryObjects.Count; i++)
			{
				HistoryObjects[i] = HistoryObjects[i].UpdateSelection(scene.name);
			}
		}

		private static void OnSceneSaved(Scene scene)
		{
			for (var i = 0; i < HistoryObjects.Count; i++)
			{
				HistoryObjects[i] =	HistoryObjects[i].UpdateName();
			}
		}

		private static History History => History.instance;

		private static readonly string _gameViewTypeName = "UnityEditor.GameView";

		private static bool _selectionWasSet;
		
#if UNITY_MOUSE_SHORTCUTS_SUPPORTED
		[Shortcut("Navigate backwards in selection history", KeyCode.Mouse3)]
		private static void NavigateBackShortcut() => Navigate(NavigationDirection.Backward);

		[Shortcut("Navigate forwards in selection history", KeyCode.Mouse4)]
		private static void NavigateForwardShortcut() => Navigate(NavigationDirection.Forward);
#elif UNITY_EDITOR_WIN
		
		private static readonly Dictionary<NavigationDirection, bool> _navigationLastState = new Dictionary<NavigationDirection, bool>()
		{
			{NavigationDirection.Backward, false},
			{NavigationDirection.Forward, false},
		};

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(ushort virtualKeyCode);

		private static bool GetKey(NavigationDirection direction)
		{
			return (GetAsyncKeyState((ushort) direction) & (1 << 16)) != 0;
		}

		private static void ReadInput()
		{
			if (!InternalEditorUtility.isApplicationActive)
			{
				return;
			}

			bool forward = GetKey(NavigationDirection.Forward);
			bool backward = GetKey(NavigationDirection.Backward);

			if (backward && !_navigationLastState[NavigationDirection.Backward])
			{
				Navigate(NavigationDirection.Backward);
			}

			if (forward && !_navigationLastState[NavigationDirection.Forward])
			{
				Navigate(NavigationDirection.Forward);
			}

			_navigationLastState[NavigationDirection.Forward] = forward;
			_navigationLastState[NavigationDirection.Backward] = backward;
		}
#endif
	}
}