using UnityEngine;

namespace Core.ShipModel.ShipIndicator {
    public class ShipInstrumentData : IShipInstrumentData {
        public Vector3 ShipWorldPosition { get; set; }
        public float ShipAltitude { get; set; }
        public float VelocityMagnitude { get; set; }
        public float AccelerationMagnitudeNormalised { get; set; }
        public float GForce { get; set; }
        public float PitchPositionNormalised { get; set; }
        public float RollPositionNormalised { get; set; }
        public float YawPositionNormalised { get; set; }
        public float ThrottlePositionNormalised { get; set; }
        public float BoostCapacitorPercent { get; set; }
        public bool BoostTimerReady { get; set; }
        public bool BoostChargeReady { get; set; }
        public bool LightsActive { get; set; }
        public bool VelocityLimiterActive { get; set; }
        public bool VectorFlightAssistActive { get; set; }
        public bool RotationalFlightAssistActive { get; set; }
        public bool ProximityWarning { get; set; }
        public float ProximityWarningSeconds { get; set; }
    }
}