using System;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools.GUI.Popup
{
	public class Item 
	{
		public const int separatorHeight = 6;
		public const int lineHeight = 20;

		public string name;
		public bool clicked;
		public bool disabled;
		public int priority;
		public Texture2D icon;  //currently doing nothing, just store data for custom draw
		public Color color;		//same
		public Action<Item,Rect> onDraw;
		public int offset;  //aka tab symbol
		public float scroll; //for the scroll position of sub-menu

		public float width;
		public float height = lineHeight;
		public bool isSeparator;

		public List<Item> subItems = null;
		public bool sortSubItems = true;
			
		public Action onClick; //action called when subitem selected
		public bool closeOnClick; //close menu after

		public int Count { get{ return subItems==null ? 0 : subItems.Count; } }
		public bool hasSubs { get{ return subItems!=null;} }

		public Item () { }
		public Item (string name, Action<Item,Rect> onDraw=null, Action onClick=null, bool disabled=false, int priority=0) 
		{ 
			this.name=name;  
			this.priority=priority; 
			this.onClick=onClick; 
			this.onDraw = onDraw;
			this.disabled=disabled;

			this.width = UnityEngine.GUI.skin.label.CalcSize( new GUIContent(name) ).x + 20;  //20 for chevron
		}
		public Item (string name, params Item[] items) 
		{
			this.name=name;  
			subItems = new List<Item>();
			subItems.AddRange(items);

			this.width = UnityEngine.GUI.skin.label.CalcSize( new GUIContent(name) ).x + 20;  //20 for chevron
		}

		public static Item Separator (int priority=0) { return new Item() { isSeparator=true, height=separatorHeight, disabled=true, priority=priority}; }

		public void SortSubItems () 
		{ 
			if (subItems == null) return;
			subItems.Sort(Compare); 
			foreach (Item subItem in subItems)
				subItem.SortSubItems();
		}
		public static void SortItems (List<Item> items) { items.Sort(Compare); }
		public static int Compare (Item a, Item b)
		{
			if (a.priority != b.priority) return b.priority - a.priority;

			if (a.name==null || b.name==null) return 0;
			if (a.name.Length==0) return -1; if (b.name.Length==0) return 1;
			if ((int)(a.name[0]) < (int)(b.name[0])) return -1;
			else if ((int)(a.name[0]) == (int)(b.name[0])) return 0;
			else return 1;
		}

		public IEnumerable<Item> All(bool inSubItems=true)
		{
			int subItemsCount = subItems.Count;
			for (int i=0; i<subItems.Count; i++)
			{
				yield return subItems[i];

				if (subItems[i].subItems != null  && inSubItems)
					foreach(Item sub in subItems[i].All(true))
						yield return sub;
			}
		}

		public Item Find (string findName, bool inSubItems=true, bool contains=false)
		{
			string findNameLower = null;
			if (contains)
				findNameLower = findName.ToLower();

			int subItemsCount = subItems.Count;
			for (int i=0; i<subItems.Count; i++)
			{
				if (subItems[i].name == null)
					continue;

				if (subItems[i].name == findName  ||  (contains && subItems[i].name.ToLower().Contains(findNameLower)))
					return subItems[i];
			}

			if (inSubItems)
			for (int i=0; i<subItems.Count; i++)
				if (subItems[i].subItems != null)
				{
					Item subFound = subItems[i].Find(findName, true);
					if (subFound != null) 
						return subFound;
				}
							
			return null;
		}

		
		public List<Item> FindAll (string findName, bool inSubItems=true, bool contains=false)
		/// Finds all references with this or contained name
		{
			List<Item> found = null;
			string findNameLower = null;
			if (contains)
				findNameLower = findName.ToLower();

			int subItemsCount = subItems.Count;
			for (int i=0; i<subItems.Count; i++)
			{
				if (subItems[i].name == null)
					continue;

				if (subItems[i].name == findName  ||  (contains && subItems[i].name.ToLower().Contains(findNameLower)))
				{
					if (found == null) found = new List<Item>();
					found.Add(subItems[i]);
				}
			}

			if (inSubItems)
			for (int i=0; i<subItems.Count; i++)
				if (subItems[i].subItems != null)
				{
					List<Item> subFound = subItems[i].FindAll(findName, true, contains);
					if (subFound != null)
					{
						if (found == null) found = new List<Item>();
						found.AddRange(subFound);
					}
				}
							
			return found;
		}
	}

}