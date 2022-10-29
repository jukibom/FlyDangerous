using System;
using System.Collections.Generic;

namespace Den.Tools
{
	//surprisingly Unity's ArrayUtility is an Editor calss
	//so this is it's analog to use in builds
	
	public static class ArrayTools 
	{
		#region Array
			
			static public void RemoveAt<T> (ref T[] array, int num) { array = RemoveAt(array, num); }
			static public T[] RemoveAt<T> (T[] array, int num)
			{
				if (num >= array.Length || num < 0) num = array.Length-1; //note that negative pos will remove LAST element

				T[] newArray = new T[array.Length-1];

				if (num!=0) Array.Copy(array, newArray, num);
				if (num!=array.Length) Array.Copy(array, num+1, newArray, num, newArray.Length-num);

				return newArray;
			}

			static public void Remove<T> (ref T[] array, T obj) where T : class  {array = Remove(array, obj); }
			static public T[] Remove<T> (T[] array, T obj) where T : class
			{
				int num = Find<T>(array, obj);
				return RemoveAt<T>(array,num);
			}

			static public void Add<T> (ref T[] array, T element) { array = Add(array, element); }
			static public T[] Add<T> (T[] array, T element)
			{
				if (array==null || array.Length==0) 
					return new T[] { element };

				T[] newArray = new T[array.Length+1];
				Array.Copy(array, newArray, array.Length);

				newArray[array.Length] = element;
				
				return newArray;
			}

			static public void Add<T> (ref T[] array, T element1, T element2) { array = Add(array, element1, element2); }
			static public T[] Add<T> (T[] array, T element1, T element2)
			//Just adds to elements instead of array
			{
				if (array==null || array.Length==0) 
					return new T[] { element1, element2 };

				T[] newArray = new T[array.Length+2];
				Array.Copy(array, newArray, array.Length);

				newArray[array.Length] = element1;
				newArray[array.Length+1] = element2;
				
				return newArray;
			}

			static public void Add<T> (ref T[] array, T element1, T element2, T element3) { array = Add(array, element1, element2, element3); }
			static public T[] Add<T> (T[] array, T element1, T element2, T element3)
			//Just adds to elements instead of array
			{
				if (array==null || array.Length==0) 
					return new T[] { element1, element2, element3 };

				T[] newArray = new T[array.Length+3];
				Array.Copy(array, newArray, array.Length);

				newArray[array.Length] = element1;
				newArray[array.Length+1] = element2;
				newArray[array.Length+2] = element3;
				
				return newArray;
			}

			static public void AddRange<T> (ref T[] array, T[] other) { array = AddRange(array, other); }
			static public T[] AddRange<T> (T[] array, T[] other)
			{
				if (array==null || array.Length==0) 
				{
					T[] newArray = new T[other.Length];
					Array.Copy(other, newArray, other.Length);
					return newArray;
				}

				else
				{
					T[] newArray = new T[array.Length+other.Length];
					Array.Copy(array, newArray, array.Length);
					Array.Copy(other, 0, newArray, array.Length, other.Length);
					return newArray;
				}
			}

			static public void AddLayer<T> (ref T[,,] array, T[,,] otherArray, int channel) { array = AddLayer(array, otherArray, channel); }
			static public T[,,] AddLayer<T> (T[,,] array, T[,,] otherArray, int channel)
			{
				int lengthX = array.GetLength(0);
				int lengthZ = array.GetLength(1);
				int numChannels = array.GetLength(2);

				T[,,] newArray = new T[lengthX,lengthZ,numChannels+1];
				Array.Copy(array, newArray, lengthX*lengthZ*numChannels);
				CopyLayer(otherArray, newArray, channel, numChannels);

				return newArray;
			}

			static public void Insert<T> (ref T[] array, int pos, T element) { array = Insert(array, pos, element); }
			static public void Insert<T> (ref T[] array, int pos, Func<int,T> createElement) { array = Insert(array, pos, createElement(pos)); }
			static public T[] Insert<T> (T[] array, int pos, T element)
			{
				if (array==null || array.Length==0) 
					return new T[] {element};

				if (pos > array.Length || pos < 0) pos = array.Length; //note that negative pos will ADD element

				T[] newArray = new T[array.Length+1];

				if (pos != 0) Array.Copy(array, newArray, pos);
				if (pos != array.Length) Array.Copy(array, pos, newArray, pos+1, array.Length-pos);

				newArray[pos] = element;

				return newArray;
			}

