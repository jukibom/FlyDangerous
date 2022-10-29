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
	[GeneratorMenu (menu="Map/Modifiers", name ="Curve", iconName="GeneratorIcons/Curve", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Curve")]
	public class Curve200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld> 
	{
		public Curve curve = new Curve( new Vector2(0,0), new Vector2(1,1) );   

		[NonSerialized] public float[] histogram = null;
		public const int histogramSize = 256;


		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			if (data.isPreview)
				histogram = src.Histogram(histogramSize, max:1, normalize:true);

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src);

			if (stop!=null && stop.stop) return;
			curve.Refresh(updateLut:true);

			if (stop!=null && stop.stop) return;
			//for (int i=0; i<dst.arr.Length; i++) dst.arr[i] = curve.EvaluateLuted(dst.arr[i]);
			dst.UniformCurve(curve.lut);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}
	}


	/*[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="nonExisting", iconName="GeneratorIcons/Curve", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Curve")]
	public class NonExisting200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld> 
	{
		[Val(name="Intensity")] public float brightness = 0f;
		[Val(name="Contrast")] public float contrast = 1f;

		public Curve curve = new Curve( new Vector2(0,0), new Vector2(1,1) );   

		[NonSerialized] public float[] histogram = null;
		public const int histogramSize = 256;


		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			if (data.isPreview)
				histogram = src.Histogram(histogramSize, max:1, normalize:true);

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src);

			if (stop!=null && stop.stop) return;
			curve.Refresh(updateLut:true);

			if (stop!=null && stop.stop) return;
			//for (int i=0; i<dst.arr.Length; i++) dst.arr[i] = curve.EvaluateLuted(dst.arr[i]);
			dst.UniformCurve(curve.lut);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}
	}
	*/


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Levels", iconName="GeneratorIcons/Levels", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Levels")]
	public class Levels200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld> 
	{
		//public Vector2 min = new Vector2(0,0);
		//public Vector2 max = new Vector2(1,1);
		public float inMin = 0;
		public float inMax = 1;
		public float gamma = 1f; //min/max bias. 0 for min 2 for max, 1 is straight curve

		public float outMin = 0;
		public float outMax = 1;

		[NonSerialized] public float[] histogram = null;
		public const int histogramSize = 256;

		public bool guiParams = false;


		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			if (data.isPreview)
				histogram = src.Histogram(histogramSize, max:1, normalize:true);

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src);

			if (stop!=null && stop.stop) return;
			dst.Levels(inMin, inMax, gamma, outMin, outMax);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Contrast", iconName="GeneratorIcons/Contrast", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Contrast")]
	public class Contrast200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld> 
	{
		[Val(name="Intensity")] public float brightness = 0f;
		[Val(name="Contrast")] public float contrast = 1f;


		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			//src.Histogram(256, max:1, normalize:true);
			//if (data.isPreview)
			//	histogram = src.Histogram(histogramSize, max:1, normalize:true);

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src);

			if (stop!=null && stop.stop) return;
			dst.BrighnesContrast(brightness, contrast);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Unity Curve", iconName="GeneratorIcons/UnityCurve", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/UnityCurve")]
	public class UnityCurve200 : Generator, IMultiInlet, IOutlet<MatrixWorld> 
	{
		[Val("Inlet", "Inlet")] public readonly IInlet<MatrixWorld> srcIn = new Inlet<MatrixWorld>();
		[Val("Mask", "Inlet")]	public readonly IInlet<MatrixWorld> maskIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets() { yield return srcIn; yield return maskIn; }

		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public Vector2 min = new Vector2(0,0);
		public Vector2 max = new Vector2(1,1);

		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld src = data.ReadInletProduct(srcIn);
			MatrixWorld mask = data.ReadInletProduct(maskIn);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			//preparing output
			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src);

			//curve
			if (stop!=null && stop.stop) return;
			AnimCurve c = new AnimCurve(curve);
			for (int i=0; i<dst.arr.Length; i++) dst.arr[i] = c.Evaluate(dst.arr[i]);

			//mask
			if (stop!=null && stop.stop) return;
			if (mask != null) dst.InvMix(src,mask);

			data.StoreProduct(this, dst);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Mask", iconName="GeneratorIcons/MapMask", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/MapMask")]
	public class Mask200 : Generator, IMultiInlet, IOutlet<MatrixWorld> 
	{
		[Val("Input A", "Inlet")]	public readonly Inlet<MatrixWorld> aIn = new Inlet<MatrixWorld>();
		[Val("Input B", "Inlet")]	public readonly Inlet<MatrixWorld> bIn = new Inlet<MatrixWorld>();
		[Val("Mask", "Inlet")]	public readonly Inlet<MatrixWorld> maskIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return aIn; yield return bIn; yield return maskIn; }

		[Val("Invert")]	public bool invert = false;


		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			MatrixWorld matrixA = data.ReadInletProduct(aIn);
			MatrixWorld matrixB = data.ReadInletProduct(bIn);
			MatrixWorld mask = data.ReadInletProduct(maskIn);
			if (matrixA == null || matrixB == null) return; 
			if (!enabled || mask == null) { data.StoreProduct(this, matrixA); return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(matrixA);

			if (stop!=null && stop.stop) return;
			dst.Mix(matrixB, mask, 0, 1, invert, false, 1);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Blend", iconName="GeneratorIcons/Blend", disengageable = true, colorType = typeof(MatrixWorld), 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Blend")]
	public class Blend200 : Generator, IMultiInlet, IOutlet<MatrixWorld>
	{
		public class Layer
		{
			public readonly Inlet<MatrixWorld> inlet = new Inlet<MatrixWorld>();
			public BlendAlgorithm algorithm = BlendAlgorithm.add;
			public float opacity = 1;
			public bool guiExpanded = false;
		}

		public Layer[] layers = new Layer[] { new Layer(), new Layer() };
		public Layer[] Layers => layers; 
		public void SetLayers(object[] ls) => layers = Array.ConvertAll(ls, i=>(Layer)i);

		public IEnumerable<IInlet<object>> Inlets() 
		{ 
			for (int i=0; i<layers.Length; i++)
				yield return layers[i].inlet;
		}

		public override void Generate (TileData data, StopToken stop) 
		{
			if (stop!=null && stop.stop) return;
			if (!enabled) return;
			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			
			if (stop!=null && stop.stop) return;

			if (stop!=null && stop.stop) return;
				for (int i = 0; i < layers.Length; i++)
			{
				Layer layer = layers[i];
				if (layer.inlet == null) continue;

				MatrixWorld blendMatrix = data.ReadInletProduct(layer.inlet);
				if (blendMatrix == null) continue;

				Blend(matrix, blendMatrix, layer.algorithm, layer.opacity);
			}
			
			data.StoreProduct(this, matrix);
		}


		public enum BlendAlgorithm {
			mix=0, 
			add=1, 
			subtract=2, 
			multiply=3, 
			divide=4, 
			difference=5, 
			min=6, 
			max=7, 
			overlay=8, 
			hardLight=9, 
			softLight=10} 
			
		public static void Blend (Matrix m1, Matrix m2, BlendAlgorithm algorithm, float opacity=1)
		{
			switch (algorithm)
			{
				case BlendAlgorithm.mix: default: m1.Mix(m2, opacity); break;
				case BlendAlgorithm.add: m1.Add(m2, opacity); break;
				case BlendAlgorithm.subtract: m1.Subtract(m2, opacity); break;
				case BlendAlgorithm.multiply: m1.Multiply(m2, opacity); break;
				case BlendAlgorithm.divide: m1.Divide(m2, opacity); break;
				case BlendAlgorithm.difference: m1.Difference(m2, opacity); break;
				case BlendAlgorithm.min: m1.Min(m2, opacity); break;
				case BlendAlgorithm.max: m1.Max(m2, opacity); break;
				case BlendAlgorithm.overlay: m1.Overlay(m2, opacity); break;
				case BlendAlgorithm.hardLight: m1.HardLight(m2, opacity); break;
				case BlendAlgorithm.softLight: m1.SoftLight(m2, opacity); break;
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Map/Modifiers", 
		name ="Normalize", 
		disengageable = true, 
		iconName="GeneratorIcons/Normalize",
		drawInlets = false,
		drawOutlet = false,
		colorType = typeof(MatrixWorld),
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Normalize")]
	public class Normalize200 : Generator, IMultiInlet, IMultiOutlet
	{
		public class NormalizeLayer : IInlet<MatrixWorld>, IOutlet<MatrixWorld>
		{
			public float Opacity { get; set; }

			public Generator Gen { get; private set; }
			public void SetGen (Generator gen) => Gen=gen;
			public NormalizeLayer (Generator gen) { this.Gen = gen; }
			public NormalizeLayer () { Opacity = 1; }

			public ulong id; //properties not serialized
			public ulong Id { get{return id;} set{id=value;} } 
			public ulong LinkedOutletId { get; set; }  //if it's inlet. Assigned every before each clear or generate
			public ulong LinkedGenId { get; set; } 

			public IUnit ShallowCopy() => (NormalizeLayer)this.MemberwiseClone();
		}

		public NormalizeLayer[] layers = new NormalizeLayer[0];
		public NormalizeLayer[] Layers => layers; 
		public void SetLayers(object[] ls) => layers = Array.ConvertAll(ls, i=>(NormalizeLayer)i);


		public IEnumerable<IInlet<object>> Inlets() 
		{ 
			for (int i=0; i<layers.Length; i++)
				yield return layers[i];
		}

		public IEnumerable<IOutlet<object>> Outlets() 
		{ 
			for (int i=0; i<layers.Length; i++)
				yield return layers[i];
		}

		public override void Generate (TileData data, StopToken stop)
		{
			if (layers.Length == 0) return;
			
			//reading/copying products
			MatrixWorld[] dstMatrices = new MatrixWorld[layers.Length];
			float[] opacities = new float[layers.Length];

			if (stop!=null && stop.stop) return;
			for (int i=0; i<layers.Length; i++)
			{
				if (stop!=null && stop.stop) return;

				MatrixWorld srcMatrix = data.ReadInletProduct(layers[i]);
				if (srcMatrix != null) dstMatrices[i] = new MatrixWorld(srcMatrix);
				else dstMatrices[i] = new MatrixWorld(data.area.full.rect, (Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize);

				opacities[i] = layers[i].Opacity;
			}

			//normalizing
			if (stop!=null && stop.stop) return;
			dstMatrices.FillNulls(() => new MatrixWorld(data.area.full.rect, (Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize));
			dstMatrices[0].Fill(1);
			Matrix.BlendLayers(dstMatrices, opacities);

			//saving products
			if (stop!=null && stop.stop) return;
			for (int i=0; i<layers.Length; i++)
				data.StoreProduct(layers[i], dstMatrices[i]);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Blur", iconName="GeneratorIcons/Blur", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Blur")]
	public class Blur200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		[Val("Downsample")] public float downsample = 10f;
		[Val("Blur")] public float blur = 3f;

		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			MatrixWorld dst = new MatrixWorld(src);

			int rrDownsample = (int)(downsample / Mathf.Sqrt(dst.PixelSize.x));
			float rrBlur = blur / dst.PixelSize.x;

			if (rrDownsample > 1)
				MatrixOps.DownsampleBlur(src, dst, rrDownsample, rrBlur);
			else
				MatrixOps.GaussianBlur(src, dst, rrBlur);

			data.StoreProduct(this, dst);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Cavity", iconName="GeneratorIcons/Cavity", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Cavity")]
	public class Cavity200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		public enum CavityType { Convex=0, Concave=1, Both=2 }
		[Val("Type")]		public CavityType type = CavityType.Convex;
		[Val("Intensity")]	public float intensity = 3;
		[Val("Spread")]		public float spread = 10; //actually the pixel size (in world units) of the lowerest mipmap. Same for draft and main

		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src.rect, src.worldPos, src.worldSize);

			if (stop!=null && stop.stop) return;
			Cavity(src, dst, type, intensity, spread, src.PixelSize.x, data.area.active.worldSize, stop);
			
			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}

		public static void Cavity (Matrix src, Matrix dst, 
			CavityType cavityType, float intensity, float spread,
			float pixelSize, Vector2D worldSize, StopToken stop)
		{
			MatrixOps.Cavity(src, dst); //produces the map with range -1 - 1
			dst.Multiply(1f / Mathf.Pow(pixelSize, 0.25f));

			float minResolution = worldSize.x / spread;  //area worldsize / (spread = min pixel size)
			float downsample = Mathf.Log(src.rect.size.x, 2);
			downsample -= Mathf.Log(minResolution, 2);

			if (stop!=null && stop.stop) return;
			MatrixOps.OverblurMipped(dst, downsample:Mathf.Max(0,downsample), escalate:1.5f);

			if (stop!=null && stop.stop) return;
			dst.Multiply(intensity*100f);

			switch (cavityType)
			{
				case CavityType.Convex: dst.Invert(); break;
				//case CavityType.Concave: break;
				case CavityType.Both: dst.Invert(); dst.Multiply(0.5f); dst.Add(0.5f); break;
			}
			
			dst.Clamp01();

			//blending 50% map if downsample doesn't allow cavity here (for drafts or low-res)
			if (stop!=null && stop.stop) return;
			if (downsample < 0f)
			{
				float subsample = -downsample/4; 
				if (subsample > 1) subsample = 1;

				float avg = dst.Average();
				dst.Fill(avg, subsample);
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Slope", iconName="GeneratorIcons/Slope", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Slope")]
	public class Slope200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		[Val("From")]			public float from = 30;
		[Val("To")]				public float to = 90;
		[Val("Smooth Range")]	public float range = 30f;
		
		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src==null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = Slope(src, data.globals.height, from, to, range);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}


		public static MatrixWorld Slope (MatrixWorld heights, float height, float from, float to, float range) =>
			Slope(heights, heights.worldPos, heights.worldSize, height, from, to, range);

		public static MatrixWorld Slope (Matrix heights, Vector3 worldPos, Vector3 worldSize, float height, float from, float to, float range)
		{
			//delta map
			MatrixWorld delta = new MatrixWorld(heights.rect, worldPos, worldSize);
			MatrixOps.Delta(heights, delta);

			//slope map
			float minAng0 = from-range/2;
			float minAng1 = from+range/2;
			float maxAng0 = to-range/2;
			float maxAng1 = to+range/2;

			float pixelSize = 1f * worldSize.x / heights.rect.size.x; //using the terain-height relative values
			
			float minDel0 = Mathf.Tan(minAng0*Mathf.Deg2Rad) * pixelSize / height;
			float minDel1 = Mathf.Tan(minAng1*Mathf.Deg2Rad) * pixelSize / height;
			float maxDel0 = Mathf.Tan(maxAng0*Mathf.Deg2Rad) * pixelSize / height;
			float maxDel1 = Mathf.Tan(maxAng1*Mathf.Deg2Rad) * pixelSize / height;

			//dealing with 90-degree
			if (maxAng0 > 89.9f) maxDel0 = 20000000; 
			if (maxAng1 > 89.9f) maxDel1 = 20000000;

			if (from < 0.00001f) { minDel0=-1; minDel1=-1; }
			//not right, but intuitive - if user wants to mask from 0 don't add gradient here

			//ignoring min if it is zero
			//if (steepness.x<0.0001f) { minDel0=0; minDel1=0; }

			delta.SelectRange(minDel0, minDel1, maxDel0, maxDel1);

			return delta;
		}

	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Selector", iconName="GeneratorIcons/Selector", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Selector")]
	public class Selector200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		public enum RangeDet { Transition, MinMax}
		public RangeDet rangeDet = RangeDet.Transition;
		public enum Units { Map, World }
		public Units units = Units.Map;
		public Vector2 from = new Vector2(0.4f, 0.6f);
		public Vector2 to = new Vector2(1f, 1f);
		
		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src==null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src);

			if (stop!=null && stop.stop) return;
			Select(dst, from, to, inWorldUnits:units==Units.World, worldHeight:data.globals.height);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}

		public static void Select (MatrixWorld dst, Vector2 from, Vector2 to, bool inWorldUnits, float worldHeight)
		{
			float min0 = from.x;  if (inWorldUnits) min0 /= worldHeight;
			float min1 = from.y;  if (inWorldUnits) min1 /= worldHeight;
			float max0 = to.x;    if (inWorldUnits) max0 /= worldHeight;
			float max1 = to.y;    if (inWorldUnits) max1 /= worldHeight;
			dst.SelectRange(min0, min1, max0, max1);
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Map/Modifiers", 
		name ="Terrace", 
		iconName="GeneratorIcons/Terrace", 
		disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Terrace")]
	public class Terrace200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		[Val("Seed")]		 public int seed = 12345;
		[Val("Num")]		 public int num = 10;
		[Val("Uniformity")] public float uniformity = 0.5f;
		[Val("Steepness")]	 public float steepness = 0.5f;
		//[Val("Intensity")]	 public float intensity = 1f;

		
		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null || num <= 1) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			MatrixWorld dst = new MatrixWorld(src); 
			float[] terraceLevels = TerraceLevels(new Noise(data.random,seed), num, uniformity);
			
			if (stop!=null && stop.stop) return;
			dst.Terrace(terraceLevels, steepness);

			data.StoreProduct(this, dst);
		}


		public static float[] TerraceLevels (Noise random, int num, float uniformity)
		{
			//creating terraces
			float[] terraces = new float[num];

			float step = 1f / (num-1);
			for (int t=1; t<num; t++)
				terraces[t] = terraces[t-1] + step;

			for (int i=0; i<10; i++)
				for (int t=1; t<num-1; t++)
				{
					float rndVal = random.Random(i);
					rndVal = terraces[t-1] +  rndVal*(terraces[t+1]-terraces[t-1]);
					terraces[t] = terraces[t]*uniformity + rndVal*(1-uniformity);
				}

			return terraces;
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Map/Modifiers", 
		name ="Direction", 
		iconName="GeneratorIcons/Direction", 
		disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Direction")]
	public class Direction210 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		[Val("Hor Angle", min=-360, max=360)] public float horAngle = 0;
		[Val("Vert Angle", min=-89.99f, max=89.99f)] public float vertAngle = 0;
		[Val("Intensity", min =0)] public float intensity = 1;
		[Val("Wrapping", min=-1, max=1)] public float wrapping = 0;

		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			Vector3 dir = new Vector3(Mathf.Sin(horAngle*Mathf.Deg2Rad), Mathf.Tan(vertAngle*Mathf.Deg2Rad), Mathf.Cos(horAngle*Mathf.Deg2Rad));
			dir = dir.normalized;

			MatrixWorld dst = new MatrixWorld(src.rect, src.worldPos, src.worldSize);

			if (stop!=null && stop.stop) return;
			MatrixOps.NormalsDir(src, dst, dir, data.area.PixelSize.x, data.globals.height, intensity, wrapping);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Map/Modifiers", 
		name ="Ledge", 
		iconName="GeneratorIcons/Ledge", 
		disengageable = true, 
		advancedOptions = true,
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Ledge")]
	public class Ledge210 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>, IMultiInlet
	{
		[Val("Level")]		public float level = 50;
		[Val("Contour Blur")]	public float contourBlur = 3;
		[Val("Height")]		public float height = 10;
		[Val("Steep")]		public float steep = 30; //aka width

		[Val("Top Shoulder", "Advanced")]	public float topShoulder = 2f;
		[Val("Bottom Shoulder", "Advanced")]	public float bottomShoulder = 2f;
		[Val("Smooth", "Advanced")]	public bool smooth = true;

		[Val("Mask", "Inlet")] public readonly Inlet<MatrixWorld> heightMaskIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets()  { yield return heightMaskIn; }

		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			if (stop!=null && stop.stop) return;

			MatrixWorld heightMask = data.ReadInletProduct(heightMaskIn);

			Matrix ledgeMask = LedgeMask(src, contourBlur);

			MatrixWorld dst = new MatrixWorld(src.rect, src.worldPos, src.worldSize);
			/*LedgeStep(src, mask, dst,
					minFrom: (level - height*steep/2) / data.globals.height, 
					maxFrom: (level + height*steep/2) / data.globals.height,
					minTo: (level - height/2) / data.globals.height,
					maxTo: (level + height/2) / data.globals.height,
					bottomShoulder, topShoulder);*/

			LedgeStep(src, ledgeMask, heightMask, dst,
				level/data.globals.height, height/data.globals.height, steep,
				bottomShoulder, topShoulder, smooth);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
		}

		public static Matrix LedgeMask (Matrix src, float blur)
		{
			Matrix mask;

			if (blur > 2)
			{
				mask = new Matrix(src);
				MatrixOps.DownsampleBlur(mask, (int)blur, 1.75f);
			}
			else if (blur > 0.001f)
			{
				mask = new Matrix(src);
				MatrixOps.GaussianBlur(mask, blur);
			}
			else
				mask = src;

			return mask;
		}

/*		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
			[DllImport("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "GeneratorLedgeStep")]
			private static extern void LedgeStep (Matrix src, Matrix mask, Matrix dst,
				float minFrom, float maxFrom, float minTo, float maxTo, 
				float bottomShoulder, float topShoulder);
		#else */
			public static void LedgeStep (Matrix src, Matrix mask, Matrix intensity, Matrix dst,
				float minFrom, float maxFrom, float minTo, float maxTo, 
				float bottomShoulder, float topShoulder)
			{
				for (int i=0; i<dst.arr.Length; i++)
				{
					float heightVal = src.arr[i];
					float maskVal = mask.arr[i];

					if (maskVal < minFrom)
					{
						float p = heightVal/minFrom;
						p = ((p/bottomShoulder) + (bottomShoulder-1)/(bottomShoulder)) * p  +  p * (1-p); //inverse of (p/shoulder)*(1-p) + p*p;
						dst.arr[i] = minTo * p;
					}

					else if (maskVal > maxFrom)
					{
						float p = ((heightVal-maxFrom)/(1-maxFrom));
						p = (p/topShoulder)*(1-p) + p*p; //Mathf.Pow(p,topShoulder);
						dst.arr[i] = maxTo + (1-maxTo) * p;
					}

					else 
					{
						float p = (heightVal-minFrom) / (maxFrom-minFrom);
						float ip = 1-p;
						float tp = ((p/topShoulder) + (topShoulder-1)/(topShoulder)) * p  +  p * (1-p);
						float lp = (p/bottomShoulder)*(1-p) + p*p;
						float bp = 3*p*p - 2*p*p*p;

						p = lp*(1-bp)  +  tp*bp;

						dst.arr[i] = minTo + (maxTo-minTo)*p;
					}
				}
			}
//		#endif


			public static void LedgeStep (Matrix src, Matrix mask,  Matrix intensity, Matrix dst,
				float level, float height, float steep,
				float bottomShoulder, float topShoulder,
				bool smooth=true)
			{
				for (int i=0; i<dst.arr.Length; i++)
				{
					float intensityVal = intensity!=null ? intensity.arr[i] : 1;
					float heightVal = src.arr[i];
					float maskVal = mask.arr[i];

					float start = level - 1f/steep*intensityVal;
					float end = level + 1f/steep*intensityVal;

					float startShoulder = start - (height*intensityVal)/2*bottomShoulder;
					float endShoulder = end + (height*intensityVal)/2*topShoulder;

					//ledge itself
					if (maskVal > start  &&  maskVal < end)
					{
						float p = (maskVal-start) / (end-start);

						if (smooth)
							p = 3*p*p - 2*p*p*p;
						//p = 3*p*p - 2*p*p*p;
						//p = 6*p*p*p*p*p - 15*p*p*p*p + 10*p*p*p;
						dst.arr[i] = (heightVal - (height*intensityVal)/2)*(1-p)  +  (heightVal + (height*intensityVal)/2)*p;
					}

					//bottom shoulder
					else if (maskVal > startShoulder  &&  maskVal < level)
					{
						float p = (maskVal-startShoulder) / (start-startShoulder);

						if (smooth)
							p = 3*p*p - 2*p*p*p;

						dst.arr[i] = heightVal - (height*intensityVal)/2*p;
					}

					//top shoulder
					else if (maskVal < endShoulder  &&  maskVal > level)
					{
						float p = (endShoulder-maskVal) / (endShoulder-end);

						if (smooth)
							p = 3*p*p - 2*p*p*p;

						dst.arr[i] = heightVal + (height*intensityVal)/2*p;
					}

					//everything else
					else
						dst.arr[i] = heightVal;
				}
			}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Map/Modifiers", 
		name ="Beach", 
		iconName="GeneratorIcons/Beach", 
		disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Beach")]
	public class Beach210 : Generator, IInlet<MatrixWorld>, IMultiInlet, IOutlet<MatrixWorld>, IMultiOutlet
	{
		[Val("Level")]	public float level = 50;
		[Val("Contour Relax")]	public float relax = 100;
		[Val("Size")]	public float size = 40;
		[Val("Height")]	public float height = 5;
		[Val("Sand Tex Blur")]	public float sandBlur = 5f;

		[Val("Mask", "Inlet")] public readonly Inlet<MatrixWorld> beachMaskIn = new Inlet<MatrixWorld>();
		[Val("Sand", "Inlet")] public readonly Outlet<MatrixWorld> sandMaskOut = new Outlet<MatrixWorld>();

		public IEnumerable<IInlet<object>> Inlets()  { yield return beachMaskIn; }
		public IEnumerable<IOutlet<object>> Outlets()  { yield return sandMaskOut; }


		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled) { data.StoreProduct(this, src); return; }

			MatrixWorld beachMask = data.ReadInletProduct(beachMaskIn);

			if (stop!=null && stop.stop) return;
			Matrix shoreSpread = PrepareShoreMask(src, 
					level/data.globals.height, 
					size/data.area.PixelSize.x,
					height/data.globals.height,
					relax, stop);

			if (stop!=null && stop.stop) return;
			MatrixWorld dst = new MatrixWorld(src.rect, src.worldPos, src.worldSize);
			BeachHeight(src, dst, shoreSpread, beachMask,
					level/data.globals.height, 
					height/data.globals.height);

			if (stop!=null && stop.stop) return;
			MatrixWorld sand = new MatrixWorld(src.rect, src.worldPos, src.worldSize);
			BeachSand(src, dst, sand, level/data.globals.height, sandBlur/data.globals.height);
			
			if (stop!=null && stop.stop) return;
			dst.Max(src);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, dst);
			data.StoreProduct(sandMaskOut, sand);
		}

		public static Matrix PrepareShoreMask (Matrix src, float level, float size, float height, float relax, StopToken stop)
		/// Relaxes the shore line and creates the beach mask
		{
			Matrix shore = new Matrix(src);
			shore.Select(level+height/2); 

			//relaxing shore line: moving beach inside
			Matrix insShore = new Matrix(shore.rect);
				if (stop!=null && stop.stop) return null;
			
			shore.InvertOne();
			MatrixOps.SpreadLinear(shore, insShore, 1f/relax, diagonals:true, quarters:true);
			insShore.Select(0.01f);
			insShore.InvertOne();
				if (stop!=null && stop.stop) return null;

			MatrixOps.GaussianBlur(insShore, 3.75f); //just to smoothen remaining edges
			insShore.Select(0.5f);
				if (stop!=null && stop.stop) return null;
			
			//and then outside
			shore.Fill(0);
			MatrixOps.SpreadLinear(insShore, shore, 1f/relax, diagonals:true, quarters:true);
			shore.Select(0.01f);
				if (stop!=null && stop.stop) return null;

			//now standard shore spreading
			Matrix shoreSpread = insShore; shoreSpread.Fill(0); //re-using matrix // = new Matrix(src.rect);
			MatrixOps.SpreadLinear(shore, shoreSpread, 1f/size, diagonals:true, quarters:true);  //0.5 above water, 0.5 below water, 1 
			shoreSpread.Clamp01();
				if (stop!=null && stop.stop) return null;

			//blurring a bit (to avoid spread artifacts)
			//MatrixOps.DownsampleBlur(shoreSpread, smooth, 1.75f);
			MatrixOps.GaussianBlur(shoreSpread, 3.75f);

			return shoreSpread;
		}

