using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;

namespace Core.ShipModel.Feedback.socket {
    // Used to test the output from the UdpServer
    public class UdpTestListener : MonoBehaviour {
        [SerializeField] private int listenPort = 11000;
        [SerializeField] private BroadcastFormat broadcastFormat = BroadcastFormat.Json;
        private IPEndPoint _ipEndPoint;

        private bool _isEnabled;
        private UdpClient _listener;

        private Task<UdpReceiveResult> _receiveTask;
        private FlyDangerousTelemetry _telemetry;

        private void FixedUpdate() {
            if (!_isEnabled) return;

            // Because we're using Unity here and we can't (easily) handle async tasks in our update loops,
            // we're going to treat the async task like a js promise and just output whenever we have a result.
            if (_receiveTask is { IsCompleted: true }) {
                var bytes = _receiveTask.Result.Buffer;
                Debug.Log($"Received broadcast from {_ipEndPoint}, {bytes.Length} bytes:");
                var telemetry = FlyDangerousTelemetryDecoder.DecodePacket(broadcastFormat, bytes);
                Debug.Log(telemetry);
            }

            if (_receiveTask == null || _receiveTask.IsCompleted) {
                Debug.Log("Waiting for broadcast");
                _receiveTask = _listener.ReceiveAsync();
            }
        }

        private void OnDisable() {
            StopListener();
        }

        [Button("Start Listener")]
        [UsedImplicitly]
        private void StartListener() {
            Debug.Log($"Starting {broadcastFormat} UDP Listener (port {listenPort}) ... ");
            _listener = new UdpClient(listenPort);
            _ipEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            _isEnabled = true;
        }

        [Button("Stop Listener")]
        [UsedImplicitly]
        private void StopListener() {
            Debug.Log("Shutting down UDP Listener");
            _listener?.Close();
            _receiveTask = null;
            _isEnabled = false;
        }
    }
}