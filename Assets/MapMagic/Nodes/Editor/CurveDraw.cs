using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Products;

namespace MapMagic.Nodes.GUI
{

	public static class CurveDraw
	{	
		private const int pointSize = 5;
		private const int moveRange = 10;
		private const int addRange = 5;
		private const int addRectDensity = 15; //each 15 pixels
		private const int maxSubSegments = 25;

		public const int uiSize = 146;
		public const int uiVertMargins = 6;
		public const float nodesDraggedZoom = 0.7f;

		private enum PointRemoveState { None, MovedOut, Removed }

		private static Curve draggedCurve; //curve currently dragging, a backup without a node removed. Presuming we can drag only one object only one instance exists


		public static void DrawCurve (Curve curve, float[] histogram)
		{
			if (!UI.current.layout && !(UI.current.optimizeElements && !UI.current.IsInWindow()))
			{
				if (histogram != null) 
				{
					Material histogramMat = UI.current.textures.GetMaterial("Hidden/DPLayout/Histogram");
					histogramMat.SetFloatArray("_Histogram", histogram);
					histogramMat.SetVector("_Backcolor", new Vector4(0,0,0,0));
					histogramMat.SetVector("_Forecolor", new Vector4(0,0,0,0.25f));
//					Draw.Texture(null, histogramMat); 
				}

				Draw.Grid(new Color(0,0,0,0.4f)); //background grid
			}

			if (UI.current.scrollZoom == null || UI.current.scrollZoom.zoom > 0.75f) 
				CurveDraw.DragCurve(curve);
			CurveDraw.DisplayCurve(curve);
		}


		public static void DragCurve (Curve curve)
		/// Backup curve to return the removed nodes when dragging cursor returned to curve field
		{
			if (UI.current.layout) return;
			if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;

			//moving
			bool isDragging = false;
			bool isReleased = false;
			int dragNum = -1;
			Curve.Node[] originalPoints = null;

			for (int i=0; i<curve.points.Length; i++)
			{
				bool newDragging = false; bool newReleased = false;
				Vector2 newPos = DragPoint(curve, i, ref newDragging, ref newReleased);

				if (newDragging)
				{
					originalPoints = new Curve.Node[curve.points.Length]; //curve.GetPositions();
					for (int p=0; p<originalPoints.Length; p++)
						originalPoints[p] = new Curve.Node(curve.points[p]);

					curve.points[i].pos = newPos;
				}

				if (newDragging || newReleased) 
					dragNum = i;

				isDragging = isDragging || newDragging;
				isReleased = isReleased || newReleased;
			}

			//adding
			if (ClickedNearCurve(curve))
			{
				Vector2 addPos = ToCurve(UI.current.mousePos);
				int addedNum = AddPoint(curve, addPos);

				//starting drag
				DragPoint (curve, addedNum, ref isDragging, ref isReleased);  //just to start drag
			}
						
			//removing
			if (isDragging)
			{
				//calc if node should be removed
				bool isRemoved = false;
				if (dragNum!=0  &&  dragNum!=curve.points.Length-1) //ignoring first and last
				{
					Vector2 pos = ToCell(curve.points[dragNum].pos);
					if (!Cell.current.InternalRect.Extended(10).Contains(pos))
						isRemoved = true;
				}

				//removing
				if (isRemoved)
				{
					UI.current.MarkChanged(completeUndo:true);
					ArrayTools.RemoveAt(ref curve.points, dragNum);
				}

				//clamping if cursor is too close to the field to remove
				else
					ClampPoint(curve, dragNum);
			}

			//if returned dragging to field
			else if (DragDrop.obj != null && DragDrop.obj.GetType() == typeof((Curve,Curve.Node,int)) )
			{
				(Curve curve, Curve.Node node, int num) dragObj = ((Curve,Curve.Node,int))DragDrop.obj;
				if (dragObj.curve == curve && !curve.points.Contains(dragObj.node))
				{
					DragDrop.TryDrag(dragObj, UI.current.mousePos);
					//to make it repaint

					if (Cell.current.InternalRect.Extended(10).Contains(UI.current.mousePos))
					{
						ArrayTools.Insert(ref curve.points, dragObj.num, dragObj.node);
						dragObj.node.pos = ToCurve(UI.current.mousePos);
						ClampPoint(dragObj.curve, dragObj.num); //this will place it between prev and next points
					}

					DragDrop.TryRelease(dragObj, UI.current.mousePos);
					//otherwise it will not be released forever
				}
			}

			if (Cell.current.valChanged)
				curve.Refresh();
		}


