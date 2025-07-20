using Unity.Cinemachine;
using FdUI;
using UnityEngine;

namespace Gameplay {
    [RequireComponent(typeof(CinemachineCamera))]
    public class ShipFreeCamera : MonoBehaviour {
        private float _ascendMotion;
        private CinemachineHardLookAt _hardLookAt;

        private Vector2 _motion;

        private float _motionMultiplier = 25;
        private Vector2 _rotation;
        private CinemachineFollow _transposer;
        private CinemachineCamera _virtualCamera;
        private float _zoom;
        public ShipCamera ShipCamera { get; private set; }

        private void Update() {
            _transposer.FollowOffset += _transposer.LookAtTarget.transform.InverseTransformDirection(transform.TransformDirection(new Vector3(
                _motion.x * Time.unscaledDeltaTime * _motionMultiplier,
                _ascendMotion * Time.unscaledDeltaTime * _motionMultiplier,
                _motion.y * Time.unscaledDeltaTime * _motionMultiplier
            )));

            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + new Vector3(
                _rotation.y * Time.unscaledDeltaTime * -1,
                _rotation.x * Time.unscaledDeltaTime,
                0
            ));

            _virtualCamera.Lens.FieldOfView += _zoom * Time.unscaledDeltaTime;
        }

        private void OnEnable() {
            _virtualCamera = GetComponent<CinemachineCamera>();
            _transposer = _virtualCamera.GetComponent<CinemachineFollow>();
            _hardLookAt = _virtualCamera.GetComponent<CinemachineHardLookAt>();
            ShipCamera = GetComponent<ShipCamera>();
            InitPosition(new Vector3(10, 0, 0));
        }

        public void InitPosition(Vector3 position) {
            _transposer.FollowOffset = position;
            if (_hardLookAt != null) _hardLookAt.enabled = false;
        }

        public void Move(Vector2 motion) {
            _motion = motion;
        }

        public void LookAround(Vector2 rotation) {
            _rotation = rotation * 100;
        }

        public void Ascend(float ascension) {
            _ascendMotion = ascension;
        }

        public void ToggleAimLock() {
            _hardLookAt.enabled = !_hardLookAt.enabled;
        }

        public void Zoom(float zoomAmount) {
            _zoom = zoomAmount * -30;
        }

        public void IncrementMotionMultiplier(float amount) {
            // increase by 5 times the amount if over 5 already so you don't have to press it a million times
            if (_motionMultiplier > 5) {
                _motionMultiplier += amount * 5;
            }
            else if (_motionMultiplier > 1) {
                _motionMultiplier += amount;
                _motionMultiplier = Mathf.Round(_motionMultiplier);
            }
            else if (_motionMultiplier <= 1) {
                _motionMultiplier += amount / 5;
            }

            _motionMultiplier = Mathf.Clamp(_motionMultiplier, 0.2f, 500);
            FdConsole.Instance.LogMessage("Set Free Cam Motion Multiplier " + _motionMultiplier);
        }
    }
}