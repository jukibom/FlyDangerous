using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

namespace Den.Tools.GUI
{
	public class Undo
	{
    	public UnityEngine.Object undoObject;
		public string undoName;
		public Action undoAction;
		public string lastUndoName;  //the last undo group name (kept to know it on UndoRedoPerformed)

		public Undo ()
		{
			UnityEditor.Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			UnityEditor.Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}


		public void OnUndoRedoPerformed ()
		{
			string currGroupName = UnityEditor.Undo.GetCurrentGroupName();

			if (currGroupName == undoName || currGroupName == lastUndoName)
			// a bit hacky here. On undoRedoPerformed there is already no current group in stack, and no way to get current group name
			// so we store previous (before mm change) name and performing undo if this name is first in stack
			// TODO: use undo from MapMagicBrush with ids, it's more stable
			{
				EditorWindow.focusedWindow?.Repaint();
				undoAction?.Invoke();

				if (currGroupName == lastUndoName)
					lastUndoName = null;
			}
		}

		public void Record (bool completeUndo=false)
		{
			if (undoObject==null) return;

			string currGroupName = UnityEditor.Undo.GetCurrentGroupName();
			if (currGroupName != undoName)
				lastUndoName = currGroupName;

			if (completeUndo) UnityEditor.Undo.RegisterCompleteObjectUndo(undoObject, undoName);
			else UnityEditor.Undo.RecordObject(undoObject, undoName);

			EditorUtility.SetDirty(undoObject);
		}
	}
}
