using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Den.Tools;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using Den.Tools.Matrices;
using MapMagic.Nodes.MatrixGenerators;

namespace MapMagic.Terrains
{
	[System.Serializable]
	public class EdgesSet
	{
		public bool ready = false;

		public Edges heightEdges = new Edges(0,0);
		public Edges[] splatEdges;
		public Edges[] controlEdges;

		public Lowered lowered;

	}

	[System.Serializable]
	public class Edges
	{
		public float[] arr_X;
		public float[] arr_Z;
		public float[] arr_x;
		public float[] arr_z;

		public Edges (int sizeX, int sizeZ) 
		{ 
			arr_x = new float[sizeX]; arr_X = new float[sizeX];
			arr_z = new float[sizeZ]; arr_Z = new float[sizeZ];
		}

		public bool IsEmpty => arr_x.Length==0 && arr_X.Length==0 && arr_z.Length==0 && arr_Z.Length==0;

		public int SizeX {get{ return arr_x.Length; }}
		public int SizeZ {get{ return arr_z.Length; }}

		public float[] GetArr (Coord dir)
		{
			if (dir.x==0  &&  dir.z==-1) return arr_x;
			else if (dir.x==0  &&  dir.z==1) return arr_X;
			else if (dir.x==-1  &&  dir.z==0) return arr_z;
			else if (dir.x==1  &&  dir.z==0) return arr_Z;
			else return null;
		}

		#region Read/Write

			public void ReadFloats2D (float[,] heights2D)
			{
				int sizeX = heights2D.GetLength(1);
				int sizeZ = heights2D.GetLength(0);

				if (arr_x.Length != sizeX) arr_x = new float[sizeX];
				if (arr_X.Length != sizeX) arr_X = new float[sizeX];
				if (arr_z.Length != sizeZ) arr_z = new float[sizeZ];
				if (arr_Z.Length != sizeZ) arr_Z = new float[sizeZ];

				for (int x=0; x<sizeX; x++)
				{
					arr_x[x] = heights2D[0, x];
					arr_X[x] = heights2D[sizeZ-1, x];
				}

				for (int z=0; z<sizeZ; z++)
				{
					arr_z[z] = heights2D[z, 0];
					arr_Z[z] = heights2D[z, sizeX-1];
				}
			}


			public void WriteFloats2D (float[,] heights2D)
			{
				int sizeX = heights2D.GetLength(1);
				int sizeZ = heights2D.GetLength(0);

				for (int x=0; x<sizeX; x++)
				{
					heights2D[0, x] = arr_x[x];
					heights2D[sizeZ-1, x] = arr_X[x];
				}

				for (int z=0; z<sizeZ; z++)
				{
					heights2D[z, 0] = arr_z[z];
					heights2D[z, sizeX-1] = arr_Z[z];
				}
			}


			public void ReadSplitFloats2D (float[][,] heights2DSplits)
			{
				int sizeX = 0; for (int i=0; i<heights2DSplits.Length; i++) sizeX += heights2DSplits[i].GetLength(0);
				int sizeZ = heights2DSplits[0].GetLength(1);

				if (arr_x.Length != sizeX) arr_x = new float[sizeX];
				if (arr_X.Length != sizeX) arr_X = new float[sizeX];
				if (arr_z.Length != sizeZ) arr_z = new float[sizeZ];
				if (arr_Z.Length != sizeZ) arr_Z = new float[sizeZ];

				float[,] last = heights2DSplits[heights2DSplits.Length-1];
				int lastWidth = last.GetLength(0);

				for (int x=0; x<sizeX; x++)
				{
					arr_x[x] = heights2DSplits[0][0, x];
					arr_X[x] = last[lastWidth-1, x];
				}

				int offset = 0;
				for (int i=0; i<heights2DSplits.Length; i++)
				{
					int segLength = heights2DSplits[i].GetLength(0);
					for (int z=0; z<segLength; z++)
					{
						arr_z[offset] = heights2DSplits[i][z, 0];
						arr_Z[offset] = heights2DSplits[i][z, sizeZ-1];

						offset++;
					}
				}
			}


