using System;
using System.Runtime.InteropServices;
using System.Text;
using Core.MapData.Serializable;
using Newtonsoft.Json;

namespace Core.ShipModel.Feedback.socket {
    internal enum BroadcastFormat {
        Bytes,
        Json
    }

    // base struct uses regular strings for values
    public struct FlyDangerousTelemetry {
        // Meta
        public uint flyDangerousTelemetryId;
        public uint packetId;

        // Game State
        public string gameVersion;
        public string currentGameMode;
        public string currentLevelName;
        public string currentMusicTrackName;
        public string currentShipName;
        public string playerName;
        public string playerFlagIso;

        public int currentPlayerCount;

        // Instrument Data
        public SerializableVector3 shipWorldPosition;
        public float shipAltitude;
        public float shipHeightFromGround;
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

        public void SetFromFlyDangerousTelemetryBytes(ref FlyDangerousTelemetryBytes telemetry) {
            // string values
            gameVersion = new string(telemetry.gameVersion).TrimEnd();
            currentLevelName = new string(telemetry.currentLevelName).TrimEnd();
            currentGameMode = new string(telemetry.currentGameMode).TrimEnd();
            currentMusicTrackName = new string(telemetry.currentMusicTrackName).TrimEnd();
            currentShipName = new string(telemetry.currentShipName).TrimEnd();
            playerName = new string(telemetry.playerName).TrimEnd();
            playerFlagIso = new string(telemetry.playerFlagIso).TrimEnd();

            // safe values
            flyDangerousTelemetryId = telemetry.flyDangerousTelemetryId;
            packetId = telemetry.packetId;

            currentPlayerCount = telemetry.currentPlayerCount;
            shipWorldPosition = telemetry.shipWorldPosition;
            shipAltitude = telemetry.shipAltitude;
            shipHeightFromGround = telemetry.shipHeightFromGround;
            shipSpeed = telemetry.shipSpeed;
            accelerationMagnitudeNormalised = telemetry.accelerationMagnitudeNormalised;
            gForce = telemetry.gForce;
            pitchPosition = telemetry.pitchPosition;
            rollPosition = telemetry.rollPosition;
            yawPosition = telemetry.yawPosition;
            throttlePosition = telemetry.throttlePosition;
            lateralHPosition = telemetry.lateralHPosition;
            lateralVPosition = telemetry.lateralVPosition;
            boostCapacitorPercent = telemetry.boostCapacitorPercent;
            boostTimerReady = telemetry.boostTimerReady;
            boostChargeReady = telemetry.boostChargeReady;
            lightsActive = telemetry.lightsActive;
            velocityLimiterActive = telemetry.velocityLimiterActive;
            vectorFlightAssistActive = telemetry.vectorFlightAssistActive;
            rotationalFlightAssistActive = telemetry.rotationalFlightAssistActive;
            proximityWarning = telemetry.proximityWarning;
            proximityWarningSeconds = telemetry.proximityWarningSeconds;
            collisionThisFrame = telemetry.collisionThisFrame;
            collisionStartedThisFrame = telemetry.collisionStartedThisFrame;
            collisionImpactNormalised = telemetry.collisionImpactNormalised;
            collisionDirection = telemetry.collisionDirection;
            isBoostSpooling = telemetry.isBoostSpooling;
            boostSpoolStartedThisFrame = telemetry.boostSpoolStartedThisFrame;
            isBoostThrustActive = telemetry.isBoostThrustActive;
            boostThrustStartedThisFrame = telemetry.boostThrustStartedThisFrame;
            boostSpoolTotalDurationSeconds = telemetry.boostSpoolTotalDurationSeconds;
            boostThrustTotalDurationSeconds = telemetry.boostThrustTotalDurationSeconds;
            boostThrustProgressNormalised = telemetry.boostThrustProgressNormalised;
            shipShakeNormalised = telemetry.shipShakeNormalised;
            currentLateralVelocity = telemetry.currentLateralVelocity;
            currentLateralForce = telemetry.currentLateralForce;
            currentAngularVelocity = telemetry.currentAngularVelocity;
            currentAngularTorque = telemetry.currentAngularTorque;
            currentLateralVelocityNormalised = telemetry.currentLateralVelocityNormalised;
            currentLateralForceNormalised = telemetry.currentLateralForceNormalised;
            currentAngularVelocityNormalised = telemetry.currentAngularVelocityNormalised;
            currentAngularTorqueNormalised = telemetry.currentAngularTorqueNormalised;
            maxSpeed = telemetry.maxSpeed;
        }

        public override string ToString() {
            return
                $"Fly Dangerous version {gameVersion}, packetId: {packetId}\nGame details: game mode - {currentGameMode}, level - {currentLevelName}, music - {currentMusicTrackName}\nShip details: speed - {shipSpeed}m/s, altitude {shipAltitude} - world position {shipWorldPosition}";
        }
    }

    // byte encoding version of FlyDangerousTelemetry with fixed padded char fields for strings (consistent sized packets)
    [StructLayout(LayoutKind.Sequential)]
    public struct FlyDangerousTelemetryBytes {
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
        public float shipHeightFromGround;
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

