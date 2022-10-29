//using System; //conflicts with UnityEngine.Object
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Den.Tools.Matrices
{
	//[System.Serializable]
	public partial class MatrixSet
	{
		public struct Prototype
		{
			public TerrainLayer layer;
			public Texture2D tex;
			public GameObject prefab;
			//public TerrainLayer tlayer; //not complicating things yet
			public int instanceNum; //if this texture is used in array more than once. Starts with 0

			public Object Object 
			{get{
				if (layer != null) return layer;
				else if (tex != null) return tex;
				else return prefab;
			}}

			public Prototype (TerrainLayer layer) { this.layer=layer; this.tex = null; this.prefab = null; instanceNum = 0; }
			public Prototype (Texture2D texture) { this.layer=null; this.tex = texture; this.prefab = null; instanceNum = 0; }
			public Prototype (GameObject prefab) { this.layer=null; this.tex = null; this.prefab = prefab; instanceNum = 0; }
			public Prototype (TerrainLayer layer, int instanceNum) { this.layer=layer; this.tex = null; this.prefab = null; this.instanceNum = instanceNum; }
			public Prototype (Texture2D texture, int instanceNum) { this.layer=null; this.tex = texture; this.prefab = null; this.instanceNum = instanceNum; }
			public Prototype (GameObject prefab, int instanceNum) { this.layer=null; this.tex = null; this.prefab = prefab; this.instanceNum = instanceNum; }

			public Prototype (System.Array layers, int num)
			/// Creates a prototype and assigns proper texture/object and instance num
			/// No need to make generic
			{
				object layer = layers.GetValue(num);
				Object obj = GetLayerObject(layer);
				if (obj == null)
					{this.layer = null; this.tex = null; this.prefab = null; instanceNum = 0; return; }

				instanceNum = 0;
				for (int i=0; i<num; i++)
				{
					Object prevObj =  GetLayerObject( layers.GetValue(i) );
					if (prevObj == null)
						continue;

					if (prevObj == obj)
						instanceNum++;
				}

				if (obj is TerrainLayer layerObj) this.layer = layerObj;
				else this.layer = null;

				if (obj is Texture2D texObj) this.tex = texObj;
				else this.tex = null;

				if (obj is GameObject goObj) this.prefab = goObj;
				else this.prefab = null;
			}


			public T[] CheckAppendLayers<T> (T[] layers) where T: class
			/// If there is no such prototype in layers - appending layers with newly created prototype
			/// Returns layers themselves if no need to add
			{
				int currInstNum = 0;
				Object thisObj = Object;
				bool foundInLayers = false;

				for (int i=0; i<layers.Length; i++)
				{
					Object layerObj = GetLayerObject( layers.GetValue(i) );
					if (layerObj == null)
						continue;

					if (layerObj==thisObj)
					{
						if (currInstNum==instanceNum) 
							{ foundInLayers=true; break; }

						currInstNum++;
					}
				}

				if (!foundInLayers)
					ArrayTools.Add(ref layers, NewLayer<T>(this));

				return layers;
			}


			private static Object GetLayerObject (object layer)
			/// Gets terrainLayer/detailPrototype main object (texture or prefab)
			{
				switch (layer)
				{
					case TerrainLayer tlayer: return tlayer;
					case DetailPrototype dprot: return  dprot.Object();
					case Texture2D tex: return tex;
					default: return null;
				}
			}


			public static T NewLayer<T> (Prototype prot) where T: class
			/// Converts prototype to new terrainLayer or detailPrototype - depends on T type
			{
				if (typeof(T) == typeof(Texture2D))
				{
					if (prot.tex!=null) return (T)(object)prot.tex;
					if (prot.prefab!=null) return (T)(object)prot.prefab.GetMainTexture();
				}

				if (typeof(T) == typeof(TerrainLayer))
				{
					if (prot.layer!=null) return (T)(object)prot.layer;
					if (prot.tex!=null) 
					{
						TerrainLayer tlayer = new TerrainLayer() {
							diffuseTexture = prot.tex,
							normalMapTexture = prot.tex.GetNormalTexture(),
							tileSize = new Vector2(20,20) };
						#if UNITY_EDITOR
						UnityEditor.AssetDatabase.CreateAsset(tlayer, "Assets/" + prot.tex.name + "_TerrainLayer.terrainlayer");
						#endif
						return tlayer as T;
					}
				}

				if (typeof(T) == typeof(DetailPrototype))
				{
					if (prot.tex!=null) return new DetailPrototype() {
						renderMode = DetailRenderMode.Grass,
						dryColor = new Color(0.95f, 1f, 0.65f), 
						healthyColor = new Color(0.5f, 0.65f, 0.35f),
						prototypeTexture = prot.tex } as T;
					//if (prot.tlayer!=null) return new DetailPrototype() {
					//	renderMode = DetailRenderMode.Grass,
					//	prototypeTexture = prot.tlayer.diffuseTexture } as T;
					else if (prot.prefab!=null) return new DetailPrototype() {
						renderMode = DetailRenderMode.VertexLit,
						usePrototypeMesh = true,
						prototype = prot.prefab } as T;
				}

				return null;
			}
		}
	}
}