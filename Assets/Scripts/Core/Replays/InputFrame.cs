using System;
using MessagePack;
using UnityEngine;

namespace Core.Replays {
    [MessagePackObject]
    public struct InputFrameV100 {
        [Key(0)] public float pitch;
        [Key(1)] public float yaw;
        [Key(2)] public float roll;
        [Key(3)] public float throttle;
        [Key(4)] public float lateralH;
        [Key(5)] public float lateralV;
        [Key(6)] public bool boostHeld;
        [Key(7)] public bool limiterHeld;
        [Key(8)] public bool shipLightsEnabled;
        [Key(9)] public bool reserved1;
        [Key(10)] public bool reserved2;
        [Key(11)] public bool reserved3;
        [Key(12)] public bool reserved4;
        [Key(13)] public bool reserved5;
    }

    [MessagePackObject]
    public struct InputFrameV110 {
        [Key(0)] public float pitch;
        [Key(1)] public float yaw;
        [Key(2)] public float roll;
        [Key(3)] public float throttle;
        [Key(4)] public float lateralH;
        [Key(5)] public float lateralV;
        [Key(6)] public bool boostHeld;
        [Key(7)] public bool limiterHeld;
        [Key(8)] public bool shipLightsEnabled;
        [Key(9)] public bool reserved1;
        [Key(10)] public bool reserved2;
        [Key(11)] public bool reserved3;
        [Key(12)] public bool reserved4;
        [Key(13)] public bool reserved5;
        [Key(14)] public Vector3 modifierShipForce;
        [Key(15)] public float modifierShipDeltaSpeedCap;
        [Key(16)] public float modifierShipDeltaThrust;

        public static InputFrameV110 Deserialize(string version, ref byte[] bytes) {
            if (version == "1.0.0") {
                var inputFrame = MessagePackSerializer.Deserialize<InputFrameV100>(bytes);
                return new InputFrameV110 {
                    pitch = inputFrame.pitch,
                    yaw = inputFrame.yaw,
                    roll = inputFrame.roll,
                    throttle = inputFrame.throttle,
                    lateralH = inputFrame.lateralH,
                    lateralV = inputFrame.lateralV,
                    boostHeld = inputFrame.boostHeld,
                    limiterHeld = inputFrame.limiterHeld,
                    shipLightsEnabled = inputFrame.shipLightsEnabled
                };
            }

            if (version == "1.1.0")
                return MessagePackSerializer.Deserialize<InputFrameV110>(bytes);

            throw new Exception("Unrecognised replay version, cannot deserialize input frame");
        }
    }
}