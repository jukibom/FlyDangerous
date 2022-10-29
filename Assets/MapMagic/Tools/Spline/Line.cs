using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;



namespace Den.Tools.Splines
{ 
	[System.Serializable]
	public class Line
	{
		public Segment[] segments = new Segment[0];

		public bool looped;
		public float length;


		public Line () {}
		public Line (Line src)
		{
			segments = new Segment[src.segments.Length];
			Array.Copy(src.segments, segments, segments.Length);

			looped = src.looped;
			length = src.length;
		}

		public Line (Vector3 start, Vector3 end) 
			{ segments = new Segment[] { new Segment(start,end) }; }


		public Vector3 GetPoint (int segNum, float segPercent)
		{
			return segments[segNum].GetPoint(segPercent);
		}

		public void SetSegmentsCount (int count) 
			{ Array.Resize(ref segments, count); }

		public void InsertSegment (int index, Segment segment)
			{ ArrayTools.Insert(ref segments, index, segment); }


		public void AddSegment (Segment segment)
			{ ArrayTools.Add(ref segments, segment); }





		public void SetAllTangentTypes (Node.TangentType type)
		{ 
			for (int s=0; s<segments.Length; s++) 
			{
				segments[s].start.type = type;
				segments[s].end.type = type;
			}
		}


		#region Nodes Operations

			//	segments (3):	    0		1		2
			//	nodes (4):		0 ----- 1 ----- 2 ----- 3

			public int NodesCount 
			{get{
				if (segments.Length == 0) return 0;
				if (looped) return segments.Length;
				else return segments.Length+1;
			}}

			public void InsertNode (int n, Node node) 
			{ 
				InsertSegment(n-1, new Segment(segments[n-1].start, node));
				segments[n].start = node;
				segments[n].start.dir = -segments[n].start.dir;
			}

			public void RemoveNode (int n)
			{
				if (n == segments.Length) ArrayTools.RemoveAt(ref segments, segments.Length-1);
				else if (n == 0) ArrayTools.RemoveAt(ref segments, 0);
				else
				{
					Segment joined = Segment.Join(segments[n-1], segments[n]);
					segments[n-1] = joined;
					ArrayTools.RemoveAt(ref segments, n);
				}
			}

			public void AddNode (Node node) 
			{   
				AddSegment(new Segment(segments[segments.Length-1].end, node));
			}

			public void SetNodes (Vector3[] poses)
			{
				segments = new Segment[poses.Length-1];
				for (int s=0; s<segments.Length; s++)
					segments[s] = new Segment(poses[s], poses[s+1]);
			}

			public Vector3 GetNodePos (int n)
			{
				if (looped) n = n % segments.Length;
				if (n < segments.Length) return segments[n].start.pos;
				else return segments[n-1].end.pos;
			}

			public void SetNodePos (int n, Vector3 pos)
			{
				if (n != segments.Length) 
					segments[n].start.pos = pos;
				if (n != 0) 
					segments[n-1].end.pos = pos;
			}

			public Vector3? GetNodeInTangent (int n)
			{
				if (n == 0) return null;
				return segments[n-1].end.dir;
			}

			public Vector3? GetNodeOutTangent (int n)
			{
				if (n == segments.Length) return null;
				else return segments[n].start.dir;
			}

		#endregion


		#region Update

			public void Update ()
			/// Called by GUI on every line change and by system on global change
			{
				UpdatePositions();
				UpdateTangents();
				UpdateLength();
			}


			public void UpdatePositions ()
			{
				for (int s=0; s<segments.Length-1; s++)
					segments[s].end.pos = segments[s+1].start.pos;
			}


			public void UpdateTangents ()
			{
				for (int n=0; n<segments.Length+1; n++)
					UpdateTangent(n);
			}