			static public void InsertRemoveLast<T> (this T[] array, int pos, T element)
			{
				Array.Copy(array, pos, array, pos+1, array.Length-pos-1);
				array[pos] = element;
			}

			static public T[] InsertRange<T> (T[] array, int after, T[] add)
			{
				//if (array==null || array.Length==0) { return add; } //should create a copy anyways

				if (after > array.Length || after<0) after = array.Length;
				
				T[] newArray = new T[array.Length+add.Length];

				if (after != 0) Array.Copy(array, newArray, after);
				Array.Copy(add, 0, newArray, after, add.Length);
				if (after != array.Length) Array.Copy(array, after, newArray, after+add.Length, array.Length-after);

				return newArray;
			}

			static public void Resize<T> (ref T[] array, int newSize, Func<int,T> createElement=null) { array = Resize(array, newSize, createElement); }
			static public T[] Resize<T> (T[] array, int newSize, Func<int,T> createElement=null)
			{
				//if (array.Length == newSize) return array; //should create a copy anyways

				T[] newArray = new T[newSize];
				Array.Copy(array, newArray, newSize<array.Length? newSize : array.Length);

				if (newSize > array.Length && createElement != null)
				{
					for (int i=array.Length; i<newSize; i++)
						newArray[i] = createElement(i);
				}

				return newArray;
			}

			static public void Resize<T> (ref T[] array, int newSize, T def) { array = Resize(array, newSize, def); }
			static public T[] Resize<T> (T[] array, int newSize, T def)
			{
				//if (array.Length == newSize) return array; //should create a copy anyways

				T[] newArray = new T[newSize];
				Array.Copy(array, newArray, newSize<array.Length? newSize : array.Length);

				if (newSize > array.Length)
				{
					for (int i=array.Length; i<newSize; i++)
						newArray[i] = def;
				}

				return newArray;
			}

			static public void ResizeLayers<T> (ref T[,,] array, int newSize, T def) { array = ResizeLayers(array, newSize, def); }
			static public T[,,] ResizeLayers<T> (T[,,] array, int newSize, T def)
			{
				int oldSize = array.GetLength(2);
				int sizeX = array.GetLength(0);
				int sizeZ = array.GetLength(1);

				T[,,] newArray = new T[array.GetLength(0), array.GetLength(1), newSize];
				//Array.Copy(array, 0, newArray, 0, newSize<oldSize? newArray.Length : array.Length); //could be used if channel 0 is changed, but not 2

				for (int x=0; x<sizeX; x++)
					for (int z=0; z<sizeZ; z++)
						for (int i=0; i<newSize; i++)
						{
							T val = i<oldSize ? array[x,z,i] : def;
							newArray[x,z,i] = val;
						}

				return newArray;
			}

			static public void CopyLayer<T> (T[,,] src, T[,,] dst, int srcNum, int dstNum)
			{
				int sizeX = src.GetLength(0);
				int sizeZ = src.GetLength(1);

				for (int x=0; x<sizeX; x++)
					for (int z=0; z<sizeZ; z++)
						dst[x,z,dstNum] = src[x,z,srcNum];
			}

			static public void Append<T> (ref T[] array, T[] additional) { array = Append(array, additional); }
			static public T[] Append<T> (T[] array, T[] additional)
			{
				T[] newArray = new T[array.Length+additional.Length];
				for (int i=0; i<array.Length; i++) { newArray[i] = array[i]; }
				for (int i=0; i<additional.Length; i++) { newArray[i+array.Length] = additional[i]; }
				return newArray;
			}

			static public void Switch<T> (T[] array, int num1, int num2)
			{
				if (num1<0 || num1>=array.Length || num2<0 || num2 >=array.Length) return;
				
				T temp = array[num1];
				array[num1] = array[num2];
				array[num2] = temp;
			}

			static public void Switch<T> (T[] array, T obj1, T obj2) where T : class
			{
				int num1 = Find<T>(array, obj1);
				int num2 = Find<T>(array, obj2);
				Switch<T>(array, num1, num2);
			}

