using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Den.Tools;
using MapMagic.Nodes;

namespace MapMagic.Expose
{
	[Serializable]
	public class Exposed : ISerializationCallbackReceiver
	{
		[Serializable]
		public class Entry : ISerializationCallbackReceiver
		{
			public ulong id;
			public string name;
			public int channel = -1;
			public int arrIndex = -1;
			
			public string expression; //serializes calculator and stores expression in an original form

			[NonSerialized] public Type type;  //always matches the field type. Evel if one channel of Vector3 exposed - it will be Vector3
			[NonSerialized] public Calculator calculator;


			public Entry () {} //for serializer

			public Entry (ulong id, string name, int channel, int arrIndex, string expression, Type type, Calculator calculator)
				{ this.id = id; this.name=name; this.channel=channel; this.arrIndex=arrIndex; this.expression=expression; this.type=type; this.calculator=calculator; }

			public Entry (ulong id, string name, int channel, int arrIndex, string expression, Type type)
				{ this.id = id; this.name=name; this.channel=channel; this.arrIndex=arrIndex; this.expression=expression; this.type=type; calculator=Calculator.Parse(expression); }

			public Entry (ulong id, string name, string expression, Type type, Calculator calculator)
				{ this.id = id; this.name=name; this.channel=-1; this.expression=expression; this.type=type; this.calculator=calculator; }

			public Entry (ulong id, string name, string expression, Type type)
				{ this.id = id; this.name=name; this.channel=-1; this.expression=expression; this.type=type; calculator=Calculator.Parse(expression); }


			#region Serialize

				[SerializeField] private string serType;

				public void OnBeforeSerialize () 
				{ 
					serType = type.AssemblyQualifiedName;  
				}

				public void OnAfterDeserialize () 
				{ 
					if (serType==null) return;
					type = Type.GetType(serType);  
					calculator = Calculator.Parse(expression); 
				}

			#endregion
		}

		[NonSerialized] Dictionary<ulong,Entry[]> idToExpressions = new Dictionary<ulong,Entry[]>();
			//used as multi-level dict: id -> name -> channel


		public Entry this[ulong id, string name, int channel=-1, int arrIndex=-1]
		/// Returns null if entry is not listed
		{get{
			Entry[] entries;
			if (!idToExpressions.TryGetValue(id, out entries))
				return null;

			else
			{
				Entry exp = entries.FindMember(e => e.name==name && e.channel==channel && e.arrIndex==arrIndex);
				return exp;
			}
		}}
		

		public Entry this[Entry entry]
		{set{
			Entry[] entries = idToExpressions[entry.id];
			int num = entries.Find(e => e.name==entry.name && e.channel==entry.channel && e.arrIndex==entry.arrIndex);
				
			if (num >= 0)
				entries[num] = value;
			else
				throw new Exception ("Value is not contained in array");
		}}


		public Entry[] this[ulong id]
		/// Returns null if entry is not listed
		{get{
			if (!idToExpressions.TryGetValue(id, out Entry[] entries))
				return entries;
			else
				return null;
		}}


		public string GetExpression (ulong id, string name, int channel=-1, int arrIndex=-1)
		/// If we don't want to deal with entries
		{
			Entry entry = this[id, name, channel, arrIndex];
			return entry!=null ? entry.expression : null;
		}

		public Calculator GetCalculator (ulong id, string name, int channel=-1, int arrIndex=-1)
		/// If we don't want to deal with entries
		{
			Entry entry = this[id, name, channel, arrIndex];
			return entry!=null ? entry.calculator : null;
		}

		public Type GetType (ulong id, string name, int channel=0)
		/// If we don't want to deal with entries
		{
			Entry entry = this[id,name,channel];
			return entry!=null ? entry.type : null;
		}


		public void Add (Entry entry, bool overwrite=false)
		/// If overwrite could be used as ForceAdd
		{
			Entry[] entries;
			if (!idToExpressions.TryGetValue(entry.id, out entries))
			{
				entries = new Entry[1];
				entries[0] = entry;
				idToExpressions.Add(entry.id, entries);
			}
			else
			{
				int num = entries.Find(e => e.name==entry.name && e.channel==entry.channel && e.arrIndex==entry.arrIndex);
				if (num >= 0)
				{
					if (overwrite) entries[num] = entry;
					else
						throw new Exception ("Can't add value since it's already in array");
				}
				else
				{
					ArrayTools.Add(ref entries, entry);
					idToExpressions[entry.id] = entries; //ArrayTools ref does not update dictionary
				}
			}
		}


		public void AddRange (Entry[] entries, bool overwrite=false)
		/// Adds exposed values for one gen only
		{
			foreach (Entry entry in entries)
				Add(entry, overwrite);
		}


		public void AddRange (Exposed other, bool overwrite=false)
		/// Adds exposed values from other graph (for duplicating or copy/paste generators)
		{
			foreach (Entry[] entries in other.idToExpressions.Values)
				foreach (Entry entry in entries)
					Add(entry, overwrite);
		}


