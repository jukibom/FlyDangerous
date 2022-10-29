using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Den.Tools.SceneEdit;

namespace Den.Tools.Splines
{
	public static class SplineEditor
	{
		const float knobSize = 15f;
		const float junctionSize = 5f;

		public static HashSet<(int,int)> selectedKnobs = new HashSet<(int,int)>();
		public static HashSet<(int,int)> dispSelectedKnobs = new HashSet<(int,int)>(); //knobs that are virtually selected when dragging the selection frame
		public static int[] selectionNumsBounds = new int[0]; //to clear selection if line number or num nodes in each line changed

		/*public struct Nullable<T> 
		{
			public T obj;
			public bool defined;

			public Nullable(T obj) { this.obj=obj; defined=true; }

			public static explicit operator T(Nullable<T> n) => n.obj;
			public static explicit operator Nullable<T>(T o) => new Nullable<T>(o);
		}*/

		#region Knob

			//	segments (3):	    0		1		2
			//	nodes (4):		0 ----- 1 ----- 2 ----- 3
			

			public struct KnobLocation
			{
				public int l; //line number
				public int n; //node number
				public KnobLocation (int l, int n) { this.l=l; this.n=n; }
			}

			public struct Knob
			/// Combined data from two adjacent nodes
			{
				public Vector3 pos;
				public Vector3 inDir; 
				public Vector3 outDir;
		
				public bool inDefined;
				public bool outDefined;

				public Node.TangentType type;
			}

			public static Knob GetKnob (this Line line, int n)
			{
				Knob knob = new Knob();

				//common case
				if (n > 0  &&  n < line.segments.Length)
				{
					knob.pos = line.segments[n].start.pos;
					knob.type = line.segments[n].start.type;
					knob.outDir = line.segments[n].start.dir;
					knob.inDir = line.segments[n-1].end.dir;
					knob.outDefined = knob.inDefined = true;
				}

				//first
				else if (n == 0  &&  n < line.segments.Length)
				{
					knob.pos = line.segments[n].start.pos;
					knob.type = line.segments[n].start.type;
					knob.outDir = line.segments[n].start.dir;
					knob.outDefined = true;
				}

				//last
				else if (n > 0  &&  n == line.segments.Length)
				{
					knob.pos = line.segments[n-1].end.pos;
					knob.type = line.segments[n-1].end.type;
					knob.inDir = line.segments[n-1].end.dir;
					knob.inDefined = true;
				}

				//only one
				else
				{
					knob.pos = line.segments[n].start.pos;
					knob.type = line.segments[n].start.type;
				}

				return knob;
			}

			public static void SetKnob (this Line line, Knob knob, int n)
			{
				if (n > 0)
				{
					line.segments[n-1].end.pos = knob.pos;
					if (knob.inDefined) line.segments[n-1].end.dir = knob.inDir;
					line.segments[n-1].end.type = knob.type;
				}

				if (n < line.segments.Length)
				{
					line.segments[n].start.pos = knob.pos;
					if (knob.outDefined) line.segments[n].start.dir = knob.outDir;
					line.segments[n].start.type = knob.type;
				}
			}


		#endregion

		#region Draw

			public static void DrawSplineSys (SplineSys sys, Matrix4x4 trs)
			/// Drawing inactive (non-editable) system
			{
				for (int l=0; l<sys.lines.Length; l++)
				{
					Line line = sys.lines[l];

					//segments
					for (int s=0; s<line.segments.Length; s++)
					{
						if (sys.guiDrawSegments) DrawSegment(line.segments[s], trs);
						if (sys.guiDrawDots) DrawDots(line.segments[s], trs, sys.guiDotsCount, sys.guiDotsEquidist);
					}

					//nodes
					if (sys.guiDrawNodes)
						for (int n=0; n<line.NodesCount; n++)
							DrawNode(line.GetKnob(n), trs);
				}
			}

			private static void DrawNode (Knob knob, Matrix4x4 trs)
			{
				Vector3 pos = knob.pos + new Vector3(trs.m03, trs.m13, trs.m23);

				Handles.DotHandleCap(0, pos, Quaternion.identity, HandleUtility.GetHandleSize(pos)/knobSize, EventType.Repaint);

				if (knob.type == Node.TangentType.correlated  ||  knob.type  == Node.TangentType.broken)
				{
					if (knob.inDefined) Handles.DrawLine(pos, pos+knob.inDir);
					if (knob.outDefined) Handles.DrawLine(pos, pos+knob.outDir);
				}
			}

