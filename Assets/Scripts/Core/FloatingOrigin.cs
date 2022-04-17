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

        public Transform FocalTransform {
            get => focalTransform;
            set {
                focalTransform = value;
                origin = Vector3.zero;
            }
        }

        public Vector3 FocalObjectPosition => FocalTransform.position + Origin;

        private void FixedUpdate() {
            // if we have a focal object, perform the floating origin fix
            if (FocalTransform && FocalTransform.position.magnitude > correctionDistance) {
                var focalPosition = FocalTransform.position;
                origin += focalPosition;

                // reset focal object (local player) to 0,0,0
                FocalTransform.position = Vector3.zero;

                OnFloatingOriginCorrection?.Invoke(focalPosition);
            }
        }

        // Draw a bounding sphere on selection in the editor
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, correctionDistance);
        }

        // Replace the focal transform without resetting the origin - the objects should always be at the same place when doing this!
        public void SwapFocalTransform(Transform newTransform) {
            focalTransform = newTransform;
        }

        public void ForceUpdate() {
            FixedUpdate();
        }

        public static event FloatingOriginCorrectionAction OnFloatingOriginCorrection;
    }
}