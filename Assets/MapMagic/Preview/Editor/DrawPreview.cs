		
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Den.Tools;
using Den.Tools.Tasks;
using Den.Tools.GUI;

using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Terrains;

using MapMagic.Nodes.GUI;

namespace MapMagic.Previews
{	
	public static class PreviewDraw
	{
		public static Color BackgroundColor =>  StylesCache.isPro ? 
			new Color(0.3f, 0.3f, 0.3f, 1) : 
			new Color(0.4f, 0.4f, 0.4f, 1);
		private static Material textureRawMat;


		public static void DrawPreview (IOutlet<object> outlet)
		{
			IPreview preview = PreviewManager.GetPreview(outlet);
			if (preview == null) 
			{
				preview = PreviewManager.CreatePreview(outlet);
				if (preview == null) return; //for unknown types
				if (GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)
					preview.SetObject(outlet, mapMagicObject.PreviewData);
			}

			//preview itself
			using (Cell.Full)
			{
				Color backColor = StylesCache.isPro ? 
						new Color(0.33f, 0.33f, 0.33f, 1) : 
						new Color(0.4f, 0.4f, 0.4f, 1);
				Draw.Rect(BackgroundColor);  //background in case no preview, or preview object was not assigned

				if (preview != null) preview.DrawInGraph();
			}

			//clock/na
			if (GraphWindow.current.mapMagic!=null)
			{
				if (preview.Stage==PreviewStage.Generating  ||  (preview.Stage==PreviewStage.Blank && GraphWindow.current.mapMagic.IsGenerating()))
					using (Cell.Full) Draw.Icon(UI.current.textures.GetTexture("MapMagic/PreviewSandClock"));
				else if (preview.Stage==PreviewStage.Blank)
					using (Cell.Full) Draw.Icon(UI.current.textures.GetTexture("MapMagic/PreviewNA"));
			}

			//terrain buttons
			if (preview != null)
				using (Cell.Full)
					TerrainWindowButtons(preview);
		}


		private static void TerrainWindowButtons (IPreview preview)
		{
			using (Cell.LineStd)
			{
				using (Cell.RowPx(20)) 
				{
					if (preview.Terrain != null)
					{
						if (Draw.Button(UI.current.textures.GetTexture("MapMagic/Icons/PreviewToTerrainActive"), visible:false))
							PreviewManager.RemoveAllFromTerrain();
					}
					else
					{
						if (Draw.Button(UI.current.textures.GetTexture("MapMagic/Icons/PreviewToTerrain"), visible:false))
						{
							PreviewManager.RemoveAllFromTerrain();
							if (GraphWindow.current.mapMagic is MapMagicObject mapMagicObject)
							{
								TerrainTile previewTile = mapMagicObject.PreviewTile;
								preview.ToTerrain(previewTile?.main?.terrain, previewTile?.draft?.terrain);
							}
						}
					}
				}

				using (Cell.RowPx(20)) 
					if (Draw.Button(UI.current.textures.GetTexture("MapMagic/Icons/PreviewToWindow"), visible:false))
						PreviewManager.GetCreateWindow(preview);

				//Cell.EmptyRow();
			}
		}


		public static void DrawGenerateMarkInWindow (PreviewStage stage, Vector2 center)
		/// Draws non-scaled N/A or clock icon in the center of the preview window if necessary
		{
			Texture2D tex;
			switch (stage)
			{
				case PreviewStage.Blank: tex = UI.current.textures.GetTexture("MapMagic/PreviewNA"); break;
				case PreviewStage.Generating: tex = UI.current.textures.GetTexture("MapMagic/PreviewSandClock"); break;
				default: tex = null; break;
			}

			if (tex != null)
				using (Cell.Full)
					using (Cell.Custom(center, Vector2.one))
			{
				Cell.current.pixelSize = new Vector2( tex.width/UI.current.scrollZoom.zoom, tex.height/UI.current.scrollZoom.zoom );
				Cell.current.pixelOffset -= Cell.current.pixelSize / 2;

				Draw.Texture(tex);
			}
		}
	}
}