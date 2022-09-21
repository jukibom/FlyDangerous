using System.Net;
using System.Net.Sockets;
using Core.Player;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using JetBrains.Annotations;
using Misc;
using NaughtyAttributes;
using UnityEngine;

namespace Core.ShipModel.Feedback.simRacingStudio {
    public class SimRacingStudio : Singleton<SimRacingStudio>, IShipMotion, IShipInstruments {
        private IPAddress _broadcastIpAddress = IPAddress.Parse("127.0.0.1");
        private int _broadcastPort = 11000;
        [CanBeNull] private IPEndPoint _ipEndPoint;

        private bool _isEnabled;

        private SimRacingStudioData _simRacingStudioData;
        [CanBeNull] private Socket _socket;

        private void FixedUpdate() {
            if (!_isEnabled) return;
            if (!Game.Instance.InGame) return;

            // Add missing / global data to telemetry
            // Meta
            _simRacingStudioData.version = 102;
            _simRacingStudioData.apiMode = "api".ToCharArray();
            _simRacingStudioData.game = "Fly Dangerous".PadRight(50).ToCharArray();

            // Game State
            var player = FdPlayer.FindLocalShipPlayer;
            _simRacingStudioData.location =
                (Game.Instance.LoadedLevelData.name != "" ? Game.Instance.LoadedLevelData.name : "None").PadRight(50).ToCharArray();
            if (player != null) _simRacingStudioData.vehicleName = (player.ShipPhysics.ShipProfile?.shipModel ?? "None").PadRight(50).ToCharArray();

            // Serialise and send
            var packet = SimRacingStudioDataEncoder.EncodePacket(_simRacingStudioData);
            if (_socket != null && _ipEndPoint != null) _socket.SendTo(packet, _ipEndPoint);
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            _simRacingStudioData.speed = shipInstrumentData.Speed / 3.6f;
            _simRacingStudioData.maxRpm = 30000;
            _simRacingStudioData.rpm = shipInstrumentData.AccelerationMagnitudeNormalised * 10000;
            _simRacingStudioData.gear = shipInstrumentData.ThrottlePositionNormalised > 0 ? 1 : -1;
        }

        public void OnShipMotionUpdate(IShipMotionData shipMotionData) {
            _simRacingStudioData.lateralAcceleration = shipMotionData.CurrentLateralForceNormalised.x * 10;
            _simRacingStudioData.verticalAcceleration = shipMotionData.CurrentLateralForceNormalised.y * 10;
            _simRacingStudioData.longitudinalAcceleration = shipMotionData.CurrentLateralForceNormalised.z * 10;

            _simRacingStudioData.pitch = SimRacingStudioDataEncoder.MapAngleToSrs(shipMotionData.WorldRotationEuler.x);
            _simRacingStudioData.yaw = SimRacingStudioDataEncoder.MapAngleToSrs(shipMotionData.WorldRotationEuler.y);
            _simRacingStudioData.roll = SimRacingStudioDataEncoder.MapAngleToSrs(shipMotionData.WorldRotationEuler.z);
        }

        [Button("Start Server")]
        [UsedImplicitly]
        private void StartServer() {
            Debug.Log($"Starting Sim Racing Studio Server ({_broadcastIpAddress}:{_broadcastPort}) ... ");
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _ipEndPoint = new IPEndPoint(_broadcastIpAddress, _broadcastPort);
            _isEnabled = true;
        }

        [Button("Stop Server")]
        [UsedImplicitly]
        private void StopServer() {
            Debug.Log("Shutting down UDP Server");
            _socket?.Close();
            _socket = null;
            _isEnabled = false;
        }

        private void OnGameSettingsApplied() {
            _isEnabled = Preferences.Instance.GetBool("simRacingStudioEnabled");
            _broadcastIpAddress = IPAddress.Parse(Preferences.Instance.GetString("simRacingStudioOutputAddress"));
            _broadcastPort = (int)Preferences.Instance.GetFloat("simRacingStudioOutputPort");

            if (_isEnabled) StartServer();
            else StopServer();
        }
    }
}