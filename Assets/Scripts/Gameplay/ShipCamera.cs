using System.Collections;
using Unity.Cinemachine;
using Core;
using Misc;
using UnityEngine;

namespace Gameplay {
    public enum CameraType {
        FirstPerson,
        ThirdPerson,
        FreeCam
    }

    [RequireComponent(typeof(CinemachineVirtualCameraBase))]
    [RequireComponent(typeof(AudioListener))]
    public class ShipCamera : MonoBehaviour {
        [SerializeField] private string cameraName;
        [SerializeField] public CameraType cameraType;
        [SerializeField] public Vector3 maxOffset = Vector3.one;
        [SerializeField] public Vector3 maxBoostOffset = Vector3.one;
        [SerializeField] public bool useLowPassAudio;
        [SerializeField] public bool showShipDataUI = true;

        public float smoothSpeed = 0.1f;
        private AudioListener _audioListener;
        private float _baseFov;

        private CinemachineCamera _camera;
        private Vector3 _currentMaxOffset = Vector3.one;
        private CinemachineBasicMultiChannelPerlin _noise;
        private Vector3 _offset = Vector3.zero;
        private Vector3 _targetOffset = Vector3.zero;

        private Coroutine _cameraResetCoroutine;
        private float _cameraRotationDampingOnAwake;

        public string Name => cameraName;

        // Use the starting position of the active camera as the pivot otherwise the cinemachine system
        // will FREAK THE FUCK OUT trying to update the position while basing that formula on the position itself
        public Vector3 BaseLocalPosition { get; private set; }

        public AudioListener AudioListener {
            get {
                if (_audioListener == null) _audioListener = GetComponent<AudioListener>();
                return _audioListener;
            }
        }

        public CinemachineCamera Camera {
            get {
                if (_camera == null) _camera = GetComponent<CinemachineCamera>();
                return _camera;
            }
        }

        public void Awake() {
            BaseLocalPosition = transform.localPosition;
            var rotationComponent = Camera.GetComponent<CinemachineRotateWithFollowTarget>();
            if (rotationComponent != null) _cameraRotationDampingOnAwake = rotationComponent.Damping;
        }

        public void Reset() {
            // Kinda gross but the fastest way to override the cinemachine brain is to just disable damping,
            // snap the camera, wait a frame for the "animation" (of nothing) to happen and re-enable damping.
            IEnumerator ResetPosition() {
                var rotationComponent = Camera.GetComponent<CinemachineRotateWithFollowTarget>();

                if (rotationComponent) {
                    // reset position
                    Camera.PreviousStateIsValid = false;
                    _offset = Vector3.zero;
                    _targetOffset = Vector3.zero;
                    var cameraTransform = Camera.transform;
                    cameraTransform.localPosition = _targetOffset;
                    cameraTransform.localRotation = Quaternion.identity;

                    // handle damping animation
                    rotationComponent.Damping = 0;
                    yield return YieldExtensions.WaitForFixedFrames(10);
                    rotationComponent.Damping = _cameraRotationDampingOnAwake;
                }
            }

            if (gameObject.activeInHierarchy) {
                if (_cameraResetCoroutine != null) StopCoroutine(_cameraResetCoroutine);
                _cameraResetCoroutine = StartCoroutine(ResetPosition());
            }
        }

        public void OnEnable() {
            Game.OnGameSettingsApplied += SetBaseFov;
            _noise = Camera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            SetBaseFov();
            _currentMaxOffset = maxOffset;
        }

        public void OnDisable() {
            Game.OnGameSettingsApplied -= SetBaseFov;
        }

        public void SetCameraActive(bool active) {
            if (active) {
                Camera.Prioritize();
                _offset = Vector3.zero;
                _targetOffset = Vector3.zero;
            }

            AudioListener.enabled = active;
            foreach (var audioLowPassFilter in FindObjectsOfType<AudioLowPassFilter>()) audioLowPassFilter.enabled = useLowPassAudio && active;
        }

        public void UpdateVelocityFov(Vector3 velocity, float maxVelocity, bool lerp = true) {
            var velocityNormalised = velocity.z / maxVelocity;
            var fov = Mathf.Lerp(Camera.Lens.FieldOfView,
                velocityNormalised.Remap(0, 1, _baseFov, _baseFov + 10),
                lerp ? smoothSpeed : 1
            );
            Camera.Lens.FieldOfView = fov;
        }

        public Vector3 GetCameraOffset(Vector3 force, float maxForce, bool lerp = true) {
            var forceNormalised = force / maxForce;
            _targetOffset = Vector3.Lerp(_targetOffset, new Vector3(
                forceNormalised.x.Remap(-1, 1, _currentMaxOffset.x, -_currentMaxOffset.x),
                forceNormalised.y.Remap(-1, 1, _currentMaxOffset.y, -_currentMaxOffset.y),
                forceNormalised.z.Remap(-1, 1, _currentMaxOffset.z, -_currentMaxOffset.z)
            ), lerp ? 0.1f : 1);

            _offset = Vector3.Lerp(_offset, _targetOffset, 0.04f);
            return _offset;
        }

        public void SetShakeEffect(float amount) {
            if (_noise) _noise.FrequencyGain = amount;

            _currentMaxOffset.x = amount.Remap(0, 1, maxOffset.x, maxBoostOffset.x);
            _currentMaxOffset.y = amount.Remap(0, 1, maxOffset.y, maxBoostOffset.y);
            _currentMaxOffset.z = amount.Remap(0, 1, maxOffset.z, maxBoostOffset.z);
        }

        private void SetBaseFov() {
            _baseFov = cameraType == CameraType.FirstPerson
                ? Preferences.Instance.GetFloat("graphics-field-of-view")
                : Preferences.Instance.GetFloat("graphics-field-of-view-ext");
            Camera.Lens.FieldOfView = _baseFov;
        }
    }
}