
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;


namespace Den.Tools.Matrices
{
	[CustomEditor(typeof(MatrixObject))]
	public class MatrixObjectInspector : Editor
	{
		MatrixObject matrixObject;
		UI ui = new UI(); 

		bool colorize = false;
		bool relief = false;
	
		public override void OnInspectorGUI () => ui.Draw(DrawGUI, inInspector:true);
		private void DrawGUI ()
		/// Copy of MatrixAssetInspector
		/// TODO: reuse code
		{
			matrixObject = (MatrixObject)target;

			if (matrixObject.matrix != null  &&  matrixObject.preview == null)
				matrixObject.RefreshPreview();

			if (matrixObject.preview != null)
			{
				using (Cell.LinePx(256))
				{
					Cell.EmptyRowRel(1);
					using (Cell.RowPx(256)) 
					{
						//Draw.Texture(matrixAsset.preview);
						Draw.MatrixPreviewTexture(matrixObject.preview, colorize:colorize, relief:relief, min:0, max:1);
						Draw.MatrixPreviewReliefSwitch(ref colorize, ref relief);
					}
					Cell.EmptyRowRel(1);
				}

				if (matrixObject.rawPath != null)
					using (Cell.LineStd) Draw.Label(matrixObject.rawPath);

				if (matrixObject.matrix != null)
					using (Cell.LineStd) Draw.Label(matrixObject.matrix.rect.size.x + ", " + matrixObject.matrix.rect.size.z);
			}

			Cell.EmptyLinePx(5);

			using (Cell.LineStd) Draw.Field(ref matrixObject.source, "Map Source");

			if (matrixObject.source == MatrixAsset.Source.Raw)
			{
				using (Cell.LinePx(22)) Draw.Label("Square gray 16bit RAW, PC byte order", style:UI.current.styles.helpBox);

				using (Cell.LineStd) if (Draw.Button("Load RAW"))
				{
					string newPath = EditorUtility.OpenFilePanel("Import Texture File", "", "raw,r16");

					if (newPath!=null && newPath.Length==0)
					{
						UnityEditor.Undo.RecordObject(this, "Import RAW");
						matrixObject.rawPath = newPath;

						matrixObject.Reload();

						EditorUtility.SetDirty(matrixObject);
					}
				}
			}

			else if (matrixObject.source == MatrixAsset.Source.Texture)
				using (Cell.LinePx(0))
				{
					using (Cell.LineStd) Draw.ObjectField(ref matrixObject.textureSource, "Texture"); //
					using (Cell.LineStd) Draw.Field(ref matrixObject.channelSource, "Channel"); //

					if (Cell.current.valChanged)
						matrixObject.Reload();
				}

			else if (matrixObject.source == MatrixAsset.Source.New)
			{
				using (Cell.LinePx(0)) 
				{
					using (Cell.LineStd) Draw.Field(ref matrixObject.newRes, "Resolution");
					using (Cell.LineStd) Draw.Field(ref matrixObject.newOffset, "Offset");

					if (Cell.current.valChanged)
						matrixObject.Reload();
				}
			}

			using (Cell.LineStd)
			{
				Cell.current.disabled = 
					(matrixObject.source == MatrixAsset.Source.Raw && matrixObject.rawPath == null) ||
					(matrixObject.source == MatrixAsset.Source.Texture && matrixObject.textureSource == null);
				if (Draw.Button("Reload"))
				{
					matrixObject.Reload();
					EditorUtility.SetDirty(matrixObject);
				}
			}

			Cell.EmptyLinePx(5);
			using (Cell.LineStd) Draw.Field(ref matrixObject.worldPosition, "World Pos");
			using (Cell.LineStd) Draw.Field(ref matrixObject.worldSize, "World Size");
			using (Cell.LineStd) Draw.Field(ref matrixObject.worldHeight, "World Height");

			Cell.EmptyLinePx(5);
			using (Cell.LineStd) Draw.Field(ref matrixObject.displayGizmo, "Gizmo");
			using (Cell.LineStd) Draw.ToggleLeft(ref matrixObject.centerCell, "Center Cell");
			using (Cell.LineStd) Draw.Field(ref matrixObject.filterMode, "Filter Mode");
		}


		public virtual void OnSceneGUI()
		{
			if (matrixObject.displayGizmo == MatrixObject.DisplayGizmo.Texture)
			{
				if (matrixObject.textureGizmo == null) matrixObject.Reload();
				matrixObject.textureGizmo.SetOffsetSize(matrixObject.worldPosition, matrixObject.worldSize);
				matrixObject.textureGizmo.Draw();
			}

			if (matrixObject.displayGizmo == MatrixObject.DisplayGizmo.Height  ||  matrixObject.displayGizmo == MatrixObject.DisplayGizmo.FacetedHeight)
			{
				if (matrixObject.heightGizmo == null) matrixObject.Reload();
				matrixObject.heightGizmo.SetOffsetSize(
					(Vector3)matrixObject.worldPosition, 
					new Vector3(matrixObject.worldSize.x, matrixObject.worldHeight, matrixObject.worldSize.z));
				matrixObject.heightGizmo.Draw();
			}
		}
	}
}