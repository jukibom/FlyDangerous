using UnityEngine;

namespace Core {
    public class FloatingOrigin : MonoBehaviour {

        public static FloatingOrigin Instance { get; private set; }

        public delegate void FloatingOriginCorrectionAction(Vector3 offset);

        public static event FloatingOriginCorrectionAction OnFloatingOriginCorrection;

        public Vector3 Origin { get; private set; }
        
        // The object to track - this should be the local client player
        [SerializeField] private Transform focalTransform; 
        public Transform FocalTransform {
            get => focalTransform;
            set {
                focalTransform = value;
                Origin = Vector3.zero;
            }
        }

        // Distance required to perform a correction. If 0, will occur every frame.
        [SerializeField] public float correctionDistance = 1000.0f;

        public Vector3 FocalObjectPosition => FocalTransform.position + Origin;

        void Awake() {
            // singleton shenanigans
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }
        }

        void Update() {

            // if we have a focal object, perform the floating origin fix
            if (FocalTransform && FocalTransform.position.magnitude > correctionDistance) {
                var focalPosition = FocalTransform.position;
                Origin += focalPosition;

                OnFloatingOriginCorrection?.Invoke(focalPosition);

                // reset focal object (local player) to 0,0,0
                FocalTransform.position = Vector3.zero;
            }
        }
    }
}