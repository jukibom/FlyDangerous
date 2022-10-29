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
	public static class CreateRightClick
	{
		public static HashSet<Type> generatorTypes = new HashSet<Type>();
		//public so addons could enlist themselves on initialize


		public static void DrawCreateItems (Vector2 mousePos, Graph graph)
			{ DrawItem( CreateItems(mousePos, graph) ); }


		public static void DrawInsertItems (Vector2 mousePos, Graph graph, IInlet<object> clickedLink)
			{ DrawItem( InsertItems(mousePos, graph, clickedLink) ); }


		public static void DrawAppendItems (Vector2 mousePos, Graph graph, IOutlet<object> clickedOutlet)
			{ DrawItem( AppendItems(mousePos, graph, clickedOutlet) ); }


		private static void DrawItem (Item item)
		{
			#if MM_EXP || UNITY_2020_1_OR_NEWER || UNITY_EDITOR_LINUX
			SingleWindow menu = new SingleWindow(item);
			#else
			PopupMenu menu = new PopupMenu() {items=item.subItems, minWidth=150};
			#endif

			menu.Show(Event.current.mousePosition);
		}


		public static Item CreateItems (Vector2 mousePos, Graph graph, int priority=5)
		{
			Item create = new Item("Add (Create)");
			create.onDraw = RightClick.DrawItem;
			create.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Create");
			create.color = RightClick.defaultColor;
			create.subItems = new List<Item>();
			create.priority = priority;

			//automatically adding generators from this assembly
			Type[] types = typeof(Generator).Subtypes();
			for (int t=0; t<types.Length; t++)
				if (!generatorTypes.Contains(types[t])) generatorTypes.Add(types[t]);

			//adding outer-assembly types
			//via their initialize

			//creating unsorted create items
			foreach (Type type in generatorTypes)
			{
				GeneratorMenuAttribute attribute = GeneratorDraw.GetMenuAttribute(type);
				if (attribute == null) continue;

				string texPath = attribute.iconName ?? "MapMagic/Popup/Standard";
				string texName = texPath;
				//if (StylesCache.isPro) texName += "_icon";

				Item item = new Item( ) {
					name = (attribute.menuName!=null && attribute.menuName.Length!=0) ? attribute.menuName : attribute.name, 
					onDraw = RightClick.DrawItem,
					icon = RightClick.texturesCache.GetTexture(texPath, texName),
					color = GeneratorDraw.GetGeneratorColor(attribute.colorType ?? Generator.GetGenericType(type)),
					onClick = ()=> GraphEditorActions.CreateGenerator(graph, type, mousePos),
					priority = attribute.priority };

				//moving into the right section using priority
				//int sectionPriority = 10000 - attribute.section*1000;
				//item.priority += sectionPriority;


				//placing items in categories
				string catName = attribute.menu;
				if (catName == null) continue; //if no 'menu' defined this generator could not be created 
				string[] catNameSplit = catName.Split('/');

				Item currCat = create;
				if (catName != "")  //if empty menu is defined using root category
					for (int i=0; i<catNameSplit.Length; i++)
					{
						//trying to find category
						bool catFound = false;
						if (currCat.subItems != null)
							foreach (Item sub in currCat.subItems)
							{
								if (sub.onClick == null  &&  sub.name == catNameSplit[i])
								{
									currCat = sub;
									catFound = true;
									break;
								}
							}

						//creating if not found
						if (!catFound)
						{
							Item newCat = new Item(catNameSplit[i]);
							if (currCat.subItems == null) currCat.subItems = new List<Item>();
							currCat.subItems.Add(newCat);
							currCat = newCat;

							newCat.color = item.color;
						}
					}

				if (currCat.subItems == null) currCat.subItems = new List<Item>();
				currCat.subItems.Add(item);
			}

			//default sorting order
			foreach (Item item in create.All(true))
			{
				if (item.name == "Map" && item.onClick==null) 
				{ 
					item.priority = 10004; 
					item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Map"); 
					item.color = GeneratorDraw.GetGeneratorColor(typeof(Den.Tools.Matrices.MatrixWorld)); 
				}
				if (item.name == "Objects" && item.onClick==null) 
				{
					item.priority = 10003; 
					item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Objects"); 
					item.color = GeneratorDraw.GetGeneratorColor(typeof(TransitionsList)); 
				}
				if (item.name == "Spline" && item.onClick==null) 
				{ 
					item.priority = 10002; 
					item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Spline"); 
					item.color = GeneratorDraw.GetGeneratorColor(typeof(Den.Tools.Splines.SplineSys)); 
				}
				if (item.name == "Biomes") 
				{ 
					item.priority = 9999; 
					item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Biomes"); 
					item.color = GeneratorDraw.GetGeneratorColor(typeof(IBiome)); 
				}
				if (item.name == "Functions") 
				{ 
					item.priority = 9999; 
					item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Function"); 
					item.color = GeneratorDraw.GetGeneratorColor(typeof(IBiome)); 
				}

				if (item.name == "Enter"  && item.onClick==null) { item.icon = RightClick.texturesCache.GetTexture("GeneratorIcons/FunctionIn"); }
				if (item.name == "Exit"  && item.onClick==null) { item.icon = RightClick.texturesCache.GetTexture("GeneratorIcons/FunctionOut"); }

				if (item.name == "Initial") { item.priority = 10009; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Initial"); }
				if (item.name == "Modifiers") { item.priority = 10008; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Modifier"); }
				if (item.name == "Standard") { item.priority = 10009; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Standard"); }
				if (item.name == "Output") { item.priority = 10007; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Output"); }
				if (item.name == "Outputs") { item.priority = 10006; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Output"); }
				if (item.name == "Input") { item.priority = 10005; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Input"); }
				if (item.name == "Inputs") { item.priority = 10004; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Input"); }
				if (item.name == "Portals") { item.priority = 10003; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Portals"); }
				if (item.name == "Function") { item.priority = 10002; item.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Function"); }

				if (item.name == "Height") item.priority = 10003;
				if (item.name == "Textures") item.priority = 10002;
				if (item.name == "Grass") item.priority = 10001;

				if (item.onDraw == null) item.onDraw = RightClick.DrawItem;
			}

			//adding separator between standard and special categories
			if (create.subItems.FindIndex(i=>i.name=="Biomes") >= 0) //add separator if biomes item present
			{
				Item separator = Item.Separator(priority:10001);
				separator.onDraw = RightClick.DrawSeparator;
				separator.color =  RightClick.defaultColor;
				create.subItems.Add(separator);
			}

			return create;
		}


		public static Item InsertItems (Vector2 mousePos, Graph graph, IInlet<object> inlet, int priority=4)
		{ 
			IOutlet<object> outlet = graph.GetLink(inlet);
			Item insertItems;

			if (inlet != null  &&  outlet != null)
			{
				Type genericLinkType = Generator.GetGenericType(inlet);

				Item createItems = CreateItems(mousePos, graph);
				Item catItems = createItems.Find( GetCategoryByType(genericLinkType) );
				insertItems = catItems.Find("Modifiers");

				//adding link to all create actions
				foreach(Item item in insertItems.All(true))
					if (item.onClick != null)
					{
						Action baseOnClick = item.onClick;
						item.onClick = () =>
						{
							baseOnClick();

							Generator createdGen = graph.generators[graph.generators.Length-1]; //the last on is the one that's just created. Hacky
							if (createdGen!=null  &&  Generator.GetGenericType((Generator)createdGen) == genericLinkType)
							{
								//inlet
								graph.AutoLink(createdGen, outlet);

								//outlet
								if (createdGen is IOutlet<object> createdOutlet)
									graph.Link(createdOutlet, inlet);
							}

							GraphWindow.current?.RefreshMapMagic(createdGen);
						};
					}
			}
			else
			{
				insertItems = new Item("Add");
				insertItems.onDraw = RightClick.DrawItem;
				insertItems.disabled = true;
			}

			insertItems.name = "Add (Insert)";
			insertItems.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Create");
			insertItems.color =  RightClick.defaultColor;
			insertItems.priority = priority;
			return insertItems;
		}


		public static Item AppendItems (Vector2 mousePos, Graph graph, IOutlet<object> clickedOutlet, int priority=4)
		/// Item set appeared on node or outlet click
		{ 
			Item addItems = null;

			if (clickedOutlet != null)
			{
				Type genericLinkType = Generator.GetGenericType(clickedOutlet);
				if (genericLinkType==null)
					throw new Exception("Could not find category " + clickedOutlet.GetType().ToString());

				Item createItems = CreateItems(mousePos, graph);
				addItems = createItems.Find( GetCategoryByType(genericLinkType) );
			}

			if (addItems != null  &&  addItems.subItems != null)
			{
				Item initial = addItems.Find("Initial");
				if (initial != null) initial.disabled = true;

				//adding link to all create actions
				foreach(Item item in addItems.All(true))
					if (item.onClick != null)
					{
						Action baseOnClick = item.onClick;
						item.onClick = () =>
						{
							baseOnClick();

							Generator createdGen = graph.generators[graph.generators.Length-1]; //the last on is the one that's just created. Hacky

							Vector2 pos = clickedOutlet.Gen.guiPosition + new Vector2(200, 0);
							GeneratorDraw.FindPlace(ref pos, new Vector2(100,200), GraphWindow.current.graph);
							createdGen.guiPosition = pos;

							graph.AutoLink(createdGen, clickedOutlet);

							GraphWindow.current?.RefreshMapMagic(createdGen);
						};
					}
			}
			else
			{
				addItems = new Item("Add");
				addItems.onDraw = RightClick.DrawItem;
				addItems.disabled = true;
			}

			addItems.name = "Add (Append)";
			addItems.icon = RightClick.texturesCache.GetTexture("MapMagic/Popup/Create");
			addItems.color =  RightClick.defaultColor;
			addItems.priority = priority;

			return addItems;
		}


		private static string GetCategoryByType (Type genericType)
		{
			if (genericType == typeof(Den.Tools.Matrices.MatrixWorld)) return "Map";
			else if (genericType == typeof(TransitionsList)) return "Objects";
			//else if (genericType == typeof(Den.Tools.Segs.SplineSys)) return "Spline";
			else if (genericType == typeof(Den.Tools.Splines.SplineSys)) return "Spline";
			return null;
		}
	}
}