			static public void Replace<T> (this T[] array, T obj1, T obj2) where T : IEquatable<T>
			{
				for (int i=0; i<array.Length; i++)
					if (array[i].Equals(obj1))
						array[i] = obj2;
			}

			static public void ReplaceNum<T> (this T[] array, int n1, int n2) where T : IEquatable<T>
			{
				T obj1 = array[n1];
				T obj2 = array[n2];

				for (int i=0; i<array.Length; i++)
					if (array[i].Equals(obj1))
						array[i] = obj2;
			}

			static public void Move<T> (T[] array, int src, int dst)
			{
				T srcVal = array[src];
				if (src < dst) //moving down
				{
					for (int i=src; i<dst; i++)
						array[i] = array[i+1];
				}
				if (src > dst) //moving up
				{
					for (int i=src; i>dst; i--)
						array[i] = array[i-1];
				}
				array[dst] = srcVal;
			}


			static public T[] Truncated<T> (this T[] src, int length)
			{
				T[] dst = new T[length];
				for (int i=0; i<length; i++) dst[i] = src[i];
				return dst;
			}

			public static bool Equals<T> (T[] a1, T[] a2) where T : class
			{
				if (a1.Length != a2.Length) return false;
				for (int i=0; i<a1.Length; i++)
					if (a1[i] != a2[i]) return false;
				return true;
			}

			public static bool EqualsEquatable<T> (T[] a1, T[] a2) where T : IEquatable<T>
			{
				if (a1.Length != a2.Length) return false;
				for (int i=0; i<a1.Length; i++)
					if (!Equals(a1[i],a2[i])) return false;
				return true;
			}

			public static bool EqualsVector3 (UnityEngine.Vector3[] a1, UnityEngine.Vector3[] a2, float delta=float.Epsilon)
			{
				if (a1==null || a2==null || a1.Length != a2.Length) return false;
				for (int i=0; i<a1.Length; i++)
				{
					float dist = a1[i].x-a2[i].x;
					if (!(dist<delta && -dist<delta)) return false;

					dist = a1[i].y-a2[i].y;
					if (!(dist<delta && -dist<delta)) return false;

					dist = a1[i].z-a2[i].z;
					if (!(dist<delta && -dist<delta)) return false;
				}
				return true;
			}

			//public static int Find(this Array array, object obj)
			//{
			//	for (int i=0; i<array.Length; i++)
			//		if (array.GetValue(i) == obj) return i;
			//	return -1;
			//}
			//when comparing two (object)strings VS says "true", Unity says "false". Maybe there are some other cases of improper compare


			static public int Find<T> (this T[] array, T obj) //where T : class   where T : IEquatable<T>
			{
				for (int i=0; i<array.Length; i++)
					if (Equals(array[i],obj)) return i;
				return -1;
			}

			static public int Find<T> (this T[] array, Predicate<T> func) //where T : class   where T : IEquatable<T>
			{
				for (int i=0; i<array.Length; i++)
					if (func(array[i])) return i;
				return -1;
			}

			static public T FindMember<T> (this T[] array, Func<T,bool> func) where T : class
			{
				for (int i=0; i<array.Length; i++)
					if (func(array[i])) return array[i];
				return null;
			}

			public static int FindCount<T>(this T[] array, T obj)
			{
				int count = 0;
				for (int i=0; i<array.Length; i++)
					if (Equals(array[i],obj)) count++;
				return count;
			}

			public static List<int> FindAllIndexes<T> (this T[] array, Func<T,bool> func)
			/// Returns all instances
			{
				List<int> result = new List<int>();
				for (int i=0; i<array.Length; i++)
					if (func(array[i])) result.Add(i);
				return result;
			}

			public static TT FindMemberOfType<T,TT> (this T[] array)
			{
				for (int i=0; i<array.Length; i++)
					if (array[i] is TT tt) return tt;
				return default;
			}

