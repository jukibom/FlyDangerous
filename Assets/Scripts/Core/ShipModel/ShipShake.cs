using System.Collections.Generic;
using Core.Player;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.ShipModel {
    public class Shake {
        public float amount;
        public AnimationCurve animationCurve;
        public float duration;
        public bool shakeCamera;
        public float timer;
    }

    public class ShipShake {
        private readonly AnimationCurve _linearCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));
        private readonly Vector3 _originalPos;
        private readonly List<Shake> _shakes = new();
        [CanBeNull] private readonly ShipCameraRig _shipCameraRig;

        private readonly Transform _shipTransform;

        public ShipShake(Transform shipTransform, ShipCameraRig shipCameraRig = null) {
            _shipTransform = shipTransform;
            _originalPos = _shipTransform.localPosition;

            _shipCameraRig = shipCameraRig;
        }

        public float CurrentShakeAmount { get; private set; }
        public float CurrentBoostShakeAmount { get; private set; }

        public void AddShake(float duration, float amount, bool includeExternalCameraShake = false, AnimationCurve shakeAmountCurve = null) {
            if (shakeAmountCurve == null)
                shakeAmountCurve = _linearCurve;

            _shakes.Add(new Shake {
                duration = duration,
                timer = duration,
                amount = amount,
                shakeCamera = includeExternalCameraShake,
                animationCurve = shakeAmountCurve
            });
        }

        public void Reset() {
            _shakes.Clear();
            CurrentShakeAmount = 0;
        }

        public void Update() {
            CurrentShakeAmount = 0;
            CurrentBoostShakeAmount = 0;

            // accumulate shakes
            foreach (var shake in _shakes) {
                Debug.Log(shake + " " + shake.duration + " " + shake.timer);
                if (shake.timer > 0) {
                    var shakeFactor = shake.animationCurve.Evaluate(shake.timer / shake.duration);
                    CurrentShakeAmount += shakeFactor * shake.amount;
                    if (shake.shakeCamera) CurrentBoostShakeAmount += shakeFactor * shake.amount;
                    shake.timer -= Time.deltaTime;
                }
            }

            // apply shakes
            _shipTransform.localPosition = _originalPos + Random.insideUnitSphere * CurrentShakeAmount;
            if (_shipCameraRig != null) {
                Debug.Log(CurrentBoostShakeAmount);
                _shipCameraRig.SetBoostEffect(CurrentBoostShakeAmount);
            }

            // clear defunct shakes
            _shakes.RemoveAll(shake => shake.timer <= 0);
        }
    }
}