			public void UpdateTangent (int n)
			{
				//first
				if (n == 0)
				{
					Node.TangentType type = segments[n].start.type;
					if (type == Node.TangentType.auto || type == Node.TangentType.linear)
						segments[n].start.dir = Node.LinearTangent(segments[n].start.pos, segments[n].end.pos);
					//do nothing for correlated and broken
				}

				//common case
				else if (n != segments.Length)
				{
					Node.TangentType type = segments[n].start.type;
					segments[n-1].end.type = type;
					if (type == Node.TangentType.broken) return; //do nothing for broken

					Vector3 inDir = segments[n-1].end.dir;
					Vector3 outDir = segments[n].start.dir;

					if (type == Node.TangentType.auto)
						(inDir, outDir) = Node.AutoTangents(segments[n-1].start.pos, segments[n-1].end.pos, segments[n].end.pos);

					else if (type == Node.TangentType.linear)
						(inDir, outDir) = Node.LinearTangents(segments[n-1].start.pos, segments[n-1].end.pos, segments[n].end.pos);

					else if (type == Node.TangentType.correlated)
						inDir = Node.CorrelatedInTangent(inDir, outDir);

					segments[n-1].end.dir = inDir;
					segments[n].start.dir = outDir;
				}

				//last
				else
				{
					Node.TangentType type = segments[n-1].end.type;
					if (type == Node.TangentType.auto || type == Node.TangentType.linear)
						segments[n-1].end.dir = Node.LinearTangent(segments[n-1].end.pos, segments[n-1].start.pos);
				}
			}


			public void UpdateLength (int iterations=32, float[] tmp=null)
			//much slower than UpdateTangents and not always needed
			{
				if (tmp == null) tmp = new float[iterations];

				length = 0;
				for (int s=0; s<segments.Length; s++)
				{
					segments[s].UpdateLength(iterations, tmp);
					length += segments[s].length;
				}
			}


			public void UpdateLength (int num, int iterations=32)
			{
				segments[num].UpdateLength(iterations);
				length = 0;
				for (int s=0; s<segments.Length-1; s++)
					length += segments[s].length;
			}

		#endregion



		#region Ops

			public Vector3[] GetAllPoints (float resPerUnit=0.1f, int minRes=3, int maxRes=20)
			/// Converts line into array of points to draw polyline
			/// Requires length updated
			{
				//calculating number of points
				int numPoints = 0;
				for (int s=0; s<segments.Length; s++)
				{
					int modRes = (int)( segments[s].length * resPerUnit );
					if (modRes < minRes) modRes = minRes;
					if (modRes > maxRes) modRes = maxRes;

					numPoints += modRes;
				}

				Vector3[] points = new Vector3[numPoints + 1];
			
				int i=0;
				for (int s=0; s<segments.Length; s++)
				{
					int modRes = (int)( segments[s].length * resPerUnit );
					if (modRes < minRes) modRes = minRes;
					if (modRes > maxRes) modRes = maxRes;

					for (int p=0; p<modRes; p++)
					{
						float percent = 1f*p / modRes;
						points[i] = segments[s].GetPoint(percent);
						i++;
					}
				}

				//the last one
				points[points.Length-1] = segments[segments.Length-1].end.pos;

				return points; 
			}


			public void Split (int s, float p)
			{
				(Segment prev, Segment next) = segments[s].GetSplitted(p);
				segments[s] = prev;
				ArrayTools.Insert(ref segments, s+1, next);
			}


			public void Subdivide (int num)
			/// Splits each segment in several shorter segments
			{
				Segment[] newSegments = new Segment[segments.Length*num];
				for (int s=0; s<segments.Length; s++)
				{
					Segment currSegment = segments[s];

					for (int i=0; i<num; i++)
					{
						float percent = 1f / (num-i);
						(Segment,Segment) split = currSegment.GetSplitted(percent);

						newSegments[s*num+i] = split.Item1;
						currSegment = split.Item2;
					}
				}

				segments = newSegments;
			}


			public (int l, int s, float p, float dist) GetClosest (Vector3 point, int initialApprox=10, int recursiveApprox=10) =>
				GetClosest(null, point, initialApprox, recursiveApprox);

