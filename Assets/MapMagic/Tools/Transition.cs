using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Den.Tools.Matrices;

namespace Den.Tools
{
	[Serializable]
	public struct Transition
	{
		public Vector3 pos;
		public Quaternion rotation;
		public Vector3 scale;
		public float terrainHeight; //additional height for relative placement

		public int id;   //always unique for every posTab (sets on adding to posTab)
		public int hash; //stays the same on merging posTabs. The one the random depends on
		public int num;  //for brush: tree prototype in tree relocate or prefub instance num for obj relocate.

		[NonSerialized] public Transform instance; //to make brush remember the read objects

		public float Yaw
		{
			get{
				return Mathf.Acos(rotation.w) / Mathf.Deg2Rad * 2;
			}

			set{
				float halfVal = value/2 * Mathf.Deg2Rad;
				rotation.w = Mathf.Cos(halfVal);
				rotation.z = 0;
				rotation.y = Mathf.Sin(halfVal);
				rotation.x = 0;
			}
		}

		public Transition (float x, float z)
		/// Adds empty transition at coordinates
		{
			pos = new Vector3(x,0,z);
			rotation = new Quaternion(0,0,0,1);
			scale = new Vector3(1,1,1);
			terrainHeight = 0;
			id = 0;
			hash = 0;
			num = 0;
			instance =null;
		}


		public Transition (float x, float y, float z)
		/// Adds empty transition at coordinates
		{
			pos = new Vector3(x,y,z);
			rotation = new Quaternion(0,0,0,1);
			scale = new Vector3(1,1,1);
			terrainHeight = 0;
			id = 0;
			hash = 0;
			num = 0;
			instance =null;
		}


		public Transition (Transform transform)
		{
			pos = transform.position; //MM is generating world positions
			rotation = transform.rotation;
			scale = transform.localScale;
			terrainHeight = 0;
			id = 0;
			hash = 0;
			num = 0;
			instance = transform;
		}


		public Transition (TreeInstance tree, Vector3 terrainPos, Vector3 terrainSize, Transform prototypePrefab=null)
		{
			pos = new Vector3(tree.position.x*terrainSize.x, tree.position.y*terrainSize.y, tree.position.z*terrainSize.z) + terrainPos;
			rotation = Quaternion.Euler(0, tree.rotation, 0);
			scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
			terrainHeight = 0;
			id = 0;
			hash = 0;
			num = tree.prototypeIndex;
			instance = prototypePrefab;
		}

		/*public static Transition operator * (Transition trs, float val)
		{
			trs.pos *= val;
			trs.rotation *= val;
		}*/
	}


	[System.Serializable]
	public class TransitionsList : ICloneable 
	{
		public Transition[] arr = new Transition[4];
		public int count = 0;

		private int idCounter;


		public TransitionsList () { }
		public TransitionsList (int count) { arr=new Transition[count]; this.count=count; }
		//public TransitionsList (int count, int startId) { arr=new Transition[count]; this.count=count; idCounter=startId; }
		public TransitionsList (TransitionsList src) { arr=new Transition[src.arr.Length]; Array.Copy(src.arr, arr, src.arr.Length); count = src.count; }


		public void Add (TransitionsList other)
		/// Combines two lists in current
		{
			for (int t=0; t<other.count; t++)
				Add(other.arr[t]);
			//TODO: avoid resizing array several times
		}


		public void Add (float x, float z)
		{
			Transition trs = new Transition(x,z);
			Add(trs);
		}


		public void Add (Transition trs)
		{
			idCounter++;
			if (idCounter > 0x7FFFFFFE) idCounter = 1;
			trs.id = idCounter;

			if (trs.hash == 0) //if hash not defined
				trs.hash = trs.id;

			if (arr.Length <= count)
				SetCapacity(arr.Length*2);

			arr[count] = trs;
			count++;
		}


		private void SetCapacity (int newCapacity)
		{
			Transition[] newArr = new Transition[newCapacity];
			Array.Copy(arr, newArr, arr.Length);
			arr = newArr;
		}


		public Vector3 Min ()
		{
			Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			for (int t=0; t<count; t++)
			{
				if (arr[t].pos.x < min.x) min.x = arr[t].pos.x;
				if (arr[t].pos.y < min.y) min.y = arr[t].pos.y;
				if (arr[t].pos.z < min.z) min.z = arr[t].pos.z;
			}
			return min;
		}


		public Vector3 Max ()
		{
			Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			for (int t=0; t<count; t++)
			{
				if (arr[t].pos.x > max.x) max.x = arr[t].pos.x;
				if (arr[t].pos.y > max.y) max.y = arr[t].pos.y;
				if (arr[t].pos.z > max.z) max.z = arr[t].pos.z;
			}
			return max;
		}


		public int CountInRect (Vector2D pos, Vector2D size)
		{
			int c = 0;
			for (int t=0; t<count; t++)
			{
				if (arr[t].pos.x > pos.x  &&  arr[t].pos.x < pos.x+size.x  &&
					arr[t].pos.z > pos.z  &&  arr[t].pos.z < pos.z+size.z)
						c ++;
			}
			return c; 
		}


		public object Clone () 
		{
			return new TransitionsList() { arr=(Transition[])(arr.Clone()), count=count };
		}


		#region Per-Transition Operations

			public static void Mask (TransitionsList src, TransitionsList dst, MatrixWorld mask, Noise random, bool invert)
			/// Adds masked (or unmasked if invert) objects to dst
			{
				for (int t=0; t<src.count; t++)
				{
					Vector3 pos = src.arr[t].pos;

					if (pos.x <= mask.worldPos.x  &&   pos.x >= mask.worldPos.x+mask.worldSize.x  &&
						pos.z <= mask.worldPos.z  &&   pos.z >= mask.worldPos.x+mask.worldSize.z)
							continue; //do remove out of range objects?

					float val = mask.GetWorldValue(pos.x, pos.z);
					float rnd = random.Random(src.arr[t].hash);

					if (val<rnd && invert) dst.Add(src.arr[t]);
					if (val>=rnd && !invert) dst.Add(src.arr[t]);
				}
			}

		#endregion
	}


}
