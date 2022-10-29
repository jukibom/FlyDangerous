using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.GUI;

namespace Den.Tools
{
	public static class Pathfinding
	{
		public class Factors
		{
			[Val("Incline")] public float incline = 0.25f;
			public float lowland = 0.25f;
			public float highland = 0;
			public float straighten = 0.25f;
			public float distance = 0.25f; //actually not used, but moving other factors to 0 will make spline as short as possible

			private float this[int num] 
			{get{
				switch (num)
				{
					case 0: return incline;
					case 1: return lowland;
					case 2: return highland;
					case 3: return straighten;
					default: return distance;
				}
			}
			set{
				switch (num)
				{
					case 0: incline = value; break;
					case 1: lowland = value; break;
					case 2: highland = value; break;
					case 3: straighten = value; break;
					default: distance = value; break;
				}
			}}


			private void SetNormalizedVal (int num, float val)
			{
				if (val<0) val = 0;
				if (val>1) val = 1;

				this[num] = 0;
				float sum = incline + lowland + highland + straighten + distance;

				for (int i=0; i<5; i++)
					this[i] = sum!=0 ? this[i]/sum * (1-val) : (1-val)/4;
				
				this[num] = val;
			}


			public float Incline {set{ SetNormalizedVal(0,value); }}
			public float Lowland {set{ SetNormalizedVal(1,value); }}
			public float Highland {set{ SetNormalizedVal(2,value); }}
			public float Straighten {set{ SetNormalizedVal(3,value); }}
			public float Distance {set{ SetNormalizedVal(4,value); }}
		}


		public class FixedList<T>
		/// More handy in natives, faster in managed
		{
			public T[] arr;
			public T count;

			public FixedList (int capacity)	{ arr = new T[capacity]; }
			public FixedList (T[] arr) { this.arr = arr; }
		}


		private static readonly Coord[] neigDiagonalFirst = new Coord[] { 
			new Coord(-1,-1), new Coord(-1,1), new Coord(1,1), new Coord(1,-1), 
			new Coord(0,-1), new Coord(-1,0), new Coord(0,1), new Coord(1,0) }; 

		private static readonly Coord[] neigLineFirst = new Coord[] { 
			new Coord(0,-1), new Coord(-1,0), new Coord(0,1), new Coord(1,0),
			new Coord(-1,-1), new Coord(-1,1), new Coord(1,1), new Coord(1,-1) }; 

		private static readonly bool[] directionRnd = new bool[] {true, false, false, false, true, false, true, false, true, false, 
			true, true, true, false, true, true, true, false, true, false, true, true, false, false, false, false, false, false, false, 
			false, false, true, true, true, false, false, true, true, true, true, false, false, false, true, false, false, false, false, 
			true, false, true, false, true, false, true, false, true, false, false, true, false, true, true, false, false, true, false, 
			true, true, true, false, true, true, false, true, true, false, false, false, false, false, true, false, true, false, true, 
			false, true, true, false, false, true, false, false, true, true, false, false, false, false, true};


		public static float CalcWeight (Coord coord, Coord dir, MatrixWorld heights, Matrix mask, Factors factors)
		/// returns 0-infinity range, where 0 is no friction, 1 is the standard step, infinity is impassable
		{
			CoordRect rect = heights != null ? heights.rect : mask.rect;
			int pos = (coord.z-rect.offset.z)*rect.size.x + coord.x - rect.offset.x;

			int nPos = pos + rect.size.x*dir.z + dir.x;
			
			float diagonal = 1;
			if (dir.x*dir.z != 0) diagonal = 1.414213562373f;

			float distanceFactor = diagonal;

			float maskFactor = 1;
			if (mask != null)
			{
				maskFactor = mask.arr[nPos]; //range 0-1 (0 is standard step, 1 is impassable)
				
				if (maskFactor > 0.999f) maskFactor = float.MaxValue; //float.PositiveInfinity;
				else maskFactor = 1 / (1-mask.arr[nPos]) + 1;
				// 0-1 -> 1-Inf
			}

			float inclineFactor = 1;
			//float highlandFactor = 1;
			//float lowlandFactor = 1;
			if (heights != null)
			{
				float nHeight = heights.arr[nPos];

				float pixelSize = heights.worldSize.x/heights.rect.size.x;
				float elevation = nHeight - heights.arr[pos]; //thisHeight;
				if (elevation < 0) elevation = -elevation;
				elevation *= heights.worldSize.y;
				elevation = elevation / (pixelSize*diagonal); //since diagonal is less steep
				
				inclineFactor = elevation / factors.incline;  //0 - 1_or_more, 0 is standard, 1 is impassable
				if (inclineFactor > 0.999f) inclineFactor = float.MaxValue;
				else inclineFactor = 1 / (1-inclineFactor);
				
				//inclineFactor = Mathf.Atan(elevation) / (Mathf.PI/2); //range 0-1 (less is more passable)
			}

			float directionFactor = 1; //directionRnd[nPos%directionRnd.Length] ? 1 : 0.999f; 
			//adding epsilon random factor to avoid long "cornered" look when factors are equal

			//transforming 
			float factor = maskFactor * distanceFactor * inclineFactor * directionFactor; 
			return factor;// != 1 ? 1/(1-factor) : float.MaxValue;
		}