			public (int l, int s, float p, float dist) GetClosest (Func<Vector3,Vector3,float> distanceFn, int initialApprox=10, int recursiveApprox=10) =>
				GetClosest(distanceFn, initialApprox, recursiveApprox);
			
			public (int l, int s, float p, float dist) GetClosest (Func<Vector3,Vector3,float> distanceFn, Vector3 point, int initialApprox=10, int recursiveApprox=10)
			{
				float minDist = float.MaxValue;
				int ml=0; int ms=0; float mp=0;

				for (int s=0; s<segments.Length; s++)
				{
					if (distanceFn==null  &&  !segments[s].IsWithinRange(point, Mathf.Sqrt(minDist)))
						continue;

					(float curPercent, float curDist) = segments[s].GetClosest(distanceFn, point, initialApprox, recursiveApprox);
					if (curDist < minDist)
					{
						minDist = curDist;
						ms = s;
						mp = curPercent;
					}
				}

				return (ml, ms, mp, minDist);
			}

			public void Optimize (float deviation)
			/// Removes those nodes that should not change the shape a lot
			/// Works with auto-tangents only
			{
				int iterations = segments.Length-1; //in worst case should remove all but start/end
				if (iterations <= 0) return;
				for (int i=0; i<iterations; i++) //using recorded itterations since nodes count will change
				//for (int i=0; i<deviation; i++)
				{
					float minDeviation = float.MaxValue;
					int minN = -1;

					for (int s=1; s<segments.Length; s++)
					{
						//checking how far point placed from tangent-tangent line
						float currDistToLine = DistanceToLine(
							segments[s-1].start.pos + segments[s-1].start.dir, 
							segments[s].end.pos + segments[s].end.dir, 
							segments[s].start.pos);
						//float currLine = ((nodes[n-1].pos+nodes[n-1].outDir) - (nodes[n+1].pos+nodes[n+1].inDir)).magnitude;
						//float currDeviation = (currDistToLine*currDistToLine) / currLine;
						float currDeviation = currDistToLine;

						if (currDeviation < minDeviation)
						{
							minN = s;
							minDeviation = currDeviation;
						}
					}

					if (minDeviation > deviation) break;

					segments[minN-1].end = segments[minN].end;
					ArrayTools.RemoveAt(ref segments, minN);

					UpdateTangents(); //(minN-1);
				}
			}

			private static float DistanceToLine (Vector3 lineStart, Vector3 lineEnd, Vector3 point)
			/// Just helper fn for optimize, isn't related with beizer
			{
				Vector3 lineDir = lineStart-lineEnd;
				float lineLengthSq = lineDir.x*lineDir.x + lineDir.y*lineDir.y + lineDir.z*lineDir.z;
				float lineLength = Mathf.Sqrt(lineLengthSq);
				float startDistance = (lineStart-point).magnitude;
				float endDistance = (lineEnd-point).magnitude;

				//finding height of triangle 
				float halfPerimeter = (startDistance + endDistance + lineLength) / 2;
				float square = Mathf.Sqrt( halfPerimeter*(halfPerimeter-endDistance)*(halfPerimeter-startDistance)*(halfPerimeter-lineLength) );
				float height = 2/lineLength * square;

				//dealing with out of line cases
				float distFromStartSq = startDistance*startDistance - height*height;
				float distFromEndSq = endDistance*endDistance - height*height;

				if (distFromStartSq > lineLengthSq && distFromStartSq > distFromEndSq) return endDistance;
				else if (distFromEndSq > lineLengthSq) return startDistance; 
				else return height;
			}


			public void Relax (float blur, int iterations) 
			/// Moves nodes to make the spline smooth
			/// Works with auto-tangents only
			{
				for (int i=0; i<iterations; i++)
					Relax(blur);
			}

