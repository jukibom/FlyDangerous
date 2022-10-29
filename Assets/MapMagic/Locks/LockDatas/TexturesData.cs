using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Terrains;
using MapMagic.Nodes;
using MapMagic.Nodes.MatrixGenerators;

namespace MapMagic.Locks
{
	public class TexturesData : ILockData
	{
		private float[,,] lockSplats;
		private TerrainLayer[] lockPrototypes;

		CoordCircle circle;
		int resolution; //to determine if it's changed and avoid writing


		public void Read (Terrain terrain, Lock lk) 
		{
			TerrainData terrainData = terrain.terrainData;
			if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
				{ lockSplats=null; lockPrototypes=null; return; }
				// Don't perform lock

			resolution = terrain.terrainData.alphamapResolution;
			circle = new CoordCircle(terrain, resolution, lk.worldPos, lk.worldRadius, lk.worldTransition);

			lockSplats = terrainData.GetAlphamaps(circle.rect.offset.x, circle.rect.offset.z, circle.rect.size.x, circle.rect.size.z);
			lockPrototypes = terrainData.terrainLayers;
		}


		public void WriteInThread (IApplyData applyData)
		{
			if (! (applyData is TexturesOutput200.ApplyData applyTexData) ) return;

			if (lockSplats == null || lockPrototypes == null) return; // Don't perform lock if nothing is stored
			if (applyTexData.Resolution != resolution) return; //Don't perform lock if resolution changed

			UnifyPrototypes(ref applyTexData.prototypes, ref applyTexData.splats, ref lockPrototypes, ref lockSplats);

			Matrix lockMatrix = new Matrix(circle.rect);
			Matrix genMatrix = new Matrix(circle.rect);
			for (int c=0; c<lockPrototypes.Length; c++)
			{
				lockMatrix.Fill(0);
				lockMatrix.ImportSplats(lockSplats, circle.rect.offset, c);
				genMatrix.ImportSplats(applyTexData.splats, new Coord(0,0), c);

				lockMatrix.ExtendCircular(circle.center, circle.radius-1, circle.transition+2, 0);
				lockMatrix.Clamp01();

				genMatrix.BlendStamped(genMatrix, lockMatrix, circle.center.x, circle.center.z, circle.radius, circle.transition);

				genMatrix.ExportSplats(applyTexData.splats, new Coord(0,0), c);
			}
		}


		public void WriteInApply (Terrain terrain, bool resizeTerrain=false) 
		{ 
			if (lockSplats == null || lockPrototypes == null) return; // Don't perform lock if nothing is stored

			TerrainData terrainData = terrain.terrainData;

			if (terrain.terrainData.alphamapResolution != resolution)
			{
				if (resizeTerrain)
					terrainData.alphamapResolution = resolution;
				else 
					return;
			}

			terrainData.terrainLayers = lockPrototypes;
			terrainData.SetAlphamaps(circle.rect.offset.x, circle.rect.offset.z, lockSplats);
		}


		public void ApplyHeightDelta (Matrix src, Matrix dst) { }


		public void ResizeFrom (ILockData otherData)
		{
			TexturesData src = (TexturesData)otherData;

			if (src.lockSplats==null || lockSplats==null) return;
			int numLayers = Mathf.Min(src.lockSplats.GetLength(2), lockSplats.GetLength(2));
			if (numLayers==0) return;

			Matrix srcMatrix = new Matrix(src.circle.rect);
			Matrix dstMatrix = new Matrix(circle.rect);

			for (int i=0; i<numLayers; i++)
			{
				srcMatrix.ImportSplats(src.lockSplats, i);
				MatrixOps.Resize(srcMatrix, dstMatrix);
				dstMatrix.ExportSplats(lockSplats, i);
			}
		}


		private static void UnifyPrototypes (ref TerrainLayer[] basePrototypes, ref float[,,] baseData, 
											 ref TerrainLayer[] addPrototypes, ref float[,,] addData)
		/// Makes both datas prototypes arrays equal, and the layers arrays relevant to prototypes (empty arrays)
		/// Safe per-channel blend could be performed after this operation
		{
			//guard if prototypes have not been changed
			if (ArrayTools.MatchExactly(basePrototypes, addPrototypes)) return;

			//creating array of unified prototypes
			List<TerrainLayer> unifiedPrototypes = new List<TerrainLayer>();
			unifiedPrototypes.AddRange(basePrototypes); //do not change the base prototypes order
			for (int p=0; p<addPrototypes.Length; p++)
			{
				if (!unifiedPrototypes.Contains(addPrototypes[p]))
					unifiedPrototypes.Add(addPrototypes[p]);
			}

			//lut to convert prototypes indexes
			Dictionary<int,int> baseToUnifiedIndex = new Dictionary<int, int>();
			Dictionary<int,int> addToUnifiedIndex = new Dictionary<int, int>();

			for (int p=0; p<basePrototypes.Length; p++)
				baseToUnifiedIndex.Add(p, unifiedPrototypes.IndexOf(basePrototypes[p]));  //should be 1,2,3,4,5, but doing this in case unified prototypes gather will be optimized

			for (int p=0; p<addPrototypes.Length; p++)
				addToUnifiedIndex.Add(p, unifiedPrototypes.IndexOf(addPrototypes[p]));

			//re-creating base data
			{
				float[,,] newBaseData = new float[baseData.GetLength(0), baseData.GetLength(1), unifiedPrototypes.Count];
			
				int baseDataLayers = baseData.GetLength(2);
				for (int i=0; i<baseDataLayers; i++)
					ArrayTools.CopyLayer(baseData, newBaseData, i, baseToUnifiedIndex[i]);

				baseData = newBaseData;
			}

			//re-creating add data
			{
				float[,,] newAddData = new float[addData.GetLength(0), addData.GetLength(1), unifiedPrototypes.Count];
			
				int addDataLayers = addData.GetLength(2);
				for (int i=0; i<addDataLayers; i++)
					ArrayTools.CopyLayer(addData, newAddData, i, addToUnifiedIndex[i]);

				addData = newAddData;
			}

			//saving prototypes
			basePrototypes = unifiedPrototypes.ToArray();
			addPrototypes = unifiedPrototypes.ToArray();
		}


		private static bool ComparePrototypes (TerrainLayer p1, TerrainLayer p2)
		{
			return  p1.diffuseTexture==p2.diffuseTexture && 
					p1.normalMapTexture==p2.normalMapTexture && 
					p1.tileSize==p2.tileSize &&
					p1.tileOffset==p2.tileOffset &&
					p1.smoothness==p2.smoothness &&
					p1.metallic==p2.metallic; //for test
		}


		[Obsolete] private static void Stamp (float[,,] arr, int arrChannel, float[,,] stamp, Coord stampOffset, int stampChannel, Coord center, int radius)
		/// stamps one splat array onto the other using coords center, radius and transition
		/// works much faster than matrices
		{
			int stampResX = stamp.GetLength(1); //x and z are swapped
			int stampResZ = stamp.GetLength(0);

			for (int x=0; x<stampResX; x++)
				for (int z=0; z<stampResZ; z++)
				{
					int sx = stampOffset.x + x;
					int sz = stampOffset.z + z;

					float dist = Mathf.Sqrt((sx-center.x)*(sx-center.x) + (sz-center.z)*(sz-center.z));
					if (dist > radius) continue;

					arr[sz, sx, arrChannel] = stamp[z, x, stampChannel];
				}
		}
	}

}