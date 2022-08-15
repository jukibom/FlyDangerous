using Bhaptics.Tact.Unity;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using Misc;
using UnityEngine;

namespace Core.ShipModel.Feedback.bHaptics {
    public class BHapticsShipFeedback : MonoBehaviour, IShipFeedback, IShipIndicators {

        [SerializeField] private HapticClip boostDropVestHapticSource;
        [SerializeField] private HapticClip boostFireVestHapticSource;

        // public void Collision(float forceNormalised, Vector3 direction) {
        //     Debug.Log("Collision! " + forceNormalised + " " + direction);
        //     
        //     boostFireVestHapticSource.Play();
        // }
        //
        // public void BoostDrop(float progressNormalised, bool firstFrame) {
        //     Debug.Log("Boost Drop " + progressNormalised);
        //     boostDropVestHapticSource.Play(1, 0.02f, 0, MathfExtensions.Remap(0, 1, 0.5f, -0.5f, progressNormalised));
        // }
        //
        // public void Boost(float progressNormalised, bool firstFrame) {
        //     Debug.Log("Boost " + progressNormalised);
        //     // Fire once and rely on shake for the rest
        //     if (firstFrame) {
        //         boostFireVestHapticSource.Play();
        //     }
        // }
        //
        // public void ShipShake(float shakeAmountNormalised) {
        //     Debug.Log("Ship Shake " + shakeAmountNormalised);
        // }
        //
        // public void ShipIndicatorUpdate(IShipIndicatorData shipIndicatorData) {
        //     
        //     Debug.Log("ship indicators! yay!");
        // }

        public void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
            Debug.Log("Feedback update");
        }

        public void OnShipIndicatorUpdate(IShipIndicatorData shipIndicatorData) {
            Debug.Log("Indicator Update");
        }
    }
}