			public void Relax (float blur)
			/// Moves nodes to make the spline smooth
			/// Works with auto-tangents only
			{
				for (int n=1; n<segments.Length; n++)
				{
					Vector3 midPos = (segments[n-1].start.pos + segments[n].end.pos)/2;
					segments[n].start.pos = midPos*blur/2f + segments[n].start.pos*(1-blur/2f);
					segments[n-1].end.pos = segments[n].start.pos;
				}
			}


			public void CutByRect (Vector3 pos, Vector3 size)
			/// Splits all segments so that each intersection with AABB rect has a node
			{			
				for (int i=0; i<13; i++) //spline could be divided in 12 parts maximum
				{
					List<Segment> newSegments = new List<Segment>();

					for (int s=0; s<segments.Length; s++)
					{
						//early check - if inside/outside rect
						Vector3 min = segments[s].ApproxMin();
						Vector3 max = segments[s].ApproxMax();

						if (max.x < pos.x  ||  min.x > pos.x+size.x ||
							max.z < pos.z  ||  min.z > pos.z+size.z) 
								{ newSegments.Add(segments[s]); continue; } //fully outside
						if (min.x > pos.x  &&  max.x < pos.x+size.x &&
							min.z > pos.z  &&  max.z < pos.z+size.z) 
								{ newSegments.Add(segments[s]); continue; } //fully inside

						//splitting
						float sp = segments[s].IntersectRect(pos, size);
						if (sp < 0.0001f  ||  sp > 0.999f) 
							{ newSegments.Add(segments[s]); continue; }  //no intersection

						(Segment s1, Segment s2) = segments[s].GetSplitted(sp);
						newSegments.Add(s1);
						newSegments.Add(s2);
					}

					bool segemntsCountChanged = segments.Length != newSegments.Count;
					segments = newSegments.ToArray();
					if (!segemntsCountChanged) break; //if no nodes added - exiting 12 iterations
				}
			}


			public List<Line> OuterSegmentsRemoved (Vector3 pos, Vector3 size)
			/// Removes segments with center placed out of pos-size AABB rect
			/// Since some of the segments removed this should create several new lines
			{
				List<Line> newLines = new List<Line>();
				List<Segment> newSegments = new List<Segment>();

				for (int s=0; s<segments.Length; s++)
				{
					Vector3 center = segments[s].GetPoint(0.5f);

					bool inRect = center.x > pos.x  &&  center.x < pos.x+size.x  &&
								  center.z > pos.z  &&  center.z < pos.z+size.z;

					//if in rect - going on picking nodes
					if (inRect) newSegments.Add(segments[s]);

					//if first one not in rect - closing line
					else if (newSegments.Count != 0)
					{
						Line line = new Line {segments = newSegments.ToArray()};
						newLines.Add(line);
						newSegments.Clear();
					}
				}

				//closing line when it's ended within rect
				if (newSegments.Count != 0)
				{ 
					Line line = new Line {segments = newSegments.ToArray()};
					newLines.Add(line);
				}

				return newLines;
			}


			public List<Line> Clamped (Vector3 pos, Vector3 size)
			/// Splits all segments so they are within AABB
			/// This will create new lines
			{
				Line cpy = new Line(this);
				cpy.CutByRect(pos, size);
				return cpy.OuterSegmentsRemoved(pos, size);
			}


			public void PushPoints (Vector3[] points, float[] ranges, bool horizontalOnly=true, float distFactor=1)
			{
				for (int s=0; s<segments.Length; s++)
					segments[s].PushPoints(points, ranges, horizontalOnly, distFactor);
			}


			public void PushStartEnd (Vector3[] points, float[] ranges, bool horizontalOnly=true, float distFactor=1)
			{
				for (int s=0; s<segments.Length; s++)
					segments[s].PushStartEnd(points, ranges, horizontalOnly, distFactor);
			}


