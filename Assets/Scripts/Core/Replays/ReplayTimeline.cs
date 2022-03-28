using System.IO;
using System.Text;
using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using UnityEngine;

namespace Core.Replays {
    public interface IReplayShip {
        string PlayerName { get; set; }
        Transform Transform { get; }
        Rigidbody Rigidbody { get; }
        ShipPhysics ShipPhysics { get; }
        public void SetAbsolutePosition(Vector3 ghostFloatingOrigin, Vector3 position);
    }

    public class ReplayTimeline : MonoBehaviour {
        private byte[] _inputFrameByteBuffer;

        [CanBeNull] private BinaryReader _inputFrameReader;

        private uint _inputTicks;
        private bool _isPlaying;
        private byte[] _keyFrameByteBuffer;
        [CanBeNull] private BinaryReader _keyFrameReader;
        private uint _keyFrameTicks;

        // private float _playSpeed = 1f;

        [CanBeNull] private Replay _replay;
        [CanBeNull] private IReplayShip _shipReplayObject;

        public void FixedUpdate() {
            if (_isPlaying)
                if (_replay != null && _shipReplayObject != null && _inputFrameReader != null && _keyFrameReader != null) {
                    UpdateKeyFrame();
                    UpdateInputFrame();
                }
        }

        private void OnDestroy() {
            if (_replay != null) {
                _replay.InputFileStream.Close();
                _replay.KeyFrameFileStream.Close();
            }
        }

        public void LoadReplay(IReplayShip ship, Replay replay) {
            _replay = replay;
            _shipReplayObject = ship;
            ship.ShipPhysics.RefreshShipModel(replay.ShipProfile);
            ship.PlayerName = replay.ShipProfile.playerName;

            _inputFrameReader = new BinaryReader(replay.InputFileStream, Encoding.UTF8, true);
            _keyFrameReader = new BinaryReader(replay.KeyFrameFileStream, Encoding.UTF8, true);
            _inputFrameByteBuffer = new byte[replay.ReplayMeta.InputFrameBufferSizeBytes];
            _keyFrameByteBuffer = new byte[replay.ReplayMeta.KeyFrameBufferSizeBytes];

            _inputTicks = 0;
            _keyFrameTicks = 0;
        }

        public void Play() {
            _isPlaying = true;
        }

        public void Pause() {
            _isPlaying = false;
        }

        public void Stop() {
            _inputTicks = 0;
            _isPlaying = false;
        }

        private void UpdateKeyFrame() {
            if (_replay != null && _inputTicks % _replay.ReplayMeta.KeyFrameIntervalTicks == 0 && _shipReplayObject != null) {
                _keyFrameReader?.BaseStream.Seek(_keyFrameTicks * _replay.ReplayMeta.KeyFrameBufferSizeBytes, SeekOrigin.Begin);
                _keyFrameReader?.Read(_keyFrameByteBuffer, 0, _replay.ReplayMeta.KeyFrameBufferSizeBytes);

                var keyFrame = MessagePackSerializer.Deserialize<KeyFrame>(_keyFrameByteBuffer);

                _shipReplayObject.SetAbsolutePosition(keyFrame.replayFloatingOrigin, keyFrame.position);
                _shipReplayObject.Transform.rotation = keyFrame.rotation;
                _shipReplayObject.Rigidbody.velocity = keyFrame.velocity;
                _shipReplayObject.Rigidbody.angularVelocity = keyFrame.angularVelocity;
                _keyFrameTicks++;
            }
        }

        private void UpdateInputFrame() {
            // TODO: This is slow as all hell! We should abstract this and use SeekOrigin.Current in typical ghost run

            // Check for end of file
            if (_replay != null) {
                var maxRead = _inputTicks * _replay.ReplayMeta.InputFrameBufferSizeBytes + _replay.ReplayMeta.InputFrameBufferSizeBytes;
                if (maxRead < _inputFrameReader?.BaseStream.Length) {
                    _inputFrameReader.BaseStream.Seek(_inputTicks * _replay.ReplayMeta.InputFrameBufferSizeBytes, SeekOrigin.Begin);
                    _inputFrameReader.Read(_inputFrameByteBuffer, 0, _replay.ReplayMeta.InputFrameBufferSizeBytes);

                    var inputFrame = MessagePackSerializer.Deserialize<InputFrame>(_inputFrameByteBuffer);
                    _shipReplayObject?.ShipPhysics.UpdateShip(inputFrame.pitch, inputFrame.roll, inputFrame.yaw, inputFrame.throttle, inputFrame.lateralH,
                        inputFrame.lateralV, inputFrame.boostHeld, false, false, false);

                    _inputTicks++;
                }
                else {
                    Debug.Log("Replay finished");
                    Stop();
                }
            }
        }
    }
}