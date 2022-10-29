using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Den.Tools.GUI;

namespace Den.Tools.Splines
{
	[CustomEditor(typeof(SplineObject))]
	public partial class SplineObjectInspector : Editor
	{
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
		static void DrawInactiveGizmo (SplineObject obj, GizmoType gizmoType)
		{
			//if (gizmoType.HasFlag(GizmoType.Selected)) Handles.color = new Color(1, 0.5f, 0, 1);
			//else if (gizmoType.HasFlag(GizmoType.NonSelected)) Handles.color = new Color(0.75f, 0.25f, 0, 1);
			//SplineEditor.DrawSplineSys(obj.splineSys, obj.transform.localToWorldMatrix);
			//drawing selected in OnSceneGUI - otherwise will be erased by matrix gizmo drawn in tester's OnSceneGUI

			if (gizmoType.HasFlag(GizmoType.NonSelected))
			{
				Handles.color = new Color(0.75f, 0.25f, 0, 1);
				SplineEditor.DrawSplineSys(obj.splineSys, obj.transform.localToWorldMatrix);
			}
		}

		private void OnSceneGUI () 
		{
			SplineObject splineObj = (SplineObject)target;

			Handles.color = new Color(1, 0.5f, 0, 1);
			SplineEditor.DrawSplineSys(splineObj.splineSys, splineObj.transform.localToWorldMatrix);

			SplineEditor.EditSplineSys(splineObj.splineSys, splineObj.transform.localToWorldMatrix, splineObj);
		}


		UI ui = new UI();
		public override void OnInspectorGUI () 
			{ ui.Draw(DrawGUI, inInspector:true); }

		private void DrawGUI ()
		{
			SplineObject splineObj = (SplineObject)target;
			//SplineInspector.DrawSpline(splineObj.splineSys);
		}
	}
}