			public void SplitNearPoints (Vector3[] points, float[] ranges, bool horizontalOnly=true, float startEndProximityFactor=0, int maxIterations=10)
			/// Splits segments within the range from each point
			/// If skipCloseStartEnd enabled disregards points that are withinrange from start or end
			{
				if (maxIterations == 0) return;

				List<Segment> newSegments = new List<Segment>();
				for (int s=0; s<segments.Length; s++)
				{
					bool isNear = segments[s].IsNearPoints(points, ranges, out float nearPercent, out int nearPointNum, horizontalOnly, startEndProximityFactor);
					if (!isNear) { newSegments.Add(segments[s]); continue; }

					(Segment seg1, Segment seg2) = segments[s].GetSplitted(nearPercent);
					newSegments.Add(seg1);
					newSegments.Add(seg2);
				}

				bool countChanged = segments.Length != newSegments.Count;
				segments = newSegments.ToArray();

				if (!countChanged  ||  maxIterations<=1) return;
				else SplitNearPoints(points, ranges, horizontalOnly, startEndProximityFactor, maxIterations-1);
			}

		#endregion

		#region Weld

			public static bool AreCloseToWeld (Line line1, Line line2, float threshold)
			{
				Vector3[] line2Mins = new Vector3[line2.segments.Length];
				Vector3[] line2Maxs = new Vector3[line2.segments.Length];

				for (int s2=0; s2<line2.segments.Length; s2++)
				{
					line2Mins[s2] = line2.segments[s2].ApproxMin();
					line2Maxs[s2] = line2.segments[s2].ApproxMax();
				}

				for (int s1=0; s1<line1.segments.Length; s1++)
				{
					Vector3 min1 = line1.segments[s1].ApproxMin(); min1.x-=threshold; min1.y-=threshold; min1.z-=threshold;
					Vector3 max1 = line1.segments[s1].ApproxMax(); min1.x+=threshold; min1.y+=threshold; min1.z+=threshold;

					for (int s2=0; s2<line2.segments.Length; s2++)
					{
						if (line2Mins[s2].x > max1.x || line2Maxs[s2].x < min1.x ||
							line2Mins[s2].y > max1.y || line2Maxs[s2].y < min1.y ||
							line2Mins[s2].z > max1.z || line2Maxs[s2].z < min1.z)
								continue;

						return true;
					}
				}

				return false;
			}


			public static Line[] WeldClose (Line line1, Line line2, float threshold)
			{
				line1.CutSegmentsForWeld(line2, threshold);
				line2.CutSegmentsForWeld(line1, threshold);

				OverlapLines(line1, line2, threshold);
				//return new Line[] { line1, line2 };

				Segment[] allSegs = new Segment[line1.segments.Length+line2.segments.Length];
				Array.Copy(line1.segments, allSegs, line1.segments.Length);
				Array.Copy(line2.segments, 0, allSegs, line1.segments.Length, line2.segments.Length);
				return NeatWeldSegments(allSegs);
			}


			public void CutSegmentsForWeld (Line line2, float threshold)
			/// Cuts line1 segment if line2 has a point within threshold
			{
				for (int i=0; i<=line2.segments.Length+2; i++)
				//while true
				{
					List<Segment> newSegs = new List<Segment>();

					for (int s1=0; s1<segments.Length; s1++)
					{
						Vector3 min = segments[s1].ApproxMin() - new Vector3(threshold,threshold,threshold);
						Vector3 max = segments[s1].ApproxMax() + new Vector3(threshold,threshold,threshold);

						float splitPercent = -1;

						for (int p2=0; p2<line2.segments.Length+1; p2++)
						{
							Vector3 point2 = p2!=line2.segments.Length ? line2.segments[p2].start.pos : line2.segments[p2-1].end.pos;

							if (point2.x < min.x || point2.x > max.x || 
								point2.y < min.y || point2.y > max.y ||
								point2.z < min.z || point2.z > max.z)
									continue;

							if ((point2-segments[s1].start.pos).sqrMagnitude < threshold*threshold ||
								(point2-segments[s1].end.pos).sqrMagnitude < threshold*threshold )
									continue;
									//if close to segment start or end

							(float percent, float distSq) = segments[s1].GetClosest(point2, 5,5);

							if (distSq > threshold*threshold) 
								continue;
							else
								splitPercent = percent;
						}

						if (splitPercent < 0) //no split for this segment
							newSegs.Add(segments[s1]);

						else //splitting
						{
							(Segment prevSeg, Segment nextSeg) = segments[s1].GetSplitted(splitPercent);
							newSegs.Add(prevSeg);
							newSegs.Add(nextSeg);
						}
					}

					if (newSegs.Count == segments.Length)
						return;
					segments = newSegs.ToArray();
					
					if (i==line2.segments.Length+2)
						throw new Exception("CutSegmentsForWeld reached maximum number of iterations");
				}
			}

