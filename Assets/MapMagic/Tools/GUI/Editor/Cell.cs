using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Den.Tools.GUI
{

	public class Cell : IDisposable
	{
		#if UNITY_2019_3_OR_NEWER
		public const int lineHeight = 20;
		#else
		public const int lineHeight = 18;
		#endif

		public static List<Cell> activeStack = new List<Cell>();
		public static Cell current; // { get { return activeStack[activeStack.Count-1]; } }
		public static Cell Parent 
		{get{ 
			int activeStackCount = activeStack.Count;
			if (activeStack.Count < 2) return null; 
			else return activeStack[activeStack.Count-2]; 
		}}

		public bool stackHor;
		public bool stackVer;
		public Vector2 relativeSize;
		public Vector2 relativeOffset;
		public Vector2 pixelSize;
		public Vector2 pixelOffset;
		//public Vector2 pixelResize; //same as offset for size. Adjust the final size with this value

		public Vector2 minPixelSize;  // cell size could not be lesser than that because of this.pixelSize | contentsPixelSize
		public Vector2 contentsPixelSize;  //only contents size, this.pixelSize could set bigger value
		public Vector2 finalSize;
		public Vector2 cellPosition;
		public Vector2 worldPosition;


		//public Rect cellRect;
		//public Rect worldRect;

		public List<Cell> subCells;
		public int subCounter;

		public IEnumerable SubCellsRecursively (bool includeSelf=false)
		{
			if (includeSelf)
				yield return this;

			if (subCells ==null)
				yield break;

			//top level first
			int count = subCells.Count;
			for (int i=0; i<count; i++) 
			//for (int i=0; i<subCounter; i++) 
			//iterating real list, since most sub cells won't be active
				yield return subCells[i];

			for (int i=0; i<count; i++) 
				foreach (Cell subSub in subCells[i].SubCellsRecursively())
					yield return subSub;
		}

//		public string name;
		public override string ToString() { return "Cell" + " " + worldPosition.x + "," + worldPosition.y + " " + finalSize.x + "," + finalSize.y; }

		public bool disabled;
		public bool inactive; //same view, but controls (field drag, buttons) do not work 
		public float fieldWidth = 0.5f;

		public bool isLayouted = true; //is this cells size taken into account when calculating parent size? (no for scrolls)

		public bool trackChange = true;
		public bool valChanged;

		[Flags] public enum Special { None=0, Label=1, Field=2, LabelField=4, Vector=8, VectorX=16, VectorY=32, VectorZ=64, VectorW=128 } //button, icon, etc
		public Special special; //Just determines the kind of cell by it's contents with Draw. Was made to integrate expose system flawlessly.


		public Vector2 InternalCenter {get{ return new Vector2(worldPosition.x+finalSize.x/2f, worldPosition.y+finalSize.y/2); }}

		public Rect InternalRect //note it's internal rect, not scaled with zoom!
		{
			get{ return new Rect(worldPosition.x, worldPosition.y, finalSize.x, finalSize.y); } 
			set{ worldPosition=value.position; finalSize=value.size; }
		} 
		
		public bool Contains (Vector2 pos, int padding=0) 
		{ 
			return  pos.x > worldPosition.x-padding && pos.x < worldPosition.x+finalSize.x+padding &&
					pos.y > worldPosition.y-padding && pos.y < worldPosition.y+finalSize.y+padding;
		}

		public Rect GetRect (ScrollZoom scrollZoom=null, int padding=0, bool pixelPerfect=true)
		{
			if (scrollZoom==null)
				return new Rect(
					worldPosition.x + padding, 
					worldPosition.y + padding, 
					finalSize.x - padding*2, 
					finalSize.y - padding*2);
			else
				return scrollZoom.ToScreen(
					worldPosition.x + padding, 
					worldPosition.y + padding, 
					finalSize.x - padding*2, 
					finalSize.y - padding*2,
					pixelPerfect:pixelPerfect);
		}

		public void MakeStatic ()
		/// Discards scroll/zoom information for this cell, making it to be displayed at the same pos no matter of scroll and zoom
		{
			if (UI.current.scrollZoom != null)
			{
				pixelSize /= UI.current.scrollZoom.zoom;
				pixelOffset = (pixelOffset - UI.current.scrollZoom.scroll) / UI.current.scrollZoom.zoom;
			}
		}


		#region Layout

			public void CalculateMinContentsSize ()
			/// Gets minPixelSize (as well as contentsPixelSize btw)
			/// From child to parent
			{
				minPixelSize = pixelSize;
				//don't use assigned pixelSize anymore, it should not be changed with layout

				if (subCells == null) return;
				int subCount = subCells.Count;
				if (subCount == 0) return;

				contentsPixelSize = new Vector2();
				for (int s=0; s<subCount; s++)
				{
					Cell sub = subCells[s];

					if (sub.subCells != null)
						sub.CalculateMinContentsSize();
					else
						sub.minPixelSize = sub.pixelSize; //instead of CalculateMinContentsSize

					if (!sub.isLayouted)
						continue;

					if (sub.stackHor) contentsPixelSize.x += sub.minPixelSize.x;
					else contentsPixelSize.x = contentsPixelSize.x>sub.minPixelSize.x ? contentsPixelSize.x : sub.minPixelSize.x; //Mathf.Max(contentsPixelSize.x, sub.minPixelSize.x);

					if (sub.stackVer) contentsPixelSize.y += sub.minPixelSize.y;
					else contentsPixelSize.y = contentsPixelSize.y>sub.minPixelSize.y ? contentsPixelSize.y : sub.minPixelSize.y; //Mathf.Max(contentsPixelSize.y, sub.minPixelSize.y);
				}

				if (contentsPixelSize.x > minPixelSize.x) minPixelSize.x = contentsPixelSize.x;
				if (contentsPixelSize.y > minPixelSize.y) minPixelSize.y = contentsPixelSize.y;
			}


			public void CalculateSubRects ()
			/// Sets finalPixelSize for child cells
			/// From parent to children
			{
				if (subCells == null) return;
				int subCount = subCells.Count;
				if (subCount == 0) return;

				//calculating relative sum
				float relativeSizeSumX=0; float relativeSizeSumY=0; //Vector2 relativeSizeSum = new Vector2();
				for (int s=0; s<subCount; s++)
				{
					Cell sub = subCells[s];
					if (sub.stackHor) relativeSizeSumX += sub.relativeSize.x;
					if (sub.stackVer) relativeSizeSumY += sub.relativeSize.y;
				}

				//calculating the number of remaining pixels for relative
				float subsPixelsSumX=0; float subsPixelsSumY=0; //Vector2 subsPixelsSum = new Vector2();
				for (int s=0; s<subCount; s++)
				{
					Cell sub = subCells[s];
					subsPixelsSumX += sub.pixelSize.x; //using assigned size, ignoring subs min contents size
					subsPixelsSumY += sub.pixelSize.y;
				}
				//Vector2 relativePixelsLeft = new Vector2(finalSize.x - subsPixelsSum.x, finalSize.y - subsPixelsSum.y);
				float relativePixelsLeftX = finalSize.x - subsPixelsSumX;
				float relativePixelsLeftY = finalSize.y - subsPixelsSumY;

				//setting rects
				float currentPosX=0; float currentPosY=0;  //Vector2 currentPos = new Vector2();
				for (int s=0; s<subCount; s++)
				{
					Cell sub = subCells[s];

					//horizontal
					if (sub.stackHor)
					{
						float relativePixelsX = 0;
						if (relativeSizeSumX != 0 && sub.relativeSize.x != 0)
							relativePixelsX = sub.relativeSize.x/relativeSizeSumX * relativePixelsLeftX;
						
						sub.finalSize.x = sub.minPixelSize.x>relativePixelsX ? sub.minPixelSize.x : relativePixelsX ; //Mathf.Max(sub.minPixelSize.x, relativePixelsX);
						sub.cellPosition.x = currentPosX;
						
						currentPosX += sub.finalSize.x;
					}
					else
					{
						sub.finalSize.x = //Mathf.Max(sub.minPixelSize.x, finalSize.x*sub.relativeSize.x+sub.pixelSize.x);
							sub.minPixelSize.x > finalSize.x*sub.relativeSize.x+sub.pixelSize.x ? 
								sub.minPixelSize.x : 
								finalSize.x*sub.relativeSize.x+sub.pixelSize.x;
						sub.cellPosition.x = finalSize.x * sub.relativeOffset.x;
					}

					//vertical
					if (sub.stackVer)
					{
						float relativePixelsY = 0;
						if (relativeSizeSumY != 0 && sub.relativeSize.y != 0)
							relativePixelsY = sub.relativeSize.y/relativeSizeSumY * relativePixelsLeftY;
						
						sub.finalSize.y = sub.minPixelSize.y>relativePixelsY ? sub.minPixelSize.y : relativePixelsY; //Mathf.Max(sub.minPixelSize.y, relativePixelsY);
						sub.cellPosition.y = currentPosY;
						
						currentPosY += sub.finalSize.y;
					}
					else
					{
						sub.finalSize.y = //Mathf.Max(sub.minPixelSize.y, finalSize.y*sub.relativeSize.y+sub.pixelSize.y);
							sub.minPixelSize.y > finalSize.y*sub.relativeSize.y+sub.pixelSize.y ? 
								sub.minPixelSize.y : 
								finalSize.y*sub.relativeSize.y+sub.pixelSize.y;
						sub.cellPosition.y = finalSize.y * sub.relativeOffset.y;
					}

					sub.cellPosition += sub.pixelOffset;
					sub.worldPosition = worldPosition + sub.cellPosition;

					///sub.finalSize += sub.pixelResize;

					if (sub.subCells != null)
						sub.CalculateSubRects();
				}
			}


			public void CalculateRootRects ()
			/// Called on root cell to calculate all rects
			/// From parent to children
			{
				cellPosition.x = pixelOffset.x;
				cellPosition.y = pixelOffset.y;
				finalSize.x = minPixelSize.x;
				finalSize.y = minPixelSize.y;

				worldPosition = cellPosition;

				CalculateSubRects();
			}


			public void FillCellsUnderCursor (List<Cell> cells, Vector2 mousePos)
			/// Finds all child cells containing mousePos
			/// Should be done after layout
			{
				if (mousePos.x > worldPosition.x  &&  mousePos.x < worldPosition.x+finalSize.x  &&
					mousePos.y > worldPosition.y  &&  mousePos.y < worldPosition.y+finalSize.y)
						cells.Add(this);

				if (subCells != null)
				{
					int subCount = subCells.Count;
					for (int s=0; s<subCount; s++)
					{
						Cell sub = subCells[s];
						subCells[s].FillCellsUnderCursor(cells, mousePos);
					}
				}
			}


			public void UseEvenetsOnClick ()
			{
				if (Event.current.isMouse)
					Event.current.Use();
			}


		#endregion


		#region Factory / Dispose

			public static Cell GetCell ()
			{
				//trying to re-use existing cell
				Cell cell = null;
				if (current != null  &&  current.subCells != null)
				{
					int subsCount = current.subCells.Count;
					if (current.subCounter < subsCount)
					{
						cell = current.subCells[current.subCounter];
						current.subCounter ++;
					}
				}

				//creating new if could not re-use
				if (cell == null) 
				{
					cell = new Cell();

					if (current != null)
					{
						if (current.subCells == null)
							current.subCells = new List<Cell>();

						current.subCells.Add(cell);
						current.subCounter ++;
					}
				}

				if (UI.current.layout) //all parameters should remain the same on repaint
					cell.Init(current);

				return cell;
			}


			public void Init (Cell parent)
			{
				//resetting child counter
				subCounter = 0;

				//resetting parameters
				pixelOffset.x=0;		pixelOffset.y=0;
				pixelSize.x=0;			pixelSize.y=0;
				relativeSize.x=0;		relativeSize.y=0;
				relativeOffset.x=0;		relativeOffset.y=0;
				pixelOffset.x=0;		pixelOffset.y=0;
				stackHor = false;
				stackVer = false;
				isLayouted = true;
				special = Special.None;

				//copy properties
				trackChange = parent.trackChange;
				valChanged = false;
				disabled = parent.disabled;
				inactive = parent.inactive;
				fieldWidth = parent.fieldWidth;
			}


			//[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Activate ()
			{
				activeStack.Add(this);
				current = this;
			}


			public void UpdateSubs ()
			/// Removes extra subs and resets subCounter recursively
			{
				if (subCells == null) return;

				int subsCount = subCells.Count;
				if (subCounter < subsCount-1)
					subCells.RemoveRange(subCounter, subsCount-1-subCounter);
				
				subCounter = 0;

				for (int s=0; s<subsCount; s++)
				{
					if (subCells[s].subCells != null)
						subCells[s].UpdateSubs();
				}
			}


			public void Dispose ()
			{
				

				//if (activeStack[lastIndex] != this)
				//	throw new Exception("Cell: Trying to de-activate cell that is currently non-active");

			//	int lastIndex = activeStack.Count-1;
			//	activeStack.RemoveAt(lastIndex);
			//	if (activeStack.Count != 0) current = activeStack[activeStack.Count-1];
			//	else current = null;


				//if (current != this)
				//	throw new Exception("Cell: Trying to de-activate cell that is currently non-active");

				int activeStackCount = activeStack.Count;
				if (activeStackCount == 0)
					return; //otherwise EditorUtility.DisplayDialog will log an error

				activeStack.RemoveAt(activeStackCount-1); //removing last
				if (activeStackCount > 1) //if there's something in stack after remove
					current = activeStack[activeStackCount-2]; //then assigning the new last one
				else current = null;

				//removing from child list
				/*if (activeStack.Count != 0)
				{
					Cell prevActive = activeStack[activeStack.Count-1];
					//prevActive.subCells.Remove(this);
				}*/

				//removing extra children
				if (subCells != null)
				{
					int subsCount = subCells.Count;
					if (subCounter <= subsCount-1)
						subCells.RemoveRange(subCounter, subsCount-subCounter);
				}
				
				subCounter = 0;
			}


			public void Skip ()
			/// Emulates cell being drawn while it isn't (out of window, for instance)
			/// Prevents it being ruined on Dispose
			{
				if (subCells != null)
					subCounter = subCells.Count;
			}

		#endregion


		#region Static Constructors

			public static Cell Line
			{get{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,1);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}}


			public static Cell LinePx (float size)
			{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,0);
				cell.pixelSize = new Vector2(0,size);
//				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}

			public static Cell LineRel (float size)
			{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,size);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}

			public static void EmptyLinePx (float size)
			{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,0);
				cell.pixelSize = new Vector2(0,size);