			public void WriteSplitFloats2D (float[][,] heights2D)
			{
				int sizeX = arr_x.Length;
				int sizeZ = arr_z.Length;

				float[,] last = heights2D[heights2D.Length-1];
				int lastWidth = last.GetLength(0);
				for (int x=0; x<sizeX; x++)
				{
					heights2D[0][0, x] = arr_x[x];
					last[lastWidth-1, x] = arr_X[x];
				}

				int offset = 0;
				for (int i=0; i<heights2D.Length; i++)
				{
					int segLength = heights2D[i].GetLength(0);
					for (int z=0; z<segLength; z++)
					{
						heights2D[i][z, 0] = arr_z[offset];
						heights2D[i][z, sizeZ-1] = arr_Z[offset];

						offset++;
					}
				}
			}


			public void ReadRawFloat (byte[] bytes)
			{
				int res = (int)Mathf.Sqrt(bytes.Length/4);

				if (arr_x.Length != res) arr_x = new float[res];
				if (arr_X.Length != res) arr_X = new float[res];
				if (arr_z.Length != res) arr_z = new float[res];
				if (arr_Z.Length != res) arr_Z = new float[res];

				Matrix.FloatToBytes converter = new Matrix.FloatToBytes();

				for (int x=0; x<res; x++)
				{
					int pos = x*4;
					converter.b0 = bytes[pos];
					converter.b1 = bytes[pos +1];
					converter.b2 = bytes[pos +2];
					converter.b3 = bytes[pos +3];
					arr_x[x] = converter.f*2; //texture requires halved value for some reason

					pos = (x + res*(res-1) )*4;
					converter.b0 = bytes[pos];
					converter.b1 = bytes[pos +1];
					converter.b2 = bytes[pos +2];
					converter.b3 = bytes[pos +3];
					arr_X[x] = converter.f*2;
				}

				for (int z=0; z<res; z++)
				{
					int pos = z*res*4;
					converter.b0 = bytes[pos];
					converter.b1 = bytes[pos +1];
					converter.b2 = bytes[pos +2];
					converter.b3 = bytes[pos +3];
					arr_z[z] = converter.f*2;

					pos = (z*res + res-1)*4;
					converter.b0 = bytes[pos];
					converter.b1 = bytes[pos +1];
					converter.b2 = bytes[pos +2];
					converter.b3 = bytes[pos +3];
					arr_Z[z] = converter.f*2;
				}
			}


			public void WriteRawFloat (byte[] bytes)
			{
				int res = (int)Mathf.Sqrt(bytes.Length/4);

				Matrix.FloatToBytes converter = new Matrix.FloatToBytes();

				for (int x=0; x<res; x++)
				{
					converter.f = arr_x[x]/2; //texture requires halved value for some reason
					int pos = x*4;
					bytes[pos] = converter.b0;
					bytes[pos +1] = converter.b1;
					bytes[pos +2] = converter.b2;
					bytes[pos +3] = converter.b3;

					converter.f = arr_X[x]/2;
					pos = (x + res*(res-1) )*4;
					bytes[pos] = converter.b0;
					bytes[pos +1] = converter.b1;
					bytes[pos +2] = converter.b2;
					bytes[pos +3] = converter.b3;
				}

				for (int z=0; z<res; z++)
				{
					converter.f = arr_z[z]/2;
					int pos = z*res*4;
					bytes[pos] = converter.b0;
					bytes[pos +1] = converter.b1;
					bytes[pos +2] = converter.b2;
					bytes[pos +3] = converter.b3;

					converter.f = arr_Z[z]/2;
					pos = (z*res + res-1)*4;
					bytes[pos] = converter.b0;
					bytes[pos +1] = converter.b1;
					bytes[pos +2] = converter.b2;
					bytes[pos +3] = converter.b3;
				}
			}


