using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; //to copy properties

namespace Den.Tools
{
	static public class CoordinatesExtensions
	{
		public static bool InRange (this Rect rect, Vector2 pos) 
		{ 
			return (rect.center - pos).sqrMagnitude < (rect.width/2f)*(rect.width/2f); 
			//return rect.Contains(pos);
		}

		public static Vector3 ToDir (this float angle) { return new Vector3( Mathf.Sin(angle*Mathf.Deg2Rad), 0, Mathf.Cos(angle*Mathf.Deg2Rad) ); }
		public static float ToAngle (this Vector3 dir) { return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; }

		public static Vector3 Mul (this Vector3 v, Vector3 m) { return new Vector3(v.x*=m.x, v.y*=m.y, v.z*=m.z); }
		public static Vector3 Div (this Vector3 v, Vector3 m) { return new Vector3(v.x/=m.x, v.y/=m.y, v.z/=m.z); }

		public static Vector3 V3 (this Vector2 v2) { return new Vector3(v2.x, 0, v2.y); }
		public static Vector2 V2 (this Vector3 v3) { return new Vector2(v3.x, v3.z); }
		public static Vector3 ToV3 (this float f) { return new Vector3(f,f, f); }
		public static Vector4 ToV4 (this Rect r) { return new Vector4(r.x, r.y, r.width, r.height); }

		public static Quaternion EulerToQuat (this Vector3 v) { Quaternion rotation = Quaternion.identity; rotation.eulerAngles = v; return rotation; }
		public static Quaternion EulerToQuat (this float f) { Quaternion rotation = Quaternion.identity; rotation.eulerAngles = new Vector3(0,f,0); return rotation; }

		public static Vector3 Direction (this float angle) { return new Vector3( Mathf.Sin(angle*Mathf.Deg2Rad), 0, Mathf.Cos(angle*Mathf.Deg2Rad) ); }
		public static float Angle (this Vector3 dir) { return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; }

		public static Rect Clamp (this Rect r, float p) { return new Rect(r.x, r.y, r.width*p, r.height); }
		public static Rect ClampFromLeft (this Rect r, float p) { return new Rect(r.x+r.width*(1f-p), r.y, r.width*p, r.height); }
		public static Rect Clamp (this Rect r, int p) { return new Rect(r.x, r.y, p, r.height); }
		public static Rect ClampFromLeft (this Rect r, int p) { return new Rect(r.x+(r.width-p), r.y, p, r.height); }

		public static Vector3 Pow (this Vector3 v, float pow) => new Vector3( Mathf.Pow(v.x,pow), Mathf.Pow(v.y,pow), Mathf.Pow(v.z,pow) );

		public static Rect Intersect (Rect r1, Rect r2) 
		{ 
			Rect result = new Rect(0,0,0,0);

			result.x = Mathf.Max(r1.x, r2.x);
			result.y = Mathf.Max(r1.y, r2.y);

			result.max = new Vector2(
				Mathf.Min(r1.max.x, r2.max.x),
				Mathf.Min(r1.max.y, r2.max.y) );
			
			if (result.size.x<0) result.size = new Vector2(0, result.size.y);
			if (result.size.y<0) result.size = new Vector2(result.size.y, 0);

			return result;
		}

		public static Rect Intersect (Rect r1, CoordRect r2) 
		{ 
			Rect result = new Rect(0,0,0,0);

			result.x = Mathf.Max(r1.x, r2.offset.x);
			result.y = Mathf.Max(r1.y, r2.offset.z);

			result.max = new Vector2(
				Mathf.Min(r1.max.x, r2.offset.x+r2.size.x),
				Mathf.Min(r1.max.y, r2.offset.z+r2.size.z) );
			
			if (result.size.x<0) result.size = new Vector2(0, result.size.y);
			if (result.size.y<0) result.size = new Vector2(result.size.y, 0);

			return result;
		}


		public static Rect ToRect(this Vector3 center, float range) { return new Rect(center.x-range, center.z-range, range*2, range*2); }

