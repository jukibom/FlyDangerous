#if MAPMAGIC2 //shouldn't work if MM assembly not compiled

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Core;  //used once to get tile size
using MapMagic.Products;
using MapMagic.Nodes.MatrixGenerators;


namespace MapMagic.Nodes.GUI
{

	public static class CTSEditor
	{
		private static string[] textureNames;

        [UnityEditor.InitializeOnLoadMethod]
		static void EnlistInMenu ()
		{
			CreateRightClick.generatorTypes.Add(typeof(CTSOutput200));
		}

		[Draw.Editor(typeof(MatrixGenerators.CTSOutput200))]
		public static void DrawCTS (MatrixGenerators.CTSOutput200 gen) 
		{ 
			#if CTS_PRESENT

			CTS.CTSProfile profile = GraphWindow.current.mapMagic?.Globals.ctsProfile as CTS.CTSProfile;
			
			if (GraphWindow.current.mapMagic != null)
			 using (Cell.LineStd)
				{
					CTS.CTSProfile newProfile = Draw.ObjectField(profile, "Profile");
					if (profile != newProfile) 
					{
						GraphWindow.current.mapMagic.Globals.ctsProfile = newProfile;
						profile = newProfile;
					}
				}
			else
				using (Cell.LinePx(18+18)) Draw.Label("Not assigned to current \nMapMagic object");

//			using (Cell.LineStd)
//				if (Draw.Button("Update Shader"))
//					CTS_UpdateShader(ctsProfile, MapMagic.instance.terrainSettings.material);

			//populating texture names
			if (profile != null)
			{
				List<CTS.CTSTerrainTextureDetails> textureDetails = profile.TerrainTextures;
				if (textureNames==null || textureNames.Length!=textureDetails.Count) textureNames = new string[textureDetails.Count];
				textureNames.Process(i=>textureDetails[i].m_name);
			}

			#else
			using (Cell.LinePx(60))
                Draw.Helpbox("CTS doesn't seem to be installed, or CTS compatibility is not enabled in settings");
			#endif

			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawCTSLayer);
		}

		private static void DrawCTSLayer (Generator tgen, int num)
		{
			CTSOutput200 gen = (CTSOutput200)tgen;
			CTSOutput200.CTSLayer layer = gen.layers[num];
			if (layer == null) return;

			#if CTS_PRESENT
			CTS.CTSProfile profile = GraphWindow.current.mapMagic?.Globals.ctsProfile as CTS.CTSProfile;
			#endif

			Cell.EmptyLinePx(3);
			using (Cell.LinePx(28))
			{
				//Cell.current.margins = new Padding(0,0,0,1); //1-pixel more padding from the bottom since layers are 1 pixel overlayed

				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);

				Cell.EmptyRowPx(10);

				#if CTS_PRESENT

				//icon	
				using (Cell.RowPx(28)) 
				{
					Texture2D icon = null;
					if (profile != null)
					{
						List<CTS.CTSTerrainTextureDetails> textureDetails = profile.TerrainTextures;
						if (layer.channelNum < textureDetails.Count)
							icon = textureDetails[layer.channelNum].Albedo;
					}
					Draw.TextureIcon(icon);
				}

				//channel selector
				Cell.EmptyRowPx(5);
				using (Cell.Row)
				{
					Cell.EmptyLine();
					using (Cell.LineStd)
					{
						if (textureNames != null)
							Draw.PopupSelector(ref layer.channelNum, textureNames);	
						else
							Draw.Field(ref layer.channelNum, "Channel");
					}
					Cell.EmptyLine();
				}

				#else
				using (Cell.Row) Draw.Field(ref layer.channelNum, "Channel");
				#endif

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
			Cell.EmptyLinePx(3);
		}

        	/*public static void DrawCTSShaderNameWarning ()
			{
				Terrains.TerrainSettings settings = GraphWindow.current.mapMagic.terrainSettings;
				{
					using (Cell.LinePx(70))
					{
						//Cell.current.margins = new Padding(4);

						GUIStyle backStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/Foldout");
						Draw.Element(backStyle);
						Draw.Element(backStyle);

						using (Cell.Row) Draw.Label("No CTS material \nis assigned as \nCustom Material in \nTerrain Settings");

						using (Cell.RowPx(30))
							if (Draw.Button("Fix"))
							{
								Shader shader = Shader.Find("CTS/CTS Terrain Shader Basic");
								settings.material = new Material(shader);

								#if CTS_PRESENT
								if (ctsProfile != null) CTS_UpdateShader(ctsProfile, MapMagic.instance.terrainSettings.material);
								#endif
							
								GraphWindow.current.mapMagic.ApplyTerrainSettings();

								GraphWindow.RefreshMapMagic();
							}
					}
					Cell.EmptyLinePx(5);
				}
			}
*/
	}
}

#endif