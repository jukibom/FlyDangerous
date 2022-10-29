using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Profiling;
using UnityEditor;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.GUI.Popup;
using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Nodes.GUI;

namespace MapMagic.Nodes.GUI
{
	public static class GraphPopup
	{
			public static Item GraphItems (Graph graph, int priority=1)
			{
				Item graphItems = new Item("Graph");
				graphItems.onDraw = RightClick.DrawItem;
				graphItems.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Graph");
				graphItems.color = RightClick.defaultColor;
				graphItems.subItems = new List<Item>();
				graphItems.priority = priority;


				Item importItem = new Item("Import", onDraw:RightClick.DrawItem, priority:9) { icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Import"), color = RightClick.defaultColor };
				importItem.onClick = ()=>
				{
					Graph imported = ScriptableAssetExtensions.LoadAsset<Graph>("Load Graph", filters: new string[]{"Asset","asset"} );
					if (imported != null)
						graph.Import(imported);
				};
				graphItems.subItems.Add(importItem);
				

				Item exportItem = new Item("Export", onDraw:RightClick.DrawItem, priority:9) { icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Export"), color = RightClick.defaultColor };
				exportItem.disabled = GraphWindow.current.selected==null || GraphWindow.current.selected.Count==0;
				exportItem.onClick = ()=>
				{
					Graph exported = graph.Export(GraphWindow.current.selected);
					ScriptableAssetExtensions.SaveAsset(exported, caption:"Save Graph", type:"asset");
				};
				graphItems.subItems.Add(exportItem);
				

				graphItems.subItems.Add( new Item("Update All", onDraw:RightClick.DrawItem, priority:1) { icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Update"), color = RightClick.defaultColor } );

				//graphItems.subItems.Add( new Item("Background", onClick:()=>GraphWindow.current.noBackground=!GraphWindow.current.noBackground, onDraw:RightClick.DrawItem, priority:8));
				//graphItems.subItems.Add( new Item("Debug", onClick:()=>GraphWindow.current.drawGenDebug=!GraphWindow.current.drawGenDebug, onDraw:RightClick.DrawItem, priority:9));

				return graphItems;
			} 
	}
}