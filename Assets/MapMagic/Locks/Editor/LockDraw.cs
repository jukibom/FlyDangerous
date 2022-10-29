
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Terrains;
using MapMagic.Core.GUI;

namespace MapMagic.Locks
{
	public static class LockDraw
	{
		static readonly Color lockColor = new Color(1,0.4f,0,1); 
		static readonly Color transitionColor = new Color(0.6f, 0.1f, 0, 1); //alpha is 1
		static readonly Color illegalLockColor = new Color(1,0,0.2f,1);
		static readonly Color illegalTransitionColor = new Color(0.5f,0,0.1f,1);
		const int numCorners = 128;
		const float lineThickness = 5;

		static private PolyLine polyLine = new PolyLine(numCorners);
		static private Vector3[] corners = new Vector3[numCorners];
		

		public static void DrawSceneGUI (MapMagicObject mapMagic)
		{
			//drawing locks
			if (Event.current.type == EventType.Repaint)
			{
				UnityEngine.Profiling.Profiler.BeginSample("Drawing Locks");

				int curNumCorners = numCorners;
				if (mapMagic.locks.Length > 3) curNumCorners = numCorners/2;
				if (mapMagic.locks.Length > 6) curNumCorners = numCorners/4;
				if (corners.Length > curNumCorners) corners = new Vector3[curNumCorners];

				//list of all terrains to draw brush
				//Terrain[] terrains = mapMagic.GetComponentsInChildren<Terrain>();
				List<Terrain> terrains = new List<Terrain>(mapMagic.tiles.AllActiveTerrains());
				List<Rect> terrainRects = new List<Rect>(mapMagic.tiles.AllWorldRects());

				//offseting MM-coordsys on mm pos
				//for (int i=0; i<terrainRects.Count; i++) 
				//	terrainRects[i] = new Rect(terrainRects[i].position + mapMagic.transform.position.V2(), terrainRects[i].size);

				for (int l=0; l<mapMagic.locks.Length; l++)
				{
					Lock lockArea = mapMagic.locks[l];
					//if (!lockArea.guiExpanded) continue;

					Color color = lockColor;
					Color tcolor = transitionColor;
					if (!lockArea.IsContainedInAll(terrainRects))
						{ color = illegalLockColor; tcolor = illegalTransitionColor; }

					lockArea.guiHeight = DrawCircle(
						polyLine, 
						lockArea.worldPos, 
						lockArea.worldRadius, 
						color, corners, terrains, terrainRects,
						parent:mapMagic.transform);

					DrawCircle(
						polyLine, 
						lockArea.worldPos, 
						lockArea.worldRadius+lockArea.worldTransition, 
						tcolor, corners, terrains, terrainRects,
						parent:mapMagic.transform);
				}

				UnityEngine.Profiling.Profiler.EndSample();
			}


			//drawing locks handles
			UnityEngine.Profiling.Profiler.BeginSample("Drawing Lock Handles " + Event.current.type);

			for (int l=0; l<mapMagic.locks.Length; l++)
			{
				Lock lockArea = mapMagic.locks[l];

				if (!lockArea.locked)
				{
					UnityEditor.Undo.RecordObject(mapMagic, "MapMagic Lock Move");

					Vector3 lockCenter = lockArea.worldPos + mapMagic.transform.position;
					lockCenter.y = lockArea.guiHeight;
					lockCenter = Handles.PositionHandle(lockCenter, Quaternion.identity);
					lockArea.worldPos = new Vector3(lockCenter.x, 0, lockCenter.z) - mapMagic.transform.position;
				}
				//Handles.ArrowHandleCap(0, lk.worldPos, Quaternion.identity, 100, EventType.Repaint);
			}

			UnityEngine.Profiling.Profiler.EndSample();
		}


		public static void DrawInspectorGUI (MapMagicObject mapMagic)
		{
			/*using (Cell.Line)
			{
				//Cell.current.margins = new Padding(-2,0);
				foreach (int num in LayersEditor.DrawLayersEnumerable(
					mapMagic.locks.Length,
					onAdd:n => AddLockLayer(mapMagic,n),
					onRemove:n => RemoveLockLayer(mapMagic,n),
					onMove:(n1,n2) => MoveLockLayer(mapMagic,n1,n2)) )
					{
						using (Cell.Line)
							DrawLock(mapMagic.locks[num]);
					}
			}*/

			using (Cell.Line)
			{
				LayersEditor.DrawLayers(
					mapMagic.locks.Length,
					onDraw:n => DrawLock(mapMagic.locks[n]),
					onAdd:n => AddLockLayer(mapMagic,n),
					onRemove:n => RemoveLockLayer(mapMagic,n),
					onMove:(n1,n2) => MoveLockLayer(mapMagic,n1,n2));
			}
		}

