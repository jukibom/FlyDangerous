using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Core;
using MapMagic.Products;

namespace MapMagic.Nodes.MatrixGenerators
{
	[System.Serializable]
	[GeneratorMenu (menu="Map/Initial", name ="Constant", iconName="GeneratorIcons/Constant", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Constant")]
	public class Constant200 : Generator, IOutlet<MatrixWorld>
	{
		[Val("Level", min:0)] public float level;

		public override void Generate (TileData data, StopToken stop) 
		{
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			matrix.Fill(level);
			data.StoreProduct(this, matrix);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Initial", name ="Noise", iconName="GeneratorIcons/Noise", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Noise")]
	public class Noise200 : Generator, IOutlet<MatrixWorld>
	{
		public enum Type { Unity=0, Linear=1, Perlin=2, Simplex=3 };
		[Val("Type")] public Type type = Type.Perlin;

		[Val("Seed")]		public int seed = 12345;
		[Val("Intensity")]	public float intensity = 1f;
		[Val("Size")]		public float size = 200f;
		[Val("Detail")]		public float detail = 0.5f;
		[Val("Turbulence")] public float turbulence = 0f;
		[Val("Offset")]		public Vector2D offset = new Vector2D(0,0);
		

		public override void Generate (TileData data, StopToken stop) 
		{
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			Noise noise = new Noise(data.random, seed);

            #if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP) 
			if (type!=Type.Unity) //obviously, no native for unity noise
				GeneratorNoise200(matrix, noise, stop,
					(int)type, intensity, size, detail, turbulence, offset.x, offset.z, 
					matrix.worldPos.x, matrix.worldPos.z, matrix.worldSize.x, matrix.worldSize.z);
			else
				Noise(matrix, noise, stop, (int)type, intensity, size, detail, turbulence, offset);
			#else
			Noise(matrix, noise, stop, (int)type, intensity, size, detail, turbulence, offset);
	        #endif

			data.StoreProduct(this, matrix); 
		}

		[DllImport("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "GeneratorNoise200")]
		private static extern float GeneratorNoise200(Matrix matrix, Noise noise, StopToken stop,
	        int type, float intensity, float size, float detail, float turbulence, float offsetX, float offsetZ,
			float worldRectPosX, float worldRectPosZ, float worldRectSizeX, float worldRectSizeZ);

		private static void Noise (MatrixWorld matrix, Noise noise, StopToken stop,
			int type, float intensity, float size, float detail, float turbulence, Vector2D offset)
		{
			int iterations = (int)Mathf.Log(size,2) + 1; //+1 max size iteration

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
			{
				for (int z=min.z; z<max.z; z++)
				{ 
					Vector2D relativePos = new Vector2D (
						(float)(x - matrix.rect.offset.x) / (matrix.rect.size.x-1),
						(float)(z - matrix.rect.offset.z) / (matrix.rect.size.z-1) );

					Vector2D worldPos = new Vector2D (
						relativePos.x*matrix.worldSize.x + matrix.worldPos.x,
						relativePos.z*matrix.worldSize.z + matrix.worldPos.z );

				    float val = noise.Fractal(worldPos.x+offset.x, worldPos.z+offset.z, size, iterations, detail,turbulence,(int)type);
					val *= intensity;

					if (val < 0) val = 0; //before mask?
					if (val > 1) val = 1;

					matrix[x,z] += val;
				}

				if (stop!=null && stop.stop) return; //checking stop every x line
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Initial", name ="Voronoi", iconName="GeneratorIcons/Voronoi", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Voronoi")]
	public class Voronoi200 : Generator, IOutlet<MatrixWorld>
	{
		[Val("Intensity")]	public float intensity = 1f;
		[Val("Cell Size", min:0)]	public int cellSize = 50;
		[Val("Uniformity")]	public float uniformity = 0;
		[Val("Seed")]	public int seed = 12345;
		public enum BlendType { flat, closest, secondClosest, cellular, organic }
		[Val("Blend Type")]	public BlendType blendType = BlendType.cellular;


		public override void Generate (TileData data, StopToken stop) 
		{
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			Voronoi(matrix, new Noise(data.random,seed), stop);
			data.StoreProduct(this, matrix);
		}


		public void Voronoi (MatrixWorld matrix, Noise random, StopToken stop=null)
		{
			Vector3 matrixPos = matrix.worldPos;
			Vector3 matrixSize = matrix.worldSize;

			CoordRect rect = CoordRect.WorldToPixel((Vector2D)matrixPos, (Vector2D)matrixSize, (Vector2D)cellSize);  //will convert to cells as well
			matrixPos = new Vector3(rect.offset.x*cellSize, matrixPos.y, rect.offset.z*cellSize);  //modifying worldPos/size too
			matrixSize = new Vector3(rect.size.x*cellSize, matrixSize.y, rect.size.z*cellSize);

			rect.offset -= 1; rect.size += 2; //leaving 1-cell margins
			matrixPos.x -= cellSize; matrixPos.z -= cellSize;
			matrixSize.x += cellSize*2; matrixSize.z += cellSize*2;
				
			PositionMatrix posMatrix = new PositionMatrix(rect, matrixPos, matrixSize);

			posMatrix.Scatter(uniformity, random);
			posMatrix = posMatrix.Relaxed();

			float relativeIntensity = intensity * (matrix.worldSize.x / cellSize) * 0.05f;

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max; 
			for (int x=min.x; x<max.x; x++)
			{
				if (stop!=null && stop.stop) return; //checking stop every x line

				for (int z=min.z; z<max.z; z++)
				{
					//Vector3 worldPos = matrix.PixelToWorld(x,z);

					Vector2D relativePos = new Vector2D (
						(float)(x - matrix.rect.offset.x) / (matrix.rect.size.x-1),
						(float)(z - matrix.rect.offset.z) / (matrix.rect.size.z-1) );

					Vector2D worldPos = new Vector2D (
						relativePos.x*matrix.worldSize.x + matrix.worldPos.x,
						relativePos.z*matrix.worldSize.z + matrix.worldPos.z );

					Vector3 closest; Vector3 secondClosest;
					float minDist; float secondMinDist;
					posMatrix.GetTwoClosest((Vector3)worldPos, out closest, out secondClosest, out minDist, out secondMinDist); 

					float val = 0;
					switch (blendType)
					{
						case BlendType.flat: val = closest.y; break;
						case BlendType.closest: val = minDist / (matrix.worldSize.x*16); break;  //(MapMagic.instance.resolution*16); //TODO: why 16?
						case BlendType.secondClosest: val = secondMinDist / (matrix.worldSize.x*16); break;
						case BlendType.cellular: val = (secondMinDist-minDist) / (matrix.worldSize.x*16); break;
						case BlendType.organic: val = (secondMinDist+minDist)/2 / (matrix.worldSize.x*16); break;
					}

					matrix[x,z] += val*relativeIntensity;
				}
			}
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map/Initial", name ="Simple Form", iconName="GeneratorIcons/SimpleForm", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/SimpleForm")]
	public class SimpleForm200 : Generator, IOutlet<MatrixWorld>, ISceneGizmo
	{
		public enum FormType { GradientX, GradientZ, Pyramid, Cone }
		[Val("Type")]		public FormType type = FormType.Cone;
		[Val("Intensity")]	public float intensity = 1;
		[Val("Scale")]		public float scale = 1;
		[Val("Offset")]		public Vector2 offset;
		[Val("Wrap")]		public CoordRect.TileMode wrap = CoordRect.TileMode.Tile;


		public bool hideDefaultToolGizmo { get; set; }
		public bool drawOffsetScaleGizmo = false;

		public override void Generate (TileData data, StopToken stop) 
		{
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			SimpleForm(matrix, (Vector2D)offset, scale * (Vector2D)data.area.active.worldSize, stop); //size is chunk-size relative
			matrix.Clamp01();
			data.StoreProduct(this, matrix);
		}

		public void SimpleForm (MatrixWorld matrix, Vector2D formOffset, Vector2D formSize, StopToken stop=null)
		{
			Vector2D center = formSize/2 + formOffset;
			float radius = Mathf.Min(formSize.x,formSize.z) / 2f;

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;

			for (int x=min.x; x<max.x; x++)
			{
				if (stop!=null && stop.stop) return;

				for (int z=min.z; z<max.z; z++)
				{
					//Vector3 worldPos = matrix.PixelToWorld(x,z);

					Vector2D relativePos = new Vector2D (
						(float)(x - matrix.rect.offset.x) / (matrix.rect.size.x-1),
						(float)(z - matrix.rect.offset.z) / (matrix.rect.size.z-1) );

					Vector2D worldPos = new Vector2D (
						relativePos.x*matrix.worldSize.x + matrix.worldPos.x,
						relativePos.z*matrix.worldSize.z + matrix.worldPos.z );

					Vector2D formPos = Tile(worldPos, formOffset, formSize, wrap);

					float val = 0;
					switch (type)
					{
						case FormType.GradientX:
							val = (formPos.x-formOffset.x) / formSize.x;
							break;
						case FormType.GradientZ:
							val = (formPos.z-formOffset.z) / formSize.z;
							break;
						case FormType.Pyramid:
							float valX = (formPos.x-formOffset.x) / formSize.x; if (valX > 1-valX) valX = 1-valX;
							float valZ = (formPos.z-formOffset.z) / formSize.z; if (valZ > 1-valZ) valZ = 1-valZ;
							val = valX<valZ? valX*2 : valZ*2;
							break;
						case FormType.Cone:
							val = 1 - ((center-formPos).Magnitude)/radius;
							if (val<0) val = 0;
							break;
					}

					matrix[x,z] = val*intensity;
				}
			}
		}

		public Vector2D Tile (Vector2D pos, Vector2D rectOffset, Vector2D rectSize, CoordRect.TileMode tileMode)
		/// Tiling pos in a 0-size rect
		/// Similar to CoordRect.Tile
		{
			pos.x -= rectOffset.x; //tile requires 0-based coordinates
			pos.z -= rectOffset.z;

			switch (tileMode)
			{
				case CoordRect.TileMode.Clamp:
					if (pos.x < 0) pos.x = 0; 
					if (pos.x >= rectSize.x) pos.x = rectSize.x - 1;
					if (pos.z < 0) pos.z = 0; 
					if (pos.z >= rectSize.z) pos.z = rectSize.z - 1;
					break;

				case CoordRect.TileMode.Tile:
					pos.x = pos.x % rectSize.x; 
					if (pos.x < 0) pos.x= rectSize.x + pos.x;
					pos.z = pos.z % rectSize.z; 
					if (pos.z < 0) pos.z= rectSize.z + pos.z;
					break;

				case CoordRect.TileMode.PingPong:
					pos.x = pos.x % (rectSize.x*2); 
					if (pos.x < 0) pos.x = rectSize.x*2 + pos.x; 
					if (pos.x >= rectSize.x) pos.x = rectSize.x*2 - pos.x - 1;

					pos.z = pos.z % (rectSize.z*2); 
					if (pos.z<0) pos.z=rectSize.z*2 + pos.z; 
					if (pos.z>=rectSize.z) pos.z = rectSize.z*2 - pos.z - 1;
					break;
			}

			pos.x += rectOffset.x;
			pos.z += rectOffset.z;

			return pos;
		}

		/*[Val]
		public void OnGUI_SetInScene (object graphBox)
		{
			UI.Empty(Size.LinePixels(5));
			UI.CheckButton(ref drawOffsetScaleGizmo, "Set In Scene");
		}*/

		public void DrawGizmo ()
		{
			//hideDefaultToolGizmo = drawOffsetScaleGizmo;
			//if (drawOffsetScaleGizmo)
			//	GeneratorUI.DrawNodeOffsetSize(ref offset, ref scale, nodeToChange:this);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map/Initial", name ="Import", iconName="GeneratorIcons/Import", disengageable = true, disabled = false, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Import")]
	public class Import200 : Generator, IOutlet<MatrixWorld>, ISceneGizmo
	{
		[Val("Map", priority = 10, type = typeof(MatrixAsset))]	public MatrixAsset matrixAsset;

		[Val("Wrap Mode", priority = 4)]	public CoordRect.TileMode wrapMode = CoordRect.TileMode.Clamp;
		[Val("Scale", priority = 3)]		public float scale = 1;
		[Val("Offset", priority = 2)]		public Vector2 offset;
		

		public bool hideDefaultToolGizmo { get; set; }
		public bool drawOffsetScaleGizmo = false;

		static Import200()
		{
			MatrixAsset.OnReloaded += OnMatrixAssetReloaded_ReGenerate;
		}

		public static void OnMatrixAssetReloaded_ReGenerate (MatrixAsset ma)
		/// If this matrixAsset is used then clearing this node in all related mapmagics and starting generate
		{
			MapMagicObject[] mapMagics = GameObject.FindObjectsOfType<MapMagicObject>();
			foreach (MapMagicObject mapMagic in mapMagics)
			{
				bool containsMa = false;
				if (mapMagic.graph != null)
					foreach (Import200 gen in mapMagic.graph.GeneratorsOfType<Import200>())
					{
						if (gen.matrixAsset == ma) 
						{
							mapMagic.Clear(gen);

							containsMa = true;
							break;
						}
					}

				if (containsMa)
					mapMagic.StartGenerate();
			}
		}


		public override void Generate (TileData data, StopToken stop) 
		{
			if (stop!=null && stop.stop) return;
			if (matrixAsset == null || matrixAsset.matrix==null || matrixAsset.matrix.rect.Count==0  || !enabled) { data.RemoveProduct(this); return; }

			//preparing matrices (note that their world coordinates use the same coordsys)
			MatrixWorld dstMatrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);

			MatrixWorld srcMatrix = new MatrixWorld(
				matrixAsset.matrix,
				(Vector2D)offset,
				data.area.active.worldSize * scale, //since it should be equal to acvtive area when scale 1
				data.globals.height);

			if (srcMatrix.rect.size == data.area.active.rect.size  &&  scale<1.0001f  &&  scale>0.999f)
			//if active resolution matches src matrix (common case when importing map exported from this terrain)
				Matrix.ReadMatrix(srcMatrix, dstMatrix, wrapMode);

			else if (srcMatrix.PixelSize.x >= dstMatrix.PixelSize.x)
				ImportWithEnlarge(srcMatrix, dstMatrix, wrapMode, stop);

			else
				ImportWithDownscale(srcMatrix, dstMatrix, wrapMode, stop);

			data.StoreProduct(this, dstMatrix);
		}


		public void ImportWorldInerpolated (MatrixWorld src, MatrixWorld dst, StopToken stop)
		/// Takes a part of raw (src) and expands it to fill tile (dst)
		{
			Coord min = dst.rect.Min; Coord max = dst.rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					Vector3 worldPos = dst.PixelToWorld(x,z);
					Coord srcPos = src.WorldToPixel(worldPos.x, worldPos.z);

					if (!src.rect.Contains(srcPos))
						continue;

					dst[x,z] = src.GetWorldInterpolatedValue(worldPos.x, worldPos.z); //src[srcPos];
				}
		}


		public static void ImportWithEnlarge (MatrixWorld src, MatrixWorld dst, CoordRect.TileMode wrapMode, StopToken stop)
		/// Takes a part of raw (src) and expands it to fill tile (dst)
		/// The new function, but doesn't work for some reason (no offset)
		{
			//finding read area - dst rect in src coordsys
			Vector2D worldRatio = (Vector2D)dst.worldSize / (Vector2D)src.worldSize;
			Vector2D rectRatio = (Vector2D)dst.rect.size / (Vector2D)src.rect.size;

			Vector2D ratio = worldRatio / rectRatio;
			Vector2D readPos = (Vector2D)dst.rect.offset * ratio  -  (Vector2D)src.worldPos / src.PixelSize; 
			Vector2D readSize = (Vector2D)dst.rect.size * ratio; //was /ratio, but we need dst-1 for size

			//int pixelMarg = Mathf.Max((int)(0.5f/ratio.x), 2); //2 initially, but increases for big scale values  //can't rescale when src dst pixelsizes are about the same
			int pixelMarg = (int)(0.5f/ratio.x); //2 initially, but increases for big scale values
			CoordRect readRect = new CoordRect(Coord.Floor(readPos)-pixelMarg, (Coord)readSize+pixelMarg*2);

			if (stop!=null && stop.stop) return;
			Matrix readMatrix = new Matrix(readRect);
			Matrix.ReadMatrix(src, readMatrix, wrapMode);

			if (stop!=null && stop.stop) return;
			MatrixOps.Upsize(readMatrix, readPos, readSize, dst);
		}


		public static void ImportWithDownscale (MatrixWorld src, MatrixWorld dst, CoordRect.TileMode wrapMode, StopToken stop)
		/// Downsizes raw (src) and copies it to tile (dst)
		{
			//src (raw) rect in tile pixels coordsys
			CoordRect dstPartRect = dst.WorldRectToPixels((Vector2D)src.worldPos, (Vector2D)src.worldSize);
				//using matrix size = full area here

			//creating a small matrix
			Matrix smallMatrix = new Matrix(dstPartRect);

			if (stop!=null && stop.stop) return;
			MatrixOps.Downsize(src, smallMatrix);
			
			//writing small matrix to dst
			if (stop!=null && stop.stop) return;
			Matrix.ReadMatrix(smallMatrix, dst, wrapMode);
		}

		/*[Val]
		public void OnGUI_SetInScene (object graphBox)
		{
			UI.Empty(Size.LinePixels(5));
			UI.CheckButton(ref drawOffsetScaleGizmo, "Set In Scene");
		}*/

		public void DrawGizmo ()
		{
			//hideDefaultToolGizmo = drawOffsetScaleGizmo;
			//if (drawOffsetScaleGizmo)
			//	GeneratorUI.DrawNodeOffsetSize(ref offset, ref scale, nodeToChange:this);
		}
	}


	[Serializable]
	[GeneratorMenu(
		menu = "Map/Initial", 
		name = "Spot", 
		section=2, 
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/Spot",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Spot")]
	public class Spot210 : Generator, IOutlet<MatrixWorld>
	/// Creates a single round spot
	/// Analog of Objects.Stroke, but made to make brush possible without objects
	{
		[Val("Intensity")] public float intensity = 1;
		[Val("Position")] public Vector2D position;
		[Val("Radius")] public float radius = 30;
		[Val("Hardness")] public float hardness = 0.5f;


		public override void Generate (TileData data, StopToken stop) 
		{ 
			if (stop!=null && stop.stop) return;
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			float pixelSize= matrix.PixelSize.x;
			Vector2D pixelPos = position/pixelSize;
			float pixelRadius = radius/pixelSize;

			matrix.Stroke(pixelPos, pixelRadius, hardness);

			if (intensity < 0.999f  ||  intensity > 1.001f)
				matrix.Multiply(intensity);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, matrix);
		}
	}


	[System.Serializable]
	//[GeneratorMenu (menu="Map/Test", name ="World To Pixel", iconName="GeneratorIcons/Constant", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	//[GeneratorMenu (menu=null, name ="World To Pixel", iconName="GeneratorIcons/Constant", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class WorldToPixelTest : Generator, IOutlet<MatrixWorld>, IPrepare
	{
		[Val("Level")] public float level;
		[Val("Grid")] public float grid;
		[Val("Transform", allowSceneObject =true)] public Transform tfm;
		[Val("Interpolated")] public bool interpolated;

		private Vector3 pos;

		public void Prepare (TileData data, Terrain terrain)
		{
			pos = tfm.position;
		}

		public override void Generate (TileData data, StopToken stop) 
		{
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);

			for (int x=matrix.rect.offset.x; x<matrix.rect.offset.x+matrix.rect.size.x; x++)
				for (int z=matrix.rect.offset.z; z<matrix.rect.offset.z+matrix.rect.size.z; z++)
				{
					matrix[x,z] = grid * (x%2) * (z%2);
				}

			if (interpolated) 
			{
				Vector3 vec = matrix.WorldToPixelInterpolated(pos.x, pos.z);
				Coord coord = Coord.Floor((Vector2D)vec);
				matrix[coord] = level;
				matrix[coord.x+1, coord.z] = level*0.75f;
				matrix[coord.x, coord.z+1] = level*0.5f;
				matrix[coord.x+1, coord.z+1] = level*0.25f;

				Vector3 retVec = matrix.PixelToWorld(vec.x, vec.z);
				DebugGizmos.DrawDot("WorldToPixelTest", retVec, 6, Color.green);
			}
			else
			{
				Coord coord = matrix.WorldToPixel(pos.x, pos.z);
				matrix[coord] = level;

				Vector3 retVec = matrix.PixelToWorld(coord.x, coord.z);
				DebugGizmos.DrawDot("WorldToPixelTest", retVec, 6, Color.green);
			}

			data.StoreProduct(this, matrix);
		}
	}


	[System.Serializable]
	#if MM_DEBUG
	[GeneratorMenu (menu="Map/Test", name ="MultiProperties", iconName="GeneratorIcons/Constant", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	//[GeneratorMenu (menu=null, name ="MultiProperties", iconName="GeneratorIcons/Constant", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	#endif
	public class MultiPropertiesTest : Generator, IOutlet<MatrixWorld>, IPrepare
	{
		[Val("Bool")] public bool boolean;
		[Val("Vector2")] public Vector2 vector2;
		[Val("Vector3")] public Vector3 vector3;
		[Val("Vector4")] public Vector4 vector4;
		[Val("Color")] public Color color;
		[Val("Texture")] public Texture2D texture;
		[Val("Terrain Layer")] public TerrainLayer terrainLayer;

		public void Prepare (TileData data, Terrain terrain) 
		{

		}

		public override void Generate (TileData data, StopToken stop) 
		{
			Debug.Log("Bool " + boolean.ToString() + "\n" +
				"Vector2 " + vector2.ToString() + "\n" +
				"Vector3 " + vector3.ToString() + "\n" +
				"Vector4 " + vector4.ToString() + "\n" +
				"Color " + color.ToString() + "\n" +
				"Texture " + (texture!=null ? "assigned" : "null") + "\n" +  //ToString could not be called not in main thread
				"Terrain Layer " + (terrainLayer!=null ? "assigned" : "null") );
		}
	}
}
