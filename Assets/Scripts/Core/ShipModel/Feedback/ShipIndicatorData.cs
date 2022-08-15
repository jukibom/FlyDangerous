namespace Core.ShipModel.ShipIndicator {
    public class ShipIndicatorData : IShipIndicatorData {
        public float ThrottlePositionNormalised { get; set; }
        public float VelocityMagnitude { get; set; }
        public float AccelerationMagnitudeNormalised { get; set; }
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