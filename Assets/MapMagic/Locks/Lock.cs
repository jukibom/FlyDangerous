using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.CompilerServices;

using Den.Tools;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Terrains;


namespace MapMagic.Locks
{
	[Serializable]
	public class Lock
	{
		public bool locked; //i.e. enabled

		public Vector3 worldPos;
		public float worldRadius = 100;
		public float worldTransition = 20;

		public bool rescaleDraft = true;
		public bool relativeHeight = false;

		public string guiName = "Location";
		public bool guiExpanded = true;
		public float guiHeight; //the height of the position handle

		//[NonSerialized] Dictionary<TileData,LockDataSet> lockDatas = new Dictionary<TileData, LockDataSet>();

		static Dictionary<TileData, Dictionary<Lock,LockDataSet>> lockDatas = new Dictionary<TileData, Dictionary<Lock, LockDataSet>>();

		#region Events

			#if UNITY_EDITOR
			[UnityEditor.InitializeOnLoadMethod]
			#endif
			[RuntimeInitializeOnLoadMethod] 
			static void Subscribe ()
			{
				TerrainTile.OnBeforeTilePrepare += OnTilePrepare_ReadLocks;
				TerrainTile.OnBeforeTileGenerate += OnGenerateStarted_ResizeDrafts;
				Graph.OnOutputFinalized += OnOutputFinalized_WriteLocksInThread;
				//TerrainTile.OnTileApplied += OnTileApplied_WriteLocksInApply; //using apply in thread instead
				TerrainTile.OnAllComplete += OnAllComplete_FlushAllLocks;  //just in case some locks still left

				TerrainTile.OnBeforeResetTerrain += OnTerrainReset_ReadLocks;
				TerrainTile.OnAfterResetTerrain += OnTerrainReset_WriteLocks;
			}


			public static void OnTilePrepare_ReadLocks (TerrainTile tile, TileData tileData)
			{
				//finding locks intersecting tile
				List<Lock> intersectingLocks = null;
				Lock[] allLocks = tile.mapMagic.locks;
				for (int i=0; i<allLocks.Length; i++)
				{
					if (!allLocks[i].locked) continue;
					if (!allLocks[i].IsIntersecting(tileData.area.active)) continue;
					if (tileData.isDraft && !allLocks[i].rescaleDraft) continue;

					if (intersectingLocks==null)
						intersectingLocks = new List<Lock>();
					intersectingLocks.Add(allLocks[i]);
				}

				if (intersectingLocks == null) //no locks on this tile
					return;

				//preparing lock-to-data dict
				Dictionary<Lock, LockDataSet> lockDatasDict;
				if (!lockDatas.TryGetValue(tileData, out lockDatasDict))
				{
					lockDatasDict = new Dictionary<Lock,LockDataSet>();
					lockDatas.Add(tileData, lockDatasDict);
				}

				//writing locks
				Terrain terrain = tile.GetTerrain(tileData.isDraft);

				int intersectingCount = intersectingLocks.Count;
				for (int i=0; i<intersectingCount; i++)
				{
					Lock lk = intersectingLocks[i];

					if (lockDatasDict.ContainsKey(lk)) continue;
					//do not read anything if already contains data ?

					LockDataSet lockData = new LockDataSet();
					lockData.Read(terrain, lk);
					lockDatasDict.Add(lk, lockData);
				}
			}


			public static void OnGenerateStarted_ResizeDrafts (TerrainTile tile, TileData draftTileData, StopToken stop)
			{
				if (!draftTileData.isDraft) return;

				TileData mainTileData = tile.main?.data;
				if (mainTileData == null) return;

				if (!lockDatas.TryGetValue(draftTileData, out Dictionary<Lock, LockDataSet> draftLockDatasDict)) return;
				if (!lockDatas.TryGetValue(mainTileData, out Dictionary<Lock, LockDataSet> mainLockDatasDict)) return;

				foreach (var kvp in draftLockDatasDict)
				{
					Lock lk = kvp.Key;
					if (!lk.rescaleDraft) continue;

					LockDataSet draftLockData = kvp.Value;
					LockDataSet mainLockData;

					if (!mainLockDatasDict.TryGetValue(lk, out mainLockData)) continue;

					LockDataSet.Resize(mainLockData, draftLockData);
				}
			}


			public static void OnOutputFinalized_WriteLocksInThread (Type type, TileData tileData, IApplyData applyData, StopToken stop)
			{
				Dictionary<Lock, LockDataSet> lockDatasDict;
				if (!lockDatas.TryGetValue(tileData, out lockDatasDict)) 
					return; 

				foreach (var kvp in lockDatasDict)
				{
					Lock lk = kvp.Key;
					LockDataSet lockData = kvp.Value;

					if (!lk.locked) continue;

					bool relativeHeight = lk.relativeHeight;
					if (lk.IsIntersecting(tileData.area.active)  &&  !lk.IsContained(tileData.area.active))
						relativeHeight = false;

					lockData.WriteInThread(applyData, relativeHeight);
				}
			}


