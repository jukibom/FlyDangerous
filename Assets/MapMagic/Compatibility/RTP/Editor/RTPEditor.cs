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

	public static class RTPEditor
	{
		#if RTP
		static string[] textureNames = null;
		#endif
		
		 [UnityEditor.InitializeOnLoadMethod]
		static void EnlistInMenu ()
		{
			CreateRightClick.generatorTypes.Add(typeof(RTPOutput200));
		}

		[Draw.Editor(typeof(RTPOutput200))]
		public static void DrawRTP (RTPOutput200 gen) 
		{ 
			#if RTP
			GeneratorEditors.UpdateMaterial();
			gen.rtp = GraphWindow.current.mapMagic?.GetComponent<ReliefTerrain>();

			if (gen.rtp != null)
			{
				if (textureNames==null || textureNames.Length!=gen.rtp.globalSettingsHolder.numLayers) textureNames = new string[gen.rtp.globalSettingsHolder.numLayers];
				textureNames.Process(i=>gen.rtp.globalSettingsHolder.splats[i]!=null ? gen.rtp.globalSettingsHolder.splats[i].name : null); //nb linq?
			}

			using (Cell.Line)
			{
				//Cell.current.margins = new Padding(4);
				//DrawCustomMaterialWarning(MapMagic.instance.terrainSettings);
				DrawRTPComponentWarning();
			}
				
			#else
			using (Cell.LinePx(36)) Draw.Label("RTP is not installed or RTP \ncompatibility is disabled");
			#endif

			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true, unlinkBackground:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawRTPLayer);
		}

		private static void DrawRTPLayer (Generator tgen, int num)
		{
			RTPOutput200 gen = (RTPOutput200)tgen;
			RTPOutput200.RTPLayer layer = gen.layers[num];
			if (layer == null) return;

			using (Cell.LinePx(32))
			{
				//Cell.current.margins = new Padding(0,0,0,1); //1-pixel more padding from the bottom since layers are 1 pixel overlayed

				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);

				Cell.EmptyRowPx(10);

				//icon
				#if RTP
				Texture2D icon = null;
				if (gen.rtp != null)
				{
					if (layer.channelNum < gen.rtp.globalSettingsHolder.splats.Length)
						icon = gen.rtp.globalSettingsHolder.splats[layer.channelNum];

					using (Cell.RowPx(28))
					{
						Cell.EmptyLinePx(2);
						using (Cell.Line) Draw.TextureIcon(icon); 
						Cell.EmptyLinePx(2);
					}

					//channel selector
					Cell.EmptyRowPx(3);
					using (Cell.Row) 
					{
						Cell.EmptyLine();
						using (Cell.LineStd) Draw.PopupSelector(ref layer.channelNum, textureNames);
						Cell.EmptyLine();
					}
				}
				else
				#endif
					using (Cell.Row) 
					{
						Cell.EmptyLine();
						using (Cell.LineStd) Draw.Field(ref layer.channelNum, "Channel");
						Cell.EmptyLine();
					}

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
		}


		public static void DrawRTPComponentWarning ()
		{
			#if RTP
			if (GraphWindow.current.mapMagic == null)
				return;

			if (GraphWindow.current.mapMagic?.gameObject.GetComponent<ReliefTerrain>()==null || GraphWindow.current.mapMagic?.gameObject.GetComponent<Renderer>()==null)
			{
				using (Cell.LinePx(70))
				{
					GUIStyle backStyle = UI.current.textures.GetElementStyle("DPUI/Backgrounds/Foldout");

					using (Cell.Row)
						Draw.Label("RTP or Renderer \ncomponents are \nnot assigned to \nMapMagic object");

					using (Cell.RowPx(30))
						if (Draw.Button("Fix"))
					{
						if (GraphWindow.current.mapMagic.gameObject.GetComponent<Renderer>() == null)
						{
							MeshRenderer renderer = GraphWindow.current.mapMagic.gameObject.AddComponent<MeshRenderer>();
							renderer.enabled = false;
						}
						if (GraphWindow.current.mapMagic.gameObject.GetComponent<ReliefTerrain>() == null)
						{
							ReliefTerrain rtp = GraphWindow.current.mapMagic.gameObject.AddComponent<ReliefTerrain>();

							//filling empty splats
							Texture2D emptyTex = TextureExtensions.ColorTexture(4,4,new Color(0.5f, 0.5f, 0.5f, 1f));
							emptyTex.name = "Empty";
							rtp.globalSettingsHolder.splats = new Texture2D[] { emptyTex,emptyTex,emptyTex,emptyTex };
						}
					}
				}
				Cell.EmptyLinePx(5);
			}
			#endif
		}
	}
}

#endif