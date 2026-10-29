using System;
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
        public bool SpectatorActive { get; set; }
        public void SetAbsolutePosition(Vector3 ghostFloatingOrigin, Vector3 position);
    }

    public class ReplayTimeline : MonoBehaviour {
        private byte[] _inputFrameByteBuffer;

        [CanBeNull] private BinaryReader _inputFrameReader;
        private ShipPhysics shipPhysics {get; set; }

        private uint _inputTicks;
        private bool _isPlaying;
        private bool _isPlayer;
        private byte[] _keyFrameByteBuffer;
        [CanBeNull] private BinaryReader _keyFrameReader;

        // private float _playSpeed = 1f;

        [CanBeNull] public Replay Replay { get; private set; }
        [CanBeNull] public IReplayShip ShipReplayObject { get; private set; }

        public void UpdateReplay() {
            if (_isPlaying)
                if (Replay != null && (ShipReplayObject != null || _isPlayer) && _inputFrameReader != null && _keyFrameReader != null) {
                    UpdateKeyFrame();
                    UpdateInputFrame();
                }
        }

        private void OnEnable() {
            ReplayPrioritizer.Instance.RegisterReplay(this);
        }

        private void OnDisable() {
            ReplayPrioritizer.Instance.UnregisterReplay(this);
        }

        private void OnDestroy() {
            Stop();
        }

        public void LoadReplay(IReplayShip ship, Replay replay) {
            Replay = replay;
            ShipReplayObject = ship;
            shipPhysics = ship.ShipPhysics;
            shipPhysics.ShipProfile = replay.ShipProfile;
            shipPhysics.FlightParameters = replay.ShipParameters;

            // hide all rendering assets until told to show (e.g. by distance in FixedUpdate)
            if (shipPhysics.ShipModel != null) shipPhysics.ShipModel.SetVisible(false);

            ship.PlayerName = replay.ShipProfile.playerName;
            ship.PlayerFlag = Flag.FromFilename(replay.ShipProfile.playerFlagFilename);

            _inputFrameReader = new BinaryReader(replay.InputFrameStream, Encoding.UTF8, true);
            _keyFrameReader = new BinaryReader(replay.KeyFrameStream, Encoding.UTF8, true);
            _inputFrameByteBuffer = new byte[replay.ReplayMeta.InputFrameBufferSizeBytes];
            _keyFrameByteBuffer = new byte[replay.ReplayMeta.KeyFrameBufferSizeBytes];

            _isPlayer = false;
            _inputTicks = 0;
        }

        public void LoadPlayerReplay(Replay replay, uint tick) {
            Replay = replay;
            shipPhysics = FdPlayer.FindLocalShipPlayer.ShipPhysics;

            _inputFrameReader = new BinaryReader(replay.InputFrameStream, Encoding.UTF8, true);
            _keyFrameReader = new BinaryReader(replay.KeyFrameStream, Encoding.UTF8, true);
            _inputFrameByteBuffer = new byte[replay.ReplayMeta.InputFrameBufferSizeBytes];
            _keyFrameByteBuffer = new byte[replay.ReplayMeta.KeyFrameBufferSizeBytes];

            _isPlayer = true;
            _inputTicks = tick;
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
                _keyFrameReader?.BaseStream.Seek(_inputTicks/this.Replay.ReplayMeta.KeyFrameIntervalTicks * Replay.ReplayMeta.KeyFrameBufferSizeBytes, SeekOrigin.Begin);
                _keyFrameReader?.Read(_keyFrameByteBuffer, 0, Replay.ReplayMeta.KeyFrameBufferSizeBytes);

                var keyFrame = MessagePackSerializer.Deserialize<KeyFrame>(_keyFrameByteBuffer);

                if(_isPlayer)
                    FdPlayer.FindLocalShipPlayer.SetTransformWorld(keyFrame.position+keyFrame.replayFloatingOrigin, keyFrame.rotation);
                else
                    ShipReplayObject.SetAbsolutePosition(keyFrame.replayFloatingOrigin, keyFrame.position);
                shipPhysics.transform.rotation = keyFrame.rotation;
                shipPhysics.Rigidbody.linearVelocity = keyFrame.velocity;
                shipPhysics.Rigidbody.angularVelocity = keyFrame.angularVelocity;
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

                    shipPhysics.OverwriteModifiers(inputFrame.modifierShipForce, inputFrame.modifierShipDeltaSpeedCap,
                        inputFrame.modifierShipDeltaThrust, inputFrame.modifierShipDrag, inputFrame.modifierShipAngularDrag);

                    shipPhysics.UpdateShip(inputFrame.pitch, inputFrame.roll, inputFrame.yaw, inputFrame.throttle, inputFrame.lateralH,
                        inputFrame.lateralV, inputFrame.boostHeld, inputFrame.limiterHeld, false, false);

                    if (shipPhysics.IsNightVisionActive != inputFrame.shipLightsEnabled)
                        shipPhysics.NightVisionToggle(inputFrame.shipLightsEnabled, _ => { });


                    if (!_isPlayer && ShipReplayObject?.SpectatorActive == true ) {
                        shipPhysics.GhostCollisionChecks();
                        var velocity = transform.InverseTransformDirection(shipPhysics.Velocity);
                        var frameThrust = shipPhysics.CurrentFrameThrust;
                        FdPlayer.FindLocalShipPlayer.User.ShipCameraRig.UpdateCameras(velocity, shipPhysics.FlightParameters.maxSpeed, frameThrust, shipPhysics.FlightParameters.maxThrust);
                    }

                    _inputTicks++;
                }
                else {
                    Debug.Log("Replay finished");

                    // last collition check to see if it hit the final checkpoint
                    if (ShipReplayObject?.SpectatorActive == true )
                        ShipReplayObject.ShipPhysics.GhostCollisionChecks();

                    Stop();
                }
            }
        }
    }
}