			public static T FindSmallest<T> (this T[] array, Func<T,float> func)
			{
				if (array.Length == 0) return default(T);
				if (array.Length == 1) return array[0];

				float smallestVal = float.MaxValue;
				int smallestIndex = -1;
				for (int i=0; i<array.Length; i++)
				{
					float val = func(array[i]);
					if (val < smallestVal)
						{ smallestVal = val; smallestIndex = i; }
				}

				return array[smallestIndex];
			}

			public static T FindBiggest<T> (this T[] array, Func<T,float> func)
			{
				if (array.Length == 0) return default(T);
				if (array.Length == 1) return array[0];

				float biggesttVal = float.MinValue;
				int biggestIndex = -1;
				for (int i=0; i<array.Length; i++)
				{
					float val = func(array[i]);
					if (val > biggesttVal)
						{ biggesttVal = val; biggestIndex = i; }
				}

				return array[biggestIndex];
			}

			static public bool Contains<T>(this T[] array, T obj)
			{
				if (array == null) return false;  //wierd case
				if (Array.IndexOf(array, obj) >= 0) return true;
				else return false;
			}

			static public bool Contains<T>(this T[] array, Predicate<T> func)
			{
				if (array == null) return false;  //wierd case
				if (Find(array, func) >= 0) return true;
				else return false;
			}

			static public bool ContainsNull<T>(this T[] array) where T: class
			{
				if (array == null) return false;  //wierd case
				if (Array.IndexOf(array, null) >= 0) return true;
				else return false;
			}

			static public bool AllNull<T>(this T[] array) where T: class
			{
				if (array == null) return false;  //wierd case
				for (int i=0; i<array.Length; i++)
					if (array[i]!=null) return false;
				return true;
			}


			static public T Any<T> (this T[] array) where T : class
			{
				for (int i=0; i<array.Length; i++)
					if (array[i]!=null) return array[i];
				return null;
			}

			static public int Max (this int[] array)
			{
				int max = int.MinValue;
				for (int i=0; i<array.Length; i++)
					if (array[i] > max) max = array[i];
				return max;
			}

			static public bool Empty<T> (this T[] array) where T : class
			{
				for (int i=0; i<array.Length; i++)
					if (array[i]!=null) return false;
				return true;
			}

			static public void RemoveAll<T> (ref T[] array, T obj) where T : class  {array = RemoveAll(array, obj); }
			static public T[] RemoveAll<T> (T[] array, T obj) where T : class
			/// Removes all the occurrences of obj in array
			{
				bool[] remove = new bool[array.Length];
				int removeCount = 0;

				for (int i=0; i<array.Length; i++)
					if (Equals(array[i],obj)) { removeCount++; remove[i] = true; }

				T[] newArr = new T[array.Length-removeCount];

				int counter = 0;
				for (int i=0; i<array.Length; i++)
				{
					if (remove[i]) continue;

					newArr[counter] = array[i];
					counter++;
				}

				return newArr;
			}

			static public void RemoveAllFunc<T> (ref T[] array,  Func<T,bool> func) where T : class  {array = RemoveAllFunc(array, func); }
			static public T[] RemoveAllFunc<T> (T[] array, Func<T,bool> func) where T : class
			/// Finds all elementsz with a callback and removes them
			{
				bool[] remove = new bool[array.Length];
				int removeCount = 0;

				for (int i=0; i<array.Length; i++)
					if (func(array[i])) { removeCount++; remove[i] = true; }

				T[] newArr = new T[array.Length-removeCount];

				int counter = 0;
				for (int i=0; i<array.Length; i++)
				{
					if (remove[i]) continue;

					newArr[counter] = array[i];
					counter++;
				}

				return newArr;
			}

			static public T[] RemoveNulls<T>(T[] array) where T: class
			{
				if (array == null) return array;
				if (Array.IndexOf(array, null) >= 0) return array;

				int numNulls = 0;
				for (int i=0; i<array.Length; i++)
					if (array[i]==null) numNulls++;

				T[] newArr = new T[array.Length - numNulls];
				
				int c = 0;
				for (int i=0; i<array.Length; i++)
				{
					if (array[i]==null) continue;

					newArr[c] = array[i];
					c++;
				}

				return newArr;
			}

			static public bool MatchExactly<T> (T[] arr1, T[] arr2)
			/// Returns true if arr1 and arr2 have same elements, in same order
			{
				if (arr1.Length != arr2.Length) return false;

				for (int i=0; i<arr1.Length; i++)
					if (!Equals(arr1[i], arr2[i])) return false;

				return true;
			}

