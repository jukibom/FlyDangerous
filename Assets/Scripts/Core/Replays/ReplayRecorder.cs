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
            WriteFrame(_targetShip);
        }

        public void WriteFrame( ShipPhysics targetShip) {
            if (_recording && Replay != null) {
                // record a keyframe every specified amount of ticks
                if (_ticks % Replay.ReplayMeta.KeyFrameIntervalTicks == 0)
                    RecordKeyFrame(new KeyFrameV2 {
                        replayFloatingOrigin = FloatingOrigin.Instance.Origin,
                        position = targetShip.Position,
                        rotation = targetShip.Rotation,
                        velocity = targetShip.Velocity,
                        angularVelocity = targetShip.AngularVelocity,
                        boostStatus = targetShip.BoostStatus,
                        boostProgressTicks = (float)targetShip.BoostProgressTicks,
                        boostTime = targetShip.CurrentBoostTime,
                        boostCapacitorPercent = targetShip.BoostCapacitorPercent,
                    });

                RecordInputFrame(new InputFrameV110 {
                    pitch = targetShip.Pitch,
                    roll = targetShip.Roll,
                    yaw = targetShip.Yaw,
                    throttle = targetShip.Throttle,
                    lateralH = targetShip.LatH,
                    lateralV = targetShip.LatV,
                    boostHeld = targetShip.BoostButtonHeld,
                    limiterHeld = targetShip.VelocityLimitActive,
                    shipLightsEnabled = targetShip.IsNightVisionActive,
                    modifierShipForce = targetShip.AppliedEffects.shipForce,
                    modifierShipDeltaSpeedCap = targetShip.AppliedEffects.shipDeltaSpeedCap,
                    modifierShipDeltaThrust = targetShip.AppliedEffects.shipDeltaThrust,
                    modifierShipDrag = targetShip.AppliedEffects.shipDeltaDrag,
                    modifierShipAngularDrag = targetShip.AppliedEffects.shipDeltaAngularDrag
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