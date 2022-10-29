using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Den.Tools;
using MapMagic.Terrains;

#if VEGETATION_STUDIO_PRO
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.VegetationSystem;
using AwesomeTechnologies.Vegetation.Masks;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.Billboards;
#endif

namespace MapMagic.VegetationStudio
{
	[ExecuteInEditMode]
	public static class VSProOps
	/// Helper script to automatically add/remove and serialize textures and persistent storage data 
	{
		#if VEGETATION_STUDIO_PRO

		public static VegetationSystemPro CopyVegetationSystem (VegetationSystemPro src, Transform parent)
		/// Clones src VegetationSystemPro object as a child of parent
		{
			GameObject copiedSourceVSProGo = GameObject.Instantiate(src.gameObject, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
			copiedSourceVSProGo.SetActive(true);
			copiedSourceVSProGo.name = "VegetationSystemPro Copy";
			copiedSourceVSProGo.transform.parent = parent;
			copiedSourceVSProGo.transform.position = Vector3.zero;

			foreach (Transform child in copiedSourceVSProGo.transform)
				GameObject.DestroyImmediate(child.gameObject);

			VegetationSystemPro copiedSource = copiedSourceVSProGo.GetComponent<VegetationSystemPro>();
			copiedSource.PersistentVegetationStorage.PersistentVegetationStoragePackage = null; 
			return copiedSource;
		}


		public static UnityTerrain AddUnityTerrain (Terrain terrain)
		/// Adds VSPro UnityTerrain required component to terrain
		{
			UnityTerrain unityTerrain = terrain.gameObject.GetComponent<UnityTerrain>();
			if (unityTerrain == null) unityTerrain = terrain.gameObject.AddComponent<UnityTerrain>();
			unityTerrain.Terrain = terrain;
			unityTerrain.TerrainPosition = terrain.transform.position;
			return unityTerrain;
		}


		public static VegetationSystemPro GetTopVegetationSystem ()
		/// Finds main system to be used as source for clones
		{
			GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
			
			foreach (GameObject rootObject in rootObjects)
				if (rootObject.GetComponent<VegetationStudioManager>())
				{
					int childCount = rootObject.transform.childCount;
					for (int c=0; c<childCount; c++)
					{
						Transform child = rootObject.transform.GetChild(c);
						VegetationSystemPro sys = child.GetComponent<VegetationSystemPro>();

						if (sys != null)
							return sys;
					}
				}
			
			return null;
		}


		public static VegetationSystemPro GetCopyVegetationSystem (Terrain terrain)
		{
			Transform tileTfm = terrain.transform.parent;

			//VegetationSystemPro copySystem = tileTfm.GetComponentInChildren<VegetationSystemPro>();
			//seems not to be searching top level first, others then, but deep search from first object. Will iterate all objects.

			foreach (Transform child in tileTfm)
				if (child.name == "VegetationSystemPro Copy")
				{
					VegetationSystemPro copySystem = child.GetComponent<VegetationSystemPro>();
					return copySystem;
				}
			
			return null;
		}


		public static VegetationSystemPro UpdateCopySystem (VegetationSystemPro copySystem, Terrain terrain, VegetationPackagePro package, VegetationSystemPro srcSystem)
		{
			Transform tileTfm = terrain.transform.parent;

			//erasing legacy settings on copyVS change
			//checking if src system has any extents, and resetting them - otherwise refreshing clone systems will take too long
			if (srcSystem.VegetationSystemBounds.extents.x > 1) 
			{
				srcSystem.VegetationSystemBounds = new Bounds (Vector3.zero, Vector3.zero);
				srcSystem.RefreshVegetationSystem();
			}
			if (srcSystem.VegetationStudioTerrainObjectList.Count != 0)
				srcSystem.RemoveAllTerrains();

			//unityterrain
			UnityTerrain unityTerrain = terrain.GetComponent<UnityTerrain>();  //VS Pro component added to terrain
			if (unityTerrain == null) unityTerrain = VSProOps.AddUnityTerrain(terrain);
			unityTerrain.TerrainPosition = terrain.transform.position;

			//resetting clone package just in case
			if (copySystem.VegetationPackageProList.Count == 0  ||  copySystem.VegetationPackageProList[0] != package)
			{
				copySystem.VegetationPackageProList.Clear();
				copySystem.AddVegetationPackage(package);
			}
			
			//reset bounds
			copySystem.AutomaticBoundsCalculation = false;
			copySystem.VegetationSystemBounds = new Bounds (  //or use unityTerrain.TerrainBounds;
				terrain.terrainData.bounds.center + terrain.transform.position,
				terrain.terrainData.bounds.extents * 2);

			//add terrain
			if (copySystem.VegetationStudioTerrainObjectList.Count != 1  ||  copySystem.VegetationStudioTerrainObjectList[0] != terrain.gameObject)  
			{
				if (copySystem.VegetationStudioTerrainObjectList.Count != 0) copySystem.RemoveAllTerrains();
				copySystem.AddTerrain(terrain.gameObject);  //will call RefreshVegetationStudioTerrains and RefreshVegetationStudioTerrains
			}

			//clearing storage
			if (copySystem.PersistentVegetationStorage.PersistentVegetationStoragePackage == null)
				copySystem.PersistentVegetationStorage.PersistentVegetationStoragePackage = ScriptableObject.CreateInstance<PersistentVegetationStoragePackage>(); //new PersistentVegetationStoragePackage(); //changed on feedback from forum
				//clearing is done in apply

			//refresh system (takes >100 ms)
			UnityEngine.Profiling.Profiler.BeginSample("VegetationSystemPro.RefreshVegetationSystem");
//			copySystem.RefreshVegetationSystem();
			if (copySystem != null) //re-initializing
			{
				copySystem.enabled = false;
				copySystem.enabled = true; //to call private OnEnable
			}

			UnityEngine.Profiling.Profiler.EndSample();

			return copySystem;
		}


		public static void UpdateSourceSystem (VegetationSystemPro srcSystem, Terrain terrain)
		{
			//erasing legacy settings on copyVS change
			//returning extents after using per-terrain systems
			VegetationSystemPro copySystem = VSProOps.GetCopyVegetationSystem(terrain);

			if (srcSystem.VegetationSystemBounds.extents.x < 1) 
			{
				srcSystem.VegetationSystemBounds = new Bounds (
					terrain.transform.position + terrain.terrainData.size/2, 
					terrain.terrainData.size*3.4f);
				srcSystem.RefreshVegetationSystem();
			}

			if (copySystem != null  &&  
				srcSystem.PersistentVegetationStorage != null  &&  
				srcSystem.PersistentVegetationStorage.PersistentVegetationStoragePackage == copySystem.PersistentVegetationStorage.PersistentVegetationStoragePackage)
					srcSystem.PersistentVegetationStorage.InitializePersistentStorage();
			
			if (copySystem != null)
				GameObject.DestroyImmediate(copySystem.gameObject);

			//unityterrain
			UnityTerrain unityTerrain = terrain.GetComponent<UnityTerrain>();  //VS Pro component added to terrain
			if (unityTerrain == null) unityTerrain = VSProOps.AddUnityTerrain(terrain);
			unityTerrain.TerrainPosition = terrain.transform.position;

			if (srcSystem != null  &&  !srcSystem.VegetationStudioTerrainList.Contains(unityTerrain))  //check if already added since AddTerrain is long operation
			srcSystem.AddTerrain(terrain.gameObject);

			//refresh system (takes >100 ms)
			UnityEngine.Profiling.Profiler.BeginSample("VegetationSystemPro.RefreshVegetationSystem");
			srcSystem.RefreshVegetationSystem();
			UnityEngine.Profiling.Profiler.EndSample();
		}


		public static void SetTextures (VegetationSystemPro system, VegetationPackagePro package, Texture2D[] textures, int[] maskGroupNums, Rect terrainRect)
		{
			for (int i=0; i<textures.Length; i++)
			{
				Texture2D tex = textures[i];
				if (tex == null) continue;

				TextureMaskGroup maskGroup = package.TextureMaskGroupList[maskGroupNums[i]];

				//creating new mask only if the mask with the same rect doesn't exist
				TextureMask mask = maskGroup.TextureMaskList.Find(m => m.TextureRect == terrainRect);
				if (mask == null)
				{
					mask = new TextureMask { TextureRect = terrainRect };
					maskGroup.TextureMaskList.Add(mask);
				}

				mask.MaskTexture = tex;
			}

			//VegetationSystemPro system = GameObject.FindObjectOfType<VegetationSystemPro>();
			//if (system != null) 
			//	system.ClearCache(); //clearing cache causes flickering
			system.RefreshTerrainHeightmap();
		}


		public static void ClearTextures (VegetationPackagePro package, Rect terrainRect)
		{
			if (package == null) return;

			foreach (TextureMaskGroup maskGroup in package.TextureMaskGroupList)
			{
				for (int i=maskGroup.TextureMaskList.Count-1; i>=0; i--)
				{
					TextureMask mask = maskGroup.TextureMaskList[i];
					if (mask.MaskTexture == null  ||  mask.TextureRect == terrainRect)
					{
						mask.Dispose();
						maskGroup.TextureMaskList.RemoveAt(i);
						//if (system != null) system.SelectedTextureMaskGroupTextureIndex = 0;
					}
				}
			}

			//if (system != null) 
			//	system.ClearCache();
		}


		public static void SetObjects (VegetationSystemPro system, List<Transition>[] allTransitions, string[] allIds, byte sourceId)
		{
			PersistentVegetationStorage storage = system.PersistentVegetationStorage;

			//if (system.VegetationCellQuadTree == null) //could happen on scene loading or playmode change
			//	system.RefreshVegetationSystem();
			if (!system.InitDone)
				{ system.enabled = true; }

			for (int i=0; i<allTransitions.Length; i++)
			{
				if (allTransitions[i] == null) continue;
				foreach (Transition obj in allTransitions[i])
				{
					storage.AddVegetationItemInstance(
							allIds[i], 
							obj.pos,
							obj.scale, 
							obj.rotation,
							applyMeshRotation: true, 
							vegetationSourceID: sourceId, 
							distanceFalloff: 1, 
							clearCellCache:true);
				}
			}

			//for (int i=0; i<system.VegetationCellList.Count; i++)
			//	system.VegetationCellList[i].ClearCache();

			system.RefreshBillboards();

			#if UNITY_EDITOR
			if (storage.PersistentVegetationStoragePackage!=null)
				UnityEditor.EditorUtility.SetDirty(storage.PersistentVegetationStoragePackage);
			UnityEditor.EditorUtility.SetDirty(system);
			#endif
		}


		public static void FlushObjects (VegetationSystemPro system, Rect terrainRect, bool clearCache=true)
		{
			PersistentVegetationStorage storage = system.PersistentVegetationStorage;

			PersistentVegetationStoragePackage storagePackage = system.PersistentVegetationStorage.PersistentVegetationStoragePackage;
			if (storagePackage == null) return;

			List<VegetationCell> overlapCellList = new List<VegetationCell>(); 
			system.VegetationCellQuadTree.Query(terrainRect, overlapCellList);

			for (int i=0; i < overlapCellList.Count; i++)
			{
				int cellIndex = overlapCellList[i].Index;

				//storagePackage.PersistentVegetationCellList[cellIndex].ClearCell();

				var infoList = storagePackage.PersistentVegetationCellList[cellIndex].PersistentVegetationInfoList;
				for (int j=0; j<infoList.Count; j++)
				{
					var itemList = infoList[j].VegetationItemList;

					for (int k=itemList.Count-1; k>=0; k--)
					{
						Vector3 pos = itemList[k].Position + system.VegetationSystemPosition;
						Vector2 pos2 = pos.V2();

						if (terrainRect.Contains(pos2))
						{
							itemList.RemoveAt(k);
							//storage.RemoveVegetationItemInstance(infoList[j].VegetationItemID, pos, 1, clearCellCache:false);
						}
					}
				}

				//VegetationItemIndexes indexes = VegetationSystemPro.GetVegetationItemIndexes(vegetationItemID);                    
				//system.ClearCache(overlapCellList[i],indexes.VegetationPackageIndex,indexes.VegetationItemIndex);

				
			}

			//if (clearCache)
			//{
			//	for (int i=0; i<system.VegetationCellList.Count; i++)
			//		system.VegetationCellList[i].ClearCache();
			//}

			system.RefreshBillboards();
		}


		#endif
	}
}