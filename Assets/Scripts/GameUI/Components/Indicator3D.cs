using Misc;
using UnityEngine;

namespace GameUI.Components {
    public class Indicator3D : MonoBehaviour {
        [SerializeField] private MeshRenderer frontTriangleTop;
        [SerializeField] private MeshRenderer frontTriangleBottom;
        [SerializeField] private MeshRenderer leftTriangleTop;
        [SerializeField] private MeshRenderer leftTriangleBottom;
        [SerializeField] private MeshRenderer rightTriangleTop;
        [SerializeField] private MeshRenderer rightTriangleBottom;

        private Transform _leftTriangleTopTransform;
        private Transform _leftTriangleBottomTransform;
        private Transform _rightTriangleTopTransform;
        private Transform _rightTriangleBottomTransform;

        private void Awake() {
            _leftTriangleTopTransform = leftTriangleTop.transform;
            _leftTriangleBottomTransform = leftTriangleBottom.transform;
            _rightTriangleTopTransform = rightTriangleTop.transform;
            _rightTriangleBottomTransform = rightTriangleBottom.transform;
        }

        // facing value = 0 when facing (either toward or away) from camera and 1 when it isn't.
        public void SetFacingValueNormalized(float facingValue) {
            // opacity of front triangle
            var material = frontTriangleTop.material;
            var meshMaterial = material;
            var opactiyColor = new Color(meshMaterial.color.r, meshMaterial.color.g, meshMaterial.color.b, facingValue);
            material.color = opactiyColor;
            frontTriangleBottom.material.color = opactiyColor;

            // position of side triangles (this is even more gross than above but honestly who cares)
            _leftTriangleTopTransform.localPosition = new Vector3(0, facingValue.Remap(0, 1, 0.2f, 0), 0);
            _leftTriangleBottomTransform.localPosition = new Vector3(0, facingValue.Remap(0, 1, -0.2f, 0), 0);
            _leftTriangleTopTransform.localRotation = Quaternion.Euler(facingValue.Remap(0, 1, -70, -90), -90, 90);
            _leftTriangleBottomTransform.localRotation = Quaternion.Euler(facingValue.Remap(0, 1, -70, -90), 90, 90);

            _rightTriangleTopTransform.localPosition = new Vector3(0, facingValue.Remap(0, 1, 0.2f, 0), 0);
            _rightTriangleBottomTransform.localPosition = new Vector3(0, facingValue.Remap(0, 1, -0.2f, 0), 0);
            _rightTriangleTopTransform.localRotation = Quaternion.Euler(facingValue.Remap(0, 1, -110, -90), -90, 90);
            _rightTriangleBottomTransform.localRotation = Quaternion.Euler(facingValue.Remap(0, 1, -110, -90), 90, 90);
        }
    }
}