using System.IO;
using System.Text;
using Core.Player;
using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using UnityEngine;

namespace Core.Replays {
    public class ReplayRecorder : MonoBehaviour {
        private bool _recording;

        private ShipPhysics _targetShip;

        private uint _ticks;

        [CanBeNull] public Replay Replay { get; private set; }

        public uint CurrentTick => _ticks;
        
        private BinaryWriter inputReplayWriter;
        private BinaryWriter keyframeReplayWriter;

        private void OnDestroy() {
            CancelRecording();
        }

        public void StartNewRecording(ShipPhysics targetShip) {
            _targetShip = targetShip;
            _recording = true;
            _ticks = 0;
            _targetShip.OnShipPhysicsUpdated += RecordFrame;
            
            var replay = Replay.CreateNewWritable(Game.Instance.ShipParameters, Game.Instance.LoadedLevelData, ShipProfile.FromPreferences());
            inputReplayWriter = new BinaryWriter(replay.InputFrameStream, Encoding.UTF8, true);
            keyframeReplayWriter = new BinaryWriter(replay.KeyFrameStream, Encoding.UTF8, true);
            Replay = replay;
        }

        public void CancelRecording() {
            StopRecording();
            if (Replay != null) {
                Replay.InputFrameStream?.Close();
                Replay.KeyFrameStream?.Close();
                Replay.InputFrameStream?.Dispose();
                Replay.KeyFrameStream?.Dispose();
                Replay = null;
            }
        }

        public void StopRecording() {
            if (_targetShip != null) _targetShip.OnShipPhysicsUpdated -= RecordFrame;
            
            inputReplayWriter?.Close();
            keyframeReplayWriter?.Close();
            
            inputReplayWriter?.Dispose();
            keyframeReplayWriter?.Dispose();

            inputReplayWriter = null;
            keyframeReplayWriter = null;
            
            _recording = false;
            _ticks = 0;
        }

        /**
        * Record the frame every physics time step
        */
        private void RecordFrame() {
            if (_recording && Replay != null) {
                // record a keyframe every specified amount of ticks
                if (_ticks % Replay.ReplayMeta.KeyFrameIntervalTicks == 0)
                    RecordKeyFrame(new KeyFrameV2 {
                        replayFloatingOrigin = FloatingOrigin.Instance.Origin,
                        position = _targetShip.Position,
                        rotation = _targetShip.Rotation,
                        velocity = _targetShip.Velocity,
                        angularVelocity = _targetShip.AngularVelocity,
                        boostStatus = _targetShip.BoostStatus,
                        boostProgressTicks = (float)_targetShip.BoostProgressTicks,
                        boostTime = _targetShip.CurrentBoostTime,
                        boostCapacitorPercent = _targetShip.BoostCapacitorPercent,
                    });

                RecordInputFrame(new InputFrameV110 {
                    pitch = _targetShip.Pitch,
                    roll = _targetShip.Roll,
                    yaw = _targetShip.Yaw,
                    throttle = _targetShip.Throttle,
                    lateralH = _targetShip.LatH,
                    lateralV = _targetShip.LatV,
                    boostHeld = _targetShip.BoostButtonHeld,
                    limiterHeld = _targetShip.VelocityLimitActive,
                    shipLightsEnabled = _targetShip.IsNightVisionActive,
                    modifierShipForce = _targetShip.AppliedEffects.shipForce,
                    modifierShipDeltaSpeedCap = _targetShip.AppliedEffects.shipDeltaSpeedCap,
                    modifierShipDeltaThrust = _targetShip.AppliedEffects.shipDeltaThrust,
                    modifierShipDrag = _targetShip.AppliedEffects.shipDeltaDrag,
                    modifierShipAngularDrag = _targetShip.AppliedEffects.shipDeltaAngularDrag
                });

                _ticks++;
            }
        }

        private void RecordInputFrame(InputFrameV110 inputFrame) {
            if (Replay is { CanWrite: true }) {
                var inputFrameBytes = MessagePackSerializer.Serialize(inputFrame);
                inputReplayWriter.Write(inputFrameBytes);
            }
        }

        private void RecordKeyFrame(KeyFrameV2 keyFrame) {
            if (Replay is { CanWrite: true }) {
                var keyFrameBytes = MessagePackSerializer.Serialize(keyFrame);
                keyframeReplayWriter.Write(keyFrameBytes);
            }
        }
    }
}