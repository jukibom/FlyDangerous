using System.IO;
using System.Text;
using Core.Player;
using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using UnityEngine;

namespace Core.Replays {
    public interface IReplayShip {
        string PlayerName { get; set; }
        Flag PlayerFlag { get; set; }
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

        [CanBeNull] public Replay Replay { get; private set; }
        [CanBeNull] public IReplayShip ShipReplayObject { get; private set; }

        public void FixedUpdate() {
            if (_isPlaying)
                if (Replay != null && ShipReplayObject != null && _inputFrameReader != null && _keyFrameReader != null) {
                    UpdateKeyFrame();
                    UpdateInputFrame();
                }
        }

        private void OnDestroy() {
            Stop();
        }

        public void LoadReplay(IReplayShip ship, Replay replay) {
            Replay = replay;
            ShipReplayObject = ship;
            ship.ShipPhysics.ShipProfile = replay.ShipProfile;
            ship.ShipPhysics.FlightParameters = replay.ShipParameters;

            // hide all rendering assets until told to show (e.g. by distance in FixedUpdate)
            if (ship.ShipPhysics.ShipModel != null) ship.ShipPhysics.ShipModel.SetVisible(false);

            ship.PlayerName = replay.ShipProfile.playerName;
            ship.PlayerFlag = Flag.FromFilename(replay.ShipProfile.playerFlagFilename);

            _inputFrameReader = new BinaryReader(replay.InputFrameStream, Encoding.UTF8, true);
            _keyFrameReader = new BinaryReader(replay.KeyFrameStream, Encoding.UTF8, true);
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
            ShipReplayObject?.ShipPhysics.BringToStop();
        }

        private void UpdateKeyFrame() {
            if (Replay != null && _inputTicks % Replay.ReplayMeta.KeyFrameIntervalTicks == 0 && ShipReplayObject != null) {
                _keyFrameReader?.BaseStream.Seek(_keyFrameTicks * Replay.ReplayMeta.KeyFrameBufferSizeBytes, SeekOrigin.Begin);
                _keyFrameReader?.Read(_keyFrameByteBuffer, 0, Replay.ReplayMeta.KeyFrameBufferSizeBytes);

                var keyFrame = MessagePackSerializer.Deserialize<KeyFrame>(_keyFrameByteBuffer);

                ShipReplayObject.SetAbsolutePosition(keyFrame.replayFloatingOrigin, keyFrame.position);
                ShipReplayObject.Transform.rotation = keyFrame.rotation;
                ShipReplayObject.Rigidbody.velocity = keyFrame.velocity;
                ShipReplayObject.Rigidbody.angularVelocity = keyFrame.angularVelocity;


                _keyFrameTicks++;
            }
        }

        private void UpdateInputFrame() {
            if (Replay != null) {
                // Check for end of file
                var maxRead = _inputTicks * Replay.ReplayMeta.InputFrameBufferSizeBytes + Replay.ReplayMeta.InputFrameBufferSizeBytes;
                if (maxRead < _inputFrameReader?.BaseStream.Length) {
                    _inputFrameReader.BaseStream.Seek(_inputTicks * Replay.ReplayMeta.InputFrameBufferSizeBytes, SeekOrigin.Begin);
                    _inputFrameReader.Read(_inputFrameByteBuffer, 0, Replay.ReplayMeta.InputFrameBufferSizeBytes);

                    var inputFrame = InputFrameV110.Deserialize(Replay.Version, ref _inputFrameByteBuffer);

                    ShipReplayObject?.ShipPhysics.UpdateShip(inputFrame.pitch, inputFrame.roll, inputFrame.yaw, inputFrame.throttle, inputFrame.lateralH,
                        inputFrame.lateralV, inputFrame.boostHeld, inputFrame.limiterHeld, false, false);

                    if (ShipReplayObject?.ShipPhysics.IsNightVisionActive != inputFrame.shipLightsEnabled)
                        ShipReplayObject?.ShipPhysics.NightVisionToggle(inputFrame.shipLightsEnabled, _ => { });

                    ShipReplayObject?.ShipPhysics.OverwriteModifiers(inputFrame.modifierShipForce, inputFrame.modifierShipDeltaSpeedCap,
                        inputFrame.modifierShipDeltaThrust, inputFrame.modifierShipDrag, inputFrame.modifierShipAngularDrag);

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