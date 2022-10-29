using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Den.Tools.Matrices;

namespace Den.Tools
{
	[System.Serializable]
	public class PositionMatrix : Matrix2D<Vector3>
	/// A Matrix of Vector3 that always has an origin at the center of coordinates
	/// WorldRect just intersects the real rect. Rect never starts at worldPos, except 0 coordinate
	{
		public Vector3 worldPos;
		public Vector3 worldSize;
		public float cellSize;
		public int margins; //number of cells rect expanded, bounds arount world rect

		public PositionMatrix (CoordRect rect, Vector3 worldPos, Vector3 worldSize)
		{
			this.rect = rect;
			this.worldPos = worldPos;
			this.worldSize = worldSize;
			this.cellSize = worldSize.x / rect.size.x;

			count = rect.size.x*rect.size.z;
			arr = new Vector3[count];
		}


		public Coord GetCoord (Vector3 worldPos)
		/// Returns the cell that contains worldPos
		{
			int x = (int)(float)(worldPos.x/cellSize); if (worldPos.x<0) x--;
			int z = (int)(float)(worldPos.z/cellSize); if (worldPos.z<0) z--;
			return new Coord(x,z);
		}

		public void SetPosition (Vector3 worldPos)
		{
			//GetCoord(worldPos)
			int x = (int)(float)(worldPos.x/cellSize); if (worldPos.x<0) x--;
			int z = (int)(float)(worldPos.z/cellSize); if (worldPos.z<0) z--;
			
			//return this[x,z];
			arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x] = worldPos;
		}



