
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.SceneEdit;

using MapMagic.Core;


///Draws tile frames around terrains

namespace MapMagic.Terrains.GUI
{
	public static class FrameDraw
	{
		public static readonly Color standardColor = new Color(0.3f, 0.5f, 0.8f, 1); 
		public static readonly Color pinColor = new Color(0.5f, 0.7f, 1, 1);
		public static readonly Color selectPreviewColor = new Color(0.2f, 0.4f, 0.7f, 1); 
		public static readonly Color previewColor = new Color(0.15f, 0.35f, 0.6f, 1); 
		public static readonly Color exportColor = new Color(0.31f, 0.55f, 0.26f);
		public static readonly Color unpinColor = new Color(1,0,0,1); 
		public const float dotsPerSide = 9.5f;
		public const float defaultZOffset = 0.05f;

		public static int width = 5;

		private static Texture2D clockIconTex;

		private static ConditionalWeakTable<Terrain,PolyLine> terrainLinesCache = new ConditionalWeakTable<Terrain, PolyLine>();

		#region Event

			[RuntimeInitializeOnLoadMethod, UnityEditor.InitializeOnLoadMethod] 
			static void Subscribe ()
			{
				TerrainTile.OnTileApplied += UpdateTile_OnTileApplied;
			}

			private static void UpdateTile_OnTileApplied (TerrainTile tile, Products.TileData tileData, Products.StopToken stop)
			{
				Terrain terrain = tile.GetTerrain(tileData.isDraft);
				if (terrain == null) return; //seems to be happen when stopping playmode while tile generating
				
				if (!terrainLinesCache.TryGetValue(terrain, out PolyLine polyLine))
				{
					polyLine = CreateTerrainLine(terrain);
					terrainLinesCache.Add(terrain, polyLine);
				}

				else
				{
					Vector3[] lineArr = CreateLinePoints(terrain);
					if (polyLine.MaxPoints < lineArr.Length)
					{
						polyLine = CreateTerrainLine(terrain);
						terrainLinesCache.Remove(terrain);
						terrainLinesCache.Add(terrain, polyLine);
					}

					else
						polyLine.SetPoints(lineArr);
				}
			}

		#endregion

		
		public static void DrawSceneGUI (MapMagicObject mapMagic)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Profiler.BeginSample("Drawing Frames");

				//drawing standard tiles
				foreach (TerrainTile tile in mapMagic.tiles.All())
				{
					if (!tile.Ready)
						DrawClock(tile, mapMagic.transform);

					if (tile != mapMagic.PreviewTile)
					{
						Terrain activeTerrain = tile.ActiveTerrain;
						if (activeTerrain != null)
							DrawTerrainFrame(activeTerrain, standardColor, dotted:tile.main==null);
					}
				}

				//preview tile above them
				if (mapMagic.PreviewTile != null &&  mapMagic.PreviewTile.main!=null)
				{
					DrawTerrainFrame(mapMagic.PreviewTile.main.terrain, previewColor, false, offset:defaultZOffset*1.5f);
				}