			private static void DrawSegment (Segment segment, Matrix4x4 trs, bool dotted=false, bool equidistMarkers=false)
			{
				Vector3 pos = new Vector3(trs.m03, trs.m13, trs.m23);
				Handles.DrawBezier(
					segment.start.pos + pos, 
					segment.end.pos + pos, 
					segment.start.pos + segment.start.dir + pos, 
					segment.end.pos + segment.end.dir + pos, 
						Handles.color, null, 2);
			}


			private static void DrawDots (Segment segment, Matrix4x4 trs, int num, bool equidist=false)
			{
				for (int i=1; i<num+1; i++)
				{
					float percent = 1f*i/(num+1);

					if (equidist)
						percent = segment.NormLengthToPercent(percent);

					Vector3 point = segment.GetPoint(percent);
					Handles.DotHandleCap(0, point, Quaternion.identity, HandleUtility.GetHandleSize(point)/knobSize/3, EventType.Repaint);
				}
			}


		#endregion


		#region Edit

			
			public static bool EditSplineSys (SplineSys sys, Matrix4x4 trs, Object undoObject=null)
			/// Select, move, rotate, scale nodes (and draw selection/transform gizmos)
			/// Returns true if there was a change
			{
				bool added = false;
				bool removed = false;
				bool moved = false;

				//clearing selection if it's bounds changed
				if (!IsSplineNodeNumbersMatch(sys))
				{
					selectedKnobs.Clear();
					dispSelectedKnobs.Clear();
				}


				//adding/removing  points (before selection to remove selected)
				Event eventCurrent = Event.current;
				if (eventCurrent.type == EventType.MouseUp  &&  eventCurrent.button == 0  &&  !eventCurrent.alt  &&  !SceneEdit.Select.isFrame)  //releasing mouse with no frame
				{
					//adding
					if (eventCurrent.shift) 
						added = AddNode(sys, eventCurrent.mousePosition, undoObject);

					//removing
					else if (eventCurrent.control)
						removed = RemoveNode(sys, eventCurrent.mousePosition, undoObject);
				}


				//if selected single - drawing full ui with tangents
				if (selectedKnobs.Count == 1)
				{
					(int l, int n) = selectedKnobs.Any();
					Knob knob = sys.lines[l].GetKnob(n);

					bool dirChange, posChange = false;
					posChange = MoveNode(ref knob);
					dirChange = EditTangents(ref knob);
					
					moved = posChange || dirChange;

					if (moved) 
					{
						if (undoObject!=null)
							Undo.RecordObject(undoObject, "Spline Node Move");

						sys.lines[l].SetKnob(knob, n);
						sys.lines[l].Update();
					}
				}


				//if selected many - transforming them
				if (selectedKnobs.Count > 1)
				{
					Knob[] knobs = new Knob[selectedKnobs.Count];

					int i=0;
					foreach ((int l, int n) in selectedKnobs)
					{
						knobs[i] = sys.lines[l].GetKnob(n);
						i++;
					}

					moved = MoveSelectedNodes(knobs);

					if (moved) //updating all selected nodes
					{
						if (undoObject!=null)
							Undo.RecordObject(undoObject, "Spline Node Move");

						HashSet<Line> linesToUpdate = new HashSet<Line>(); //don't update lines per-node since several nodes could belong to one line

						i=0;
						foreach ((int l, int n) in selectedKnobs)
						{
							sys.lines[l].SetKnob(knobs[i], n);
							if (!linesToUpdate.Contains(sys.lines[l])) linesToUpdate.Add(sys.lines[l]);
							i++;
						}

						foreach (Line line in linesToUpdate)
							line.Update();
					}
				}


				//selecting nodes (after move, to avoid drawing frame instead of moving)
				SelectNodes(sys, trs);

				//re-drawing selected nodes
				if (selectedKnobs.Count != 0)
				{
					Color hColor = Handles.color;
					Handles.color = new Color(1,1,0,1);

					foreach ((int l, int n) in selectedKnobs)
					{
						Knob knob = sys.lines[l].GetKnob(n);
						DrawNode(knob, trs);
					}

					Handles.color = hColor;
				}

				return added || removed || moved;
			}


