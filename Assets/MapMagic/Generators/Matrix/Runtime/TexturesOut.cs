using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Products;
using MapMagic.Terrains;

using UnityEngine.Profiling;

namespace MapMagic.Nodes.MatrixGenerators {
	[System.Serializable]
	public class BaseTextureLayer : IInlet<MatrixWorld>, IOutlet<MatrixWorld>
	{
		public string name = "Layer";
		public int channelNum = 0; //for RTP, CTS and custom

		[SerializeField] private float opacity = 1;
		public float Opacity { get=>opacity; set=>opacity=value; }

		public Generator Gen { get { return gen; } private set { gen = value;} }
		public Generator gen; //property is not serialized
		public void SetGen (Generator gen) => this.gen=gen;

		public ulong id; //properties not serialized
		public ulong Id { get{return id;} set{id=value;} } 
		public ulong LinkedOutletId { get; set; }  //if it's inlet. Assigned every before each clear or generate
		public ulong LinkedGenId { get; set; } 

		public IUnit ShallowCopy() => (BaseTextureLayer)this.MemberwiseClone();
	}


	[System.Serializable]
	public abstract class BaseTexturesOutput<L> : OutputGenerator, IMultiLayer, IMultiInlet, IMultiOutlet  where L: BaseTextureLayer, new()
	{
		public OutputLevel outputLevel = OutputLevel.Draft | OutputLevel.Main;
		public override OutputLevel OutputLevel { get{ return outputLevel; } }


		public L[] layers = new L[0];
		public IList<IUnit> Layers { get => layers; set => layers=ArrayTools.Convert<L,IUnit>(value); }
//		public virtual void SetLayers(object[] ls) => layers = Array.ConvertAll(ls, i=>(L)i);
		public virtual bool Inversed => true;
		public virtual bool HideFirst => true; //not for all - direct matrices and textures do not hide first layer

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