			public static Line[] NeatWeldSegments (Segment[] segments)
			/// Creates a spline system from splitted segments heap
			{
				bool[] usedSegments = new bool[segments.Length];

				//removing degradated segments
				for (int s=0; s<segments.Length; s++)
					if ((segments[s].start.pos-segments[s].end.pos).sqrMagnitude < 0.0001f)
						usedSegments[s] = true;

				//removing duplicating segments
				for (int s1=0; s1<segments.Length; s1++)
				{
					if (usedSegments[s1]) continue;

					for (int s2=0; s2<segments.Length; s2++)
					{
						if (usedSegments[s2]) continue;
						if (s2 == s1) continue;

						if ((segments[s1].start.pos-segments[s2].start.pos).sqrMagnitude < 0.0001f  &&
							(segments[s1].end.pos-segments[s2].end.pos).sqrMagnitude < 0.0001f)
								usedSegments[s2] = true;

						if ((segments[s1].start.pos-segments[s2].end.pos).sqrMagnitude < 0.0001f  &&
							(segments[s1].end.pos-segments[s2].start.pos).sqrMagnitude < 0.0001f)
								usedSegments[s2] = true;
					}
				}

				//finding weld indexes
				Dictionary<int,int> weldLut = CompileWeldIndexLut(segments, usedSegments);

				//compiling line segments
				List< List<int> > allLinesSegs = new List<List<int>>(); //Each sub-list is a line, in that order. Storing segment indexes only
				for (int s=0; s<segments.Length; s++) 
				{
					if (usedSegments[s]) continue;

					if (!weldLut.ContainsKey(s*2) || !weldLut.ContainsKey(s*2+1)) //finding any line start
						allLinesSegs.Add( CompileLineSegments(weldLut, usedSegments, s) );
				}

				//creating lines
				Line[] lines = new Line[allLinesSegs.Count];
				for (int l=0; l<lines.Length; l++)
				{
					Segment[] lineSegs = new Segment[allLinesSegs[l].Count];
					for (int s=0; s<lineSegs.Length; s++)
						lineSegs[s] = segments[allLinesSegs[l][s]];

					lines[l] = new Line() { segments=lineSegs };
				}

				//inversing line segments if needed
				foreach (Line line in lines)
					line.AutoInverseSegments();

				return lines;
			}

			private static Dictionary<int,int> CompileWeldIndexLut (Segment[] segments, bool[] usedSegments)
			/// Creates welding lut - for each of the segments start (index s*2) and end (s*2+1) finds other segments start or end with the same position
			/// Will ignore segment tip if it has no connections OR has 2 or more connections
			/// (to get segment num from index just use t/2)
			{
				Dictionary<int,int> weldLut = new Dictionary<int,int>();

				for (int t1=0; t1<segments.Length * 2; t1++) 
				{
					int s1 = t1/2;
					if (usedSegments[s1]) continue;

					Vector3 point1 = t1%2 == 0 ?
						segments[s1].start.pos :
						segments[s1].end.pos;

					int numSegmentsConnected = 0;
					int lastConnectedIndex = -1;

					for (int s2=0; s2<segments.Length; s2++)
					{
						if (usedSegments[s2]) continue;
						if (s2==s1) continue;

						if ((point1-segments[s2].start.pos).sqrMagnitude < 0.0001f)
							{ lastConnectedIndex=s2*2; numSegmentsConnected++; }

						if ((point1-segments[s2].end.pos).sqrMagnitude < 0.0001f)
							{ lastConnectedIndex=s2*2+1; numSegmentsConnected++; }
					}

					if (numSegmentsConnected == 1)
						weldLut.Add(t1, lastConnectedIndex);
				}

				return weldLut;
			}

