using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Gameplay;
using JetBrains.Annotations;
using UnityEngine;
using CameraType = Gameplay.CameraType;

namespace Core.Player {
    public enum CameraPositionUpdate {
        Relative,
        Absolute
    }

    public class ShipCameraRig : MonoBehaviour {
        private static readonly Vector3 baseTargetPosition = new(0, 0, 20);

        [SerializeField] private User user;
        [SerializeField] private List<ShipCamera> cameras;
        [SerializeField] private ShipCamera endScreenCamera1;
        [SerializeField] private ShipCamera endScreenCamera2;
        [SerializeField] private ShipFreeCamera freeCamera;
        [SerializeField] private Transform cameraTarget;
        private Vector3 _cameraOffset;
        private Vector2 _currentRotation;
        private Coroutine _endScreenCameraTransition;
        private Transform _transform;

        [CanBeNull] public ShipCamera ActiveCamera { get; private set; }
        public ShipFreeCamera ShipFreeCamera => freeCamera;

        public void Reset() {
            RecoverPreferredCameraFromPreferences();
            if (_endScreenCameraTransition != null) StopCoroutine(_endScreenCameraTransition);
            endScreenCamera1.SetCameraActive(false);
            endScreenCamera2.SetCameraActive(false);
            SoftReset();
            if (ActiveCamera != null)
                ActiveCamera.Reset();
            cameraTarget.localPosition = baseTargetPosition;
            cameraTarget.transform.rotation = _transform.rotation;
            SetBoostEffect(0);
        }

        private void Start() {
            _transform = transform;
            RecoverPreferredCameraFromPreferences();
        }

        private void OnEnable() {
            // Set active camera from preference (this also happens on VR disable as the component is re-enabled)
            // this may take a frame to do as other cameras may have come online at the same time
            IEnumerator ResetCamera() {
                yield return new WaitForEndOfFrame();
                RecoverPreferredCameraFromPreferences();
            }

            StartCoroutine(ResetCamera());
        }

        public void SetPosition(Vector2 position, CameraPositionUpdate cameraType) {
            if (ActiveCamera != null && ActiveCamera.cameraType != CameraType.FreeCam) {
                // reset rotation before processing input
                cameraTarget.localPosition = baseTargetPosition;
                cameraTarget.transform.rotation = _transform.rotation;

                switch (cameraType) {
                    case CameraPositionUpdate.Absolute:
                        UpdateAbsolute(position);
                        break;
                    case CameraPositionUpdate.Relative:
                        UpdateRelative(position);
                        break;
                }

                // handle offset based on force
                var cameraOffsetWorld = _transform.position - _transform.TransformPoint(_cameraOffset);
                cameraTarget.position -= cameraOffsetWorld;
            }
        }

        private void UpdateAbsolute(Vector2 absolutePosition) {
            if (ActiveCamera != null) {
                if (ActiveCamera.cameraType == CameraType.FirstPerson) {
                    _currentRotation = new Vector2(
                        Mathf.Lerp(_currentRotation.x, absolutePosition.x, 0.02f),
                        Mathf.Lerp(_currentRotation.y, absolutePosition.y, 0.02f)
                    );
                    var angleY = _currentRotation.y * 90;
                    var angleX = _currentRotation.x * 90;

                    var pivot = _transform.TransformPoint(ActiveCamera.BaseLocalPosition);

                    cameraTarget.RotateAround(pivot, _transform.right, -angleY);
                    cameraTarget.RotateAround(pivot, _transform.up, angleX);
                }

                if (ActiveCamera.cameraType == CameraType.ThirdPerson) {
                    // input is used to rotate the view around the ship
                    // bias towards looking forward (only activate over a sensible deadzone)
                    if (Mathf.Abs(absolutePosition.x) > 0.2f || Mathf.Abs(absolutePosition.y) > 0.2f) {
                        _currentRotation = new Vector2(
                            Mathf.Lerp(_currentRotation.x, absolutePosition.x, 0.3f),
                            Mathf.Lerp(_currentRotation.y, absolutePosition.y, 0.3f)
                        );

                        var rotationRads = Mathf.Atan2(_currentRotation.x, _currentRotation.y);
                        cameraTarget.RotateAround(_transform.position, _transform.up, rotationRads * Mathf.Rad2Deg);
                    }
                    else {
                        _currentRotation = Vector2.zero;
                    }
                }
            }
        }

