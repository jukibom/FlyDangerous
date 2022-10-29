using System;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools.Splines
{ 

	[System.Serializable]
	public struct Segment
	{
		public Node start;
		public Node end;

		public StructArray lengthToPercentLut; // lengthToPercentLut[normalizedLength] = percent
		public float length;
		
		public Segment (Node start, Node end)
			{ this.start = start; this.end = end; lengthToPercentLut=new StructArray(); length=0; }

		public Segment (Vector3 start, Vector3 end)
			{ this.start = new Node() {pos=start}; this.end = new Node() {pos=end}; lengthToPercentLut=new StructArray(); length=0; }

		public override string ToString() => $"Segment ({start.pos.x},{start.pos.y},{start.pos.z})-({end.pos.x},{end.pos.y},{end.pos.z}) {StartEndLength}";

		public float StartEndLength => (start.pos-end.pos).magnitude;

		#region Segment Operations

			public Vector3 GetPoint (float p)
			/// Returns non-equalized beizer curve point
			{
				float ip = 1f-p;
				return  
					ip*ip*ip*start.pos + 
					3*p*ip*ip*(start.pos+start.dir) + 
					3*p*p*ip*(end.pos+end.dir) + 
					p*p*p*end.pos;
			}


			public Vector3 GetDerivative (float p) 
			/// Finds a derivative at p, useful for quick tangent calculations
			{
				float it = 1-p;
				return 
					it*it  *	3*(start.pos+start.dir - start.pos) +
					2 * it * p *3*(end.pos+end.dir - (start.pos+start.dir)) +
					p*p  *		3*(end.pos - (end.pos+end.dir));
			}


			public (Vector3 inDir, Vector3 outDir) GetSplitTangents (float p, Vector3 point)
			/// Returns tangents as if segment was split at this percent
			/// Differs from GetDerivative in tangent length and 2x slower performance. Use for split purpose only
			/// For other tangent calculations use GetDerivative
			{
				float ip = 1-p;
				Vector3 a = start.pos + start.dir*p;
				Vector3 b = (start.pos+start.dir)*ip + (end.pos+end.dir)*p;
				Vector3 c = end.pos + end.dir*ip;

				return (
					a*ip + b*p - point, 
					c*p + b*ip - point );
			}


			public (Segment,Segment) GetSplitted (float p) 
			/// Same as Median, but adjusts start and end tangents too to preserve the spline look
			{
				float ip = 1-p;
				Vector3 a = start.pos + start.dir*p;
				Vector3 b = (start.pos+start.dir)*ip + (end.pos+end.dir)*p;
				Vector3 c = end.pos + end.dir*ip;

				Vector3 point = GetPoint(p);

				Segment prev = new Segment() { 
					start = new Node() { pos = start.pos,	dir = start.dir*p,			type = start.type },
					end = new Node() {	 pos = point,		dir = a*(1-p) + b*p-point,	type = start.type } };
				Segment next = new Segment() { 
					start = new Node() { pos = point,		dir = c*p + b*ip - point,	type = start.type },
					end = new Node() {	 pos = end.pos,		dir = end.dir*ip,			type = end.type } };

				return (prev,next);
			}


			public static Segment Join (Segment prev, Segment next)
			/// Adjusts neighbor tangents to minimize the remove effect
			{
				float beforeDist = (prev.start.pos - prev.end.pos).magnitude;
				float afterDist = (next.start.pos - next.end.pos).magnitude;
				float p = beforeDist / (beforeDist+afterDist);

				return new Segment {
					start = new Node() { pos = prev.start.pos, dir = prev.start.dir / p },
					end = new Node() {pos = next.end.pos, dir = prev.end.dir / (1-p) } };
			}


			public Vector3 GetPerpendicular (float p, Vector3 cross) 
			/// Finds a normal using reference vector
			{
				Vector3 der = GetDerivative(p);
				return Vector3.Cross(der, cross);
			}


			public Vector2D GetPerpendicular2D (float p) 
			/// Finds a normal at XZ plane by taking derivative and swapping coordinates
			/// Equivalent to GetPerpendicular(cross:Vector3.up)
			{
				Vector3 der = GetDerivative(p);
				return new Vector2D(-der.z, der.x);
			}

			
			public Vector3 ApproxMin ()
			/// Uses the node and tangent positions to evaluate AABB min
			{
				Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

				if (start.pos.x < min.x) min.x = start.pos.x;  if (start.pos.y < min.y) min.y = start.pos.y;  if (start.pos.z < min.z) min.z = start.pos.z;
				if (end.pos.x < min.x) min.x = end.pos.x;  if (end.pos.y < min.y) min.y = end.pos.y;  if (end.pos.z < min.z) min.z = end.pos.z;
				if (start.pos.x+start.dir.x < min.x) min.x = start.pos.x+start.dir.x;  if (start.pos.y+start.dir.y < min.y) min.y = start.pos.y+start.dir.y;  if (start.pos.z+start.dir.z < min.z) min.z = start.pos.z+start.dir.z;
				if (end.pos.x+end.dir.x < min.x) min.x = end.pos.x+end.dir.x;  if (end.pos.y+end.dir.y < min.y) min.y = end.pos.y+end.dir.y;  if (end.pos.z+end.dir.z < min.z) min.z = end.pos.z+end.dir.z;
			
				return min;
			}

			public Vector3 ApproxMax ()
			/// Uses the node and tangent positions to evaluate AABB max
			{
				Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

				if (start.pos.x > max.x) max.x = start.pos.x;  if (start.pos.y > max.y) max.y = start.pos.y;  if (start.pos.z > max.z) max.z = start.pos.z;
				if (end.pos.x > max.x) max.x = end.pos.x;  if (end.pos.y > max.y) max.y = end.pos.y;  if (end.pos.z > max.z) max.z = end.pos.z;
				if (start.pos.x+start.dir.x > max.x) max.x = start.pos.x+start.dir.x;  if (start.pos.y+start.dir.y > max.y) max.y = start.pos.y+start.dir.y;  if (start.pos.z+start.dir.z > max.z) max.z = start.pos.z+start.dir.z;
				if (end.pos.x+end.dir.x > max.x) max.x = end.pos.x+end.dir.x;  if (end.pos.y+end.dir.y > max.y) max.y = end.pos.y+end.dir.y;  if (end.pos.z+end.dir.z > max.z) max.z = end.pos.z+end.dir.z;
			
				return max;
			}


			public Vector3 Min ()
			{
				throw new System.NotImplementedException();
			}


			public Vector3 Max ()
			{
				throw new System.NotImplementedException();
			}


			public Segment Inverted => new Segment(end, start);


			public float IntersectRect (Vector3 pos, Vector3 size)
			/// Returns closest to start percent intersecting rect
			{
				Vector3 min = ApproxMin();
				Vector3 max = ApproxMax();
				
				float minP = float.MaxValue;

				//left
				if (min.x < pos.x  &&  max.x > pos.x)
				{
					bool IntersectFn (Vector3 point) { return point.x > pos.x; }
					float p = GetIntersectPercent(IntersectFn);
					if (p<minP) minP = p;
				}

				//right
				if (min.x < pos.x+size.x  &&  max.x > pos.x+size.x)
				{
					bool IntersectFn (Vector3 point) { return point.x < pos.x+size.x; }
					float p = GetIntersectPercent(IntersectFn);
					if (p<minP) minP = p;
				}

				//top
				if (min.z < pos.z  &&  max.z > pos.z)
				{
					bool IntersectFn (Vector3 point) { return point.z > pos.z; }
					float p = GetIntersectPercent(IntersectFn);
					if (p<minP) minP = p;
				}

				//bottom
				if (min.z < pos.z+size.z  &&  max.z > pos.z+size.z)
				{
					bool IntersectFn (Vector3 point) { return point.z < pos.z+size.z; }
					float p = GetIntersectPercent(IntersectFn);
					if (p<minP) minP = p;
				}

				return minP;
			}


			public void ClampByRect (Vector3 pos, Vector3 size)
			{

			}








			public void PushPoints (Vector3[] points, float[] ranges, bool horizontalOnly=true, float distFactor=1)
			/// Moves points away from the segment so that they lay no closer than range
			/// Changes the points array
			/// DistFactor multiplies the range to move point (for iteration push it should be less than 1)
			{
				Vector3 min = ApproxMin();
				Vector3 max = ApproxMax();

				Func<Vector3,Vector3,float> distFn;
				if (horizontalOnly) distFn = (p1,p2) => (p1.x-p2.x)*(p1.x-p2.x) + (p1.z-p2.z)*(p1.z-p2.z);
				else distFn = (p1,p2) => (p1.x-p2.x)*(p1.x-p2.x) + (p1.z-p2.z)*(p1.z-p2.z) + (p1.y-p2.y)*(p1.y-p2.y);

				for (int p=0; p<points.Length; p++)
				{
					Vector3 point = points[p];
					float range = ranges[p];

					if (min.x > point.x+range || max.x < point.x-range ||
						min.z > point.z+range || max.z < point.z-range ||
						!horizontalOnly && (min.y > point.y+range || max.y < point.y-range))
							continue;

					(float percent, float distSq) = GetClosest(distFn, point, 5, 5);

					if (distSq > range*range)
						continue;
					
					Vector3 linePoint = GetPoint(percent);
					points[p] = PushPoint(point, linePoint, range, horizontalOnly, distFactor);
				}
			}


			public bool PushStartEnd (Vector3[] points, float[] ranges, bool horizontalOnly=true, float distFactor=1)
			/// Moves segment's start and end point positions away from points
			/// Returns true if segments has been moved
			{
				bool change = false;

				for (int p=0; p<points.Length; p++)
				{
					Vector3 point = points[p];
					float range = ranges[p];

					if (start.pos.x < point.x+range && start.pos.x > point.x-range &&
						start.pos.z < point.z+range && start.pos.z > point.z-range &&
						(horizontalOnly || (start.pos.y < point.y+range && start.pos.y > point.y-range)))
						{
							float dist = (point-start.pos).sqrMagnitude;
							if (dist<range*range)
							{
								start.pos = PushPoint(start.pos, point, range, horizontalOnly, distFactor);
								change = true;
							}
						}

					if (end.pos.x < end.pos.x+range && end.pos.x > point.x-range &&
						end.pos.z < end.pos.z+range && end.pos.z > point.z-range &&
						(horizontalOnly || (end.pos.y < point.y+range && end.pos.y > point.y-range)))
						{
							float dist = (point-end.pos).sqrMagnitude;
							if (dist<range*range)
							{
								end.pos = PushPoint(end.pos, point, range, horizontalOnly, distFactor);
								change = true;
							}
						}
				}

				return change;
			}


			private Vector3 PushPoint (Vector3 point, Vector3 otherPoint, float otherRange, bool horizontalOnly=true, float distFactor=1)
			{
				Vector3 awayDelta = otherPoint-point; 
				if (horizontalOnly) awayDelta.y = 0;

				Vector3 awayDir = awayDelta.normalized; //awayDelta/lineDist
				float dist = awayDelta.magnitude;
				float distLeft = otherRange-dist;
				
				return point - awayDir*distLeft*distFactor;
			}

			

		#endregion


		#region Distance Operations

			public (float percent, float distSq) ApproximateClosest  (Vector3 point, int iterations) => ApproximateClosest(null, point, iterations);
			private (float percent, float distSq) ApproximateClosest (Func<Vector3,Vector3,float> distanceFn, Vector3 point, int iterations)
			/// Finds approximate closest position by evaluating beizer points
			{
				float minDist = float.MaxValue;
				float minPercent = 0;
				Vector3 minPoint = start.pos;

				float step = 1f / (iterations-1);

				for (int p=0; p<iterations; p++)
				{
					float curPercent = p*step;
					
					Vector3 curPoint = GetPoint(curPercent);
					float curDist = distanceFn!=null  ?  
						distanceFn(curPoint,point)  :  
						(curPoint.x-point.x)*(curPoint.x-point.x) + (curPoint.z-point.z)*(curPoint.z-point.z) + (curPoint.y-point.y)*(curPoint.y-point.y);

					if (curDist < minDist)
					{
						minDist = curDist;
						minPercent = curPercent;
						minPoint.x = curPoint.x; minPoint.y = curPoint.y; minPoint.z = curPoint.z;
					}
				}

				if (minPercent > 1) minPercent = 1;
				if (minPercent < 0) minPercent = 0;

				//return new ClosestData() { dist=minDist, point=minPoint, location=new Location(minPercent) };
				return (minPercent, minDist);
			}


			public (float percent, float distSq) RefineClosest (Vector3 point, float percent, float range, int iterations) => RefineClosest(null, point, percent, range, iterations);
			private (float percent, float distSq) RefineClosest (Func<Vector3,Vector3,float> distanceFn, Vector3 point, float percent, float range, int iterations)
			/// Adjusts closest position (percent), making it more exact. Returns new closer percent
			/// Searches within full range (not half) to left and full range right
			/// If distanceFn is not defined using point V3, disregarding point otherwise
			{
				float minDist = float.MaxValue;

				for (int i=0; i<iterations; i++)
				{
					range /= 2;

					float biggerPercent = percent + range;  if (biggerPercent > 1) biggerPercent = 1;
					Vector3 biggerPoint = GetPoint(biggerPercent);
					float biggerDist = distanceFn!=null  ?  
						distanceFn(biggerPoint,point)  :  
						(biggerPoint.x-point.x)*(biggerPoint.x-point.x) + (biggerPoint.z-point.z)*(biggerPoint.z-point.z) + (biggerPoint.y-point.y)*(biggerPoint.y-point.y);  //(point-biggerPoint).sqrMagnitude;

					float lowerPercent = percent - range; if (lowerPercent < 0) lowerPercent = 0;
					Vector3 lowerPoint = GetPoint(lowerPercent);
					float lowerDist = distanceFn!=null  ?  
						distanceFn(lowerPoint,point)  :   
						(lowerPoint.x-point.x)*(lowerPoint.x-point.x) + (lowerPoint.z-point.z)*(lowerPoint.z-point.z) + (lowerPoint.y-point.y)*(lowerPoint.y-point.y);  //(point-lowerPoint).sqrMagnitude;

					if (biggerDist < lowerDist) { percent = biggerPercent; minDist = biggerDist; }
					else { percent = lowerPercent; minDist = lowerDist; }
				}

				return (percent, minDist);
			}

			public (float percent, float distSq) GetClosest  (Vector3 point, int initialApprox=10, int recursiveApprox=10)
			{
				float percent = ApproximateClosest(point, iterations:initialApprox).percent;
				return RefineClosest(point, percent, range:1f/(initialApprox-1), iterations:recursiveApprox);
			}

			public (float percent, float distSq) GetClosest  (Func<Vector3,Vector3,float> distanceFn, Vector3 point, int initialApprox=10, int recursiveApprox=10)
			{
				float percent = ApproximateClosest(distanceFn, point, iterations:initialApprox).percent;
				return RefineClosest(distanceFn, point, percent, range:1f/(initialApprox-1), iterations:recursiveApprox);
			}

			public bool IsWithinRange (Vector3 point, float range)
			/// Used to optimize line/sys distance evaluation by disregarding obviously far segments
			/// Note that range is not squared
			{
				Vector3 min = ApproxMin();
				if (point.x+range < min.x || point.y+range < min.y || point.z+range < min.z)
					return false;

				Vector3 max = ApproxMax();
				if (point.x-range > max.x || point.y-range > max.y || point.z-range > max.z)
					return false;

				return true;
			}


			public static (float,float) ClosestDistance (Segment s1, Segment s2, int initialApprox=6, int recursiveApprox=3)
			/// Returns points (percent) in segments that are closest to each other
			{
				float initialStep = 1f / (initialApprox-1);

				float minDist = float.MaxValue;
				float minPercent1 = 0; float minPercent2 = 0;

				for (int p1=0; p1<initialApprox; p1++)
				{
					float percent1 = p1*initialStep;
					Vector3 point1 = s1.GetPoint(percent1);

					(float percent2, float dist) = s2.ApproximateClosest(point1, initialApprox);
					if (dist < minDist)
					{
						minDist = dist;
						minPercent1 = percent1;  minPercent2 = percent2;
					}
				}

				//adjusting
				float recursiveStep = initialStep;
				for (int i1=0; i1<recursiveApprox; i1++)
				{
					recursiveStep /= 2;

					float biggerPercent1 = minPercent1 + recursiveStep; if (biggerPercent1 > 1) biggerPercent1 = 1;
					Vector3 biggerPoint1 = s1.GetPoint(biggerPercent1);
					(float biggerPercent2, float biggerDist) = s2.RefineClosest(biggerPoint1, minPercent2, initialStep, recursiveApprox);

					float lowerPercent1 = minPercent1 - recursiveStep; if (lowerPercent1 < 0) lowerPercent1 = 0;
					Vector3 lowerPoint1 = s1.GetPoint(lowerPercent1);
					(float lowerPercent2, float lowerDist) = s2.RefineClosest(lowerPoint1, minPercent2, initialStep, recursiveApprox);

					if (biggerDist < lowerDist) { minPercent1 = biggerPercent1; minPercent2 = biggerPercent2; }
					else { minPercent1 = lowerPercent1; minPercent2 = lowerPercent2; }
				}

				return (minPercent1, minPercent2);
			}


			private float GetIntersectPercent (Predicate<Vector3> intersectFn, int initialApprox=10, int recursiveApprox=10)
			/// Like GetClosestPoint, but evaluating if line intersects (bool) something (plane, axis, sphere, etc). A bit faster than GetClosestPoint
			/// IDEA: use Predicate<Vector3 oldPos, Vector3 newPos> to find intersection with planes
			/// Works from start to end (returns intersection closest to start), but determines intersection both ways
			{
				bool initIntersect = intersectFn(start.pos);
				float beforeIntersectPercent = 0;

				float step = 1f / (initialApprox-1);

				for (int p=0; p<initialApprox; p++)
				{
					float percent = p*step;
					
					Vector3 point = GetPoint(percent);
					bool newIntersect = intersectFn(point);

					if (newIntersect != initIntersect)
						break;

					beforeIntersectPercent = percent;
				}

				if (beforeIntersectPercent > 1) beforeIntersectPercent = 1;
				if (beforeIntersectPercent < 0) beforeIntersectPercent = 0;

				//adjusting initial position
				for (int i=0; i<recursiveApprox; i++)
				{
					step /= 2;

					float midPercent = beforeIntersectPercent + step; if (midPercent > 1) midPercent = 1;
					Vector3 midPoint = GetPoint(midPercent);
					bool midIntersect = intersectFn(midPoint);

					if (initIntersect == midIntersect) //no intersection until midPercent
						beforeIntersectPercent = midPercent;
				}

				return beforeIntersectPercent + step; //returning percent AFTER intersection in case we will intersect three times iterational
			}

			public bool IsNearPoints (Vector3[] points, float[] ranges, bool horizontalOnly=true, float startEndProximityFactor=0) => 
				IsNearPoints(points, ranges, out float nearPercent, out int nearPointNum, horizontalOnly, startEndProximityFactor);

			public bool IsNearPoints (Vector3[] points, float[] ranges, out float nearPercent, out int nearPointNum, bool horizontalOnly=true, float startEndProximityFactor=0)
			/// Checks if segment lays within range from any of the points, and returns percent and index if it does
			/// If point is closer to start or end than range*startEndProximityFactor it will be disregarded
			{
				Vector3 min = ApproxMin();
				Vector3 max = ApproxMax();

				Func<Vector3,Vector3,float> distFn;
				if (horizontalOnly) distFn = (p1,p2) => (p1.x-p2.x)*(p1.x-p2.x) + (p1.z-p2.z)*(p1.z-p2.z);
				else distFn = (p1,p2) => (p1.x-p2.x)*(p1.x-p2.x) + (p1.z-p2.z)*(p1.z-p2.z) + (p1.y-p2.y)*(p1.y-p2.y);

				for (int p=0; p<points.Length; p++)
				{
					Vector3 point = points[p];
					float range = ranges[p];

					if (min.x > point.x+range || max.x < point.x-range ||
						min.z > point.z+range || max.z < point.z-range ||
						!horizontalOnly && (min.y > point.y+range || max.y < point.y-range))
							continue;

					if (startEndProximityFactor>0.00001f)
					{
						float maxDist = range*startEndProximityFactor * range*startEndProximityFactor;
						if ((start.pos-point).sqrMagnitude < maxDist ||
							(end.pos-point).sqrMagnitude < maxDist)
								continue;
					}

					(float percent, float distSq) = GetClosest(distFn, point, 5, 5);

					if (percent > 0.999f || percent < 0.001f)
						continue;

					if (distSq < range*range)
					{
						nearPercent = percent;
						nearPointNum = p;
						return true;
					}
				}

				nearPercent = -1;
				nearPointNum = -1;
				return false;
			}

		#endregion


		#region Length Operations

			public float WorldLengthToPercent (float worldLength) { return NormLengthToPercent(worldLength/length); }

			public float NormLengthToPercent (float normLength)
			/// Converts relative length (range 0-1) to segemnt percent
			{
				int prevMark = (int)(normLength*9);  //8 points, do not include 0 and 1 => 9 intervals
				float markPercent = (normLength - prevMark/9f)*9f;

				float prevPercent;
				float nextPercent;
				switch (prevMark)
				{
					case 0: prevPercent=0; nextPercent = lengthToPercentLut.b0/255f; break;
					case 1: prevPercent=lengthToPercentLut.b0/255f; nextPercent = lengthToPercentLut.b1/255f; break;
					case 2: prevPercent=lengthToPercentLut.b1/255f; nextPercent = lengthToPercentLut.b2/255f; break;
					case 3: prevPercent=lengthToPercentLut.b2/255f; nextPercent = lengthToPercentLut.b3/255f; break;
					case 4: prevPercent=lengthToPercentLut.b3/255f; nextPercent = lengthToPercentLut.b4/255f; break;
					case 5: prevPercent=lengthToPercentLut.b4/255f; nextPercent = lengthToPercentLut.b5/255f; break;
					case 6: prevPercent=lengthToPercentLut.b5/255f; nextPercent = lengthToPercentLut.b6/255f; break;
					case 7: prevPercent=lengthToPercentLut.b6/255f; nextPercent = lengthToPercentLut.b7/255f; break;
					case 8: prevPercent=lengthToPercentLut.b7/255f; nextPercent = 1; break;
					default: prevPercent=0; nextPercent=0; break;
				}

				return (prevPercent*(1-markPercent) + nextPercent*markPercent);
			}


			public float GetLinearDistance (float a, float b)
			/// Calculates the line distance between two points
			{
				float ia = 1f-a;
				float ib = 1f-b;

				Vector3 delta = 
					(3*a*ia*ia - 3*b*ib*ib)*(start.dir) + 
					(3*a*a*ia - 3*b*b*ib)*(end.pos+end.dir-start.pos) + 
					(a*a*a - b*b*b)*(end.pos-start.pos);

				return delta.magnitude;
			}


			public float ApproxLength (float pStart=0, float pEnd=1, int iterations=32)
			/// Splits the curve, calcs distance between points, measures the approximate length of the segment part from pStart to pEnd
			{
				float length = 0;
				float pDelta = pEnd - pStart;
				float pStep = pDelta / (iterations-1);

				float prevPercent = 0;
				for (int i=1; i<iterations; i++)
				{
					float percent = pStart + pStep*i;
					length += GetLinearDistance(prevPercent,percent); //(point - prevPoint).magnitude;
					prevPercent = percent;
				}

				return length;
			}


			public void UpdateLength (int iterations=32, float[] tmp=null)
			{
				float linearLength = (start.pos-end.pos).magnitude;
				float tangentLength = start.dir.magnitude + ((start.dir+start.pos) - (end.dir+end.pos)).magnitude + end.dir.magnitude;
				
				if (linearLength > tangentLength-tangentLength/100 && linearLength < tangentLength+tangentLength/100)
					//tangents lay approximately on the line
					FillLinearLengthLut();
				else
					FillApproxLengthLut(iterations, tmp);
			}


			public void FillApproxLengthLut (int iterations=32, float[] tmp=null)
			/// Calculates length and updates length lut
			/// Fills the distance for each length (i.e. if lengthPercents is 4 then finds dist for 0.25,0.5,075,1)
			{
				float totalLength = 0;

				//calculating lengths
				float[] lengths = tmp==null ? new float[iterations] : tmp;
				for (int p=0; p<lengths.Length; p++)
				{
					float startPercent = 1f * p / iterations;
					float endPercent = 1f * (p+1) / iterations;
					
					float length = GetLinearDistance(startPercent, endPercent);
					lengths[p] = totalLength + length;
					totalLength += length;
				}
				length = totalLength;

				//transforming all lengths to normalized
				if (totalLength >= 0)
					for (int p=0; p<lengths.Length; p++)
						lengths[p] /= totalLength;

				//filling length-to-percent lut
				for (int i=0; i<8; i++)
				{
					float normLength = (i+1) / 9f; //8 points, do not include 0 and 1, so 9 intevals

					int startNum = 0;
					for (int p=0; p<lengths.Length; p++)
						if (lengths[p]>normLength) { startNum = p-1; break; }
					int endNum = startNum+1;
					
					float stEndPercent = (normLength-lengths[startNum]) / (lengths[endNum]-lengths[startNum]);
					float segPercent = (startNum*(1-stEndPercent) + endNum*stEndPercent) / lengths.Length;
					lengthToPercentLut.SetFloat(i, segPercent);
				}
			}


			public void FillLinearLengthLut ()
			{
				for (int i=0; i<8; i++)
				{
					float percent = (i+1) / 9f;
					lengthToPercentLut.SetFloat(i, percent);
				}
			}


			public float IntegralLength (float t=1, int iterations=8)
			/// A variation of Legendre-Gauss solution from Primer (https://pomax.github.io/bezierinfo/#arclength)
			{
				float z = t / 2;
				float sum = 0;

				double[] abscissaeLutArr = abscissaeLut[iterations];
				double[] weightsLutArr = weightsLut[iterations];

				float correctedT;
				for (int i=0; i<iterations; i++) 
				{
					correctedT = z * (float)abscissaeLutArr[i] + z;

					Vector3 derivative = GetDerivative(correctedT);
					float b = Mathf.Sqrt(derivative.x*derivative.x + derivative.z*derivative.z);
					sum += (float)weightsLutArr[i] * b;
				}
		
				return z * sum;
			}


			public void FillIntegralLengthLut (float fullLength=-1, int iterations=32)
			/// Fills the distance for each length (i.e. if lengthPercents is 4 then finds dist for 0.25,0.5,075,1)
			{
				if (fullLength < 0) fullLength = IntegralLength();

				float pHalf = LengthToPercent(fullLength*0.5f, iterations:iterations);
				float p025 = LengthToPercent(fullLength*0.25f, pFrom:0, pTo:pHalf, iterations:iterations/2);
				float p075 = LengthToPercent(fullLength*0.75f, pFrom:pHalf, pTo:1, iterations:iterations/2);
				float p0125 = LengthToPercent(fullLength*0.125f, pFrom:0, pTo:p025, iterations:iterations/4);
				float p0375 = LengthToPercent(fullLength*0.375f, pFrom:p025, pTo:pHalf, iterations:iterations/4);
				float p0625 = LengthToPercent(fullLength*0.625f, pFrom:pHalf, pTo:p075, iterations:iterations/4);
				float p0875 = LengthToPercent(fullLength*0.875f, pFrom:p075, pTo:1, iterations:iterations/4);

				lengthToPercentLut.b0 = (byte)(p0125*255 + 0.5f);
				lengthToPercentLut.b1 = (byte)(p025*255 + 0.5f);
				lengthToPercentLut.b2 = (byte)(p0375*255 + 0.5f);
				lengthToPercentLut.b3 = (byte)(pHalf*255 + 0.5f);
				lengthToPercentLut.b4 = (byte)(p0625*255 + 0.5f);
				lengthToPercentLut.b5 = (byte)(p075*255 + 0.5f);
				lengthToPercentLut.b6 = (byte)(p0875*255 + 0.5f);
			}


			public float LengthToPercent (float length, float pFrom=0, float pTo=1, int iterations=8)
			/// Converts world length to segments percent without using segment length LUT. Used to fill that LUT.
			{
				float pMid = (pFrom+pTo)/2;
				float lengthMid = IntegralLength(pMid);

				if (length < lengthMid)
				{
					if (iterations <= 1) return (pFrom+pMid) /2;
					else return LengthToPercent(length, pFrom:pFrom, pTo:pMid, iterations:iterations-1);
				}

				else
				{
					if (iterations <= 1) return (pMid+pTo) /2;
					else return LengthToPercent(length, pFrom:pMid, pTo:pTo, iterations:iterations-1);
				}
			}


			// https://pomax.github.io/bezierinfo/legendre-gauss.html
			// using doubles in case higher precision will be needed

			static double[][] abscissaeLut = new double[][] {
				new double[] {},
				new double[] {},
				new double[] {-0.5773502691896257645091487805019574556476,0.5773502691896257645091487805019574556476},
				new double[] {0,-0.7745966692414833770358530799564799221665,0.7745966692414833770358530799564799221665},
				new double[] {-0.3399810435848562648026657591032446872005,0.3399810435848562648026657591032446872005,-0.8611363115940525752239464888928095050957,0.8611363115940525752239464888928095050957},
				new double[] {0,-0.5384693101056830910363144207002088049672,0.5384693101056830910363144207002088049672,-0.9061798459386639927976268782993929651256,0.9061798459386639927976268782993929651256},
				new double[] {0.6612093864662645136613995950199053470064,-0.6612093864662645136613995950199053470064,-0.2386191860831969086305017216807119354186,0.2386191860831969086305017216807119354186,-0.9324695142031520278123015544939946091347,0.9324695142031520278123015544939946091347},
				new double[] {0, 0.4058451513773971669066064120769614633473,-0.4058451513773971669066064120769614633473,-0.7415311855993944398638647732807884070741,0.7415311855993944398638647732807884070741,-0.9491079123427585245261896840478512624007,0.9491079123427585245261896840478512624007},
				new double[] {-0.1834346424956498049394761423601839806667,0.1834346424956498049394761423601839806667,-0.5255324099163289858177390491892463490419,0.5255324099163289858177390491892463490419,-0.7966664774136267395915539364758304368371,0.7966664774136267395915539364758304368371,-0.9602898564975362316835608685694729904282,0.9602898564975362316835608685694729904282},
				new double[] {0,-0.8360311073266357942994297880697348765441,0.8360311073266357942994297880697348765441,-0.9681602395076260898355762029036728700494,0.9681602395076260898355762029036728700494,-0.3242534234038089290385380146433366085719,0.3242534234038089290385380146433366085719,-0.6133714327005903973087020393414741847857,0.6133714327005903973087020393414741847857},
				new double[] {-0.1488743389816312108848260011297199846175,0.1488743389816312108848260011297199846175,-0.4333953941292471907992659431657841622000,0.4333953941292471907992659431657841622000,-0.6794095682990244062343273651148735757692,0.6794095682990244062343273651148735757692,-0.8650633666889845107320966884234930485275,0.8650633666889845107320966884234930485275,-0.9739065285171717200779640120844520534282,0.9739065285171717200779640120844520534282},
				new double[] {0,-0.2695431559523449723315319854008615246796,0.2695431559523449723315319854008615246796,-0.5190961292068118159257256694586095544802,0.5190961292068118159257256694586095544802,-0.7301520055740493240934162520311534580496,0.7301520055740493240934162520311534580496,-0.8870625997680952990751577693039272666316,0.8870625997680952990751577693039272666316,-0.9782286581460569928039380011228573907714,0.9782286581460569928039380011228573907714},
				new double[] {-0.1252334085114689154724413694638531299833,0.1252334085114689154724413694638531299833,-0.3678314989981801937526915366437175612563,0.3678314989981801937526915366437175612563,-0.5873179542866174472967024189405342803690,0.5873179542866174472967024189405342803690,-0.7699026741943046870368938332128180759849,0.7699026741943046870368938332128180759849,-0.9041172563704748566784658661190961925375,0.9041172563704748566784658661190961925375,-0.9815606342467192506905490901492808229601,0.9815606342467192506905490901492808229601},
				new double[] {0,-0.2304583159551347940655281210979888352115,0.2304583159551347940655281210979888352115,-0.4484927510364468528779128521276398678019,0.4484927510364468528779128521276398678019,-0.6423493394403402206439846069955156500716,0.6423493394403402206439846069955156500716,-0.8015780907333099127942064895828598903056,0.8015780907333099127942064895828598903056,-0.9175983992229779652065478365007195123904,0.9175983992229779652065478365007195123904,-0.9841830547185881494728294488071096110649,0.9841830547185881494728294488071096110649},
				new double[] {-0.1080549487073436620662446502198347476119,0.1080549487073436620662446502198347476119,-0.3191123689278897604356718241684754668342,0.3191123689278897604356718241684754668342,-0.5152486363581540919652907185511886623088,0.5152486363581540919652907185511886623088,-0.6872929048116854701480198030193341375384,0.6872929048116854701480198030193341375384,-0.8272013150697649931897947426503949610397,0.8272013150697649931897947426503949610397,-0.9284348836635735173363911393778742644770,0.9284348836635735173363911393778742644770,-0.9862838086968123388415972667040528016760,0.9862838086968123388415972667040528016760},
				new double[] {0,-0.2011940939974345223006283033945962078128,0.2011940939974345223006283033945962078128,-0.3941513470775633698972073709810454683627,0.3941513470775633698972073709810454683627,-0.5709721726085388475372267372539106412383,0.5709721726085388475372267372539106412383,-0.7244177313601700474161860546139380096308,0.7244177313601700474161860546139380096308,-0.8482065834104272162006483207742168513662,0.8482065834104272162006483207742168513662,-0.9372733924007059043077589477102094712439,0.9372733924007059043077589477102094712439,-0.9879925180204854284895657185866125811469,0.9879925180204854284895657185866125811469},
				new double[] {-0.0950125098376374401853193354249580631303,0.0950125098376374401853193354249580631303,-0.2816035507792589132304605014604961064860,0.2816035507792589132304605014604961064860,-0.4580167776572273863424194429835775735400,0.4580167776572273863424194429835775735400,-0.6178762444026437484466717640487910189918,0.6178762444026437484466717640487910189918,-0.7554044083550030338951011948474422683538,0.7554044083550030338951011948474422683538,-0.8656312023878317438804678977123931323873,0.8656312023878317438804678977123931323873,-0.9445750230732325760779884155346083450911,0.9445750230732325760779884155346083450911,-0.9894009349916499325961541734503326274262,0.9894009349916499325961541734503326274262},
				new double[] {0,-0.1784841814958478558506774936540655574754,0.1784841814958478558506774936540655574754,-0.3512317634538763152971855170953460050405,0.3512317634538763152971855170953460050405,-0.5126905370864769678862465686295518745829,0.5126905370864769678862465686295518745829,-0.6576711592166907658503022166430023351478,0.6576711592166907658503022166430023351478,-0.7815140038968014069252300555204760502239,0.7815140038968014069252300555204760502239,-0.8802391537269859021229556944881556926234,0.8802391537269859021229556944881556926234,-0.9506755217687677612227169578958030214433,0.9506755217687677612227169578958030214433,-0.9905754753144173356754340199406652765077,0.9905754753144173356754340199406652765077} };

			static readonly double[][] weightsLut = new double[][] {
				new double[] {},
				new double[] {},
				new double[] {1.0,1.0},
				new double[] {0.8888888888888888888888888888888888888888,0.5555555555555555555555555555555555555555,0.5555555555555555555555555555555555555555},
				new double[] {0.6521451548625461426269360507780005927646,0.6521451548625461426269360507780005927646,0.3478548451374538573730639492219994072353,0.3478548451374538573730639492219994072353},
				new double[] {0.5688888888888888888888888888888888888888,0.4786286704993664680412915148356381929122,0.4786286704993664680412915148356381929122,0.2369268850561890875142640407199173626432,0.2369268850561890875142640407199173626432},
				new double[] {0.3607615730481386075698335138377161116615,0.3607615730481386075698335138377161116615,0.4679139345726910473898703439895509948116,0.4679139345726910473898703439895509948116,0.1713244923791703450402961421727328935268,0.1713244923791703450402961421727328935268},
				new double[] {0.4179591836734693877551020408163265306122,0.3818300505051189449503697754889751338783,0.3818300505051189449503697754889751338783,0.2797053914892766679014677714237795824869,0.2797053914892766679014677714237795824869,0.1294849661688696932706114326790820183285,0.1294849661688696932706114326790820183285},
				new double[] {0.3626837833783619829651504492771956121941,0.3626837833783619829651504492771956121941,0.3137066458778872873379622019866013132603,0.3137066458778872873379622019866013132603,0.2223810344533744705443559944262408844301,0.2223810344533744705443559944262408844301,0.1012285362903762591525313543099621901153,0.1012285362903762591525313543099621901153},
				new double[] {0.3302393550012597631645250692869740488788,0.1806481606948574040584720312429128095143,0.1806481606948574040584720312429128095143,0.0812743883615744119718921581105236506756,0.0812743883615744119718921581105236506756,0.3123470770400028400686304065844436655987,0.3123470770400028400686304065844436655987,0.2606106964029354623187428694186328497718,0.2606106964029354623187428694186328497718},
				new double[] {0.2955242247147528701738929946513383294210,0.2955242247147528701738929946513383294210,0.2692667193099963550912269215694693528597,0.2692667193099963550912269215694693528597,0.2190863625159820439955349342281631924587,0.2190863625159820439955349342281631924587,0.1494513491505805931457763396576973324025,0.1494513491505805931457763396576973324025,0.0666713443086881375935688098933317928578,0.0666713443086881375935688098933317928578},
				new double[] {0.2729250867779006307144835283363421891560,0.2628045445102466621806888698905091953727,0.2628045445102466621806888698905091953727,0.2331937645919904799185237048431751394317,0.2331937645919904799185237048431751394317,0.1862902109277342514260976414316558916912,0.1862902109277342514260976414316558916912,0.1255803694649046246346942992239401001976,0.1255803694649046246346942992239401001976,0.0556685671161736664827537204425485787285,0.0556685671161736664827537204425485787285},
				new double[] {0.2491470458134027850005624360429512108304,0.2491470458134027850005624360429512108304,0.2334925365383548087608498989248780562594,0.2334925365383548087608498989248780562594,0.2031674267230659217490644558097983765065,0.2031674267230659217490644558097983765065,0.1600783285433462263346525295433590718720,0.1600783285433462263346525295433590718720,0.1069393259953184309602547181939962242145,0.1069393259953184309602547181939962242145,0.0471753363865118271946159614850170603170,0.0471753363865118271946159614850170603170},
				new double[] {0.2325515532308739101945895152688359481566,0.2262831802628972384120901860397766184347,0.2262831802628972384120901860397766184347,0.2078160475368885023125232193060527633865,0.2078160475368885023125232193060527633865,0.1781459807619457382800466919960979955128,0.1781459807619457382800466919960979955128,0.1388735102197872384636017768688714676218,0.1388735102197872384636017768688714676218,0.0921214998377284479144217759537971209236,0.0921214998377284479144217759537971209236,0.0404840047653158795200215922009860600419,0.0404840047653158795200215922009860600419},
				new double[] {0.2152638534631577901958764433162600352749,0.2152638534631577901958764433162600352749,0.2051984637212956039659240656612180557103,0.2051984637212956039659240656612180557103,0.1855383974779378137417165901251570362489,0.1855383974779378137417165901251570362489,0.1572031671581935345696019386238421566056,0.1572031671581935345696019386238421566056,0.1215185706879031846894148090724766259566,0.1215185706879031846894148090724766259566,0.0801580871597602098056332770628543095836,0.0801580871597602098056332770628543095836,0.0351194603317518630318328761381917806197,0.0351194603317518630318328761381917806197},
				new double[] {0.2025782419255612728806201999675193148386,0.1984314853271115764561183264438393248186,0.1984314853271115764561183264438393248186,0.1861610000155622110268005618664228245062,0.1861610000155622110268005618664228245062,0.1662692058169939335532008604812088111309,0.1662692058169939335532008604812088111309,0.1395706779261543144478047945110283225208,0.1395706779261543144478047945110283225208,0.1071592204671719350118695466858693034155,0.1071592204671719350118695466858693034155,0.0703660474881081247092674164506673384667,0.0703660474881081247092674164506673384667,0.0307532419961172683546283935772044177217,0.0307532419961172683546283935772044177217},
				new double[] {0.1894506104550684962853967232082831051469,0.1894506104550684962853967232082831051469,0.1826034150449235888667636679692199393835,0.1826034150449235888667636679692199393835,0.1691565193950025381893120790303599622116,0.1691565193950025381893120790303599622116,0.1495959888165767320815017305474785489704,0.1495959888165767320815017305474785489704,0.1246289712555338720524762821920164201448,0.1246289712555338720524762821920164201448,0.0951585116824927848099251076022462263552,0.0951585116824927848099251076022462263552,0.0622535239386478928628438369943776942749,0.0622535239386478928628438369943776942749,0.0271524594117540948517805724560181035122,0.0271524594117540948517805724560181035122},
				new double[] {0.1794464703562065254582656442618856214487,0.1765627053669926463252709901131972391509,0.1765627053669926463252709901131972391509,0.1680041021564500445099706637883231550211,0.1680041021564500445099706637883231550211,0.1540457610768102880814315948019586119404,0.1540457610768102880814315948019586119404,0.1351363684685254732863199817023501973721,0.1351363684685254732863199817023501973721,0.1118838471934039710947883856263559267358,0.1118838471934039710947883856263559267358,0.0850361483171791808835353701910620738504,0.0850361483171791808835353701910620738504,0.0554595293739872011294401653582446605128,0.0554595293739872011294401653582446605128,0.0241483028685479319601100262875653246916,0.0241483028685479319601100262875653246916} };

		#endregion


	}


}
