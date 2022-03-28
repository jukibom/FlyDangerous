using MessagePack;
using UnityEngine;

namespace Core.Replays {
    [MessagePackObject]
    public struct KeyFrame {
        [Key(0)] public Vector3 replayFloatingOrigin;
        [Key(1)] public Vector3 position;
        [Key(2)] public Quaternion rotation;
        [Key(3)] public Vector3 velocity;
        [Key(4)] public Vector3 angularVelocity;
    }
}