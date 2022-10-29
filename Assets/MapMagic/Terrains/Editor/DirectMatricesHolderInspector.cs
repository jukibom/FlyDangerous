
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;

using MapMagic.Nodes;
using MapMagic.Terrains;


namespace MapMagic.Core.GUI 
{
	[CustomEditor(typeof(DirectMatricesHolder))]
	public class DirectMatricesHolderInspector : Editor
	{
		UI ui = new UI();

		public override void  OnInspectorGUI ()
		{
			ui.Draw(DrawGUI, inInspector:true);
		}

		public void DrawGUI ()
		{
			DirectMatricesHolder holder = (DirectMatricesHolder)target;

			using (Cell.Line)
			{
				Cell layersCell = Cell.current;

				using (Cell.LinePx(0)) 
					LayersEditor.DrawLayersThemselves(Cell.current, 
						holder.maps.Count,
						onDraw:n => DrawLayer(holder.maps.GetKeyByNum(n), holder.maps[n]) );
			}
		}

		private static void DrawLayer (string name, Matrix map)
		{
			Cell.EmptyLinePx(4);

			using (Cell.LineStd)
			{
				Cell.EmptyRowPx(4);
				//using (Cell.RowPx(64)) Draw.TextureIcon(texture); 
				using (Cell.Row) Draw.Label(name);
				Cell.EmptyRowPx(4);
			}

			Cell.EmptyLinePx(4);
		}

	}//class

}//namespace