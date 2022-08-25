using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    // Typically used to provide feedback data to integrations like rumble / haptics.
    // Most values are provided normalised (-1:1 or 0:1) for ease of integration.
    public interface IShipFeedbackData {
        // True if collision occured on this frame, and the collision data is valid to read.
        bool CollisionThisFrame { get; }

        // True if a collision has BEGUN this frame (rather than a continuation of a previous collision)
        bool CollisionStartedThisFrame { get; }

        // The normalised force of the impact as calculated from the dot product of the normalised velocity
        // and the collision normal multiplied by normalised velocity. ~0 = light graze, 1 = full speed head-on
        float CollisionImpactNormalised { get; }

        // The direction from the users' perspective in which the collision occured
        Vector3 CollisionDirection { get; }

        // True if the boost is currently firing but not yet active (the charge-up period)
        bool IsBoostSpooling { get; }

        // True if the boost spooling was initiated on this physics frame
        bool BoostSpoolStartThisFrame { get; }

        // True if the boost is currently firing
        bool IsBoostThrustActive { get; }

        // True if the boost effect after spooling was initiated this frame
        bool BoostThrustStartThisFrame { get; }

        // The time in seconds it takes for a boost to spool
        public float BoostSpoolTotalDurationSeconds { get; }

        // The time in seconds the boost is set to last
        public float BoostThrustTotalDurationSeconds { get; }

        // Amount of spooling occured as a normalised value
        float BoostSpoolProgressNormalised { get; }

        // The progress through the boost effect as a normalised value
        float BoostThrustProgressNormalised { get; }

        // The amount of ship shake currently occurring as a normalised value
        float ShipShakeNormalised { get; }
    }
}