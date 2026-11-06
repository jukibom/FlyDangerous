using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.Player;
using Core.ShipModel;
using Gameplay.Game_Modes;
using JetBrains.Annotations;
using MessagePack;
using Mono.Cecil;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        private ShipPhysics ShipPhysics {get; set; }

        private uint _inputTicks;
        private bool _isPlaying;
        private bool _isPlayer;
        private byte[] _keyFrameByteBuffer;
        private List<float> _replaySplits;
        private List<float> _playerSplits;
        [CanBeNull] private BinaryReader _keyFrameReader;

        // private float _playSpeed = 1f;

        [CanBeNull] public Replay Replay { get; private set; }
        [CanBeNull] public IReplayShip ShipReplayObject { get; private set; }

        public void UpdateReplay() {
            if (_isPlaying)
                if (Replay != null && (ShipReplayObject != null || _isPlayer) && _inputFrameReader != null && _keyFrameReader != null) {
                    UpdateKeyFrame();
                    UpdateInputFrame();
                    CheckSplits();
                }
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
            ShipPhysics = ship.ShipPhysics;
            ShipPhysics.ShipProfile = replay.ShipProfile;
            ShipPhysics.FlightParameters = replay.ShipParameters;

            // hide all rendering assets until told to show (e.g. by distance in FixedUpdate)
            if (ShipPhysics.ShipModel != null) ShipPhysics.ShipModel.SetVisible(false);

            ship.PlayerName = replay.ShipProfile.playerName;
            ship.PlayerFlag = Flag.FromFilename(replay.ShipProfile.playerFlagFilename);

            _inputFrameReader = new BinaryReader(replay.InputFrameStream, Encoding.UTF8, true);
            _keyFrameReader = new BinaryReader(replay.KeyFrameStream, Encoding.UTF8, true);
            _inputFrameByteBuffer = new byte[replay.ReplayMeta.InputFrameBufferSizeBytes];
            _keyFrameByteBuffer = new byte[replay.ReplayMeta.KeyFrameBufferSizeBytes];

            _isPlayer = false;
            _inputTicks = 0;

            if (Game.Instance.GameModeHandler.GameMode is TimeTrialSprint sprint) {
                _replaySplits = replay.ScoreData.splits;
                if(sprint.GameModeScore.CurrentBestSplits.Count != 0)
                    _playerSplits = sprint.GameModeScore.CurrentBestSplits;
                else _playerSplits = _playerSplits = Enumerable.Range(0, _replaySplits.Count).Select(i => 0f).ToList();
            }
        }

        public void LoadPlayerReplay(Replay replay, uint tick) {
            Replay = replay;
            ShipPhysics = FdPlayer.FindLocalShipPlayer?.ShipPhysics;

            _inputFrameReader = new BinaryReader(replay.InputFrameStream, Encoding.UTF8, true);
            _keyFrameReader = new BinaryReader(replay.KeyFrameStream, Encoding.UTF8, true);
            _inputFrameByteBuffer = new byte[replay.ReplayMeta.InputFrameBufferSizeBytes];
            _keyFrameByteBuffer = new byte[replay.ReplayMeta.KeyFrameBufferSizeBytes];

            _isPlayer = true;
            _inputTicks = tick;
        }

        public void Play() {
            _isPlaying = true;
            ReplayPrioritizer.Instance.RegisterReplay(this);
        }

        public void Pause() {
            _isPlaying = false;
        }

        public void Stop() {
            _inputTicks = 0;
            _isPlaying = false;
            ShipReplayObject?.ShipPhysics.BringToStop();
        }

        private void CheckSplits() {
            if (!_isPlayer && ShipReplayObject.SpectatorActive && Game.Instance.GameModeHandler.GameMode is TimeTrialSprint sprint) 
                for (int i = 0; i < _replaySplits.Count; i++)
                    if(_replaySplits[i] >= sprint.GameModeTimer.CurrentSessionTimeSeconds && _replaySplits[i] < sprint.GameModeTimer.CurrentSessionTimeSeconds + Time.deltaTime)
                        sprint.DisplaySplitTimer(_replaySplits[i],_playerSplits[i]);   
        }

        private void UpdateKeyFrame() {
            if (Replay != null && _inputTicks % Replay.ReplayMeta.KeyFrameIntervalTicks == 0 && ShipReplayObject != null) {
                _keyFrameReader?.BaseStream.Seek(_inputTicks/this.Replay.ReplayMeta.KeyFrameIntervalTicks * Replay.ReplayMeta.KeyFrameBufferSizeBytes, SeekOrigin.Begin);
                _keyFrameReader?.Read(_keyFrameByteBuffer, 0, Replay.ReplayMeta.KeyFrameBufferSizeBytes);

                var keyFrame = KeyFrameV2.Deserialize(Replay.ReplayMeta.Version, ref _keyFrameByteBuffer);

                if (_isPlayer) {
                    FdPlayer.LocalShipPlayer.SetTransformWorld(keyFrame.position + keyFrame.replayFloatingOrigin,
                        keyFrame.rotation);
                    FdPlayer.LocalShipPlayer.transform.rotation = keyFrame.rotation;
                }
                else {
                    ShipReplayObject.SetAbsolutePosition(keyFrame.replayFloatingOrigin, keyFrame.position);
                    ShipReplayObject.Transform.rotation = keyFrame.rotation;
                }
                ShipPhysics.Rigidbody.linearVelocity = keyFrame.velocity;
                ShipPhysics.Rigidbody.angularVelocity = keyFrame.angularVelocity;
                if (Replay.ReplayMeta.Version == "1.1.1") {
                    ShipPhysics.BoostStatus = keyFrame.boostStatus;
                    ShipPhysics.BoostProgressTicks = (int)keyFrame.boostProgressTicks;
                    ShipPhysics.CurrentBoostTime = keyFrame.boostTime;
                    ShipPhysics.BoostCapacitorPercent = keyFrame.boostCapacitorPercent;
                }
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

                    ShipPhysics.OverwriteModifiers(inputFrame.modifierShipForce, inputFrame.modifierShipDeltaSpeedCap,
                        inputFrame.modifierShipDeltaThrust, inputFrame.modifierShipDrag, inputFrame.modifierShipAngularDrag);

                    ShipPhysics.UpdateShip(inputFrame.pitch, inputFrame.roll, inputFrame.yaw, inputFrame.throttle, inputFrame.lateralH,
                        inputFrame.lateralV, inputFrame.boostHeld, inputFrame.limiterHeld, false, false);

                    if (ShipPhysics.IsNightVisionActive != inputFrame.shipLightsEnabled)
                        ShipPhysics.NightVisionToggle(inputFrame.shipLightsEnabled, _ => { });

                    if (!_isPlayer && ShipReplayObject?.SpectatorActive == true ) {
                        var velocity = transform.InverseTransformDirection(ShipPhysics.Velocity);
                        var frameThrust = ShipPhysics.CurrentFrameThrust;
                        FdPlayer.LocalShipPlayer.User.ShipCameraRig.UpdateCameras(velocity, ShipPhysics.FlightParameters.maxSpeed, frameThrust, ShipPhysics.FlightParameters.maxThrust);
                    }

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