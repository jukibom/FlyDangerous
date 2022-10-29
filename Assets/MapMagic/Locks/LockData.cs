using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Terrains;
using MapMagic.Nodes;

namespace MapMagic.Locks
{
	//Could be unified with ITerrainData, but actually there is not a single reason to do so

	public interface ILockData
	{
		void Read (Terrain terrain, Lock lk);
		void WriteInThread (IApplyData applyData);
		void WriteInApply (Terrain terrain, bool resizeTerrain);
		void ApplyHeightDelta (Matrix src, Matrix dst);
		void ResizeFrom (ILockData lockData);

		//void Write (TypeDict apply, out float heightDelta, float relativeHeight); //writes heightdelta for height locks
		//void Write (TypeDict apply, float heightDelta); //reads height delta for others
		//void Process (); //do blending, circle extend, height delta calculation
		
	}

	public class LockDataSet
	{
		private ILockData[] lockDatas;

		public LockDataSet ()
		{
			//default lock datas
			List<ILockData> lockDatasList = new List<ILockData> {
				new HeightData(),
				new TexturesData(),
				new GrassData() };
				//new TreesData(),
				//new ObjectsData() };
				//these lock datas are in Objects module

			//other lock datas (objects)
			Type[] allLockTypes = typeof(ILockData).Subtypes();

			for (int i=0; i<allLockTypes.Length; i++)
			{
				if (lockDatasList.FindIndex(ld => ld.GetType() == allLockTypes[i]) < 0)
					lockDatasList.Add(Activator.CreateInstance(allLockTypes[i]) as ILockData);
			}
				
			lockDatas = lockDatasList.ToArray();
		}

		private HeightData HeightData { get{ return (HeightData)lockDatas[0]; } }


		public void Read (Terrain terrain, Lock lk) 
		{
			for (int i=0; i<lockDatas.Length; i++)
				lockDatas[i].Read(terrain, lk);
		}

		/*public void Process (Dictionary<Type,IApplyData> apply, float heightDelta)
		{
			for (int i=0; i<lockDatas.Length; i++)
				lockDatas[i].Process(apply, heightDelta);
		}*/

		public static void Resize (LockDataSet src, LockDataSet dst)
		{
			for (int i=0; i<dst.lockDatas.Length; i++)
				dst.lockDatas[i].ResizeFrom(src.lockDatas[i]);
		}

		public void WriteInThread (IApplyData applyData, bool relativeHeight) 
		{
			if (!relativeHeight)
			{
				for (int i=0; i<lockDatas.Length; i++)
					lockDatas[i].WriteInThread(applyData);
			}

			else
			{
				//if height - applying height and amending other lock reads with height delta
				//NB that height finalize always go first by design
				if (applyData is Nodes.MatrixGenerators.HeightOutput200.IApplyHeightData applyHeightData)
				{
					(Matrix heightSrc, Matrix heightDst) = HeightData.WriteWithHeightDelta(applyHeightData);

					for (int i=1; i<lockDatas.Length; i++)
						lockDatas[i].ApplyHeightDelta(heightSrc, heightDst);
				}

				//or write others
				else
				{
					for (int i=1; i<lockDatas.Length; i++)
						lockDatas[i].WriteInThread(applyData);
				}
			}
		}

		public void WriteInApply (Terrain terrain, bool resizeTerrain) 
		{
			for (int i=0; i<lockDatas.Length; i++)
				lockDatas[i].WriteInApply(terrain, resizeTerrain);
		}


		//public float GetHeightDelta (Dictionary<Type,IApplyData> apply)
		//	{ return HeightData.GetHeightDelta(apply); }
	}


	public struct CoordCircle
	{
		public Coord center; 
		public int radius;
		public int transition;
		public int fullRadius;
		public CoordRect rect; //not the same as center+radius since it could be clamped by terrain

		public CoordCircle (Terrain terrain, int resolution, Vector3 worldCenter, float worldRadius, float worldTransition)
		{
			Vector3 terrainPos = terrain.transform.parent.localPosition; //taking tile local position, tot terrain one!
			TerrainData terrainData = terrain.terrainData;
			Vector3 terrainSize = terrainData.size;

			center = Coord.Round(
				(worldCenter.x-terrainPos.x)/terrainSize.x * resolution, 
				(worldCenter.z-terrainPos.z)/terrainSize.z * resolution);
			radius = (int)(worldRadius/terrainSize.x * resolution);
			transition = (int)(worldTransition/terrainSize.x * resolution);
			fullRadius = radius+transition;
			
			rect = new CoordRect(center, radius+transition);
			rect = CoordRect.Intersected(rect, new CoordRect(0,0,resolution,resolution));
		}
	}
}