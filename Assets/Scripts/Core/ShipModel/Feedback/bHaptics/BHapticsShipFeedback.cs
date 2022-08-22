using Bhaptics.Tact.Unity;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using UnityEngine;

namespace Core.ShipModel.Feedback.bHaptics {
    public class BHapticsShipFeedback : MonoBehaviour, IShipFeedback, IShipInstruments {
        [SerializeField] private FeedbackEngine feedbackEngine;
        [SerializeField] private HapticClip boostDropVestHapticSource;
        [SerializeField] private HapticClip boostFireVestHapticSource;

        private void OnEnable() {
            feedbackEngine.SubscribeFeedbackObject(this);
        }

        private void OnDisable() {
            feedbackEngine.RemoveFeedbackObject(this);
        }

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
            // if (shipFeedbackData.BoostDropStartThisFrame) Debug.Log("BOOST DROP START " + Time.frameCount);
            // if (shipFeedbackData.BoostThrustStartThisFrame) Debug.Log("BOOST THRUST START " + Time.frameCount);
            // if (shipFeedbackData.IsBoostDropActive) Debug.Log("Drop " + shipFeedbackData.BoostDropProgressNormalised);
            // if (shipFeedbackData.IsBoostThrustActive) Debug.Log("Thrust " + shipFeedbackData.BoostThrustProgressNormalised);
            //
            // if (shipFeedbackData.IsBoostDropActive)
            //     boostDropVestHapticSource.Play(0.5f, 1, 0, shipFeedbackData.BoostDropProgressNormalised);
        }

        public void OnShipIndicatorUpdate(IShipInstrumentData shipInstrumentData) {
        }
    }
}