			private static List<int> CompileLineSegments (Dictionary<int,int> weldLut, bool[] usedSegments, int startSegNum)
			{
				List<int> lineSegs = new List<int>();
				lineSegs.Add(startSegNum);
				usedSegments[startSegNum] = true;

				int startIndex = startSegNum*2;
				int endIndex = startSegNum*2+1;

				int nextIndex = -1;
				if (weldLut.ContainsKey(startIndex)) nextIndex = startIndex;
				if (weldLut.ContainsKey(endIndex)) nextIndex = endIndex;
				if (nextIndex == -1) return lineSegs;

				for (int i=0; i<=usedSegments.Length; i++) //while true
				{
					int prevIndex = weldLut[nextIndex];

					int segNum = prevIndex/2;

					startIndex = segNum*2;
					endIndex = segNum*2+1;

					nextIndex = prevIndex==startIndex ? endIndex : startIndex;

					if (usedSegments[segNum])
						throw new Exception("CompileLineSegments trying to add used segment to line");

					lineSegs.Add(segNum);
					usedSegments[segNum] = true;

					if (!weldLut.ContainsKey(nextIndex)) break;

					if (i==usedSegments.Length)
						throw new Exception("CompileLineSegments reached maximum number of iterations");
				}

				return lineSegs;
			}


			public static void OverlapLines (Line line1, Line line2, float threshold)
			/// Welds line points if they are within threshold distance
			/// Doesn't combine points, just places them in a same position
			{
				line1.OverlapTo(line2, threshold, moveFactor:0.5f);
				line2.OverlapTo(line1, threshold);
				line1.OverlapTo(line2, threshold);
			}


			public void OverlapTo (Line refLine, float threshold, float moveFactor=1)
			/// Makes this line points take form of refLine points if they are within threshold
			{
				for (int n=0; n<segments.Length+1; n++)
				{
					Vector3 point = n!=segments.Length ? segments[n].start.pos : segments[n-1].end.pos;

					int n2 = refLine.GetClosestNodeIndex(point, maxThreshold:threshold);
					if (n2 < 0) //no points within threshold
						continue;

					Vector3 refPoint = refLine.GetNodePos(n2);
					SetNodePos(n, refPoint*moveFactor + point*(1-moveFactor));
				}
			}


			public static bool AreCodirected (Line line1, Line line2)
			/// Checks if two lines have the same orientation (should not be inverted to merge) by comparing the closest points
			/// More reliable for merge operations than comparing lines starts/ends
			{
				(int n1, int n2) = GetClosestNodeIndexes(line1, line2);

				Vector3 prev1 = line1.GetNodePos(n1>0 ? n1-1 : n1); 
				Vector3 next1 = line1.GetNodePos(n1<line1.NodesCount-1 ? n1+1 : line1.NodesCount-1); 

				Vector3 prev2 = line2.GetNodePos(n2>0 ? n2-1 : n2); 
				Vector3 next2 = line2.GetNodePos(n2<line2.NodesCount-1 ? n2+1 : line2.NodesCount-1); 
				
				float prevDist = (prev1-prev2).sqrMagnitude;
				float nextDist = (next1-next2).sqrMagnitude;
				float aCrossDist = (next1-prev2).sqrMagnitude;
				float bCrossDist = (prev1-next2).sqrMagnitude;

				if (prevDist<aCrossDist  &&  prevDist<bCrossDist  &&  prevDist<nextDist) return true;
				if (nextDist<aCrossDist  &&  nextDist<bCrossDist  &&  nextDist<prevDist) return true;
				if (aCrossDist<prevDist  &&  aCrossDist<nextDist  &&  aCrossDist<bCrossDist) return false;
				if (bCrossDist<prevDist  &&  bCrossDist<nextDist  &&  bCrossDist<aCrossDist) return false;

				throw new Exception("Failed to determine codirection");
			}


