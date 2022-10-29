
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;


//namespace MapMagic.Core 
namespace Den.Tools
{
	[CustomEditor(typeof(MatrixAsset))]
	public class MatrixAssetInspector : Editor
	{
		MatrixAsset matrixAsset;
		UI ui = new UI(); 

		bool colorize = false;
		bool relief = false;


		public override void OnInspectorGUI ()
		{
			ui.Draw(DrawGUI, inInspector:true);
		}


		private void DrawGUI ()
		{
			matrixAsset = (MatrixAsset)target;

			using (Cell.LinePx(32))
				Draw.Label("WARNING: Serializing this asset when selected in \nInspector can slow down editor GUI performance.", style:UI.current.styles.helpBox);

			Cell.EmptyLinePx(5);

			if (matrixAsset.matrix != null  &&  matrixAsset.preview == null)
				matrixAsset.RefreshPreview();

			if (matrixAsset.preview != null)
			{
				using (Cell.LinePx(256))
				{
					Cell.EmptyRowRel(1);
					using (Cell.RowPx(256)) 
					{
						//Draw.Texture(matrixAsset.preview);
						Draw.MatrixPreviewTexture(matrixAsset.preview, colorize:colorize, relief:relief, min:0, max:1);
						Draw.MatrixPreviewReliefSwitch(ref colorize, ref relief);
					}
					Cell.EmptyRowRel(1);
				}

				if (matrixAsset.rawPath != null)
					using (Cell.LineStd) Draw.Label(matrixAsset.rawPath);

				if (matrixAsset.matrix != null)
					using (Cell.LineStd) Draw.Label(matrixAsset.matrix.rect.size.x + ", " + matrixAsset.matrix.rect.size.z);
			}

			Cell.EmptyLinePx(5);

			using (Cell.LineStd) Draw.Field(ref matrixAsset.source, "Map Source");

			if (matrixAsset.source == MatrixAsset.Source.Raw)
			{
				using (Cell.LinePx(22)) Draw.Label("Square gray 16bit RAW, PC byte order", style:UI.current.styles.helpBox);

				using (Cell.LineStd) if (Draw.Button("Load RAW"))
				{
					string newPath = EditorUtility.OpenFilePanel("Import Texture File", "", "raw,r16");

					if (newPath!=null && newPath.Length!=0)
					{
						UnityEditor.Undo.RecordObject(this, "Import RAW");
						matrixAsset.rawPath = newPath;

						matrixAsset.Reload();

						EditorUtility.SetDirty(matrixAsset);
					}
				}
			}

			else //texture
				using (Cell.LinePx(0))
				{
					using (Cell.LineStd) Draw.ObjectField(ref matrixAsset.textureSource, "Texture"); //
					using (Cell.LineStd) Draw.Field(ref matrixAsset.channelSource, "Channel"); //

					if (Cell.current.valChanged)
						matrixAsset.Reload();
				}

			using (Cell.LineStd)
			{
				Cell.current.disabled = 
					(matrixAsset.source == MatrixAsset.Source.Raw && matrixAsset.rawPath == null) ||
					(matrixAsset.source == MatrixAsset.Source.Texture && matrixAsset.textureSource == null);
				if (Draw.Button("Reload"))
				{
					matrixAsset.Reload();
					EditorUtility.SetDirty(matrixAsset);
				}
			}
		}
	}

	class MatrixAssetTexturePostprocessor : AssetPostprocessor 
	{
		//public static WeakEvent<Texture2D> OnTextureImported = new WeakEvent<Texture2D>();
		//In MatrixAsset since it should work in non-editors

		//public void OnPostprocessTexture(Texture2D tex)  //using OnPostprocessAllAssets because tex here is not the same tex as the changed one

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			for (int a=0; a<importedAssets.Length; a++)
			{
				if (AssetDatabase.GetMainAssetTypeAtPath(importedAssets[a]) != typeof(Texture2D)) continue;
				Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(importedAssets[a]);

				if (MatrixAsset.OnTextureImported != null)
					MatrixAsset.OnTextureImported(tex);
			}
		}
	}
}