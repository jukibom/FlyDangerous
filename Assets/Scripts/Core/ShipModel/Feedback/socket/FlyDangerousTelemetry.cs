using System;
using System.Runtime.InteropServices;
using System.Text;
using Misc;
using Newtonsoft.Json;

namespace Core.ShipModel.Feedback.socket {
    internal enum BroadcastFormat {
        Bytes,
        Json
    }

    public struct FlyDangerousTelemetry {
        // Meta
        public uint flyDangerousTelemetryId;
        public uint packetId;

        // Game State
        public string gameVersion;
        public string currentGameMode;
        public string currentLevelName;
        public string currentMusicTrackName;
        public int currentPlayerCount;

        // Instrument Data
        public SerializableVector3 shipWorldPosition;
        public float shipAltitude;
        public float shipSpeed;
        public float accelerationMagnitudeNormalised;
        public float gForce;
        public float pitchPosition;
        public float rollPosition;
        public float yawPosition;
        public float throttlePosition;
        public float lateralHPosition;
        public float lateralVPosition;
        public float boostCapacitorPercent;
        public bool boostTimerReady;
        public bool boostChargeReady;
        public bool lightsActive;
        public bool velocityLimiterActive;
        public bool vectorFlightAssistActive;
        public bool rotationalFlightAssistActive;
        public bool proximityWarning;
        public float proximityWarningSeconds;

        // Feedback Data
        public bool collisionThisFrame;
        public bool collisionStartedThisFrame;
        public float collisionImpactNormalised;
        public SerializableVector3 collisionDirection;
        public bool isBoostSpooling;
        public bool boostSpoolStartedThisFrame;
        public bool isBoostThrustActive;
        public bool boostThrustStartedThisFrame;
        public float boostSpoolTotalDurationSeconds;
        public float boostThrustTotalDurationSeconds;
        public float boostThrustProgressNormalised;
        public float shipShakeNormalised;

        // Motion Data
        public SerializableVector3 currentLateralVelocity;
        public SerializableVector3 currentLateralForce;
        public SerializableVector3 currentAngularVelocity;
        public SerializableVector3 currentAngularTorque;
        public SerializableVector3 currentLateralVelocityNormalised;
        public SerializableVector3 currentLateralForceNormalised;
        public SerializableVector3 currentAngularVelocityNormalised;
        public SerializableVector3 currentAngularTorqueNormalised;
        public float maxSpeed;

        public override string ToString() {
            return
                $"Fly Dangerous version {gameVersion}, packetId: {packetId}, ship details: speed - {shipSpeed}m/s, altitude {shipAltitude} - world position {shipWorldPosition}";
        }
    }

    #region Serialisers

    public static class FlyDangerousTelemetryEncoder {
        internal static byte[] EncodePacket(BroadcastFormat broadcastFormat, FlyDangerousTelemetry telemetry) {
            byte[] packet;
            switch (broadcastFormat) {
                case BroadcastFormat.Bytes:
                    packet = StructureToByteArray(telemetry);
                    break;
                case BroadcastFormat.Json:
                    packet = StructureToJson(telemetry);
                    break;
                default:
                    throw new Exception("Failed attempting to output telemetry without target format!");
            }

            return packet;
        }

        private static byte[] StructureToJson(FlyDangerousTelemetry telemetryStruct) {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryStruct, Formatting.Indented));
        }

        // TODO: can we pre-alloc and update the same byte array?
        private static byte[] StructureToByteArray(FlyDangerousTelemetry telemetryStruct) {
            var size = Marshal.SizeOf(telemetryStruct);
            var arr = new byte[size];
            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(telemetryStruct, ptr, false);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }
    }

    #endregion

    #region Deserialisers

    public static class FlyDangerousTelemetryDecoder {
        private static FlyDangerousTelemetry _telemetry;

        internal static FlyDangerousTelemetry DecodePacket(BroadcastFormat broadcastFormat, byte[] bytes) {
            switch (broadcastFormat) {
                case BroadcastFormat.Bytes:
                    _telemetry = ByteArrayToStructure(bytes);
                    break;
                case BroadcastFormat.Json:
                    _telemetry = JsonStringToStructure(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    break;
            }

            return _telemetry;
        }

        private static FlyDangerousTelemetry JsonStringToStructure(string json) {
            return JsonConvert.DeserializeObject<FlyDangerousTelemetry>(json);
        }

        private static FlyDangerousTelemetry ByteArrayToStructure(byte[] bytes) {
            var telemetry = new FlyDangerousTelemetry();

            var size = Marshal.SizeOf(telemetry);
            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                telemetry = (FlyDangerousTelemetry)Marshal.PtrToStructure(ptr, telemetry.GetType());
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            return telemetry;
        }
    }

    #endregion
}