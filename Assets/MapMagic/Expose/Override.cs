using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Den.Tools;
using Den.Tools.Serialization;
using MapMagic.Nodes;

namespace MapMagic.Expose
{
	[Serializable]
	public class Override : ISerializationCallbackReceiver
	{
		[NonSerialized] private DictionaryOrdered<string,ObjType> dict = new DictionaryOrdered<string,ObjType>();
			//dictionary of structs - for each value type is determined by is/GetType
			//for unity objects using UnityObject

		private struct ObjType
		{
			public object obj;
			public Type type;

			public ObjType (object obj, Type type) { this.obj=obj; this.type=type; }
		}


		public Override () { }
		public Override (Override src) { dict = new DictionaryOrdered<string,ObjType>(src.dict); }


		public object this[string key] 
		{ 
			get => dict[key].obj;
			set => dict[key] = new ObjType(value, value.GetType());
		}


		public bool TryGetValue (string name, out Type type, out object obj)
		{
			if (dict.TryGetValue(name, out ObjType ot))
			{
				obj = ot.obj; type = ot.type;
				return true;
			}

			else 
			{ 
				obj=default; type=null; 
				return false; 
			}
		}


		public void Add (string name, Type type, object val) => dict.Add(name, new ObjType(val, type));

		public void AddDefault (string name, Type type)
		/// Adding default value (checking it's not null for value types)
		{
			object val = default;
			if (type.IsValueType)
				val = Activator.CreateInstance(type);

			Add(name, type, val);
		}

		public void SetOrAdd (string name, Type type, object val)
		/// Adds value if it's not in dict
		{
			if (dict.ContainsKey(name))
				dict[name] = new ObjType(val, type);
			else
				dict.Add(name, new ObjType(val, type));
		}

		public void SetIfContains (string name, Type type, object val)
		/// Sets only if name key is in dict and type match
		{
			if (dict.TryGetValue(name, out ObjType ot)  &&  ot.type == type)
				dict[name] = new ObjType(val, type);
		}

		public void RemoveAt (int num) => dict.RemoveAt(num);

		public void Switch (int n1, int n2) => dict.Switch(n1, n2);

		public int Count => dict.Count;

		public bool Contains (string name) => dict.Contains(name);

		public void Clear () => dict.Clear();


		public (string,Type,object) GetOverrideAt (int num)
		/// Reads name, dataand object at specified index
		{
			string name = ((IList<string>)dict)[num]; //default call returns value. Treating dict as list to get key
			ObjType ot = dict[name];

			return (name, ot.type, ot.obj);
		}

		public void SetOverrideAt (int num, string name, Type type, object obj)
		/// Re-writes previous name, data and object at specified index
		{
			string prevName = ((IList<string>)dict)[num];

			if (name != prevName) //removing entry completely
			{
				dict.RemoveAt(num);
				dict.Add(name, new ObjType(obj,type));
			}

			else
				dict[prevName] = new ObjType(obj,type);
		}


		public void AddExposed (Exposed.Entry entry, Graph graph=null)
		/// Adds all references from entry expression (there could be several). Tries to assign the generator fieldvalue to each of them
		/// Does nothing if reference already assigned
		{
			// assigning type of float for exposed channels
			Type assignType;
			if (entry.channel >= 0)
			{
				if (entry.type == typeof(Coord))
					assignType = typeof(int);
				else
					assignType = typeof(float);
			}
			else
				assignType = entry.type;

			//getting default value
			object defaultVal = null;
			if (graph != null)
			{
				defaultVal = GetFieldValue(graph, entry.id, entry.name);

				//convert to vec and back to set channel and just to make sure it's of proper type
				Calculator.Vector vec = new Calculator.Vector(defaultVal);
				if (entry.channel >= 0)  //picking the right channel if it's defined
					vec.Unify(entry.channel);
				defaultVal = vec.Convert(assignType);
			}

			//assigning
			while (true) //could be several variables in one expression (yep, all of them will get default value)
			{
				string assignName = entry.calculator.CheckOverrideAssign(this);
				if (assignName == null) //nothing more to assign
					break;

				if (defaultVal!=null)
					graph.defaults.Add(assignName, assignType, defaultVal);
				else
					graph.defaults.AddDefault(assignName, assignType);
			}
		}


		public void AddAllExposed (Exposed exp, Graph graph=null)
		/// Adds all exposed values (from graph) 
		{
			foreach (Exposed.Entry entry in exp.AllEntries())
				AddExposed(entry, graph);
		}


		public void RemoveAllUnused (Exposed exp)
		/// Removes all values that are not used in Expose
		{
			HashSet<string> allReferences = new HashSet<string>();

			foreach (Exposed.Entry entry in exp.AllEntries())
				foreach (string reference in entry.calculator.AllRefenrences())
					allReferences.Add(reference);

			List<string> refsToRemove = new List<string>();

			foreach (string reference in dict.Keys)
				if (!allReferences.Contains(reference))
					refsToRemove.Add(reference);

			foreach (string reference in refsToRemove)
				dict.Remove(reference);
		}


		private static object GetFieldValue (Graph graph, ulong id, string name)
		/// Gets generator original field value
		/// To find AddExposed default
		{
			IUnit unit = null;
			foreach (IUnit aunit in graph.AllUnits())
				if (aunit.Id == id)
					{ unit = aunit; break; }

			if (unit == null)
				return null;

			FieldInfo field = unit.GetType().GetField(name);
			if (field == null)
				return null;

			object val = field.GetValue(unit);
			return val;
		}


		public void Sync (Override ovd)
		/// Adds new overriden values (with ovd values) and removes values which are not in ovd
		/// Do not change values if they already exist
		{
			for (int i=dict.Count-1; i>=0; i--)
			{
				string name = ((IList<string>)dict)[i];
				Type type = dict[name].type;

				if (!ovd.dict.Contains(name)  ||  ovd.dict[name].type!=type) //removing if improper type too
					RemoveAt(i);
			}

			for (int i=0; i<ovd.dict.Count; i++)
			{
				string ovdName = ((IList<string>)ovd.dict)[i];

				if (!dict.Contains(ovdName))
					dict.Add(ovdName, ovd.dict[ovdName]);
			}
		}


		#region Serialization

			[SerializeField] private string serializedDict;
			[SerializeField] private UnityEngine.Object[] unityObjects;
			//name orders is serialized on itself

			public void OnBeforeSerialize ()
			{
				serializedDict = Den.Tools.Serialization.Serializer.Serialize(dict, out unityObjects);
			}

			public void OnAfterDeserialize ()
			{
				if (serializedDict != null  &&  unityObjects != null)
					dict = (DictionaryOrdered<string,ObjType>)Den.Tools.Serialization.Serializer.Deserialize(serializedDict, unityObjects);

				#if UNITY_2021_2_OR_NEWER
				// Unity 2021.2 Beta cannot use re-serialized dictionary. Cannot find key that definitely in it. Probably some compared serialization issue.
				// This one re-creates dictionary from scratch
				dict.ReCreateDictionary();
				#endif
			}

		#endregion
	}
}