			public static void OnTileApplied_WriteLocksInApply (TerrainTile tile, TileData tileData, StopToken stop)
			{
				Dictionary<Lock, LockDataSet> lockDatasDict;
				if (!lockDatas.TryGetValue(tileData, out lockDatasDict)) return;

				Terrain terrain = tile.GetTerrain(tileData.isDraft);

				foreach (LockDataSet lockData in lockDatasDict.Values)
					lockData.WriteInApply(terrain, resizeTerrain:false);

				if (!tileData.isDraft)
					lockDatas.Remove(tileData);
					//leaving lock data for draft since it might be currently generating and nearly applyied (and there is no data left!)
			}


			public static void OnAllComplete_FlushAllLocks (MapMagicObject mapMagic)
			{
				lockDatas.Clear();
			}

		#endregion

		#region Preserve Lock while reset

			//Non-threaded, executed in a single frame

			private static Dictionary<Lock, LockDataSet> mainDatasDict;
			private static Dictionary<Lock, LockDataSet> draftDatasDict;

			public static void OnTerrainReset_ReadLocks (TerrainTile tile)
			{
				//finding locks intersecting tile
				List<Lock> intersectingLocks = null;
				Lock[] allLocks = tile.mapMagic.locks;
				for (int i=0; i<allLocks.Length; i++)
				{
					if (!allLocks[i].locked) continue;
					if (!allLocks[i].IsIntersecting(tile.WorldRect)) continue;

					if (intersectingLocks==null)
						intersectingLocks = new List<Lock>();
					intersectingLocks.Add(allLocks[i]);
				}

				if (intersectingLocks == null) //no locks on this tile
					return;

				//writing locks
				if (tile.main != null) mainDatasDict = new Dictionary<Lock,LockDataSet>();
				if (tile.draft != null) draftDatasDict = new Dictionary<Lock,LockDataSet>();

				foreach (Lock lk in intersectingLocks)
				{
					if (tile.main != null)
					{
						LockDataSet lockData = new LockDataSet();
						lockData.Read(tile.main.terrain, lk);
						mainDatasDict.Add(lk, lockData);
					}

					if (tile.draft != null)
					{
						LockDataSet lockData = new LockDataSet();
						lockData.Read(tile.draft.terrain, lk);
						draftDatasDict.Add(lk, lockData);
					}
				}
			}

			public static void OnTerrainReset_WriteLocks (TerrainTile tile)
			{
				if (tile.main != null  &&  mainDatasDict != null)
				{
					foreach (LockDataSet lockData in mainDatasDict.Values)
						lockData.WriteInApply(tile.main.terrain, resizeTerrain:true);
				}

				if (tile.draft != null  &&  draftDatasDict != null)
				{
					foreach (LockDataSet lockData in draftDatasDict.Values)
						lockData.WriteInApply(tile.draft.terrain, resizeTerrain:true);
				}

				mainDatasDict = null;
				draftDatasDict = null;
			}


		#endregion

		#region Legacy 
		/*public void ReadLock (TerrainTile tile, TileData tileData)
		{
			if (!locked) return;
			if (!IsIntersecting(tileData.area)) return;

			lockDatas.TryGetValue(tileData, out LockDataSet lockData);

			//if (tileData.isDraft && lockData != null) continue; //do not reload draft data if draft is already generating
			//if (lockData != null) return; //do not reload any data if tile is already generating

			if (lockData == null)
			{
				lockData = new LockDataSet(); 
				lockDatas.Add(tileData, lockData); 
			}

			Terrain terrain = tile.GetTerrain(tileData.isDraft);
			lockData.Read(terrain, worldPos, worldRadius, worldTransition);
		}


		public void ProcessLock (TerrainTile tile, TileData tileData, StopToken stop)
		{
			if (!lockDatas.TryGetValue(tileData, out LockDataSet lockData)) return;
			if (!IsIntersecting(tileData.area) || !locked) return;

			float heightDelta = 0;
			if (relativeHeight && !(tileData.isDraft && rescaleDraft))
				heightDelta = lockData.GetHeightDelta(tileData.apply);

			//re-scaling draft data
			if (tileData.isDraft && rescaleDraft) 
			{
				TileData mainTileData = tile.main.data;
				if (lockDatas.TryGetValue(mainTileData, out LockDataSet mainLockData))
				{
					LockDataSet.Resize(mainLockData, lockData);

					if (relativeHeight)
						heightDelta = mainLockData.GetHeightDelta(mainTileData.apply);
				}
			}

			lockData.Process(tileData.apply, heightDelta);
		}


		public void WriteLock (TerrainTile tile, TileData tileData, StopToken stop)
		{
			if (!lockDatas.TryGetValue(tileData, out LockDataSet lockData)) return;
			if (!IsIntersecting(tileData.area) || !locked) return;

			Terrain terrain = tile.GetTerrain(tileData.isDraft);
			lockData.Write(terrain);

			//if (!stop.stop  &&  !stop.restart) //if non-stopped and draft not restarted
			//	lockDatas.Remove(tileData); //flushing after writing
		}*/
		#endregion

