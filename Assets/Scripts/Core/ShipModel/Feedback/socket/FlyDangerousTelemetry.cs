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

    [StructLayout(LayoutKind.Sequential)]
    public struct FlyDangerousTelemetry {
        // Meta
        public uint flyDangerousTelemetryId;
        public uint packetId;

        // Game State
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] gameVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] currentGameMode;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] currentLevelName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] currentMusicTrackName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] currentShipName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] playerName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] playerFlagIso;

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

        // string helpers for char[] handling
        public string GameVersion => new string(gameVersion).TrimEnd();
        public string CurrentGameMode => new string(currentGameMode).TrimEnd();
        public string CurrentLevelName => new string(currentLevelName).TrimEnd();
        public string CurrentMusicTrackName => new string(currentMusicTrackName).TrimEnd();

        public override string ToString() {
            return
                $"Fly Dangerous version {GameVersion}, packetId: {packetId}\nGame details: game mode - {CurrentGameMode}, level - {CurrentLevelName}, music - {CurrentMusicTrackName}\nShip details: speed - {shipSpeed}m/s, altitude {shipAltitude} - world position {shipWorldPosition}";
        }
    }

    #region Serialisers

    public static class FlyDangerousTelemetryEncoder {
        // used for pre-allocating bytes for encoding (size of struct never changes)
        private static int _sizeBytes;
        private static byte[] _arrBytes;

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

        private static byte[] StructureToByteArray(FlyDangerousTelemetry telemetryStruct) {
            if (_arrBytes == null) {
                _sizeBytes = Marshal.SizeOf(telemetryStruct);
                _arrBytes = new byte[_sizeBytes];
            }

            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(_sizeBytes);
                Marshal.StructureToPtr(telemetryStruct, ptr, false);
                Marshal.Copy(ptr, _arrBytes, 0, _sizeBytes);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            return _arrBytes;
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