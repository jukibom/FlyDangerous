using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipFeedbackData {
        bool CollisionThisFrame { get; }
        bool CollisionStartedThisFrame { get; }
        float CollisionImpactNormalised { get; }
        Vector3 CollisionDirection { get; }
        bool BoostDropStart { get; }
        float BoostDropProgressNormalised { get; }
        bool BoostStart { get; }
        float BoostProgressNormalised { get; }
        float ShipShake { get; }
    }
}