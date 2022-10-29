using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Den.Tools
{
	[System.Serializable]
	public class ObjectsPool : MonoBehaviour
	{
		[System.Serializable]
		public class Prototype //: IEquatable<Prototype>
		{
			public GameObject prefab;

			public bool allowReposition = true;
			public bool instantiateClones = false;

			//public bool useRotation = true;
			//public bool rotateYonly = false;
			public bool regardPrefabRotation = false;

			//public bool useScale = true;
			//public bool scaleYonly = false;
			public bool regardPrefabScale = false;


			/*public bool Equals (Prototype p)
			{
				Debug.Log("Equals");

				return prefab == p.prefab &&
					allowReposition == p.allowReposition &&
					instantiateClones == p.instantiateClones;
			}*/

			public override int GetHashCode () 
				{ return prefab.GetHashCode(); }

			public Prototype () { }
			public Prototype (Prototype src)
			{
				prefab = src.prefab;
				allowReposition = src.allowReposition;
				instantiateClones = src.instantiateClones;
				//useRotation = src.useRotation;
				//rotateYonly = src.rotateYonly;
				regardPrefabRotation = src.regardPrefabRotation;
				//useScale = src.useScale;
				//scaleYonly = src.scaleYonly;
				regardPrefabScale = src.regardPrefabScale;
			}
		}

		[System.Serializable]
		private class Pool 
		{
			[SerializeField] public Prototype prototype; //readonly, but readonly is not serialized
			[SerializeField] public List<GameObject> instances;

			//statistics
			private int created = 0;
			private int moved = 0;
			private int deleted = 0;
			public string stats {get{ return "created:" + created + " moved:" + moved + " deleted:" + deleted; }}
			public void ResetStats () { created=0; moved=0; deleted=0; }

			public Pool (Prototype prototype)
			{
				this.prototype = new Prototype(prototype);
				this.instances = new List<GameObject>();
			}


			private void ClearNulls () 
			/// Clearing removed items
			{	
				for (int i=instances.Count-1; i>=0; i--)
				{
					if (instances[i] == null)
						instances.RemoveAt(i);
				}
			}

			private void ClampCount (int targetCount)  
			/// And removing unused instances
			{				
				int instancesCount = instances.Count;
				if (instances.Count < targetCount) return;

				for (int i=targetCount; i<instancesCount; i++)
				{
					#if UNITY_EDITOR
					if (!UnityEditor.EditorApplication.isPlaying)
						GameObject.DestroyImmediate(instances[i].gameObject);
					else
					#endif
						GameObject.Destroy(instances[i].gameObject);

					deleted++;
				}

				instances.RemoveRange(targetCount, instancesCount-targetCount);
			}


			private void AppendCount (int targetCount, Transform parent=null)  
			/// Will create new items until it reach target count
			{
				int instancesCount = instances.Count;

				moved += instancesCount;
					
				for (int i=instancesCount; i<targetCount; i++)
				{
					GameObject gobj = InstantiateObject();
					gobj.transform.parent = parent;
					gobj.hideFlags = parent.gameObject.hideFlags;

					instances.Add(gobj);
					created++;
				}
			}


			private void MoveItems (List<Transition> drafts, int startNum, int endNum)
			{
				for (int i=startNum; i<endNum; i++)
				{
					GameObject gobj = instances[i];
					Transform tfm = gobj.transform;
					Transition draft = drafts[i];

					tfm.localPosition = draft.pos; //transformation.MultiplyPoint3x4(draft.pos);

					if (prototype.regardPrefabRotation) tfm.localRotation = draft.rotation * prototype.prefab.transform.rotation;
					else tfm.localRotation = draft.rotation;

					if (prototype.regardPrefabScale) tfm.transform.localScale = draft.scale.x * prototype.prefab.transform.localScale;
					else tfm.transform.localScale = draft.scale;
				}
			}


			public void Reposition (List<Transition> drafts, Transform parent=null)
			{
				ResetStats();
				ClearNulls();

				int draftsCount = drafts.Count;
				int instancesCount = instances.Count;

				if (draftsCount < instances.Count) 
					ClampCount(draftsCount);

				else if (draftsCount > instances.Count)
					AppendCount(draftsCount, parent);

				MoveItems(drafts, 0, draftsCount);
			}


			public IEnumerator RepositionRoutine (List<Transition> drafts, Transform parent=null, int objsPerIteration=500)
			{
				ResetStats();
				ClearNulls();

				int draftsCount = drafts.Count;
				int instancesCount = instances.Count;
				int iterations = draftsCount / objsPerIteration + 1;

				if (draftsCount < instances.Count) 
					ClampCount(draftsCount);

				for (int i=0; i<iterations; i++)
				{
					Profiler.BeginSample("Pool Apply");

					int startNum = i * objsPerIteration;
					int endNum = (i+1) * objsPerIteration;
					if (endNum > draftsCount) endNum = draftsCount;
					
					Profiler.BeginSample("Pool Instantiate");
					if (endNum > instances.Count)
						AppendCount(endNum, parent);
					Profiler.EndSample();

					Profiler.BeginSample("Pool Move");
					MoveItems(drafts, startNum, endNum);
					Profiler.EndSample();

					Profiler.EndSample();

					yield return null;
				}
			}


			public GameObject InstantiateObject ()
			{
				GameObject gobj = null;
			
				#if UNITY_EDITOR
				if (!prototype.instantiateClones && 
					!UnityEditor.EditorApplication.isPlaying && 
					UnityEditor.PrefabUtility.GetPrefabAssetType(prototype.prefab)!=UnityEditor.PrefabAssetType.NotAPrefab)  //if not playing and prefab is prefab
						gobj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prototype.prefab);
				else
						gobj = GameObject.Instantiate(prototype.prefab); 
				#else
				gobj = GameObject.Instantiate(prototype.prefab);
				#endif

				return gobj;
			}

			public void Clear ()
			{
				if (instances == null) return; //when error occurred on pool creation
				int instancesCount = instances.Count;
				for (int i=0; i<instancesCount; i++)
				{
					#if UNITY_EDITOR
					if (!UnityEditor.EditorApplication.isPlaying)
						GameObject.DestroyImmediate(instances[i].gameObject);
					else
					#endif
						GameObject.Destroy(instances[i].gameObject);
				}
				instances.Clear(); 
			}
		}


		//private Dictionary<Prototype,Pool> pools = new Dictionary<Prototype,Pool>();
		[SerializeField] private Pool[] pools = new Pool[0];



		public void SetPrototypes (Prototype[] prototypes)
		/// Will clear and remove pools that are not in array and add new pools with array prototypes
		/// Will set the pools order exactly as it was given in the array
		{
			//clearing pools with null prefab (rare case)
			for (int i=pools.Length-1; i>=0; i--)
				if (pools[i]==null || pools[i].prototype==null || pools[i].prototype.prefab==null || pools[i].prototype.prefab.Equals(null))
				{
					pools[i].Clear();
					ArrayTools.RemoveAt(ref pools, i);
				}

			//creating the dictionary of used prototypes/pools
			//TODO: use ClearPrortotypes?
			Dictionary<Prototype,Pool> usedPools = new Dictionary<Prototype,Pool>(pools.Length);//, new PrototypeComparer());
			for (int i=0; i<pools.Length; i++)
				if (!usedPools.ContainsKey(pools[i].prototype)) //avoiding two pools with one prototype (could be created when duplicating obj out node)
					usedPools.Add(pools[i].prototype, pools[i]);

			//reposition
			Pool[] newPools = new Pool[prototypes.Length];
			for (int i=0; i<prototypes.Length; i++)
			{
				if (usedPools.TryGetValue(prototypes[i], out Pool pool))
					usedPools.Remove(prototypes[i]);
				else
					pool = new Pool(prototypes[i]);

				//other than instantiateClones and allowReposition (they are in comparer) should be copied
				pool.prototype.regardPrefabRotation = prototypes[i].regardPrefabRotation;
				pool.prototype.regardPrefabScale = prototypes[i].regardPrefabScale;

				newPools[i] = pool;
			}

			//clearing unused
			if (usedPools.Count != 0)
				foreach (var kvp in usedPools)
					kvp.Value.Clear();

			pools = newPools;
		}


		public void ClearPrototypes (Prototype[] prototypes)
		/// Will clear and remove pools that are in array
		{
			HashSet<Prototype> prototypesHashSet = new HashSet<Prototype>(prototypes);//, new PrototypeComparer()); //struct-like compare left some objects remain somehow

			List<Pool> newPools = new List<Pool>();
			for (int i=0; i<pools.Length; i++)
			{
				if (prototypesHashSet.Contains(pools[i].prototype))
					pools[i].Clear();
				else
					newPools.Add(pools[i]);
			}

			pools = newPools.ToArray();
		}


		public void Reposition (Prototype[] prototypes, List<Transition>[] drafts)
		{
			//excluding 0-count from input drafts and prototypes
			for (int i=drafts.Length-1; i>0; i--)
				if (drafts[i].Count == 0) 
					{ ArrayTools.RemoveAt(ref prototypes,i); ArrayTools.RemoveAt(ref drafts,i); }

			SetPrototypes(prototypes); //will set the pools count and order as given in prototypes array

			for (int i=0; i<pools.Length; i++)
				pools[i].Reposition(drafts[i], this.transform);
		}


		public IEnumerator RepositionRoutine (Prototype[] prototypes, List<Transition>[] drafts, int objsPerIteration=500)
		{
			//excluding 0-count from input drafts and prototypes
			for (int i=drafts.Length-1; i>=0; i--)
				if (drafts[i].Count == 0) 
					{ ArrayTools.RemoveAt(ref prototypes,i); ArrayTools.RemoveAt(ref drafts,i); }

			SetPrototypes(prototypes); //will set the pools count and order as given in prototypes array

			for (int i=0; i<pools.Length; i++)
			{
				IEnumerator e = pools[i].RepositionRoutine(drafts[i], this.transform, objsPerIteration);
				while (e.MoveNext()) { yield return null; }
			}
		}


		public class PrototypeComparer : IEqualityComparer<Prototype>
		{
			public bool Equals (Prototype p1, Prototype p2)
			{
				return p1.prefab == p2.prefab &&
					p1.allowReposition == p2.allowReposition &&
					p1.instantiateClones == p2.instantiateClones;
			}

			public int GetHashCode (Prototype p)
			{
				if (p==null || p.prefab==null || p.prefab.Equals(null))
					return 0;

				return p.prefab.GetHashCode() +
					(p.allowReposition ? 0 : 1) +
					(p.instantiateClones ? 0 : 2);
			}
		}


		
		public void Depool (GameObject obj)
		/// Removes an object from the pool without actually deleting it
		{
			for (int p=0; p<pools.Length; p++)
				pools[p].instances.Remove(obj);
			//SOON: check performance
		}



		/*#region Serialization

			[SerializeField] private Pool[] serializedPools;
			
			public void OnBeforeSerialize ()
			{
				if (serializedPools==null || serializedPools.Length!=pools.Count) serializedPools = new Pool[pools.Count];

				int i = 0;
				foreach (var kvp in pools)
				{
					Pool pool = kvp.Value;
					serializedPools[i] = pool;

					i++; 
				}
			}

			public void OnAfterDeserialize ()
			{
				if (serializedPools==null) return;
				
				pools.Clear();

				for (int i=0; i<serializedPools.Length; i++)
				{
					Pool pool = serializedPools[i];
					if (pool.prototype == null) continue;
					if (pool.instances[i] == null || pool.instances.Count==0) continue;

					pools.Add(pool.prototype, pool);
				}
			}

		#endregion*/
	}
}
