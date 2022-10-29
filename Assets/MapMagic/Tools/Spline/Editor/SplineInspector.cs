using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Den.Tools;
using Den.Tools.GUI;

namespace Den.Tools.AutoSplines
{
	//[CustomEditor(typeof(SplineObject))]
/*	public static class SplineInspector// : Editor
	{ 
		public static bool guiLines = true;
		public static bool guiKnobs = true;
		public static bool guiDisplay = true;


		public static void DrawSpline (SplineSys sys)
		{
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref guiDisplay, "Display", isLeft:true))
					if (guiDisplay)
					{
						using (Cell.LineStd) Draw.ToggleLeft(ref sys.guiDrawNodes, "Draw Nodes");
						using (Cell.LineStd) Draw.ToggleLeft(ref sys.guiDrawSegments, "Draw Segments");
						using (Cell.LineStd) Draw.ToggleLeft(ref sys.guiDrawDots, "Draw Dots");
						using (Cell.LineStd) Draw.Field(ref sys.guiDotsCount, "Dots Count");
						using (Cell.LineStd) Draw.Toggle(ref sys.guiDotsEquidist, "Dots Equidist");

						if (Cell.current.valChanged)
							SceneView.lastActiveSceneView?.Repaint();
					}

			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref guiLines, "Lines", isLeft:true))
					if (guiLines)
					{
						using (Cell.LineStd) Draw.DualLabel("Lines Count", sys.lines.Length.ToString());

						if (SplineEditor.selectedKnobs.Count == 1)
						{
							(int l, int n) = SplineEditor.selectedKnobs.Any();
							Line line = sys.lines[l];

							using (Cell.LineStd) Draw.DualLabel("Length", line.length.ToString());
							using (Cell.LineStd) Draw.DualLabel("Segments", line.segments.Length.ToString());
							using (Cell.LineStd) Draw.Toggle(ref line.looped, "Looped");

							using (Cell.LineStd)
								if (Draw.Button("Remove"))
								{
									sys.RemoveLine(l);
									SplineEditor.selectedKnobs.Clear();
									SplineEditor.dispSelectedKnobs.Clear();
								}
						}

						using (Cell.LineStd)
							if (Draw.Button("Add"))
								sys.AddLine(
									SceneView.lastActiveSceneView.pivot - new Vector3(SceneView.lastActiveSceneView.cameraDistance/10,0,0),
									SceneView.lastActiveSceneView.pivot + new Vector3(SceneView.lastActiveSceneView.cameraDistance/10,0,0) );
					}

			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref guiKnobs, "Knobs", isLeft:true))
					if (guiKnobs)
					{
						using (Cell.LineStd) Draw.DualLabel("Selected", SplineEditor.selectedKnobs.Count.ToString());
						
						if (SplineEditor.selectedKnobs.Count == 1)
						{
							Cell.EmptyLinePx(4);
							(int l, int n) = SplineEditor.selectedKnobs.Any();
							using (Cell.LinePx(0)) DrawKnob(sys, l, n);
						}
					}

			Cell.EmptyLinePx(4);
			using (Cell.LineStd)
				if (Draw.Button("Update"))
				{
					sys.Update();
					SceneView.lastActiveSceneView?.Repaint();
				}
		}

		public static void DrawKnob (SplineSys sys, int l, int n)
		{
			using (Cell.LineStd) Draw.DualLabel("Number", "Line:" + l.ToString() + ", Node:" + n.ToString());
			using (Cell.LineStd) Draw.Toggle(n==0, "Is First");
			using (Cell.LineStd) Draw.Toggle(n==sys.lines[l].segments.Length, "Is Last");

			SplineEditor.Knob knob = sys.lines[l].GetKnob(n);

			using (Cell.LinePx(0))
			{
				using (Cell.LineStd) Draw.Field(ref knob.pos, "Position");
				//using (Cell.LineStd) Draw.DualLabel("In Dir", knob.inDir.ToString());
				//using (Cell.LineStd) Draw.DualLabel("Out Dir", knob.outDir.ToString());
				using (Cell.LineStd) Draw.Field(ref knob.inDir, "In Dir");
				using (Cell.LineStd) Draw.Field(ref knob.outDir, "Out Dir");
				using (Cell.LineStd) Draw.Field(ref knob.type, "Type");

				if (Cell.current.valChanged)
				{
					sys.lines[l].SetKnob(knob, n);
					sys.lines[l].Update();
					SceneView.lastActiveSceneView?.Repaint();
				}
			}
		}
	}*/
}
