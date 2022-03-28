using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using Core.Player;
using Core.Scores;
using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using Misc;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Core.Replays {

    public class ReplayRecorder : MonoBehaviour {
        private static readonly int keyFrameIntervalTicks = 25;
        
        private int _ticks;
        private bool _recording;
        private ShipPhysics _targetShip;

        [CanBeNull] private Replay _replay;

        public void StartNewRecording(ShipPhysics targetShip) {
            _targetShip = targetShip;
            _targetShip.OnShipPhysicsUpdated += RecordFrame;
            _recording = true;
            _ticks = 0;
            _replay = Replay.CreateNewWritable(Game.Instance.ShipParameters, Game.Instance.LevelDataAtCurrentPosition, ShipProfile.FromPreferences());
        }

        private void OnDestroy() {
            StopRecording();
        }

        public void StopRecording(ScoreData scoreData = new()) {
            if (_targetShip) {
                _targetShip.OnShipPhysicsUpdated -= RecordFrame;
            }
            _recording = true;
            _ticks = 0;

            if (_replay != null) {
                _replay.Save(scoreData);
            }
        }

        /** Record the frame every physics time step */
        private void RecordFrame(
            float pitch, float roll, float yaw, float throttle, float lateralH, float lateralV, bool boost, bool limiter, bool shipLightsEnabled
        ) {
            if (_recording) {
                // record a keyframe every specified amount of ticks
                if (_ticks % keyFrameIntervalTicks == 0) {
                    RecordKeyFrame(new KeyFrame {
                        position = _targetShip.AbsoluteWorldPosition,
                        rotation = _targetShip.Rotation,
                        velocity = _targetShip.Velocity,
                        angularVelocity = _targetShip.AngularVelocity
                    });
                }

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
                byte[] inputFrameBytes = MessagePackSerializer.Serialize(inputFrame);
                using BinaryWriter bw = new BinaryWriter(_replay.InputFileStream, Encoding.UTF8, true);
                bw.Write(inputFrameBytes);
                SimpleDebug.Log(inputFrameBytes.Length);
            } 
        }

        private void RecordKeyFrame(KeyFrame keyFrame) {
            if (_replay is { CanWrite: true }) {
                byte[] keyFrameBytes = MessagePackSerializer.Serialize(keyFrame);
                using BinaryWriter bw = new BinaryWriter(_replay.KeyFrameFileStream, Encoding.UTF8, true);
                bw.Write(keyFrameBytes);
                SimpleDebug.Log(keyFrameBytes.Length);
            }
        }
    }
}