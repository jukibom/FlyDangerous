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
        public uint CurrentTick
        {
            get => _ticks;
            set => _ticks = value;
        }

        private void OnDestroy() {
            CancelRecording();
        }

        public void StartNewRecording(ShipPhysics targetShip) {
            _targetShip = targetShip;
            _recording = true;
            _ticks = 0;
            _targetShip.OnShipPhysicsUpdated += RecordFrame;
            Replay = Replay.CreateNewWritable(Game.Instance.ShipParameters, Game.Instance.LoadedLevelData, ShipProfile.FromPreferences());
        }

        public void CancelRecording() {
            StopRecording();
            if (Replay != null) {
                Replay.InputFrameStream.Close();
                Replay.KeyFrameStream.Close();
                Replay.InputFrameStream.Dispose();
                Replay.KeyFrameStream.Dispose();
                Replay = null;
            }
        }

        public void StopRecording() {
            if (_targetShip != null) _targetShip.OnShipPhysicsUpdated -= RecordFrame;
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
                        boostStatus = targetShip._boostStatus,
                        boostProgressTicks = (float)targetShip._boostProgressTicks,
                        boostTime = targetShip._currentBoostTime,
                        boostCapacitorPercent = targetShip._boostCapacitorPercent,
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
                using var bw = new BinaryWriter(Replay.InputFrameStream, Encoding.UTF8, true);
                bw.Write(inputFrameBytes);
            }
        }

        private void RecordKeyFrame(KeyFrameV2 keyFrame) {
            if (Replay is { CanWrite: true }) {
                var keyFrameBytes = MessagePackSerializer.Serialize(keyFrame);
                using var bw = new BinaryWriter(Replay.KeyFrameStream, Encoding.UTF8, true);
                bw.Write(keyFrameBytes);
            }
        }
    }
}