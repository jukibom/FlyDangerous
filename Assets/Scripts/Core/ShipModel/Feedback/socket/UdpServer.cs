using System.Net;
using System.Net.Sockets;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using JetBrains.Annotations;
using Misc;
using NaughtyAttributes;
using UnityEngine;

namespace Core.ShipModel.Feedback.socket {
    public class UdpServer : Singleton<UdpServer>, IShipMotion, IShipInstruments, IShipFeedback {
        [SerializeField] private BroadcastFormat broadcastFormat = BroadcastFormat.Json;
        [SerializeField] private IPAddress broadcastIpAddress = IPAddress.Parse("127.0.0.1");
        [SerializeField] private int broadcastPort = 11000;
        [SerializeField] private float emitInterval = 0.02f;
        [CanBeNull] private IPEndPoint _ipEndPoint;

        private bool _isEnabled;

        private uint _packetId;
        [CanBeNull] private Socket _socket;

        private FlyDangerousTelemetry _telemetry;

        private void FixedUpdate() {
            if (!_isEnabled) return;
            // if (Game.Instance.LoadedLevelData == null) return;

            _telemetry.version = Application.version;
            _telemetry.packetId = _packetId;
            _telemetry.currentTrackName = Game.Instance.LoadedLevelData.name;
            _telemetry.currentGameMode = Game.Instance.LoadedLevelData.gameType.Name;

            var packet = FlyDangerousTelemetryEncoder.EncodePacket(broadcastFormat, _telemetry);

            // TODO: emit interval implementation
            if (_socket != null && _ipEndPoint != null) _socket.SendTo(packet, _ipEndPoint);

            _packetId++;
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        public void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            _telemetry.velocity = shipInstrumentData.VelocityMagnitude;
        }

        public void OnShipMotionUpdate(IShipMotionData shipMotionData) {
        }

        [Button("Start Server")]
        [UsedImplicitly]
        private void StartListener() {
            Debug.Log($"Starting {broadcastFormat} UDP Server ({broadcastIpAddress}:{broadcastPort}) ... ");
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _ipEndPoint = new IPEndPoint(broadcastIpAddress, broadcastPort);
            _packetId = 0;
            _isEnabled = true;
        }

        [Button("Stop Server")]
        [UsedImplicitly]
        private void StopListener() {
            Debug.Log("Shutting down UDP Server");
            _socket?.Close();
            _socket = null;
            _isEnabled = false;
        }

        private void OnGameSettingsApplied() {
            Debug.Log("TODO: UDP CONFIG FUN TIMES");

            if (_isEnabled) StartListener();
            else StopListener();
        }
    }
}