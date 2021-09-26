using Misc;
using UnityEngine;

namespace Core {
    public class FloatingOrigin : Singleton<FloatingOrigin> {
        
        public delegate void FloatingOriginCorrectionAction(Vector3 offset);

        public static event FloatingOriginCorrectionAction OnFloatingOriginCorrection;

        [SerializeField] private Vector3 origin;
        public Vector3 Origin => origin;
        
        // The object to track - this should be the local client player
        [SerializeField] private Transform focalTransform; 
        public Transform FocalTransform {
            get => focalTransform;
            set {
                focalTransform = value;
                origin = Vector3.zero;
            }
        }

        // Distance required to perform a correction. If 0, will occur every frame.
        [SerializeField] public float correctionDistance = 1000.0f;

        public Vector3 FocalObjectPosition => FocalTransform.position + Origin;

        void FixedUpdate() {

            // if we have a focal object, perform the floating origin fix
            if (FocalTransform && FocalTransform.position.magnitude > correctionDistance) {
                var focalPosition = FocalTransform.position;
                origin += focalPosition;

                UpdateTrails(focalPosition);
                OnFloatingOriginCorrection?.Invoke(focalPosition);

                // reset focal object (local player) to 0,0,0
                FocalTransform.position = Vector3.zero;
            }
        }

        private void UpdateTrails(Vector3 offset) {
            var trails = FindObjectsOfType<TrailRenderer>();
            foreach (var trail in trails)
            {
                var positions = new Vector3[trail.positionCount];

                int positionCount = trail.GetPositions(positions);
                for (int i = 0; i < positionCount; ++i)
                    positions[i] -= offset;

                trail.SetPositions(positions);
            }
        }
    }
}