        private void UpdateRelative(Vector2 relativePosition) {
            if (ActiveCamera != null) {
                var pivot = _transform.TransformPoint(ActiveCamera.BaseLocalPosition);
                if (ActiveCamera.cameraType == CameraType.ThirdPerson) pivot = _transform.position;

                _currentRotation = new Vector2(
                    _currentRotation.x + relativePosition.x * 2 * Time.deltaTime,
                    _currentRotation.y + relativePosition.y * 2 * Time.deltaTime
                );
                var angleY = _currentRotation.y * 90;
                var angleX = _currentRotation.x * 90;

                cameraTarget.RotateAround(pivot, _transform.right, -angleY);
                cameraTarget.RotateAround(pivot, _transform.up, angleX);
            }
        }

        public void SoftReset() {
            _currentRotation = Vector2.zero;
        }

        public void UpdateCameras(Vector3 velocity, float maxVelocity, Vector3 force, float maxForce) {
            if (ActiveCamera != null && ActiveCamera.cameraType != CameraType.FreeCam) {
                ActiveCamera.UpdateVelocityFov(velocity, maxVelocity);
                _cameraOffset = ActiveCamera.GetCameraOffset(force, maxForce);
            }
        }

        public void ToggleActiveCamera() {
            if (gameObject.activeSelf)
                if (ActiveCamera != null) {
                    // on toggle camera from free cam state, revert to last known
                    if (ActiveCamera.cameraType == CameraType.FreeCam) {
                        RecoverPreferredCameraFromPreferences();
                        return;
                    }

                    // otherwise cycle through and save the new preferred camera;
                    var index = cameras.IndexOf(ActiveCamera);
                    SetActiveCamera(index == cameras.Count - 1 ? cameras[0] : cameras[index + 1]);
                    Preferences.Instance.SetString("preferredCamera", ActiveCamera!.Name);
                }
        }

        public void SetFreeCameraEnabled(bool freeCamEnabled) {
            if (ActiveCamera != null)
                if (freeCamEnabled) // freeCamera.InitPosition(transform.localPosition + new Vector3(10, 0, 0));
                    SetActiveCamera(freeCamera.ShipCamera);
        }

        public void SetBoostEffect(float amount) {
            if (ActiveCamera != null) ActiveCamera.SetBoostEffect(amount);
        }

        private void SetActiveCamera(ShipCamera newCamera) {
            if (ActiveCamera != null) ActiveCamera.SetCameraActive(false);

            SoftReset();
            ActiveCamera = newCamera;
            user.InGameUI.ShipStats.SetStatsVisible(!Game.Instance.IsVREnabled && newCamera.showShipDataUI);
            newCamera.SetCameraActive(true);
        }

        private void RecoverPreferredCameraFromPreferences() {
            if (cameras.Count > 0) {
                var preferredCameraName = Preferences.Instance.GetString("preferredCamera");
                var preferredCamera = cameras.Find(c => c.Name == preferredCameraName);
                SetActiveCamera(preferredCamera != null ? preferredCamera : cameras.Last());
            }
        }

        public void SwitchToEndScreenCamera() {
            SetActiveCamera(endScreenCamera1);

            IEnumerator TransitionToSecondCamera() {
                yield return new WaitForFixedUpdate();
                var cinemachine = FindObjectOfType<CinemachineBrain>();
                if (cinemachine) {
                    var switchNextTime = cinemachine.ActiveBlend?.Duration ?? 2;
                    yield return new WaitForSeconds(switchNextTime);
                    SetActiveCamera(endScreenCamera2);
                }
            }

            _endScreenCameraTransition = StartCoroutine(TransitionToSecondCamera());
        }
    }
}