				Profiler.EndSample();
			}
		}


		public static void DrawClock (TerrainTile tile, Transform parent=null)
		{
			if (clockIconTex == null) clockIconTex = Resources.Load<Texture2D>("MapMagic/PreviewSandClock");
		
			Rect tileRect =  tile.WorldRect;
			Vector3 tileCenter = new Vector3(tileRect.center.x, 0, tileRect.center.y);
			if (parent != null) tileCenter = parent.TransformPoint(tileCenter);

			Terrain activeTerrain = tile.ActiveTerrain;
			if (activeTerrain != null) tileCenter.y = activeTerrain.terrainData.bounds.center.y;
			Vector2 screenPos = HandleUtility.WorldToGUIPoint(tileCenter);

			Handles.BeginGUI();
			UnityEngine.GUI.DrawTexture( new Rect(screenPos.x-clockIconTex.width/2, screenPos.y-clockIconTex.height/2, clockIconTex.width, clockIconTex.height), clockIconTex);
			Handles.EndGUI();
		}


		public static void DrawFrame (Coord coord, Vector3 tileSize, Color color, bool dotted, Dictionary<Coord,TerrainTile> terrainsLut=null,  float offset=defaultZOffset, Transform parent=null)
		/// Drawing a frame on terrain (if found in terrainsLut) or on the ground level
		{
			//drawing terrain
			Terrain terrain = null;
			if (terrainsLut != null  &&  terrainsLut.ContainsKey(coord))
				terrain = terrainsLut[coord].ActiveTerrain;
				
			if (terrain != null) 
				DrawTerrainFrame(terrain, color, dotted, offset);

			//drawing empty
			else
				DrawEmptyFrame(coord.vector3.Mul(tileSize), new Vector3(tileSize.x,0,tileSize.z), color, dotted, offset, parent);
		}


		public static void DrawEmptyFrame (Vector3 start, Vector3 size, Color color, bool dotted, float offset=defaultZOffset, Transform parent=null)
		{
			Vector3[] framePoints = new Vector3[] {
				start, 
				start + new UnityEngine.Vector3(0,0,size.z), 
				start + size, 
				start + new UnityEngine.Vector3(size.x,0,0), 
				start};

			if (parent != null)
				for (int i=0; i<framePoints.Length; i++)
					framePoints[i] = parent.TransformPoint(framePoints[i]);

			PolyLine.InstantLine(framePoints, color, width, dotted ? size.x/dotsPerSide : 0, offset:offset);
			//Handles.DrawAAPolyLine(width, framePoints);
		}


		public static void DrawTerrainFrame (Terrain terrain, Color color, bool dotted, float offset=defaultZOffset)
		{
			if (!terrainLinesCache.TryGetValue(terrain, out PolyLine polyLine))
			{
				polyLine = CreateTerrainLine(terrain);
				terrainLinesCache.Add(terrain, polyLine);
			}

			polyLine.DrawLine(color, width, dotted ? terrain.terrainData.size.x/dotsPerSide : 0, offset:offset, parent:terrain.transform);
		}
		

		private static PolyLine CreateTerrainLine (Terrain terrain)
		{
			Vector3[] lineArr = CreateLinePoints(terrain);
			PolyLine polyLine = new PolyLine(lineArr.Length);
			polyLine.SetPoints(lineArr);

			return polyLine;
		}


		private static Vector3[] CreateLinePoints (Terrain terrain)
		{
			TerrainData data = terrain.terrainData;
			int heightmapResolution = data.heightmapResolution;
			int resolution = heightmapResolution; //Mathf.Min(heightmapResolution, 128);

			Vector3 terrainPos = terrain.transform.localPosition;
			Vector3 terrainSize = data.size;

			Vector3[] lineArr = new Vector3[resolution*4 - 3]; //dnw 3 works

			float[,] heights = data.GetHeights(0,0, 1, heightmapResolution);
			for (int x=0; x<heightmapResolution; x++)
			{
				float val = heights[x,0]; //x and z swaped

				float percent = 1f * x / (heightmapResolution-1);
				lineArr[x] = new Vector3(terrainPos.x, val*terrainSize.y + terrainPos.y, terrainPos.z + percent*terrainSize.z);
			}

			heights = data.GetHeights(0,heightmapResolution-1, heightmapResolution, 1);
			for (int z=1; z<heightmapResolution; z++)
			{
				float val = heights[0,z]; //x and z swaped

				float percent = 1f * z / (heightmapResolution-1);
				lineArr[heightmapResolution-1 + z] = new Vector3(terrainPos.x + percent*terrainSize.x, val*terrainSize.y + terrainPos.y, terrainPos.z + terrainSize.z);
			}

			heights = data.GetHeights(heightmapResolution-1, 0, 1, heightmapResolution);
			for (int x=1; x<heightmapResolution; x++)
			{
				float val = heights[heightmapResolution-1-x,0]; //x and z swaped

				float percent = 1 - 1f * x / (heightmapResolution-1);
				lineArr[(heightmapResolution-1)*2 + x] = new Vector3(terrainPos.x + terrainSize.x, val*terrainSize.y + terrainPos.y, terrainPos.z + percent*terrainSize.z);
			}

			heights = data.GetHeights(0,0, heightmapResolution, 1);
			for (int z=1; z<heightmapResolution; z++)
			{
				float val = heights[0,heightmapResolution-1-z]; //x and z swaped

				float percent = 1 - 1f * z / (heightmapResolution-1);
				lineArr[(heightmapResolution-1)*3 + z] = new Vector3(terrainPos.x + percent*terrainSize.x, val*terrainSize.y + terrainPos.y, terrainPos.z);
			}

			return lineArr;
		}
	}
}