		public static Vector3 Average (this Vector3[] vecs) { Vector3 result = Vector3.zero; for (int i=0; i<vecs.Length; i++) result+=vecs[i]; return result / vecs.Length; }

		public static bool Intersects (this Rect r1, Rect r2)
		{
			Vector2 r1min = r1.min; Vector2 r1max = r1.max;
			Vector2 r2min = r2.min; Vector2 r2max = r2.max;
			if (r2max.x < r1min.x || r2min.x > r1max.x || r2max.y < r1min.y || r2min.y > r1max.y) return false;
			else return true;
		}

		public static bool Intersects (this Rect r1,  Vector2 pos, Vector2 size)
		{
			Vector2 r1min = r1.min; Vector2 r1max = r1.max;
			Vector2 r2min = pos; Vector2 r2max = pos+size;
			if (r2max.x < r1min.x || r2min.x > r1max.x || r2max.y < r1min.y || r2min.y > r1max.y) return false;
			else return true;
		}

		public static bool Intersects (this Rect r1, Rect[] rects)
		{
			for (int i=0; i<rects.Length; i++) 
				if (r1.Intersects(rects[i])) return true;
			return false;
		}

		public static bool Contains (this Rect r1, Rect r2)
		{
			Vector2 r1min = r1.min; Vector2 r1max = r1.max;
			Vector2 r2min = r2.min; Vector2 r2max = r2.max;
			if (r2min.x > r1min.x && r2max.x < r1max.x && r2min.y > r1min.y && r2max.y < r1max.y) return true;
			else return false;
		}

		public static bool Contains (this Rect r, Vector2 pos, Vector2 size)
		{
			Vector2 r1min = r.min; Vector2 r1max = r.max;
			Vector2 r2min = pos; Vector2 r2max = pos+size;
			if (r2min.x > r1min.x && r2max.x < r1max.x && r2min.y > r1min.y && r2max.y < r1max.y) return true;
			else return false;
		}

		public static bool ContainsOrIntersects (this Rect r1, Rect r2)
		{
			Vector2 r1min = r1.min; Vector2 r1max = r1.max;
			Vector2 r2min = r2.min; Vector2 r2max = r2.max;
			if (r2min.x>r1max.x || r2min.y>r1max.y || r2max.x<r1min.x || r2max.y<r1min.y) return false;
			else return true;
		}

		public static bool ContainsOrIntersects (this Rect r1, Rect r2, float padding)
		{
			Vector2 r1min = r1.min; Vector2 r1max = r1.max;
			Vector2 r2min = r2.min; Vector2 r2max = r2.max;
			if (r2min.x>r1max.x+padding || r2min.y>r1max.y+padding || r2max.x<r1min.x-padding || r2max.y<r1min.y-padding) return false;
			else return true;
		}

		public static Rect Extended (this Rect r, float f) { return new Rect(r.x-f, r.y-f, r.width+f*2, r.height+f*2); }

		public static Rect Extended (this Rect rect, float r, float l, float t, float b) { return new Rect(rect.x-l, rect.y-t, rect.width+r+l, rect.height+t+b); }

		public static Rect Encapsulate (this Rect r, Rect n)
		{
			if (n.xMin < r.xMin) r.xMin = n.xMin;
			if (n.yMin < r.yMin) r.yMin = n.yMin;
			if (n.xMax > r.xMax) r.xMax = n.xMax;
			if (n.yMax > r.yMax) r.yMax = n.yMax;
			return r;
		}

		public static Rect TurnNonNegative (this Rect r)
		{
			float width = r.width;
			if (width < 0) { r.width = -width; r.x += width; }

			float height = r.height;
			if (height < 0) { r.height = -height; r.y += height; }

			return r;
		}


		public static float DistToRectCenter (this Vector3 pos, float offsetX, float offsetZ, float size)
		{
			float deltaX = pos.x - (offsetX+size/2); float deltaZ = pos.z - (offsetZ+size/2); float dist = deltaX*deltaX + deltaZ*deltaZ;
			return Mathf.Sqrt(dist);
		}

