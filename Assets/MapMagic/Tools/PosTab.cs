using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools
{
	[System.Serializable]
	public class PosTab : ICloneable 
	{
//make child of Matrix2
//id is number in list

//2rect matrix
// - child of matrix
// - standard operators are map sized
// - worldRect rect, and GetWorldPoint fn

/*		[System.Serializable]
		public struct Pos
		{
			public float x;
			public float z;
			public float height;
			public float rotation;
			public float inclineX;
			public float inclineZ;
			public float size;
			public int type;
			public int id; //num to apply per-object random that does not depend of object coords. 0 if pos is null. Note: not unique because of Combine, Forest, etc
		}*/

		[System.Serializable]
		public struct Cell
		{
			public CoordRect rect;
			public Transition[] poses;  //SOMEDAY: list? (currently replacing list features)
			public int count;

			public int GetPosNum (float x, float z)
			{
				for (int i=0; i<count; i++)
				{
					//if (Vector2.FloatsEqual(poses[i].pos.x, x) && Vector2.FloatsEqual(poses[i].pos.z, z))
					float dx = poses[i].pos.x-x; if (dx < 0) dx = -dx;
					float dz = poses[i].pos.z-z; if (dz < 0) dz = -dz;

					if (dx < 0.0001f && dz < 0.0001f)
						return i;
				}
				return -1;
			}
		}

		public readonly CoordRect rect;
		public readonly Vector3 pos;
		public readonly Vector3 size;
		public Matrix2D<Cell> cells;
		public readonly int resolution; //number of cells
		public readonly Coord cellSize;

		public int totalCount = 0;
		public int idCounter = 1; //always increases, not changes if pos removed


		public PosTab (Vector3 pos, Vector3 size, int resolution)
		{
			//if rect%resolution!=0 the last cell size will be lower than usual

			this.resolution = resolution;
			this.rect = new CoordRect(Coord.Round(pos), Coord.Round(size));
			this.pos = pos;
			this.size = size;

			cells = new Matrix2D<Cell>(resolution, resolution);

			cellSize = new Coord( Mathf.CeilToInt(1f*rect.size.x/resolution), Mathf.CeilToInt(1f*rect.size.z/resolution) );

			for (int x=0; x<resolution; x++)
				for (int z=0; z<resolution; z++)
			{
				Cell cell = new Cell();
				cell.rect = new CoordRect(
					x*cellSize.x + rect.offset.x,
					z*cellSize.z + rect.offset.z,
					Mathf.Min(cellSize.x, Mathf.Max(0, rect.size.x - x*cellSize.x)),
					Mathf.Min(cellSize.z, Mathf.Max(0, rect.size.z - z*cellSize.z)) );
				cell.rect.offset.x = Mathf.Min(cell.rect.offset.x, rect.offset.x+rect.size.x);
				cell.rect.offset.z = Mathf.Min(cell.rect.offset.z, rect.offset.z+rect.size.z);
				cells[x,z] = cell;
			}
		}


		public PosTab Copy ()
		{
			PosTab copy = new PosTab(pos, size, resolution);
			for (int c=0; c<cells.arr.Length; c++)
			{
				copy.cells.arr[c].count = cells.arr[c].count;
				copy.cells.arr[c].rect = cells.arr[c].rect;

				if (cells.arr[c].poses == null) continue;
				copy.cells.arr[c].poses = new Transition[cells.arr[c].poses.Length];
				Array.Copy(cells.arr[c].poses, copy.cells.arr[c].poses, cells.arr[c].poses.Length);
			}
			copy.totalCount = totalCount;
			copy.idCounter = idCounter;
			return copy;
		}

		public object Clone () //IClonable
			{ return Copy(); }

		private Coord GetCellCoord (float x, float z, bool throwExceptions=true)
		{
			int ix = (int)((x-rect.offset.x)/cellSize.x);
			int iz = (int)((z-rect.offset.z)/cellSize.z); //no need to process negative values

			if (throwExceptions && (ix > cells.rect.size.x || iz > cells.rect.size.z)) throw new Exception("Out of cells range " + ix + "," + iz);

			return new Coord(ix, iz);
		}

		private int GetCellNum (float x, float z, bool throwExceptions=true)
		{
			int ix = (int)((x-rect.offset.x)/cellSize.x);
			int iz = (int)((z-rect.offset.z)/cellSize.z); //no need to process negative values

			if (throwExceptions && (ix > cells.rect.size.x || iz > cells.rect.size.z)) throw new Exception("Out of cells range " + ix + "," + iz);

			int n = iz*cells.rect.size.x + ix;

			if (throwExceptions && (n < 0)) throw new Exception("Could not find object at coord " + x + "," + z);

			return n;

		}


		public void Add (PosTab tab)
		/// Combines two hash sets in current
		{
			foreach (Transition trs in tab.All())
				Add(trs);
		}


		public void Add (float x, float z)
		{
			Transition trs = new Transition(x,z);
			Add(trs);
		}


		public void Add (Transition trs)
		{
			//checking in range
			if (!rect.Contains(trs.pos)) return;

			idCounter++;
			//if (idCounter > 2147000000) idCounter = 1;
			trs.id = idCounter;
			if (trs.hash == 0) //if hash not defined
				trs.hash = trs.id;
			
			int n = GetCellNum(trs.pos.x, trs.pos.z);

			//creating poses array
			if (cells.arr[n].poses == null) cells.arr[n].poses = new Transition[1];
			
			//resizing poses array
			if (cells.arr[n].poses.Length == cells.arr[n].count)
			{
				Transition[] newPoses = new Transition[cells.arr[n].count*4];
				Array.Copy(cells.arr[n].poses, newPoses, cells.arr[n].count);
				cells.arr[n].poses = newPoses;
			}

			//adding to array
			cells.arr[n].poses[ cells.arr[n].count ] = trs;

			cells.arr[n].count++;
			totalCount++;
		}

		public void Add (List<Transition> transitions)
		{
			for (int i=0; i<transitions.Count; i++)
				Add(transitions[i]);
		}


		public void Add (TransitionsList transitions)
		{
			for (int t=0; t<transitions.count; t++)
				Add(transitions.arr[t]);
		}
		

		public void Remove (int cellNum, int posNum)
		{
			//swapping given pos num with the last one
			cells.arr[cellNum].poses[posNum] = cells.arr[cellNum].poses[ cells.arr[cellNum].count-1 ];
			cells.arr[cellNum].poses[ cells.arr[cellNum].count-1 ].hash = 0;

			cells.arr[cellNum].count--;
			totalCount--;

			//shrinking array length
			if (cells.arr[cellNum].count == 0) cells.arr[cellNum].poses = null;
			else if (cells.arr[cellNum].count < cells.arr[cellNum].poses.Length / 2)
			{
				Transition[] newPoses = new Transition[cells.arr[cellNum].count];
				Array.Copy(cells.arr[cellNum].poses, newPoses, cells.arr[cellNum].count);
				cells.arr[cellNum].poses = newPoses;
			}
		}

		public void RemoveAt (float x, float z)
		{
			int c = GetCellNum(x,z);
			int n = cells.arr[c].GetPosNum(x,z);
			
			if (n!=-1) Remove(c,n);
		}

		public void Move (int cellNum, int posNum, float newX, float newZ)
		///Moves pos to the new coordinates, preserving all other data
		{
			int newCellNum = GetCellNum(newX,newZ);

			//if moving withing same cell
			if (newCellNum == cellNum)
			{
				cells.arr[cellNum].poses[posNum].pos.x = newX;
				cells.arr[cellNum].poses[posNum].pos.z = newZ;

				bool inRange =  newX >= rect.offset.x &&
					newZ >= rect.offset.z &&
					newX < rect.offset.x+rect.size.x &&
					newZ < rect.offset.z+rect.size.z;
				if (!inRange) Remove(cellNum, posNum);
				//if (!inRange) throw new Exception("Pos out of range: " + newX + "," + newZ + " rect:" + rect.ToString());
			}

			//removing from other cell and adding to new
			else
			{
				Transition trn = cells.arr[cellNum].poses[posNum];
				Remove(cellNum, posNum);

				trn.pos.x = newX;
				trn.pos.z = newZ;

				Add(trn);
			}
		}

		public void GetAndMove (float oldX, float oldZ, float newX, float newZ)
		{
			int c = GetCellNum(oldX,oldZ);
			int n = cells.arr[c].GetPosNum(oldX,oldZ);
			
			if (n < 0) throw new Exception("Could not find object at coord " + oldX + "," + oldZ + " cell num:" + c);

			Move(c,n, newX, newZ);
		}

		public bool Exists (float x, float z)
		///Checks if there an object at specified coord. Mainly for test purpose
		{
			int c = GetCellNum(x,z, throwExceptions:false);
			if (c > cells.arr.Length) return false;

			int n = cells.arr[c].GetPosNum(x,z);

			return n >= 0;
		}

		public void Flush ()
		///Removes unused array tails, reducing PosTab size (up to 4 times)
		{
			for (int c=0; c<cells.arr.Length; c++)
			{
				if (cells.arr[c].poses == null) continue;
				
				if (cells.arr[c].count == 0) cells.arr[c].poses = null;

				if (cells.arr[c].poses.Length > cells.arr[c].count)
				{
					Transition[] newPoses = new Transition[cells.arr[c].count];
					Array.Copy(cells.arr[c].poses, newPoses, cells.arr[c].count);
					cells.arr[c].poses = newPoses;
				}
			}
		}


		public Transition Closest (float x, float z, float minDist=0, float maxDist=2147000000, Predicate<Transition> filterFn=null)
		///Finds the closest Pos to given x and z in all cells. Use minDist=epsilon to exclude self.
		{
			//alternative way: keep a bool array of process cells, and increase rect each iteration (skipping processed)

			float minDistSq = maxDist*maxDist;
			Transition closestPos = new Transition() { hash=0 };

			Coord center = GetCellCoord(x,z);

			//finding cell search limit
			int maxP = (int)(maxDist / cellSize.x * 1.42f + 1);

			int cellsToBounds = center.x>center.z? center.x : center.z; //a _maximum_ distance from center to cells rect bounds
			if (cells.rect.size.x-center.x > cellsToBounds) cellsToBounds = cells.rect.size.x-center.x; 
			if (cells.rect.size.z-center.z > cellsToBounds) cellsToBounds = cells.rect.size.z-center.z; 

			if (maxP > cellsToBounds) maxP = cellsToBounds;
			maxP++;

			int minP = (int)(minDist / cellSize.x * 0.7f);

			//looking in perimeters
			for (int p=0; p<maxP; p++)
			{
				ClosestInPerimeter(ref minDistSq, ref closestPos, center, p, x,z,minDist,maxDist, filterFn);

				//if closest found at least - checking 2 perimeters more
				if (closestPos.hash != 0) 
				{
					int maxPleft = (int)( (Mathf.Sqrt(minDistSq) / cellSize.x) * 0.3f);  //0.3 is 1-0.7071
					if (maxPleft < 2) maxPleft = 2;
					
					for (int p2=0; p2<=maxPleft; p2++)
						ClosestInPerimeter(ref minDistSq, ref closestPos, center, p+p2, x,z,minDist,maxDist, filterFn);
					
					break;
				}
			}

			return closestPos;
		}


		public void ClosestInPerimeter (ref float minDistSq, ref Transition closestPos, Coord center, int perimSize, float x, float z, float minDist, float maxDist, Predicate<Transition> filterFn=null)
		///finds the closest in a rectangular (square) perimeter. Just a helper for Closest. PerDist is the perimeter size (distance from center).
		{
			if (perimSize == 0) //in current cell
			{
				Cell curCell = cells[center];
				for (int i=0; i<curCell.count; i++)
				{
					float curDistSq = (curCell.poses[i].pos.x-x)*(curCell.poses[i].pos.x-x) + (curCell.poses[i].pos.z-z)*(curCell.poses[i].pos.z-z);
					if (curDistSq<minDistSq && 
						curDistSq>=minDist*minDist && 
						(filterFn==null || filterFn(curCell.poses[i])) ) 
							{ minDistSq=curDistSq; closestPos=curCell.poses[i]; }
				}
			}

			else //in perimeter
			{
				for (int s=0; s<perimSize; s++)
					foreach (Coord c in center.DistanceStep(s,perimSize))
				{
					//checking cell in range
					if (!(c.x >= cells.rect.offset.x && c.x < cells.rect.offset.x + cells.rect.size.x &&
			        c.z >= cells.rect.offset.z && c.z < cells.rect.offset.z + cells.rect.size.z)) continue;

					Cell curCell = cells[c];
					for (int i=0; i<curCell.count; i++)
					{
						float curDistSq = (curCell.poses[i].pos.x-x)*(curCell.poses[i].pos.x-x) + (curCell.poses[i].pos.z-z)*(curCell.poses[i].pos.z-z);
						if (curDistSq<minDistSq && 
							curDistSq>=minDist*minDist &&
							(filterFn==null || filterFn(curCell.poses[i]))) 
								{ minDistSq=curDistSq; closestPos=curCell.poses[i]; }
					}
				}
			}
		}


		public Transition ClosestDebug (float x, float z, float minDist=0, float maxDist=20000000000)
		///Finds closest iterating in all cells and objects. Hust fn to test Closest.
		{
			float minDistSq = maxDist;
			Transition closestPos = new Transition() { hash=0 };

			for (int c=0; c<cells.arr.Length; c++)
			{
				Cell curCell = cells.arr[c];
				for (int i=0; i<curCell.count; i++)
				{
					float curDistSq = (curCell.poses[i].pos.x-x)*(curCell.poses[i].pos.x-x) + (curCell.poses[i].pos.z-z)*(curCell.poses[i].pos.z-z);
					if (curDistSq<minDistSq && curDistSq>=minDist*minDist) { minDistSq=curDistSq; closestPos=curCell.poses[i]; }
				}
			}

			return closestPos;
		}

		public void TwoClosest (out Transition minTrs1, out Transition minTrs2)
		/// Finds two most closest objects of all
		{
			float minDist = float.MaxValue;
			minTrs1 = default;
			minTrs2 = default;
			foreach (Transition trs in All())
			{
				Transition closest = Closest(trs.pos.x, trs.pos.z, minDist:0.0001f);
				float dist = (trs.pos - closest.pos).sqrMagnitude;
				if (dist < minDist) { minDist = dist; minTrs1 = trs; minTrs2 = closest; }
			}
			//note that if t1 has t2 as closest does NOT mean that t1 is closest for t2.
		}



		#region MapMagic functions

			public static PosTab Combine (params PosTab[] posTabs)
			//NOTE: combine ids are not unique
			{
				if (posTabs.Length==0) return null;

				PosTab any = ArrayTools.Any(posTabs);
				if (any == null) return null;
				PosTab result = new PosTab(any.pos, any.size, any.resolution);

				for (int i=0; i<posTabs.Length; i++)
				{
					PosTab posTab = posTabs[i];
					if (posTab == null) continue;

					for (int c=0; c<posTab.cells.arr.Length; c++)
					{
						Cell cell = posTab.cells.arr[c];
						for (int p=0; p<cell.count; p++)
							result.Add(cell.poses[p]);
					}
				}

				return result;
			}


		#endregion


		public IEnumerable<Transition> All ()
		{
			for (int c=0; c<cells.arr.Length; c++)
			{
				Cell cell = cells.arr[c];
				for (int i=0; i<cell.count; i++)
					yield return cell.poses[i];
			}
		}

		public Transition Any ()
		{
			for (int c=0; c<cells.arr.Length; c++)
			{
				Cell cell = cells.arr[c];
				if (cell.count != 0)
					return cell.poses[0];
			}
			return default;
		}

		public int GetCountInRect (Vector3 rectPos, Vector3 rectSize)
		{
			int count = 0;

			for (int c=0; c<cells.arr.Length; c++)
			{
				Cell cell = cells.arr[c];
				for (int i=0; i<cell.count; i++)
				{
					if (cell.poses[i].pos.x < rectPos.x || cell.poses[i].pos.x > rectPos.x+rectSize.x ||
						cell.poses[i].pos.z < rectPos.z || cell.poses[i].pos.z > rectPos.z+rectSize.z)
							continue;
					count ++;
				}
			}

			return count;
		}


		public IEnumerable<int> CellNumsInRect(Vector2 min, Vector2 max, bool inCenter=true) //obsolete?
		{
			int minX = (int)((min.x-rect.offset.x) / cellSize.x);
			int minY = (int)((min.y-rect.offset.z) / cellSize.z);
			int maxX = (int)((max.x-rect.offset.x) / cellSize.x);
			int maxY = (int)((max.y-rect.offset.z) / cellSize.z); 

			minX = Mathf.Max(0, minX); minY = Mathf.Max(0, minY);
			maxX = Mathf.Min(resolution-1, maxX); maxY = Mathf.Min(resolution-1, maxY);  

			//processing all the rect
			if (inCenter)
				for (int x=minX; x<=maxX; x++)
					for (int y=minY; y<=maxY; y++)
						yield return y*resolution + x;

			//borders only
			else 
			{
				for (int x=minX; x<=maxX; x++) { yield return minY*resolution + x; yield return maxY*resolution + x; }
				for (int y=minY; y<=maxY; y++) { yield return y*resolution + minX; yield return y*resolution + maxX; }
			}
		}

		public CoordRect CellsWithinBounds (Vector2 min, Vector2 max)
		{
			CoordRect rect = new CoordRect();
			min.x-=this.rect.offset.x;  if (min.x<0) min.x--;  rect.offset.x = (int)(float)(min.x/cellSize.x);
			min.y-=this.rect.offset.z;  if (min.y<0) min.y--; rect.offset.z = (int)(float)(min.y/cellSize.z);
			max.x-=this.rect.offset.x;  if (max.x<0) max.x--; rect.MaxX = (int)(float)(max.x/cellSize.x + 1f);
			max.y-=this.rect.offset.z;  if (max.y<0) max.y--; rect.MaxZ = (int)(float)(max.y/cellSize.z + 1f);

			rect = CoordRect.Intersected(rect, new CoordRect(0,0,resolution,resolution));

			rect.Clamp(new Coord(0,0), new Coord(resolution,resolution));  //original resolution-1

			return rect;
		}

		public void RemoveObjsInRange(float posX, float posZ, float range)
		{
			Rect rect = new Rect(posX-range, posZ-range, range*2, range*2);
			rect = CoordinatesExtensions.Intersect(rect, this.rect);

			foreach (int c in CellNumsInRect(rect.min, rect.max))
			{
				for (int p=cells.arr[c].count-1; p>=0; p--)
				{
					float distSq = (cells.arr[c].poses[p].pos.x-posX)*(cells.arr[c].poses[p].pos.x-posX) + (cells.arr[c].poses[p].pos.z-posZ)*(cells.arr[c].poses[p].pos.z-posZ);
					if (distSq < range*range) Remove(c,p);
				}
			}
		}

		public bool IsAnyObjInRange(float posX, float posZ, float range) 
		{
			Vector2 min = new Vector2(posX-range, posZ-range);
			Vector2 max = new Vector2(posX+range, posZ+range);

			//foreach (int c in CellNumsInRect(min,max)) {
			CoordRect cellsCoords = CellsWithinBounds(min,max);
			Coord cMin = cellsCoords.Min; Coord cMax = cellsCoords.Max;
			for (int cx=cMin.x; cx<cMax.x; cx++)
				for (int cz=cMin.z; cz<cMax.z; cz++)
			{
				int c = cz*resolution + cx;

				for (int p=cells.arr[c].count-1; p>=0; p--)
				{
					float distSq = (cells.arr[c].poses[p].pos.x-posX)*(cells.arr[c].poses[p].pos.x-posX) + (cells.arr[c].poses[p].pos.z-posZ)*(cells.arr[c].poses[p].pos.z-posZ);
					if (distSq < range*range) return true;
				}
			}
			return false;
		}


		public List<Transition> ToList ()
		{
			List<Transition> transitions = new List<Transition>();

			for (int c=0; c<cells.arr.Length; c++)
			{
				Cell cell = cells.arr[c];
				transitions.AddRange(cell.poses);
			}

			return transitions;
		}


		public TransitionsList ToTransitionsList ()
		{
			TransitionsList trns = new TransitionsList(); //capacity totalCount

			for (int c=0; c<cells.arr.Length; c++)
			{
				Cell cell = cells.arr[c];
				for (int i=0; i<cell.count; i++)
					trns.Add(cells.arr[c].poses[i]);
			}

			return trns;
		}
	}
}