		public void SetHeight (int x, int z, float height)
		{
			//this[x,z].y = height;
			arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x].y = height;
		}


		public float GetHeight (int x, int z)
		{
			//return this[x,z].y;
			return arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x].y;
		}


		public void Scatter (float uniformity, Noise rnd, float maxHeight=1)
		/// use maxHeight = 0 if want to ignore scattering y
		{
			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					Vector3 pos = new Vector3(x*cellSize + cellSize/2, 0, z*cellSize + cellSize/2); //cell center
					if (uniformity < 1)
					{
						Vector3 rndPos = new Vector3(
							x*cellSize  +  rnd.Random(x,z,0)*cellSize,
							rnd.Random(x,z,2) * maxHeight,
							z*cellSize  +  rnd.Random(x,z,1)*cellSize);
						pos = pos*uniformity + rndPos*(1-uniformity);
					}

					this[x,z] = pos;
				}
		}


		public PositionMatrix Relaxed (float strength=1)
		/// Trying to increase the distance if two objects are too close to each other
		{
			float relStrength = strength*cellSize;
			PositionMatrix newMatrix = new PositionMatrix(rect, worldPos, worldSize);

			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				Vector3 pos = this[x,z];
				Vector3 relaxVec = new Vector3();

				for (int ix=-1; ix<=1; ix++)
					for (int iz=-1; iz<=1; iz++)
				{
					if (ix==0 && iz==0) continue;
					int nx = x+ix; int nz=z+iz;
					if (nx<min.x || nx>=max.x || nz<min.z || nz>=max.z) continue;

					Vector3 npos = arr[(nz-rect.offset.z)*rect.size.x + nx - rect.offset.x]; //this[nx,nz];

					Vector3 relaxDir = pos - npos;
					relaxVec += relaxDir.normalized * (1/relaxDir.sqrMagnitude);
				}

				pos += relaxVec*relStrength;

				//clamping within cell
				if (pos.x < x*cellSize) pos.x = x*cellSize;
				if (pos.x > (x+1)*cellSize) pos.x = (x+1)*cellSize;
				if (pos.z < z*cellSize) pos.z = z*cellSize;
				if (pos.z > (z+1)*cellSize) pos.z = (z+1)*cellSize;

				newMatrix[x,z] = pos;
			}

			return newMatrix;
		}


		public void CleanUp (Matrix probMatrix, Noise rnd)
		/// Erasing objects according to probability matrix
		/// Erasing means setting Y coord to negative infinity
		/// This and probMatrix rects are combined (projected onto each other)
		{
			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				int i = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;

				Vector3 pos = arr[i];
				float relX = (pos.x-worldPos.x) / worldSize.x;
				float relZ = (pos.z-worldPos.z) / worldSize.z;

				float probX = (relX * probMatrix.rect.size.x) + probMatrix.rect.offset.x;
				float probZ = (relZ * probMatrix.rect.size.z) + probMatrix.rect.offset.z;

				if (probX < probMatrix.rect.offset.x) probX = probMatrix.rect.offset.x;
				if (probZ < probMatrix.rect.offset.z) probZ = probMatrix.rect.offset.z;
				if (probX >= probMatrix.rect.offset.x+probMatrix.rect.size.x-1) probX = probMatrix.rect.offset.x+probMatrix.rect.size.x-1;
				if (probZ >= probMatrix.rect.offset.z+probMatrix.rect.size.z-1) probZ = probMatrix.rect.offset.z+probMatrix.rect.size.z-1;

				//float probVal = probMatrix.GetInterpolated(probX, probZ); //has a 0.5-pixel offset (oddly enough)
				float probVal = probMatrix[(int)probX, (int)probZ];
				float rndVal = rnd.Random(x,z,0);
				if (probVal < rndVal) arr[i].y = Mathf.NegativeInfinity;
			}
		}


		public void GetTwoClosest (Vector3 worldPos, out Vector3 closest, out Vector3 secondClosest, out float minDist, out float secondMinDist)
		{
			//GetCoord(worldPos)
			int x = (int)(float)(worldPos.x/cellSize); if (worldPos.x<0) x--;
			int z = (int)(float)(worldPos.z/cellSize); if (worldPos.z<0) z--;

			Vector3 point = arr[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; //this[x,z];

			closest = secondClosest = point; //to avoid using unassigned
			minDist = secondMinDist = 200000000;

			for (int ix=-1; ix<=1; ix++)
				for (int iz=-1; iz<=1; iz++)
			{
				//if (ix==0 && iz==0) continue;

				int nx = x+ix; int nz=z+iz;

				//if (!rect.CheckInRange(nx,nz)) continue;
				if (nx<rect.offset.x || nx>=rect.offset.x+rect.size.x ||
					nz<rect.offset.z || nz>=rect.offset.z+rect.size.z) continue;

				Vector3 nPoint = arr[(nz-rect.offset.z)*rect.size.x + nx - rect.offset.x]; //this[nx, nz];

				float dist = (worldPos.x-nPoint.x)*(worldPos.x-nPoint.x) + (worldPos.z-nPoint.z)*(worldPos.z-nPoint.z);
				if (dist<minDist) 
				{ 
					secondMinDist = minDist; minDist = dist; 
					secondClosest = closest; closest = nPoint;
				}
				else if (dist<secondMinDist) 
				{
					secondMinDist = dist; 
					secondClosest = nPoint;
				}
			}
		}


		public void FillPosTab (PosTab posTab, float minHeight=-200000000)
		/// Copy all of the positions to posTab, skipping objects that out of range and those who have height below minHeight
		{
			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					Vector3 pos = this[x,z];

					//skipping out of cell 
					if (pos.x < x*cellSize || pos.x > (x+1)*cellSize ||
						pos.z < z*cellSize || pos.z > (z+1)*cellSize) continue;

					//skipping out of range
					if (pos.x < posTab.pos.x || pos.x > posTab.pos.x+posTab.size.x ||
						pos.z < posTab.pos.z || pos.z > posTab.pos.z+posTab.size.z) continue;

					//skipping height
					if (pos.y<minHeight) continue;

					Transition trs = new Transition(pos.x, pos.z);
					trs.hash = x*2000 + z; //to make hash independent from grid size
					posTab.Add(trs);
				}
		}


		public Vector3[] ToArray ()
		{
			Vector3[] arr = new Vector3[rect.size.x * rect.size.z];

			int t = 0;
			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					arr[t] = this[x,z];
					t++;
				}

			return arr;
		}


		public void AddTransitionsList (TransitionsList trns)
		{
			for (int t=0; t<trns.count; t++)
				SetPosition(trns.arr[t].pos);
		}

		public void AddTransitionsList (TransitionsList trns, float customHeight)
		{
			for (int t=0; t<trns.count; t++)
			{
				if (trns.arr[t].pos.x < worldPos.x || trns.arr[t].pos.x > worldPos.x+worldSize.x ||
					trns.arr[t].pos.z < worldPos.z || trns.arr[t].pos.z > worldPos.z+worldSize.z)
						continue;
					
				SetPosition( new Vector3(trns.arr[t].pos.x, customHeight, trns.arr[t].pos.z) );
			}
		}


		public TransitionsList ToTransitionsList ()
		{
			TransitionsList list = new TransitionsList(); //capacity rect.size.x * rect.size.z

			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					Vector3 pos = this[x,z];
					//if (pos.y < minHeight) continue;
					Transition trs = new Transition(pos.x, pos.z);
					trs.hash = x*2000 + z; //to make hash independent from grid size
					list.Add(trs);
				}

			return list;
		}
	}
}