//				cell.pixelOffset = new Vector2(0,0);
				if (cell.subCells != null) cell.subCells.Clear();
			}

			public static void EmptyLineRel (float size)
			{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,size);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				if (cell.subCells != null) cell.subCells.Clear();
			}

			public static void EmptyLine ()
			{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,1);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				if (cell.subCells != null) cell.subCells.Clear();
			}


			public static Cell LineStd
			{get{
				Cell cell = GetCell();
				cell.stackVer = true;
				cell.relativeSize = new Vector2(1,0);
				cell.pixelSize = new Vector2(0, lineHeight);
//				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}}


			public static Cell Row
			{get{
				Cell cell = GetCell();
				cell.stackHor = true;
				cell.relativeSize = new Vector2(1,1);
				cell.Activate();
				return cell;
			}}


			public static Cell RowPx (float size)
			{
				Cell cell = GetCell();
				cell.stackHor = true;
				cell.relativeSize = new Vector2(0,1);
				cell.pixelSize = new Vector2(size, 0);
//				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}


			public static Cell RowRel (float size)
			{
				Cell cell = GetCell();
				cell.stackHor = true;
				cell.relativeSize = new Vector2(size,1);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}


			public static void EmptyRowPx (float size)
			{
				Cell cell = GetCell();
				cell.stackHor = true;
				cell.relativeSize = new Vector2(0,1);
				cell.pixelSize = new Vector2(size, 0);
//				cell.pixelOffset = new Vector2(0,0);
				if (cell.subCells != null) cell.subCells.Clear();
			}


			public static void EmptyRowRel (float size)
			{
				Cell cell = GetCell();
				cell.stackHor = true;
				cell.relativeSize = new Vector2(size,1);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				if (cell.subCells != null) cell.subCells.Clear();
			}

			public static void EmptyRow ()
			{
				Cell cell = GetCell();
				cell.stackHor = true;
				cell.relativeSize = new Vector2(1,1);
//				cell.pixelSize = new Vector2(0,0);
//				cell.pixelOffset = new Vector2(0,0);
				if (cell.subCells != null) cell.subCells.Clear();
			}


			public static Cell Custom (float sizeX, float sizeY)
			{
				Cell cell = GetCell();
				cell.pixelSize = new Vector2(sizeX, sizeY);
				cell.stackHor = false;
				cell.stackVer = false; 
				cell.Activate();
				return cell;
			}

			public static Cell Custom (Vector2 pos, Vector2 size) => Custom(pos.x, pos.y, size.x, size.y);

			public static Cell Custom (float posX, float posY, float sizeX, float sizeY)
			{
				Cell cell = GetCell();
//				cell.relativeSize = new Vector2(0,0);
				cell.pixelSize = new Vector2(sizeX, sizeY);
				cell.pixelOffset = new Vector2(posX, posY);
				cell.Activate();
				return cell;
			}


			public static Cell Custom (Rect rect)
			{
				Cell cell = GetCell();
//				cell.relativeSize = new Vector2(0,0);
				cell.pixelSize = rect.size;
				cell.pixelOffset = rect.position;
				cell.Activate();
				return cell;
			}

			public static Cell Custom (Cell other)
			{
				Cell cell = GetCell();
				cell.relativeOffset = other.relativeOffset;
				cell.relativeSize = other.relativeSize;
				cell.isLayouted = other.isLayouted;
				cell.pixelOffset = other.pixelOffset;
				cell.pixelSize = other.pixelSize;
				cell.stackHor = other.stackHor;
				cell.stackVer = other.stackVer; 
				cell.Activate();
				return cell;
			}


			public static Cell Static (float posX, float posY, float sizeX, float sizeY)
			///Cell independent from current scroll zoom
			{
				Cell cell = GetCell();
				cell.pixelSize = new Vector2(sizeX, sizeY);
				cell.pixelOffset = new Vector2(posX, posY);

				if (UI.current.scrollZoom != null)
				{
					cell.pixelSize /= UI.current.scrollZoom.zoom;
					cell.pixelOffset = (cell.pixelOffset - UI.current.scrollZoom.scroll) / UI.current.scrollZoom.zoom;
				}

				cell.Activate();
				return cell;
			}


			public static Cell Full
			{get{
				Cell cell = GetCell();
				cell.relativeSize = new Vector2(1,1);
//				cell.pixelSize = new Vector2(0,0);
				cell.pixelOffset = new Vector2(0,0);
				cell.Activate();
				return cell;
			}} 


			public static Cell Padded (int left, int right, int top, int bottom) //padding will not be counted in min size
			{
				Cell cell = GetCell();
				cell.relativeSize = new Vector2(1,1);
				cell.pixelSize = new Vector2(-left-right, -top-bottom);
				cell.pixelOffset = new Vector2(left,top);
//				cell.pixelResize = new Vector2(-left-right, -top-bottom);
				cell.Activate();
				return cell;
			}


			public static Cell Padded (int padding)
			{
				Cell cell = GetCell();
				cell.relativeSize = new Vector2(1,1);
				cell.pixelSize = new Vector2(-padding*2, -padding*2);
				cell.pixelOffset = new Vector2(padding,padding);
//				cell.pixelResize = new Vector2(-padding*2, -padding*2);
				cell.Activate();
				return cell;
			}


			public static Cell Center (float width, float height)
			{
				Cell cell = GetCell();
				//cell.relativeSize = new Vector2(0,0);
				cell.relativeOffset = new Vector2(0.5f, 0.5f);
				cell.pixelSize = new Vector2(width, height);
				cell.pixelOffset = new Vector2(-width/2, -height/2);
//				cell.pixelResize = new Vector2(width, height);
				cell.Activate();
				return cell;
			}

			public static Cell Root (ref Cell cell, Vector2 min, Vector2 max)
			{
				if (activeStack.Count != 0)
					throw new Exception("Trying to create root cell with non-empty active stack");

				if (cell == null) cell = new Cell();
				cell.Activate(); //activeStack.Add(cell);

//				cell.relativeSize = new Vector2(0,0);
				cell.pixelSize = max-min;
				cell.pixelOffset = min;

				return cell;
			}


			public static Cell Root (ref Cell cell, Rect rect)
			{
				if (activeStack.Count != 0)
				{
					activeStack.Clear();
					throw new Exception("Trying to create root cell with non-empty active stack"); 
				}

				if (cell == null) cell = new Cell();
				cell.Activate(); //activeStack.Add(cell);
				cell.valChanged = false;

//				cell.relativeSize = new Vector2(0,0);
				cell.pixelSize = rect.size;
				cell.pixelOffset = rect.position;

				return cell;
			}

			public static Cell Root (ref Cell cell, float posX, float posY, float sizeX, float sizeY)
			{
				if (activeStack.Count != 0)
					throw new Exception("Trying to create root cell with non-empty active stack");

				if (cell == null) cell = new Cell();
				cell.Activate(); //activeStack.Add(cell);
				cell.valChanged = false;

//				cell.relativeSize = new Vector2(0,0);
				cell.pixelSize = new Vector2(sizeX, sizeY);
				cell.pixelOffset = new Vector2(posX, posY);

				return cell;
			}

		#endregion

	}

}