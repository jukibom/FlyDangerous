using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.SceneEdit;

using MapMagic.Core;


namespace MapMagic.Terrains.GUI
{
	public static class PinDraw
	{
		private static readonly Color pinButtonColor = new Color(0.5f, 0.58f, 0.7f);
		private static readonly Color previewButtonColor = new Color(0.22f, 0.42f, 0.69f);
		private static readonly Color exportButtonColor = new Color(0.31f, 0.55f, 0.26f);
		private static readonly Color unpinButtonColor = new Color(0.68f, 0.14f, 0.15f);

		private const float margins = 0;
		private const int numSteps = 100;
		private const float width = 4;

		private static PolyLine polyLine; //to draw frames
		private static Texture2D lineTex;

		public enum SelectionMode { none, pin, pinLowres, pinExisting, selectPreview, export, exportCopy, exportCluster, unpin }


		public static void DrawInspectorGUI (MapMagicObject mapMagic, ref SelectionMode selectionMode, ref bool saveDraft)
		{
			Cell.EmptyLinePx(2);

			using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.pin, "MapMagic/Icons/Pin", "MapMagic/PinButtons/PinTop", "Pin New Tile", pinButtonColor);
			using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.pinLowres, "MapMagic/Icons/PinDraft", "MapMagic/PinButtons/PinBottom","Pin As Draft", pinButtonColor);
			//using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.pinExisting, "MapMagic/Icons/PinExisting", "MapMagic/PinButtons/PinBottom", "Pin Existing Terrain", pinButtonColor);
			
