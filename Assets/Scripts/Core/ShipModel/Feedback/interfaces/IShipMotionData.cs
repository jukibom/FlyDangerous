using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipMotionData {
        Vector3 CurrentLateralVelocity { get; }
        Vector3 CurrentLateralForce { get; }
        Vector3 CurrentAngularVelocity { get; }
        Vector3 CurrentAngularTorque { get; }
        Vector3 CurrentLateralVelocityNormalised { get; }
        Vector3 CurrentLateralForceNormalised { get; }
        Vector3 CurrentAngularVelocityNormalised { get; }
        Vector3 CurrentAngularTorqueNormalised { get; }
        float MaxLateralVelocity { get; }
    }
}