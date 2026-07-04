using System;
using System.IO;
using System.Text;
using Core.Player;
using Core.ShipModel;
using Gameplay.Game_Modes;
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

        private bool _isPlaying;
        private byte[] _keyFrameByteBuffer;
        [CanBeNull] private BinaryReader _keyFrameReader;

        // private float _playSpeed = 1f;

        [CanBeNull] public Replay Replay { get; private set; }
        [CanBeNull] public IReplayShip ShipReplayObject { get; private set; }

        public void UpdateReplay() {
            if (_isPlaying)
                if (Replay != null && ShipReplayObject != null && _inputFrameReader != null && _keyFrameReader != null) {
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

        public uint inputTicks;
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

            inputTicks = 0;
        }

        public void Play() {
            _isPlaying = true;
        }

        public void Pause() {
            _isPlaying = false;
        }

        public void Stop() {
            inputTicks = 0;
            _isPlaying = false;
            ShipReplayObject?.ShipPhysics.BringToStop();
        }
        private ShipPlayer ShipPlayer => ShipPlayer.FindLocalShipPlayer;
        private void UpdateKeyFrame() {
            if (Replay != null && inputTicks % Replay.ReplayMeta.KeyFrameIntervalTicks == 0 && ShipReplayObject != null) {
                _keyFrameReader?.BaseStream.Seek(inputTicks/this.Replay.ReplayMeta.KeyFrameIntervalTicks * Replay.ReplayMeta.KeyFrameBufferSizeBytes, SeekOrigin.Begin);
                _keyFrameReader?.Read(_keyFrameByteBuffer, 0, Replay.ReplayMeta.KeyFrameBufferSizeBytes);

                //var keyFrame = MessagePackSerializer.Deserialize<KeyFrame>(_keyFrameByteBuffer);
                var keyFrame = KeyFrameV2.Deserialize(Replay.ReplayMeta.Version, ref _keyFrameByteBuffer);

                ShipReplayObject.SetAbsolutePosition(keyFrame.replayFloatingOrigin, keyFrame.position);
                ShipReplayObject.Transform.rotation = keyFrame.rotation;
                ShipReplayObject.Rigidbody.velocity = keyFrame.velocity;
                ShipReplayObject.Rigidbody.angularVelocity = keyFrame.angularVelocity;

                if (Replay.ReplayMeta.Version == "1.1.1") {
                    ShipReplayObject.ShipPhysics._boostStatus = keyFrame.boostStatus;
                    ShipReplayObject.ShipPhysics._boostProgressTicks = (int)keyFrame.boostProgressTicks;
                    ShipReplayObject.ShipPhysics._currentBoostTime = keyFrame.boostTime;
                    ShipReplayObject.ShipPhysics._boostCapacitorPercent = keyFrame.boostCapacitorPercent;
                }
            }
        }

        private void UpdateInputFrame() {
            if (Replay != null) {
                // Check for end of file
                var maxRead = inputTicks * Replay.ReplayMeta.InputFrameBufferSizeBytes + Replay.ReplayMeta.InputFrameBufferSizeBytes;
                if (maxRead < _inputFrameReader?.BaseStream.Length) {
                    _inputFrameReader.BaseStream.Seek(inputTicks * Replay.ReplayMeta.InputFrameBufferSizeBytes, SeekOrigin.Begin);
                    _inputFrameReader.Read(_inputFrameByteBuffer, 0, Replay.ReplayMeta.InputFrameBufferSizeBytes);

                    var inputFrame = InputFrameV110.Deserialize(Replay.Version, ref _inputFrameByteBuffer);

                    ShipReplayObject?.ShipPhysics.OverwriteModifiers(inputFrame.modifierShipForce, inputFrame.modifierShipDeltaSpeedCap,
                        inputFrame.modifierShipDeltaThrust, inputFrame.modifierShipDrag, inputFrame.modifierShipAngularDrag);

                    ShipReplayObject?.ShipPhysics.UpdateShip(inputFrame.pitch, inputFrame.roll, inputFrame.yaw, inputFrame.throttle, inputFrame.lateralH,
                        inputFrame.lateralV, inputFrame.boostHeld, inputFrame.limiterHeld, false, false);

                    if (ShipReplayObject != null && ShipReplayObject.SpectatorActive) {
                        //This helps 
                        ShipReplayObject.ShipPhysics.ghostCollisionChecks();
                        Game.Instance.GameModeHandler.ReplayRecorder.WriteFrame(ShipReplayObject.ShipPhysics);
                        Game.Instance.GameModeHandler._shipSnapshotBuffer.Add(ShipReplayObject.ShipPhysics.GenerateShipSnapShot());
                    }

                    if (ShipReplayObject?.ShipPhysics.IsNightVisionActive != inputFrame.shipLightsEnabled)
                        ShipReplayObject?.ShipPhysics.NightVisionToggle(inputFrame.shipLightsEnabled, _ => { });

                    inputTicks++;
                }
                else {
                    Debug.Log("Replay finished");
                    Stop();
                }
            }
        }
    }
}