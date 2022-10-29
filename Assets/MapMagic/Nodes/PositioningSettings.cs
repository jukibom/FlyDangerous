using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Products;
using MapMagic.Terrains;

namespace MapMagic.Nodes
{
	public enum BiomeBlend { Sharp, Random, Scale, Pure }

	[System.Serializable]
	public class PositioningSettings
	/// Contains the classical PRS settings for placing both trees and objects (for brush too)
	/// Not in objects but in Nodes since it's used by Brush core
	{
		//height
		public bool objHeight = true;
		public bool relativeHeight = true;
		public bool guiHeight;
		
		//rotation
		public bool useRotation = true; //in base since tree could also be rotated. Not the imposter ones, but anyways
		public bool takeTerrainNormal = false;
		public bool rotateYonly = false;
		public bool regardPrefabRotation = false;
		public bool guiRotation;

		//scale
		public bool useScale = true;
		public bool scaleYonly = false;
		public bool regardPrefabScale = false;
		public bool guiScale;


		public void MoveRotateScale (ref Transition trs, TileData data)
		/// Floors object, and erases (yep) trs roation/scale values according to layer settings
		{
			if (!objHeight) trs.pos.y = 0;

			//flooring
			float terrainHeight = 0;
			if (relativeHeight && data.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
				terrainHeight = data.heights.GetWorldInterpolatedValue(trs.pos.x, trs.pos.z, roundToShort:true);
			if (terrainHeight > 1) terrainHeight = 1;
			terrainHeight *= data.globals.height;  //all coords should be in world units
			trs.pos.y += terrainHeight; 

			if (!useScale) trs.scale = new Vector3(1,1,1);
			else if (scaleYonly) trs.scale = new Vector3(1, trs.scale.y, 1);

			if (!useRotation) trs.rotation = Quaternion.identity;
			else if (takeTerrainNormal) 
			{
				Vector3 terrainNormal = GetTerrainNormal(trs.pos.x, trs.pos.z, data.heights, data.globals.height, data.area.PixelSize.x);
				Vector3 terrainTangent = Vector3.Cross(trs.rotation*new Vector3(0,0,1), terrainNormal);
				trs.rotation = Quaternion.LookRotation(terrainTangent, terrainNormal);
			}
			else if (rotateYonly) trs.rotation = Quaternion.Euler(0,trs.Yaw,0);
		}


		public static Vector3 GetTerrainNormal (float fx, float fz, MatrixWorld heightmap, float heightFactor, float pixelSize)
		{
			Coord coord = heightmap.WorldToPixel(fx, fz);
			int pos = heightmap.rect.GetPos(coord);

			float curHeight = heightmap.arr[pos];
						
			float prevXHeight = curHeight;
			if (coord.x>=heightmap.rect.offset.x+1) prevXHeight = heightmap.arr[pos-1];

			float nextXHeight = curHeight;
			if (coord.x<=heightmap.rect.offset.x+heightmap.rect.size.x-1) nextXHeight = heightmap.arr[pos+1];
									
			float prevZHeight = curHeight;
			if (coord.z>=heightmap.rect.offset.z+1) prevZHeight = heightmap.arr[pos-heightmap.rect.size.x];

			float nextZHeight = curHeight;
			if (coord.z<=heightmap.rect.offset.z+heightmap.rect.size.z-1) nextZHeight = heightmap.arr[pos+heightmap.rect.size.z];

			return new Vector3((prevXHeight-nextXHeight)*heightFactor, pixelSize*2, (prevZHeight-nextZHeight)*heightFactor).normalized;
		}


		public static bool SkipOnBiome (ref Transition trs, BiomeBlend biomeBlend, MatrixWorld biomeMask, Noise random)
		/// True if object should not be spawned because of biome mask
		/// ref since it can change scale
		{
			float biomeFactor = biomeMask!=null ?  biomeMask.GetWorldInterpolatedValue(trs.pos.x, trs.pos.z) : 1;
			if (biomeFactor < 0.00001f) return true;

			bool skip;
			switch (biomeBlend)
			{
				case BiomeBlend.Sharp: 
					skip = biomeFactor < 0.5f;
					break;
				case BiomeBlend.Random:
					float rnd = random.Random((int)trs.pos.x, (int)trs.pos.y); //TODO: use id?
					if (biomeFactor > 0.5f) rnd = 1-rnd;
					skip = biomeFactor < rnd;
					break;
				case BiomeBlend.Scale:
					trs.scale *= biomeFactor;
					skip = biomeFactor < 0.0001f;
					break;
				case BiomeBlend.Pure:
					skip = biomeFactor < 0.9999f;
					break;
				default: skip = false; break;
			}

			return skip;
		}
	}
}