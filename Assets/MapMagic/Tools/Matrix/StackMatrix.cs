using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Den.Tools.Matrices;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Tools.Tests.Editor")]

namespace Den.Tools
{
	[System.Serializable]
	public class StackMatrix<T>
	/// Matrix-like array with limited length, that holds only N elements
	/// Adds new ones with Set, and erases old ones when stack is full
	{
		[SerializeField] internal Dictionary<Coord, (uint,T)> stack = new Dictionary<Coord, (uint, T)>();  //coord to addNumber and T
		[SerializeField] private int count;
		[SerializeField] private int capacity = 10;
		[SerializeField] private uint lastId = 0;

		public StackMatrix() {}
		public StackMatrix (int capacity) {this.capacity=capacity;}

		public T this[Coord c] 
		{
			get 
			{ 
				stack.TryGetValue(c, out (uint,T) numT);
				return numT.Item2;
			}
			set 
			{
				lastId++;

				//coord already in stack - just replacing with new, and refreshing id
				if (stack.ContainsKey(c))
					stack[c] = (lastId, value);

				else
				{
					//capacity isn't reached - adding to stack
					if (count < capacity)
					{
						stack.Add(c, (lastId,value));
						count++;
					}

					//capacity reached - common case - removing old, adding new
					else
					{
						Coord lowerestCoord = GetCoordWithLowerestId();
						stack.Remove(lowerestCoord);
						stack.Add(c, (lastId,value));
					}
				}

				if (lastId >= 4294967294)
				{
					SetMaxId(2147483647);
					lastId = 2147483648;
				}
			}
		}


		public T this[int x, int z]
		{
			get => this[new Coord(x,z)];
			set => this[new Coord(x,z)] = value;
		}


		public int Count { get => count; }


		public int Capacity 
		{
			get {return capacity;}
			set
			{
				if (value < count)
					Limit(value);
				capacity = value;
			}
		}


		public bool Contains (Coord c) => stack.ContainsKey(c);
		public bool Contains (int x, int z) => stack.ContainsKey(new Coord(x,z));


		private void Limit (int num)
		///Leaves only num or less elements in stack
		{
			Coord[] sorted = SortByIds();

			Dictionary<Coord, (uint,T)> srcStack = new Dictionary<Coord, (uint, T)>(stack);
			stack.Clear();

			for (int i=sorted.Length-1; i>=sorted.Length-num; i--)
			{
				Coord c = sorted[i];
				(uint,T) el = srcStack[c];
				stack.Add(c, el);
			}

			count = num;
		}


		private Coord[] SortByIds ()
		///Returns all of the stack coordinates sorted by id nums from min (oldest) to max (newest)
		{
			Coord[] coords = new Coord[stack.Count];
			uint[] ids = new uint[stack.Count];

			int i=0;
			foreach (var kvp in stack)
			{
				coords[i] = kvp.Key;
				ids[i] = kvp.Value.Item1;
				i++;
			}

			Array.Sort(ids, coords);

			return coords;
		}


		private Coord GetCoordWithLowerestId ()
		{
			uint lowerestId = uint.MaxValue;
			Coord lowerestCoord = new Coord();

			foreach (var kvp in stack)
				if (kvp.Value.Item1 <= lowerestId)
				{
					lowerestId = kvp.Value.Item1;
					lowerestCoord = kvp.Key;
				}

			return lowerestCoord;
		}


		internal void SetMaxId (uint maxId)
		///In case reached uint limitation
		{
			Dictionary<Coord, (uint,T)> srcStack = new Dictionary<Coord, (uint, T)>(stack);
			stack.Clear();

			foreach (var kvp in srcStack)
			{
				uint id = kvp.Value.Item1;
				if (id>maxId) id -= maxId;
				else id = 0;
				stack.Add(kvp.Key, (id, kvp.Value.Item2));
			}
		}
	}
}