		public static void FillDirs (Matrix2D<Coord> dirs, Coord to, MatrixWorld heights, Matrix mask, Factors factors,
			Matrix weights=null, FixedList<int> changedPoses=null, FixedList<int> newChangedPoses=null,
			int maxIterations = -1)
		/// Using Dijkstra to calculate directions matrix
		/// Re-writing weights, dirs and changedposes
		/// Returns null if path could not be found (in manhattan dist * 2 cells)
		{
			CoordRect rect = heights!=null ? heights.rect : mask.rect;
			Coord rectMin = rect.offset; Coord rectMax = rect.offset + rect.size;

			if (weights == null) weights = new Matrix(rect);
			if (dirs == null) dirs = new Matrix2D<Coord>(rect);

			if (changedPoses == null) changedPoses = new FixedList<int>(capacity:10000);
			if (newChangedPoses == null) newChangedPoses = new FixedList<int>(capacity:10000);

			weights.Fill(float.MaxValue); //clearing arrays (migh be used for previous pathfinding)
			weights[to] = 0;

			dirs.Fill(new Coord());

			changedPoses.arr[0] = rect.GetPos(to);
			changedPoses.count = 1;

			if (maxIterations<0) maxIterations = rect.size.x+rect.size.z;
			Coord min = rect.Min; Coord max = rect.Max;
			for (int i=0; i<maxIterations; i++)
			{
				for (int c=0; c<changedPoses.count; c++)
				{
					int pos = changedPoses.arr[c];
					Coord coord = rect.GetCoord(pos);

					if (coord.x < rectMin.x  ||  coord.x > rectMax.x-1  ||
						coord.z < rectMin.z  ||  coord.z > rectMax.z-1) return;
					
					float weight = weights.arr[pos];

					for (int d=0; d<8; d++)
					{
						Coord nCoord = neigDiagonalFirst[d];

						if ((coord.x==rectMin.x && nCoord.x==-1) || (coord.x==rectMax.x-1 && nCoord.x==1) ||
							(coord.z==rectMin.z && nCoord.z==-1) || (coord.z==rectMax.z-1 && nCoord.z==1)) continue;

						int nPos = pos + rect.size.x*nCoord.z + nCoord.x;

						float nWeight = weights.arr[nPos];
						float nNewWeight = CalcWeight(coord, nCoord, heights, mask, factors) + weight;

						if (nNewWeight < nWeight-0.0001f)  //using an epsilon delta to avoid overwriting nearly equal values
						{
							weights.arr[nPos] = nNewWeight;
							dirs.arr[nPos] = nCoord;

							newChangedPoses.arr[newChangedPoses.count] = nPos;
							newChangedPoses.count++;
						}
					}
				}

				FixedList<int> tempList = changedPoses;
				changedPoses = newChangedPoses;
				newChangedPoses = tempList;
				newChangedPoses.count = 0;
			}
		}


		public static Coord[] DrawPath (Matrix2D<Coord> dirs, Coord from, Coord to)
		{
			List<Coord> path = new List<Coord>();
			path.Add(from);
			Coord curr = from;
			for (int i=0; i<1000; i++)
			{
				curr -= dirs[curr];
				path.Add(curr);

				if (curr == to) break;
			}

			return path.ToArray(); 
		}
	}
}
