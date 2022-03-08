using System.Collections.Generic;
using System.Linq;
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

        public void SetCameraAbsolute(Vector2 absolutePosition) {
            // reset rotation before processing input
            cameraTarget.localPosition = baseTargetPosition;
            cameraTarget.transform.rotation = _transform.rotation;

            UpdateAbsolute(absolutePosition);
            
            // handle offset based on force
            var cameraOffsetWorld = _transform.position - _transform.TransformPoint(_cameraOffset);
            cameraTarget.position -= cameraOffsetWorld;
        }
        
        public void SetCameraRelative(Vector2 relativePosition) {
            // reset rotation before processing input
            cameraTarget.localPosition = baseTargetPosition;
            cameraTarget.transform.rotation = _transform.rotation;

            UpdateRelative(relativePosition);
            
            // handle offset based on force
            var cameraOffsetWorld = _transform.position - _transform.TransformPoint(_cameraOffset);
            cameraTarget.position -= cameraOffsetWorld;
        }

        private void UpdateAbsolute(Vector2 absolutePosition) {
            if (ActiveCamera.cameraType == CameraType.FirstPerson) {
                
                _currentRotation = new Vector2(
                    Mathf.Lerp(_currentRotation.x, absolutePosition.x, 0.01f),
                    Mathf.Lerp(_currentRotation.y, absolutePosition.y, 0.01f)
                );
                var angleY = _currentRotation.y * 90;
                var angleX = _currentRotation.x * 90;
                
                var pivot = _transform.TransformPoint(ActiveCamera.BaseLocalPosition);

                cameraTarget.RotateAround(pivot, _transform.right, -angleY);
                cameraTarget.RotateAround(pivot, _transform.up, angleX);
            }
            
            if (ActiveCamera.cameraType == CameraType.ThirdPerson) {
                // input is used to rotate the view around the ship
                // bias towards looking forward
                if (Mathf.Abs(absolutePosition.x) < 0.2f && Mathf.Abs(absolutePosition.y) < 0.2f) {
                    absolutePosition = new Vector2(0, 1);
                }

                _currentRotation = new Vector2(
                    Mathf.Lerp(_currentRotation.x, absolutePosition.x, 0.02f),
                    Mathf.Lerp(_currentRotation.y, absolutePosition.y, 0.02f)
                );
                
                var rotationRads = Mathf.Atan2(_currentRotation.x, _currentRotation.y);
                cameraTarget.RotateAround(_transform.position, _transform.up, rotationRads * Mathf.Rad2Deg);
            }
        }

        private void UpdateRelative(Vector2 relativePosition) {
            var pivot = _transform.TransformPoint(ActiveCamera.BaseLocalPosition);
            if (ActiveCamera.cameraType == CameraType.ThirdPerson) {
                pivot = _transform.position;
            }

            _currentRotation = new Vector2(
                _currentRotation.x + relativePosition.x * 2 * Time.deltaTime,
                _currentRotation.y + relativePosition.y * 2 * Time.deltaTime
            );
            var angleY = _currentRotation.y * 90;
            var angleX = _currentRotation.x * 90;

            cameraTarget.RotateAround(pivot, _transform.right, -angleY);
            cameraTarget.RotateAround(pivot, _transform.up, angleX);
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