		public static void DisplayCurve (Curve curve)
		/// Just draws curve without dragging nodes
		{
			if (UI.current.layout) return;
			if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;

			//drawing curve nodes
			if (UI.current.scrollZoom == null  ||  UI.current.scrollZoom.zoom > nodesDraggedZoom)
				for (int i=0; i<curve.points.Length; i++)
			{
				Vector2 cellPos = ToCell(curve.points[i].pos);
				Draw.Icon(UI.current.textures.GetTexture("DPUI/Curve/Key"), cellPos);
			}

			//dispPos.x = (int)(dispPos.x + 1.5f); //1-pixel offset to place nice
			//dispPos.y = (int)(dispPos.y + 0.5f);

			//Rect pointRect = new Rect(dispPos.x-(scrolledSize-1)/2, dispPos.y-(scrolledSize-1)/2, scrolledSize,scrolledSize);
			//Texture2D pointTex = UI.current.textures.GetTexture("DPUI/Curve/Key");
			//UnityEngine.GUI.DrawTexture(pointRect, pointTex); 

			//drawing curve itself
			Material curveMat = UI.current.textures.GetMaterial("Hidden/DPLayout/Curve");
			curveMat.SetVector("_Forecolor", new Vector4(0,0,0,1));
			curveMat.SetVector("_CurveRect", Cell.current.GetRect(UI.current.scrollZoom).ToV4()); 
			curveMat.SetFloatArray("_Curve", curve.lut);
			Draw.Texture(null, curveMat); 
		}


		[Obsolete] public static void DisplayCurve_AAPolyLine (Curve curve)
		/// Older way to display a curve without shader
		{
			if (UI.current.layout) return;
			if (UI.current.optimizeElements && !UI.current.IsInWindow()) return;

			UnityEditor.Handles.color = Color.black;

			if (UI.current.scrollZoom == null  ||  UI.current.scrollZoom.zoom > nodesDraggedZoom)
			for (int i=0; i<curve.points.Length; i++)
				DrawPoint(curve.points[i]);

			int numSubSegments = UI.current.scrollZoom != null ?
				(int)(maxSubSegments*UI.current.scrollZoom.zoom) :
				maxSubSegments;
			if (numSubSegments < 3) numSubSegments = 3;
			Vector3[] posArray = new Vector3[numSubSegments]; //positions array re-use
			for (int i=0; i<curve.points.Length-1; i++)
				DrawSegment(curve.points[i], curve.points[i+1], posArray, Cell.current);

			//first and last segments
			if (curve.points[0].pos.x > 0.0001f)
				DrawHorizontalSegment(new Vector2(0,curve.points[0].pos.y), curve.points[0].pos, posArray);

			int lastNum = curve.points.Length-1;
				DrawHorizontalSegment(curve.points[curve.points.Length-1].pos, new Vector2(1,curve.points[ curve.points.Length-1].pos.y), posArray);



		}


		private static void DrawPoint (Curve.Node node)
		{
			float zoom = UI.current.scrollZoom != null ? UI.current.scrollZoom.zoom : 1;
			float scrolledSize = pointSize*UI.current.scrollZoom.zoom / 2f  +  pointSize / 2f;
			
			Vector2 cellPos = ToCell(node.pos);
			Vector2 dispPos = UI.current.scrollZoom != null ? 
				UI.current.scrollZoom.ToScreen(cellPos) :
				cellPos;

			//dispPos.x = (int)(dispPos.x + 1.5f); //1-pixel offset to place nice
			//dispPos.y = (int)(dispPos.y + 0.5f);

			Rect pointRect = new Rect(dispPos.x-(scrolledSize-1)/2, dispPos.y-(scrolledSize-1)/2, scrolledSize,scrolledSize);
			Texture2D pointTex = UI.current.textures.GetTexture("DPUI/Curve/Key");
			UnityEngine.GUI.DrawTexture(pointRect, pointTex);
		}


