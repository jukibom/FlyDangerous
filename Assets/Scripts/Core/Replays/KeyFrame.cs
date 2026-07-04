using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using UnityEngine;
using System;

namespace Core.Replays {
    [MessagePackObject]
    public struct KeyFrame {
        [Key(0)] public Vector3 replayFloatingOrigin;
        [Key(1)] public Vector3 position;
        [Key(2)] public Quaternion rotation;
        [Key(3)] public Vector3 velocity;
        [Key(4)] public Vector3 angularVelocity;
    }

    [MessagePackObject]
        public struct KeyFrameV2 {
        [Key(0)] public Vector3 replayFloatingOrigin;
        [Key(1)] public Vector3 position;
        [Key(2)] public Quaternion rotation;
        [Key(3)] public Vector3 velocity;
        [Key(4)] public Vector3 angularVelocity;
        [Key(5)] public BoostStatus boostStatus;
        [Key(6)] public float boostProgressTicks; // yea casting this to a float is dumb, but intergers dont have fixed size when written so... it wont break till 16777216 which should be more then enough
        [Key(7)] public float boostTime;
        [Key(8)] public float boostCapacitorPercent;
    

        public static KeyFrameV2 Deserialize(string version, ref byte[] bytes) {
        if (version == "1.0.0" || version == "1.1.0") {
            var keyFrame = MessagePackSerializer.Deserialize<KeyFrame>(bytes);
            return new KeyFrameV2 {
                replayFloatingOrigin = keyFrame.replayFloatingOrigin,
                position = keyFrame.position,
                rotation = keyFrame.rotation,
                velocity = keyFrame.velocity,
                angularVelocity = keyFrame.angularVelocity,
            };
        }

        if (version == "1.1.1")
            return MessagePackSerializer.Deserialize<KeyFrameV2>(bytes);

        throw new Exception("Unrecognised replay version, cannot deserialize input frame");
        }
    }
}