		private static void DrawLock (Lock lk)
		{
			bool locked = lk.locked;
			Texture2D icon = locked ? UI.current.textures.GetTexture("DPUI/Icons/LockLocked") : UI.current.textures.GetTexture("DPUI/Icons/LockUnlocked");

			Cell.EmptyLinePx(4);

			using (Cell.LineStd)
			{
				using (Cell.RowPx(20)) Draw.Icon(icon); 
				
				using (Cell.Row) Draw.EditableLabel(ref lk.guiName);

				using (Cell.RowPx(20))
					lk.guiExpanded = Draw.CheckButton(lk.guiExpanded, 
						UI.current.textures.GetTexture("DPUI/Chevrons/Down"),  
						UI.current.textures.GetTexture("DPUI/Chevrons/Left"), 
						iconScale:0.5f,
						visible:false );
			}

			

			if (lk.guiExpanded)
			{
				Cell.EmptyLinePx(4);

				using (Cell.Line)
				{
					Cell.EmptyRowPx(20);

					using (Cell.Row)
					{
						using (Cell.Line)
						{
							Cell.current.disabled = locked;

							using (Cell.LineStd)
							{
								using (Cell.RowRel(0.3f)) Draw.Label("Position");
								using (Cell.RowRel(0.7f))
								{
									Cell.current.fieldWidth = 0.8f;
									using (Cell.Row) Draw.Field(ref lk.worldPos.x, "X");
									using (Cell.Row) Draw.Field(ref lk.worldPos.z, "Z");
								}
							}

							Cell.current.fieldWidth = 0.7f;
							using (Cell.LineStd) Draw.Field(ref lk.worldRadius, "Radius");
							using (Cell.LineStd) Draw.Field(ref lk.worldTransition, "Transition");

							if (Cell.current.valChanged)
								SceneView.lastActiveSceneView?.Repaint();
						}

						bool isContainedInAll = lk.IsContainedInAll(MapMagicInspector.current.mapMagic.tiles.AllWorldRects()); //aka is fully on terrains
						bool isContainedInAny = lk.IsContainedInAny(MapMagicInspector.current.mapMagic.tiles.AllWorldRects()); //aka is on seam

						using (Cell.LineStd) Draw.ToggleLeft(ref lk.rescaleDraft, "Sync Draft");
						using (Cell.LineStd) 
						{
							Cell.current.disabled = !isContainedInAny;
							Draw.ToggleLeft(ref lk.relativeHeight, "Relative Height (beta)");
						}

						if (!isContainedInAny)
							using (Cell.LinePx(42))
								Draw.Helpbox("This lock is partly placed on terrain edge. The Relative Height feature is not available.");
						
						if (!isContainedInAll)
							using (Cell.LinePx(42))
								Draw.Helpbox("This lock is partly placed on non-pinned terrain. It could not be welded with the newly generated terrains this way.");


						using (Cell.LinePx(22))
						{
							Draw.CheckButton(ref locked, "");
							if (Cell.current.valChanged)
								lk.locked = locked;

							Cell.EmptyRowRel(1);
							using (Cell.RowPx(14)) Draw.Icon(icon);
							using (Cell.RowPx(35))
							{
								Cell.EmptyLineRel(0.5f);
								using (Cell.LinePx(20)) Draw.Label("Lock", style:UI.current.styles.middleLabel);
								Cell.EmptyLineRel(0.5f);
							}
							Cell.EmptyRowRel(1);
						}
					}

					Cell.EmptyRowPx(4);

					//if (Draw.Button("Lock Current"))
					//	lk.ReadTerrain();
				}
			}

			Cell.EmptyLinePx(4);
		}

		private static void AddLockLayer (MapMagicObject mapMagic, int num) 
		{ 
			Lock newLock = new Lock();
			newLock.guiName = "Location " + mapMagic.locks.Length;
			newLock.worldPos = new Vector3(mapMagic.tileSize.x/2, 0, mapMagic.tileSize.z/2); //placing at the center of the tile 0

			ArrayTools.Insert(ref mapMagic.locks, mapMagic.locks.Length, newLock);
			//mapMagic.ClearAll(); //don't jenerate on adding lock (we might have a custom location prepared)
			//mapMagic.StartGenerate();
			//mapMagic.GenerateAll();
			SceneView.RepaintAll();
		}
		public static void RemoveLockLayer (MapMagicObject mapMagic, int num) 
		{ 
			ArrayTools.RemoveAt(ref mapMagic.locks, num);
			//mapMagic.ClearAll();
			//mapMagic.StartGenerate();
			//mapMagic.GenerateAll();
			SceneView.RepaintAll();
		}
		public static void MoveLockLayer (MapMagicObject mapMagic, int from, int to) 
		{ 
			ArrayTools.Move(mapMagic.locks, from, to);
			//mapMagic.ClearAll();
			//mapMagic.StartGenerate();
			//mapMagic.GenerateAll();
			SceneView.RepaintAll();
		}


		public static float DrawCircle (PolyLine line, Vector3 center, float radius, Color color, Vector3[] corners, List<Terrain> terrains, List<Rect> terrainRects, Transform parent=null)
		/// It will return an average height BTW almost for free :)
		{
			int numCorners = corners.Length;
			float step = 360f/(numCorners-1);

			Terrain prevTerrain = null;
			Rect prevRect = new Rect();

			for (int i=0; i<corners.Length; i++)
			{
				//corner initial position
				Vector3 corner = new Vector3( Mathf.Sin(step*i*Mathf.Deg2Rad), 0, Mathf.Cos(step*i*Mathf.Deg2Rad) ) * radius + center;

				//checking if the corner lays within the same terrain first
				Terrain terrain = null;
				if (prevRect.Contains( new Vector2(corner.x, corner.z) ))
					terrain = prevTerrain;

				//finding proper terrain in all terrains in it's not in rect
				else
				{
					int rectsCount = terrainRects.Count;
					for (int r=0; r<rectsCount; r++)
					{
						if (terrainRects[r].Contains( new Vector2(corner.x, corner.z) ))
						{
							terrain = terrains[r]; 
							prevTerrain = terrains[r];
							prevRect = terrainRects[r];
							break; 
						}
					}
				}

				//sampling height
				corners[i] = corner;
				if (terrain != null) corners[i].y = terrain.SampleHeight(corner);
			}

			line.DrawLine(corners, color, lineThickness, zMode:PolyLine.ZMode.Overlay, offset:0, parent:parent);
			//Handles.DrawAAPolyLine(lineThickness, corners);

			//adjusting center height
			float heightSum = 0;
			for (int i=0; i<corners.Length; i++)
				heightSum += corners[i].y;
			return heightSum/(corners.Length-1);
		}
	}

}
