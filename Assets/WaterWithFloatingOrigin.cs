using Core;
using UnityEngine;

public class WaterWithFloatingOrigin : MonoBehaviour {
    private MeshRenderer _meshRenderer;
    private static readonly int FloatingOriginOffset = Shader.PropertyToID("_FloatingOriginOffset");

    [Tooltip("The default unity plane has a uv scale of 10 as it has 10x10 segments which each map to 1 full texture")] [SerializeField]
    private Vector2 planeUvScale = new(10, 10);

    private float xOffset;
    private float yOffset;
    private static readonly int PlaneSizeMeters = Shader.PropertyToID("_PlaneSizeMeters");

    private void OnEnable() {
        FloatingOrigin.OnFloatingOriginCorrection += OnFloatingOriginCorrection;
        _meshRenderer = GetComponent<MeshRenderer>();

        var scale = transform.localScale;
        _meshRenderer.material.SetVector(PlaneSizeMeters, new Vector4(scale.x * planeUvScale.x, scale.z * planeUvScale.x));
    }

    private void OnDisable() {
        FloatingOrigin.OnFloatingOriginCorrection -= OnFloatingOriginCorrection;
    }

    private void OnFloatingOriginCorrection(Vector3 offset) {
        var planeTransform = transform;
        var position = planeTransform.position;

        // water stays at fixed x and z position but height is maintained by floating origin
        planeTransform.position = new Vector3(
            position.x,
            position.y - offset.y,
            position.z
        );

        // var x = positiveX ? offset.x : -offset.x;
        // var y = positiveY ? offset.z : -offset.z;
        //
        // xOffset += x;
        // yOffset += y;

        // water is on a split plane of 10x10, and scaled to 10,000m^2, therefore 1m = 0.001f in this case
        // _meshRenderer.material.SetVector(FloatingOriginOffset, new Vector4(xOffset * floatingOriginScaler.x, yOffset * floatingOriginScaler.y));
        // var xblah = positiveX ? FloatingOrigin.Instance.Origin.x : -FloatingOrigin.Instance.Origin.x;
        // var yblah = positiveY ? FloatingOrigin.Instance.Origin.z : -FloatingOrigin.Instance.Origin.z;
        // _meshRenderer.material.SetVector(FloatingOriginOffset, new Vector4(xblah, yblah));

        var x = -FloatingOrigin.Instance.Origin.x; // / scale.x;
        var y = -FloatingOrigin.Instance.Origin.z; // / scale.z;
        _meshRenderer.material.SetVector(FloatingOriginOffset, new Vector4(x, y));
    }
}