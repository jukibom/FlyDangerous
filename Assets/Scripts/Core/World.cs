using UnityEngine;

namespace Core {
    public class World : MonoBehaviour {
        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += PerformCorrection;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= PerformCorrection;
        }

        private void PerformCorrection(Vector3 offset) {
            foreach (Transform child in transform) child.position -= offset;
        }
    }
}