		private static void DrawSegment (Curve.Node prevNode, Curve.Node nextNode, Vector3[] posArray, Cell cell)
		{
			if (prevNode.pos.x > nextNode.pos.x)
				{ Debug.LogError("Wrong curve nodes order"); return; }  //throw new Exception("Wrong curve nodes order");  //should not ruin dragging

			float range = nextNode.pos.x - prevNode.pos.x;

			int pointsNum = (int)(posArray.Length*range) + 5;
			if (pointsNum > posArray.Length) pointsNum = posArray.Length; //in case spline has the only segment

			float step = 1f / (pointsNum-1);

			//making time/value array
			for (int i=0; i<pointsNum; i++)
			{
				float val = Curve.EvaluatePrecise(prevNode, nextNode, i*step);
				posArray[i] = new Vector3(prevNode.pos.x + i*step*range, val, 0);
			}

			//clamping time/values
			for (int i=1; i<pointsNum-1; i++)
				posArray[i] = Clamp01Line(posArray[i-1], posArray[i], posArray[i+1]);
			Clamp01Line(posArray[0], posArray[1], posArray[1]);
			Clamp01Line(posArray[pointsNum-2], posArray[pointsNum-2], posArray[pointsNum-1]);
				

			//transforming time/values to point coordinates
			for (int i=0; i<pointsNum; i++)
			{
				Vector2 pos = ToCell(posArray[i]);
				if (UI.current.scrollZoom != null) pos = UI.current.scrollZoom.ToScreen(pos);
				posArray[i] = pos  +  new Vector2(0.5f, 0.5f); //adding 0.5 to make the line center-sized at pixel
			}

			UnityEditor.Handles.color = Color.black;
			UnityEditor.Handles.DrawAAPolyLine(2, pointsNum, posArray);
		}


		private static void DrawHorizontalSegment (Vector2 start, Vector2 end, Vector3[] posArray)
		{
			posArray[0] = UI.current.scrollZoom != null ?
				UI.current.scrollZoom.ToScreen( ToCell(start) ) :
				ToCell(start);
			posArray[1] = UI.current.scrollZoom != null ?
				UI.current.scrollZoom.ToScreen( ToCell(end) ) :
				ToCell(end);

			//rounding to in-between pixel
			posArray[0].x = (int)(posArray[0].x)+1f;  posArray[0].y = (int)(posArray[0].y)+1f;
			posArray[1].x = (int)(posArray[1].x)+1f;  posArray[1].y = (int)(posArray[1].y)+1f;

			UnityEditor.Handles.color = Color.black;
			UnityEditor.Handles.DrawAAPolyLine(2, 2, posArray);
		}


		private static Vector2 DragPoint (Curve curve, int num, ref bool isDragging, ref bool isReleased)
		/// Will not move the point, just returns it's new dragged position
		{
			Curve.Node node = curve.points[num];
			Vector2 pos = ToCell(node.pos);
			Vector2 newPos = pos;
			Rect rect = new Rect(pos.x-moveRange, pos.y-moveRange, 1+moveRange*2, 1+moveRange*2);

			//cursor
			Rect dispRect = UI.current.scrollZoom != null ?
				UI.current.scrollZoom.ToScreen(rect.position, rect.size) :
				new Rect(rect.position, rect.size);
			UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.MoveArrow);

			(Curve,Curve.Node,int) dragObj = (curve,node,num);

			if (DragDrop.TryDrag(dragObj, UI.current.mousePos))
			{
				newPos = DragDrop.initialMousePos + DragDrop.totalDelta;
				newPos = ToCurve(newPos);

				if (newPos.x > pos.x+0.001f || newPos.x < pos.x-0.001f || newPos.y > pos.y+0.001f || newPos.y < pos.y-0.001f)
					UI.current.MarkChanged();

				isDragging = true;
			}

			if (DragDrop.TryRelease(dragObj, UI.current.mousePos))
				isReleased = true;

			if (DragDrop.TryStart(dragObj, UI.current.mousePos, rect))
				DragDrop.group = "DragCurve";

			return newPos;
		}


