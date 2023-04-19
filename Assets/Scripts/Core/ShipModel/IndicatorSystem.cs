using Core.Player;
using Core.ShipModel.Feedback.interfaces;
using GameUI.Components.Terrain_Indicator;
using Misc;
using UnityEngine;
using UnityEngine.UI;
using CameraType = Gameplay.CameraType;

namespace Core.ShipModel {
    public class IndicatorSystem : MonoBehaviour, IShipInstruments, IShipMotion {
        [SerializeField] private GameObject forwardCrosshairIndicator;
        [SerializeField] private CanvasGroup tviIndicators;
        [SerializeField] private Image tviForward;
        [SerializeField] private Image tviReverse;
        [SerializeField] private float tviAlphaSmoothing = 0.5f;
        [SerializeField] private float tviPositionalSmoothing = 0.5f;
        [SerializeField] private int indicatorDistance = 500;
        [SerializeField] private CanvasGroup terrainIndicator;
        [SerializeField] private OrientationIndicator orientationIndicator;
        [SerializeField] private HeightDeltaIndicator heightDeltaIndicator;
        [SerializeField] private HeightMarker heightIndicator;
        [SerializeField] private HeightMarker altitudeIndicator;
        [SerializeField] private Text xCoord;
        [SerializeField] private Text yCoord;
        [SerializeField] private Text zCoord;
        [SerializeField] private Text gravity;

        private bool _userShouldShow = true;
        private bool _cameraShouldShowOrientationIndicators = true;
        private Camera _mainCamera;
        private readonly Vector3 _stationaryDirectionVector = new(0, 0, 1);
        private Vector3 _lookAtUpVector;
        private Vector3 _targetTVIForwardPosition;
        private Vector3 _targetTVIReversePosition;
        private float _targetTVIAlpha;
        private IShipInstrumentData _shipInstrumentData;
        private IShipMotionData _shipMotionData;

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void OnGameSettingsApplied() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                var activeCamera = player.User.ShipCameraRig.ActiveCamera;
                if (activeCamera) OnCameraChanged(activeCamera.cameraType);
            }

