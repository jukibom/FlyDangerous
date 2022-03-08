using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Gameplay;
using UnityEngine;
using CameraType = Gameplay.CameraType;

namespace Core.Player {
    public class ShipCameraRig : MonoBehaviour {
        
        [SerializeField] public List<ShipCamera> cameras;
        [SerializeField] private Transform cameraTarget;

        private static readonly Vector3 baseTargetPosition = new(0,0,20);
        private Transform _transform;
        private Vector3 _cameraOffset;
        private Vector2 _currentRotation;

        // This should be a vec2 from -1 to 1
        public Vector2 CameraUserRotation { get; set; }

        public ShipCamera ActiveCamera { get; private set; }
        
        private void Start() {
            _transform = transform;
            // Set active camera from preference
            var preferredCameraName = Preferences.Instance.GetString("preferredCamera");
            var preferredCamera = cameras.Find(c => c.Name == preferredCameraName);
            SetActiveCamera(preferredCamera != null ? preferredCamera : cameras.Last());
        }
        
        private void OnDisable() {
            Preferences.Instance.SetString("preferredCamera", ActiveCamera.Name);
            Preferences.Instance.Save();
        }

        private void Update() {
            // reset rotation before processing input
            cameraTarget.localPosition = baseTargetPosition;
            cameraTarget.transform.rotation = _transform.rotation;
            
            if (ActiveCamera.cameraType == CameraType.FirstPerson) {
                
                _currentRotation = new Vector2(
                    Mathf.Lerp(_currentRotation.x, CameraUserRotation.x, 0.01f),
                    Mathf.Lerp(_currentRotation.y, CameraUserRotation.y, 0.01f)
                );
                var angleY = _currentRotation.y * 90;
                var angleX = _currentRotation.x * 90;
                
                // Use the starting position of the active camera as the pivot otherwise the cinemachine system
                // will FREAK THE FUCK OUT trying to update the position while basing that formula on the position itself
                var pivot = _transform.TransformPoint(ActiveCamera.BaseLocalPosition);

                cameraTarget.RotateAround(pivot, _transform.right, -angleY);
                cameraTarget.RotateAround(pivot, _transform.up, angleX);
            }
            
            else if (ActiveCamera.cameraType == CameraType.ThirdPerson) {
                // input is used to rotate the view around the ship
                // bias towards looking forward
                if (Mathf.Abs(CameraUserRotation.x) < 0.2f && Mathf.Abs(CameraUserRotation.y) < 0.2f) {
                    CameraUserRotation = new Vector2(0, 1);
                }

                _currentRotation = new Vector2(
                    Mathf.Lerp(_currentRotation.x, CameraUserRotation.x, 0.02f),
                    Mathf.Lerp(_currentRotation.y, CameraUserRotation.y, 0.02f)
                );
                

                var rotationRads = Mathf.Atan2(_currentRotation.x, _currentRotation.y);
                
                cameraTarget.RotateAround(_transform.position, _transform.up, rotationRads * Mathf.Rad2Deg);
            }

            var cameraOffsetWorld = _transform.position - _transform.TransformPoint(_cameraOffset);
            cameraTarget.position -= cameraOffsetWorld;
        }

        public void Reset() {
            _cameraOffset = Vector3.zero;
            _currentRotation = Vector2.zero;
            cameraTarget.localPosition = baseTargetPosition;
        }

        public void UpdateCameras(Vector3 velocity, float maxVelocity, Vector3 force, float maxForce) {
            ActiveCamera.UpdateFov(velocity, maxVelocity);
            _cameraOffset = ActiveCamera.GetCameraOffset(force, maxForce);
        }

        public void ToggleActiveCamera() {
            var index = cameras.IndexOf(ActiveCamera);
            SetActiveCamera(index == cameras.Count - 1 ? cameras[0] : cameras[index + 1]);
        }

        private void SetActiveCamera(ShipCamera newCamera) {
            Reset();
            ActiveCamera = newCamera;
            ActiveCamera.Camera.MoveToTopOfPrioritySubqueue();
        }
    }
}