		private static bool ClampPoint (Curve curve, int num)
		/// return true if point was clamped (moved)
		{
			bool clamped = false;

			//between min and max
			if (curve.points[num].pos.x < 0) { curve.points[num].pos.x = 0; clamped=true; }
			if (curve.points[num].pos.x > 1) { curve.points[num].pos.x = 1; clamped=true; }
			if (curve.points[num].pos.y < 0) { curve.points[num].pos.y = 0; clamped=true; }
			if (curve.points[num].pos.y > 1) { curve.points[num].pos.y = 1; clamped=true; }

			//between prev and next
			if (num != 0)
			{
				float prev = curve.points[num-1].pos.x;
				if (curve.points[num].pos.x < prev+0.001f) { curve.points[num].pos.x = prev+0.001f;  clamped=true; }
			}

			if (num != curve.points.Length-1)
			{
				float next = curve.points[num+1].pos.x;
				if (curve.points[num].pos.x > next-0.001f) { curve.points[num].pos.x = next-0.001f; clamped=true; }
			}

			return clamped;
		}


		private static bool ClickedNearCurve (Curve curve)
		{
			//TODO: to optimize A LOT. Calculating AddCursorRect each frame are damn slow!
			//however it's called only in cursor is in curve field

			if (UI.current.layout) return false;
			//if (Event.current.type != EventType.MouseDown) return false;
			//if (!Cell.current.Rect.Extended(10).Contains(UI.mousePos)) return false;

			for (int s=0; s<curve.points.Length-1; s++)
			{
				Curve.Node prevNode = curve.points[s];
				Curve.Node nextNode = curve.points[s+1];
				float range = nextNode.pos.x - prevNode.pos.x;
				float start = prevNode.pos.x;

				int numAddRects = (int)(range * Cell.current.finalSize.x / addRectDensity) + 2;
				
				for (int i=0; i<numAddRects-1; i++)
				{
					float time = 1f*i / (numAddRects-1);
					float val = Den.Tools.Curve.EvaluatePrecise(prevNode,nextNode,time);
					Vector2 prev = ToCell( new Vector2(start + time*range,val) );

					time = 1f*(i+1) / (numAddRects-1);
					val = Den.Tools.Curve.EvaluatePrecise(prevNode,nextNode,time);
					Vector2 next = ToCell( new Vector2(start + time*range,val) );

					Rect rect = new Rect(
						prev.x,
						Mathf.Min(next.y, prev.y),
						next.x-prev.x,
						Mathf.Abs(next.y-prev.y) );

					if (rect.y < Cell.current.worldPosition.y) rect.y = Cell.current.worldPosition.y;
					if (rect.max.y > Cell.current.worldPosition.y+Cell.current.finalSize.y) rect.max = new Vector2(rect.max.x, Cell.current.worldPosition.y+Cell.current.finalSize.y);
					if (rect.height < 0) { rect.y += rect.height; rect.height = 0; }

					rect = rect.Extended(addRange);

					#if UNITY_EDITOR
					Rect dispRect = UI.current.scrollZoom != null ?
						UI.current.scrollZoom.ToScreen(rect.position, rect.size) :
						new Rect(rect.position, rect.size);
					UnityEditor.EditorGUIUtility.AddCursorRect(dispRect, UnityEditor.MouseCursor.ArrowPlus);
					//UnityEditor.EditorGUI.DrawRect(dispRect,Color.red);
					#endif

					if (Event.current.type==EventType.MouseDown  &&  rect.Contains(UI.current.mousePos)  &&  Event.current.button==0)
					{
						//excluding points that should be dragged instead
						for (int n=0; n<curve.points.Length; n++)
						{
							Vector2 nPos = ToCell(curve.points[n].pos);
							Rect nRect = new Rect(nPos.x-moveRange, nPos.y-moveRange, 1+moveRange*2, 1+moveRange*2);
							if (nRect.Contains(UI.current.mousePos)) return false;
						}

						return true;
					}
				}
			}

			return false;
		}


		private static int AddPoint (Curve curve, Vector2 addPos)
		/// Returns the number of added point. AddPos is in curve-relative coordinates
		{
			//finding segemnt
			int addSegment = 0;
			if (addPos.x < curve.points[0].pos.x) addSegment = 0;
			else if (addPos.x > curve.points[curve.points.Length-1].pos.x) addSegment = curve.points.Length;
			else
				for (int p=0; p<curve.points.Length-1; p++)
				{
					if (addPos.x > curve.points[p].pos.x && addPos.x < curve.points[p+1].pos.x)
						addSegment = p+1;
				}

			UI.current.MarkChanged();

			ArrayTools.Insert(ref curve.points, addSegment, new Curve.Node(addPos));

			return addSegment;
		}


