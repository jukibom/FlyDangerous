namespace Core.ShipModel.ShipIndicator {
    public class ShipIndicatorData : IShipIndicatorData {
        public float ThrottlePosition { get; set; }
        public float Velocity { get; set; }
        public float Acceleration { get; set; }
        public float Throttle { get; set; }
        public float GForce { get; set; }
        public float BoostCapacitorPercent { get; set; }
        public bool BoostTimerReady { get; set; }
        public bool BoostChargeReady { get; set; }
        public bool LightsActive { get; set; }
        public bool VelocityLimiterActive { get; set; }
        public bool VectorFlightAssistActive { get; set; }
        public bool RotationalFlightAssistActive { get; set; }
    }
}