using System;
using UnityEngine;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Nodes;

namespace MapMagic.Nodes.GUI
{
	public static class LevelsDraw
	{	
		public const float diamondHeight = 15;
		public const int uiWidth = 140;
		public const int uiCurveHeight = 70;

		public static Vector3[] posArray = new Vector3[20];


		public static void DrawLevels (
			ref float inMin, ref float inMax, 
			ref float gamma, //1-2
			ref float outMin, ref float outMax, 
			float[] histogram = null)
		{
			//curve
			using (Cell.Line) 
			{
				using (Cell.RowPx(diamondHeight))
				{
					Texture2D minTex = UI.current.textures.GetTexture("DPUI/Levels/SliderVerBlack");
					outMin = VerDiamond(outMin, minTex);
				}

				//main field
				using (Cell.Row)
					if (!UI.current.layout && !(UI.current.optimizeElements && !UI.current.IsInWindow()))
				{
					Draw.Grid(new Color(0,0,0,0.5f));

					if (histogram != null) 
					{
						Material histogramMat = UI.current.textures.GetMaterial("Hidden/DPLayout/Histogram");
						histogramMat.SetFloatArray("_Histogram", histogram);
						histogramMat.SetVector("_Backcolor", new Vector4(0,0,0,0));
						histogramMat.SetVector("_Forecolor", new Vector4(0,0,0,0.25f));
						Draw.Texture(null, histogramMat);
					}

					DrawCurve(inMin, inMax, gamma, outMin, outMax, posArray);
					//CheckCurve(min, max, gamma);
				}

				//right slider
				using (Cell.RowPx(diamondHeight)) 
				{
					Texture2D maxTex = UI.current.textures.GetTexture("DPUI/Levels/SliderVerWhite");
					outMax = VerDiamond(outMax, maxTex);
				}
			}

			//bottom sliders
			using (Cell.LinePx(diamondHeight))
			{
				Cell.EmptyRowPx(diamondHeight); //reserved for left slider

				using (Cell.Row)
					BottomDiamonds(ref inMin, ref inMax, ref gamma);

				Cell.EmptyRowPx(diamondHeight); //reserved for right slider
			}
		}


		public static void BottomDiamonds (ref float min, ref float max, ref float gamma)
		{
			if (UI.current.layout) return;
			if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;

			float mid = min*(1-gamma/2f) + max*(gamma/2f); //in percent, 0-1
			float origMid = mid; //to track change and convert mid to gamma on change

			//preparing textures
			Texture2D minTex = UI.current.textures.GetTexture("DPUI/Levels/SliderBlack");
			Texture2D midTex = UI.current.textures.GetTexture("DPUI/Levels/SliderGray");
			Texture2D maxTex = UI.current.textures.GetTexture("DPUI/Levels/SliderWhite");

			min = HorDiamond(min, minTex, clickStart:0, clickEnd:(min+mid)/2, min:0, max:max);
			mid = HorDiamond(mid, midTex, clickStart:(min+mid)/2, clickEnd:(mid+max)/2, min:min, max:max, def:(min+max)/2, removable:true);
			max = HorDiamond(max, maxTex, clickStart:(mid+max)/2, clickEnd:1, min:min, max:1);

			//converting mid to gamma
			if (mid>origMid+0.00001f || mid<origMid-0.00001f)						
			{
				UI.current.MarkChanged();

				if (max-min!=0) gamma = (mid-min)/(max-min)*2;
				else gamma = 1;

				gamma = ((int)(gamma*1000))/1000f;
			}

			//cursor
			Rect cellRect = Cell.current.InternalRect;
			cellRect.x -= minTex.width/2; cellRect.width += minTex.width;
			Rect dispRect = UI.current.scrollZoom.ToScreen(cellRect);
			UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.MoveArrow);
		}