			private static bool IsSplineNodeNumbersMatch (SplineSys sys)
			/// Compares sys lines count (and number of nodes in each) with stored array
			/// BTW Will modify the array if not match
			{
				bool match = true;

				if (selectionNumsBounds.Length != sys.lines.Length) 
					{ match = false; selectionNumsBounds = new int[sys.lines.Length]; }

				if (match)
					for (int l=0; l<sys.lines.Length; l++)
					{
						int nodesCount = sys.NodesCount;
						if (selectionNumsBounds[l] != nodesCount) 
							match = false;
						selectionNumsBounds[l] = nodesCount;
					}

				return match;
			}


			private static void SelectNodes (SplineSys sys, Matrix4x4 trs)
			/// Draws selection and selects new nodes (segment starts and junctions). Returns the list of currently selected segments.
			{
				//disabling selection
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				SceneEdit.Select.UpdateFrame();

				bool selectionChanged = false;

				for (int l=0; l<sys.lines.Length; l++)
				{
					Line line = sys.lines[l];
					for (int n=0; n<line.NodesCount; n++)
					{
						Knob knob = line.GetKnob(n);

						bool isSelected = selectedKnobs.Contains((l,n));
						bool isDispSelected = dispSelectedKnobs.Contains((l,n));

						bool newSelected = isSelected;
						bool newDispSelected = isDispSelected;

						SceneEdit.Select.CheckSelected(knob.pos, knobSize/2, ref newSelected, ref newDispSelected);

						if (newSelected && !isSelected)
						{ 
							if (!SceneEdit.Select.isFrame)  selectedKnobs.Clear(); //selecting only one (but de-selecting all)
							selectedKnobs.Add((l,n)); selectionChanged=true; 
						}
						
						if (!newSelected && isSelected)
							{ selectedKnobs.Remove((l,n)); selectionChanged=true; }

						if (newDispSelected && !isDispSelected)
							dispSelectedKnobs.Add((l,n));

						if (!newDispSelected && isDispSelected)
							dispSelectedKnobs.Remove((l,n));
					}
				}

				if (selectionChanged) 
				{
					Editor[] allEditors = Resources.FindObjectsOfTypeAll<Editor>();
					for (int e=0; e<allEditors.Length; e++)
						allEditors[e].Repaint();
				}
			}


			private static bool MoveNode (ref Knob knob)
			/// Draws transform gizmo for the node, and transform it with it's tangents (i.e. it could be rotated and scale)
			{
				//hiding the defaul tool
				UnityEditor.Tools.hidden = true;

				//gathering all positions
				Vector3[] allPositions = new Vector3[] { 
					knob.pos, 
					knob.pos + knob.outDir,
					knob.pos + knob.inDir };

				//transforming
				bool changed = MoveRotateScale.Update(allPositions, knob.pos);

				//setting back positions
				if (changed)
				{
					knob.pos = allPositions[0];
					knob.outDir = allPositions[1] - knob.pos;
					knob.inDir = allPositions[2] - knob.pos;
				}

				return changed;
			}


			private static bool EditTangents (ref Knob knob)
			{
				bool change = false;

				//moving tans
				if (knob.type == Node.TangentType.broken || knob.type == Node.TangentType.correlated)
				{
					if (knob.outDefined)
					{
						Vector3 outTanPos = knob.pos+knob.outDir;
						Vector3 newOutTanPos = Handles.PositionHandle(outTanPos, Quaternion.identity);
						if ((outTanPos - newOutTanPos).sqrMagnitude != 0)
						{ 
							knob.outDir = newOutTanPos - knob.pos; 
							if (knob.type == Node.TangentType.correlated) knob.inDir = Node.CorrelatedInTangent(knob.inDir, knob.outDir);
							change = true; 
						}
					}

					if (knob.inDefined)
					{
						Vector3 inTanPos = knob.pos+knob.inDir;
						Vector3 newInTanPos = Handles.PositionHandle(inTanPos, Quaternion.identity);
						if ((inTanPos - newInTanPos).sqrMagnitude != 0)
						{ 
							knob.inDir = newInTanPos - knob.pos;
							if (knob.type == Node.TangentType.correlated) knob.outDir = Node.CorrelatedOutTangent(knob.inDir, knob.outDir);
							change = true; 
						}
					}
				}

				return change;
			}


