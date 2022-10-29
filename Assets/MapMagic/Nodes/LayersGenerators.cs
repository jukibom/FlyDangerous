using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices; //Normalize gen
//using Den.Tools.Segs;

using MapMagic.Core;
using MapMagic.Products;

namespace MapMagic.Nodes
{
	/*[System.Serializable]
	public abstract class ILayersGenerator : Generator, IMultiNode
	/// The node with the layers. Each layer has 0 or more inputs and one output.
	{
		public abstract class Layer : INode, IOutlet<object>
		{
			public Generator parent;
			public Generator Gen { get{ return parent;} set{parent=value;} }
		}


		public Layer[] layers = new Layer[0];

		public abstract Layer CreateLayer ();

		public virtual void AddLayer (int num) { ArrayTools.Insert(ref layers, layers.Length, CreateLayer()); }
		public virtual void RemoveLayer (int num) { ArrayTools.RemoveAt(ref layers, num); }
		public virtual void MoveLayer (int from, int to) { ArrayTools.Move(layers, from, to); }

		public override IEnumerable<IInlet<object>> Inlets() 
		{ 
			for (int i=0; i<layers.Length; i++)
				foreach (IInlet<object> inlet in layers[i].Inlets())
					yield return inlet;
		}

		public IEnumerable<INode> InternalNodes()
		{
			for (int i=0; i<layers.Length; i++)
				yield return layers[i];
		}
	}*/

	
	/*public abstract class NormalizeGenerator : Generator, IMultiInlet, IMultiOutlet
	/// A generator with normalized layers. Each layer has 1 input and 1 output, matrix only.
	/// Here only because it's used quite often
	{
		public interface INormalizableLayer : IInlet<MatrixWorld>, IOutlet<MatrixWorld> 
		{ 
			float Opacity { get; }
		}

		public class NormalizeLayer : INormalizableLayer, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
		{
			public Generator Gen {get; set; }
			public float Opacity { get; set; }
		}

		public NormalizeLayer[] layers = new NormalizeLayer[0];

		//inversed oder
		public void AddLayer (int num)  { ArrayTools.Insert(ref layers, layers.Length, new NormalizeLayer() {Gen=this, Opacity=1} ); }
		public void RemoveLayer (int num)  { ArrayTools.RemoveAt(ref layers, layers.Length-1 - num); }
		public void MoveLayer (int from, int to)  { ArrayTools.Move(layers, layers.Length-1 - from, layers.Length-1 - to); }

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
			NormalizeLayers(layers, data, stop);
		}


		public static void NormalizeLayers (INormalizableLayer[] layers, TileData data, StopToken stop)
		{
			//reading products
			MatrixWorld[] matrices = new MatrixWorld[layers.Length];
			float[] opacities = new float[layers.Length];

			if (stop!=null && stop.stop) return;
			for (int i=0; i<layers.Length; i++)
			{
				if (stop!=null && stop.stop) return;
				NormalizeLayer layer = (NormalizeLayer)layers[i];

				MatrixWorld srcMatrix = data.ReadInletProduct(layer);
				if (srcMatrix != null) matrices[i] = new MatrixWorld(srcMatrix);

				opacities[i] = layer.Opacity;
			}

			//normalizing
			if (stop!=null && stop.stop) return;
			matrices.FillNulls(() => new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize));
			matrices[0].Fill(1);
			Matrix.BlendLayers(matrices, opacities);

			//saving products
			if (stop!=null && stop.stop) return;
			for (int i=0; i<layers.Length; i++)
				data.products[layers[i]] = matrices[i];
		}
	}*/

}