        public void SetFromFlyDangerousTelemetry(ref FlyDangerousTelemetry telemetry) {
            // string values
            gameVersion = telemetry.gameVersion.PadRight(20).ToCharArray();
            currentLevelName = telemetry.currentLevelName.PadRight(100).ToCharArray();
            currentGameMode = telemetry.currentGameMode.PadRight(50).ToCharArray();
            currentMusicTrackName = telemetry.currentMusicTrackName.PadRight(100).ToCharArray();
            currentShipName = telemetry.currentShipName.PadRight(50).ToCharArray();
            playerName = telemetry.playerName.PadRight(50).ToCharArray();
            playerFlagIso = telemetry.playerFlagIso.PadRight(50).ToCharArray();

            // safe values
            flyDangerousTelemetryId = telemetry.flyDangerousTelemetryId;
            packetId = telemetry.packetId;

            currentPlayerCount = telemetry.currentPlayerCount;
            shipWorldPosition = telemetry.shipWorldPosition;
            shipAltitude = telemetry.shipAltitude;
            shipHeightFromGround = telemetry.shipHeightFromGround;
            shipSpeed = telemetry.shipSpeed;
            accelerationMagnitudeNormalised = telemetry.accelerationMagnitudeNormalised;
            gForce = telemetry.gForce;
            pitchPosition = telemetry.pitchPosition;
            rollPosition = telemetry.rollPosition;
            yawPosition = telemetry.yawPosition;
            throttlePosition = telemetry.throttlePosition;
            lateralHPosition = telemetry.lateralHPosition;
            lateralVPosition = telemetry.lateralVPosition;
            boostCapacitorPercent = telemetry.boostCapacitorPercent;
            boostTimerReady = telemetry.boostTimerReady;
            boostChargeReady = telemetry.boostChargeReady;
            lightsActive = telemetry.lightsActive;
            velocityLimiterActive = telemetry.velocityLimiterActive;
            vectorFlightAssistActive = telemetry.vectorFlightAssistActive;
            rotationalFlightAssistActive = telemetry.rotationalFlightAssistActive;
            proximityWarning = telemetry.proximityWarning;
            proximityWarningSeconds = telemetry.proximityWarningSeconds;
            collisionThisFrame = telemetry.collisionThisFrame;
            collisionStartedThisFrame = telemetry.collisionStartedThisFrame;
            collisionImpactNormalised = telemetry.collisionImpactNormalised;
            collisionDirection = telemetry.collisionDirection;
            isBoostSpooling = telemetry.isBoostSpooling;
            boostSpoolStartedThisFrame = telemetry.boostSpoolStartedThisFrame;
            isBoostThrustActive = telemetry.isBoostThrustActive;
            boostThrustStartedThisFrame = telemetry.boostThrustStartedThisFrame;
            boostSpoolTotalDurationSeconds = telemetry.boostSpoolTotalDurationSeconds;
            boostThrustTotalDurationSeconds = telemetry.boostThrustTotalDurationSeconds;
            boostThrustProgressNormalised = telemetry.boostThrustProgressNormalised;
            shipShakeNormalised = telemetry.shipShakeNormalised;
            currentLateralVelocity = telemetry.currentLateralVelocity;
            currentLateralForce = telemetry.currentLateralForce;
            currentAngularVelocity = telemetry.currentAngularVelocity;
            currentAngularTorque = telemetry.currentAngularTorque;
            currentLateralVelocityNormalised = telemetry.currentLateralVelocityNormalised;
            currentLateralForceNormalised = telemetry.currentLateralForceNormalised;
            currentAngularVelocityNormalised = telemetry.currentAngularVelocityNormalised;
            currentAngularTorqueNormalised = telemetry.currentAngularTorqueNormalised;
            maxSpeed = telemetry.maxSpeed;
        }

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
        private static FlyDangerousTelemetryBytes _telemetryBytes;

        internal static byte[] EncodePacket(BroadcastFormat broadcastFormat, ref FlyDangerousTelemetry telemetry) {
            byte[] packet;
            switch (broadcastFormat) {
                case BroadcastFormat.Bytes:
                    packet = StructureToByteArray(ref telemetry);
                    break;
                case BroadcastFormat.Json:
                    packet = StructureToJson(ref telemetry);
                    break;
                default:
                    throw new Exception("Failed attempting to output telemetry without target format!");
            }

            return packet;
        }

        private static byte[] StructureToJson(ref FlyDangerousTelemetry telemetry) {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetry, Formatting.Indented));
        }

        private static byte[] StructureToByteArray(ref FlyDangerousTelemetry telemetry) {
            _telemetryBytes.SetFromFlyDangerousTelemetry(ref telemetry);
            if (_arrBytes == null) {
                _sizeBytes = Marshal.SizeOf(_telemetryBytes);
                _arrBytes = new byte[_sizeBytes];
            }

            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(_sizeBytes);
                Marshal.StructureToPtr(_telemetryBytes, ptr, false);
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
        private static FlyDangerousTelemetryBytes _telemetryBytes;

        internal static ref FlyDangerousTelemetry DecodePacket(BroadcastFormat broadcastFormat, byte[] bytes) {
            switch (broadcastFormat) {
                case BroadcastFormat.Bytes:
                    _telemetry = ByteArrayToStructure(bytes);
                    break;
                case BroadcastFormat.Json:
                    _telemetry = JsonStringToStructure(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    break;
            }

            return ref _telemetry;
        }

        private static ref FlyDangerousTelemetry JsonStringToStructure(string json) {
            _telemetry = JsonConvert.DeserializeObject<FlyDangerousTelemetry>(json);
            return ref _telemetry;
        }

        private static ref FlyDangerousTelemetry ByteArrayToStructure(byte[] bytes) {
            var size = Marshal.SizeOf(_telemetryBytes);
            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                _telemetryBytes = (FlyDangerousTelemetryBytes)Marshal.PtrToStructure(ptr, _telemetryBytes.GetType());
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            _telemetry.SetFromFlyDangerousTelemetryBytes(ref _telemetryBytes);
            return ref _telemetry;
        }
    }

    #endregion
}