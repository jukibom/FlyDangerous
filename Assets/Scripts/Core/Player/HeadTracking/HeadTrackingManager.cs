using Misc;
using UnityEngine;

namespace Core.Player.HeadTracking {
    public struct HeadTransform {
        public Vector3 position;
        public Quaternion orientation;

        public override string ToString() {
            var euler = orientation.eulerAngles;
            return $"x: {position.x}, y: {position.y}, z: {position.z}, pitch: {euler.x}, yaw: {euler.y}, roll: {euler.z}";
        }
    }

    public class HeadTrackingManager : MonoBehaviour {
        [SerializeField] private TrackIRComponent trackIr;

        // prefs
        private float _autoTrackDamping;
        private float _autoTrackDeadzone;
        private Vector3 _autoTrackMax;
        private Vector3 _autoTrackMin;
        private bool _autoTrackSnap;
        private float _autoTrackAmount;

        // containers
        private Vector3 _shipVelocity = Vector3.zero;
        private HeadTransform _headTransform;
        private OpenTrackData _openTrackData;
        private Vector3 _openTrackHeadPosition = Vector3.zero;
        private Quaternion _openTrackHeadOrientation = Quaternion.identity;
        private Vector3 _trackIrHeadPosition = Vector3.zero;
        private Quaternion _trackIrHeadOrientation = Quaternion.identity;
        private Quaternion _autoTrackHeadOrientation;

        public bool IsOpenTrackEnabled { get; private set; }
        public bool IsTrackIrEnabled { get; private set; }
        public bool IsAutoTrackEnabled { get; set; }
        public ref HeadTransform HeadTransform => ref _headTransform;

        private void FixedUpdate() {
            _headTransform.position = Vector3.zero;
            _headTransform.orientation = Quaternion.identity;

            // OpenTrack Head Position
            if (IsOpenTrackEnabled) {
                _openTrackData.ReceiveOpenTrackDataAsync(data => {
                    // position is in cm in flipped in X and Z space, convert
                    // max magnitude 1m in any direction
                    _openTrackHeadPosition = Vector3.ClampMagnitude(new Vector3(-(float)data.x / 100, (float)data.y / 100, (float)-data.z / 100), 1);
                    // orientation convert from vec 3 euler
                    _openTrackHeadOrientation = Quaternion.Euler(-(float)data.pitch, (float)data.yaw, -(float)data.roll);
                }, 4242, 1000);
            }
            else {
                _openTrackHeadPosition = Vector3.zero;
                _openTrackHeadOrientation = Quaternion.identity;
            }

            // Track IR
            if (IsTrackIrEnabled) {
                var trackIrTransform = trackIr.transform;
                var trackIrLocalRotation = trackIrTransform.localRotation;

                // for some reason the trackIR sdk outputs flipped z rotation? Not sure if that's an OpenTrack problem so make sure to 
                // check with whoever actually owns one of these things
                var rotationZFlip = trackIrLocalRotation.eulerAngles.z * -2;
                var mirrorQuaternion = Quaternion.Euler(0, 0, rotationZFlip);

                _trackIrHeadPosition = trackIrTransform.localPosition;
                _trackIrHeadOrientation = trackIrLocalRotation * mirrorQuaternion;
            }
            else {
                _trackIrHeadPosition = Vector3.zero;
                _trackIrHeadOrientation = Quaternion.identity;
            }

            // Auto vector rotation
            if (IsAutoTrackEnabled && _shipVelocity.magnitude > 1) {
                // the orientation toward the direction of travel
                var lookDirection = Quaternion.LookRotation(transform.InverseTransformDirection(_shipVelocity), Vector3.up);

                // deadzone cancel out of look direction
                if (Quaternion.Angle(Quaternion.identity, lookDirection) < _autoTrackDeadzone)
                    lookDirection = Quaternion.identity;

                // keep within the users' defined "cone"
                var clampedDirection = MathfExtensions.ClampRotation(lookDirection, _autoTrackMin, _autoTrackMax);

                // use the speed of the ship to handle how much to look too
                var speedAdjustedDirection = Quaternion.Lerp(Quaternion.identity, clampedDirection, _shipVelocity.magnitude.Remap(50, 300, 0, 1));

                // use the amount as requested by the users' prefs as to how much to look
                var preferenceAdjustedDirection = Quaternion.Lerp(Quaternion.identity, speedAdjustedDirection, _autoTrackAmount);

                // if enabled and direction is outside the cone of vision, revert to forward
                if (_autoTrackSnap && clampedDirection != lookDirection)
                    preferenceAdjustedDirection = Quaternion.identity;

                // smoothing
                _autoTrackHeadOrientation = Quaternion.Lerp(_autoTrackHeadOrientation, preferenceAdjustedDirection, _autoTrackDamping);
            }
            else {
                _autoTrackHeadOrientation = Quaternion.identity;
            }

            // Add any other tracking methods here

            // Accumulate the transform
            // positional
            _headTransform.position += _openTrackHeadPosition;
            _headTransform.position += _trackIrHeadPosition;

            // rotational
            _headTransform.orientation *= _openTrackHeadOrientation;
            _headTransform.orientation *= _trackIrHeadOrientation;
            _headTransform.orientation *= _autoTrackHeadOrientation;
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        public void SetShipVelocityVector(Vector3 velocity) {
            _shipVelocity = velocity;
        }

        private void OnGameSettingsApplied() {
            IsOpenTrackEnabled = Preferences.Instance.GetBool("openTrackEnabled");
            IsTrackIrEnabled = Preferences.Instance.GetBool("trackIrEnabled");
            IsAutoTrackEnabled = Preferences.Instance.GetBool("autoTrackEnabled") && Preferences.Instance.GetString("autoTrackBindType") != "hold";
            _autoTrackAmount = Preferences.Instance.GetFloat("autoTrackAmount");
            _autoTrackDamping = Mathf.Pow(Preferences.Instance.GetFloat("autoTrackDamping").Remap(0, 1, 1f, 0.4f), 5);
            _autoTrackDeadzone = Preferences.Instance.GetFloat("autoTrackDeadzoneDegrees");
            _autoTrackMin = new Vector3(
                -Preferences.Instance.GetFloat("autoTrackUpDegrees"),
                -Preferences.Instance.GetFloat("autoTrackHorizontalDegrees"),
                0
            );
            _autoTrackMax = new Vector3(
                Preferences.Instance.GetFloat("autoTrackDownDegrees"),
                Preferences.Instance.GetFloat("autoTrackHorizontalDegrees"),
                0
            );
            _autoTrackSnap = Preferences.Instance.GetBool("autoTrackSnapForward");

            trackIr.gameObject.SetActive(IsTrackIrEnabled);
        }
    }
}