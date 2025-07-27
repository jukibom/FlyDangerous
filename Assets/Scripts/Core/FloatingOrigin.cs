using Misc;
using UnityEngine;

namespace Core {
    public class FloatingOrigin : Singleton<FloatingOrigin> {

        public delegate void FloatingOriginCorrectionAction(Vector3 offset);

        [SerializeField] private Vector3 origin;

        // The object to track - this should be the local client player
        [SerializeField] private Transform focalTransform;

        // Distance required to perform a correction. If 0, will occur every frame.
        [SerializeField] public float correctionDistance = 1000.0f;

        public Vector3 Origin => origin;

        public Transform FocalTransform => focalTransform;

        public Vector3 FocalObjectPosition => FocalTransform.position + Origin;

        private void FixedUpdate() {
            CheckNeedsUpdate();
        }

        public void CheckNeedsUpdate() {
            // if we have a focal object, perform the floating origin fix
            if (FocalTransform && FocalTransform.position.magnitude > correctionDistance) {
                var offset = FocalTransform.position;
                DoFloatingOriginCorrection(Origin + offset, offset);
            }
        }

        public void DoFloatingOriginCorrection(Vector3 newOrigin, Vector3 offset) {
            origin = newOrigin;

            // reset the focal object (local player) to 0,0,0
            FocalTransform.position = Vector3.zero;

            OnFloatingOriginCorrection?.Invoke(offset);
        }

        // Draw a bounding sphere on selection in the editor
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, correctionDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position - Origin, correctionDistance);
        }

        // Return the "real world" position of a given transform
        public Vector3 GetAbsoluteWorldPosition(Transform objectTransform) {
            return objectTransform.position + Origin;
        }

        public void SetAbsoluteWorldPosition(Transform objectTransform, Vector3 newPosition, Quaternion? newRotation = null) {
            var worldPosition = newPosition - Origin;
            if (newRotation.HasValue) 
                objectTransform.SetPositionAndRotation(worldPosition, newRotation.Value);
            else 
                objectTransform.position = worldPosition;
        }

        public void SwapFocalTransform(Transform newTransform) {
            if (FocalTransform == newTransform) return;
            
            var delta = newTransform.position;
            focalTransform = newTransform;
            
            DoFloatingOriginCorrection(Origin + delta, delta);
            
            if (focalTransform.TryGetComponent<Rigidbody>(out var rb)) {
                rb.MovePosition(focalTransform.position);
            }
        }

        public void ResetOrigin() {
            var offset = -Origin;

            OnFloatingOriginCorrection?.Invoke(offset);
            origin = Vector3.zero;
        }

        public static event FloatingOriginCorrectionAction OnFloatingOriginCorrection;
    }
}