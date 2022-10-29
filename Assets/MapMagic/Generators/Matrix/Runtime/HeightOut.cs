using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Terrains;

using UnityEngine.Profiling;

#if UN_MapMagic
using uNature.Core.Extensions.MapMagicIntegration;
using uNature.Core.FoliageClasses;
#endif


namespace MapMagic.Nodes.MatrixGenerators
{
	[Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "Height", 
		section=2, 
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/HeightOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Height")]
	public class  HeightOutput200 : OutputGenerator, IInlet<MatrixWorld>
	{
		public OutputLevel outputLevel = OutputLevel.Draft | OutputLevel.Main;
		public override OutputLevel OutputLevel { get{ return outputLevel; } }

		//public float height; //stored in graph
		public enum Interpolation { None, Smooth, Scale2X, Scale4X };
		//public Interpolation interpolation; //stored in globals
		//public int splitInFrames = 4; //stored in globals

		public enum ApplyType { SetHeights, SetHeightsDelayLOD, TextureToHeightmap }
		//public ApplyType mainApply = ApplyType.TextureToHeightmap;
		//public ApplyType draftApply = ApplyType.SetHeights;
		//in globals

		public bool guiApplyType = false;


		public override void Generate (TileData data, StopToken stop)
		{
			//loading source
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
//			if (!enabled) { data.finalize.Remove(finalizeAction, this); return; }

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				data.StoreOutput(this, typeof(HeightOutput200), this, src);  //adding src since it's not changing
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);

			#if MM_DEBUG
			Log.Add("Height generated (id:" + id + " draft:" + data.isDraft + ")");
			#endif
		}


		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop)
		{
			//blending all biomes in data.height matrix
			if (data.heights == null || 
				data.heights.rect.size != data.area.full.rect.size || 
				data.heights.worldPos != (Vector3)data.area.full.worldPos || 
				data.heights.worldSize != (Vector3)data.area.full.worldSize) 
					data.heights = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			data.heights.worldSize.y = data.globals.height;
			data.heights.Fill(0);	

			foreach ((HeightOutput200 output, MatrixWorld product, MatrixWorld biomeMask) 
				in data.Outputs<HeightOutput200,MatrixWorld,MatrixWorld> (typeof(HeightOutput200), inSubs:true) )
			{
				if (data.heights == null) //height output not generated or received null result
					return;

				for (int a=0; a<data.heights.arr.Length; a++)
				{
					if (stop!=null && stop.stop) return;

					float val = product.arr[a];
					float biomeVal = biomeMask!=null ? biomeMask.arr[a] : 1;

					data.heights.arr[a] += val * biomeVal;
				}
			}

			//determining resolutions
			if (stop!=null && stop.stop) return;
			Interpolation interpolation = data.globals.heightInterpolation;
			int upscale = GetUpscale(interpolation);
			int margins =  data.area.Margins;
			int matrixRes = (data.heights.rect.size.x - margins*2 - 1)*upscale + margins*2*upscale + 1;

			//creating upscaled/blurred height matrix
			if (stop!=null && stop.stop) return;
			Matrix matrix;
			switch (interpolation)
			{
				default: matrix = data.heights; break;
				case Interpolation.Smooth:
					matrix = new Matrix(data.heights);
					MatrixOps.GaussianBlur(matrix, 0.5f);
					break;
				//case Interpolation.Scale2X:
				//	matrix = new Matrix( new CoordRect(data.heights.rect.offset, new Coord(matrixRes)) ); 
				//	MatrixOps.UpscaleFast(data.heights, matrix);
				//	MatrixOps.GaussianBlur(matrix, 0.5f); //upscaleFast interpolates linear, so each new vert is exactly between the old ones
				//	break;
				//nah, summary effect is better with classic resize
				case Interpolation.Scale4X: case Interpolation.Scale2X:
					matrix = new Matrix( new CoordRect(data.heights.rect.offset, new Coord(matrixRes)) ); 
					MatrixOps.Resize(data.heights, matrix);
					break;
			}

			//clamping heights to 0-1 (otherwise culing issues can occur)
			matrix.Clamp01();

			//2Darray resolution and 
			int arrRes = matrix.rect.size.x - margins*upscale*2;

			//splits number (used for SetHeightsDelayLOD and Texture)
			int splitSize = data.globals.heightSplit;
			int numSplits = arrRes / splitSize;
			if (arrRes % splitSize != 0) numSplits++;

			//getting apply data
			ApplyType applyType = data.isDraft ? data.globals.heightDraftApply : data.globals.heightMainApply;
			IApplyData applyData;

			if (applyType == ApplyType.SetHeights)
			{
				float[,] heights2Dfull = new float[arrRes,arrRes];
				matrix.ExportHeights(heights2Dfull, matrix.rect.offset + margins*upscale);
				applyData = new ApplySetData() {heights2D=heights2Dfull, height=data.globals.height};
			}

			else if (applyType == ApplyType.SetHeightsDelayLOD)
			{
				float[][,] height2DSplits = new float[numSplits][,];

				int offset = 0;
				for (int i=0; i<numSplits; i++)
				{
					int spaceLeft = arrRes - offset;
					int currSplitSize = Mathf.Min(splitSize, arrRes-offset);

					float[,] heights2D = new float[currSplitSize, arrRes];

					Coord heights2Dcoord = new Coord(
						matrix.rect.offset.x + margins*upscale, 
						matrix.rect.offset.z + margins*upscale + offset );

					matrix.ExportHeights(heights2D, heights2Dcoord);

					height2DSplits[i] = heights2D;

					offset += currSplitSize;
				}

				applyData = new ApplySplitData() {heights2DSplits=height2DSplits, height=data.globals.height};
			}

			#if UNITY_2019_1_OR_NEWER
			else //if TextureToHeightmap
			{
				byte[] bytes = new byte[arrRes*arrRes*4];
				float ushortEpsilon = 1f / 65535; //since setheights is using not full ushort range, but range-1
				matrix.ExportRawFloat(bytes, matrix.rect.offset+margins*upscale, new Coord(arrRes,arrRes), mult:0.5f-ushortEpsilon);
				//not coord(margins) since matrix rect has -margins offset
				//somehow requires halved values

				applyData = new ApplyTexData() { res=arrRes, margins=margins, splitSize=splitSize, height=data.globals.height, texBytes=bytes }; 
			}
			#else
			else
				throw new Exception("Unknown Height Output apply type.\n For Unity 2018.4 or older choose either SetHeights or SetHeightsDelayLOD in Outputs Settings - Height Output - Apply Type - Main or Draft");
			#endif

			//pushing to apply
			if (stop!=null && stop.stop) return;
			Graph.OnOutputFinalized?.Invoke(typeof(HeightOutput200), data, applyData, stop);
			data.MarkApply(applyData);

			#if MM_DEBUG
			Log.Add("HeightOut Finalized");
			#endif
		}

