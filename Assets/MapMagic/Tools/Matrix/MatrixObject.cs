using UnityEngine;
using System.Collections;
using System;
using System.IO;

using Den.Tools;

namespace Den.Tools.Matrices
{
	public class MatrixObject : MonoBehaviour
	/// Same as MatrixAsset, but saved in scene instead of assets
	/// Plus preview gizmos
	/// Mostly for test purpose
	{
		[NonSerialized] public Matrix matrix = new Matrix( new CoordRect(0,0,1,1) );
		public MatrixWorld MatrixWorld => new MatrixWorld(matrix, (Vector3)worldPosition, new Vector3(worldSize.x, worldHeight, worldSize.z) );
		
		public Vector2D worldPosition;
		public Vector2D worldSize = new Vector2D(1000, 1000);
		public float worldHeight = 250;

		[NonSerialized] public Texture2D preview; 

		public MatrixAsset.Source source;

		//values to reload
		public string rawPath;
		public Texture2D textureSource;
		public MatrixAsset.Channel channelSource;
		public int newRes = 128;
		public Coord newOffset;

		//gizmos
		public enum DisplayGizmo { None, Texture, Height, FacetedHeight }
		public DisplayGizmo displayGizmo;
		public bool centerCell = true;
		public FilterMode filterMode;
		[NonSerialized] public MatrixHeightGizmo heightGizmo;
		[NonSerialized] public MatrixTextureGizmo textureGizmo;


		public void RefreshPreview (int size=128)
		{
			if (matrix != null)
			{
				Matrix previewMatrix = matrix;// new Matrix( new CoordRect(0,0,size,size) );
				//MatrixOps.Resize(matrix, previewMatrix);
				preview = new Texture2D(previewMatrix.rect.size.x, previewMatrix.rect.size.z);
				previewMatrix.ExportTexture(preview, -1);
			}
			else preview = TextureExtensions.ColorTexture(2,2,Color.black);
		}


		public void RefreshGizmos ()
		{
			switch (displayGizmo)
			{
				case DisplayGizmo.Texture:
					if (textureGizmo == null) textureGizmo = new MatrixTextureGizmo();
					textureGizmo.SetMatrix(matrix, centerCell:centerCell, filterMode:filterMode);
					break;

				case DisplayGizmo.Height:
					if (heightGizmo == null) heightGizmo = new MatrixHeightGizmo();
					heightGizmo.SetMatrix(matrix);
					break;

				case DisplayGizmo.FacetedHeight:
					if (heightGizmo == null) heightGizmo = new MatrixHeightGizmo();
					heightGizmo.SetMatrix(matrix, faceted:true);
					break;
			}
		}


		public void Reload ()
		{
			switch (source)
			{
				case MatrixAsset.Source.Raw:
					if (rawPath != null) 
						MatrixAsset.ImportRaw(ref matrix, rawPath);
					break;

				case MatrixAsset.Source.Texture:
					if (textureSource != null)
						MatrixAsset.ImportTexture(ref matrix, textureSource, channelSource);
					break;
				
				case MatrixAsset.Source.New:
					if (matrix == null  ||  matrix.rect.size.x !=newRes  ||  matrix.rect.size.z != newRes)
						matrix = new Matrix( new CoordRect(0,0,newRes,newRes) );
					else matrix.Fill(0);
					matrix.rect.offset = newOffset;
					break;
			}

			RefreshPreview();
			RefreshGizmos();
		}


	}
}