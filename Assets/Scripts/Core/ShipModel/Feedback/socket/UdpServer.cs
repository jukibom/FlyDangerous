using System.Net;
using System.Net.Sockets;
using Audio;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using JetBrains.Annotations;
using Misc;
using NaughtyAttributes;
using UnityEngine;

namespace Core.ShipModel.Feedback.socket {
    public class UdpServer : Singleton<UdpServer>, IShipMotion, IShipInstruments, IShipFeedback {
        [SerializeField] private BroadcastFormat broadcastFormat = BroadcastFormat.Json;
        [SerializeField] private float emitIntervalSeconds = 0.02f;
        private IPAddress _broadcastIpAddress = IPAddress.Parse("127.0.0.1");
        private int _broadcastPort = 11000;
        private float _emitTimer;
        [CanBeNull] private IPEndPoint _ipEndPoint;

        private bool _isEnabled;

        private uint _packetId;
        [CanBeNull] private Socket _socket;

        private FlyDangerousTelemetry _telemetry;

        private void FixedUpdate() {
            if (!_isEnabled) return;

            if (_emitTimer >= emitIntervalSeconds) {
                _emitTimer = 0;

                // Add missing / global data to telemetry
                // Meta
                _telemetry.flyDangerousTelemetryId = 1;
                _telemetry.packetId = _packetId;

                // Game State
                _telemetry.gameVersion = Application.version.PadRight(20).ToCharArray();
                _telemetry.currentLevelName =
                    (Game.Instance.LoadedLevelData.name != "" ? Game.Instance.LoadedLevelData.name : "None").PadRight(50).ToCharArray();
                _telemetry.currentGameMode = Game.Instance.LoadedLevelData.gameType.Name.PadRight(50).ToCharArray();
                _telemetry.currentMusicTrackName = (MusicManager.Instance.CurrentPlayingTrack?.Name ?? "None").PadRight(50).ToCharArray();
                _telemetry.currentPlayerCount = FdNetworkManager.Instance.numPlayers;

                // Serialise and send
                var packet = FlyDangerousTelemetryEncoder.EncodePacket(broadcastFormat, _telemetry);
                if (_socket != null && _ipEndPoint != null) _socket.SendTo(packet, _ipEndPoint);
                _packetId++;
            }

            _emitTimer += Time.fixedDeltaTime;
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        public void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
            if (!_isEnabled) return;

            _telemetry.collisionThisFrame = shipFeedbackData.CollisionThisFrame;
            _telemetry.collisionStartedThisFrame = shipFeedbackData.CollisionStartedThisFrame;
            _telemetry.collisionImpactNormalised = shipFeedbackData.CollisionImpactNormalised;
            _telemetry.collisionDirection = SerializableVector3.AssignOrCreateFromVector3(_telemetry.collisionDirection, shipFeedbackData.CollisionDirection);
            _telemetry.isBoostSpooling = shipFeedbackData.IsBoostSpooling;
            _telemetry.boostSpoolStartedThisFrame = shipFeedbackData.BoostSpoolStartThisFrame;
            _telemetry.isBoostThrustActive = shipFeedbackData.IsBoostThrustActive;
            _telemetry.boostThrustStartedThisFrame = shipFeedbackData.BoostThrustStartThisFrame;
            _telemetry.boostSpoolTotalDurationSeconds = shipFeedbackData.BoostSpoolTotalDurationSeconds;
            _telemetry.boostThrustTotalDurationSeconds = shipFeedbackData.BoostThrustTotalDurationSeconds;
            _telemetry.boostThrustProgressNormalised = shipFeedbackData.BoostThrustProgressNormalised;
            _telemetry.shipShakeNormalised = shipFeedbackData.ShipShakeNormalised;
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            if (!_isEnabled) return;

            _telemetry.shipWorldPosition = SerializableVector3.AssignOrCreateFromVector3(_telemetry.shipWorldPosition, shipInstrumentData.WorldPosition);
            _telemetry.shipAltitude = shipInstrumentData.Altitude;
            _telemetry.shipSpeed = shipInstrumentData.Speed;
            _telemetry.accelerationMagnitudeNormalised = shipInstrumentData.AccelerationMagnitudeNormalised;
            _telemetry.gForce = shipInstrumentData.GForce;
            _telemetry.pitchPosition = shipInstrumentData.PitchPositionNormalised;
            _telemetry.rollPosition = shipInstrumentData.RollPositionNormalised;
            _telemetry.yawPosition = shipInstrumentData.YawPositionNormalised;
            _telemetry.throttlePosition = shipInstrumentData.ThrottlePositionNormalised;
            _telemetry.lateralHPosition = shipInstrumentData.LateralHPositionNormalised;
            _telemetry.lateralVPosition = shipInstrumentData.LateralVPositionNormalised;
            _telemetry.boostCapacitorPercent = shipInstrumentData.BoostCapacitorPercent;
            _telemetry.boostTimerReady = shipInstrumentData.BoostTimerReady;
            _telemetry.boostChargeReady = shipInstrumentData.BoostChargeReady;
            _telemetry.lightsActive = shipInstrumentData.LightsActive;
            _telemetry.velocityLimiterActive = shipInstrumentData.VelocityLimiterActive;
            _telemetry.vectorFlightAssistActive = shipInstrumentData.VectorFlightAssistActive;
            _telemetry.rotationalFlightAssistActive = shipInstrumentData.RotationalFlightAssistActive;
            _telemetry.proximityWarning = shipInstrumentData.ProximityWarning;
            _telemetry.proximityWarningSeconds = shipInstrumentData.ProximityWarningSeconds;
        }

