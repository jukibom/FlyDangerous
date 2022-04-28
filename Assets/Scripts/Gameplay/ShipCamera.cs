using System.Collections;
using Cinemachine;
using Core;
using Misc;
using UnityEngine;

namespace Gameplay {
    public enum CameraType {
        FirstPerson,
        ThirdPerson
    }

    [RequireComponent(typeof(CinemachineVirtualCamera))]
    [RequireComponent(typeof(AudioListener))]
    public class ShipCamera : MonoBehaviour {
        [SerializeField] private string cameraName;
        [SerializeField] public CameraType cameraType;
        [SerializeField] public Vector3 maxOffset = Vector3.one;
        [SerializeField] public bool useLowPassAudio;
        [SerializeField] public bool showShipDataUI = true;

        public float smoothSpeed = 0.1f;
        private AudioListener _audioListener;
        private float _baseFov;

        private CinemachineVirtualCamera _camera;
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
                // yield return new WaitForEndOfFrame();
                var rotationComponent = Camera.GetCinemachineComponent<CinemachineSameAsFollowTarget>();
                if (rotationComponent) {
                    var damping = rotationComponent.m_Damping;

                    Camera.PreviousStateIsValid = false;
                    _offset = Vector3.zero;
                    _targetOffset = Vector3.zero;
                    var cameraTransform = Camera.transform;
                    cameraTransform.localPosition = _targetOffset;
                    cameraTransform.localRotation = Quaternion.identity;

                    rotationComponent.m_Damping = 0;
                    yield return new WaitForEndOfFrame();
                    rotationComponent.m_Damping = damping;
                }
            }

            StartCoroutine(ResetPosition());
        }

        public void OnEnable() {
            Game.OnGameSettingsApplied += SetBaseFov;
            SetBaseFov();
        }

        public void OnDisable() {
            Game.OnGameSettingsApplied -= SetBaseFov;
        }

        public void SetCameraActive(bool active) {
            if (active) Camera.MoveToTopOfPrioritySubqueue();
            AudioListener.enabled = active;
            foreach (var audioLowPassFilter in FindObjectsOfType<AudioLowPassFilter>()) audioLowPassFilter.enabled = useLowPassAudio && active;
        }

        public void UpdateFov(Vector3 velocity, float maxVelocity) {
            var fov = Mathf.Lerp(Camera.m_Lens.FieldOfView,
                MathfExtensions.Remap(0, 1, _baseFov, _baseFov + 10, velocity.z / maxVelocity),
                smoothSpeed
            );
            Camera.m_Lens.FieldOfView = fov;
            Game.Instance.InGameUICamera.fieldOfView = fov;
        }

        public Vector3 GetCameraOffset(Vector3 force, float maxForce) {
            _targetOffset = Vector3.Lerp(_targetOffset, new Vector3(
                MathfExtensions.Remap(-1, 1, maxOffset.x, -maxOffset.x, force.x / maxForce),
                MathfExtensions.Remap(-1, 1, maxOffset.y, -maxOffset.y, force.y / maxForce),
                MathfExtensions.Remap(-1, 1, maxOffset.z, -maxOffset.z, force.z / maxForce)
            ), 0.1f);

            _offset = Vector3.Lerp(_offset, _targetOffset, 0.04f);
            return _offset;
        }

        private void SetBaseFov() {
            _baseFov = cameraType == CameraType.FirstPerson
                ? Preferences.Instance.GetFloat("graphics-field-of-view")
                : Preferences.Instance.GetFloat("graphics-field-of-view-ext");
        }
    }
}