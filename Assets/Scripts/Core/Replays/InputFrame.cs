using MessagePack;

namespace Core.Replays {
    
    [MessagePackObject]
    public struct InputFrame {
        [Key(0)]
        public float pitch;
        [Key(1)]
        public float yaw;
        [Key(2)]
        public float roll;
        [Key(3)]
        public float throttle;
        [Key(4)]
        public float lateralH;
        [Key(5)]
        public float lateralV;
        [Key(6)]
        public bool boostHeld;
        [Key(7)]
        public bool limiterHeld;
        [Key(8)]
        public bool shipLightsEnabled;
        [Key(9)]
        public bool reserved1;
        [Key(10)]
        public bool reserved2;
        [Key(11)]
        public bool reserved3;
        [Key(12)]
        public bool reserved4;
        [Key(13)]
        public bool reserved5;
    }
}