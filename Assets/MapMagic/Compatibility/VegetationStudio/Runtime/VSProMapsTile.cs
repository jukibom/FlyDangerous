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
	public class VSProMapsTile : MonoBehaviour
	/// Helper script to automatically add/remove and serialize textures and persistent storage data
	/// This will clear package mask on moving terrain and playmode disable as well
	{
		#if VEGETATION_STUDIO_PRO

		public VegetationSystemPro system;		//either source or clone system
		public VegetationPackagePro package;
		public Rect terrainRect;

		public Texture2D[] textures;
		public int[] maskGroupNums;
		[System.NonSerialized] public bool masksApplied;

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] 
		static void Subscribe ()  => TerrainTile.OnTileMoved += ClearOnMove;
		static void ClearOnMove (TerrainTile tile) 
		{
			VSProMapsTile vsTile = tile.GetComponent<VSProMapsTile>();
			vsTile?.OnDisable();
		}


		//public void OnEnable ()  //VSPro has got to run OnEnable first (objects only)
		public void Start () 
		{
			if (package == null) 
				return;

			if (!masksApplied) VSProOps.SetTextures(system, package, textures, maskGroupNums, terrainRect);
			masksApplied = true;
		}


		public void OnDisable ()
		{
			if (masksApplied) VSProOps.ClearTextures(package, terrainRect);
			masksApplied = false;
		}

		#endif
	}
}