		public static float DistToRectAxisAligned (this Vector3 pos, float offsetX, float offsetZ, float size) //NOT manhattan dist. offset and size are instead of UnityEngine.Rect
		{
			//finding x distance
			float distPosX = offsetX - pos.x;
			float distNegX = pos.x - offsetX - size;
			
			float distX;
			if (distPosX >= 0) distX = distPosX;
			else if (distNegX >= 0) distX = distNegX;
			else distX = 0;

			//finding z distance
			float distPosZ = offsetZ - pos.z;
			float distNegZ = pos.z - offsetZ - size;
			
			float distZ;
			if (distPosZ >= 0) distZ = distPosZ;
			else if (distNegZ >= 0) distZ = distNegZ;
			else distZ = 0;

			//returning the maximum(!) distance 
			if (distX > distZ) return distX;
			else return distZ;
		}



		public static float DistToRectCenter (this Vector3[] poses, float offsetX, float offsetZ, float size)
		{
			float minDist = 200000000;
			for (int p=0; p<poses.Length; p++)
			{
				float dist = poses[p].DistToRectCenter(offsetX, offsetZ, size);
				if (dist < minDist) minDist = dist;
			}
			return minDist;
		}

		public static float DistToRectAxisAligned (this Vector3[] poses, float offsetX, float offsetZ, float size)
		{
			float minDist = 200000000;
			for (int p=0; p<poses.Length; p++)
			{
				float dist = poses[p].DistToRectAxisAligned(offsetX, offsetZ, size);
				if (dist < minDist) minDist = dist;
			}
			return minDist;
		}

		public static float DistAxisAligned (this Vector3 center, Vector3 pos)
		{
			float distX = center.x - pos.x; if (distX<0) distX = -distX;
			float distZ = center.z - pos.z; if (distZ<0) distZ = -distZ;

			//returning the maximum(!) distance 
			if (distX > distZ) return distX;
			else return distZ;
		}



		/*public static Coord ToCoord (this Vector3 pos, float cellSize, bool ceil=false) //to use in object grid
		{
			if (!ceil) return new Coord(
				Mathf.FloorToInt((pos.x) / cellSize),
				Mathf.FloorToInt((pos.z) / cellSize) ); 
			else return new Coord(
				Mathf.CeilToInt((pos.x) / cellSize),
				Mathf.CeilToInt((pos.z) / cellSize) ); 
		}*/

		[Obsolete("Use Coord.Round(v)")] public static Coord RoundToCoord (this Vector2 pos) //to use in spatial hash (when sphash and matrix sizes are equal)
		{
			int posX = (int)(pos.x + 0.5f); if (pos.x < 0) posX--; //snippet for RoundToInt
			int posZ = (int)(pos.y + 0.5f); if (pos.y < 0) posZ--;
			return new Coord(posX, posZ);
		}

		[Obsolete("Use Coord.Floor(v/cellSize)")] public static Coord FloorToCoord (this Vector3 pos, float cellSize) { return new Coord( Mathf.FloorToInt(pos.x / cellSize),		Mathf.FloorToInt(pos.z / cellSize)  ); }
		[Obsolete("Use Coord.Ceil(v/cellSize)")] public static Coord CeilToCoord (this Vector3 pos, float cellSize) { return new Coord( Mathf.CeilToInt(pos.x / cellSize),		Mathf.CeilToInt(pos.z / cellSize)  ); }
		[Obsolete("Use Coord.Round(v/cellSize)")] public static Coord RoundToCoord (this Vector3 pos, float cellSize) { return new Coord( Mathf.RoundToInt(pos.x / cellSize),		Mathf.RoundToInt(pos.z / cellSize)  ); }
		public static CoordRect ToCoordRect (this Vector3 pos, float range, float cellSize) //this one works with Terrain Sculptor
		{
			Coord min = new Coord( Mathf.FloorToInt((pos.x-range)/cellSize),	Mathf.FloorToInt((pos.z-range)/cellSize)  );
			Coord max = new Coord( Mathf.FloorToInt((pos.x+range)/cellSize),	Mathf.FloorToInt((pos.z+range)/cellSize)  )  +  1;
			return new CoordRect(min, max-min);
		}

