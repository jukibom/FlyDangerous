using System;
using System.Collections;
using System.Collections.Generic;

namespace Den.Tools 
{
	public class DictionaryOrdered<TKey,TValue> : 
		IDictionary<TKey, TValue>,	IList<TKey>,
		ICollection<KeyValuePair<TKey, TValue>>,	ICollection<TKey>,		ICollection, 
		IEnumerable<KeyValuePair<TKey, TValue>>,	IEnumerable<TKey>,		IEnumerable, 
		IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyCollection<TKey>
		
	/// Maintains the order of keys, and has access by number
	{
		protected Dictionary<TKey,TValue> dict = new Dictionary<TKey, TValue>();
		protected List<TKey> order = new List<TKey>();

		public DictionaryOrdered () { }
		public DictionaryOrdered (IEqualityComparer<TKey> comparer) { dict=new Dictionary<TKey,TValue>(comparer); order=new List<TKey>(); }
		public DictionaryOrdered (int capacity) { dict=new Dictionary<TKey, TValue>(capacity); order=new List<TKey>(capacity); }
		public DictionaryOrdered (int capacity, IEqualityComparer<TKey> comparer) { dict=new Dictionary<TKey,TValue>(capacity,comparer); order=new List<TKey>(capacity); }
		public DictionaryOrdered (DictionaryOrdered<TKey,TValue> other) { dict=new Dictionary<TKey,TValue>(other.dict); order=new List<TKey>(other.order); }

		public Dictionary<TKey,TValue>.ValueCollection Values => dict.Values;
		public Dictionary<TKey,TValue>.KeyCollection Keys => dict.Keys;
		ICollection<TValue> IDictionary<TKey, TValue>.Values => dict.Values;
		ICollection<TKey> IDictionary<TKey, TValue>.Keys => dict.Keys;
		public int Count => order.Count;
		public IEqualityComparer<TKey> Comparer => dict.Comparer;

		public TValue this[TKey key] 
		{ 
			get => dict[key]; 
			set => dict[key]=value; 
		}

		public TValue this[int num] 
		{ 
			get => dict[order[num]]; 
			set => dict[order[num]]=value; 
		}

		TKey IList<TKey>.this[int num] 
		{ 
			get => order[num]; 
			set => order[num]=value; 
		}

		public TKey GetKeyByNum(int num) => order[num];
		public void SetKeyByNum(int num, TKey val) => order[num] = val;

		public bool TryGetValue (TKey key, out TValue value) => dict.TryGetValue(key, out value);


		public void Add (TKey key, TValue value)
		{
			dict.Add(key, value); //dict goes first just to avoid adding to order on excepton
			order.Add(key);
		}

		public void Add (KeyValuePair<TKey,TValue> kvp)
		{
			dict.Add(kvp.Key, kvp.Value);
			order.Add(kvp.Key);
		}

		public void Add (TKey key)  /// Adds default value
		{
			dict.Add(key, default(TValue));
			order.Add(key);
		}

		public bool TryAdd (TKey key, TValue value)
		{
			if (dict.ContainsKey(key)) return false;

			dict.Add(key, value); //dict goes first just to avoid adding to order on excepton
			order.Add(key);
			return true;
		}

		public void Insert (int index, TKey key, TValue value)
		{
			dict.Add(key, value);
			order.Insert(index, key);
		}

		public void Insert (int index, TKey key)  /// Adds default value
		{
			dict.Add(key, default(TValue));
			order.Insert(index, key);
		}

		public int IndexOf (TKey key) => order.IndexOf(key);

		public void Clear ()
		{
			dict.Clear();
			order.Clear();
		}

		public bool Remove (TKey key)
		{
			if (!dict.ContainsKey(key))
				return false;

			int index = order.IndexOf(key);
			if (index<0)
				return false;

			dict.Remove(key);
			order.RemoveAt(index);
			return true;
		}

		public bool Remove (KeyValuePair<TKey,TValue> kvp)
		{
			if (!((ICollection<KeyValuePair<TKey,TValue>>)dict).Contains(kvp))
				return false;

			int index = order.IndexOf(kvp.Key);
			if (index<0)
				return false;

			dict.Remove(kvp.Key);
			order.RemoveAt(index);
			return true;
		}

		public void RemoveAt (int index)
		{
			dict.Remove(order[index]);
			order.RemoveAt(index);
		}

		public void Switch (int n1, int n2)
		{
			TKey temp = order[n1];
			order[n1] = order[n2];
			order[n2] = temp;
		}


		public bool ContainsKey (TKey key) => dict.ContainsKey(key);
		public bool ContainsValue (TValue value) => dict.ContainsValue(value);
		public bool Contains (TKey key) => dict.ContainsKey(key);
		public bool Contains (KeyValuePair<TKey,TValue> kvp) => ((ICollection<KeyValuePair<TKey,TValue>>)dict).Contains(kvp);

		public void CopyTo (Array array, int index)
		{	
			int max = array.Length<(order.Count+index) ? array.Length : order.Count+index;
			for (int i=index; i<max; i++)
			{
				TKey key = order[i-index];
				array.SetValue(new KeyValuePair<TKey,TValue> (key, dict[key]), i);
			}
		}

		public void CopyTo (KeyValuePair<TKey,TValue>[] array, int index) => CopyTo((Array)array, index);
		public void CopyTo (TKey[] array, int index) => order.CopyTo(array,index);


		bool ICollection.IsSynchronized => ((ICollection)dict).IsSynchronized;
		object ICollection.SyncRoot => ((ICollection)dict).SyncRoot;
		public bool IsReadOnly => ((ICollection<KeyValuePair<TKey,TValue>>)dict).IsReadOnly && ((ICollection<TKey>)order).IsReadOnly;

		public IEnumerator<KeyValuePair<TKey,TValue>> GetEnumerator()
		{
			foreach (TKey key in order)
				yield return new KeyValuePair<TKey,TValue> (key, dict[key]);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (TKey key in order)
				yield return new KeyValuePair<TKey,TValue> (key, dict[key]);
		}
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => order.GetEnumerator();


		#region Extensions

			public DictionaryOrdered (TKey[] keys, TValue[] values)
			/// Creates new dictionary from keys and values
			/// Doesn't check keys array for duplicates
			{
				if (keys.Length != values.Length)
					throw new Exception("Could not create ordered dictionary: keys and values lengths differ");

				dict=new Dictionary<TKey, TValue>(keys.Length); 
				for (int i=0; i<keys.Length; i++)
					dict.Add(keys[i], values[i]);

				order=new List<TKey>(keys.Length);
				order.AddRange(keys);
			}


			public DictionaryOrdered (TKey[] keys)
			/// Creates new empty dictionary with keys structure. Fills all values as null
			/// Doesn't check keys array for duplicates
			{
				dict=new Dictionary<TKey, TValue>(keys.Length); 
				order=new List<TKey>(keys.Length);

				dict=new Dictionary<TKey, TValue>(keys.Length); 
				for (int i=0; i<keys.Length; i++)
					dict.Add(keys[i], default);

				order=new List<TKey>(keys.Length);
				order.AddRange(keys);
			}


			public void TakeMatchingValuesFrom (DictionaryOrdered<TKey,TValue> src)
			/// If src has a same key it's values is copied to the dict
			{
				foreach (TKey key in order)
				{
					if (src.dict.TryGetValue(key, out TValue val))
						dict[key] = val; //iterating order, so dict should not change
				}
			}

		#endregion

		#region Serialization

			public (TKey[], TValue[]) Serialize ()
			{
				int count = Count;
				TKey[] keys = new TKey[Count];
				TValue[] vals = new TValue[Count];

				for (int i=0; i<count; i++)
				{
					TKey key = order[i];
					keys[i] = key;
					vals[i] = dict[key];
				}

				return (keys, vals);
			}

			public void Serialize (ref TKey[] keys, ref TValue[] vals) 
			{
				int count = Count;
				if (keys.Length != count) keys = new TKey[Count];
				if (vals.Length != count) vals = new TValue[Count];

				for (int i=0; i<count; i++)
				{
					TKey key = order[i];
					keys[i] = key;
					vals[i] = dict[key];
				}
			}

			public void Deserialize (TKey[] keys, TValue[] values)
			{
				if (keys.Length != values.Length)
					throw new Exception("Could not deserialize ordered dictionary: keys and values lengths differ");

				Clear();

				for (int i=0; i<keys.Length; i++)
					Add(keys[i], values[i]);
			}

			public void ReCreateDictionary ()
			/// Unity 2021.2 Beta cannot use re-serialized dictionary. Cannot find key that definately in it. Probably some compared serialization issue.
			/// This one re-creates dictionary from scratch
			{
				var newdict = new Dictionary<TKey, TValue>();
				foreach (var kvp in dict)
					newdict.Add(kvp.Key, kvp.Value);
				dict = newdict;
			}

		#endregion
	}
}