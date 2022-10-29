using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Den.Tools
{

	public interface ITile
	{		
		//Coord Coord { set; }
		//bool Pinned { set; }
		//int Distance { get; set; } //from deploy rects centers, in chunks
		bool IsNull { get; } //if main object was removed externally. Checking on ad, remove and deploy
		
		//static ITile Construct (object holder);
		void Move (Coord coord, float dist);
		void Dist (float dist);
		void Remove ();
	}

	public class TileManager<T> : ISerializationCallbackReceiver where T: ITile//, IEquatable<T>
	{
		public Dictionary<Coord,T> grid = new Dictionary<Coord,T>();
		public object gridLocker = new object();

		public bool allowMove = false;
		public bool generateInfinite = true;
		public int generateRange = 2;
		public int retainMargin = 1;

		public bool genAroundMainCam = true;
		public bool genAroundObjsTag = false;
		public string genAroundTag = null;

		[System.NonSerialized] protected Coord[] camCoords = null;
		//[System.NonSerialized] protected CoordRect[] deployRects;  //used to find chunks difference and for Unpin


		public T this[Coord coord] 
		{get{ 
			if (grid.TryGetValue(coord, out T t)) return t; 
			else return default(T); 
		}}

		public T this[int x, int z] {get{ return this[ new Coord(x,z) ]; }}


		public bool Contains (Coord coord)
		/// Checks if tile is contained in hash dictionary. 
		{
			return grid.ContainsKey(coord);
		}


		protected T ConstructTile (MonoBehaviour holder)
		{
			return (T)typeof(T).GetMethod("Construct", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null,new object[]{holder});
			//TODO: make smth with it
		}


		public IEnumerable<T> Tiles ()
		{
			foreach (KeyValuePair<Coord,T> kvp in grid)
				yield return kvp.Value;
		}


		public virtual T Closest ()
		{
			float minDist = int.MaxValue;
			T minTile = default;

			foreach (var kvp in grid)
			{
				if (camCoords == null) return kvp.Value;

				Coord coord = kvp.Key;
				float dist = GetRemoteness(coord, camCoords);
				if (dist<minDist) { minDist=dist; minTile=kvp.Value; }
			}

			return minTile;
		}




		#region Per-frame/Update

			public void Update (Vector3 tileSize, Dictionary<Coord,T> pinned=null, MonoBehaviour holder=null, bool distsOnly=false)
			{
				Profiler.BeginSample("Remove Nulls");
				RemoveNulls(); //excluding removed objects
				Profiler.EndSample();
				
				Profiler.BeginSample("RefreshCamCoords");
				bool camCoordsChanged = RefreshCamCoords(tileSize.x, holder);
				if (!camCoordsChanged || camCoords.Length==0) { Profiler.EndSample(); return; }
				Profiler.EndSample();

				Profiler.BeginSample("Deploy");
				if (!distsOnly && generateInfinite) Deploy(camCoords, pinned:pinned, holder:holder);
				Profiler.EndSample();

				Profiler.BeginSample("ChangeDists");
				ChangeDists(camCoords);
				Profiler.EndSample();
			}


			public void ReDeploy (Vector3 tileSize, Dictionary<Coord,T> pinned=null, MonoBehaviour holder=null)
			{
				RemoveNulls(); //excluding removed objects
				RefreshCamCoords(tileSize.x);
				Deploy(camCoords, pinned:pinned, holder:holder);
				ChangeDists(camCoords);
			}


			private bool RefreshCamCoords (float tileSize, MonoBehaviour holder=null)
			/// Gets a list of camera (or tagged objects) positions. Uses a cached camPoses array. Returns true if camera positions changed.
			{
				bool coordsChanged = false;

				#if UNITY_EDITOR
				if (!UnityEditor.EditorApplication.isPlaying) 
				{
					if (UnityEditor.SceneView.lastActiveSceneView?.camera==null || UnityEditor.SceneView.lastActiveSceneView.camera==null) //this happens right after script compile 
						camCoords = new Coord[0]; 

					else
					{
						Vector3 sceneCamPos = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
						Coord sceneCamCoord = Coord.Floor(sceneCamPos.x/tileSize, sceneCamPos.z/tileSize);

						if (camCoords==null || camCoords.Length!=1) { camCoords = new Coord[1]; coordsChanged = true; }
						if (camCoords[0] != sceneCamCoord) { camCoords[0] = sceneCamCoord; coordsChanged = true; }
					}
				}

				else
				#endif
				{
					//finding objects with tag
					GameObject[] taggedObjects = null;
					if (genAroundTag!=null && genAroundTag.Length!=0) taggedObjects = GameObject.FindGameObjectsWithTag(genAroundTag);

					//calculating cams array length and rescaling it
					int camsLength = 0;
					if (genAroundMainCam) camsLength++;
					if (taggedObjects !=null) camsLength += taggedObjects.Length;

					if (camCoords == null || camsLength != camCoords.Length) { camCoords = new Coord[camsLength]; coordsChanged = true; }
				
					if (camsLength == 0) 
						//throw new Exception("TileManager: No Camera in scene to generate tiles.");
						return coordsChanged;

					//filling cams array
					int counter = 0;
					if (genAroundMainCam) 
					{
						Camera mainCam = Camera.main;
						if (mainCam == null) mainCam = GameObject.FindObjectOfType<Camera>(); //in case it was destroyed or something
						if (mainCam != null) //if still no camera
						{
							Vector3 camPos = mainCam.transform.position;
							if (holder != null) camPos = holder.transform.InverseTransformPoint(camPos);
						
							Coord camCoord = Coord.Floor(camPos.x/tileSize, camPos.z/tileSize);
							if (camCoords[0] != camCoord) { camCoords[0] = camCoord; coordsChanged = true; }
							counter++;
						}
					}
					if (taggedObjects != null)
						for (int i=0; i<taggedObjects.Length; i++) 
						{
							Vector3 objPos = taggedObjects[i].transform.position;
							if (holder != null) objPos = holder.transform.InverseTransformPoint(objPos);

							Coord objCoord = Coord.Floor(objPos.x/tileSize, objPos.z/tileSize);
							if (camCoords[i+counter] != objCoord) { camCoords[i+counter] = objCoord; coordsChanged = true; }
						}
				}

				return coordsChanged;
			}

		#endregion


		#region Deploy

			public virtual void ChangeDists (Coord[] camCoords)
			/// Fast deploy that changes distances only
			{
				foreach (var kvp in grid)
				{
					Coord coord = kvp.Key;
					T tile = kvp.Value;

					tile.Dist( GetRemoteness(coord, camCoords) );
				}
			}	


			public virtual void Deploy (Coord[] camCoords, Dictionary<Coord,T> pinned=null, MonoBehaviour holder=null)
			/// Creates all tiles within createRect, removes tiles outside removeRect. Tries to move tiles instead of creating new (if allowed). 
			/// Note that all rects contain chunks, not world units
			/// Holder is a parent object that called refresh, to parent created tiles
			{
				CoordRect[] createRects = GetDeployRects(camCoords, generateRange);

				//it would be easier to create new grid and fill it then, but 
				//no change should be made in original grid because of multithreading
				Dictionary<Coord,T> dstGrid = new Dictionary<Coord,T>();
				Dictionary<Coord,T> srcGrid = new Dictionary<Coord,T>(grid); 

				//transferring pinned tiles to new grid
				Profiler.BeginSample("Transf Pin To New");
				if (pinned != null)
					foreach(KeyValuePair<Coord,T> kvp in pinned)
				{
					Coord coord = kvp.Key;
					T tile = kvp.Value;
					
					srcGrid.Remove(coord);
					dstGrid.Add(coord, tile);

					tile.Dist(GetRemoteness(coord, camCoords)); //calculating dist to every tile added to dstGrid
				}
				Profiler.EndSample();


				//adding objects within create range + margin (on their respective coordinates)
				Profiler.BeginSample("Adding Objs");
				for (int r=0; r<createRects.Length; r++)
				{
					CoordRect rect = createRects[r];
					//rect.Expand(retainMargin);
					Coord min = rect.Min-retainMargin; Coord max = rect.Max+retainMargin;

					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
						{
							Coord coord = new Coord(x,z);

							if (srcGrid.TryGetValue(coord, out T tile))
							{
								srcGrid.Remove(coord);
								dstGrid.Add(coord,tile);

								tile.Dist(GetRemoteness(coord, camCoords));
							}
						}
				}
				Profiler.EndSample();

				//filling create rects empty areas with unused (or new) objects and moving them
				Profiler.BeginSample("Fillin Empty");
				Queue<T> pool = new Queue<T>(srcGrid.Values);
				List<(T tile, Coord coord, float dist)> moved = new List<(T,Coord,float)>();
				for (int r=0; r<createRects.Length; r++)
				{
					CoordRect rect = createRects[r];
					Coord min = rect.Min; Coord max = rect.Max;

					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
					{
						Coord newCoord = new Coord(x,z);

						if (dstGrid.ContainsKey(newCoord)) continue;

						T tile;

						//moving
						if (pool.Count != 0  &&  allowMove)
						{
							//Coord oldCoord = srcGrid.AnyKey();
							//T tile = srcGrid[oldCoord];
							tile = pool.Dequeue();
						}

						//creating
						else 
						{
							//Debug.Log("No tiles left. Creating. Coord:" + newCoord);
							Profiler.BeginSample("Construct Tile");
							tile = ConstructTile(holder);
							Profiler.EndSample();
						}	

						dstGrid.Add(newCoord, tile);

						//tile.Move(newCoord, GetRemoteness(newCoord, camCoords)); //moving after according to their distance
						moved.Add( (tile, newCoord, GetRemoteness(newCoord, camCoords)) );
					}

					//HashSet<T> curChangedTiles = RelocateTiles(dstGrid, rect, pool, holder);
					//changedTiles.UnionWith(curChangedTiles);
				}
				Profiler.EndSample();

				//calling remove fn on all other objs left (no need to remove from srcDict - just not including them in dst)
				Profiler.BeginSample("Callin Remove");
				while (pool.Count != 0)
				{
					T tile = pool.Dequeue();
					tile.Remove();
				}
				Profiler.EndSample();

				//calling Move function in order depending on remoteness
				Profiler.BeginSample("Callin Move");
				moved.Sort((x,y) => 
				{
					float delta = x.dist-y.dist;
					if (delta > 0.00001f) return 1;
					else if (delta < -0.000001f) return -1;
					else return 0;
				});

				//assigning new grid and deployed rects
				//this should be done before calling Move (moves calls MM welding, and welding reads grid)
				lock (gridLocker)
					grid = dstGrid;

				int movedCount = moved.Count;
				for (int m=0; m<movedCount; m++)
					moved[m].tile.Move(moved[m].coord, moved[m].dist);
				Profiler.EndSample();
			}

		#endregion

		#region Helpers

			private static CoordRect[] GetDeployRects (Coord[] camCoords, int range)
			/// Converts each cam coord to chunk rect using the generate range
			{
				CoordRect[] deployRects = new CoordRect[camCoords.Length]; 

				for (int r=0; r<camCoords.Length; r++) 
					deployRects[r] = new CoordRect(camCoords[r].x - range, camCoords[r].z - range, range*2 +1, range*2 +1);

				return deployRects;
			}


			protected static float GetRemoteness (Coord coord, Coord[] camCoords)
			/// Returns an axis/priority distance to the closest cam
			{
				float minDist = float.MaxValue;

				if (camCoords == null) return minDist;

				for (int r=0; r<camCoords.Length; r++)
				{
					float dist = Coord.DistanceAxisPriority(camCoords[r], coord);
					if (dist < minDist) minDist = dist;
				}

				return minDist;
			}


			public virtual void RemoveNulls ()
			/// Removes tiles that were deleted externally from the collection
			{
				List<Coord> removedCoords = null;

				foreach (KeyValuePair<Coord,T> kvp in grid)
				{
					T tile = kvp.Value;

					if (tile == null || tile.IsNull) 
					{
						if (removedCoords == null) removedCoords = new List<Coord>(); //do not create list if there's nothing to remove
						removedCoords.Add(kvp.Key);
					}
				}
			
				if (removedCoords != null)
					foreach (Coord coord in removedCoords)
						grid.Remove(coord);
			}


			[Obsolete] private CoordRect WorldToChunksRect (CoordRect wrect, int size)
			/// Not used anywhere, but contains a tested code just in case
			{
				Coord cMin = new Coord(
					wrect.offset.x>=0 ? wrect.Min.x/size : (wrect.Min.x+1)/size-1,
					wrect.offset.z>=0 ? wrect.Min.z/size : (wrect.Min.z+1)/size-1 );
			
				Coord cMax = new Coord(
					wrect.offset.x+wrect.size.x>0 ? (wrect.offset.x+wrect.size.x-1)/size + 1 :  (wrect.offset.x+wrect.size.x)/size,
					wrect.offset.z+wrect.size.z>0 ? (wrect.offset.z+wrect.size.z-1)/size + 1 :  (wrect.offset.z+wrect.size.z)/size );
				//tested

				return new CoordRect(cMin, cMax-cMin);
			}




		#endregion


		#region Serialization 
		//generics do not serialize. Derive to use it.

			public T[] serializedTiles;
			public Coord[] serializedCoords;

			public virtual void OnBeforeSerialize ()
			{
				if (serializedTiles == null || serializedTiles.Length != grid.Count) serializedTiles = new T[grid.Count];
				if (serializedCoords == null || serializedCoords.Length != grid.Count) serializedCoords = new Coord[grid.Count];

				int counter = 0;
				foreach (var kvp in grid)
				{
					serializedTiles[counter] = kvp.Value;
					serializedCoords[counter] = kvp.Key;
					counter++;
				}
			}

			public virtual void OnAfterDeserialize ()
			{
				Dictionary<Coord,T> newTiles = new Dictionary<Coord,T>();
				for (int i=0; i<serializedTiles.Length; i++) 
				{
					if (serializedTiles[i] != null)
						newTiles.Add(serializedCoords[i], serializedTiles[i]);
				}
				lock (grid) { grid = newTiles; } 
			}

		#endregion
	}


	public interface IPinTile : ITile { void Pin(); }

	public class TilePinManager<T> : TileManager<T> where T: IPinTile, IEquatable<T>
	{	
		private Dictionary<Coord,T> pinned = new Dictionary<Coord,T>();

	
		public void Pin (Coord coord, MonoBehaviour holder=null)
		/// Creates new tile at the coord if it's empty and pin it
		{
			grid.TryGetValue(coord, out T tile);

			if (tile == null)
			{
				tile = ConstructTile(holder);
				grid.Add(coord, tile);

				tile.Pin();
				tile.Move(coord, camCoords != null ? GetRemoteness(coord,camCoords) : 0);
			}

			else
				tile.Pin(); 

			if (pinned.ContainsKey(coord))
				pinned.Add(coord, tile);
		}


		public void Unpin (Coord coord)
		/// Clears pin flag for tile at the coord and re-deploys grid to remove it if needed
		{
			if (!pinned.ContainsKey(coord)) return;

			pinned.Remove(coord);

			//re-deploying to find out if this tile should be removed or left as unpinned
			if (camCoords != null)
				Deploy(camCoords, pinned, holder:null); //deploying without holder since it shouldn't create new tiles anyways

			//no deploy was performed - removing pinned
			else
			{
				grid[coord].Remove();
				grid.Remove(coord);
			}
		}

		public void Deploy (Coord[] camCoords, MonoBehaviour holder=null)
			{ Deploy(camCoords, pinned, holder); }
	}
}