			private static bool MoveSelectedNodes (Knob[] knobs)
			/// Draws currently selected tool gizmo and translates the selected nodes
			/// Not only moves, but rotates and scales
			{
				//hiding the defaul tool
				UnityEditor.Tools.hidden = true;

				//gathering all positions
				Vector3[] allPositions = new Vector3[knobs.Length*3]; //for each node 3 positions: 1 pos and 2 tangents
				Vector3 pivot = new Vector3(0,0,0);
				for (int k=0; k<knobs.Length; k++)
				{
					allPositions[k*3] = allPositions[k*3+1] = allPositions[k*3+2] = knobs[k].pos;
					if (knobs[k].inDefined) allPositions[k*3+1] += knobs[k].inDir;
					if (knobs[k].outDefined) allPositions[k*3+2] += knobs[k].outDir;

					pivot += knobs[k].pos;
				}
				pivot /= knobs.Length;

				//transforming
				bool changed = MoveRotateScale.Update(allPositions, pivot);

				//setting back positions
				if (changed)
				{
					for (int k=0; k<knobs.Length; k++)
					{
						knobs[k].pos = allPositions[k*3];
						knobs[k].inDir = allPositions[k*3+1] - knobs[k].pos;
						knobs[k].outDir = allPositions[k*3+2] - knobs[k].pos;
					}
				}

				return changed;
			}




			private static bool AddNode (SplineSys sys, Vector2 mousePos, Object undoObject=null)
			{
				bool change = false;

				float DistFn (Vector3 pointOnLine, Vector3 tempPoint)
				{
					Vector2 screenPoint = HandleUtility.WorldToGUIPoint(pointOnLine);
					return (screenPoint-mousePos).magnitude;
				}

				(int l, int s, float p, float dist) = sys.GetClosest(DistFn);

				if (dist < 10) //10 pixels from line on screen
				{
					//avoiding adding a node instead selection
					float minNodeDist =  GetNodeDistance(sys, mousePos);
					if (minNodeDist < knobSize/2)
						return false;

					if (undoObject!=null)
						Undo.RecordObject(undoObject, "Spline Node Add");

					Line line = sys.lines[l];
					line.Split(s, p);

					change = true;
					line.Update();
					EditorWindow.focusedWindow?.Repaint(); 
				}

				return change;
			}

			private static bool RemoveNode (SplineSys sys, Vector2 mousePos, Object undoObject)
			{
				bool change = false;

				int lineNum = 0;
				int nodeNum = 0;
				float minDist = GetNodeDistance(sys, mousePos, out lineNum, out nodeNum);

				if (minDist < knobSize/2) //10 pixels from line on screen
				{
					if (undoObject!=null)
						Undo.RecordObject(undoObject, "Spline Node Remove");

					Line line = sys.lines[lineNum];
					line.RemoveNode(nodeNum);

					change = true;
					line.Update();
					if (UnityEditor.EditorWindow.focusedWindow != null) UnityEditor.EditorWindow.focusedWindow.Repaint(); 
				}

				return change;
			}

			private static float GetNodeDistance (SplineSys sys, Vector2 mousePos, out int lineNum, out int nodeNum)
			/// Gets the closest node in screen-space, returns the distance to this node
			{
				float minDist = Mathf.Infinity;
				lineNum = 0;
				nodeNum = 0;

				for (int l=0; l<sys.lines.Length; l++) 
				{
					Line line = sys.lines[l];
					for (int n=0; n<line.NodesCount; n++)
					{
						Vector2 nodeScreenPos = HandleUtility.WorldToGUIPoint( line.GetNodePos(n) );
						float curDist = (nodeScreenPos-mousePos).magnitude;
						if (curDist < minDist) 
						{ 
							lineNum = l; 
							nodeNum = n;
							minDist = curDist; 
						}
					}
				}

				return minDist;
			}

			private static float GetNodeDistance (SplineSys sys, Vector2 mousePos)
			{
				int lineNum = 0;
				int nodeNum = 0;
				return GetNodeDistance(sys, mousePos, out lineNum, out nodeNum);
			}

		#endregion
	}
}