		private static int GetUpscale (Interpolation interpolation)
		// Could be done via enum, but using this for compatibility reasons
		{
			switch (interpolation)
			{
				case Interpolation.Scale2X: return 2;
				case Interpolation.Scale4X: return 4;
				default: return 1;
			}
		}


		public override void ClearApplied (TileData data, Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;
			Vector3 terrainSize = terrainData.size;

			terrainData.heightmapResolution = 33;
			terrain.groupingID = terrainData.heightmapResolution;
			terrainData.size = terrainSize;

			data.heights = null;
		}

		public interface IApplyHeightData : IApplyData { } //common type for all height applies
		
		public class ApplySetData : IApplyData, IApplyHeightData
		{
			public float[,] heights2D;
			public float height;
			public Coord offset;  //a partial rect to avoid reading-writing all of the terrain. Size is the size of the array. 0 is data 0. Max should not be more than data size.

			public void Read (Terrain terrain) 
			{ 
				int heightRes = terrain.terrainData.heightmapResolution;
				Read(terrain, new CoordRect(0,0,heightRes,heightRes));
			}

			public void Read (Terrain terrain, CoordRect rect) 
			{ 
				heights2D = terrain.terrainData.GetHeights(rect.offset.x, rect.offset.z, rect.size.x, rect.size.z);
				offset = rect.offset;
			}

			public void Apply (Terrain terrain)
			{
				if (terrain==null || terrain.Equals(null) || terrain.terrainData==null) return; //chunk removed during apply
				TerrainData data = terrain.terrainData;

				//no resize algorithm
				Vector3 terrainSize = data.size;
				data.heightmapResolution = heights2D.GetLength(0);
				terrain.groupingID = data.heightmapResolution; //groups all the terrains of the same resolution
				data.size = new Vector3(terrainSize.x, height, terrainSize.z);

				data.SetHeights(offset.x, offset.z, heights2D);
				terrain.Flush();

				#if MM_DEBUG
				Log.Add("HeightOut Applied Set");
				#endif
			}

			public static ApplySetData Empty 
				{get{ return new ApplySetData() { heights2D = new float[33,33] }; }}

