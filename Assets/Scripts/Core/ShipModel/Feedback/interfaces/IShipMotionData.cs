using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    // Typically used to provide physics data to integrations like motion rigs.
    // Most values are provided normalised (-1:1 or 0:1) using the maximums for ease of integration.
    public interface IShipMotionData {
        // The ship is ready to fly
        public bool ShipActive { get; }

        // The raw lateral velocity in ship local space m/s
        Vector3 CurrentLateralVelocity { get; }

        // The raw lateral acceleration force in ship local space N
        Vector3 CurrentLateralForce { get; }

        // The raw angular velocity in ship local space radians per second
        Vector3 CurrentAngularVelocity { get; }

        // The raw angular torque in ship local space Newton Meters
        Vector3 CurrentAngularTorque { get; }

        // The lateral velocity as a normalised value from the maximum
        Vector3 CurrentLateralVelocityNormalised { get; }

        // The lateral acceleration force in ship local space normalised values
        Vector3 CurrentLateralForceNormalised { get; }

        // The angular velocity in ship local space normalised values
        Vector3 CurrentAngularVelocityNormalised { get; }

        // The raw angular torque in ship local space normalised values
        Vector3 CurrentAngularTorqueNormalised { get; }

        // The raw orientation of the craft in world space
        Vector3 WorldRotationEuler { get; }

        // The raw maximum lateral velocity magnitude in m/s permitted by the ship configuration
        float MaxSpeed { get; }
    }
}