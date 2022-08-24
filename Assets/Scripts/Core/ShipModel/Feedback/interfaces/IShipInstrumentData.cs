namespace Core.ShipModel.ShipIndicator {
    public interface IShipInstrumentData {
        public float VelocityMagnitude { get; }
        public float AccelerationMagnitudeNormalised { get; }
        public float GForce { get; }
        public float PitchPositionNormalised { get; }
        public float RollPositionNormalised { get; }
        public float YawPositionNormalised { get; }
        public float ThrottlePositionNormalised { get; }
        public float BoostCapacitorPercent { get; }
        public bool BoostTimerReady { get; }
        public bool BoostChargeReady { get; }
        public bool LightsActive { get; }
        public bool VelocityLimiterActive { get; }
        public bool VectorFlightAssistActive { get; }
        public bool RotationalFlightAssistActive { get; }
        public bool ProximityWarning { get; }
        public float ProximityWarningSeconds { get; }
    }
}