/*		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
			[DllImport("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "GeneratorBeachHeight210")]
			private static extern void BeachHeight (Matrix src, Matrix dst, Matrix shoreSpread, Matrix mask, float level, float height);
		#else*/
			public static void BeachHeight (Matrix src, Matrix dst, Matrix shoreSpread, Matrix mask, float level, float height)
			/// Generates beach height using spread & mask. Should be combined with original height with Max
			{
				for (int i=0; i<dst.count; i++)
				{
					float heightVal = src.arr[i];
					float shoreVal = shoreSpread.arr[i];
					float maskVal = mask != null ? 1-(1-mask.arr[i])*(1-mask.arr[i]) : 1;

					if (shoreVal > 0.999f)
						dst.arr[i] = level+height/2;

					if (shoreVal < 0.0001f)
						dst.arr[i] = src.arr[i];

					float percent = shoreVal * maskVal;
					float val = level-height/2 + percent*height;

					float bottomPercent = (0.5f-percent) * 2; //0->1, 0.5->0
					if (bottomPercent < 0) bottomPercent = 0;
					bottomPercent = 3*bottomPercent*bottomPercent - 2*bottomPercent*bottomPercent*bottomPercent;
					val = val*(1-bottomPercent) + heightVal*bottomPercent;

					dst.arr[i] = val;
				}
			}