		#region Intersections

		public bool IsIntersecting (Terrain terrain)
			/// Checks if this terrain contains this lock
			{
				float fullRadius = worldRadius + worldTransition;

				Vector3 terrainPos = terrain.transform.localPosition;
				TerrainData terrainData = terrain.terrainData;
				Vector3 terrainSize = terrainData.size;

				if (terrainPos.x > worldPos.x + fullRadius  ||  terrainPos.x + terrainSize.x < worldPos.x - fullRadius ||
					terrainPos.z > worldPos.z + fullRadius  ||  terrainPos.z + terrainSize.z < worldPos.z - fullRadius )
						return false;

				return true;
			}


			public bool IsIntersecting (Area.Dimensions dim)
			/// Checks if locks placed within the area or intersects it
			{
				float fullRadius = worldRadius + worldTransition;

				if (dim.worldPos.x > worldPos.x + fullRadius  ||  dim.worldPos.x + dim.worldSize.x < worldPos.x - fullRadius ||
					dim.worldPos.z > worldPos.z + fullRadius  ||  dim.worldPos.z + dim.worldSize.z < worldPos.z - fullRadius )
						return false;

				return true;
			}

			public bool IsIntersecting (Rect rect)
			/// Checks if locks placed within the area or intersects it
			{
				Vector2 min = rect.min;
				Vector2 max = rect.max;

				float fullRadius = worldRadius + worldTransition;

				if (worldPos.x+fullRadius < min.x  ||  worldPos.x-fullRadius > max.x ||
					worldPos.z+fullRadius < min.y  ||  worldPos.z-fullRadius > max.y )
						return false;

				return true;
			}


			public bool IsContained (Area.Dimensions dim)
			/// Checks if this lock is placed fully within area.dimensions
			{
				float fullRadius = worldRadius + worldTransition;

				if (worldPos.x-fullRadius > dim.worldPos.x  &&  worldPos.x+fullRadius < dim.worldPos.x+dim.worldSize.x  &&
					worldPos.z-fullRadius > dim.worldPos.z  &&  worldPos.z+fullRadius < dim.worldPos.z+dim.worldSize.z)
						return true;

				return false;
			}


			public bool IsContained (Rect rect)
			/// Checks if this lock is placed fully within area.dimensions
			{
				Vector2 min = rect.min;
				Vector2 max = rect.max;

				float fullRadius = worldRadius + worldTransition;

				if (worldPos.x-fullRadius > min.x  &&  worldPos.x+fullRadius < max.x  &&
					worldPos.z-fullRadius > min.y  &&  worldPos.z+fullRadius < max.y)
						return true;

				return false;
			}


			public bool IsContainedInAll (IEnumerable<Rect> rects)
			/// Checks if lock is on all the terrains, and not intersecting outer terrain group borders (however it might be placed on several terrains)
			{
				CoordRect lockRect = new CoordRect( new Coord((int)(worldPos.x+0.5f), (int)(worldPos.z+0.5f)), (int)(worldRadius+worldTransition) );
				int numUnits = lockRect.size.x * lockRect.size.z;
			
				foreach (Rect rect in rects)
				{
					CoordRect areaRect = new CoordRect((int)(rect.position.x+0.5f), (int)(rect.position.y+0.5f), (int)(rect.size.x+0.5f), (int)(rect.size.y+0.5f) );
					CoordRect intersection = CoordRect.Intersected(lockRect, areaRect);
					numUnits -= intersection.size.x*intersection.size.z;
				}

				return numUnits <= 0;
			}


			public bool IsContainedInAny (IEnumerable<Rect> rects)
			/// Checks if lock is fully on one of terrains, and not intersecting seam of this or other terrains
			{
				bool containedInAny = false;

				foreach (Rect rect in rects)
				{
					bool contained = IsContained(rect);
					bool intersects = IsIntersecting(rect);

					if (intersects && !contained) return false;

					if (contained) containedInAny = true;
				}

				return containedInAny;
			}

		#endregion
	}
}