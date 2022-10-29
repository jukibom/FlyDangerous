using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; //to copy properties


namespace Den.Tools
{
	public enum TerrainControlType { Height, Splats, Grass }

	static public class TerrainExtensions
	{
		public static int GetResolution (this Terrain terrain, TerrainControlType controlType)
		{
			TerrainData terrainData = terrain.terrainData;

			switch (controlType)
			{
				case TerrainControlType.Height: return terrainData.heightmapResolution;
				case TerrainControlType.Splats: return terrainData.alphamapResolution;
				case TerrainControlType.Grass: return terrainData.detailResolution;
				default: return 0;
			}
		}


		public static Vector2D PixelSize (this Terrain terrain, TerrainControlType controlType)
		{
			Vector2D worldSize = (Vector2D)terrain.terrainData.size;
			int resolution = GetResolution(terrain, controlType);
			return worldSize / (resolution-1); //since terrain size looses half pixel from both sides
		}


		public static Vector2D PixelSize (this Terrain terrain, int resolution)
		{
			Vector2D worldSize = (Vector2D)terrain.terrainData.size;
			return worldSize / (resolution-1); //since terrain size looses half pixel from both sides
		}


		public static CoordRect PixelRect (this Terrain terrain, Vector2D worldPos, Vector2D worldSize, TerrainControlType controlType)
		/// Convering offset/size rect to pixel rect
		{
			Vector2D pixelSize = PixelSize(terrain, controlType);
			return CoordRect.WorldToPixel(worldPos, worldSize, pixelSize);
		}


		public static CoordRect PixelRect (this Terrain terrain, Vector2D worldPos, Vector2D worldSize, int resolution)
		{
			Vector2D pixelSize = PixelSize(terrain, resolution);
			return CoordRect.WorldToPixel(worldPos, worldSize, pixelSize);
		}


		public static CoordRect PixelRect (this Terrain terrain, TerrainControlType controlType)
		/// Getting whole terrain pixel rect
		{
			int resolution = GetResolution(terrain, controlType);
			Vector2D pixelSize = PixelSize(terrain, controlType);

			return new CoordRect(
				Mathf.RoundToInt(terrain.transform.position.x/pixelSize.x),
				Mathf.RoundToInt(terrain.transform.position.z/pixelSize.z),
				resolution,
				resolution);
		}


		public static CoordRect PixelRect (this Terrain terrain, int resolution)
		/// Getting whole terrain pixel rect
		{
			Vector2D pixelSize = PixelSize(terrain, resolution);

			return new CoordRect(
				Mathf.RoundToInt(terrain.transform.position.x/pixelSize.x),
				Mathf.RoundToInt(terrain.transform.position.z/pixelSize.z),
				resolution,
				resolution);
		}


		/*public static CoordRect HeightAlignedPixelRect (this Terrain terrain, Vector2D worldPos, Vector2D worldSize, TerrainControlType controlType)
		/// Returns non-heightmap rect. It's size perfectly corresponds with height resolution (if it's twice smaller - the rect size will always be twice smaller)
		{
			int splatsResolution = GetResolution(terrain, controlType);
			if (splatsResolution == 0) //no output of this type is assigned
				splatsResolution = terrain.terrainData.heightmapResolution; 

			int heightResolution = terrain.terrainData.heightmapResolution;
			float ratio = HeightAlignedRatio(heightResolution, splatsResolution);

			CoordRect heightRect = PixelRect(terrain, worldPos, worldSize, TerrainControlType.Height);
			return heightRect * ratio;
		}*/


		public static float HeightAlignedRatio (int heightResolution, int splatsResolution)
		/// Returns proper ratio for HeightAlignedPixelRect
		/// Tested (MiscTests)
		{
			float ratio = 1f * splatsResolution / heightResolution;
			
			//trying to find perfect ratio (0.25, 0.5, 1, 2, etc)
			if (splatsResolution > heightResolution)
			{
				int pRatio = (int)(ratio+0.5f);
				int restoredRes = heightResolution*pRatio;
				if (restoredRes-splatsResolution >= -pRatio  ||  restoredRes-splatsResolution <= pRatio)
					ratio = pRatio;
			}
			else
			{
				int invpRatio = (int)(1f/ratio + 0.5f);
				int restoredRes = splatsResolution*invpRatio;
				if (restoredRes-splatsResolution >= -invpRatio  ||  restoredRes-splatsResolution <= invpRatio)
					ratio = 1f / invpRatio;
			}

			return ratio;
		}