		public static void RightDiamonds (ref float min, ref float max)
		{
			if (UI.current.layout) return;
			if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;

			Texture2D minTex = UI.current.textures.GetTexture("DPUI/Levels/SliderVerBlack");
			Texture2D maxTex = UI.current.textures.GetTexture("DPUI/Levels/SliderVerWhite");

			min = VerDiamond(min, minTex, clickStart:0, clickEnd:(min+max)/2, min:0, max:max);
			max = VerDiamond(max, maxTex, clickStart:(min+max)/2, clickEnd:1, min:min, max:1);

			//cursor
			Rect cellRect = Cell.current.InternalRect;
			cellRect.y -= minTex.height/2; cellRect.height += minTex.height;
			Rect dispRect = UI.current.scrollZoom.ToScreen(cellRect);
			UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.MoveArrow);
		}


		private static float HorDiamond (
			float val, 
			Texture2D texture, 
			float clickStart, float clickEnd, //clicking between this values will drag the diamond
			float min=0, float max=1,
			float def=0, bool removable=false, //default when diamond is removed
			bool trackChange = true)
		{
			//if (UI.current.layout) return val;
			//if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;
			//in BottomDiamonds

			Rect cellRect = Cell.current.InternalRect;
			Vector2 cellPos = cellRect.position; //somehow differes from Cell.current.worldPosition
			Vector2 cellSize = cellRect.size;

			Rect clickRect = new Rect (0, cellRect.y, 0, cellRect.height); 
			clickRect.xMin = cellPos.x + cellSize.x*clickStart;
			clickRect.xMax = cellPos.x + cellSize.x*clickEnd;

			//appending click rects for Min and Max diamonds. Hacky, can live without it
			if (min < 0.0001f) clickRect.xMin -= texture.width/2;
			if (max > 0.999f) clickRect.xMax += texture.width/2;

			(Cell cell, Texture2D tex) dragObj = (Cell.current, texture); //using a temporary object to drag 

			if (DragDrop.TryDrag(dragObj, UI.current.mousePos))
			{
				DragDrop.group = "DragLevels";

				float newVal = (UI.current.mousePos.x - cellPos.x) / cellSize.x;

				//removing
				if (removable && !cellRect.Extended(20).Contains(UI.current.mousePos))
					newVal = def;

				//clamping
				float pixelSize = 1f / cellSize.x;
				if (newVal < min+pixelSize) newVal = min+pixelSize;
				if (newVal > max-pixelSize) newVal = max-pixelSize;

				newVal = ((int)(newVal*1000))/1000f;

				if (trackChange && (newVal>val+0.00001f || newVal<val-0.00001f))
				{
					UI.current.MarkChanged();
					val = newVal;
				}
			}

			DragDrop.TryRelease(dragObj, UI.current.mousePos);

			DragDrop.TryStart(dragObj, UI.current.mousePos, clickRect);

			//icon
			Vector2 iconPos = new Vector2(
				cellPos.x + val*cellSize.x,
				//cellPos.y + texture.height/2);
				cellPos.y + cellSize.y/2);
			Draw.Icon(texture, iconPos);

			//cursor
			cellRect.x -= texture.width/2; cellRect.width += texture.width;
			Rect dispRect = UI.current.scrollZoom.ToScreen(cellRect);
			UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.MoveArrow);

			//cursor (in BottomDiamonds)
			//cellRect.y -= texture.height/2; cellRect.height += texture.height;
			//Rect dispRect = UI.current.scrollZoom.ToScreen(cellRect);
			//UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.MoveArrow);

			return val;
		}


		private static float VerDiamond (
			float val, 
			Texture2D texture, 
			float clickStart=0, float clickEnd=1, //clicking between this values will drag the diamond. Clicking outside - ignore click. Not used
			float min=0, float max=1,  //values that could be passed over while dragging. Not used
			float def=0, bool removable=false) //default when diamond is removed
		{
			if (UI.current.layout) return val;
			if (UI.current.optimizeElements && !UI.current.IsInWindow()) return val;

			Rect cellRect = Cell.current.InternalRect;
			Vector2 cellPos = cellRect.position; //somehow differes from Cell.current.worldPosition
			Vector2 cellSize = cellRect.size;

			Rect clickRect = new Rect (cellRect.x, 0, cellRect.width, 0); 
			clickRect.yMin = cellPos.y + cellSize.y*(1-clickEnd);
			clickRect.yMax = cellPos.y + cellSize.y*(1-clickStart);

			//appending click rects for Min and Max diamonds. Hacky, can live without it
			if (min < 0.999f) clickRect.yMin -= texture.height/2;
			if (max > 0.0001f) clickRect.yMax += texture.height/2;

			(Cell cell, Texture2D tex) dragObj = (Cell.current, texture); //using a temporary object to drag 

			if (DragDrop.TryDrag(dragObj, UI.current.mousePos))
			{
				float newVal = 1 - (UI.current.mousePos.y - cellPos.y) / cellSize.y;

				//removing
				if (removable && !cellRect.Extended(20).Contains(UI.current.mousePos))
					newVal = def;

				//clamping
				float pixelSize = 1f / cellSize.y;
				if (newVal < min+pixelSize) newVal = min+pixelSize;
				if (newVal > max-pixelSize) newVal = max-pixelSize;

				newVal = ((int)(newVal*1000))/1000f;

				if (newVal>val+0.00001f || newVal<val-0.00001f)
				{
					UI.current.MarkChanged();
					val = newVal;
				}
			}

			DragDrop.TryRelease(dragObj, UI.current.mousePos);

			DragDrop.TryStart(dragObj, UI.current.mousePos, clickRect);

			//icon
			Vector2 iconPos = new Vector2(
				//cellPos.x + texture.width/2,
				cellPos.x + cellSize.x/2, 
				cellPos.y + (1-val)*cellSize.y);
			Draw.Icon(texture, iconPos);

			//cursor
			cellRect.y -= texture.height/2; cellRect.height += texture.height;
			Rect dispRect = UI.current.scrollZoom.ToScreen(cellRect);
			UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.MoveArrow);

			return val;
		}


		private static void Diamond (
			ref float val, //in percent, 0-1
			Texture2D texture,
			float min=0,
			float max=1,
			float def=0, //default when diamond is removed
			bool removable=false)
		{
			if (UI.current.layout) return;

			Rect rect = new Rect(
				Cell.current.worldPosition.x + Cell.current.finalSize.x*val - texture.width/2,
				Cell.current.worldPosition.y + Cell.current.finalSize.y/2 - texture.height/2,
				texture.width,
				texture.height);

			//drag
			DiamondDragObj dragObj = new DiamondDragObj(Cell.current, texture); //using a temporary object to drag 


			if (DragDrop.TryDrag(dragObj, UI.current.mousePos))
			{
				float newVal = (DragDrop.initialRect.center.x + DragDrop.totalDelta.x - Cell.current.worldPosition.x) / Cell.current.finalSize.x;

				//removing
				if (removable)
				{
					Rect cellRect = Cell.current.InternalRect.Extended(20);
					if (!cellRect.Contains(UI.current.mousePos))
						newVal = def;
				}

				//clamping
				if (newVal < min) newVal = min;
				if (newVal > max) newVal = max;

				if (newVal != val) 
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				//refreshing diamond pos if dragging
				rect.x = Cell.current.worldPosition.x + Cell.current.finalSize.x*val - texture.width/2;
			}

			DragDrop.TryRelease(dragObj, UI.current.mousePos);

			DragDrop.TryStart(dragObj, UI.current.mousePos, rect);


			//icon
			Draw.Icon(texture, center:rect.center);
		}


		private static void VerticalDiamond (
			ref float val, //in percent, 0-1
			Texture2D texture,
			float min=0,
			float max=1,
			float def=0, //default when diamond is removed
			bool removable=false)
		{
			if (UI.current.layout) return;

			Rect rect = new Rect(
				Cell.current.worldPosition.x + Cell.current.finalSize.x/2 - texture.width/2,
				Cell.current.worldPosition.y + Cell.current.finalSize.y*(1-val) - texture.height/2,
				texture.width,
				texture.height);

			//drag
			//DiamondDragObj dragObj = new DiamondDragObj(Cell.current, texture); //using a temporary object to drag 
			Cell dragObj = Cell.current; //only one slider per cell

			if (DragDrop.TryDrag(dragObj, UI.current.mousePos))
			{
				float newVal = (DragDrop.initialRect.center.y + DragDrop.totalDelta.y - Cell.current.worldPosition.y) / Cell.current.finalSize.y;
				newVal = 1 - newVal;

				//removing
				if (removable)
				{
					Rect cellRect = Cell.current.InternalRect.Extended(20);
					if (!cellRect.Contains(UI.current.mousePos))
						newVal = def;
				}

				//clamping
				if (newVal < min) newVal = min;
				if (newVal > max) newVal = max;

				if (newVal != val) 
				{
					UI.current.MarkChanged();
					val = newVal;
				}

				//refreshing diamond pos if dragging
				rect.y = Cell.current.worldPosition.y + Cell.current.finalSize.y*(1-val) - texture.height/2;
			}

			DragDrop.TryRelease(dragObj, UI.current.mousePos);

			DragDrop.TryStart(dragObj, UI.current.mousePos, rect);


			//icon
			Draw.Icon(texture, center:rect.center);
		}


		struct DiamondDragObj 
		{
			public Cell cell;
			public Texture2D texture;
			public DiamondDragObj (Cell cell, Texture2D texture) { this.cell=cell; this.texture=texture; }
		}


		private static void DrawCurve (
			float inMin, float inMax,  //in percent, 0-1
			float gamma, //0-2, 1 is empty
			float outMin, float outMax, 
			Vector3[] posArray = null) 
		{
			if (posArray == null) posArray = new Vector3[5];

			for (int i=1; i<posArray.Length-1; i++)
			{
				float p = 1f * (i-1) / (posArray.Length-3);
				p = 3*p*p - 2*p*p*p;

				float x = inMin + p*(inMax-inMin);
				float y = LevelsFn(x, inMin, inMax, gamma, outMin, outMax);

				x = Cell.current.worldPosition.x + x*(Cell.current.finalSize.x-1); 
				y = Cell.current.worldPosition.y + (1-y)*(Cell.current.finalSize.y-1) + 1;

				posArray[i] = UI.current.scrollZoom.ToScreen( new Vector2(x,y) );
			}

			posArray[0] = UI.current.scrollZoom.ToScreen( new Vector2(Cell.current.worldPosition.x, Cell.current.worldPosition.y+Cell.current.finalSize.y*(1-outMin)) );
			posArray[posArray.Length-1] = UI.current.scrollZoom.ToScreen( new Vector2(Cell.current.worldPosition.x+Cell.current.finalSize.x, Cell.current.worldPosition.y+Cell.current.finalSize.y*(1-outMax)) );

			UnityEditor.Handles.color = Color.black;
			UnityEditor.Handles.DrawAAPolyLine(2, posArray);
		}

		private static void CheckCurve (
			float inMin, float inMax, float gamma, float outMin, float outMax,
			Cell cell=null)
		{
			Vector3[] posArray = new Vector3[100];
			for (int i=0; i<posArray.Length; i++)
			{
				float x = 1f*i / posArray.Length;
				float y = LevelsFn(x, inMin, inMax, gamma, outMin, outMax);

				x = Cell.current.worldPosition.x + x*(Cell.current.finalSize.x-1); 
				y = Cell.current.worldPosition.y + (1-y)*(Cell.current.finalSize.y-1) + 1;

				posArray[i] = UI.current.scrollZoom.ToScreen( new Vector2(x,y) );
			}

			UnityEditor.Handles.color = Color.red;
			UnityEditor.Handles.DrawAAPolyLine(2, posArray);
		}

		private static float LevelsFn (float val, float inMin, float inMax, float gamma, float outMin, float outMax)
		/// Copy of Matrix.Levels (TODO: use shader, matrix and it's array, and call matrix.Levels)
		{
			//preliminary clamping
			if (val < inMin) return outMin;
			if (val > inMax) return outMax;

			//input
			float inDelta = inMax - inMin;
			if (inDelta != 0)
				val = (val-inMin) / inDelta;
			else
				val = inMin;

			//gamma
			if (gamma>1.00001f || gamma<0.9999f)  // gamma != 1
			{
				if (gamma<1) val = Mathf.Pow(val, gamma);
				else val = Mathf.Pow(val, 1/(2-gamma));
			}

			//output
			float outDelta = outMax - outMin;
			if (outDelta != 0)
				val = outMin + val * outDelta;
			else
				val = outMin;

			return val;
		}
	}
}