			public void ReadDelta2D (float[,] heights2D)
			/// Reads not the edge values, but the delta between edges and 2nd pixel from edge
			{
				int sizeX = heights2D.GetLength(1);
				int sizeZ = heights2D.GetLength(0);

				if (arr_x.Length != sizeX) arr_x = new float[sizeX];
				if (arr_X.Length != sizeX) arr_X = new float[sizeX];
				if (arr_z.Length != sizeZ) arr_z = new float[sizeZ];
				if (arr_Z.Length != sizeZ) arr_Z = new float[sizeZ];

				for (int x=0; x<sizeX; x++)
				{
					arr_x[x] = heights2D[0,x] - heights2D[1,x];
					arr_X[x] = heights2D[sizeZ-1,x] - heights2D[sizeZ-2,x];
				}

				for (int z=0; z<sizeZ; z++)
				{
					arr_z[z] = heights2D[z,0] - heights2D[z,1];
					arr_Z[z] = heights2D[z, sizeX-1] - heights2D[z, sizeX-2];
				}
			}

			public void ReadFloats3D (float[,,] splats2D, int ch)
			{
				int sizeX = splats2D.GetLength(1);
				int sizeZ = splats2D.GetLength(0);

				if (arr_x.Length != sizeX) arr_x = new float[sizeX];
				if (arr_X.Length != sizeX) arr_X = new float[sizeX];
				if (arr_z.Length != sizeZ) arr_z = new float[sizeZ];
				if (arr_Z.Length != sizeZ) arr_Z = new float[sizeZ];

				for (int x=0; x<sizeX; x++)
				{
					arr_x[x] = splats2D[0, x, ch];
					arr_X[x] = splats2D[sizeZ-1, x, ch];
				}

				for (int z=0; z<sizeZ; z++)
				{
					arr_z[z] = splats2D[z, 0, ch];
					arr_Z[z] = splats2D[z, sizeX-1, ch];
				}
			}

			public void ReadSplats (float[,,] splats, int ch)
			{
				int sizeX = splats.GetLength(1);
				int sizeZ = splats.GetLength(0);

				if (arr_x.Length != sizeX) arr_x = new float[sizeX];
				if (arr_X.Length != sizeX) arr_X = new float[sizeX];
				if (arr_z.Length != sizeZ) arr_z = new float[sizeZ];
				if (arr_Z.Length != sizeZ) arr_Z = new float[sizeZ];

				for (int x=0; x<sizeX; x++)
				{
					arr_x[x] = splats[0, x, ch];
					arr_X[x] = splats[sizeZ-1, x, ch];
				}

				for (int z=0; z<sizeZ; z++)
				{
					arr_z[z] = splats[z, 0, ch];
					arr_Z[z] = splats[z, sizeX-1, ch];
				}
			}


			public void WriteSplats (float[,,] splats, int ch)
			{
				int sizeX = splats.GetLength(1);
				int sizeZ = splats.GetLength(0);

				for (int x=0; x<sizeX; x++)
				{
					splats[0, x, ch] = arr_x[x];
					splats[sizeZ-1, x, ch] = arr_X[x];
				}

				for (int z=0; z<sizeZ; z++)
				{
					splats[z, 0, ch] = arr_z[z];
					splats[z, sizeX-1, ch] = arr_Z[z];
				}
			}


			public void ReadColors (Color[] colors, int ch)
			{
				int res = (int)Mathf.Sqrt(colors.Length);

				if (arr_x.Length != res) arr_x = new float[res];
				if (arr_X.Length != res) arr_X = new float[res];
				if (arr_z.Length != res) arr_z = new float[res];
				if (arr_Z.Length != res) arr_Z = new float[res];

				for (int x=0; x<res; x++)
				{
					int pos = x;
					arr_x[x] = colors[x][ch];
					arr_X[x] = colors[x + res*(res-1)][ch];
				}

				for (int z=0; z<res; z++)
				{
					arr_z[z] = colors[z*res][ch];
					arr_Z[z] = colors[z*res + res-1][ch];
				}
			}


			public void WriteColors (Color[] colors, int ch)
			{
				int res = (int)Mathf.Sqrt(colors.Length);

				for (int x=0; x<res; x++)
				{
					int pos = x;
					colors[x][ch] = arr_x[x];
					colors[x + res*(res-1)][ch] = arr_X[x];
				}

				for (int z=0; z<res; z++)
				{
					colors[z*res][ch] = arr_z[z];
					colors[z*res + res-1][ch] = arr_Z[z];
				}
			}

		#endregion
	}