			public int Resolution {get{ return heights2D.GetLength(0); }}
		}

		
		public class ApplySplitData : IApplyDataRoutine, IApplyHeightData
		{
			public float[][,] heights2DSplits;
			public float height;

			public void Apply (Terrain terrain)
			{
				Profiler.BeginSample("Apply Height Splits " + terrain.transform.parent.name);

				if (terrain==null || terrain.Equals(null) || terrain.terrainData==null) return; //chunk removed during apply
				TerrainData data = terrain.terrainData;

				FastHeightmapResize(terrain, heights2DSplits[0].GetLength(1), new Vector3(data.size.x, height, data.size.z));
				terrain.groupingID = data.heightmapResolution;

				int offset = 0;
				for (int i=0; i<heights2DSplits.Length; i++)
				{
					data.SetHeights(0, offset, heights2DSplits[i]);
					offset += heights2DSplits[i].GetLength(0);
				}

				terrain.Flush();

				Profiler.EndSample();
			}


			public IEnumerator ApplyRoutine (Terrain terrain)
			{
				TerrainData data = terrain.terrainData;
				{
					Profiler.BeginSample("Apply ResizeHeightmap " + terrain.transform.parent.name);
					FastHeightmapResize(terrain, heights2DSplits[0].GetLength(1), new Vector3(data.size.x, height, data.size.z));
					terrain.groupingID = data.heightmapResolution;
					Profiler.EndSample();
				}
				yield return null;

				int offset = 0;
				for (int i=0; i<heights2DSplits.Length; i++)
				{
					Profiler.BeginSample("Apply HeightAddStrip " + offset + " " + terrain.transform.parent.name);

					data.SetHeightsDelayLOD(0, offset, heights2DSplits[i]);
					offset += heights2DSplits[i].GetLength(0);

					Profiler.EndSample();

					yield return null;
				}


				{
					Profiler.BeginSample("Apply ApplyDelayedHeightmapModification " + terrain.transform.parent.name);
					#if UNITY_2019_1_OR_NEWER
					terrain.terrainData.SyncHeightmap();
					#else
					terrain.ApplyDelayedHeightmapModification();
					#endif
					Profiler.EndSample();
				}
				yield return null;

				{
					Profiler.BeginSample("Apply Flush " + terrain.transform.parent.name);
					terrain.Flush();
					terrain.terrainData.size = terrain.terrainData.size; //this will recalculate terrain bounding box in 2017.1
					Profiler.EndSample();
				}
				yield return null;
			}


			public static void FastHeightmapResize (Terrain terrain, int res, Vector3 size)
			/// Resizing terrain the standard way is extremely slow. Even when creating a new terrain
			{
				TerrainData data = terrain.terrainData;
				if ((data.size - size).sqrMagnitude > 0.01f || data.heightmapResolution != res)
				{
					if (res <= 64) //brute force
					{
						data.heightmapResolution = res;
						data.size = new Vector3(size.x, size.y, size.z);
					}

					else //setting res 64, re-scaling to 1/64, and then changing res
					{
						data.heightmapResolution = 65;
						terrain.Flush(); //otherwise unity crashes without an error
						int resFactor = (res - 1) / 64;
						data.size = new Vector3(size.x / resFactor, size.y, size.z / resFactor);
						data.heightmapResolution = res;
					}
				}
			}


			public static ApplySplitData Empty 
				{get{ return new ApplySplitData() { heights2DSplits = new float[][,]{ new float[65,65] } }; }}

			public int Resolution 
			{get{ 
				if (heights2DSplits.Length==0) return 0;
				else return heights2DSplits[0].GetLength(1);
			}}
		}

		#if UNITY_2019_1_OR_NEWER
		public class ApplyTexData : IApplyDataRoutine, IApplyHeightData
		{
			public int res;
			public int margins;
			public int splitSize;
			public float height;

			public byte[] texBytes;
			
			private static Texture2D tempTex;
			private static RenderTexture renTex;


