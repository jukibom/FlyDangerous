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
	public static class GroupRightClick
	{
			private static GUIStyle itemTextStyle;

			public static Item GroupItems (Vector2 mousePos, Group grp, Graph graph, int priority=3)
			{
				Item genItems = new Item("Group");
				genItems.onDraw = RightClick.DrawItem;
				genItems.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Generator");
				genItems.color = RightClick.defaultColor;
				genItems.subItems = new List<Item>();
				genItems.priority = priority;

				//genItems.disabled = grp == null;

				genItems.subItems.Add( new Item("Create", onDraw:RightClick.DrawItem, priority:12) { 
					icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/GroupAdd"), 
					color =  RightClick.defaultColor,
					onClick = ()=> CreateGroup(mousePos, graph)} );

				genItems.subItems.Add( new Item("Group Selected", onDraw:RightClick.DrawItem, priority:12) { 
					icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/GroupSelected"), 
					color =  RightClick.defaultColor,
					disabled = GraphWindow.current.selected==null || GraphWindow.current.selected.Count==0,
					onClick = ()=> GroupSelected(mousePos, graph)} );

				//genItems.subItems.Add( new Item("Export", onDraw:DrawItem, priority:10) { icon = texturesCache.GetTexture("MapMagic/Popup/Export"), color = Color.gray } );
				//genItems.subItems.Add( new Item("Import", onDraw:DrawItem, priority:9) { icon = texturesCache.GetTexture("MapMagic/Popup/Import"), color = Color.gray } );
				//genItems.subItems.Add( new Item("Duplicate", onDraw:DrawItem, priority:8) { icon = texturesCache.GetTexture("MapMagic/Popup/Duplicate"), color = Color.gray } );
				//genItems.subItems.Add( new Item("Update", onDraw:DrawItem, priority:7) { icon = texturesCache.GetTexture("MapMagic/Popup/Update"), color = Color.gray } );

				genItems.subItems.Add( new Item("Ungroup", onDraw:RightClick.DrawItem, priority:5) {
					icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Ungroup"),
					color =  RightClick.defaultColor,
					onClick = ()=> RemoveGroup(grp,graph,withContent:false) });

				genItems.subItems.Add( new Item("Remove", onDraw:RightClick.DrawItem, priority:4) {
					icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Remove"),
					color =  RightClick.defaultColor,
					onClick = ()=> RemoveGroup(grp,graph,withContent:true) });

				return genItems;
			}


			public static Group CreateGroup (Vector2 mousePos, Graph graph)
			{
					GraphWindow.RecordCompleteUndo();

					Group grp = new Group();
					grp.guiPos = mousePos;
					graph.Add(grp);

					GraphWindow.current.Focus();
					GraphWindow.current.Repaint();
					//GraphWindow.RefreshMapMagic(); //not necessary

					return grp;
			}


			public static void GroupSelected (Vector2 mousePos, Graph graph)
			{
				Group ngrp = CreateGroup(mousePos, graph); 
				PullGroupToSelected(ngrp);
			}


			public static void PullGroupToSelected (Group grp)
			{
				HashSet<Generator> selected = GraphWindow.current.selected;
				if (selected != null  &&  selected.Count != 0)
				{
					Rect selectedRect = new Rect(selected.Any().guiPosition, selected.Any().guiSize);
					foreach (Generator gen in selected)
						selectedRect = selectedRect.Encapsulate( new Rect(gen.guiPosition, gen.guiSize) );

					selectedRect = selectedRect.Extended(20,20,60,20);

					grp.guiPos = selectedRect.position; 
					grp.guiSize = selectedRect.size;
				}	
			}


			public static void RemoveGroup (Group grp, Graph graph, bool withContent=false)
			{
				GraphWindow.RecordCompleteUndo();

				if (withContent)
					GroupDraw.RemoveGroupContents(grp, graph);

				graph.Remove(grp);

				GraphWindow.current.Focus();
				GraphWindow.current.Repaint();
				
				if (withContent) 
					GraphWindow.current?.RefreshMapMagic();
			}


			public static void DrawGroupColorSelector (Group group)
			{
				Item menuItem = new Item("Colors");
				menuItem.subItems = new List<Item> 
				{
					GetGroupColorSelectorItem(group, "Neutral", new Color(0.625f, 0.625f, 0.625f, 1), 210),
					GetGroupColorSelectorItem(group, "Rose Quartz", new Color(246f/256f, 202f/256f, 201f/256f, 1), 200),
					GetGroupColorSelectorItem(group, "Dahlia", new Color(235f/256f, 149f/256f, 135f/256f, 1), 190),
					GetGroupColorSelectorItem(group, "Flame", new Color(243f/256f, 85f/256f, 76f/256f, 1), 180),
					GetGroupColorSelectorItem(group, "Marsala", new Color(150f/256f, 79f/256f, 77f/256f, 1), 170),
					GetGroupColorSelectorItem(group, "Hazelnut", new Color(225f/256f, 174f/256f, 155f/256f, 1), 160),
					GetGroupColorSelectorItem(group, "Butterum", new Color(196f/256f, 142f/256f, 104f/256f, 1), 150),
					GetGroupColorSelectorItem(group, "Primrose", new Color(255f/256f, 204f/256f, 115f/256f, 1), 140),
					GetGroupColorSelectorItem(group, "Amber", new Color(255f/256f, 182f/256f, 72f/256f, 1), 130),
					GetGroupColorSelectorItem(group, "Cream Gold", new Color(221f/256f, 191f/256f, 94f/256f, 1), 120),
					GetGroupColorSelectorItem(group, "Gold Lime", new Color(155f/256f, 151f/256f, 64f/256f, 1), 110),
					GetGroupColorSelectorItem(group, "Lint", new Color(182f/256f, 186f/256f, 153f/256f, 1), 100),
					GetGroupColorSelectorItem(group, "Greenery", new Color(118f/256f, 177f/256f, 97f/256f, 1), 90),
					GetGroupColorSelectorItem(group, "Green", new Color(84f/256f, 194f/256f, 71f/256f, 1), 80),
					GetGroupColorSelectorItem(group, "Kale", new Color(89f/256f, 118f/256f, 87f/256f, 1), 70),
					GetGroupColorSelectorItem(group, "Beryl", new Color(96f/256f, 144f/256f, 135f/256f, 1), 60),
					GetGroupColorSelectorItem(group, "Arctic", new Color(100f/256f, 133f/256f, 137f/256f, 1), 50),
					GetGroupColorSelectorItem(group, "Niagara", new Color(51f/256f, 142f/256f, 163f/256f, 1), 40),
					GetGroupColorSelectorItem(group, "Island", new Color(118f/256f, 206f/256f, 216f/256f, 1), 30),
					GetGroupColorSelectorItem(group, "Carolina", new Color(139f/256f, 184f/256f, 232f/256f, 1), 20),
					GetGroupColorSelectorItem(group, "Navy", new Color(64f/256f, 63f/256f, 111f/256f, 1), 10)	
				};
				PopupMenu menu = new PopupMenu() {items=menuItem.subItems, minWidth=150};
				menu.Show(Event.current.mousePosition);
			}

			private static void DrawGroupColorSelectorItem (Item item, Rect rect)
			{
				Rect iconRect = new Rect(rect.x, rect.y, 18,18);
				Rect labelRect = new Rect(rect.x+iconRect.width+3, rect.y, rect.width-iconRect.width-3, rect.height);

				if (itemTextStyle == null)
				{
					itemTextStyle = new GUIStyle(UnityEditor.EditorStyles.label); 
					itemTextStyle.normal.textColor = itemTextStyle.focused.textColor = itemTextStyle.active.textColor = Color.black;
				}

				EditorGUI.DrawRect(iconRect.Extended(-2), new Color(item.color.r*0.7f, item.color.g*0.7f, item.color.b*0.7f, 1));
				EditorGUI.DrawRect(iconRect.Extended(-3), item.color);

				UnityEditor.EditorGUI.LabelField(labelRect, item.name, itemTextStyle);
			}

			private static Item GetGroupColorSelectorItem (Group group, string name, Color color, int priority)
			{
				TexturesCache texturesCache = UI.current.textures;
				return new Item() 
				{ 
					onDraw = DrawGroupColorSelectorItem,
					color = color,
					name = name,
					onClick = () => group.color = color,
					priority = priority
				};
			}


			private static void FocusRepaintRefreshWindow ()
			{
				if (GraphWindow.current==null) 
					return;

				GraphWindow.current.Focus();
				GraphWindow.current.Repaint();

				GraphWindow.current.RefreshMapMagic();
			}

	}
}