		public static CoordRect GetHeightRect (this Terrain terrain) 
		{
			float pixelSize = terrain.terrainData.size.x / terrain.terrainData.heightmapResolution;

			int posX = (int)(terrain.transform.localPosition.x/pixelSize + 0.5f); if (terrain.transform.localPosition.x < 0) posX--;
			int posZ = (int)(terrain.transform.localPosition.z/pixelSize + 0.5f); if (terrain.transform.localPosition.z < 0) posZ--;

			return new CoordRect(posX, posZ, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
		}

		public static Rect GetWorldRect (this Terrain terrain)
		{
			return new Rect(
				terrain.transform.position.x, 
				terrain.transform.position.z, 
				terrain.terrainData.size.x, 
				terrain.terrainData.size.z);
		}

		public static float[,] SafeGetHeights (this TerrainData data, int offsetX, int offsetZ, int sizeX, int sizeZ)
		{
			if (offsetX<0) { sizeX += offsetX; offsetX=0; } if (offsetZ<0) { sizeZ += offsetZ; offsetZ=0; } //Not Tested!
			int res = data.heightmapResolution;
			if (sizeX+offsetX > res) sizeX = res-offsetX; if (sizeZ+offsetZ > res) sizeZ = res-offsetZ;
			return data.GetHeights(offsetX, offsetZ, sizeX, sizeZ);
		}

		public static float[,,] SafeGetAlphamaps (this TerrainData data, int offsetX, int offsetZ, int sizeX, int sizeZ)
		{
			if (offsetX<0) { sizeX += offsetX; offsetX=0; } if (offsetZ<0) { sizeZ += offsetZ; offsetZ=0; } //Not Tested!
			int res = data.alphamapResolution;
			if (sizeX+offsetX > res) sizeX = res-offsetX; if (sizeZ+offsetZ > res) sizeZ = res-offsetZ;
			return data.GetAlphamaps(offsetX, offsetZ, sizeX, sizeZ);
		}

		public static float GetInterpolated (this float[,] array, float x, float z)
		/// Gets value in-between pixels using linear interpolation. X and Z are swapped.
		{
			//neig coords
			int px = (int)x; if (x<0) px--; //because (int)-2.5 gives -2, should be -3 
			int nx = px+1;

			int pz = (int)z; if (z<0) pz--; 
			int nz = pz+1;

			//reading values
			float val_pxpz = array[px, pz];  //x and z swapped
			float val_nxpz = array[px, nz];
			float val_pxnz = array[nx, pz];
			float val_nxnz = array[nx, nz];

			float percentX = x-px;
			float percentZ = z-pz;

			float val_fz = val_pxpz*(1-percentX) + val_nxpz*percentX;
			float val_cz = val_pxnz*(1-percentX) + val_nxnz*percentX;
			float val = val_fz*(1-percentZ) + val_cz*percentZ;

			return val;
		}





		public static bool Equal (Vector3 v1, Vector3 v2)
		{
			return Mathf.Approximately(v1.x, v2.x) && 
					Mathf.Approximately(v1.y, v2.y) && 
					Mathf.Approximately(v1.z, v2.z);
		}
		
		public static bool Equal (Ray r1, Ray r2)
		{
			return Equal(r1.origin, r2.origin) && Equal(r1.direction, r2.direction);
		}


		public static Vector3[] InverseTransformPoint (this Transform tfm, Vector3[] points)
		{
			for (int c=0; c<points.Length; c++) points[c] = tfm.InverseTransformPoint(points[c]);
			return points;
		}

		public static Vector3 GetCenter (this Vector3[] poses)
		{
			if (poses.Length == 0) return new Vector3();
			if (poses.Length == 1) return poses[0];

			float x=0; float y=0; float z=0;
			for (int i=0; i<poses.Length; i++)
			{
				x+=poses[i].x;
				y+=poses[i].y;
				z+=poses[i].z;
			}
			return new Vector3(x/poses.Length, y/poses.Length, z/poses.Length);
		}

		public static bool Approximately (Rect r1, Rect r2)
		{
			return  Mathf.Approximately(r1.x, r2.x) &&
					Mathf.Approximately(r1.y, r2.y) &&
					Mathf.Approximately(r1.width, r2.width) &&
					Mathf.Approximately(r1.height, r2.height);
		}

		public static IEnumerable<Vector3> CircleAround (this Vector3 center, float radius, int numPoints, bool endWhereStart=false)
		{
			float radianStep = 2*Mathf.PI / numPoints;
			if (endWhereStart) numPoints++;
			for (int i=0; i<numPoints; i++)
			{
				float angle = i*radianStep;
				Vector3 dir = new Vector3( Mathf.Sin(angle), 0, Mathf.Cos(angle) );
				yield return center + dir*radius;
			}
		}

		public static bool IntersectsLine (this Rect rect, Vector2 from, Vector2 to)
		{
			Vector2 rectMin = rect.position;
			Vector2 rectMax = rect.max;

			//if line is absolutely out of the rect
			if ((from.x < rectMin.x  &&  to.x < rectMin.x)  ||
				(from.x > rectMax.x  &&  to.x > rectMax.x)  ||
				(from.y < rectMin.y  &&  to.y < rectMin.y)  ||
				(from.y > rectMax.y  &&  to.y > rectMax.y))
					return false;

			//if line is absolutely within rect (combined with abs out)
			if ((from.x < rectMax.x  &&  to.x < rectMax.x)  ||
				(from.x > rectMin.x  &&  to.x > rectMin.x)  ||
				(from.y < rectMax.y  &&  to.y < rectMax.y)  ||
				(from.y > rectMin.y  &&  to.y > rectMin.y))
					return true;

			Vector2 rectCorner3 = new Vector2(rectMin.x, rectMax.y);
			Vector2 rectCorner4 = new Vector2(rectMax.x, rectMin.y);

			bool hand1 = rectMin.Handness(from,to) > 0;
			bool hand2 = rectMax.Handness(from,to) > 0;
			bool hand3 = rectCorner3.Handness(from,to) > 0;
			bool hand4 = rectCorner4.Handness(from,to) > 0;

			if (hand1 && hand2 && hand3 && hand4) return false;
			if (!hand1 && !hand2 && !hand3 && !hand4) return false;

			return true;
		}

		public static float Handness (this Vector2 point, Vector2 from, Vector2 to)
		/// Determines whether point lays on left or on right of the line
		{
			return (point.x-from.x)*(to.y-from.y) - (point.y-from.y)*(to.x-from.x);
		}


		public static float DistanceToLine (this Vector3 point, Vector3 lineStart, Vector3 lineEnd)
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

		public static void ClampLine (this Rect rect, ref Vector2 from, ref Vector2 to, bool checkBothWays=true)
		/// Cuts the line, leaving only the segment which is within the rect
		/// If checkBothWays disabled just cutting out from-to-rect segment, leaving rect-to-to
		{
			Vector2 dir = from-to;

			Vector2 rectMin = rect.position;
			Vector2 rectMax = rect.max;

			if (from.x < rectMin.x)
			{
				float ratio = (rectMin.x-from.x) / (to.x-from.x); //the number of times big triangle smaller
				from.y = (to.y-from.y) * ratio + from.y;
				from.x = rectMin.x;
			}

			if (from.x > rectMax.x)
			{
				float ratio = (rectMax.x-from.x) / (to.x-from.x); 
				from.y = (to.y-from.y) * ratio + from.y;
				from.x = rectMax.x;
			}

			if (from.y < rectMin.y)
			{
				float ratio = (rectMin.y-from.y) / (to.y-from.y); 
				from.x = (to.x-from.x) * ratio + from.x;
				from.y = rectMin.y;
			}

			if (from.y > rectMax.y)
			{
				float ratio = (rectMax.y-from.y) / (to.y-from.y); 
				from.x = (to.x-from.x) * ratio + from.x;
				from.y = rectMax.y;
			}

			if (checkBothWays)
				ClampLine(rect, ref to, ref from, checkBothWays=false);
		}
	}
}