			private static (int,int) GetClosestNodeIndexes (Line line1, Line line2, 
				float maxThreshold=float.MaxValue, float minThreshold=-1, 
				bool[] ignore1=null, bool[] ignore2=null)
			/// Finds two closest nodes of two splines
			/// Will ignore points closer than minThreshold and further than maxThreshold
			/// Or points that used
			{
				int closestIndex1 = -1;
				int closestIndex2 = -1;
				float minDist = float.MaxValue;

				for (int n1=0; n1<line1.segments.Length+1; n1++)
				{
					if (ignore1!=null && ignore1[n1]) continue;

					Vector3 point1 = n1!=line1.segments.Length ? line1.segments[n1].start.pos : line1.segments[n1-1].end.pos;

					int n2 = line2.GetClosestNodeIndex(point1, minThreshold, maxThreshold, ignore2);
					if (n2 < 0) continue; //no points within threshold
					Vector3 point2 = line2.GetNodePos(n2);

					float dist = (point1-point2).sqrMagnitude;
					if (dist < minDist)
					{
						minDist = dist;
						closestIndex1 = n1;
						closestIndex2 = n2;
					}
				}

				return (closestIndex1, closestIndex2);
			}


			public int GetClosestNodeIndex (Vector3 point, float maxThreshold=float.MaxValue, float minThreshold=0, bool[] ignore=null)
			/// Finds the closest segment start (or end for the last one) to the given point
			/// Will ignore points closer than minThreshold and further than maxThreshold
			{
				float minDistSq = float.MaxValue;
				int minIndex = -1;

				for (int s=0; s<segments.Length+1; s++)
				{
					if (ignore!=null && ignore[s]) continue;

					Vector3 npoint = s!=segments.Length ? segments[s].start.pos : segments[s-1].end.pos;

					float distSq =  (point.x-npoint.x)*(point.x-npoint.x) +
									(point.y-npoint.y)*(point.y-npoint.y) +
									(point.z-npoint.z)*(point.z-npoint.z);

					if (distSq < minDistSq  &&  distSq >= minThreshold*minThreshold  &&  distSq < maxThreshold*maxThreshold)
						{ minDistSq=distSq; minIndex=s; }
				}

				return minIndex;
			}


			public IEnumerable<int> NodeIndexesWithinRange (Vector3 point, float threshold)
			{
				for (int s=0; s<segments.Length+1; s++)
				{
					Vector3 npoint = s!=segments.Length ? segments[s].start.pos : segments[s-1].end.pos;

					float distSq =  (point.x-npoint.x)*(point.x-npoint.x) +
									(point.y-npoint.y)*(point.y-npoint.y) +
									(point.z-npoint.z)*(point.z-npoint.z);

					if (distSq < threshold*threshold)
						yield return s;
				}
			}

			private void AutoInverseSegments ()
			/// Automatically changes segment directions based on start/end positions
			{
				if (segments.Length == 1) return;

				if ((segments[0].start.pos - segments[1].start.pos).sqrMagnitude < 0.0001f  ||  
					(segments[0].start.pos - segments[1].end.pos).sqrMagnitude < 0.0001f )
						segments[0] = segments[0].Inverted;

				for (int s=1; s<segments.Length; s++)
				{
					if ((segments[s].end.pos - segments[s-1].end.pos).sqrMagnitude < 0.0001f)
						segments[s] = segments[s].Inverted;
				}
			}

		#endregion


	}
}