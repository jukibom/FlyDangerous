using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using MapMagic.Products;

namespace MapMagic.Nodes.MatrixGenerators
{

	/*[System.Serializable]
	[GeneratorMenu (menu="Map/Input", name ="Textures In", section=1, disengageable = true)]
	public class TexturesInput : Generator, IOutlet<MatrixWorld>, ITerrainReader
	{
		[Val(name="Channel")] public int channel = 0;

		public void CheckReadTerrain (Terrain terrain, Results results)
		{
			if (results.terrainReads.ContainsKey(typeof(SplatData))) return; //already read

			SplatData data = new SplatData();
			data.ReadFromTerrain(terrain);
			results.terrainReads.Add(typeof(SplatData), data);
		}

		public override void Generate (Results results, Area area, int seed, StopCallback stop)
		{
			if (!enabled) { results.SetProduct(this, null); return; }  //should set anything to mark as generated

			SplatData data = null;
			if (results.terrainReads.ContainsKey(typeof(SplatData))) data = (SplatData)results.terrainReads[typeof(SplatData)];
			if (data==null) { results.SetProduct(this, null); return; }

			if (stop!=null && stop(0)) return; 

			MatrixWorld matrix = new MatrixWorld(area.full.resolution, area.full.position, area.full.size);
			Floats3DtoMatrix(data.splats3D, channel, matrix, area);

			if (stop!=null && stop(0)) return;
			results.SetProduct(this, matrix);
		}

		public void Floats3DtoMatrix (float[,,] splats3D, int channel, Matrix matrix, Area area)
		{
			int splatsResolution = splats3D.GetLength(0);
			int margins = area.Margins;
			
			//simple case if resolution match
			if (area.active.resolution == splatsResolution)
			{
				for (int x=0; x<splatsResolution; x++)
					for (int z=0; z<splatsResolution; z++)
					{
						float val = splats3D[z,x, channel];
						matrix.array[(z+margins)*matrix.rect.size.x + x+margins] = val; //do not use matrix[x,z] since x/z are 0-based
					}

				//TODO: fill margins
			}
		
			//interpolated if resolution doesn't match
			else
			{
				Matrix tmpMatrix = new Matrix( new Coord(0,0), new Coord(splatsResolution,splatsResolution) );

				for (int x=0; x<splatsResolution; x++)
					for (int z=0; z<splatsResolution; z++)
						tmpMatrix.array[z*splatsResolution + x] = splats3D[z,x, channel];
				
				Debug.Log("could not read heightmap - resolutions mismatch");

				//TODO: interpolated
			}
		}
	}*/

}
