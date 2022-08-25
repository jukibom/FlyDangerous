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

        [SerializeField] private ArmsHapticClip collisionImpactRightArmHapticClip;
        [SerializeField] private ArmsHapticClip boostSpoolRightArmHapticClip;
        [SerializeField] private ArmsHapticClip boostFireRightArmHapticClip;
        [SerializeField] private ArmsHapticClip shipShakeRightArmHapticClip;

        // No idea why but `IsPlaying()` always returns false :/
        private float _shakeHapticPlayTime;

        public void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
            if (shipFeedbackData.BoostSpoolStartThisFrame) {
                boostSpoolVestHapticClip.Play();
                boostSpoolLeftArmHapticClip.Play(0.3f, 2);
                boostSpoolRightArmHapticClip.Play(0.3f, 2);
            }

            if (shipFeedbackData.BoostThrustStartThisFrame) {
                boostFireVestHapticClip.Play(5, 1.5f);
                boostFireLeftArmHapticClip.Play();
                boostFireRightArmHapticClip.Play();
            }

            if (shipFeedbackData.ShipShakeNormalised > 0 && _shakeHapticPlayTime > 0.1f) {
                _shakeHapticPlayTime = 0;
                shipShakeVestHapticClip.Play(shipFeedbackData.ShipShakeNormalised * 5, 0.1f);
                shipShakeLeftArmHapticClip.Play(shipFeedbackData.ShipShakeNormalised * 5, 0.2f);
                shipShakeRightArmHapticClip.Play(shipFeedbackData.ShipShakeNormalised * 5, 0.2f);
            }

            _shakeHapticPlayTime += Time.fixedDeltaTime;

            if (shipFeedbackData.CollisionStartedThisFrame) {
                collisionImpactVestHapticClip.Play(shipFeedbackData.CollisionImpactNormalised, 1, shipFeedbackData.CollisionDirection, Vector3.zero,
                    Vector3.forward, 1);
                collisionImpactLeftArmHapticClip.Play(shipFeedbackData.CollisionImpactNormalised);
                collisionImpactRightArmHapticClip.Play(shipFeedbackData.CollisionImpactNormalised);
            }
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
        }
    }
}