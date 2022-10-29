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
	public static class GeneratorRightClick
	{
		public static Graph copiedGenerators;

		public static Item GeneratorItems (Vector2 mousePos, Generator gen, Graph graph, int priority=3)
		{
			Item genItems = new Item("Generator");
			genItems.onDraw = RightClick.DrawItem;
			genItems.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Generator");
			genItems.color = RightClick.defaultColor;
			genItems.subItems = new List<Item>();
			genItems.priority = priority;

			genItems.disabled = gen==null  &&  copiedGenerators==null; 

			{ //enable/disable
				string caption = (gen==null||gen.enabled) ? "Disable" : "Enable";
				Item item = new Item(caption, onDraw:RightClick.DrawItem, priority:11);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Eye");
				item.color = RightClick.defaultColor;
				item.disabled = gen==null;
				item.onClick = ()=> 
					GraphEditorActions.EnableDisableGenerators(graph, GraphWindow.current.selected, gen); 
				genItems.subItems.Add(item);
			}
				
			//genItems.subItems.Add( new Item("Export", onDraw:RightClick.DrawItem, priority:10) { icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Export"), color = Color.gray } );
			//genItems.subItems.Add( new Item("Import", onDraw:RightClick.DrawItem, priority:9) { icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Import"), color = Color.gray } );
			
			{ //duplicate
				Item item = new Item("Duplicate", onDraw:RightClick.DrawItem, priority:8);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Duplicate");
				item.color = RightClick.defaultColor;
				item.disabled = gen==null;
				item.onClick = ()=> 
					GraphEditorActions.DuplicateGenerator(graph, gen, ref GraphWindow.current.selected);
				genItems.subItems.Add(item);
			}


			{ //copy
				Item item = new Item("Copy", onDraw:RightClick.DrawItem, priority:8);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Export");
				item.color = RightClick.defaultColor;
				item.disabled = !(gen!=null || (GraphWindow.current.selected!=null && GraphWindow.current.selected.Count!=0));
				item.onClick = ()=> 
				{
					HashSet<Generator> gens;
					if (GraphWindow.current.selected!=null && GraphWindow.current.selected.Count!=0)
						gens = GraphWindow.current.selected;
					else
						{ gens = new HashSet<Generator>(); gens.Add(gen); }
					copiedGenerators = graph.Export(gens);
				};
				item.closeOnClick = true;
				genItems.subItems.Add(item);
			}

	
			{ //paste
				Item item = new Item("Paste", onDraw:RightClick.DrawItem, priority:7);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Export"); 
				item.color = RightClick.defaultColor;
				item.disabled = copiedGenerators==null;
				item.onClick = ()=> 
				{ 
					Generator[] imported = graph.Import(copiedGenerators); 
					Graph.Reposition(imported, mousePos);
				};
				item.closeOnClick = true;
				genItems.subItems.Add(item);
			}
			

			{ //update
				Item item = new Item("Update", onDraw:RightClick.DrawItem, priority:7);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Update"); 
				item.color = RightClick.defaultColor;
				item.closeOnClick = true;
				item.disabled = gen==null;
			}
			

			{ //reset
				Item item = new Item("Reset", onDraw:RightClick.DrawItem, priority:4);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Reset");
				item.color = RightClick.defaultColor;
				item.closeOnClick = true;
				item.disabled = gen==null;
			}
			

			{ //remove
				Item item = new Item("Remove", onDraw:RightClick.DrawItem, priority:5);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Remove");
				item.color = RightClick.defaultColor;
				item.disabled = gen==null;
				item.onClick = ()=> 
					GraphEditorActions.RemoveGenerators(graph, GraphWindow.current.selected, gen); 
				item.closeOnClick = true;
				genItems.subItems.Add(item);
			}


			{ //unlink
				Item item = new Item("Unlink", onDraw:RightClick.DrawItem, priority:6);
				item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Unlink");
				item.color = RightClick.defaultColor;
				item.disabled = gen==null;
				item.onClick = ()=> 
				{
					graph.UnlinkGenerator(gen);
					GraphWindow.current?.RefreshMapMagic(gen);
					//undo
				};
				item.closeOnClick = true;
				genItems.subItems.Add(item);
			}
			

			if (gen!=null) 
			{ //id
				Item item = new Item($"Id: {gen.id}", onDraw:RightClick.DrawItem, priority:3);
				item.color = RightClick.defaultColor;
				item.onClick = ()=> EditorGUIUtility.systemCopyBuffer = gen.id.ToString();
				item.closeOnClick = true;
				genItems.subItems.Add(item);
			}

			#if MM_DEBUG
			if (gen!=null) 
			{ //position
				Item item = new Item($"Pos: {gen.guiPosition}", onDraw:RightClick.DrawItem, priority:2);
				item.color = RightClick.defaultColor;
				item.onClick = ()=> EditorGUIUtility.systemCopyBuffer = gen.guiPosition.ToString();
				item.closeOnClick = true;
				genItems.subItems.Add(item);
			}

			#endif

			return genItems;
		}
	}
}