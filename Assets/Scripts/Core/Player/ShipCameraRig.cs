using System;
using System.Collections.Generic;
using Cinemachine;
using Gameplay;
using UnityEngine;
using CameraType = Gameplay.CameraType;

namespace Core.Player {
    public class ShipCameraRig : MonoBehaviour {
        
        [SerializeField] public List<ShipCamera> cameras;
        [SerializeField] private Transform cameraTarget;

        private Transform _transform;
        private Vector3 _baseTargetPosition;
        private Vector3 _cameraOffset;
        private Vector2 _currentRotation;

        // This should be a vec2 from -1 to 1
        public Vector2 CameraUserRotation { get; set; }

        public ShipCamera ActiveCamera { get; private set; }

        

        private void Start() {
            // Set active camera to preference
            // TODO: preference saving
            SetActiveCamera(cameras[0]);
            _baseTargetPosition = cameraTarget.localPosition;
            _transform = transform;
        }

        private void Update() {
            // reset rotation before processing input
            cameraTarget.localPosition = _baseTargetPosition;
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
                var x = CameraUserRotation.x;
                var y = Mathf.Abs(CameraUserRotation.y) > 0.2f ? CameraUserRotation.y : 0.2f;
                
                _currentRotation = new Vector2(
                    Mathf.Lerp(_currentRotation.x, x, 0.01f),
                    Mathf.Lerp(_currentRotation.y, y, 0.01f)
                );
                var angleY = _currentRotation.y * 90;
                var angleX = _currentRotation.x * 90;
                

                var rotationRads = Mathf.Atan2(_currentRotation.x, _currentRotation.y);
                
                cameraTarget.RotateAround(_transform.position, _transform.up, rotationRads * Mathf.Rad2Deg);
            }

            var cameraOffsetWorld = _transform.position - _transform.TransformPoint(_cameraOffset);
            cameraTarget.position -= cameraOffsetWorld;
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
            ActiveCamera = newCamera;
            ActiveCamera.Camera.MoveToTopOfPrioritySubqueue();
        }
    }
}