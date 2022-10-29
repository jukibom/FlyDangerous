using Bhaptics.Tact.Unity;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using Misc;
using UnityEngine;

namespace Core.ShipModel.Feedback.bHaptics {
    public class BHapticsShipFeedback : Singleton<BHapticsShipFeedback>, IShipFeedback, IShipInstruments {
        [SerializeField] private VestHapticClip collisionImpactVestHapticClip;
        [SerializeField] private VestHapticClip boostSpoolVestHapticClip;
        [SerializeField] private VestHapticClip boostFireVestHapticClip;
        [SerializeField] private VestHapticClip shipShakeVestHapticClip;

        [SerializeField] private ArmsHapticClip collisionImpactLeftArmHapticClip;
        [SerializeField] private ArmsHapticClip boostSpoolLeftArmHapticClip;
        [SerializeField] private ArmsHapticClip boostFireLeftArmHapticClip;
        [SerializeField] private ArmsHapticClip shipShakeLeftArmHapticClip;
        [SerializeField] private ArmsHapticClip toggleFunctionLeftArmHapticClip;

        [SerializeField] private ArmsHapticClip collisionImpactRightArmHapticClip;
        [SerializeField] private ArmsHapticClip boostSpoolRightArmHapticClip;
        [SerializeField] private ArmsHapticClip boostFireRightArmHapticClip;
        [SerializeField] private ArmsHapticClip shipShakeRightArmHapticClip;
        [SerializeField] private ArmsHapticClip toggleFunctionRightArmHapticClip;

        [SerializeField] private HeadHapticClip collisionImpactHeadHapticClip;
        [SerializeField] private HeadHapticClip boostFireHeadHapticClip;
        [SerializeField] private HeadHapticClip toggleNightVisionHeadClip;

        private bool _firstUpdate = true;
        private bool _isEnabled;
        private bool _nightVisionActive;
        private bool _rotationalAssistActive;

        // No idea why but `IsPlaying()` always returns false :/
        private float _shakeHapticPlayTime;
        private bool _vectorAssistActive;
        private bool _velocityLimiterActive;

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        public void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
            if (!_isEnabled) return;
            if (!Game.Instance.InGame) return;

            if (shipFeedbackData.BoostSpoolStartThisFrame) {
                boostSpoolVestHapticClip.Play();
                boostSpoolLeftArmHapticClip.Play(0.3f, 2);
                boostSpoolRightArmHapticClip.Play(0.3f, 2);
            }

            if (shipFeedbackData.BoostThrustStartThisFrame) {
                boostFireVestHapticClip.Play(5, 1.5f);
                boostFireLeftArmHapticClip.Play();
                boostFireRightArmHapticClip.Play();
                boostFireHeadHapticClip.Play();
            }

            if (shipFeedbackData.ShipShakeNormalised > 0 && _shakeHapticPlayTime > 0.1f) {
                _shakeHapticPlayTime = 0;
                shipShakeVestHapticClip.Play(shipFeedbackData.ShipShakeNormalised * 30, 0.2f);
                shipShakeLeftArmHapticClip.Play(shipFeedbackData.ShipShakeNormalised * 15, 0.2f);
                shipShakeRightArmHapticClip.Play(shipFeedbackData.ShipShakeNormalised * 15, 0.2f);
            }

            _shakeHapticPlayTime += Time.fixedDeltaTime;

            if (shipFeedbackData.CollisionStartedThisFrame) {
                collisionImpactVestHapticClip.Play(shipFeedbackData.CollisionImpactNormalised, 1, shipFeedbackData.CollisionDirection, Vector3.zero,
                    Vector3.forward, 1);
                collisionImpactLeftArmHapticClip.Play(shipFeedbackData.CollisionImpactNormalised);
                collisionImpactRightArmHapticClip.Play(shipFeedbackData.CollisionImpactNormalised);
                collisionImpactHeadHapticClip.Play(shipFeedbackData.CollisionImpactNormalised);
            }
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            if (!_isEnabled) return;

            if (_firstUpdate) {
                _vectorAssistActive = shipInstrumentData.VelocityLimiterActive;
                _rotationalAssistActive = shipInstrumentData.RotationalFlightAssistActive;
                _velocityLimiterActive = shipInstrumentData.VelocityLimiterActive;
                _nightVisionActive = shipInstrumentData.LightsActive;
                _firstUpdate = false;
                return;
            }

            if (_vectorAssistActive != shipInstrumentData.VectorFlightAssistActive) {
                _vectorAssistActive = shipInstrumentData.VectorFlightAssistActive;
                toggleFunctionLeftArmHapticClip.Play();
                toggleFunctionRightArmHapticClip.Play();
            }

            if (_rotationalAssistActive != shipInstrumentData.RotationalFlightAssistActive) {
                _rotationalAssistActive = shipInstrumentData.RotationalFlightAssistActive;
                toggleFunctionLeftArmHapticClip.Play();
                toggleFunctionRightArmHapticClip.Play();
            }

            if (_velocityLimiterActive != shipInstrumentData.VelocityLimiterActive) {
                _velocityLimiterActive = shipInstrumentData.VelocityLimiterActive;
                toggleFunctionLeftArmHapticClip.Play();
                toggleFunctionRightArmHapticClip.Play();
            }

            if (_nightVisionActive != shipInstrumentData.LightsActive) {
                _nightVisionActive = shipInstrumentData.LightsActive;
                toggleFunctionLeftArmHapticClip.Play();
                toggleFunctionRightArmHapticClip.Play();
                if (_nightVisionActive) toggleNightVisionHeadClip.Play();
            }
        }

        private void OnGameSettingsApplied() {
            _isEnabled = Preferences.Instance.GetBool("bHapticsEnabled");
        }
    }
}