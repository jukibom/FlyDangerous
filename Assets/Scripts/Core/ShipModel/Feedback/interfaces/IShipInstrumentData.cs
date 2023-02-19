using UnityEngine;

namespace Core.ShipModel.Feedback.interfaces {
    // Typically used for displaying instrumentation in both the in-game ship and third party integrations
    // e.g. a custom display in a sim pit.
    // Many values are exposed as normalised (-1:1 or 0:1) for ease of integration.
    public interface IShipInstrumentData {
        // The ship is ready to fly
        public bool ShipActive { get; }

        // The world position coordinates from the world origin
        public Vector3 WorldPosition { get; }

        // The ship height from the terrain, if applicable (Infinity in space!)
        public float ShipHeightFromGround { get; }

        // The ship altitude from mean sea level in meters, if applicable (Infinity in space!)
        public float Altitude { get; }

        // The raw velocity magnitude in m/s
        public float Speed { get; }

        // The amount of acceleration applied as a normalised value.
        public float AccelerationMagnitudeNormalised { get; }

        // The raw GForce currently applied to the seat of the ship in Gs
        public float GForce { get; }

        // The amount of pitch applied as input as a normalised value
        public float PitchPositionNormalised { get; }

        // The amount of roll applied as input as a normalised value
        public float RollPositionNormalised { get; }

        // The amount of yaw applied as input as a normalised value
        public float YawPositionNormalised { get; }

        // The position of the throttle as a normalised value (where -1 is reverse)
        public float ThrottlePositionNormalised { get; }

        // The position of lateral (horizontal) thrusters
        public float LateralHPositionNormalised { get; }

        // The position of vertical thrusters
        public float LateralVPositionNormalised { get; }

        // The value of the boost / engine capacitor as a percentage
        public float BoostCapacitorPercent { get; }

        // True if the boost has completed its cycle
        public bool BoostTimerReady { get; }

        // True if the boost is ready to fire
        public bool BoostChargeReady { get; }

        // True if ship lights are enabled
        public bool LightsActive { get; }

        // True if altitude is < 0
        public bool UnderWater { get; }

        // True if velocity limiter is enabled
        public bool VelocityLimiterActive { get; }

        // True if Vector Flight Assist is enabled
        public bool VectorFlightAssistActive { get; }

        // True if Rotational Flight Assist is enabled
        public bool RotationalFlightAssistActive { get; }

        // True if the Proximity Warning has detected a potential collision
        public bool ProximityWarning { get; }

        // The number of seconds before a potential collision will occur if no vector change is made
        public float ProximityWarningSeconds { get; }
    }
}