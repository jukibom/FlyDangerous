using System;
using UnityEngine;
using UnityEditor;

namespace Den.Tools.SceneEdit
{
	public static class MoveRotateScale
	{
		public static bool isDragging;
		public static Vector3 origPivot;
		public static Vector3[] origPositions;


		public static bool Update (Vector3[] poses, Vector3 pivot)
		/// Moves, rotates or scales the given positions. Returns true if any change was made.
		/// Should be fired once per frame
		{
			if (UnityEditor.Tools.current == Tool.Move)
				return Move(poses, pivot); 
			
			if (UnityEditor.Tools.current == Tool.Scale)
				return Scale(poses, pivot);

			if (UnityEditor.Tools.current == Tool.Rotate)
				return Rotate(poses, pivot);

			return false;
		}


		public static bool Scale (Vector3[] poses, Vector3 pivot)
		{
			//don't ask
			bool mouseReleased = Event.current.rawType == EventType.MouseUp; 

			if (isDragging) pivot = origPivot; 

			float gizmoSize = HandleUtility.GetHandleSize(pivot);
			Vector3 newScale = Handles.ScaleHandle(new Vector3(1,1,1), pivot, Quaternion.identity, gizmoSize);

			//resetting gizmo on mouse up (after drawing gizmo)
			if (mouseReleased) 
			{
				origPositions = null;
				isDragging = false;
				return false;
			}

			//on dragging
			if (newScale != new Vector3(1,1,1))
			{
				//if just started - saving original positions
				if (!isDragging)
				{
					origPositions = new Vector3[poses.Length];
					Array.Copy(poses, origPositions, poses.Length);

					isDragging = true;

					origPivot = pivot;
				}

				//moving positions
				for (int p=0; p<poses.Length; p++)
				{
					Vector3 relPos = origPositions[p] - pivot;
					relPos = new Vector3(relPos.x*newScale.x, relPos.y*newScale.y, relPos.z*newScale.z); // relPos *= scale;
					poses[p] = relPos + pivot;

				}

				return true;
			}

			return false;
		}


		public static bool Rotate (Vector3[] poses, Vector3 pivot)
		{
			//don't ask
			bool mouseReleased = Event.current.rawType == EventType.MouseUp; 

			if (isDragging) pivot = origPivot; 

			float gizmoSize = HandleUtility.GetHandleSize(pivot);
			Quaternion rotation = Handles.RotationHandle(Quaternion.identity, pivot);

			//resetting gizmo on mouse up (after drawing gizmo)
			if (mouseReleased) 
			{
				origPositions = null;
				isDragging = false;
				return false;
			}

			//on dragging
			if (rotation != Quaternion.identity)
			{
				//if just started - saving original positions
				if (!isDragging)
				{
					origPositions = new Vector3[poses.Length];
					Array.Copy(poses, origPositions, poses.Length);

					isDragging = true;

					origPivot = pivot;
				}

				//moving positions
				for (int p=0; p<poses.Length; p++)
				{
					Vector3 relPos = origPositions[p] - pivot;
					relPos = rotation * relPos;
					poses[p] = relPos + pivot;

				}

				return true;
			}

			return false;
		}


		public static bool Move (Vector3[] poses, Vector3 pivot)
		{
			Vector3 newPivot = Handles.PositionHandle(pivot, Quaternion.identity);
			Vector3 delta = pivot - newPivot;
			if (delta.sqrMagnitude != 0) 
			{
				for (int p=0; p<poses.Length; p++)
					poses[p] -= delta;

				return true;
			}
			return false;

			/*//don't ask
			bool mouseReleased = Event.current.rawType == EventType.MouseUp; 

			if (isDragging) pivot = origPivot; 

			float gizmoSize = HandleUtility.GetHandleSize(pivot);
			Vector3 position = Handles.PositionHandle(pivot, Quaternion.identity);

			//resetting gizmo on mouse up (after drawing gizmo)
			if (mouseReleased) 
			{
				origPositions = null;
				isDragging = false;
				return false;
			}

			//on dragging
			if (position != origPivot)
			{
				//if just started - saving original positions
				if (!isDragging)
				{
					origPositions = new Vector3[poses.Length];
					Array.Copy(poses, origPositions, poses.Length);

					isDragging = true;

					origPivot = pivot;
				}

				//moving positions
				for (int p=0; p<poses.Length; p++)
					poses[p] = origPositions[p] + position-pivot;

				return true;
			}

			return false;*/
		}
	}
}
