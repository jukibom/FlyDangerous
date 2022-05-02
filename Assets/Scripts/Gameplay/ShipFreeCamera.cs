using Cinemachine;
using UI;
using UnityEngine;

namespace Gameplay {
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class ShipFreeCamera : MonoBehaviour {
        private float _ascendMotion;
        private CinemachineHardLookAt _hardLookAt;

        private Vector2 _motion;

        private float _motionMultiplier = 25;
        private Vector2 _rotation;
        private CinemachineTransposer _transposer;
        private CinemachineVirtualCamera _virtualCamera;
        private float _zoom;
        public ShipCamera ShipCamera { get; private set; }

        private void Update() {
            _transposer.m_FollowOffset += _transposer.LookAtTarget.transform.InverseTransformDirection(transform.TransformDirection(new Vector3(
                _motion.x * Time.unscaledDeltaTime * _motionMultiplier,
                _ascendMotion * Time.unscaledDeltaTime * _motionMultiplier,
                _motion.y * Time.unscaledDeltaTime * _motionMultiplier
            )));

            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + new Vector3(
                _rotation.y * Time.unscaledDeltaTime * _motionMultiplier * -1,
                _rotation.x * Time.unscaledDeltaTime * _motionMultiplier,
                0
            ));

            _virtualCamera.m_Lens.FieldOfView += _zoom * Time.unscaledDeltaTime;
        }

        private void OnEnable() {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            _transposer = _virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            _hardLookAt = _virtualCamera.GetCinemachineComponent<CinemachineHardLookAt>();
            ShipCamera = GetComponent<ShipCamera>();
            InitPosition(new Vector3(10, 0, 0));
        }

        public void InitPosition(Vector3 position) {
            _transposer.m_FollowOffset = position;
            _hardLookAt.enabled = false;
        }

        public void Move(Vector2 motion) {
            _motion = motion;
        }

        public void LookAround(Vector2 rotation) {
            _rotation = rotation * 10;
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