		public static void FastResize (this Terrain terrain, int resolution, Vector3 size)
		{
			//setting resolution and THEN terrain size is too laggy
			//so making this trick to resize terrain or change res
			if ((terrain.terrainData.size-size).sqrMagnitude > 0.01f || terrain.terrainData.heightmapResolution != resolution) 
			{
				if (resolution <= 64) //brute force
				{
					terrain.terrainData.heightmapResolution = resolution;
					terrain.terrainData.size = new Vector3(size.x, size.y, size.z);
				}

				else //setting res 64, re-scaling to 1/64, and then changing res
				{
					terrain.terrainData.heightmapResolution = 65;
					terrain.Flush(); //otherwise unity crushes without an error
					int resFactor = (resolution-1) / 64;
					terrain.terrainData.size = new Vector3(size.x/resFactor, size.y, size.z/resFactor);
					terrain.terrainData.heightmapResolution = resolution;
				}
			}
		}


		public static bool Contains (this Terrain terrain, Vector3 pos)
		{
			Vector3 tpos = terrain.transform.position;
			Vector3 tsize = terrain.terrainData.size;
			if (pos.x>tpos.x && pos.z>tpos.z && pos.x<tpos.x+tsize.x && pos.z<tpos.z+tsize.z) return true;
			else return false;
		}


		public static float SampleAverageHeight (this Terrain terrain, Vector3 pos, int pixelExtent)
		{
			TerrainData terrainData = terrain.terrainData;
			int heightmapResolution = terrainData.heightmapResolution;
			Coord pixelPos = new Coord();
			pixelPos.x = (int)((pos.x-terrain.transform.position.x) / terrainData.size.x * heightmapResolution);
			pixelPos.z = (int)((pos.z-terrain.transform.position.z) / terrainData.size.z * heightmapResolution);
			CoordRect pixelRect = new CoordRect(pixelPos, pixelExtent);
			pixelRect = CoordRect.Intersected(pixelRect, new CoordRect(0,0,heightmapResolution,heightmapResolution));

			float avg = 0;

			float[,] heights = terrainData.GetHeights(pixelRect.offset.x, pixelRect.offset.z, pixelRect.size.x, pixelRect.size.z);

			for (int x=0; x<pixelRect.size.x; x++)
				for (int z=0; z<pixelRect.size.z; z++)
					avg += heights[z,x];

			return avg / (pixelRect.size.x*pixelRect.size.z) * terrainData.size.y;
		}


		public static UnityEngine.Object Object (this DetailPrototype prot)
		{
			if (prot.renderMode == DetailRenderMode.VertexLit)
				return prot.prototype;

			else
				return prot.prototypeTexture;
		}

		public static TerrainData Copy (this TerrainData src)
		{
			TerrainData dst = new TerrainData();

			dst.heightmapResolution = src.heightmapResolution;
			dst.SetHeights(0,0, src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution));

			dst.terrainLayers = src.terrainLayers;
			dst.alphamapResolution = src.alphamapResolution;
			dst.SetAlphamaps(0,0, src.GetAlphamaps(0,0, src.alphamapResolution, src.alphamapResolution));

			dst.detailPrototypes = src.detailPrototypes;
			dst.SetDetailResolution(src.detailResolution, src.detailResolutionPerPatch);
			int numGrass = src.detailPrototypes.Length;
			for (int i=0; i<numGrass; i++)
				dst.SetDetailLayer(0,0,i, src.GetDetailLayer(0,0,src.detailResolution, src.detailResolution, i));

			dst.treePrototypes = src.treePrototypes;
			dst.treeInstances = src.treeInstances;

			dst.size = src.size; //after changing all resolutions
			
			return dst;
		}


		public static bool CheckSplatsSum (this TerrainData data, out string error)
		// True if sum is 1 for each pixel
		{
			int resolution = data.alphamapResolution;
			float[,,] splats = data.GetAlphamaps(0,0, resolution, resolution);
			int numChannels = splats.GetLength(2);

			for (int x=0; x<resolution; x++)
				for (int z=0; z<resolution; z++)
				{
					float sum = 0;

					for (int c=0; c<numChannels; c++)
						sum += splats[x,z,c];

					if (sum < 0.999f || sum > 1.01f)
					{
						error = $"Sum not equals to 1 at {x}, {z}, sum: {sum}";
						return false;
					}
				}

			error = null;
			return true;
		}
	}
}