        public void OnShipMotionUpdate(IShipMotionData shipMotionData) {
            if (!_isEnabled) return;

            _telemetry.currentLateralVelocity =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentLateralVelocity, shipMotionData.CurrentLateralVelocity);
            _telemetry.currentLateralForce =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentLateralForce, shipMotionData.CurrentLateralForce);
            _telemetry.currentAngularVelocity =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentAngularVelocity, shipMotionData.CurrentAngularVelocity);
            _telemetry.currentAngularTorque =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentAngularTorque, shipMotionData.CurrentAngularTorque);
            _telemetry.currentLateralVelocityNormalised =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentLateralVelocityNormalised, shipMotionData.CurrentLateralVelocityNormalised);
            _telemetry.currentLateralForceNormalised =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentLateralForceNormalised, shipMotionData.CurrentLateralForceNormalised);
            _telemetry.currentAngularVelocityNormalised =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentAngularVelocityNormalised, shipMotionData.CurrentAngularVelocityNormalised);
            _telemetry.currentAngularTorqueNormalised =
                SerializableVector3.AssignOrCreateFromVector3(_telemetry.currentAngularTorqueNormalised, shipMotionData.CurrentAngularTorqueNormalised);
            _telemetry.maxSpeed = shipMotionData.MaxSpeed;
        }

        [Button("Start Server")]
        [UsedImplicitly]
        private void StartServer() {
            Debug.Log($"Starting {broadcastFormat} UDP Server ({_broadcastIpAddress}:{_broadcastPort}) ... ");
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _ipEndPoint = new IPEndPoint(_broadcastIpAddress, _broadcastPort);
            _isEnabled = true;
        }

        [Button("Stop Server")]
        [UsedImplicitly]
        private void StopServer() {
            Debug.Log("Shutting down UDP Server");
            _packetId = 0;
            _socket?.Close();
            _socket = null;
            _isEnabled = false;
        }

        private void OnGameSettingsApplied() {
            _isEnabled = Preferences.Instance.GetBool("telemetryEnabled");
            var mode = Preferences.Instance.GetString("telemetryOutputMode");
            _broadcastIpAddress = IPAddress.Parse(Preferences.Instance.GetString("telemetryOutputAddress"));
            _broadcastPort = (int)Preferences.Instance.GetFloat("telemetryOutputPort");

            switch (mode) {
                case "bytes":
                    broadcastFormat = BroadcastFormat.Bytes;
                    break;
                case "json":
                    broadcastFormat = BroadcastFormat.Json;
                    break;
                default:
                    Debug.LogWarning("Telemetry enabled but 'telemetryOutputMode' setting is not recognised.");
                    _isEnabled = false;
                    return;
            }

            _emitTimer = Preferences.Instance.GetFloat("telemetryEmitIntervalSeconds");

            if (_isEnabled) StartServer();
            else StopServer();
        }
    }
}