		public bool Remove (ulong id, string name, int channel, int arrIndex)
		/// Unexpose one entry only
		/// True if entry was really removed
		{
			Entry[] exps;
			if (!idToExpressions.TryGetValue(id, out exps))
				return false;

			else
			{
				int num = exps.Find(e => e.name==name && e.channel==channel && e.arrIndex==arrIndex);
				if (num >= 0) 
				{
					ArrayTools.RemoveAt(ref exps, num);
					idToExpressions[id] = exps; //ref from dict doesn't follow

					return true;
				}
			}

			return false;
		}


		public bool Remove (ulong id, string name, int arrIndex)
		/// Unexpose all entries with the same name, no matter of their channel
		{
			Entry[] exps;
			if (!idToExpressions.TryGetValue(id, out exps))
				return false;

			else
			{
				bool found = false;
				while (true) //there could be several channels with this name
				{
					int num = exps.Find(e => e.name==name && e.arrIndex==arrIndex);

					if (num >= 0) 
					{
						ArrayTools.RemoveAt(ref exps, num);
						idToExpressions[id] = exps; //ref from dict doesn't follow
						found = true;
					}

					else
						break;
				}

				return found;
			}
		}


		public bool Remove (ulong id)
		/// Unexpose all of the gen (on gen remove or fn graph reassign)
		/// /// True if entry was really removed
		{
			if (idToExpressions.ContainsKey(id))
			{
				idToExpressions.Remove(id);
				return true;
			}

			return false;
		}

		public bool Contains (ulong id) => idToExpressions.ContainsKey(id);

		public bool Contains (ulong id, string name) //contains any channel with given name
		{
			Entry[] entries;
			if (!idToExpressions.TryGetValue(id, out entries))
				return false;
			else
				return entries.Contains(e => e.name==name);
		}

		public bool Contains (ulong id, string name, int channel, int arrIndex)
		{
			Entry[] entries;
			if (!idToExpressions.TryGetValue(id, out entries))
				return false;
			else
			{
				int num = entries.Find(e => e.name==name && e.channel==channel && e.arrIndex==arrIndex);
				return num >= 0;
			}
		}

		
		public int Count
		{get{
			int count = 0;
			foreach (Entry[] entries in idToExpressions.Values)
				count += entries.Length;
			return count;
		}}


		public IEnumerable<Entry> EntriesById (ulong id)
		{
			if (!idToExpressions.TryGetValue(id, out Entry[] exps))
				yield break;

			foreach (Entry entry in exps)
				yield return entry;
		}

		public IEnumerable<Entry> EntiriesByReference (string reference)
		{
			foreach (Entry[] exps in idToExpressions.Values)
				foreach (Entry entry in exps)
				{
					if (entry.calculator.ContainsReference(reference))
						yield return entry;
				}

		}

		public IEnumerable<Entry> AllEntries ()
		{
			foreach (Entry[] exps in idToExpressions.Values)
				foreach (Entry entry in exps)
					yield return entry;
		}

		public IEnumerable<ulong> AllIds ()
		{
			foreach (ulong key in idToExpressions.Keys)
				yield return key;
		}

		public IEnumerable<IUnit> AllUnits (Graph graph)
		{
			foreach (IUnit unit in graph.AllUnits())
				if (idToExpressions.ContainsKey(unit.Id))
					yield return unit;
		}


		public void ReplaceIds (Dictionary<ulong,ulong> oldNewIds)
		/// Changes the ids of exposed values (when chnging gen id on duplicating or copy/paste generators)
		{
			ulong[] oldIds = oldNewIds.Keys.ToArray();
			ulong[] newIds = oldNewIds.Values.ToArray();

			for (int i=0; i<oldIds.Length; i++)
			{
				ulong oldId = oldIds[i];
				ulong newId = newIds[i];

				if (idToExpressions.TryGetValue(oldId, out Entry[] entries))
				{
					if (idToExpressions.ContainsKey(newId))
						throw new Exception("ReplaceIds: idToExpressions already contains this newid: " + newId);

					foreach (Entry entry in entries)
						entry.id = newId;

					idToExpressions.Remove(oldId);
					idToExpressions.Add(newId, entries);
				}
			}
		}


		public void RemoveUnused (Graph graph)
		/// Clears exposed expressions that do not have generator related with them
		{
			HashSet<ulong> unused = new HashSet<ulong>(idToExpressions.Keys);
			
			foreach (IUnit unit in graph.AllUnits())
				unused.Remove(unit.Id);

			foreach (ulong uid in unused)
				idToExpressions.Remove(uid);
		}


		public IEnumerable<IUnit> UnitsByReference (Graph graph, string reference)
		/// Iterates generators in graph that has expose with this variable name
		{
			foreach (IUnit unit in graph.AllUnits())
			{
				if (idToExpressions.TryGetValue(unit.Id, out Entry[] exps))
				{
					foreach (Entry entry in exps)
						if (entry.calculator.ContainsReference(reference))
						{
							yield return unit;
							break;
						}
				}
			}
		}


		#region Serialization

			[SerializeField] private Entry[] serEntries; 

			public void OnBeforeSerialize ()
			{
				serEntries = new Entry[Count];

				int i=0;
				foreach (Entry entry in AllEntries())
				{
					serEntries[i] = entry;
					i++;
				}
			}

			public void OnAfterDeserialize ()
			{
				idToExpressions.Clear(); //just in case
				
				if (serEntries==null) 
					return;

				foreach (Entry entry in serEntries)	
					Add(entry);
			}

		#endregion
	}
}