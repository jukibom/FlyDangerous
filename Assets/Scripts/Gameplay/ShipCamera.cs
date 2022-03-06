using Cinemachine;
using Core;
using Core.Player;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Gameplay {
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class ShipCamera : MonoBehaviour {

        public float smoothSpeed = 0.5f;
        public float accelerationDampener = 5f;

        private Vector3 _velocity = Vector3.zero;
        private Vector3 _lastVelocity;

        public Vector3 minPos = new Vector3(-0.1175f, -0.0678f, -0.2856f);
        public Vector3 maxPos = new Vector3(0.1175f, 0.04f, 0.0412f);

        private float _baseFov;
        private Transform _transform;
        private CinemachineVirtualCamera _camera;
    
    
        private Rigidbody _shipTarget;
        [CanBeNull] private Rigidbody Target {
            get {
                if (_shipTarget == null) {
                    var player = FdPlayer.FindLocalShipPlayer;
                    if (player != null) {
                        _shipTarget = player.GetComponent<Rigidbody>();
                    }
                }

                return _shipTarget;
            }
        }

        public void OnEnable() {
            _transform = transform;
            Game.OnGameSettingsApplied += SetBaseFov;
            // TODO: replace with vcams 
            _camera = GetComponentInChildren<CinemachineVirtualCamera>();
            SetBaseFov();
        }
    
        public void OnDisable() {
            Game.OnGameSettingsApplied -= SetBaseFov;
        }

        public void Reset() {
            _lastVelocity = Vector3.zero;
            _lastVelocity = Vector3.zero;
            transform.localPosition = Vector3.zero;
        }
    
        void FixedUpdate() {
            if (Target != null) {
                var acceleration = _transform.InverseTransformDirection(Target.velocity - _lastVelocity) /
                                   Time.fixedDeltaTime;
                var accelerationCameraDelta = -acceleration / accelerationDampener / 100f;
                
                var cameraPosition =
                    Vector3.SmoothDamp(_transform.localPosition, accelerationCameraDelta, ref _velocity, smoothSpeed);
                
                cameraPosition = Vector3.Min(Vector3.Max(cameraPosition, minPos), maxPos);
                transform.localPosition = cameraPosition;

                _lastVelocity = Target.velocity;

                // Fov
                _camera.m_Lens.FieldOfView = Mathf.Lerp(_camera.m_Lens.FieldOfView,
                    MathfExtensions.Remap(0, minPos.z, _baseFov, _baseFov + 20, cameraPosition.z), 
                    0.1f
                );
            }
        }

        private void SetBaseFov() {
            _baseFov = Preferences.Instance.GetFloat("graphics-field-of-view");
        }
    }
}