	[System.Serializable]
	public struct Lowered
	///Gets/sets lowered state by direction coord
	{
		[SerializeField] private bool lowered_x, lowered_X, lowered_z, lowered_Z; //edges where the draft is welded with main

		public bool this[Coord dir, Coord thisCoord] 
		{
			get{
				bool val = false;
				if (dir.x==0  &&  dir.z==-1) val = lowered_x;
				else if (dir.x==0  &&  dir.z==1) val = lowered_X;
				else if (dir.x==-1  &&  dir.z==0) val = lowered_z;
				else if (dir.x==1  &&  dir.z==0) val = lowered_Z;

				Debug.Log("CHECK Low state for " + thisCoord + " dir " + dir + " is " + val);  
				return val; }
		}

		public bool this[Coord dir] 
		{
			get{
				if (dir.x==0  &&  dir.z==-1) return lowered_x;
				else if (dir.x==0  &&  dir.z==1) return lowered_X;
				else if (dir.x==-1  &&  dir.z==0) return lowered_z;
				else if (dir.x==1  &&  dir.z==0) return lowered_Z;
				else return false; }
			set{
				if (dir.x==0  &&  dir.z==-1) lowered_x = value;
				else if (dir.x==0  &&  dir.z==1) lowered_X = value;
				else if (dir.x==-1  &&  dir.z==0) lowered_z = value;
				else if (dir.x==1  &&  dir.z==0) lowered_Z = value; }
		}
	}


	public static class Weld
	{
		private static readonly Coord[] weldDirections = new Coord[] { new Coord(1,0), new Coord(-1,0), new Coord(0,1), new Coord(0,-1) };
		private static readonly Coord[] cornerDirections = new Coord[] { new Coord(1,1), new Coord(-1,1), new Coord(1,-1), new Coord(-1,-1) };

		public static Action<TileData, EdgesSet> ReadEdgesCustom;
		public static Action<TileData, EdgesSet> WriteEdgesCustom;
		//to perform welding operation in other assembles like MicroSplat

		public static void WeldEdges (Edges src, Coord srcCoord, Edges dst, Coord dstCoord)
		{
			Coord dir = srcCoord-dstCoord;

			if (dir.x==0  &&  dir.z==1)
				WeldArrays(src.arr_x, dst.arr_X);

			else if (dir.x==0  &&  dir.z==-1)
				WeldArrays(src.arr_X, dst.arr_x);
			
			else if (dir.x==1  &&  dir.z==0)
				WeldArrays(src.arr_z, dst.arr_Z);

			else if (dir.x==-1  &&  dir.z==0)
				WeldArrays(src.arr_Z, dst.arr_z);
		}


		public static void WeldArrays (float[] src, float[] dst, bool lowerOnMismatch=false)
		{
			//if arrays size match - simply copy them
			if (src.Length == dst.Length)
				Array.Copy(src, dst, src.Length);
				//for (int i=0; i<src.Length; i++) dst[i] = dst[i]+0.1f;

			//welding draft with the main tile
			else if (src.Length > dst.Length)
			{
				int srcStep = (src.Length-1) / (dst.Length-1);

				//assigning value
				for (int i=0; i<dst.Length; i++)
					dst[i] = src[i*srcStep];
				
				//avoiding holes (lowering edges)
				if (lowerOnMismatch)
				{
					float prevLower = 0;
					for (int i=0; i<dst.Length-1; i++)
					{
						float nextLower = 0;
						for (int j=0; j<srcStep; j++)
						{
							float percent = 1f*j / srcStep;
							float interpolatedDst = dst[i]*(1-percent) + dst[i+1]*percent;
							float realSrc = src[i*srcStep + j];
							float delta = interpolatedDst - realSrc;
							if (delta>nextLower) nextLower = delta;
						}

						dst[i] -= prevLower>nextLower ? prevLower : nextLower;

						prevLower = nextLower;
					}
				}
			}
		}


		public static void WeldArraysDebug (float[] src, float[] dst, bool lowerOnMismatch=false)
		{
			//if (lowerOnMismatch)
			//	for (int i=0; i<dst.Length-1; i++)
			//		dst[i] = 0;
		}


		#region Weld in Thread

