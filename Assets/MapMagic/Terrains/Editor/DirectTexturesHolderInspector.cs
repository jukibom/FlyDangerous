
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;

using MapMagic.Nodes;
using MapMagic.Terrains;


namespace MapMagic.Core.GUI 
{
	[CustomEditor(typeof(DirectTexturesHolder))]
	public class DirectTexturesHolderInspector : Editor
	{
		UI ui = new UI();

		public override void  OnInspectorGUI ()
		{
			ui.Draw(DrawGUI, inInspector:true);
		}

		public void DrawGUI ()
		{
			DirectTexturesHolder holder = (DirectTexturesHolder)target;

			using (Cell.Line)
			{
				Cell layersCell = Cell.current;

				using (Cell.LinePx(0)) 
					LayersEditor.DrawLayersThemselves(Cell.current, 
						holder.textures.Count,
						onDraw:n => DrawLayer(holder.textures.GetKeyByNum(n), holder.textures[n]) );
			}
		}

		private static void DrawLayer (string name, Texture2D texture)
		{
			Cell.EmptyLinePx(4);

			using (Cell.LinePx(64))
			{
				Cell.EmptyRowPx(4);
				using (Cell.RowPx(64)) Draw.TextureIcon(texture); 
				using (Cell.Row) Draw.Label(name);
				Cell.EmptyRowPx(4);
			}

			Cell.EmptyLinePx(4);
		}

	}//class

}//namespace