			static public bool MatchElements<T> (T[] arr1, T[] arr2)
			/// Returns true if arr1 and arr2 have same elements, but in different order
			/// This works even if elements null or duplicating
			{
				if (arr1.Length != arr2.Length) return false;

				bool[] matchList = new bool[arr1.Length];

				for (int i=0; i<arr1.Length; i++)
					for (int j=0; j<arr2.Length; j++)
					{
						if (matchList[j]) continue;
						if (Equals(arr1[i], arr2[j])) { matchList[j] = true; break; }
					}

				for (int i=0; i<matchList.Length; i++)
					if (!matchList[i]) return false;
				return true;
			}


			static public void AddIfNotContains<T> (ref T[] array, T element) { array = AddIfNotContains(array, element); }
			static public T[] AddIfNotContains<T> (T[] array, T element)
			/// Adds new element only if it was not found in array
			{
				if (array.FindCount(element) == 0) return Add(array, element);
				else return array;
			}

			static public void AddRangeIfNotContains<T> (ref T[] array, IEnumerable<T> add) { array = AddRangeIfNotContains(array, add); }
			static public T[] AddRangeIfNotContains<T> (T[] array, IEnumerable<T> add)
			/// Adds each of new element only if it was not found in array
			/// Does not guarantee uniqueness: if add contains 2 same elements, they will be added both
			{
				List<T> elementsToAdd = new List<T>();
				foreach (T element in add)
					if (!array.Contains(element)) elementsToAdd.Add(element);

				if (elementsToAdd.Count == 0)
					return array;

				else
					return AddRange(array, elementsToAdd.ToArray());
			}


			static public bool IsEmpty<T> (T[] array) where T: class
			{
				for (int i=0; i<array.Length; i++)
					if (array[i] != null) return false;
				return true;
			}

			static public void Rewrite<T> (List<T> src, ref T[] dst)
			/// Fills an array with list values. Will not re-create array if it's count has not changed
			{
				if (dst.Length != src.Count) dst = new T[src.Count];
				for (int i=0; i<dst.Length; i++)
					dst[i] = src[i];
			}

			static public void Rewrite<T1,T2> (List<T1> src, ref T2[] dst, Func<T1,T2> fn)
			/// Same as rewrite, but calls fn each time
			{
				if (dst.Length != src.Count) dst = new T2[src.Count];
				for (int i=0; i<dst.Length; i++)
					dst[i] = fn(src[i]);
			}

			static public T[] Process<T> (this T[] arr, Func<int,T> func)
			/// Calls func on each arry member. Linq sux
			{
				for (int i=0; i<arr.Length; i++)
					arr[i] = func(i);
				return arr;
			}

			static public TDst[] Select<TSrc, TDst> (this TSrc[] src, Func<TSrc,TDst> fn)
			{
				TDst[] dst = new TDst[src.Length];
				for (int a=0; a<src.Length; a++)
					dst[a] = fn(src[a]);
				return dst;
			}

			static public T[] Convert<T,Y> (Y[] src)
			{
				T[] result = new T[src.Length];
				for (int i=0; i<src.Length; i++) result[i] = (T)(object)(src[i]);
				return result;
			}

			static public T[] Convert<T,Y> (ICollection<Y> src)
			{
				Y[] tmpArr = new Y[src.Count];
				src.CopyTo(tmpArr, 0);
				return Convert<T,Y>(tmpArr);
			}

			static public void FillNulls<T> (this T[] arr, Func<T> func) where T: class
			{
				for (int a=0; a<arr.Length; a++)
					if (arr[a] == null) arr[a] = func();
			}

			static public void Fill<T> (this T[] arr, T val)
			{
				for (int a=0; a<arr.Length; a++)
					arr[a] = val;
			}

			static public string ToStringMemberwise(this Array array, string separator=", ")
			{
				string s = "";
				if (array.Length == 0) return s;
				for (int i=0; i<array.Length; i++)
				{
					object val = array.GetValue(i);
					s += val!=null? val.ToString() : "";
					if (i != array.Length-1) s += separator;
				}
				return s;
			}

