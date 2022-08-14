namespace Core.ShipModel.ShipIndicator {
    public interface IShipIndicatorData {
        public float ThrottlePosition { get; } // -1 - 1
        public float Velocity { get; } // m/s
        public float Acceleration { get; } // 0 - 1
        public float Throttle { get; } // 0 - 1
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