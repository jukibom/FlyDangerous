using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipFeedbackData {
        bool CollisionThisFrame { get; }
        bool CollisionStartedThisFrame { get; }
        float CollisionImpactNormalised { get; }
        Vector3 CollisionDirection { get; }
        bool IsBoostDropActive { get; }
        bool IsBoostThrustActive { get; }
        bool BoostDropStartThisFrame { get; }
        float BoostDropProgressNormalised { get; }
        bool BoostThrustStartThisFrame { get; }
        float BoostThrustProgressNormalised { get; }
        float ShipShake { get; }
    }
}