using UnityEngine;

public class CurvedLinePoint : MonoBehaviour {
    [HideInInspector] public bool showGizmo = true;
    [HideInInspector] public float gizmoSize = 0.1f;
    [HideInInspector] public Color gizmoColor = new(1, 0, 0, 0.5f);

    private void OnDrawGizmos() {
        if (showGizmo) {
            Gizmos.color = gizmoColor;

            Gizmos.DrawSphere(transform.position, gizmoSize);
        }
    }

    //update parent line when this point moved
    private void OnDrawGizmosSelected() {
        var curvedLine = GetComponentInParent<CurvedLineRenderer>();

        if (curvedLine != null) curvedLine.Update();
    }
}