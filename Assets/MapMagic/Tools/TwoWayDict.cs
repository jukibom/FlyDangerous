using System.Collections.Generic;

namespace Den.Tools 
{
	public class DictionaryBidirectional<T1,T2>
	{
		public Dictionary<T1,T2> d1 = new Dictionary<T1,T2>();
		public Dictionary<T2,T1> d2 = new Dictionary<T2,T1>();

		public void Add (T1 t1, T2 t2)
		{
			d1.Add(t1, t2);
			d2.Add(t2, t1);
		}

		public void Add (T2 t2, T1 t1)
		{
			d1.Add(t1, t2);
			d2.Add(t2, t1);
		}

		public void ForceAdd (T1 t1, T2 t2)
		{
			if (d1.ContainsKey(t1)) d1[t1] = t2; 
			else d1.Add(t1, t2);

			if (d2.ContainsKey(t2)) d2[t2] = t1; 
			else d2.Add(t2, t1);
		}

		public void ForceAdd (T2 t2, T1 t1)
		{
			if (d1.ContainsKey(t1)) d1[t1] = t2; 
			else d1.Add(t1, t2);

			if (d2.ContainsKey(t2)) d2[t2] = t1; 
			else d2.Add(t2, t1);
		}

		public T2 this[T1 t1] 
		{
			get{ return d1[t1]; } 
			set{ d1[t1] = value; }
		}

		public T1 this[T2 t2] 
		{
			get{ return d2[t2]; } 
			set{ d2[t2] = value; }
		}

		public bool TryGetValue (T1 t1, out T2 t2) { return d1.TryGetValue(t1, out t2); }
		public bool TryGetValue (T2 t2, out T1 t1) { return d2.TryGetValue(t2, out t1); }

		public bool ContainsKey (T1 t1) { return d1.ContainsKey(t1); }
		public bool ContainsKey (T2 t2) { return d2.ContainsKey(t2); }

		public void Clear ()
		{
			d1.Clear();
			d2.Clear();
		}
	}
}