			public static void ReadEdges (TileData thisData, EdgesSet thisEdges)
			/// Reads apply data and converts to edges. Edges are ready to use after this stage.
			{
				HeightOutput200.ApplySetData heightDataFull = thisData.ApplyOfType<HeightOutput200.ApplySetData>();
				if (heightDataFull != null)
					thisEdges.heightEdges.ReadFloats2D(heightDataFull.heights2D);

				HeightOutput200.ApplySplitData heightSplitData = thisData.ApplyOfType<HeightOutput200.ApplySplitData>();
				if (heightSplitData != null)
					thisEdges.heightEdges.ReadSplitFloats2D(heightSplitData.heights2DSplits);

				#if UNITY_2019_1_OR_NEWER
				HeightOutput200.ApplyTexData heightTexData = thisData.ApplyOfType<HeightOutput200.ApplyTexData>();
				if (heightTexData != null)
					thisEdges.heightEdges.ReadRawFloat(heightTexData.texBytes);
				#endif


				TexturesOutput200.ApplyData texturesData = thisData.ApplyOfType<TexturesOutput200.ApplyData>();
				if (texturesData != null && texturesData.splats!=null)
				{
					int numChs = texturesData.splats.GetLength(2);
					if (thisEdges.splatEdges==null || thisEdges.splatEdges.Length != numChs)
						Array.Resize(ref thisEdges.splatEdges, numChs);
					
					for (int ch=0; ch<numChs; ch++)
					{
						if (thisEdges.splatEdges[ch] == null) 
							thisEdges.splatEdges[ch] = new Edges(0,0);

						thisEdges.splatEdges[ch].ReadSplats(texturesData.splats, ch);
					}
				}

				ReadEdgesCustom?.Invoke(thisData, thisEdges);

				thisEdges.ready = true;
			}


			public static void WeldEdgesInThread (EdgesSet thisEdges, TerrainTileManager tileManager, Coord coord, bool isDraft=false)
			/// Welds edges (does not affect data)
			{
				if (tileManager == null) return;

				for (int d=0; d<weldDirections.Length; d++)
				{
					TerrainTile neigTile = tileManager[coord + weldDirections[d]];
					if (neigTile == null  ||  
						(isDraft  &&  neigTile.draft == null)  ||  (isDraft  &&  !neigTile.draft.generateReady)  ||  
						(!isDraft  &&  neigTile.main  == null)  ||  (!isDraft  &&  !neigTile.main.generateReady)) 
							continue;  
							//if null or not generate-ready

					EdgesSet neigEdges = isDraft ? neigTile.draft?.edges : neigTile.main?.edges;
					if (neigEdges==null || !neigEdges.ready) continue;

					//height
					if (neigEdges.heightEdges != null && neigEdges.heightEdges.SizeX == thisEdges.heightEdges.SizeX)
						Weld.WeldEdges(neigEdges.heightEdges, neigTile.coord, thisEdges.heightEdges, coord);

					//splats
					if (neigEdges.splatEdges != null  &&  thisEdges.splatEdges != null  &&  thisEdges.splatEdges.Length == neigEdges.splatEdges.Length)
						for (int i=0; i<thisEdges.splatEdges.Length; i++)
					{
						if (thisEdges.splatEdges[i].SizeX == neigEdges.splatEdges[i].SizeX)
							Weld.WeldEdges(neigEdges.splatEdges[i], neigTile.coord, thisEdges.splatEdges[i], coord);
					}

					//textures
					if (neigEdges.controlEdges != null && thisEdges.controlEdges != null && thisEdges.controlEdges.Length == neigEdges.controlEdges.Length)
						for (int i=0; i<thisEdges.controlEdges.Length; i++)
					{
						if (thisEdges.controlEdges[i].SizeX == neigEdges.controlEdges[i].SizeX)
							Weld.WeldEdges(neigEdges.controlEdges[i], neigTile.coord, thisEdges.controlEdges[i], coord);
					}
				}
			}
			