		public MatrixWorld[] BaseGenerate (TileData data, StopToken stop) 
		/// Reads inlets, normalizes, writes outputs
		/// But not sending to finalize
		{
			if (layers.Length == 0) return null;

			//reading/copying products
			MatrixWorld[] dstMatrices = new MatrixWorld[layers.Length];
			float[] opacities = new float[layers.Length];

			if (stop!=null && stop.stop) return null;
			for (int i=0; i<layers.Length; i++)
			{
				if (stop!=null && stop.stop) return null;

				MatrixWorld srcMatrix = data.ReadInletProduct(layers[i]);
				if (srcMatrix != null) dstMatrices[i] = new MatrixWorld(srcMatrix);
				else dstMatrices[i] = new MatrixWorld(data.area.full.rect, (Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize);

				opacities[i] = layers[i].Opacity;
			}

			//normalizing
			if (stop!=null && stop.stop) return null;
			dstMatrices.FillNulls(() => new MatrixWorld(data.area.full.rect, (Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize));
			dstMatrices[0].Fill(1);
			Matrix.BlendLayers(dstMatrices, opacities);

			//saving products
			if (stop!=null && stop.stop) return null;
			for (int i=0; i<layers.Length; i++)
				data.StoreProduct(layers[i], dstMatrices[i]);

			return dstMatrices;
		}

		public abstract FinalizeAction FinalizeAction { get; } 

		//public abstract void Purge (TileData data, Terrain terrain);
	}


	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "Textures", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class TexturesOutput200 : BaseTexturesOutput<TexturesOutput200.TextureLayer>
	{
		public class TextureLayer : BaseTextureLayer
		{
			[Val("Layer", cat:"Layer")] public TerrainLayer prototype; // = new TerrainLayer() {  tileSize=new Vector2(20,20) };

			public Color color = new Color(0.75f, 0.75f, 0.75f, 1);

			public bool guiProperties;
			public bool guiRemapping;
			public bool guiTileSettings;
		}

		[SerializeField] public int guiExpanded;


		public override void Generate (TileData data, StopToken stop) 
		{
			Log.Add("Textures");

			//generating
			MatrixWorld[] dstMatrices = BaseGenerate(data, stop);

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(TexturesOutput200), layers[i].prototype,  dstMatrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}

		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			//preparing arrays
			if (stop!=null && stop.stop) return;
			data.GatherOutputs (typeof(TexturesOutput200),
				out TerrainLayer[] prototypes,
				out MatrixWorld[] matrices,
				out MatrixWorld[] masks,
				inSubs:true);

			//creating splats and prototypes arrays
			if (stop!=null && stop.stop) return;
			float[,,] splats3D = BlendLayers(matrices, masks, data.area, stop:stop);

			//pushing to apply
			if (stop!=null && stop.stop) return;
			ApplyData applyData = new ApplyData() { splats=splats3D, prototypes=prototypes };
			Graph.OnOutputFinalized?.Invoke(typeof(TexturesOutput200), data, applyData, stop);
			data.MarkApply(applyData);

			#if MM_DEBUG
			Log.Add("TexturesOut Finalized");
			#endif
		}

		public static float[,,] BlendLayers (IList<Matrix> matrices, IList<Matrix> masks, Area area, IList<int> channelNumbers=null, StopToken stop=null)
		/// If channelNumbers are not provided - blending Texture Output style. If provided - blending MicroSplat splats
		{
			int fullSize = area.full.rect.size.x;
			int activeSize = area.active.rect.size.x;
			int margins = area.Margins;

			int count;
			if (channelNumbers != null)
			{
				int maxChannelNum = 0;
				foreach (int chNum in channelNumbers)
					if (chNum > maxChannelNum) maxChannelNum=chNum;

				count = maxChannelNum + 1;
			}
			else count = matrices.Count;

			float[,,] splats3D = new float[activeSize, activeSize, count];

			for (int x=0; x<activeSize; x++)
			{
				if (stop!=null && stop.stop) return null;
				for (int z=0; z<activeSize; z++)
				{
					//int pos = (z+margins-area.full.rect.offset.z)*area.full.rect.size.x + x+margins - area.full.rect.offset.x;
					int pos = area.full.rect.GetPos(x+area.full.rect.offset.x+margins, z+area.full.rect.offset.z+margins);

					float sum = 0;
					for (int i=0; i<count; i++) 
					{
						float val = matrices[i]!=null ? matrices[i].arr[pos] : 0;
						val *= masks[i]==null ? 1 : masks[i].arr[pos];
						sum += val;
					}

					if (sum != 0)
						for (int i=0; i<count; i++) 
					{
						float val = matrices[i]!=null ? matrices[i].arr[pos] : 0;
						val *= masks[i]==null ? 1 : masks[i].arr[pos];
						val /= sum;

						if (val < 0) val = 0; if (val > 1) val = 1;

						int chNum;
						if (channelNumbers != null) chNum = channelNumbers[i];
						else chNum = i;

						splats3D[z,x,chNum] += val;
					}
				}
			}

			return splats3D;
		}

		public class ApplyData : IApplyData
		{
			public float[,,] splats;
			public TerrainLayer[] prototypes;

			public virtual void Apply (Terrain terrain)
			{
				Profiler.BeginSample("Apply Textures " + terrain.transform.name);

				if (terrain==null || terrain.Equals(null) || terrain.terrainData==null) return; //chunk removed during apply
				TerrainData data = terrain.terrainData;

				//setting resolution
				int size = splats.GetLength(0);
				if (data.alphamapResolution != size) data.alphamapResolution = size;

				terrain.terrainData.terrainLayers = prototypes; //in 2017 seems that alphamaps should go first
				terrain.terrainData.SetAlphamaps(0,0,splats);

				Profiler.EndSample();

				#if MM_DEBUG
				Log.Add("TexturesOut Applied");
				#endif
			}

			public static ApplyData Empty
			{get{
				return new ApplyData() { 
					splats = new float[64,64,0],
					prototypes = new TerrainLayer[0] };
			}}

			public int Resolution  
			{get{ 
				if (splats==null) return 0;
				else return splats.GetLength(0); 
			}}
		}

		public override void ClearApplied (TileData data, Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;
			terrainData.terrainLayers = new TerrainLayer[0];
			terrainData.alphamapResolution = 32;
		}
	}


	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "Custom Material", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class CustomShaderOutput200 : BaseTexturesOutput<CustomShaderOutput200.CustomShaderLayer>
	{
		public class CustomShaderLayer : BaseTextureLayer { } //inheriting empty class just to draw it's editor

		public static string[] controlTextureNames = new string[] { "_ControlTexture1" };

		public static string[] controlTexturePossibleNames = new string[] { "_ControlTexture1", "_ControlTexture2", 
			"_ControlTexture3", "_ControlTexture4", "_ControlTexture5", "_ControlTexture6", "_ControlTexture7",
			"_ControlTexture8", "_ControlTexture9", "_ControlTexture10", "_ControlTexture11", "_ControlTexture12"};


		public override void Generate (TileData data, StopToken stop) 
		{
			//generating
			MatrixWorld[] dstMatrices = BaseGenerate(data, stop);

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(CustomShaderOutput200), layers[i],  dstMatrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			//preparing arrays
			if (stop!=null && stop.stop) return;
			data.GatherOutputs (typeof(CustomShaderOutput200),
				out (int chNum, float opacity)[] prototypes,
				out MatrixWorld[] matrices,
				out MatrixWorld[] masks,
				inSubs:true);
			float[] opacities = prototypes.Select(p => p.opacity);
			int[] chNums = prototypes.Select(p => p.chNum);

			//purging if no outputs
			if (matrices.Length == 0)
			{
				if (stop!=null && stop.stop) return;
				data.MarkApply(ApplyData.Empty);
				return;
			}

			//creating control textures contents
			Color[][] colors = BlendMatrices(data.area.active.rect, matrices, masks, opacities, chNums, normalize:true);

			//pushing to apply
			if (stop!=null && stop.stop) return;
			var controlTexturesData = new ApplyData() {
				textureColors = colors,
				textureFormat = TextureFormat.RGBA32,
				textureBaseMapDistance = 10000000, //no base map
				textureNames = (string[])controlTextureNames.Clone() };

			Graph.OnOutputFinalized?.Invoke(typeof(CustomShaderOutput200), data, controlTexturesData, stop);
			data.MarkApply(controlTexturesData);
		}


		public static Color[][] BlendMatrices (CoordRect colorsRect, IList<Matrix> matrices, IList<Matrix> biomeMasks, IList<float> opacities, IList<int> channelNums, bool normalize=false)
		/// Reads matrices and fills normalized values to colors using masks
		/// TODO: use raw texture bytes
		/// TODO: bring to matrix
		{
			int texturesCount;
			int maxChannelNum = 0;
			foreach (int chNum in channelNums)
				if (chNum > maxChannelNum) maxChannelNum=chNum;
			texturesCount = maxChannelNum/4 + 1;

			Color[][] colors = new Color[texturesCount][];

			int matrixCount = matrices.Count;

			//getting matrices rect
			CoordRect matrixRect = new CoordRect(0,0,0,0);
			for (int m=0; m<matrixCount; m++)
				if (matrices[m] != null) matrixRect = matrices[m].rect;

			//checking rect
			for (int m=0; m<matrixCount; m++)
				if (matrices[m] != null  &&  matrices[m].rect != matrixRect)
					throw new Exception("MapMagic: Matrix rect mismatch");
			for (int b=0; b<matrixCount; b++)
				if (biomeMasks[b] != null  &&  biomeMasks[b].rect != matrixRect)
					throw new Exception("MapMagic: Biome matrix rect mismatch");

			//preparing row re-use array
			float[] values = new float[texturesCount*4];

			//blending
			for (int x=0; x<colorsRect.size.x; x++)
				for (int z=0; z<colorsRect.size.z; z++)
				{
					int matrixPosX = colorsRect.offset.x + x;
					int matrixPosZ = colorsRect.offset.z + z;
					int matrixPos = (matrixPosZ-matrixRect.offset.z)*matrixRect.size.x + matrixPosX - matrixRect.offset.x;

					int colorsPos = z*colorsRect.size.x + x; //(z-colorsRect.offset.z)*colorsRect.size.x + x - colorsRect.offset.x;

					float sum = 0;

					//resetting values
					for (int m=0; m<values.Length; m++)
						values[m] = 0;

					//getting values
					for (int m=0; m<matrixCount; m++)
					{
						Matrix matrix = matrices[m];
						if (matrix == null) 
							continue;

						float val = matrix.arr[matrixPos];

						//multiply with biome
						Matrix biomeMask = biomeMasks[m];
						if (biomeMask != null) //no empty biomes in list (so no mask == root biome)
							val *= biomeMask.arr[matrixPos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
						
						//clamp
						if (val < 0) val = 0; if (val > 1) val = 1;

						sum += val;
						values[channelNums[m]] += val;
					}

					//normalizing and writing to colors
					for (int m=0; m<values.Length; m++)
					{
						float val = values[m];

						if (normalize) val = sum!=0 ? val/sum : 0;
						
						int texNum = m / 4;
						int chNum = m % 4;

						if (colors[texNum] == null) colors[texNum] = new Color[colorsRect.size.x*colorsRect.size.z];

						switch (chNum)
						{
							case 0: colors[texNum][colorsPos].r += val; break;
							case 1: colors[texNum][colorsPos].g += val; break;
							case 2: colors[texNum][colorsPos].b += val; break;
							case 3: colors[texNum][colorsPos].a += val; break;
						}
					}
				}
			
			return colors;
		}


		public static Color[] MatricesToColors (CoordRect colorsRect, Matrix rMatrix, Matrix gMatrix, Matrix bMatrix, Matrix aMatrix)
		/// Just creates a texture from matrices without blending
		{
			CoordRect matrixRect = rMatrix.rect;
			Color[] colors = new Color[colorsRect.size.x*colorsRect.size.z];
			Color color = new Color();

			Coord min = colorsRect.Min; Coord max = colorsRect.Max;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				int matrixPos = (z-matrixRect.offset.z)*matrixRect.size.x + x - matrixRect.offset.x;
				int colorsPos =  (z-colorsRect.offset.z)*colorsRect.size.x + x - colorsRect.offset.x;
				
				color.r = rMatrix.arr[matrixPos];
				if (gMatrix != null) color.g = gMatrix.arr[matrixPos];
				if (bMatrix != null) color.b = bMatrix.arr[matrixPos];
				if (aMatrix != null) color.a = aMatrix.arr[matrixPos];

				colors[colorsPos] = color;
			}

			return colors;
		}


		public class ApplyData : IApplyData
		{
			public Color[][] textureColors; // TODO: use raw texture bytes
			public string[] textureNames;
			public string[] altTextureNames= null; //to let MicroSplat work with _Control0 and _CustomControl0
			public TextureFormat textureFormat;
			public float textureBaseMapDistance; //most custom shaders change the base distance using their profile or setting it to extremely high like megasplat


			public virtual void Apply (Terrain terrain)
			{
				if (textureColors==null) return;
				int numTextures = textureColors.Length;
				if (numTextures==0) return;
				int resolution = (int)Mathf.Sqrt(textureColors[0].Length);

				//MaterialPropertyBlock matProps = new MaterialPropertyBlock();

				//assigning material props via MaterialPropertySerializer to make them serializable
				MaterialPropertySerializer matPropSerializer = terrain.GetComponent<MaterialPropertySerializer>();
				if (matPropSerializer == null)
					matPropSerializer = terrain.gameObject.AddComponent<MaterialPropertySerializer>();


				for (int i=0; i<textureColors.Length; i++)
				{
					if (textureColors[i] == null) continue;

					string texName = null;
					if (i<textureNames.Length) texName = textureNames[i];

					Texture2D tex = matPropSerializer.GetTexture(textureNames[i]);
					if (tex==null || tex.width!=resolution || tex.height!=resolution || tex.format!=textureFormat)
					{
						if (tex!=null)
						{
							#if UNITY_EDITOR
							if (!UnityEditor.AssetDatabase.Contains(tex))
							#endif
								GameObject.DestroyImmediate(tex);
						}
							
						tex = new Texture2D(resolution, resolution, textureFormat, false, true);
						tex.name = texName;
						tex.wrapMode = TextureWrapMode.Mirror; //to avoid border seams
						//tex.hideFlags = HideFlags.DontSave;
						//tex.filterMode = FilterMode.Point;

						matPropSerializer.SetTexture(textureNames[i], tex);
					}

					tex.SetPixels(0,0,tex.width,tex.height,textureColors[i]);
					tex.Apply();

					//if (texName != null) matPropSerializer.SetTexture(texName, tex);
					if (texName != null) terrain.materialTemplate.SetTexture(texName, tex);
				}
 				
				matPropSerializer.Apply();

				terrain.basemapDistance = textureBaseMapDistance;	
			}

			public static ApplyData Empty
			{get{
				return new ApplyData() { 
					textureColors = new Color[0][],
					textureNames = new string[0]  };
			}}

			public int Resolution
			{get{
				if (textureColors.Length==0) return 0;
				else return (int)Mathf.Sqrt(textureColors[0].Length);
			}}
		}

		public override void ClearApplied (TileData data, Terrain terrain)
		{

		}
	}


	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "Direct Textures", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class DirectTexturesOutput200 : BaseTexturesOutput<DirectTexturesOutput200.DirectTexturesLayer>
	{
		public class DirectTexturesLayer : BaseTextureLayer { } //inheriting empty class just to draw it's editor

		public override bool HideFirst => false;

		public override void Generate (TileData data, StopToken stop) 
		{
			//reading products
			MatrixWorld[] matrices = new MatrixWorld[layers.Length];
			for (int i=0; i<layers.Length; i++)
			{
				if (stop!=null && stop.stop) return;
				matrices[i] = data.ReadInletProduct(layers[i]);
			}

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(DirectTexturesOutput200), (layers[i].name, layers[i].channelNum, layers[i].Opacity), matrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			//preparing arrays
			if (stop!=null && stop.stop) return;
			data.GatherOutputs (typeof(DirectTexturesOutput200),
				out (string name, int chNum, float opacity)[] prototypes,
				out MatrixWorld[] matrices,
				out MatrixWorld[] masks,
				inSubs:true);
			string[] names = prototypes.Select(p => p.name);
			float[] opacities = prototypes.Select(p => p.opacity);
			int[] chIndexes = prototypes.Select(p => p.chNum);

			//purging if no outputs
			if (matrices.Length == 0)
			{
				if (stop!=null && stop.stop) return;
				data.MarkApply(ApplyData.Empty);
				return;
			}

			//calculating number of textures, creating name->textureNum lut
			Dictionary<string,int> nameToNum = new Dictionary<string, int>();
			foreach (string name in names)
				if (!nameToNum.ContainsKey(name))
					nameToNum.Add(name, nameToNum.Count);

			//creating control textures contents
			Color[][] colors = new Color[nameToNum.Count][];
			string[] colorNames = new string[nameToNum.Count]; //texture names, in order corresponding to colors
			for (int m=0; m<matrices.Length; m++)
			{
				int textureNum = nameToNum[names[m]];
				
				colorNames[textureNum] = names[m];

				if (matrices[m] != null)
				{
					if (colors[textureNum] == null)
						colors[textureNum] = new Color[data.area.active.rect.size.x * data.area.active.rect.size.z];

					matrices[m].ExportColors(colors[textureNum], data.area.active.rect.offset, data.area.active.rect.size, chIndexes[m], markOutrange:false, mask:masks[m]);
				}
			}
			
			//pushing to apply
			if (stop!=null && stop.stop) return;
			var controlTexturesData = new ApplyData() {
				textureColors = colors,
				textureNames = colorNames,
				textureFormat = TextureFormat.RGBA32 };

			Graph.OnOutputFinalized?.Invoke(typeof(DirectTexturesOutput200), data, controlTexturesData, stop);
			data.MarkApply(controlTexturesData);
		}


		public class ApplyData : IApplyData
		{
			public Color[][] textureColors;
			public string[] textureNames;
			public TextureFormat textureFormat;

			public virtual void Apply (Terrain terrain)
			{
				if (textureColors==null  ||  textureColors.Length==0  ||  textureColors.AllNull()) return;
				int resolution = (int)Mathf.Sqrt(textureColors.Any().Length);

				DirectTexturesHolder holder = terrain.GetComponent<DirectTexturesHolder>();
				if (holder == null)
					holder = terrain.gameObject.AddComponent<DirectTexturesHolder>();

				//preparing textures
				DictionaryOrdered<string,Texture2D> newDict = new DictionaryOrdered<string,Texture2D>(textureNames);
				newDict.TakeMatchingValuesFrom(holder.textures);

				for (int i=0; i<textureColors.Length; i++)
				{
					if (textureColors[i] == null) continue;
					
					string texName = textureNames[i];
					Texture2D tex = newDict[texName];

					CheckTexture(ref tex, resolution, textureFormat);
					tex.name = textureNames[i];
					tex.wrapMode = TextureWrapMode.Mirror; //to avoid border seams

					tex.SetPixels(0,0,tex.width,tex.height,textureColors[i]);
					tex.Apply();

					newDict[texName] = tex; //it could be created from null
				}

				holder.textures = newDict;
				holder.position = (Vector2D)terrain.transform.position;
				holder.size = (Vector2D)terrain.terrainData.size;
			}


			public static void CheckTexture (ref Texture2D tex, int resolution, TextureFormat format)
			///Checks if texture has this resolution and format, and if not removes it and creates a new one
			{
				if (tex==null)
				{
					tex = new Texture2D(resolution, resolution, format, false, true);
					return;
				}

				if (tex.width!=resolution || tex.height!=resolution || tex.format!=format)
				{
					#if UNITY_EDITOR
					if (!UnityEditor.AssetDatabase.Contains(tex))
					#endif
						GameObject.DestroyImmediate(tex);
							
					tex = new Texture2D(resolution, resolution, format, false, true);
				}
			}


			public static ApplyData Empty
			{get{
				return new ApplyData() { 
					textureColors = new Color[0][],
					textureNames = new string[0]  };
			}}

			public int Resolution
			{get{
				if (textureColors.Length==0) return 0;
				else return (int)Mathf.Sqrt(textureColors[0].Length);
			}}
		}

		public override void ClearApplied (TileData data, Terrain terrain)
		{

		}
	}


	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "DirectMatrices", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class DirectMatricesOutput200 : BaseTexturesOutput<DirectMatricesOutput200.DirectMatricesLayer>
	{
		public class DirectMatricesLayer : BaseTextureLayer { } //inheriting empty class just to draw it's editor

		public override bool HideFirst => false;

		public override void Generate (TileData data, StopToken stop) 
		{
			//reading products
			MatrixWorld[] matrices = new MatrixWorld[layers.Length];
			for (int i=0; i<layers.Length; i++)
			{
				if (stop!=null && stop.stop) return;
				matrices[i] = data.ReadInletProduct(layers[i]);
			}

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(DirectMatricesOutput200), layers[i].name, matrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}


		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			//preparing arrays
			if (stop!=null && stop.stop) return;
			data.GatherOutputs (typeof(DirectMatricesOutput200),
				out string[] names,
				out MatrixWorld[] matrices,
				out MatrixWorld[] masks,
				inSubs:true);

			//purging if no outputs
			if (matrices.Length == 0)
			{
				if (stop!=null && stop.stop) return;
				data.MarkApply(ApplyData.Empty);
				return;
			}

			//writing dict
			DictionaryOrdered<string,MatrixWorld> dict = new DictionaryOrdered<string,MatrixWorld>();
			for (int m=0; m<matrices.Length; m++)
			{
				if (matrices[m] == null)
					continue;

				MatrixWorld matrixCopy = new MatrixWorld(matrices[m]); //to multyply with biome, and to keep it independent in case of matrix re-use feature
				
				if (masks[m] != null)
					matrixCopy.Multiply(masks[m]);

				matrixCopy.Crop(data.area.active.rect);
				Vector2D pixelSize = matrixCopy.PixelSize;
				matrixCopy.worldPos = (Vector3)data.area.active.worldPos;
				matrixCopy.worldSize = (Vector3)data.area.active.worldSize;
				matrixCopy.worldSize.y = matrices[m].worldSize.y;

				if (dict.ContainsKey(names[m])) dict[names[m]] = matrixCopy;
				else dict.Add(names[m], matrixCopy);
			}

			//pushing to apply
			if (stop!=null && stop.stop) return;
			var controlTexturesData = new ApplyData() {dict = dict};

			Graph.OnOutputFinalized?.Invoke(typeof(DirectMatricesOutput200), data, controlTexturesData, stop);
			data.MarkApply(controlTexturesData);
		}


		public class ApplyData : IApplyData
		{
			public DictionaryOrdered<string,MatrixWorld> dict;

			public virtual void Apply (Terrain terrain)
			{
				DirectMatricesHolder holder = terrain.GetComponent<DirectMatricesHolder>();
				if (holder == null)
					holder = terrain.gameObject.AddComponent<DirectMatricesHolder>();

				holder.maps = dict;
			}


			public static ApplyData Empty
			{
				get => new ApplyData() { dict = new DictionaryOrdered<string,MatrixWorld>() };
			}

			public int Resolution
			{get{
				if (dict.Count==0) return 0;
				else return dict[0].rect.size.x;
			}}
		}

		public override void ClearApplied (TileData data, Terrain terrain)
		{

		}
	}

}
