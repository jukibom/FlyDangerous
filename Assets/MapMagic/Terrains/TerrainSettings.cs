using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Core;


namespace MapMagic.Terrains
{
	[Serializable]
	public class TerrainSettings 
	{
		//terrain settings
		[Val(name="Auto Connect", cat="AutoConnect")]	public bool allowAutoConnect = true;
		[Val(name="Grouping ID", cat="AutoConnect")]	public int groupingID = 0;
		
		[Val(name="Base Map Dist.", cat="BaseMap")]	public int baseMapDist = 1000;
		[Val(name="Show Base Map", cat="BaseMap")]	public bool showBaseMap = true;
		[Val(name="Base Map Resolution", cat="BaseMap")] public int baseMapResolution = 1024;

		[Val(name="Draw Instanced", cat="Terrain")]	public bool drawInstanced = true;
		[Val(name="Pixel Error", cat="Terrain")]	public int pixelError = 1;
		#if UNITY_2019_1_OR_NEWER
		[Val(name="Cast Shadows", cat="Terrain")]	public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		#else
		[Val(name="Cast Shadows", cat="Terrain")]	public bool castShadows = false;
		#endif
		[Val(name="Reflection Probes", cat="Terrain")]	public UnityEngine.Rendering.ReflectionProbeUsage reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
		
		[Val(name="Editor Render Flags", cat="Misc")]	public TerrainRenderFlags editorRenderFlags = TerrainRenderFlags.All;
		[Val(name="Maximim LOD", cat="Misc")]	public int heightmapMaximumLOD = 0;
		
		//[Val(name="Physics Thickness", cat="Terrain")]	public int physicsThickness = 1;

		//materials
		//[Val(name="Material Type", cat="Materials")]	public Terrain.MaterialType materialType = Terrain.MaterialType.BuiltInStandard;
		//[Val(name="Custom Material", cat="Materials")] public bool useCustomTerrainMaterial = true;
		[Val(name="Material Template", cat="Materials", type=typeof(Material))]	public Material material = null;
		//[Val(name="Assign Material Copy", cat="Materials")] public bool copyMaterial = true;

		//details and trees
		[Val(name="Draw Detail", cat="Grass")]		public bool detailDraw = true;
		[Val(name="Detail Distance", cat="Grass")]	public float detailDistance = 80;
		[Val(name="Detail Density", cat="Grass")]	public float detailDensity = 1;

		[Val(name="Tree Distance", cat="Trees")]	public float treeDistance = 1000;
		[Val(name="Billboard Start", cat="Trees")]	public float treeBillboardStart = 200;
		[Val(name="Fade Length", cat="Trees")]		public float treeFadeLength = 5;
		[Val(name="Max Full LOD Trees", cat="Trees")]	public int treeFullLod = 150;
		[Val(name="Tree LOD Bias Multiplier", cat="Trees")]	public float treeLODBiasMultiplier = 1;  
		[Val(name="Bake Light Probes For Trees", cat="Trees")]	public bool bakeLightProbesForTrees = false;
		[Val(name="Remove Light Probe Ringing", cat="Trees")]	public bool deringLightProbesForTrees = true;

		[Val(name="Wind Speed", cat="WindTint")]		public float windSpeed = 0.5f;
		[Val(name="Wind Bending", cat="WindTint")]		public float windSize = 0.5f;  //bending is size and size is bending
		[Val(name="Wind Size", cat="WindTint")]			public float windBending = 0.5f;
		[Val(name="Grass Tint", cat="WindTint")]		public Color grassTint = Color.gray;


		//copy
		[Val(name="Copy Layers", cat="Copy")]	public bool guiCopy = false;
		[Val(name="Copy Tags", cat="Copy")]	public bool copyLayersTags = true;
		[Val(name="Copy Components", cat="Copy")]	public bool copyComponents = false;


		public void ApplyAll (Terrain terrain)
		{
			ApplySettings(terrain);
			ApplyMaterial(terrain);
			CopyLayersTagsComponents(terrain);
		}

		public void ApplySettings (Terrain terrain)
		{
			//terrain.legacyShininess
			//terrain.legacySpecular
			terrain.allowAutoConnect = allowAutoConnect;
			terrain.groupingID = groupingID;
			terrain.editorRenderFlags = editorRenderFlags;
			terrain.drawInstanced = drawInstanced;
			terrain.heightmapPixelError = pixelError;
			terrain.basemapDistance = showBaseMap ? baseMapDist : int.MaxValue;
			if (terrain.terrainData.baseMapResolution != baseMapResolution)
				terrain.terrainData.baseMapResolution = baseMapResolution; //will generate base map, takes some time
			#if UNITY_2019_1_OR_NEWER
			terrain.shadowCastingMode = shadowCastingMode;
			#else
			terrain.castShadows = castShadows;
			#endif
			//terrain.collectDetailPatches
			//terrain.patchBoundsMultiplier
			terrain.reflectionProbeUsage = reflectionProbeUsage;
			//terrain.lightmapIndex
			//terrain.realtimeLightmapIndex
			//terrain.lightmapScaleOffset
			//terrain.realtimeLightmapScaleOffset
			//terrain.freeUnusedRenderingResources
			terrain.heightmapMaximumLOD = heightmapMaximumLOD;

			terrain.drawTreesAndFoliage = detailDraw;
			terrain.detailObjectDistance = detailDistance;
			terrain.detailObjectDensity = detailDensity;
			terrain.treeDistance = treeDistance;
			terrain.treeBillboardDistance = treeBillboardStart;
			terrain.treeCrossFadeLength = treeFadeLength;
			terrain.treeLODBiasMultiplier = treeLODBiasMultiplier;
			terrain.treeMaximumFullLODCount = treeFullLod;
			#if UNITY_EDITOR
			terrain.bakeLightProbesForTrees = bakeLightProbesForTrees;
			terrain.deringLightProbesForTrees = deringLightProbesForTrees;
			#endif

			terrain.terrainData.wavingGrassSpeed = windSpeed;
			terrain.terrainData.wavingGrassAmount = windSize;
			terrain.terrainData.wavingGrassStrength = windBending;
			terrain.terrainData.wavingGrassTint = grassTint;
		}

		public void ApplyMaterial (Terrain terrain)
		/// Sets the terrain custom material state and assigns a copy of material reference if needed. Returns assigned material (material template copy)
		{
			#if !UNITY_2019_2_OR_NEWER
			terrain.materialType = Terrain.MaterialType.Custom;
			#endif

			terrain.materialTemplate = material;
		}

		public void CopyLayersTagsComponents (Terrain terrain)
		/// Copies assigned layers, tags and components from MapMagic object to terrain
		{
			//getting MapMagic game object
			GameObject go = terrain.gameObject;
			GameObject mmGo = go.transform.parent.parent.gameObject;

			//copy layer, tag, scripts from mm to terrains
			if (copyLayersTags)
			{
				go.layer = mmGo.layer;
				go.isStatic = mmGo.isStatic;
				try { go.tag = mmGo.tag; } catch { Debug.LogError("MapMagic: could not copy object tag"); }
			}
			if (copyComponents)
			{
				MonoBehaviour[] components = mmGo.GetComponents<MonoBehaviour>();
				for (int i=0; i<components.Length; i++)
				{
					if (components[i] is MapMagicObject || components[i] == null) continue; //if MapMagic itself or script not assigned
					if (terrain.gameObject.GetComponent(components[i].GetType()) == null) ReflectionExtensions.CopyComponent(components[i], go);
				}
			}
		}
	}
}