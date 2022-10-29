using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.GUI;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes.GUI;
//using MapMagic.Nodes.ObjectsGenerators; //not in objects anymore


/// Some object editor fns are brought to a separate file since they are needed by Brush module outputs

namespace MapMagic.Nodes.GUI
{
	public static partial class ObjectsEditors
	{
		public static void DrawObjectPrefabs (ref GameObject[] prefabs, bool multiPrefab=true, bool treeIcon=false)
		{
			string iconName = treeIcon ?  "DPUI/Icons/TreeDisabled" : "DPUI/Icons/ObjectDisabled";

			if (multiPrefab)
				using (Cell.LineStd) 
				{
					GameObject[] prefabsCopy = prefabs; //TODO: Action not taking ref. The layer should have onDraw function as Action<layer,int>, instead of just <int>
					LayersEditor.DrawLayers(ref prefabs, onDraw: n => DrawObjectPrefabLayer(prefabsCopy,n,iconName) );
				}
			else
			{
				if (prefabs.Length != 1) Array.Resize(ref prefabs, 1);

				Cell.EmptyLinePx(4);
				using (Cell.LineStd) 
				{
					using (Cell.RowPx(24)) Draw.Icon( UI.current.textures.GetTexture(iconName) );
					Cell.EmptyRowPx(4);
					using (Cell.Row) Draw.Field(ref prefabs[0]); 
					Cell.EmptyRowPx(4);
				}
			}

			Cell.EmptyLinePx(2);
		}

		private static void DrawObjectPrefabLayer (GameObject[] prefabs, int n, string iconName)
		{ 
			if (n>=prefabs.Length) return; //on layer remove
			Cell.EmptyLinePx(4);
			using (Cell.LineStd) 
			{
				using (Cell.RowPx(24)) Draw.Icon( UI.current.textures.GetTexture(iconName) );
				using (Cell.Row) prefabs[n] = Draw.ObjectField(prefabs[n]); 
				Cell.EmptyRowPx(4);
			}
			Cell.EmptyLinePx(4);
		}

		public static void DrawPositioningSettings (PositioningSettings posSettings, bool billboardRotWaring=false, bool showRelativeHeight=true)
		{
			//height
			Cell.EmptyLinePx(1);
			using (Cell.LinePx(0))
				using (new Draw.FoldoutGroup(ref posSettings.guiHeight, "Height"))
					if (posSettings.guiHeight)
					{
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.objHeight, "Object Height");
//						if (showRelativeHeight)
							using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.relativeHeight, "Relative Height");
					}

			//rotation
			Cell.EmptyLinePx(1);
			using (Cell.LinePx(0))
				using (new Draw.FoldoutGroup(ref posSettings.guiRotation, "Rotation"))
					if (posSettings.guiRotation)
					{
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.useRotation, "Use Rotation");
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.takeTerrainNormal, "Terrain Normal");
						using (Cell.LineStd) 
						{
							Cell.current.disabled = posSettings.takeTerrainNormal;
							Draw.ToggleLeft(ref posSettings.rotateYonly, "Rotate Y Only"); //
						}
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.regardPrefabRotation, "Use Prefab Rot.");

						if (billboardRotWaring)
						{
							using (Cell.LinePx(40)) 
								Draw.Helpbox("Note that Unity billboard trees could not be rotated");
							Cell.EmptyLinePx(2);
						}
					}

			//scale
			Cell.EmptyLinePx(1);
			using (Cell.LinePx(0))
				using (new Draw.FoldoutGroup(ref posSettings.guiScale, "Scale"))
					if (posSettings.guiScale)
					{
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.useScale, "Use Scale");
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.scaleYonly, "Scale Y Only");
						using (Cell.LineStd) Draw.ToggleLeft(ref posSettings.regardPrefabScale, "Use Prefab Scale");
						//using (Cell.LineStd) 
						//{
						//	Cell.EmptyRowPx(18);
						//	using (Cell.Row) Draw.Label("Scale");  
						//}
					}
		}
	}
}