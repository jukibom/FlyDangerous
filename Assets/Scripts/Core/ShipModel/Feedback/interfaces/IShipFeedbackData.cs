using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipFeedbackData {
        bool CollisionThisFrame { get; }
        bool CollisionStartedThisFrame { get; }
        float CollisionImpactNormalised { get; }
        Vector3 CollisionDirection { get; }
        bool IsBoostSpooling { get; }
        bool IsBoostThrustActive { get; }
        public float BoostSpoolTotalDuration { get; }
        public float BoostThrustTotalDuration { get; }

        bool BoostSpoolStartThisFrame { get; }
        float BoostSpoolProgressNormalised { get; }
        bool BoostThrustStartThisFrame { get; }
        float BoostThrustProgressNormalised { get; }
        float ShipShake { get; }
    }
}