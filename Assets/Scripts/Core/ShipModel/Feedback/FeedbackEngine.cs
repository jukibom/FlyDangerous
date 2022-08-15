using System.Collections.Generic;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using UnityEngine;

namespace Core.ShipModel.Feedback {
    public class FeedbackEngine : MonoBehaviour {
        [SerializeField] private ShipPhysics shipPhysics;
        private readonly List<IShipFeedback> _shipFeedbackSubscribers = new();
        private readonly List<IShipIndicators> _shipIndicatorSubscribers = new();
        private readonly List<IShipMotion> _shipMotionSubscribers = new();

        private void Update() {
            UpdateShipFeedback(shipPhysics.ShipFeedbackData);
            UpdateShipMotion(shipPhysics.ShipMotionData);
            UpdateShipIndicators(shipPhysics.ShipIndicatorData);
        }

        /**
         * Add a subscriber - for each of the interested interfaces the object implements (IShipFeedback, IShipIndicators, IShipMotion)
         * it will be added to the relevant list and updated each physics step. If nothing is implemented, nothing will subscribe.
         */
        public void SubscribeFeedbackObject(object feedbackObject) {
            if (feedbackObject is IShipFeedback shipFeedback)
                _shipFeedbackSubscribers.Add(shipFeedback);
            if (feedbackObject is IShipMotion shipMotion)
                _shipMotionSubscribers.Add(shipMotion);
            if (feedbackObject is IShipIndicators shipIndicators)
                _shipIndicatorSubscribers.Add(shipIndicators);
        }

        /**
         * Remove a subscriber - for each of the interested interfaces the object implements (IShipFeedback, IShipIndicators, IShipMotion)
         * it will be removed from the relevant list. If nothing is implemented, or the object isn't subscribed, nothing will be removed.
         */
        public void RemoveFeedbackObject(object feedbackObject) {
            if (feedbackObject is IShipFeedback shipFeedback)
                _shipFeedbackSubscribers.Remove(shipFeedback);
            if (feedbackObject is IShipMotion shipMotion)
                _shipMotionSubscribers.Remove(shipMotion);
            if (feedbackObject is IShipIndicators shipIndicators)
                _shipIndicatorSubscribers.Remove(shipIndicators);
        }

        /**
         * Update every subscriber with ship feedback data.
         */
        private void UpdateShipFeedback(IShipFeedbackData shipFeedback) {
            foreach (var feedbackSubscriber in _shipFeedbackSubscribers)
                feedbackSubscriber.OnShipFeedbackUpdate(shipFeedback);
        }

        /**
         * Update every subscriber with ship motion data.
         */
        private void UpdateShipMotion(IShipMotionData shipMotionData) {
            foreach (var shipMotionSubscriber in _shipMotionSubscribers)
                shipMotionSubscriber.OnShipMotionUpdate(shipMotionData);
        }

        /**
         * Update every subscriber with ship indicator data.
         */
        private void UpdateShipIndicators(IShipIndicatorData shipIndicatorData) {
            foreach (var shipIndicatorSubscriber in _shipIndicatorSubscribers)
                shipIndicatorSubscriber.OnShipIndicatorUpdate(shipIndicatorData);
        }
    }
}