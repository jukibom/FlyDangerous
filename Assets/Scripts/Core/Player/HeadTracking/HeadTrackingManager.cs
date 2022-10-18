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
        private static readonly Vector3 _autoTrackMin = new(-30, -45, 0);
        private static readonly Vector3 _autoTrackMax = new(10, 45, 0);
        private HeadTransform _headTransform;
        private OpenTrackData _openTrackData;
        private Quaternion _openTrackHeadOrientation;
        private Vector3 _openTrackHeadPosition;
        private Vector3 _shipVelocity;

        public bool IsOpenTrackEnabled { get; private set; }
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
                    _openTrackHeadPosition = Vector3.ClampMagnitude(new Vector3((float)data.x / 100, (float)data.y / 100, (float)-data.z / 100), 1);
                    // orientation convert from vec 3 euler
                    _openTrackHeadOrientation = Quaternion.Euler(-(float)data.pitch, (float)data.yaw, -(float)data.roll);
                }, 4242, 1000);

                // update the main transform synchronously each frame 
                _headTransform.position += _openTrackHeadPosition;
                _headTransform.orientation *= _openTrackHeadOrientation;
            }

            // Auto vector rotation
            if (IsAutoTrackEnabled && _shipVelocity.magnitude > 1) {
                var lookDirection = Quaternion.LookRotation(transform.InverseTransformDirection(_shipVelocity), Vector3.up);
                var clampedDirection = MathfExtensions.ClampRotation(lookDirection, _autoTrackMin, _autoTrackMax);
                _headTransform.orientation *= clampedDirection;
            }

            // Add any other tracking methods here
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
            IsAutoTrackEnabled = Preferences.Instance.GetBool("autoTrackEnabled");
            if (!IsOpenTrackEnabled) _openTrackData.Reset();
        }
    }
}