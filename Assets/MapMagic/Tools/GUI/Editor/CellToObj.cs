using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Den.Tools.GUI
{
	public class CellObjs
	{
		// id (string)  -> cell -> object
		//				-> object -> cell

		private class TwoWayDict
		{
			public Dictionary<Cell, object> cellToObj = new Dictionary<Cell, object>();
			public Dictionary<object, Cell> objToCell = new Dictionary<object, Cell>();
		}

		private Dictionary<string, TwoWayDict> dict = new Dictionary<string, TwoWayDict>();


		public void ForceAdd (object obj, Cell cell, string id=null)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
			{
				twd = new TwoWayDict();
				dict.Add(id, twd);
			}

			if (!twd.cellToObj.ContainsKey(cell)) twd.cellToObj.Add(cell, obj);
			else twd.cellToObj[cell] = obj;

			if (!twd.objToCell.ContainsKey(obj)) twd.objToCell.Add(obj, cell);
			else twd.objToCell[obj] = cell;
		}


		public bool TryGetObject<T> (Cell cell, string id, out T tobj)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				{ tobj=default; return false; }

			if (!twd.cellToObj.TryGetValue(cell, out object obj)) 
				{ tobj=default; return false; }

			if (!(obj is T))
				{ tobj=default; return false; }

			tobj = (T)obj;
			return true;
		}


		public bool TryGetCell (object obj, string id, out Cell cell)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				{ cell=null; return false; }

			if (!twd.objToCell.TryGetValue(obj, out Cell objcell)) 
				{ cell=null; return false; }

			cell = objcell;
			return true;
		}


		public T GetObject<T> (Cell cell, string id=null)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				return default;

			if (!twd.cellToObj.TryGetValue(cell, out object obj)) 
				return default;

			return (T)obj;
		}


		public Cell GetCell (object obj, string id)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				return null;

			if (!twd.objToCell.TryGetValue(obj, out Cell cell)) 
				return null;

			return cell;
		}


		public bool ContainsCell (Cell cell, string id)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				return false;

			return twd.cellToObj.ContainsKey(cell);
		}


		public IEnumerable<object> GetAllCells (string id)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				yield break;

			foreach (Cell cell in twd.cellToObj.Keys)
				yield return cell;
		}

		public IEnumerable<T> GetAllObjects<T> (string id)
		{
			if (!dict.TryGetValue(id, out TwoWayDict twd))
				yield break;

			foreach (object obj in twd.objToCell.Keys)
				yield return (T)obj;
		}


		public void Clear ()
		{
			foreach (TwoWayDict twd in dict.Values)  //leaving ids to avoid creating garbage
			{
				twd.cellToObj.Clear();
				twd.objToCell.Clear();
			}
		}
	}
}