			public static void WriteEdges (TileData thisData, EdgesSet thisEdges)
			/// Writes edges back to apply data after they have been changed
			{
				HeightOutput200.ApplySetData heightDataFull = thisData.ApplyOfType<HeightOutput200.ApplySetData>();
				if (heightDataFull != null)
					thisEdges.heightEdges.WriteFloats2D(heightDataFull.heights2D);

				HeightOutput200.ApplySplitData heightSplitData = thisData.ApplyOfType<HeightOutput200.ApplySplitData>();
				if (heightSplitData != null)
					thisEdges.heightEdges.WriteSplitFloats2D(heightSplitData.heights2DSplits);

				#if UNITY_2019_1_OR_NEWER
				HeightOutput200.ApplyTexData heightTexData = thisData.ApplyOfType<HeightOutput200.ApplyTexData>();
				if (heightTexData != null)
					thisEdges.heightEdges.WriteRawFloat(heightTexData.texBytes);
				#endif


				TexturesOutput200.ApplyData texturesData = thisData.ApplyOfType<TexturesOutput200.ApplyData>();
				if (texturesData != null && texturesData.splats!=null)
				{
					int numChs = texturesData.splats.GetLength(2);
					if (thisEdges.splatEdges==null || thisEdges.splatEdges.Length != numChs)
						Array.Resize(ref thisEdges.splatEdges, numChs);
					
					for (int ch=0; ch<numChs; ch++)
						thisEdges.splatEdges[ch].WriteSplats(texturesData.splats, ch);
				}

				WriteEdgesCustom?.Invoke(thisData, thisEdges);
			}

		#endregion


		#region Weld on LOD switch

			public static void WeldSurroundingDraftsToThisMain (TerrainTileManager tileManager, Coord coord)
			/// Welds neig draft terrains to this one when switched on main, lowering draft edges
			/// Called in main thread on switch lods
			/// On drafts only because of the performance reasons
			{
				if (tileManager == null) return;
				TerrainTile thisTile = tileManager[coord];

				for (int d=0; d<weldDirections.Length; d++)
				{
					TerrainTile neigTile = tileManager[coord + weldDirections[d]];
					if (neigTile?.draft == null) continue;
					if (!neigTile.draft.applyReady || !neigTile.draft.generateReady) continue;

					if (neigTile.ActiveTerrain == neigTile.draft.terrain)
					{
						if (neigTile.draft.edges.lowered[-weldDirections[d]]) continue;

						WeldDraftToMain(thisTile, neigTile, weldDirections[d]);
						neigTile.draft.edges.lowered[-weldDirections[d]] = true;
					}
				}
			}


			public static void WeldThisDraftWithSurroundings (TerrainTileManager tileManager, Coord coord)
			/// Welds this tile to tiles around it on switch to draft, lowering if surround is main or restoring weld if draft
			/// This will restore edges in surrounding drafts too
			{
				if (tileManager == null) return;
				TerrainTile thisTile = tileManager[coord];

				for (int d=0; d<weldDirections.Length; d++)
				{
					Coord dir = weldDirections[d];

					TerrainTile neigTile = tileManager[coord + weldDirections[d]];
					if (neigTile == null) continue;
					
					//restoring edges
					if (neigTile.draft != null  &&  neigTile.ActiveTerrain == neigTile.draft.terrain)
					{
						if (neigTile.draft.edges.lowered[-dir])
						{
							RestoreDraftWeld(neigTile, -dir);
							neigTile.draft.edges.lowered[-dir] = false;
						}

						if (thisTile.draft.edges.lowered[dir])
						{
							RestoreDraftWeld(thisTile, dir);
							thisTile.draft.edges.lowered[dir] = false;
						}
					}

					//lowering this edges
					else if (neigTile.main != null  &&  neigTile.ActiveTerrain == neigTile.main.terrain)
					{
						if (thisTile.draft.edges.lowered[dir]) continue;
						WeldDraftToMain(neigTile, thisTile, -dir);
						thisTile.draft.edges.lowered[dir] = true;
					}
				}
			}


			private static void RestoreDraftWeld (TerrainTile welded, Coord weldDir)
			/// Restoring height edges if they were lowered by welding with main
			{
				EdgesSet edges = welded.draft.edges;
				float[] arr = edges.heightEdges.GetArr(weldDir);
				Weld.ApplyToTerrain(welded.draft.terrain.terrainData, arr, weldDir);
			}


