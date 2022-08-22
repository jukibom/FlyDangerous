using Core.ShipModel.Feedback.interfaces;
using UnityEngine;

namespace Core.ShipModel.Feedback {
    public class ShipFeedbackData : IShipFeedbackData {
        public bool CollisionThisFrame { get; set; }
        public bool CollisionStartedThisFrame { get; set; }
        public float CollisionImpactNormalised { get; set; }
        public Vector3 CollisionDirection { get; set; }
        public bool IsBoostDropActive { get; set; }
        public bool IsBoostThrustActive { get; set; }
        public bool BoostDropStartThisFrame { get; set; }
        public float BoostDropProgressNormalised { get; set; }
        public bool BoostThrustStartThisFrame { get; set; }
        public float BoostThrustProgressNormalised { get; set; }
        public float ShipShake { get; set; }
    }
}