            UpdateVisibility();
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            _shipInstrumentData = shipInstrumentData;
            UpdateHeightIndicators();
        }

        public void OnShipMotionUpdate(IShipMotionData shipMotionData) {
            _shipMotionData = shipMotionData;
            UpdateHeightDeltaIndicator();
        }

        public void OnCameraChanged(CameraType cameraType) {
            _cameraShouldShowOrientationIndicators = cameraType == CameraType.FirstPerson ||
                                                     (cameraType == CameraType.ThirdPerson &&
                                                      Preferences.Instance.GetBool("showFlightOrientationIndicatorsInThirdPerson"));
            UpdateVisibility();
        }

        public void ToggleVisibility(bool shouldShow) {
            _userShouldShow = shouldShow;
            UpdateVisibility();
        }

        private void FixedUpdate() {
            // update camera if needed
            if (_mainCamera == null || _mainCamera.enabled == false || _mainCamera.gameObject.activeSelf == false)
                _mainCamera = Camera.main;

            UpdateTVIs();
        }

        private void Update() {
            if (_mainCamera == null) return;

            var tviForwardTransform = tviForward.transform;
            var tviReverseTransform = tviReverse.transform;
            var tviForwardLocalPosition = tviForwardTransform.localPosition;
            var tviReverseLocalPosition = tviReverseTransform.localPosition;

            // interpolate values
            tviIndicators.alpha = Mathf.Lerp(tviIndicators.alpha, _targetTVIAlpha, tviAlphaSmoothing);
            tviForwardLocalPosition = Vector3.Lerp(tviForwardLocalPosition, _targetTVIForwardPosition, tviPositionalSmoothing);
            tviReverseLocalPosition = Vector3.Lerp(tviReverseLocalPosition, _targetTVIReversePosition, tviPositionalSmoothing);

            // make sure the indicators are always at the required distance away on the sphere (may interpolate through the camera otherwise!)
            tviForwardTransform.localPosition = tviForwardLocalPosition.normalized * indicatorDistance;
            tviReverseTransform.localPosition = tviReverseLocalPosition.normalized * indicatorDistance;

            var mainCameraTransform = _mainCamera.transform;
            tviForwardTransform.LookAt(mainCameraTransform, _lookAtUpVector);
            tviReverseTransform.LookAt(mainCameraTransform, _lookAtUpVector);

            var player = FdPlayer.FindLocalShipPlayer;
            if (player && _mainCamera) {
                // update pitch indicator to face the player but always orient with the world
                var shipTransform = player.transform;
                orientationIndicator.PitchValueNormalized = (Vector3.Angle(Vector3.up, shipTransform.forward) - 90) * -1 / 90;
                orientationIndicator.PitchIndicator.LookAt(player.User.transform, Vector3.up);
                orientationIndicator.PitchIndicator.transform.Rotate(Vector3.up, 180);
            }
        }

        private void UpdateTVIs() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player && _mainCamera) {
                var shipTransform = player.transform;

                var velocity = player.ShipPhysics.Velocity;
                var shipDirectionVector = shipTransform.InverseTransformDirection(velocity.normalized);

                // dirty cludge for flight assist 
                var positionVector = shipDirectionVector;
                if (player.ShipPhysics.VectorFlightAssistActive)
                    positionVector = Vector3.Lerp(_stationaryDirectionVector, shipDirectionVector, velocity.magnitude.Remap(0, 100, 0, 1));

                _targetTVIForwardPosition = positionVector * indicatorDistance;
                _targetTVIReversePosition = positionVector * (indicatorDistance * -1);

                // Fade out the indicators when either slowing down or interpolating the position inverse (i.e. when the indicator is getting closer which it really shouldn't)
                _targetTVIAlpha = velocity.magnitude.Remap(2, 10, 0, 1);

                _lookAtUpVector = Game.IsVREnabled ? player.transform.up : _mainCamera.transform.up;
            }
        }

        private void UpdateHeightIndicators() {
            altitudeIndicator.HeightAboveSurface = _shipInstrumentData.Altitude;
            heightIndicator.HeightAboveSurface = _shipInstrumentData.ShipHeightFromGround;
            xCoord.text = Mathf.Round(_shipInstrumentData.WorldPosition.x) + "x";
            yCoord.text = Mathf.Round(_shipInstrumentData.WorldPosition.y) + "y";
            zCoord.text = Mathf.Round(_shipInstrumentData.WorldPosition.z) + "z";
            gravity.text = Mathf.Round(_shipInstrumentData.Gravity / 9.8f * 100) / 100 + "G";
        }

        private void UpdateHeightDeltaIndicator() {
            heightDeltaIndicator.IndicatorValueNormalized = _shipMotionData.CurrentLateralVelocity.y / (_shipMotionData.MaxSpeed * 0.5f);
        }

        private void UpdateVisibility() {
            forwardCrosshairIndicator.SetActive(_userShouldShow && Preferences.Instance.GetBool("showForwardVectorIndicator"));
            terrainIndicator.gameObject.SetActive(_userShouldShow &&
                                                  _cameraShouldShowOrientationIndicators &&
                                                  Preferences.Instance.GetBool("showFlightOrientationIndicators") &&
                                                  (Preferences.Instance.GetBool("showFlightOrientationIndicatorsInSpace") ||
                                                   Game.Instance.LoadedLevelData.location.IsTerrain));
            tviIndicators.gameObject.SetActive(_userShouldShow && Preferences.Instance.GetBool("showTrueVectorIndicator"));

            var terrainIndicatorScale = Preferences.Instance.GetFloat("flightOrientationIndicatorScale").Remap(0.5f, 3f, 20, 70);
            terrainIndicator.transform.localScale = new Vector3(terrainIndicatorScale, terrainIndicatorScale, 1);
        }
    }
}