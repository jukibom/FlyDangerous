using System.IO;
using System.Text;
using Core.Player;
using Core.Scores;
using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using UnityEngine;

namespace Core.Replays {
    public class ReplayRecorder : MonoBehaviour {
        private bool _recording;

        [CanBeNull] private Replay _replay;
        private ShipPhysics _targetShip;

        private uint _ticks;

        private void OnDestroy() {
            CancelRecording();
        }

        public void StartNewRecording(ShipPhysics targetShip) {
            _targetShip = targetShip;
            _targetShip.OnShipPhysicsUpdated += RecordFrame;
            _recording = true;
            _ticks = 0;
            _replay = Replay.CreateNewWritable(Game.Instance.ShipParameters, Game.Instance.LoadedLevelData, ShipProfile.FromPreferences());
        }

        public void CancelRecording() {
            if (_replay != null) {
                _replay.InputFrameStream.Close();
                _replay.KeyFrameStream.Close();
                _replay.InputFrameStream.Dispose();
                _replay.KeyFrameStream.Dispose();
                _replay = null;
            }
        }

        public void StopRecording(ScoreData scoreData = new()) {
            if (_targetShip) _targetShip.OnShipPhysicsUpdated -= RecordFrame;
            _recording = true;
            _ticks = 0;

            if (_replay != null) _replay.Save(scoreData);
        }

        /**
         * Record the frame every physics time step
         */
        private void RecordFrame(
            float pitch, float roll, float yaw, float throttle, float lateralH, float lateralV, bool boost, bool limiter, bool shipLightsEnabled
        ) {
            if (_recording && _replay != null) {
                // record a keyframe every specified amount of ticks
                if (_ticks % _replay.ReplayMeta.KeyFrameIntervalTicks == 0)
                    RecordKeyFrame(new KeyFrame {
                        replayFloatingOrigin = FloatingOrigin.Instance.Origin,
                        position = _targetShip.Position,
                        rotation = _targetShip.Rotation,
                        velocity = _targetShip.Velocity,
                        angularVelocity = _targetShip.AngularVelocity
                    });

                RecordInputFrame(new InputFrame {
                    pitch = pitch,
                    roll = roll,
                    yaw = yaw,
                    throttle = throttle,
                    lateralH = lateralH,
                    lateralV = lateralV,
                    boostHeld = boost,
                    limiterHeld = limiter,
                    shipLightsEnabled = shipLightsEnabled
                });

                _ticks++;
            }
        }

        private void RecordInputFrame(InputFrame inputFrame) {
            if (_replay is { CanWrite: true }) {
                var inputFrameBytes = MessagePackSerializer.Serialize(inputFrame);
                using var bw = new BinaryWriter(_replay.InputFrameStream, Encoding.UTF8, true);
                bw.Write(inputFrameBytes);
            }
        }

        private void RecordKeyFrame(KeyFrame keyFrame) {
            if (_replay is { CanWrite: true }) {
                var keyFrameBytes = MessagePackSerializer.Serialize(keyFrame);
                using var bw = new BinaryWriter(_replay.KeyFrameStream, Encoding.UTF8, true);
                bw.Write(keyFrameBytes);
            }
        }
    }
}