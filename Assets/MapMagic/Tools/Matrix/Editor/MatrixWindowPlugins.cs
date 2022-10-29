using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.GUI;


namespace Den.Tools.Matrices.Window
{
	public interface IPlugin
	{
		bool Enabled { get; set; }
		string Name { get; }

		void Setup (Matrix matrix);  //aka OnEnable, runs once on matrix assigned/changed
		void DrawWindow (Matrix matrix, BaseMatrixWindow window); //draws something in window
		void DrawInspector (Matrix matrix, BaseMatrixWindow window); //draws matrix toolbar
	}

	public class StatsPlugin : IPlugin
	{
		private bool enabled = true;
		public bool Enabled { get{return enabled;} set{enabled=value;} }
		public string Name => "Stats";

		private float min;
		private float max;
		private float[] histogram;

		public void Setup (Matrix matrix)
		{
			min = matrix.MinValue();
			max = matrix.MaxValue();
				
			histogram = matrix.Histogram(256, max:1, normalize:true);
		}


		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) { }


		public void DrawInspector (Matrix matrix, BaseMatrixWindow window)
		{
			if (matrix==null) return;

			using (Cell.LineStd) Draw.Field(matrix.rect, "Rect"); 
			using (Cell.LineStd) Draw.Field(min, "Min Value");
			using (Cell.LineStd) Draw.Field(max, "Max Value");

			Cell.EmptyLinePx(4);
			using (Cell.LinePx(64)) 
			{
				if (histogram != null && histogram.Length!=0)
					Draw.Histogram(histogram, new Vector4(0,0,0,0.25f), new Vector4(0,0,0,0));
				Draw.Grid(new Color(0,0,0,0.4f)); //background grid
			}
		}
	}


	public class ViewPlugin : IPlugin
	{
		private bool enabled = true;
		public bool Enabled { get{return enabled;} set{enabled=value;} }
		public string Name => "View";

		//public bool colorize;
		//public bool relief;
		//public float min = 0;
		//public float max = 1;

		private float matrixMin;
		private float matrixMax;

		private static bool lockScrollZoom = false;


		public void Setup (Matrix matrix) 
		{ 
			matrixMin = matrix.MinValue();
			matrixMax = matrix.MaxValue();
		}


		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) { }


		public void DrawInspector (Matrix matrix, BaseMatrixWindow window)
		{
			using (Cell.LineStd) Draw.ToggleLeft(ref window.colorize, "Colorize");
			using (Cell.LineStd) Draw.ToggleLeft(ref window.relief, "Relief");

			using (Cell.LinePx(0))
			{
				using (Cell.Row)
				{
					using (Cell.LineStd) Draw.Field(ref window.min, "Min");
					using (Cell.LineStd) Draw.Field(ref window.max, "Max");
				}
				using (Cell.RowPx(50))
					if (Draw.Button("Full")) { window.min = matrixMin; window.max = matrixMax; }

				using (Cell.RowPx(50))
					if (Draw.Button("0-1")) { window.min = 0; window.max = 1; }
			}

			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
			{
				using (Cell.Row) Draw.Label("Zoom");
				using (Cell.RowPx(40)) 
					if (Draw.Button("0.25")) window.Zoom = 0.25f;
				using (Cell.RowPx(40)) 
					if (Draw.Button("0.50")) window.Zoom = 0.5f;
				using (Cell.RowPx(40)) 
					if (Draw.Button("1")) window.Zoom = 1f;
				using (Cell.RowPx(40)) 
					if (Draw.Button("2")) window.Zoom = 2f;
				using (Cell.RowPx(40)) 
					if (Draw.Button("4")) window.Zoom = 4f;

				if (Cell.current.valChanged)
					UI.current.editorWindow.Repaint();
			}

			using (Cell.LineStd)
			{
				using (Cell.Row) Draw.Label("Scroll");
				using (Cell.RowPx(60)) 
					if (Draw.Button("Zero")) 
						window.Scroll = window.ScrollZero;
				using (Cell.RowPx(60)) 
					if (Draw.Button("Center")) 
						window.Scroll = window.ScrollCenter; 
			}

			using (Cell.LineStd)
			{
				using (Cell.LineStd) Draw.ToggleLeft(ref lockScrollZoom, "Lock All Windows Scroll/Zoom");
									
				if (lockScrollZoom)
					{
						MatrixWindow[] windows = Resources.FindObjectsOfTypeAll<MatrixWindow>();
						for (int w=0; w<windows.Length; w++)
						{
							windows[w].Scroll = window.Scroll;
							windows[w].Zoom = window.Zoom;
						}
					}
			}
		}
	}


	public class PixelPlugin : IPlugin
	{
		public bool Enabled { get; set; }
		public string Name => "Pixel";

		private float curValue;
		private Coord curCoord;
		private bool curDefined;
		private Coord oldCurCoord;

		public void Setup (Matrix matrix) { }

		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) 
		{ 
//			DragMarkers();
//			for (int r=0; r<markerRects.Length; r++)
//				Draw.Rect(FlipVertical(markerRects[r]), new Color(1,0,0,0.25f));

			if (Event.current.button == 2)
			{
				curCoord = Coord.Floor(UI.current.mousePos.x, -UI.current.mousePos.y);
				if (matrix!=null && matrix.rect.Contains(curCoord))
				{
					curValue = matrix[curCoord];
					curDefined = true;
				}
				else
					curDefined = false;

				if (curCoord != oldCurCoord)
				{
					UI.current.editorWindow.Repaint();
					oldCurCoord = curCoord;
				}
			}
		}

		public void DrawInspector (Matrix matrix, BaseMatrixWindow window)
		{
			Cell.EmptyLinePx(4);
			using (Cell.LineStd) Draw.DualLabel("Current Coordinate", curCoord.x + ", "+curCoord.z);
			using (Cell.LineStd) Draw.DualLabel("Current Value", curDefined ? curValue.ToString("0.0000f") : "Undefined");
			using (Cell.LineStd) Draw.Label("Middle-click to refresh");
		}

		private void DragMarkers ()
		{
			Vector2 cursorRect = new Vector2(UI.current.mousePos.x, -UI.current.mousePos.y);

			if (DragDrop.TryDrag(Cell.current, cursorRect))
			{
				Rect rect = new Rect(DragDrop.initialMousePos, DragDrop.totalDelta);
				Draw.Rect(FlipVertical(rect), new Color(1,0,0,0.5f));
			}
			DragDrop.TryStart(Cell.current, cursorRect, Cell.current.GetRect());
			if (DragDrop.TryRelease(Cell.current, cursorRect))
			{
				Rect rect = new Rect(DragDrop.initialMousePos, DragDrop.totalDelta);
//				ArrayTools.Add(ref markerRects, rect);
			}
		}

		private static Rect FlipVertical (Rect rect)
		{
			rect.y = -rect.y - rect.height;
			return rect;
		}
	}


	public class SlicePlugin : IPlugin
	{
		public bool Enabled { get; set; }
		public string Name => "Slice";

		private MatrixOps.Stripe slice;
		private Rect sliceRect;

		public void Setup (Matrix matrix) { }

		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) 
		{ 
			DragSlice(matrix);
			Draw.Rect(FlipVertical(sliceRect), new Color(0,1,0,0.3f));
		}

		public void DrawInspector (Matrix matrix, BaseMatrixWindow window)
		{
			using (Cell.LinePx(256))
			{
				if (slice != null && slice.arr != null && slice.arr.Length != 0)
					Draw.Histogram(slice.arr, new Vector4(0,0,0,0.5f), new Vector4(1,1,1,0.3f));
				Draw.Grid(new Color(0,0,0,0.4f), cellsNumX:1, cellsNumY:8);
			}
		}

		private void DragSlice (Matrix matrix)
		{
			Vector2 cursorRect = new Vector2(UI.current.mousePos.x, -UI.current.mousePos.y);

			if (DragDrop.TryDrag(Cell.current, cursorRect))
			{
				Rect rect = new Rect(DragDrop.initialMousePos, DragDrop.totalDelta);
				rect = To1PixelRect(rect);

				Draw.Rect(FlipVertical(rect), new Color(0,1,0,0.5f));
			}
			DragDrop.TryStart(Cell.current, cursorRect, Cell.current.GetRect());
			if (DragDrop.TryRelease(Cell.current, cursorRect))
			{
				Rect rect = new Rect(DragDrop.initialMousePos, DragDrop.totalDelta);
				rect = To1PixelRect(rect);

				int x = (int)rect.x;					// + matrix.rect.offset.x;
				int z = (int)(rect.y+1); //ceil?		//)matrix.rect.size.z - (int)rect.y + matrix.rect.offset.z - 1;

				if (rect.width!=0 && rect.height!=0)
				{
					if (rect.width > rect.height)
					{
						//line = new float[(int)rect.width];
						slice = new MatrixOps.Stripe((int)rect.width);
						MatrixOps.ReadLine(slice, matrix, x, z);
					}
					else
					{
						//line = new float[(int)rect.height];
						slice = new MatrixOps.Stripe((int)rect.height);
						MatrixOps.ReadRow(slice, matrix, x, z-slice.length);
					}
					//{
					//	slice = new Matrix.Stripe( Mathf.Max((int)rect.width, (int)rect.height) );
					//	matrix.ReadDiagonal(stripe, x, z);
					//}

					sliceRect = rect;

					//MatrixLineTesterWindow lineTesterWindow = (MatrixLineTesterWindow)GetWindow(typeof (MatrixLineTesterWindow));
					//lineTesterWindow.src = slice;
				}
			}
		}

		private Rect To1PixelRect (Rect rect)
		{
			if (rect.width > rect.height)
			{
				rect.y += rect.height/2;
				rect.height = 1;//*scrollZoom.zoom;
			}
			else
			{
				rect.x += rect.width/2;
				rect.width = 1;//*scrollZoom.zoom;
			}

			if (rect.width > 256) rect.width = 256;
			if (rect.height > 256) rect.height = 256;

			return rect;
		}

		private static Rect FlipVertical (Rect rect)
		{
			rect.y = -rect.y - rect.height;
			return rect;
		}
	}


	public class ProcessPlugin : IPlugin
	{
		public bool Enabled { get; set; }
		public string Name => "Process";

		public void Setup (Matrix matrix) { }

		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) { }

		public void DrawInspector (Matrix matrix, BaseMatrixWindow basewindow)
		{
			if (!(basewindow is MatrixWindow window) || matrix==null) return;

			using (Cell.LineStd)
			{
				using (Cell.RowPx(60)) 
					if (Draw.Button("1 - m")) matrix.InvertOne();

				using (Cell.RowPx(60)) 
					if (Draw.Button("m + 0.5")) matrix.Add(0.5f);

				using (Cell.RowPx(60)) 
					if (Draw.Button("m * 2")) matrix.Multiply(2); 
			}

			using (Cell.LineStd)
			{
				using (Cell.RowPx(60)) 
					if (Draw.Button("-m")) matrix.Invert();

				using (Cell.RowPx(60)) 
					if (Draw.Button("m - 0.5")) matrix.Add(-0.5f);

				using (Cell.RowPx(60)) 
					if (Draw.Button("m / 2")) matrix.Multiply(0.5f); 
			}

			if (Cell.current.valChanged)
				window.SetMatrix(matrix, window.name);
		}
	}


	public class ImportPlugin : IPlugin
	{
		public bool Enabled { get; set; }
		public string Name => "Import";

		public Texture2D importTexture;
		public TerrainData importTerrain;
		public MatrixAsset importAsset;

		public void Setup (Matrix matrix) { }

		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) { }

		public void DrawInspector (Matrix matrix, BaseMatrixWindow basewindow)
		{
			if (!(basewindow is MatrixWindow window)) return; 

			using (Cell.LineStd)
			{
				using (Cell.RowPx(50)) Draw.Label("Asset");
				using (Cell.Row) Draw.ObjectField(ref importAsset, allowSceneObject:true);
				using (Cell.RowPx(60))
				{
					//if (Draw.Button("Export")  &&  exportAsset!=null)
				}


				Cell.EmptyRowPx(10);
				using (Cell.RowPx(60))
				{
					//if (Draw.Button("Save...")) {}
				}
			}

			using (Cell.LineStd)
			{
				using (Cell.RowPx(50)) Draw.Label("Texture");
				using (Cell.Row) Draw.ObjectField(ref importTexture, allowSceneObject:true);
				using (Cell.RowPx(60))
					if (Draw.Button("Import")  &&  importTexture!=null)
					{
						TextureToMatrix(importTexture, ref matrix);
						window.SetMatrix(matrix, importTexture.name);
					}
				Cell.EmptyRowPx(10);
				using (Cell.RowPx(60))
					if (Draw.Button("Load..."))
					{
						Texture2D texture = ScriptableAssetExtensions.LoadAsset<Texture2D>();
						if (texture != null)
						{
							TextureToMatrix(texture, ref matrix);
							window.SetMatrix(matrix, texture.name);
						}
					}
			}

			using (Cell.LineStd)
			{
				using (Cell.RowPx(50)) Draw.Label("Terrain");
				using (Cell.Row) Draw.ObjectField(ref importTerrain, allowSceneObject:true);
				using (Cell.RowPx(60))
					if (Draw.Button("Import")  &&  importTexture!=null)
					{
						TerrainToMatrix(importTerrain, ref matrix);
						window.SetMatrix(matrix, importTerrain.name);
					}
				Cell.EmptyRowPx(10);
				using (Cell.RowPx(60))
					if (Draw.Button("Load..."))
					{
						TerrainData terrain = ScriptableAssetExtensions.LoadAsset<TerrainData>();
						if (terrain != null)
						{
							TerrainToMatrix(terrain, ref matrix);
							window.SetMatrix(matrix, terrain.name);
						}
					}
			}
		}

		
		public static void TextureToMatrix (Texture2D tex, ref Matrix matrix)
		{
			if (matrix == null || tex.width != matrix.rect.size.x || tex.height != matrix.rect.size.z)
				matrix = new Matrix( new CoordRect(0,0,tex.width, tex.height) );
			matrix.ImportTexture(tex, channel:0);
		}


		public static void TerrainToMatrix (TerrainData terrainData, ref Matrix matrix)
		{
			int res = terrainData.heightmapResolution;
			if (matrix == null || res != matrix.rect.size.x || res != matrix.rect.size.z)
				matrix = new Matrix( new CoordRect(0,0,res,res) );
			matrix.ImportHeights(terrainData.GetHeights(0,0, terrainData.heightmapResolution, terrainData.heightmapResolution));
		}
	}


	public class ExportPlugin : IPlugin
	{
		public bool Enabled { get; set; }
		public string Name => "Export";
		public int margins = 0;

		public Texture2D exportTexture;
		public TerrainData exportTerrain;
		public MatrixAsset exportAsset;

		public void Setup (Matrix matrix) { }

		public void DrawWindow (Matrix matrix, BaseMatrixWindow window) { }

		public void DrawInspector (Matrix matrix, BaseMatrixWindow window)
		{
			if (matrix==null) return;

			using (Cell.LineStd) Draw.Field(ref margins, "Margins");

			using (Cell.LineStd)
			{
				using (Cell.RowPx(50)) Draw.Label("Asset");
				using (Cell.Row) Draw.ObjectField(ref exportAsset, allowSceneObject:true);
				using (Cell.RowPx(60))
					if (Draw.Button("Save..."))
					{
						MatrixAsset matrixAsset = new MatrixAsset();
						matrixAsset.matrix = matrix;
						matrixAsset.RefreshPreview();
						ScriptableAssetExtensions.SaveAsset(matrixAsset);
					}


				Cell.EmptyRowPx(10);
				using (Cell.RowPx(60))
				{
					//if (Draw.Button("Save...")) {}
				}
			}

			using (Cell.LineStd)
			{
				using (Cell.RowPx(50)) Draw.Label("Texture");
				using (Cell.Row) Draw.ObjectField(ref exportTexture, allowSceneObject:true);
				using (Cell.RowPx(60))
					if (Draw.Button("Export")  &&  exportTexture!=null)
						MatrixToTexture(matrix, ref exportTexture);

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(60))
					if (Draw.Button("Save..."))
					{
						Texture2D texture = null;
						MatrixToTexture(matrix, ref texture);
						ScriptableAssetExtensions.SaveTexture(texture);
					}

			}

			using (Cell.LineStd)
			{
				using (Cell.RowPx(50)) Draw.Label("Terrain");
				using (Cell.Row) Draw.ObjectField(ref exportTerrain, allowSceneObject:true);
				using (Cell.RowPx(60))
					if (Draw.Button("Export")  &&  exportTerrain!=null)
						MatrixToTerrain(matrix, exportTerrain);

				Cell.EmptyRowPx(10);
				using (Cell.RowPx(60))
				{
					//if (Draw.Button("Save...")) {}
				}
			}
		}

		
		public void MatrixToTexture (Matrix matrix, ref Texture2D tex)
		{
			CoordRect rect = matrix.rect.Expanded(-margins);

			if (tex == null || tex.width != rect.size.x || tex.height != rect.size.z)
			{
				GameObject.DestroyImmediate(tex);
				tex = new Texture2D(rect.size.x, rect.size.z, textureFormat:TextureFormat.RGB24, mipChain:false);
				tex.filterMode = FilterMode.Point;
			}

			matrix.ExportTextureRaw(tex, new Coord(-margins, -margins));
		}


		public void MatrixToTerrain (Matrix matrix, TerrainData terrainData)
		{
			float[,] heights = new float[matrix.rect.size.x, matrix.rect.size.z];
			matrix.ExportHeights(heights);
			terrainData.SetHeights(0,0,heights);
		}
	}


}