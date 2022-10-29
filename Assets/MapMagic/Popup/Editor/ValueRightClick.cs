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
using MapMagic.Expose.GUI;

namespace MapMagic.Nodes.GUI
{
	public class RightClickExpose
	{
		//public FieldInfo field;
		public string fieldName;
		public Type fieldType;
		public ulong id; //generator or layer id
		public int channel; //for Vector fields
		public int arrIndex;

		public RightClickExpose (ulong id, string fieldName, Type fieldType, int channel, int arrIndex) 
			{ this.fieldName=fieldName; this.fieldType=fieldType; this.id=id; this.channel=channel; this.arrIndex=arrIndex; }
	}

	public static class ValueRightClick
	{
		public static Item ValueItems (RightClickExpose expose, Generator gen, Graph graph, int priority=2)
		/// chNum is a channel for vector fields
		{
			Item valItems = new Item("Value");
			valItems.onDraw = RightClick.DrawItem;
			valItems.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Value");
			valItems.color =  RightClick.defaultColor;
			valItems.subItems = new List<Item>();
			valItems.priority = priority;

			valItems.disabled = expose==null || gen==null;

			Item exposeItem = new Item("Expose", onDraw:RightClick.DrawItem, priority:6); 
			exposeItem.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Expose");
			if (expose != null) exposeItem.onClick =
				() => { ExposeWindow.ShowWindow(graph, expose.id, expose.fieldName, expose.fieldType, expose.channel, expose.arrIndex); UI.current.editorWindow.Close(); };
			valItems.subItems.Add(exposeItem);

			Item unExposeItem = new Item("UnExpose", onDraw:RightClick.DrawItem, priority:6); 
			unExposeItem.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/UnExpose");
			if (expose != null) unExposeItem.onClick = //() => { if (gen.exposed==null) gen.exposed=new Exposed(); gen.exposed.ExposeField(valField); };
				() => 
				{
					//graph.exposed.Unexpose(gen, valField);
					graph.exposed.Remove(gen.id, expose.fieldName, expose.channel);
					GraphWindow.current.Focus();
					GraphWindow.current.Repaint();
				};
			valItems.subItems.Add(unExposeItem);

			return valItems;
		}
	}
}