			public void Apply (Terrain terrain)
			{
				if (terrain==null || terrain.Equals(null) || terrain.terrainData==null) return; //chunk removed during apply
				TerrainData data = terrain.terrainData;

				RectInt texRect = new RectInt(0,0,res,res);

				if (tempTex == null ||  tempTex.width != res)
					tempTex = new Texture2D(res, res, TextureFormat.RFloat, mipChain:false, linear:true);
				tempTex.LoadRawTextureData(texBytes);
				tempTex.Apply(updateMipmaps:false);

				if (renTex == null ||  renTex.width != res)
				#if UNITY_2019_2_OR_NEWER
					renTex = new RenderTexture(res,res,32, RenderTextureFormat.RFloat, mipCount:0);
				#else
					renTex = new RenderTexture(res,res,32, RenderTextureFormat.RFloat);
				#endif
				Graphics.Blit(tempTex, renTex);

				//no resize algorithm
				Vector3 terrainSize = data.size;
				data.heightmapResolution = res;
				terrain.groupingID = res;
				data.size = new Vector3(terrainSize.x, height, terrainSize.z);

				RenderTexture bacRenTex = RenderTexture.active;
				RenderTexture.active = renTex;

				data.CopyActiveRenderTextureToHeightmap(texRect, texRect.min, TerrainHeightmapSyncControl.None);
				data.DirtyHeightmapRegion(texRect, TerrainHeightmapSyncControl.HeightAndLod); //or seems a bit faster on re-setting height to already existing terrains. IDK
				//data.SyncHeightmap(); //doesn't seems to make difference with DirtyHeightmapRegion, waiting for readback anyways

				RenderTexture.active = bacRenTex;
			}

			public IEnumerator ApplyRoutine (Terrain terrain)
			{
				if (terrain==null || terrain.Equals(null) || terrain.terrainData==null) yield break; //chunk removed during apply
				TerrainData data = terrain.terrainData;

				Profiler.BeginSample("PrepareTexApply");
				RectInt texRect = new RectInt(0,0,res,res);

				if (tempTex == null ||  tempTex.width != res)
					tempTex = new Texture2D(res, res, TextureFormat.RFloat, mipChain:false, linear:true);
				Profiler.BeginSample("Load Tex");
				tempTex.LoadRawTextureData(texBytes);
				tempTex.Apply(updateMipmaps:false);
				Profiler.EndSample();

				if (renTex == null ||  renTex.width != res)
				#if UNITY_2019_2_OR_NEWER
					renTex = new RenderTexture(res,res,32, RenderTextureFormat.RFloat, mipCount:0);
				#else
					renTex = new RenderTexture(res,res,32, RenderTextureFormat.RFloat);
				#endif
					
				Profiler.BeginSample("Blit Tex");
				Graphics.Blit(tempTex, renTex);
				Profiler.EndSample();

				//no resize algorithm
				Profiler.BeginSample("Change Res");
				Vector3 terrainSize = data.size;
				//data.heightmapResolution = res;
				data.size = new Vector3(terrainSize.x, height, terrainSize.z);
				ApplySplitData.FastHeightmapResize(terrain, res, new Vector3(terrainSize.x, height, terrainSize.z));
				terrain.groupingID = res;
				Profiler.EndSample();

				//splitting in parts
				int numSplits = (int)(res/splitSize);
				if (numSplits*splitSize < res) numSplits++;
				Profiler.EndSample();

				for (int sx = 0; sx<numSplits; sx++)
					for (int sz = 0; sz<numSplits; sz++)
					{
						Profiler.BeginSample("Apply Heightmap");

						Profiler.BeginSample("Set active rendertex");
						RenderTexture bacRenTex = RenderTexture.active;
						RenderTexture.active = renTex;
						Profiler.EndSample();

						RectInt rect = new RectInt(sx*splitSize, sz*splitSize, splitSize, splitSize);
						rect.xMax = Mathf.Min(rect.xMax, res);
						rect.yMax = Mathf.Min(rect.yMax, res);

						Profiler.BeginSample("Copy to heightmap");
						data.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);
						Profiler.EndSample();

						Profiler.BeginSample("Dirty heightmap");
						data.DirtyHeightmapRegion(rect, TerrainHeightmapSyncControl.HeightAndLod); //or seems a bit faster on re-setting height to already existing terrains. IDK
						Profiler.EndSample();

						Profiler.BeginSample("SyncHeightmap");
						//data.SyncHeightmap(); //doesn't seems to make difference with DirtyHeightmapRegion, waiting for readback anyways
						Profiler.EndSample();

						Profiler.BeginSample("Returning rendertex");
						RenderTexture.active = bacRenTex;
						Profiler.EndSample();

						Profiler.EndSample();

						yield return null;
					}

				#if MM_DEBUG
				Log.Add("HeightOut Applied Texture");
				#endif
			}

			public static ApplyTexData Empty
				{get{ return new ApplyTexData() { texBytes=new byte[16], height=0, splitSize = 5 }; }}

			public int Resolution {get{ return (int)Mathf.Sqrt(texBytes.Length/4); }}
		}
		#endif
	}
}