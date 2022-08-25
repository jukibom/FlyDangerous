using Core.ShipModel.Feedback.interfaces;
using UnityEngine;

namespace Core.ShipModel.Feedback {
    public class ShipFeedbackData : IShipFeedbackData {
        public bool CollisionThisFrame { get; set; }
        public bool CollisionStartedThisFrame { get; set; }
        public float CollisionImpactNormalised { get; set; }
        public Vector3 CollisionDirection { get; set; }
        public bool IsBoostSpooling { get; set; }
        public bool BoostSpoolStartThisFrame { get; set; }
        public bool IsBoostThrustActive { get; set; }
        public bool BoostThrustStartThisFrame { get; set; }
        public float BoostSpoolTotalDurationSeconds { get; set; }
        public float BoostThrustTotalDurationSeconds { get; set; }
        public float BoostSpoolProgressNormalised { get; set; }
        public float BoostThrustProgressNormalised { get; set; }
        public float ShipShakeNormalised { get; set; }
    }
}