			Cell.EmptyLinePx(5);
			using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.selectPreview, "MapMagic/Icons/Preview", "MapMagic/PinButtons/Preview", "Select Preview", previewButtonColor);
			
			Cell.EmptyLinePx(5);
			using (Cell.LinePx(25)) 
			{
				bool saveDraftCl = saveDraft;
				/*void DrawSaveDraft()
				{  
					using (Cell.RowPx(60)) 
					{
						Cell.EmptyLine();
						using (Cell.LineStd) Draw.ToggleLeft(ref saveDraftCl, "Draft"); 
						Cell.EmptyLine();
					}
				}*/
				DrawPinButton(ref selectionMode, SelectionMode.export, "MapMagic/Icons/Export", "MapMagic/PinButtons/ExportTop", "Save Terrain Data", previewButtonColor, iconScale:0.5f);
				saveDraft = saveDraftCl;

				//save draft will cause additional save file dilog, and actually usless
			}
			using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.exportCopy, "MapMagic/Icons/ExportCopy", "MapMagic/PinButtons/ExportMid", "Save As Copy", previewButtonColor, iconScale:0.5f);

			using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.exportCluster, "MapMagic/Icons/ExportCopy", "MapMagic/PinButtons/ExportBottom", "Export Cluster", previewButtonColor, iconScale:0.5f);


			Cell.EmptyLinePx(5);
			using (Cell.LinePx(25)) DrawPinButton(ref selectionMode, SelectionMode.unpin, "MapMagic/Icons/Unpin", "MapMagic/PinButtons/Remove", "Unpin", unpinButtonColor);

			Cell.EmptyLinePx(2);
		}

		private static void DrawPinButton (ref SelectionMode selectionMode, SelectionMode buttonMode, string iconName, string buttonName, string label, Color color, float iconScale=1f, Action additionalElement=null)
		{
			using (Cell.Padded(0,0,-1,-1))
			{
				bool isPinning = selectionMode==buttonMode;

				Draw.CheckButton(ref isPinning, visible:false);

				GUIStyle style = UI.current.textures.GetElementStyle(isPinning ? buttonName+"_pressed" : buttonName);
				Draw.Element(style);

				Cell.EmptyRowPx(10);

				using (Cell.RowPx(30))
				{
					//using (Cell.Padded(1,1,1,1))
					//{
					//	if (isPinning) { color = color/2; color.a=1; }
					//	Draw.Rect(color);
					//}

					Texture2D icon = UI.current.textures.GetTexture(iconName);
					Draw.Icon(icon, scale:iconScale);
				}

				using (Cell.Row) Draw.Label(label, style:UI.current.styles.middleLabel);

				additionalElement?.Invoke();

				if (Cell.current.valChanged)
				{
					if (isPinning) selectionMode = buttonMode;
					else selectionMode = SelectionMode.none;
				}
			}
		}


		private static bool DrawButton (ref SelectionMode selectionMode, SelectionMode buttonMode, string iconName, string buttonName, string label, Color color, float iconScale=1f, Action additionalElement=null)
		// Taking same visual appearance from DrawPinButton, but it's pressed momentarily
		// Was going to be used for Cluster
		{
			using (Cell.Padded(0,0,-1,-1))
			{
				bool isPressed = Draw.Button(visible:false);
				bool isPrePressed = Draw.IsButtonPrePressed;

				if (isPressed || isPrePressed) selectionMode = SelectionMode.none;

				GUIStyle style = UI.current.textures.GetElementStyle(isPrePressed ? buttonName+"_pressed" : buttonName);  
				Draw.Element(style);

				Cell.EmptyRowPx(10);

				using (Cell.RowPx(30))
				{
					Texture2D icon = UI.current.textures.GetTexture(iconName);
					Draw.Icon(icon, scale:iconScale);
				}

				using (Cell.Row) Draw.Label(label, style:UI.current.styles.middleLabel);

				additionalElement?.Invoke();

				return isPressed;
			}
		}

		public static void DrawSceneGUI (MapMagicObject mapMagic, ref SelectionMode selectionMode, bool saveDraft=false)
		{
			if (selectionMode!=SelectionMode.none  &&  !Event.current.alt)
			{
				Dictionary<Coord,TerrainTile> tilesLut = new Dictionary<Coord,TerrainTile>(mapMagic.tiles.grid);

				//returning if no scene veiw (right after script compile)
				SceneView sceneview = UnityEditor.SceneView.lastActiveSceneView;
				if (sceneview==null || sceneview.camera==null) return;

				//disabling selection
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				//canceling any pin on esc, alt or right-click
				if (Event.current.keyCode == KeyCode.Escape  ||  Event.current.alt) 
				{ 
					selectionMode = SelectionMode.none; 
					Select.CancelFrame(); 
				//	EditorWindow.GetWindow FindObjectOfType<Core.GUI.MapMagicInspector>().Repaint(); 
				}
				
				//preparing the sets of both custom and tile terrains
				HashSet<Terrain> pinnedCustomTerrains = new HashSet<Terrain>(); //custom terrains that were already pinned before
				foreach (TerrainTile tile in mapMagic.tiles.customTiles)
				{
					if (tile.main != null) pinnedCustomTerrains.Add(tile.main.terrain);
					if (tile.draft != null) pinnedCustomTerrains.Add(tile.draft.terrain);
				}

				HashSet<Terrain> pinnedTileTerrains = new HashSet<Terrain>();
				foreach (var kvp in tilesLut)
				{
					TerrainTile tile = kvp.Value;
					if (tile.main != null) pinnedTileTerrains.Add(tile.main.terrain);
					if (tile.draft != null) pinnedTileTerrains.Add(tile.draft.terrain);
				}

				if (selectionMode == SelectionMode.pin)
				{
					Handles.color = FrameDraw.pinColor;

					List<Coord> selectedCoords = TerrainAiming.SelectTiles((Vector3)mapMagic.tileSize, false, tilesLut, mapMagic.transform);
					if (selectedCoords != null)
					{
						UnityEditor.Undo.RegisterFullObjectHierarchyUndo(mapMagic.gameObject, "MapMagic Pin Terrains"); 

						foreach (Coord coord in selectedCoords)
							mapMagic.tiles.Pin(coord, false, mapMagic);

						foreach (Coord coord in selectedCoords)
							UnityEditor.Undo.RegisterCreatedObjectUndo(mapMagic.tiles[coord].gameObject, "MapMagic Pin Terrains"); 
					}
				}

				if (selectionMode == SelectionMode.pinLowres)
				{
					Handles.color = FrameDraw.pinColor;

					List<Coord> selectedCoords = TerrainAiming.SelectTiles((Vector3)mapMagic.tileSize, true, tilesLut, mapMagic.transform);
					if (selectedCoords != null)
					{
						UnityEditor.Undo.RegisterFullObjectHierarchyUndo(mapMagic.gameObject, "MapMagic Pin Draft Terrains");

						foreach (Coord coord in selectedCoords)
							mapMagic.tiles.Pin(coord, true, mapMagic);

						foreach (Coord coord in selectedCoords)
							UnityEditor.Undo.RegisterCreatedObjectUndo(mapMagic.tiles[coord].gameObject, "MapMagic Pin Terrains"); 
					}
				}

				if (selectionMode == SelectionMode.pinExisting)
				{
					//excluding tiles
					HashSet<Terrain> possibleTerrains = new HashSet<Terrain>();
					Terrain[] allTerrains = GameObject.FindObjectsOfType<Terrain>();
					foreach (Terrain terrain in allTerrains)
						if (!pinnedTileTerrains.Contains(terrain)) possibleTerrains.Add(terrain);

					HashSet<Terrain> selectedTerrains = TerrainAiming.SelectTerrains(possibleTerrains, FrameDraw.pinColor, false);
					if (selectedTerrains != null)
					{
						UnityEditor.Undo.RegisterFullObjectHierarchyUndo(mapMagic.gameObject, "MapMagic Pin Terrains");

						foreach (Terrain terrain in selectedTerrains)
						{
							if (pinnedCustomTerrains.Contains(terrain)) continue;
							terrain.transform.parent = mapMagic.transform;

							TerrainTile tile = terrain.gameObject.GetComponent<TerrainTile>();
							if (tile == null) tile = terrain.gameObject.AddComponent<TerrainTile>();
							tile.main.terrain = terrain;
							//tile.main.area = new Terrains.Area(terrain);
							//tile.main.use = true;
							//tile.lodData = null;

							mapMagic.tiles.PinCustom(tile);
							mapMagic.StartGenerate(tile);
						}
							
					}
				}


				if (selectionMode == SelectionMode.selectPreview)
				{
					//hash set of all pinned terrains contains main data
					HashSet<Terrain> pinnedMainTerrains = new HashSet<Terrain>();
					foreach (var kvp in tilesLut)
					{
						TerrainTile tile = kvp.Value;
						if (tile.main != null) pinnedMainTerrains.Add(tile.main.terrain);
					}
					pinnedMainTerrains.UnionWith(pinnedCustomTerrains);

					HashSet<Terrain> selectedTerrains = TerrainAiming.SelectTerrains(pinnedMainTerrains, FrameDraw.selectPreviewColor, false);
					if (selectedTerrains != null && selectedTerrains.Count == 1)
					{
						UnityEditor.Undo.RegisterCompleteObjectUndo(mapMagic, "MapMagic Select Preview");

						Terrain selectedTerrain = selectedTerrains.Any();

						//clearing preview
						if (selectedTerrain == mapMagic.AssignedPreviewTerrain) 
						{
							mapMagic.ClearPreviewTile();
							TerrainTile.OnPreviewAssigned(mapMagic.PreviewData);
						}

						//assigning new
						else
							foreach (var kvp in tilesLut)
							{
								TerrainTile tile = kvp.Value;
								if (tile.main?.terrain == selectedTerrain  &&  mapMagic.AssignedPreviewTerrain != selectedTerrain) 
								{
									mapMagic.AssignPreviewTile(tile);
									mapMagic.AssignedPreviewData.isPreview = true;
									TerrainTile.OnPreviewAssigned(mapMagic.AssignedPreviewData);

									UI.RepaintAllWindows();
								}
							}	
					}
				}


				if (selectionMode == SelectionMode.export  ||  selectionMode == SelectionMode.exportCopy)
				{
					HashSet<Terrain> allTerrains = new HashSet<Terrain>(pinnedTileTerrains);
					allTerrains.UnionWith(pinnedCustomTerrains);

					HashSet<Terrain> selectedTerrains = TerrainAiming.SelectTerrains(allTerrains, FrameDraw.exportColor, false);
					if (selectedTerrains != null)
						foreach (Terrain terrain in selectedTerrains)
						{
							if (!terrain.gameObject.activeSelf)
								continue;

							//finding coordinate to get proper name
							bool coordFound = false;
							Coord coord = new Coord();
							foreach (var kvp in tilesLut)
								if (kvp.Value.ContainsTerrain(terrain))
								{
									coordFound = true;
									coord = kvp.Key;
								}
							string terrainName = coordFound ? $"Terrain {coord.x}, {coord.z}" : terrain.name;

							bool asCopy = selectionMode==SelectionMode.exportCopy;
							SaveTerrainData(terrain.terrainData, terrainName, asCopy:asCopy);

							if (saveDraft)
							{
								TerrainTile tile = mapMagic.tiles.FindByTerrain(terrain);
								Terrain draftTerrain = tile.draft?.terrain;
								if (draftTerrain != null)
									SaveTerrainData(draftTerrain.terrainData, terrainName + "_draft", asCopy:asCopy);
							}
						}
				}


				if (selectionMode == SelectionMode.exportCluster)
				{
					HashSet<Terrain> allTerrains = new HashSet<Terrain>(pinnedTileTerrains);
					allTerrains.UnionWith(pinnedCustomTerrains);

					HashSet<Terrain> selectedTerrains = TerrainAiming.SelectTerrains(allTerrains, FrameDraw.exportColor, false);
					if (selectedTerrains != null && selectedTerrains.Count == 1)
						MapMagic.Core.GUI.MapMagicInspector.OnClusterExported?.Invoke(mapMagic, selectedTerrains.Any().transform.position);
						//Clusters.ClusterAssetInspector.CreateViaDialog(mapMagic, selectedTerrains.Any().transform.position);
				}


				if (selectionMode == SelectionMode.unpin)
				{
					HashSet<Terrain> possibleTerrains = new HashSet<Terrain>(pinnedTileTerrains);
					possibleTerrains.UnionWith(pinnedCustomTerrains);

					HashSet<Terrain> selectedTerrains = TerrainAiming.SelectTerrains(possibleTerrains, FrameDraw.unpinColor, false);
					if (selectedTerrains != null)
					{
						UnityEditor.Undo.RegisterFullObjectHierarchyUndo(mapMagic.gameObject, "MapMagic Unpin Terrains");

						foreach (Terrain terrain in selectedTerrains)
						{
							//terrain-to-coord lut (to pick tile terrain coord faster)
							Dictionary<Terrain, Coord> terrainToCoordLut = new Dictionary<Terrain, Coord>();
							foreach (var kvp in tilesLut)
							{
								Coord coord = kvp.Key;
								TerrainTile tile = kvp.Value;
								if (tile.main != null) terrainToCoordLut.Add(tile.main.terrain, coord);
								if (tile.draft != null) terrainToCoordLut.Add(tile.draft.terrain, coord);
							}

							//if it's tile
							if (terrainToCoordLut.ContainsKey(terrain))
							{
								Coord coord = terrainToCoordLut[terrain];
								mapMagic.tiles.Unpin(coord);
							}

							//if it's custom
							if (pinnedCustomTerrains.Contains(terrain))
							{
								TerrainTile tileComponent = terrain.gameObject.GetComponent<TerrainTile>();
								mapMagic.tiles.UnpinCustom(tileComponent);
								GameObject.DestroyImmediate(tileComponent);
							}
						}
							
					}
				}

				//redrawing scene
				SceneView.lastActiveSceneView.Repaint();
			}
		}


		private static bool SaveTerrainData (TerrainData terrainData, string terrainName, bool asCopy=false)
		/// Returns false on some error or if user click "cancel"|
		{
			TerrainData origTerrainData = terrainData;
			if (asCopy) terrainData = terrainData.Copy();

			string fileName = terrainName.Replace(",", "");
			if (asCopy) fileName += " Copy";

			if (AssetDatabase.Contains(terrainData))
			{
				Debug.Log($"Skipping {terrainName} since it's already has it's data stored in a file.\nUse 'Save Project' to save the changes.");
				return true;
				//can't save asset to the other file
			}

			string savePath = UnityEditor.EditorUtility.SaveFilePanel(
				$"Save {terrainName}",
				"Assets",
				fileName, 
				"asset");

			if (savePath == null  ||  savePath.Length==0)
				return false; //clicked cancel

			savePath = savePath.Replace(Application.dataPath, "Assets");

			//if it's not original terrain data
			if (AssetDatabase.LoadAssetAtPath(savePath, typeof(TerrainData)) == origTerrainData)
			{
				Debug.Log($"Could not overwrite {terrainName} original data with it's copy. This will make terrain data null. Choose other filename.");
				return true;
			}

			float[,,] splats = terrainData.GetAlphamaps(0,0,terrainData.alphamapResolution, terrainData.alphamapResolution);  
			//backup splats - for some reason they will be removed

			AssetDatabase.DeleteAsset(savePath);  //this will mark terrain data as 'null' in all terrains using it (thus will write null data on file overwrite)
			AssetDatabase.CreateAsset(terrainData, savePath);
			AssetDatabase.SaveAssets();

			terrainData.SetAlphamaps(0,0,splats);  //re-assign splats
			AssetDatabase.SaveAssets();

			return true;
		}
	}
}
