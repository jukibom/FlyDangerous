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
        private float _autoTrackDamping;
        private Quaternion _autoTrackHeadOrientation;
        private Vector3 _autoTrackMax;
        private Vector3 _autoTrackMin;
        private bool _autoTrackSnap;

        private HeadTransform _headTransform;
        private OpenTrackData _openTrackData;
        private Quaternion _openTrackHeadOrientation;
        private Vector3 _openTrackHeadPosition;
        private Vector3 _shipVelocity;
        private Quaternion _trackIrHeadOrientation;
        private Vector3 _trackIrHeadPosition;

        public bool IsOpenTrackEnabled { get; private set; }
        public bool IsTrackIrEnabled { get; private set; }
        public bool IsAutoTrackEnabled { get; private set; }

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
                _trackIrHeadPosition = trackIrTransform.localPosition;
                _trackIrHeadOrientation = trackIrTransform.rotation;
            }
            else {
                _trackIrHeadPosition = Vector3.zero;
                _trackIrHeadOrientation = Quaternion.identity;
            }

            // Auto vector rotation
            if (IsAutoTrackEnabled && _shipVelocity.magnitude > 1) {
                var lookDirection = Quaternion.LookRotation(transform.InverseTransformDirection(_shipVelocity), Vector3.up);
                var clampedDirection = MathfExtensions.ClampRotation(lookDirection, _autoTrackMin, _autoTrackMax);

                // if enabled and direction is outside the cone of vision, revert to forward
                if (_autoTrackSnap && clampedDirection != lookDirection)
                    clampedDirection = Quaternion.identity;

                _autoTrackHeadOrientation = Quaternion.Lerp(_autoTrackHeadOrientation, clampedDirection, _autoTrackDamping);
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
            IsAutoTrackEnabled = Preferences.Instance.GetBool("autoTrackEnabled");
            _autoTrackDamping = Preferences.Instance.GetFloat("autoTrackDamping").Remap(0, 1, 0.1f, 0.01f);
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