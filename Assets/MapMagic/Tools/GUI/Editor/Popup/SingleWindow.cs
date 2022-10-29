using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Den.Tools.GUI.Popup
{
	public class SingleWindow : PopupWindowContent 
	{
		const bool debug = false;

		public bool sortItems = true;
		public int width = 170;
		public int height = 305;

		private UI ui = UI.ScrolledUI();

		public List<Item> openedItems = new List<Item>();
		public Item RootItem => openedItems[0];
		public Item TopItem => openedItems[deepness];
		public int deepness = 0; //at which path level we are now (instead path.Count-1, since we've got to have something at the right when scrolling back)

		public static readonly Color highlightColor = new Color(0.6f, 0.7f, 0.9f);
		public static readonly Color backgroundColor =  new Color(0.925f, 0.925f, 0.925f);
		public static readonly Color highlightColorPro = new Color(0.219f, 0.387f, 0.629f);
		public static readonly Color backgroundColorPro =  new Color(0.33f, 0.33f, 0.33f);

		public static readonly float scrollSpeed = 5f;
		private static DateTime lastFrameTime = DateTime.Now;

		public string search;

		public SingleWindow (Item rootItem) => openedItems.Add(rootItem);

		public override void OnGUI (Rect rect)  =>  ui.Draw(DrawGUI, inInspector:false);

		private void DrawGUI ()
		{
			ui.scrollZoom.allowScroll = false;
			ui.scrollZoom.allowZoom = false;

			ui.optimizeElements = false;

			Draw.Rect(StylesCache.isPro ? backgroundColorPro : backgroundColor);

			//smoothly scrolling
			float deltaTime = (float)(DateTime.Now-lastFrameTime).TotalSeconds;
			lastFrameTime = DateTime.Now;

			float targetScroll = -deepness * width;

			if (!debug)
			{
				if (ui.scrollZoom.scroll.x < targetScroll)
				{
					ui.scrollZoom.scroll.x += scrollSpeed * width * deltaTime;
					if (ui.scrollZoom.scroll.x > targetScroll) ui.scrollZoom.scroll.x = targetScroll;
				}
				if (ui.scrollZoom.scroll.x > targetScroll)
				{
					ui.scrollZoom.scroll.x -= scrollSpeed * width * deltaTime;
					if (ui.scrollZoom.scroll.x < targetScroll) ui.scrollZoom.scroll.x = targetScroll;
				}
			}

			//drawing search first - otherwise it will loose focus
			using (Cell.Custom(-targetScroll, 25+2, width, 18))
			{
				Cell.EmptyRowPx(6);
				using (Cell.Row)
				{
					Draw.SearchLabel(ref search, forceFocus:true);

					if (Cell.current.valChanged)
						PerformSearch(search, openedItems[deepness], deepness);
				}
				Cell.EmptyRowPx(6);
			}

			//drawing
			for (int p=0; p<openedItems.Count; p++)
				using (Cell.RowPx(width))
					DrawMenu(openedItems[p], p);

			//refreshing selection frame
			this.editorWindow.Repaint();
		}


		private void PerformSearch (string search, Item startingItem, int startingItemNum)
		{
			//removing all other opened items
			openedItems.RemoveAfter(startingItemNum);

			//search erased - removing search tab
			if (search == null || search.Length == 0)
			{
				if (openedItems[openedItems.Count-1].name.StartsWith("Search "))
					openedItems.RemoveAfter(openedItems.Count-2);

				deepness = openedItems.Count-1;
			}

			//search entered/changed
			else
			{
				Item baseItem = null; //item we performing search at
				Item searchItem = null; //item with search results

				if (openedItems[openedItems.Count-1].name.StartsWith("Search ")) //search is already opened
				{
					searchItem = openedItems[openedItems.Count-1];
					baseItem = openedItems[openedItems.Count-2];
				}
				else //new search item
				{
					baseItem = openedItems[openedItems.Count-1];
					searchItem = new Item("Search " + baseItem.name);
					openedItems.Add(searchItem);
				}

				deepness = openedItems.Count-1;

				searchItem.subItems = baseItem.FindAll(search, inSubItems:true, contains:true);
			}
		}


		private void DrawMenu(Item item, int itemDeepness)
		{
			//header
			using (Cell.LinePx(25))
			{
				Texture2D headerTex = UI.current.textures.GetTexture("DPUI/Backgrounds/Popup");
				Draw.ColorizedTexture(headerTex, item.color);

				if (itemDeepness != 0)
				{
					Texture2D shveronTex = UI.current.textures.GetTexture("DPUI/Chevrons/TickLeft"); 
					using (Cell.RowPx(20)) Draw.Icon(shveronTex);
				}

				Draw.Label(item.name, style:UI.current.styles.boldMiddleCenterLabel);

				bool clicked = Cell.current.Contains(ui.mousePos) && Event.current.rawType == EventType.MouseDown && Event.current.button == 0 && !UI.current.layout;
				if (clicked && itemDeepness != 0) 
				{
					deepness = itemDeepness-1;

					search = null;
					UnityEditor.EditorGUI.FocusTextInControl(null); 
				}
			}

			//search (actually just phantoms, real serch is drawn before)
			Cell.EmptyLinePx(2);
			using (Cell.LinePx(18))
			{
				if (itemDeepness!=deepness)
					search = Draw.SearchLabel(search);
			}
			Cell.EmptyLinePx(2);

			//sub-items
			if (item.subItems != null)
			{
				bool coloredIcons = item.name.StartsWith("Search ");

				int itemsSpace = height-25-22;
				using (Cell.LinePx(itemsSpace))
					using (new Draw.ScrollGroup(ref item.scroll, enabled:itemsSpace<item.subItems.Count*22))
				{
					
					for (int n=0; n<item.subItems.Count; n++)
					{
						using (Cell.LinePx(0)) 
						{
							Item currItem = item.subItems[n];

							//drawing
							bool highlighted = Cell.current.Contains(ui.mousePos);
						
							if (!currItem.isSeparator)
								using (Cell.LinePx(22)) DefaultItemDraw(currItem, n, highlighted, coloredIcons);
							else
								using (Cell.LinePx(Item.separatorHeight)) DrawSeparator();

							//clicking
							bool clicked = highlighted && 
								!currItem.disabled &&
								Event.current.type == EventType.MouseDown && 
								Event.current.button == 0 && 
								!UI.current.layout;

							if (clicked && currItem.subItems != null) 
							{
								if (openedItems.Count-1 > itemDeepness)
									openedItems.RemoveAfter(itemDeepness);
								openedItems.Add(currItem);
								deepness = itemDeepness + 1;
							}
							if (clicked && currItem.onClick != null)
								currItem.onClick();

							if (clicked && currItem.closeOnClick)
								editorWindow.Close();
						}
					}
				}
			}
		}


		public void DefaultItemDraw (Item item, int num, bool selected, bool colored=false)
		{
			if (selected && !item.disabled)
				Draw.Rect(StylesCache.isPro ? highlightColorPro : highlightColor);	

			Cell.current.disabled = item.disabled;

			//icon
			using (Cell.RowPx(30))
			{
				if (colored)
					Draw.Rect(item.color);
				
				if (item.icon!=null) 
					Draw.Icon(item.icon, scale:0.5f);
			}

			//label
			using (Cell.Row) Draw.Label(item.name);

			//chevron
			if (item.subItems != null)
			{
				Texture2D chevronTex = UI.current.textures.GetTexture("DPUI/Chevrons/TickRight");
				using (Cell.RowPx(20)) Draw.Icon(chevronTex);
			}
		}


		public void DrawSeparator ()
		{
			Cell.EmptyRowPx(20);
			using (Cell.Row)
			{
				Cell.EmptyLine();
				using (Cell.LinePx(1)) Draw.Rect(Color.gray);
				Cell.EmptyLine();
			}
			Cell.EmptyRowPx(20);
		}


		public override Vector2 GetWindowSize() 
		{
			return new Vector2(width*(debug ? 5 : 1), height);
		}

		public void Show (Vector2 pos)
		{
			RootItem.SortSubItems();
			PopupWindow.Show(new Rect(pos.x-width/2,pos.y-10,width,0), this);
		}
	}

}