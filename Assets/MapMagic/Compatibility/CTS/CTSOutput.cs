#if MAPMAGIC2 //shouldn't work if MM assembly not compiled

using System.Collections.Generic;
using UnityEngine;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Products;

#if CTS_PRESENT
using CTS;
#endif

namespace MapMagic.Nodes.MatrixGenerators {
	[System.Serializable]
	[GeneratorMenu(
		menu = "Map/Output", 
		name = "CTS", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		iconName="GeneratorIcons/TexturesOut",
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class CTSOutput200 : BaseTexturesOutput<CTSOutput200.CTSLayer>
	{
		//public static CTS.CTSProfile ctsProfile;  //in globals

		public class CTSLayer : BaseTextureLayer { }

		public string[] guiTextureNames = null;


		public override void Generate (TileData data, StopToken stop) 
		{
			//generating
			MatrixWorld[] dstMatrices = BaseGenerate(data, stop);

			//adding to finalize
			if (stop!=null && stop.stop) return;
			if (enabled)
			{
				for (int i=0; i<layers.Length; i++)
					data.StoreOutput(layers[i], typeof(CTSOutput200), layers[i],  dstMatrices[i]);
				data.MarkFinalize(Finalize, stop);
			}
			else 
				data.RemoveFinalize(finalizeAction);
		}

		public override FinalizeAction FinalizeAction => finalizeAction; //should return variable, not create new
		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop) 
		{
			#if CTS_PRESENT
			if (data.globals.ctsProfile==null) return;
			
			//creating control textures
			if (stop!=null && stop.stop) return;
			data.GatherOutputs (typeof(CTSOutput200),
				out CTSLayer[] layers,
				out MatrixWorld[] matrices,
				out MatrixWorld[] masks,
				inSubs:true);
			float[] opacities = layers.Select(l=>l.Opacity);
			int[] channelNums = layers.Select(l=>l.channelNum);

			//purging if no outputs
			if (matrices.Length == 0)
			{
				if (stop!=null && stop.stop) return;
				data.MarkApply(CustomShaderOutput200.ApplyData.Empty);
				return;
			}

			//calculating number of textures
			int maxChannelNum = 0;
			foreach (int chNum in channelNums)
				if (chNum > maxChannelNum) maxChannelNum=chNum;

			int numTextures = maxChannelNum/4 + 1;

			//blending matrices
			Color[][] colors = CustomShaderOutput200.BlendMatrices(data.area.active.rect, matrices, masks, opacities, channelNums);

			//pushing to apply
			if (stop!=null && stop.stop) return;
			//var applyData = new ApplyData() { colors = colors };
			var applyData = new ApplyData()
			{
				textureColors = colors,
				profile = (CTSProfile)data.globals.ctsProfile,
			};

			Graph.OnOutputFinalized?.Invoke(typeof(ApplyData), data, applyData, stop);
			data.MarkApply(applyData);

			#endif
		}


		public override void ClearApplied (TileData data, Terrain terrain)
		{

		}

		#if CTS_PRESENT
		public class ApplyData : IApplyData
		{
			public CTSProfile profile;
			public Color[][] textureColors;

			public void Apply (Terrain terrain)
			{
				//saving enable state (since CTS switch to default on enabled when no profile assigned)
				bool activeState = terrain.gameObject.activeSelf;
				terrain.gameObject.SetActive(false);

				CompleteTerrainShader cts = terrain.GetComponent<CompleteTerrainShader>();
				if (cts == null) cts = terrain.gameObject.AddComponent<CompleteTerrainShader>();
				
				//firstly add splat textures (otherwise CTS will log error on profile assign in playmode)
				int resolution = (int)Mathf.Sqrt(textureColors[0].Length);
				TextureFormat texFormat = TextureFormat.RGBA32;
				for (int i=0; i<textureColors.Length; i++)
				{
					if (textureColors[i] == null) continue;

					Texture2D tex =
						i==0 ? cts.Splat1 :
						i==1 ? cts.Splat2 :
						i==2 ? cts.Splat3 :
						cts.Splat4;

					if (tex==null || tex.width!=resolution || tex.height!=resolution || tex.format!=texFormat)
					{
						if (tex!=null)
						{
							#if UNITY_EDITOR
							if (!UnityEditor.AssetDatabase.Contains(tex))
							#endif
								GameObject.DestroyImmediate(tex);
						}

						tex = new Texture2D(resolution, resolution, texFormat, false, true);
						tex.wrapMode = TextureWrapMode.Mirror; //to avoid border seams
						//tex.hideFlags = HideFlags.DontSave;
						//tex.filterMode = FilterMode.Point;

						if (i==0) cts.Splat1 = tex;
						else if (i==1) cts.Splat2 = tex;
						else if (i==2) cts.Splat3 = tex;
						else cts.Splat4 = tex;
					}

					tex.SetPixels(0,0,tex.width,tex.height,textureColors[i]);
					tex.Apply();
				}

				//then asssign profile
				if (cts.Profile != profile) cts.Profile = profile;

				//enable
				terrain.gameObject.SetActive(activeState);
			}

			public void Read (Terrain terrain) { throw new System.NotImplementedException(); }

			public static ApplyData Empty {get{ return new ApplyData() { textureColors = new Color[0][] }; }}

			public int Resolution
			{get{
				if (textureColors.Length==0) return 0;
				else return (int)Mathf.Sqrt(textureColors[0].Length);
			}}
		}
		#endif
	}

}

#endif //MAPMAGIC2