			private static void WeldDraftToMain (TerrainTile reference, TerrainTile welded, Coord weldDir)
			/// Lowers edges on welding to main tile
			{
				if (welded.draft.edges.heightEdges.IsEmpty)
					welded.draft.edges.heightEdges.ReadFloats2D(welded.draft.terrain.terrainData.GetHeights(0,0,welded.draft.terrain.terrainData.heightmapResolution,welded.draft.terrain.terrainData.heightmapResolution));
					//for an unknown reason ready chunks in scene do not have their edges (generated or saved). Leads to "Empty weld array" error. Forcing to read from terrain.

				EdgesSet refEdges = reference.main.edges;
				EdgesSet weldEdges = welded.draft.edges;

				//loading saved edges big array
				float[] refArr = refEdges.heightEdges.GetArr(weldDir);

				//duplicating neig saved edges short array
				float[] weldArr = weldEdges.heightEdges.GetArr(-weldDir);
				float[] weldArrCopy = new float[weldArr.Length];
				//Array.Copy(weldArr, weldArrCopy, weldArr.Length); //will be re-filled anyways
						
				//welding and apply to terrain
				Weld.WeldArrays(refArr, weldArrCopy, lowerOnMismatch:true);

				Weld.ApplyToTerrain(welded.draft.terrain.terrainData, weldArrCopy, -weldDir);

			}


			private static void ApplyToTerrain (TerrainData terrData, float[] arr, Coord dir)
			{
				if (arr.Length == 0)
					throw new Exception("Empty weld array. Possibly terrain has not been generated yet");

				int size = terrData.heightmapResolution;

				if (dir.x==-1  &&  dir.z==0)
				{
					float[,] arr_x_2D = new float[size,1];
					for (int i=0; i<size; i++) arr_x_2D[i,0] = arr[i];
					terrData.SetHeightsDelayLOD(0,0,arr_x_2D);
				}

				else if (dir.x==1  &&  dir.z==0)
				{
					float[,] arr_x_2D = new float[size,1];
					for (int i=0; i<size; i++) arr_x_2D[i,0] = arr[i];
					terrData.SetHeightsDelayLOD(size-1,0,arr_x_2D);
				}
			
				else if (dir.x==0  &&  dir.z==-1)
				{
					float[,] arr_z_2D = new float[1,size];
					for (int i=0; i<size; i++) arr_z_2D[0,i] = arr[i];
					terrData.SetHeightsDelayLOD(0,0,arr_z_2D);
				}

				else if (dir.x==0  &&  dir.z==1)
				{
					float[,] arr_z_2D = new float[1,size];
					for (int i=0; i<size; i++) arr_z_2D[0,i] = arr[i];
					terrData.SetHeightsDelayLOD(0,size-1,arr_z_2D);
				}
			}


			public static void WeldCorners (TerrainTileManager tileManager, Coord coord, bool isDraft=false)
			/// Iterates all terrain at the corner and chooses the first occurrence value. 
			{
				if (tileManager == null) return;
				TerrainTile thisTile = tileManager[coord];
				Terrain thisTerrain = isDraft ? thisTile.draft?.terrain : thisTile.main?.terrain;
				TerrainData thisData = thisTerrain.terrainData;
				if (thisTerrain == null) return;
				int resolution = thisTerrain.terrainData.heightmapResolution;
				float[,] tempArr = new float[1,1];

				for (int c=0; c<cornerDirections.Length; c++)
				{
					Coord corner = (cornerDirections[c]+1) / 2;
					//float thisHeight = thisData.GetHeight((corner.x+1)/2, (corner.z+1)/2);

					//reading and selecting maximum
					for (int d=0; d<cornerDirections.Length; d++)
					{
						Coord dir = (cornerDirections[d]+1) / 2;

						TerrainTile cornerTile = tileManager[coord + dir+corner-1];
						if (cornerTile == null || cornerTile == thisTile) continue;

						Terrain cornerTerrain = isDraft ? cornerTile.draft?.terrain : cornerTile.main?.terrain;
						if (cornerTerrain==null || !cornerTerrain.isActiveAndEnabled) continue;
						TerrainData cornerData = cornerTerrain.terrainData;

						float cornerHeight = cornerData.GetHeight((1-dir.x) * (resolution-1), (1-dir.z) * (resolution-1)) / cornerData.size.y;
						tempArr[0,0] = cornerHeight;
						thisData.SetHeightsDelayLOD(corner.x * (resolution-1), corner.z * (resolution-1), tempArr);

						break;
					}
				}
			}

