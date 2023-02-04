using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CurvedLineRenderer : MonoBehaviour {
    public float lineSegmentSize = 0.15f;
    public float lineWidth = 0.1f;
    [Header("Gizmos")] public bool showGizmos = true;
    public float gizmoSize = 0.1f;

    public Color gizmoColor = new(1, 0, 0, 0.5f);

    private CurvedLinePoint[] linePoints = new CurvedLinePoint[0];
    private Vector3[] linePositions = new Vector3[0];
    private Vector3[] linePositionsOld = new Vector3[0];

    // Update is called once per frame
    public void Update() {
        GetPoints();
        SetPointsToLine();
    }

    private void GetPoints() {
        //find curved points in children
        linePoints = GetComponentsInChildren<CurvedLinePoint>();

        //add positions
        linePositions = new Vector3[linePoints.Length];
        for (var i = 0; i < linePoints.Length; i++) linePositions[i] = linePoints[i].transform.position;
    }

    private void SetPointsToLine() {
        //create old positions if they dont match
        if (linePositionsOld.Length != linePositions.Length) linePositionsOld = new Vector3[linePositions.Length];

        //check if line points have moved
        var moved = false;
        for (var i = 0; i < linePositions.Length; i++)
            //compare
            if (linePositions[i] != linePositionsOld[i])
                moved = true;

        //update if moved
        if (moved) {
            var line = GetComponent<LineRenderer>();

            //get smoothed values
            var smoothedPoints = LineSmoother.SmoothLine(linePositions, lineSegmentSize);

            //set line settings
            line.SetVertexCount(smoothedPoints.Length);
            line.SetPositions(smoothedPoints);
            line.SetWidth(lineWidth, lineWidth);
        }
    }

    private void OnDrawGizmosSelected() {
        Update();
    }

    private void OnDrawGizmos() {
        if (linePoints.Length == 0) GetPoints();

        //settings for gizmos
        foreach (var linePoint in linePoints) {
            linePoint.showGizmo = showGizmos;
            linePoint.gizmoSize = gizmoSize;
            linePoint.gizmoColor = gizmoColor;
        }
    }
}