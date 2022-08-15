namespace Core.ShipModel.ShipIndicator {
    public interface IShipIndicatorData {
        public float ThrottlePositionNormalised { get; } // -1 - 1
        public float VelocityMagnitude { get; } // m/s
        public float AccelerationMagnitudeNormalised { get; } // 0 - 1
        public float GForce { get; }
        public float BoostCapacitorPercent { get; } // 0 - 100
        public bool BoostTimerReady { get; }
        public bool BoostChargeReady { get; }
        public bool LightsActive { get; }
        public bool VelocityLimiterActive { get; }
        public bool VectorFlightAssistActive { get; }
        public bool RotationalFlightAssistActive { get; }
    }
}