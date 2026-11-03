using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BedtimeCore.EditorHistory
{
	[Serializable, FilePath("EditorHistory/History.txt", FilePathAttribute.Location.PreferencesFolder)]
	internal class History : ScriptableSingleton<History>
	{
		private const int HISTORY_MAX = 128;
		
		[SerializeField]
		private int _location;

		[SerializeField]
		private List<HistoryObject> _historyObjects = new List<HistoryObject>(32);

		public int Location
		{
			get => _location;
			set => _location = value;
		}

		public void Save()
		{
			if (_historyObjects.Count > HISTORY_MAX)
			{
				var toRemove = _historyObjects.Count - HISTORY_MAX;
				_historyObjects.RemoveRange(0, toRemove);
			}
			Save(false);
		}

		public List<HistoryObject> HistoryObjects => _historyObjects;
	}
}