			static public void RandomMix<T> (this T[] array, int iterations=2)
			{
				for (int it=0; it<iterations; it++)
				{
					for (int a=0; a<array.Length; a++)
					{
						int b = UnityEngine.Random.Range(0, array.Length);
						if (a==b) continue;

						T tmp = array[b];
						array[b] = array[a];
						array[a] = tmp;
					}
				}
			}

			static public T Last<T> (this T[] array) where T: class => 
				array.Length != 0 ? array[array.Length-1] : null;

			static public T[] Copy<T> (this T[] array)
			{
				T[] newArr = new T[array.Length];
				Array.Copy(array, newArr, array.Length);
				return newArr;
			}

			static public T[][] CopyJagged<T> (this T[][] array)
			{
				T[][] newArr = new T[array.Length][];
				for (int i=0; i<array.Length; i++)
					newArr[i] = Copy(array[i]);
				return newArr;
			}

			static public TA[] Where<TC,TA> (this ICollection<TC> collection, Func<TC,TA> fn)
			{
				TA[] array = new TA[collection.Count];

				int i = 0;
				foreach (TC elem in collection)
				{
					array[i] = fn(elem);
					i++;
				}

				return array;
			}

		#endregion

		#region Array Sorting

			static public void QSort (float[] array) { QSort(array, 0, array.Length-1); }
			static public void QSort (float[] array, int l, int r)
			{
				float mid = array[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (array[i] < mid) i++;
					while (array[j] > mid) j--;
					if (i <= j)
					{
						float temp = array[i];
						array[i] = array[j];
						array[j] = temp;
						
						i++; j--;
					}
				}
				if (i < r) QSort(array, i, r);
				if (l < j) QSort(array, l, j);
			}

			static public void QSort<T> (T[] array, float[] reference) { QSort(array, reference, 0, reference.Length-1); }
			static public void QSort<T> (T[] array, float[] reference, int l, int r)
			{
				float mid = reference[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (reference[i] < mid) i++;
					while (reference[j] > mid) j--;
					if (i <= j)
					{
						float temp = reference[i];
						reference[i] = reference[j];
						reference[j] = temp;

						T tempT = array[i];
						array[i] = array[j];
						array[j] = tempT;
						
						i++; j--;
					}
				}
				if (i < r) QSort(array, reference, i, r);
				if (l < j) QSort(array, reference, l, j);
			}

			static public void QSort<T> (List<T> list, float[] reference) { QSort(list, reference, 0, reference.Length-1); }
			static public void QSort<T> (List<T> list, float[] reference, int l, int r)
			{
				float mid = reference[l + (r-l) / 2]; //(l+r)/2
				int i = l;
				int j = r;
				
				while (i <= j)
				{
					while (reference[i] < mid) i++;
					while (reference[j] > mid) j--;
					if (i <= j)
					{
						float temp = reference[i];
						reference[i] = reference[j];
						reference[j] = temp;

						T tempT = list[i];
						list[i] = list[j];
						list[j] = tempT;
						
						i++; j--;
					}
				}
				if (i < r) QSort(list, reference, i, r);
				if (l < j) QSort(list, reference, l, j);
			}

			static public int[] Order (int[] array, int[] order=null, int max=0, int steps=1000000, int[] stepsArray=null) //returns an order int array
			{
				if (max==0) max=array.Length;
				if (stepsArray==null) stepsArray = new int[steps+1];
				else steps = stepsArray.Length-1;
			
				//creating starts array
				int[] starts = new int[steps+1];
				for (int i=0; i<max; i++) starts[ array[i] ]++;
					
				//making starts absolute
				int prev = 0;
				for (int i=0; i<starts.Length; i++)
					{ starts[i] += prev; prev = starts[i]; }

				//shifting starts
				for (int i=starts.Length-1; i>0; i--)
					{ starts[i] = starts[i-1]; }  
				starts[0] = 0;

				//using magic to compile order
				if (order==null) order = new int[max];
				for (int i=0; i<max; i++)
				{
					int h = array[i]; //aka height
					int num = starts[h];
					order[num] = i;
					starts[h]++;
				}
				return order;
			}



		#endregion
	}

}