		#endregion


		#region Setting Neightbor

			//It all should work, but Unity calls Terrain.SetConnectivityDirty on each terrain enable or disable

			public static void SetNeighbors (TerrainTileManager tileManager, Coord coord)
			{
				if (tileManager == null) return;
				TerrainTile thisTile = tileManager[coord];

				//foreach (TerrainTile tile in tileManager.Tiles())
				TerrainTile tile = tileManager[coord];
				coord = tile.coord;
				{
					Terrain terrain = tile.main?.terrain; //isDraft ? tile.draft?.terrain : tile.main?.terrain;
					//if (terrain == null || !terrain.isActiveAndEnabled) continue;


					//for (int d=0; d<weldDirections.Length; d++)
					//unroll

					Terrain leftNeighbor = tileManager[ new Coord(coord.x-1, coord.z) ]?.main?.terrain;
					if (leftNeighbor!=null && leftNeighbor.isActiveAndEnabled)
					{
						terrain.SetNeighbors(leftNeighbor, terrain.topNeighbor, terrain.rightNeighbor, terrain.bottomNeighbor);
						leftNeighbor.SetNeighbors(leftNeighbor.leftNeighbor, leftNeighbor.topNeighbor, terrain, leftNeighbor.bottomNeighbor);
					}

					Terrain rightNeighbor = tileManager[ new Coord(coord.x+1, coord.z) ]?.main?.terrain;
					if (rightNeighbor!=null && rightNeighbor.isActiveAndEnabled)
					{
						terrain.SetNeighbors(terrain.leftNeighbor, terrain.topNeighbor, rightNeighbor, terrain.bottomNeighbor);
						rightNeighbor.SetNeighbors(terrain, rightNeighbor.topNeighbor, rightNeighbor.rightNeighbor, rightNeighbor.bottomNeighbor);
					}

					Terrain bottomNeighbor = tileManager[ new Coord(coord.x, coord.z-1) ]?.main?.terrain;
					if (bottomNeighbor!=null && bottomNeighbor.isActiveAndEnabled)
					{
						terrain.SetNeighbors(terrain.leftNeighbor, terrain.topNeighbor, terrain.rightNeighbor, bottomNeighbor);
						bottomNeighbor.SetNeighbors(bottomNeighbor.leftNeighbor, terrain, bottomNeighbor.rightNeighbor, bottomNeighbor.bottomNeighbor);
					}

					Terrain topNeighbor = tileManager[ new Coord(coord.x, coord.z+1) ]?.main?.terrain;
					if (topNeighbor!=null && topNeighbor.isActiveAndEnabled)
					{
						terrain.SetNeighbors(terrain.leftNeighbor, topNeighbor, terrain.rightNeighbor, terrain.bottomNeighbor);
						topNeighbor.SetNeighbors(topNeighbor.leftNeighbor, topNeighbor.topNeighbor, topNeighbor.rightNeighbor, terrain);
					}
				}
			}


			public static void SetNeighborsAll (TerrainTileManager tileManager)
			{
				System.Diagnostics.Stopwatch timer = null;
				timer = new System.Diagnostics.Stopwatch();
				timer.Start();

				if (tileManager == null) return;
				//TerrainTile thisTile = tileManager[coord];

				foreach (TerrainTile tile in tileManager.Tiles())
					SetNeighbors(tileManager, tile.coord);

				timer.Stop();
				Debug.Log("Neighboring in " + timer.Elapsed.TotalMilliseconds + "ms" + " (" + timer.ElapsedMilliseconds + ")");
			}


			private static Terrain GetNeighbor (Terrain terrain, Coord dir)
			{
				if (dir.x==0  &&  dir.z==-1) return terrain.bottomNeighbor;
				else if (dir.x==0  &&  dir.z==1) return terrain.topNeighbor;
				else if (dir.x==-1  &&  dir.z==0) return terrain.leftNeighbor;
				else if (dir.x==1  &&  dir.z==0) return terrain.rightNeighbor;
				else return null;
			}

		#endregion
	}
}