		private static Vector2 Clamp01Line (Vector3 prev, Vector3 current, Vector3 next)
		{
			if (current.y > 1)
			{
				if (prev.y < 1)
				{
					float clampPercent = (current.y-1) / (current.y-prev.y);
					current.x = prev.x + (current.x-prev.x)*(1-clampPercent);
				}

				if (next.y < 1)
				{
					float clampPercent = (current.y-1) / (current.y-next.y);
					current.x = next.x + (current.x-next.x)*(1-clampPercent);
				}

				current.y = 1;
			}

			if (current.y < 0)
			{
				if (prev.y > 0)
				{
					float clampPercent = current.y / (current.y-prev.y);
					current.x = prev.x + (current.x-prev.x)*(1-clampPercent);
				}

				if (next.y > 0)
				{
					float clampPercent = current.y / (current.y-next.y);
					current.x = next.x + (current.x-next.x)*(1-clampPercent);
				}

				current.y = 0;
			}

			return current;
		}


		private static void DrawSegmentBeizer (Curve curve, int num, Vector3[] posArray, Cell cell)
		///For debug purpose
		{
			/*Vector2 prevPointPercent = curve.points[num-1].pos;
			Vector2 prevPointCenter = cell.finalOffset + new Vector2(prevPointPercent.x*cell.finalSize.x, (1-prevPointPercent.y)*cell.finalSize.y);
			prevPointCenter = scrollZoom.ToDisplay(prevPointCenter);

			Vector2 prevTangentPercent = curve.points[num-1].outTangent + prevPointPercent;
			Vector2 prevTangentCenter = cell.finalOffset + new Vector2(prevTangentPercent.x*cell.finalSize.x, (1-prevTangentPercent.y)*cell.finalSize.y);
			prevTangentCenter = scrollZoom.ToDisplay(prevTangentCenter);

			Vector2 nextPointPercent = curve.points[num].pos;
			Vector2 nextPointCenter = cell.finalOffset + new Vector2(nextPointPercent.x*cell.finalSize.x, (1-nextPointPercent.y)*cell.finalSize.y);
			nextPointCenter = scrollZoom.ToDisplay(nextPointCenter);

			Vector2 nextTangentPercent = curve.points[num].inTangent + nextPointPercent;
			Vector2 nextTangentCenter = cell.finalOffset + new Vector2(nextTangentPercent.x*cell.finalSize.x, (1-nextTangentPercent.y)*cell.finalSize.y);
			nextTangentCenter = scrollZoom.ToDisplay(nextTangentCenter);

			UnityEditor.Handles.DrawBezier(
				prevPointCenter, 
				nextPointCenter, 
				prevTangentCenter,
				nextTangentCenter,
				Color.red,
				Cached.GetTexture("DPUI/SplineTex", theme:stylesTheme),
				1);



			for (int i=0; i<20; i++)
			{
				Curve.Node start = curve.points[num-1];
				Curve.Node end = curve.points[num];

				float p = i/20f;
				float ip = 1f-p;
				Vector2 pos = ip*ip*ip*start.pos + 3*p*ip*ip*(start.pos+start.outTangent) + 3*p*p*ip*(end.pos+end.inTangent) + p*p*p*end.pos;

				pos = cell.finalOffset + new Vector2(pos.x*cell.finalSize.x, (1-pos.y)*cell.finalSize.y);
				pos = ui.scrollZoom.ToDisplay(pos);

				posArray[i] = pos;
			}

			UnityEditor.Handles.color = Color.yellow;
			UnityEditor.Handles.DrawAAPolyLine(1, 20, posArray);*/
		}

		private static Rect NodeRect (Curve.Node node)
		{
			Vector2 pos = ToCell(node.pos);
			return new Rect(pos.x-moveRange, pos.y-moveRange, 1+moveRange*2, 1+moveRange*2);
		}


		private static Vector2 ToCell (Vector2 pos)
		{
			Cell cell = Cell.current;

			pos.x = cell.worldPosition.x + pos.x*(cell.finalSize.x-1); 
			pos.y = cell.worldPosition.y + (1-pos.y)*(cell.finalSize.y-1);  
			return pos;
		}


		private static Vector2 ToCurve (Vector2 pos)
		{
			Cell cell = Cell.current;
			pos.x = (pos.x - cell.worldPosition.x) / (cell.finalSize.x-1);
			pos.y = 1 - (pos.y - cell.worldPosition.y)/(cell.finalSize.y-1);
			return pos;
		}
	}
}
