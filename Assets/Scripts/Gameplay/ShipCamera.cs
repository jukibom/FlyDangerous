using System.Collections;
using Cinemachine;
using Core;
using Misc;
using UnityEngine;

namespace Gameplay {
    public enum CameraType {
        FirstPerson,
        ThirdPerson,
        FreeCam
    }

    [RequireComponent(typeof(CinemachineVirtualCamera))]
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

        private CinemachineVirtualCamera _camera;
        private Vector3 _currentMaxOffset = Vector3.one;
        private CinemachineBasicMultiChannelPerlin _noise;
        private Vector3 _offset = Vector3.zero;
        private Vector3 _targetOffset = Vector3.zero;

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

        public CinemachineVirtualCamera Camera {
            get {
                if (_camera == null) _camera = GetComponent<CinemachineVirtualCamera>();
                return _camera;
            }
        }

        public void Awake() {
            BaseLocalPosition = transform.localPosition;
        }

        public void Reset() {
            // Kinda gross but the fastest way to override the cinemachine brain is to just disable damping,
            // snap the camera, wait a frame for the "animation" (of nothing) to happen and re-enable damping.
            IEnumerator ResetPosition() {
                var rotationComponent = Camera.GetCinemachineComponent<CinemachineSameAsFollowTarget>();
                var previousDamping = rotationComponent.m_Damping;

                if (rotationComponent) {
                    // reset position
                    Camera.PreviousStateIsValid = false;
                    _offset = Vector3.zero;
                    _targetOffset = Vector3.zero;
                    var cameraTransform = Camera.transform;
                    cameraTransform.localPosition = _targetOffset;
                    cameraTransform.localRotation = Quaternion.identity;

                    // handle damping animation
                    rotationComponent.m_Damping = 0;
                    yield return new WaitForEndOfFrame();
                    rotationComponent.m_Damping = previousDamping;
                }
            }

            if (gameObject.activeInHierarchy) StartCoroutine(ResetPosition());
        }

        public void OnEnable() {
            Game.OnGameSettingsApplied += SetBaseFov;
            _noise = Camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            SetBaseFov();
            _currentMaxOffset = maxOffset;
        }

        public void OnDisable() {
            Game.OnGameSettingsApplied -= SetBaseFov;
        }

        public void SetCameraActive(bool active) {
            if (active) Camera.MoveToTopOfPrioritySubqueue();
            AudioListener.enabled = active;
            foreach (var audioLowPassFilter in FindObjectsOfType<AudioLowPassFilter>()) audioLowPassFilter.enabled = useLowPassAudio && active;
        }

        public void UpdateVelocityFov(Vector3 velocity, float maxVelocity) {
            var velocityNormalised = velocity.z / maxVelocity;
            var fov = Mathf.Lerp(Camera.m_Lens.FieldOfView,
                velocityNormalised.Remap(0, 1, _baseFov, _baseFov + 10),
                smoothSpeed
            );
            Camera.m_Lens.FieldOfView = fov;
            Game.Instance.InGameUICamera.fieldOfView = fov;
        }

        public Vector3 GetCameraOffset(Vector3 force, float maxForce) {
            var forceNormalised = force / maxForce;
            _targetOffset = Vector3.Lerp(_targetOffset, new Vector3(
                forceNormalised.x.Remap(-1, 1, _currentMaxOffset.x, -_currentMaxOffset.x),
                forceNormalised.y.Remap(-1, 1, _currentMaxOffset.y, -_currentMaxOffset.y),
                forceNormalised.z.Remap(-1, 1, _currentMaxOffset.z, -_currentMaxOffset.z)
            ), 0.1f);

            _offset = Vector3.Lerp(_offset, _targetOffset, 0.04f);
            return _offset;
        }

        public void SetShakeEffect(float amount) {
            if (_noise) _noise.m_FrequencyGain = amount;

            _currentMaxOffset.x = amount.Remap(0, 1, maxOffset.x, maxBoostOffset.x);
            _currentMaxOffset.y = amount.Remap(0, 1, maxOffset.y, maxBoostOffset.y);
            _currentMaxOffset.z = amount.Remap(0, 1, maxOffset.z, maxBoostOffset.z);
        }

        private void SetBaseFov() {
            _baseFov = cameraType == CameraType.FirstPerson
                ? Preferences.Instance.GetFloat("graphics-field-of-view")
                : Preferences.Instance.GetFloat("graphics-field-of-view-ext");
            Camera.m_Lens.FieldOfView = _baseFov;
        }
    }
}