//		#endif

		#if MM_NATIVE && (UNITY_EDITOR || !UNITY_ANDROID && !ENABLE_IL2CPP)
			[DllImport("NativePlugins", CallingConvention = CallingConvention.Cdecl, EntryPoint = "GeneratorBeachSand210")]
			private static extern void BeachSand (Matrix heights, Matrix beach, Matrix sand, float waterLevel, float maxDelta);
		#else
			public static void BeachSand (Matrix heights, Matrix beach, Matrix sand, float waterLevel, float maxDelta)
			/// Creates sand mask based on beach and original height
			{
				for (int i=0; i<heights.count; i++)
				{
					float heightVal = heights.arr[i];
					float beachVal = beach.arr[i];

					//for above water
					if (beachVal > waterLevel)
					{
						float delta = beachVal - heightVal;
						float percent = delta / maxDelta;
						if (percent > 1) percent = 1;
						if (percent < 0) percent = 0;

						sand.arr[i] = percent;
					}

					//for shallow underwater
					else if (beachVal > waterLevel-maxDelta/2)
					{
						if (beachVal > heightVal+0.0001f)
							sand.arr[i] = 1;
						else
							sand.arr[i] = 0;
					}

					//deep - always in sand
					else
						sand.arr[i] = 1;
				}
			}
		#endif

	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Erosion", iconName="GeneratorIcons/Erosion", disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Erision")]
	public class Erosion200 : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>, ICustomComplexity
	{
		[Val("Iterations")]		 public int iterations = 3;
		[Val("Durability")] public float terrainDurability=0.9f;
		//[Val("Erosion")]	 
			public float erosionAmount=1f;
		[Val("Sediment")]	 public float sedimentAmount=0.75f;
		[Val("Fluidity")]	public int fluidityIterations=3;
		[Val("Relax")]		public float relax=0.0f;

		[DllImport ("NativePlugins", EntryPoint = "SetOrder")]
		private static extern void SetOrder (float[] refArr, int[] orderArr, int length);

		[DllImport ("NativePlugins", EntryPoint = "MaskBorders")]
		private static extern int MaskBorders (int[] orderArr, CoordRect matrixRect);

		[DllImport ("NativePlugins", EntryPoint = "CreateTorrents")]
		private static extern int CreateTorrents (float[] heights, int[] order, float[] torrents, CoordRect matrixRect);

		[DllImport ("NativePlugins", EntryPoint = "Erode")]
		private static extern int Erode (float[] heights, float[] torrents, float[] mudflow, int[] order, CoordRect matrixRect,
			float erosionDurability = 0.9f, float erosionAmount = 1, float sedimentAmount = 0.5f);

		[DllImport ("NativePlugins", EntryPoint = "TransferSettleMudflow")]
		private static extern int TransferSettleMudflow(float[] heights, float[] mudflow, float[] sediments, int[] order, CoordRect matrixRect, int erosionFluidityIterations = 3);

		public float Complexity {get{ return iterations*2; }}
		public float Progress (TileData data) { return data.GetProgress(this); }


		public override void Generate (TileData data, StopToken stop)
		{
			#if MM_DEBUG
			Log.Add("Generating Erosion (draft:" + data.isDraft + " pos:" + data.area.active.worldPos);
			#endif

			MatrixWorld src = data.ReadInletProduct(this);
			if (src == null) return; 
			if (!enabled || iterations <= 0) { data.StoreProduct(this, src); return; }

			MatrixWorld dst = new MatrixWorld(src);
			Erosion(dst, data.isDraft, data, iterations, terrainDurability, erosionAmount, sedimentAmount, fluidityIterations, relax, this, stop);
				if (stop!=null && stop.stop) return;

			data.StoreProduct(this, dst);
		}


		public static void Erosion (MatrixWorld dstHeight, bool isDraft, TileData data, 
			int iterations, float terrainDurability, float erosionAmount, float sedimentAmount, int fluidityIterations, float relax,
			ICustomComplexity thisGen, StopToken stop=null)
		{
			//allocating temporary matrices
			Matrix2D<int> order = new Matrix2D<int>(dstHeight.rect);
			Matrix torrents = new Matrix(dstHeight.rect);
			Matrix mudflow = new Matrix(dstHeight.rect);
			Matrix sediment = new Matrix(dstHeight.rect);

			int curIterations = iterations;
			int curFluidity = fluidityIterations;

			if (isDraft)
			{
				curIterations = iterations/3;
				curFluidity = fluidityIterations/3;
			}

			//calculate erosion
			for (int i=0; i<curIterations; i++) 
			{
				Den.Tools.Erosion.SetOrder(dstHeight, order);
				if (stop!=null && stop.stop) return;

				Den.Tools.Erosion.MaskBorders(order);
				if (stop!=null && stop.stop) return;

				Den.Tools.Erosion.CreateTorrents(dstHeight, order, torrents);
				if (stop!=null && stop.stop) return;

				Den.Tools.Erosion.Erode(dstHeight, torrents, mudflow, order, terrainDurability, erosionAmount, sedimentAmount);
				if (stop!=null && stop.stop) return;

				Den.Tools.Erosion.TransferSettleMudflow(dstHeight, mudflow, sediment, order, curFluidity);
				if (stop!=null && stop.stop) return;

				if (relax>0.0001f  &&  i!=curIterations-1)
					MatrixOps.GaussianBlur(dstHeight, relax/(i+1));
				if (stop!=null && stop.stop) return;

				data.SetProgress(thisGen, i*2);
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map/Modifiers", name ="Sediment", iconName="GeneratorIcons/Sediment", disengageable = true, colorType = typeof(MatrixWorld), 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Sediment")]
	public class Sediment210 : Generator, IMultiInlet, IMultiOutlet
	{
		[Val("Original", "Outlet")] public readonly Inlet<MatrixWorld> origIn = new Inlet<MatrixWorld>();
		[Val("Eroded", "Outlet")] public readonly Inlet<MatrixWorld> erodedIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets()  { yield return origIn; yield return erodedIn; }

		[Val("Cliff", "Outlet")] public readonly Outlet<MatrixWorld> cliffOut = new Outlet<MatrixWorld>();
		[Val("Sediment", "Outlet")] public readonly Outlet<MatrixWorld> sedimentOut = new Outlet<MatrixWorld>();
		public IEnumerable<IOutlet<object>> Outlets()  { yield return cliffOut; yield return sedimentOut; }

		[Val("Cliff")]	public float cliffIntensity = 1f;
		[Val("Sediment")] public float sedimentIntensity = 1f;


		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld orig = data.ReadInletProduct(origIn);
			MatrixWorld eroded = data.ReadInletProduct(erodedIn);
			if (orig == null || eroded == null) return; 

			MatrixWorld cliff = new MatrixWorld(orig.rect, orig.worldPos, orig.worldSize);
			MatrixWorld sediment = new MatrixWorld(orig.rect, orig.worldPos, orig.worldSize);
				if (stop!=null && stop.stop) return;
			CliffSediment(orig, eroded, cliff, sediment);
				if (stop!=null && stop.stop) return;

			data.StoreProduct(cliffOut, cliff);
			data.StoreProduct(sedimentOut, sediment);
		}


		public void CliffSediment (MatrixWorld orig, MatrixWorld eroded, MatrixWorld cliff, MatrixWorld sediment)
		///Painting with cliff or sediment depending on the erosion change (delta) value
		///Determining whether it's sediment or cliff by comparing original and eroded inclines (if more inclined - then cliff)
		{
			MatrixWorld origInclines = cliff;  //writing incline to temporary matrices. Will be overwritten anyways
			MatrixOps.Delta(orig, origInclines);

			MatrixWorld erodedInclines = sediment;
			MatrixOps.Delta(eroded, erodedInclines);

			for (int i=0; i<orig.count; i++)
			{
				float heightDelta = orig.arr[i] - eroded.arr[i];
				if (heightDelta < 0) heightDelta = 0;

				float inclineDelta = erodedInclines.arr[i] - origInclines.arr[i];

				if (inclineDelta > 0) //if incline increased - using cliff
				{
					cliff.arr[i] = inclineDelta*1000*cliffIntensity/orig.PixelSize.x;
					sediment.arr[i] = 0; 
				}

				else //if lowered - using sediment
				{
					sediment.arr[i] = heightDelta*1000*sedimentIntensity/orig.PixelSize.x; //sediment takes delta
					cliff.arr[i] = 0; 
				}
			}
		}
	}


	[Serializable]
	[GeneratorMenu(
		menu = "Map/Modifiers", 
		name = "Parallax", 
		section=2, 
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/Parallax",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/-/wikis/MatrixGenerators/Parallax")]
	public class Parallax210 : Generator, IInlet<MatrixWorld>, IMultiInlet, IOutlet<MatrixWorld>
	{
		[Val("Intensity X", "Inlet")]	public readonly Inlet<MatrixWorld> intensityInX = new Inlet<MatrixWorld>();
		[Val("Intensity Z", "Inlet")]	public readonly Inlet<MatrixWorld> intensityInZ = new Inlet<MatrixWorld>();
		public virtual IEnumerable<IInlet<object>> Inlets () { yield return intensityInX; yield return intensityInZ; }

		[Val("Offset")] public Vector2D offset;
		public enum Interpolation { None, Always, OnTransitions }
		[Val("Interpolation")] public Interpolation interpolation = Interpolation.Always;

		public override void Generate (TileData data, StopToken stop) 
		{ 
			if (stop!=null && stop.stop) return;
			MatrixWorld map = data.ReadInletProduct(this);
			if (map == null) return;
			if (!enabled) { data.StoreProduct(this,map); return; }

			MatrixWorld intensityX = data.ReadInletProduct(intensityInX);
			MatrixWorld intensityZ = data.ReadInletProduct(intensityInZ);

			if (stop!=null && stop.stop) return;
			Vector2D pixelDir = offset / map.PixelSize;
			MatrixWorld result = new MatrixWorld(map);
			result.Parallax(pixelDir, map, intensityX, intensityZ, (int)interpolation);

			if (stop!=null && stop.stop) return;
			data.StoreProduct(this, result);
		}
	}
}

