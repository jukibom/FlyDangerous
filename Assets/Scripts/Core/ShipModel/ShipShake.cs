using Core.Player;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.ShipModel {
    public class ShipShake {
        private readonly AnimationCurve _linearCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));
        private readonly Vector3 _originalPos;
        [CanBeNull] private readonly ShipCameraRig _shipCameraRig;

        private readonly Transform _shipTransform;

        private bool _cameraShake;

        private float _shakeAmount;
        private AnimationCurve _shakeAmountCurve;

        // How long the object should shake for.
        private float _shakeDuration;
        private float _shakeTimer;

        // Amplitude of the shake. A larger value shakes the camera harder.
        private float _targetShakeAmount;

        public ShipShake(Transform shipTransform, ShipCameraRig shipCameraRig = null) {
            _shipTransform = shipTransform;
            _originalPos = _shipTransform.localPosition;

            _shipCameraRig = shipCameraRig;
        }

        public void Shake(float duration, float amount, bool includeExternalCameraShake = false, AnimationCurve shakeAmountCurve = null) {
            if (shakeAmountCurve == null)
                shakeAmountCurve = _linearCurve;

            _shakeAmountCurve = shakeAmountCurve;
            _shakeDuration = duration;
            _shakeTimer = duration;
            _targetShakeAmount = amount;
            _cameraShake = includeExternalCameraShake;
        }

        public void Reset() {
            _shakeTimer = 0;
            _shakeDuration = 0;
            _shakeAmount = 0;
        }

        public void Update() {
            if (_shakeTimer > 0) {
                var shake = _shakeAmountCurve.Evaluate(_shakeTimer / _shakeDuration);
                _shakeAmount = shake * _targetShakeAmount;
                _shipTransform.localPosition = _originalPos + Random.insideUnitSphere * _shakeAmount;
                _shakeTimer -= Time.deltaTime;

                if (_cameraShake && _shipCameraRig != null)
                    _shipCameraRig.SetBoostEffect(shake);
            }
            else {
                _shakeTimer = 0f;
                _shakeDuration = 0f;
                _shipTransform.localPosition = _originalPos;
            }
        }
    }
}