using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Player.HeadTracking {
    public struct HeadTransform {
        public Vector3 position;
        public Vector3 orientationEuler;

        public override string ToString() {
            return $"x: {position.x}, y: {position.y}, z: {position.z}, pitch: {orientationEuler.x}, yaw: {orientationEuler.y}, roll: {orientationEuler.z}";
        }
    }

    public class HeadTrackingManager : MonoBehaviour {
        private readonly IPAddress _listenIpAddress = IPAddress.Parse("127.0.0.1");
        private readonly int _listenPort = 4242;
        private HeadTransform _headTransform;

        private IPEndPoint _ipEndPoint;

        private UdpClient _listener;
        private OpenTrackData _openTrackData;
        private long _packetId;

        private Task<UdpReceiveResult> _receiveTask;

        public bool IsOpenTrackEnabled { get; private set; }

        public ref HeadTransform HeadTransform => ref _headTransform;

        private void FixedUpdate() {
            if (!IsOpenTrackEnabled) return;

            // OpenTrack Head Position
            _openTrackData.ReceiveOpenTrackDataAsync(data => {
                // position is in cm in flipped in X and Z space, convert
                // max magnitude 1m in any direction
                _headTransform.position = Vector3.ClampMagnitude(new Vector3((float)-data.x / 100, (float)data.y / 100, (float)-data.z / 100), 1);
                // orientation convert to vec 3 euler
                _headTransform.orientationEuler = new Vector3((float)data.pitch, (float)data.yaw, -(float)data.roll);
            }, _listenPort, 1000);

            // Add any other tracking methods here
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void OnGameSettingsApplied() {
            IsOpenTrackEnabled = Preferences.Instance.GetBool("openTrackEnabled");
            if (!IsOpenTrackEnabled) _openTrackData.Reset();
        }
    }
}