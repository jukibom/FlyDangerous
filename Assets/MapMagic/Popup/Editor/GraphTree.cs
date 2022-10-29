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
	public static class GraphTreePopup
	{
		public static void DrawGraphTree (Graph rootGraph)
		{
			List<Item> items = new List<Item>();
			FillSubGraphItems(rootGraph, rootGraph, "", items);

			PopupMenu menu = new PopupMenu() { items=items, sortItems=false };  //items=new List<Item>() {item}
			menu.Show(Event.current.mousePosition);
		}


		private static void FillSubGraphItems (Graph graph, Graph root, string prefix, List<Item> items)
		{
			Item item = new Item(prefix + graph.name);
			items.Add(item);
			item.onClick = () => GraphWindow.current.OpenBiome(graph, root);

			foreach (Graph subGraph in graph.SubGraphs())
				FillSubGraphItems(subGraph, root, $"{prefix}   ", items);
		}
	}
}