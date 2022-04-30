using System;
using Cinemachine;
using UI;
using UnityEngine;

namespace Gameplay {
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class ShipFreeCamera : MonoBehaviour {
        private CinemachineVirtualCamera _virtualCamera;
        private CinemachineTransposer _transposer;
        private CinemachineHardLookAt _hardLookAt;
        public ShipCamera ShipCamera { get; private set; }

        private Vector2 _motion;
        private Vector2 _rotation;
        private float _ascendMotion;
        private float _zoom;

        private float _motionMultiplier = 25;

        private void OnEnable() {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            _transposer = _virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            _hardLookAt = _virtualCamera.GetCinemachineComponent<CinemachineHardLookAt>();
            ShipCamera = GetComponent<ShipCamera>();
            InitPosition(new Vector3(10, 0, 0));
        }

        private void Update() {
            
            _transposer.m_FollowOffset += _transposer.LookAtTarget.transform.InverseTransformDirection(transform.TransformDirection( new Vector3(
                _motion.x * Time.unscaledDeltaTime,
                _ascendMotion * Time.unscaledDeltaTime,
                _motion.y * Time.unscaledDeltaTime
            )));
            
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + new Vector3(
                _rotation.y * Time.unscaledDeltaTime * -1,
                _rotation.x * Time.unscaledDeltaTime,
                0
            ));

            _virtualCamera.m_Lens.FieldOfView += _zoom * Time.unscaledDeltaTime;
        }

        public void InitPosition(Vector3 position) {
            _transposer.m_FollowOffset = position;
            _hardLookAt.enabled = false;
        }
        
        public void Move(Vector2 motion) {
            _motion = motion * _motionMultiplier;
        }
        
        public void LookAround(Vector2 rotation) {
            _rotation = rotation * 250;
        }

        public void Ascend(float ascension) {
            _ascendMotion = ascension * _motionMultiplier;
        }
        
        public void ToggleAimLock() {
            _hardLookAt.enabled = !_hardLookAt.enabled;
        }

        public void Zoom(float zoomAmount) {
            _zoom = zoomAmount * -30;
        }

        public void IncrementMotionMultiplier(float amount) {
            // increase by 5 times the amount if over 5 already so you don't have to press it a million times
            _motionMultiplier += amount * (_motionMultiplier >= 5 ? 5 : 1);
            _motionMultiplier = Mathf.Clamp(_motionMultiplier, 1, 500);
            FdConsole.Instance.LogMessage("Set Free Cam Motion Multiplier " + _motionMultiplier);
        }
    }
}