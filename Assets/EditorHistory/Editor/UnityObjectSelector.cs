using System;
using UnityEditor;

namespace BedtimeCore.EditorHistory
{
    public class UnityObjectSelector : IHistorySelector
    {
        public UnityObjectSelector()
        {
            Selection.selectionChanged += () => AddToHistory?.Invoke(Selection.activeObject);    
        }
        
        public Type Type => typeof(UnityEngine.Object);
        
        public bool Select(object selection)
        {
            if(EditorApplication.isPlaying && EditorWindow.focusedWindow != null)
            {
                var name = EditorWindow.focusedWindow.GetType().Name;
                if(name == "GameView")
                {
                    return false;
                }
            }
            
            if(selection is UnityEngine.Object obj)
            {
                Selection.activeObject = obj;
                return true;
            }
            return false;
        }

        public event Action<object> AddToHistory;
    }
}