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
            if (_recording && Replay != null) {
                // record a keyframe every specified amount of ticks
                if (_ticks % Replay.ReplayMeta.KeyFrameIntervalTicks == 0)
                    RecordKeyFrame(new KeyFrame {
                        replayFloatingOrigin = FloatingOrigin.Instance.Origin,
                        position = _targetShip.Position,
                        rotation = _targetShip.Rotation,
                        velocity = _targetShip.Velocity,
                        angularVelocity = _targetShip.AngularVelocity
                    });

                RecordInputFrame(new InputFrame {
                    pitch = _targetShip.Pitch,
                    roll = _targetShip.Roll,
                    yaw = _targetShip.Yaw,
                    throttle = _targetShip.Throttle,
                    lateralH = _targetShip.LatH,
                    lateralV = _targetShip.LatV,
                    boostHeld = _targetShip.BoostButtonHeld,
                    limiterHeld = _targetShip.VelocityLimitActive,
                    shipLightsEnabled = _targetShip.IsShipLightsActive
                });

                _ticks++;
            }
        }

        private void RecordInputFrame(InputFrame inputFrame) {
            if (Replay is { CanWrite: true }) {
                var inputFrameBytes = MessagePackSerializer.Serialize(inputFrame);
                using var bw = new BinaryWriter(Replay.InputFrameStream, Encoding.UTF8, true);
                bw.Write(inputFrameBytes);
            }
        }

        private void RecordKeyFrame(KeyFrame keyFrame) {
            if (Replay is { CanWrite: true }) {
                var keyFrameBytes = MessagePackSerializer.Serialize(keyFrame);
                using var bw = new BinaryWriter(Replay.KeyFrameStream, Encoding.UTF8, true);
                bw.Write(keyFrameBytes);
            }
        }
    }
}