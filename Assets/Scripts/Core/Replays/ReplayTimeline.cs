using System;
using System.IO;
using System.Text;
using Core.ShipModel;
using JetBrains.Annotations;
using MessagePack;
using UnityEngine;

namespace Core.Replays {

    public interface IReplayShip {
        // ReSharper disable once InconsistentNaming
        Transform transform { get; }
        ShipPhysics ShipPhysics { get; }
        public void SetAbsolutePosition(Vector3 position);
    }
    
    public class ReplayTimeline : MonoBehaviour {

        private const int SizeInputFrameBytes = 39;
        private const int SizeKeyFrameBytes = 71;
        
        [CanBeNull] private Replay _replay;
        [CanBeNull] private IReplayShip _shipReplayObject;
        private int _ticks = 0;
        private float _playSpeed = 1f;
        private bool _isPlaying;
        private void OnDestroy() {
            if (_replay != null) {
                _replay.InputFileStream.Close();
                _replay.KeyFrameFileStream.Close();
            }
        }

        public void LoadReplay(IReplayShip ship, Replay replay) {
            _replay = replay;
            _shipReplayObject = ship;
            ship.ShipPhysics.RefreshShipModel(replay.ShipProfile);
            ship.SetAbsolutePosition(replay.LevelData.startPosition.ToVector3());
            ship.transform.rotation = Quaternion.Euler(
                replay.LevelData.startRotation.x,
                replay.LevelData.startRotation.y,
                replay.LevelData.startRotation.z
            );
            _ticks = 0;
        }

        public void Play() {
            _isPlaying = true;
        }

        public void Pause() {
            _isPlaying = false;
        }

        public void Stop() {
            _ticks = 0;
            _isPlaying = false;
        }

        public void FixedUpdate() {
            Debug.Log("yay " + _replay + " " + _shipReplayObject);
            if (_replay != null && _shipReplayObject != null) {
                using BinaryReader br = new BinaryReader(_replay.InputFileStream, Encoding.UTF8, true);
                byte[] inputFrameBytes = new byte[SizeInputFrameBytes];
                
                // TODO: This is slow as all hell! We should abstract this and use SeekOrigin.Current in typical ghost run
                // TODO: Detect end of file
                br.BaseStream.Seek(_ticks * SizeInputFrameBytes, SeekOrigin.Begin);
                br.Read(inputFrameBytes, 0, SizeInputFrameBytes);
                
                var inputFrame = MessagePackSerializer.Deserialize<InputFrame>(inputFrameBytes);
                _shipReplayObject.ShipPhysics.UpdateShip(inputFrame.pitch, inputFrame.roll, inputFrame.yaw, inputFrame.throttle, inputFrame.lateralH, inputFrame.lateralV, inputFrame.boostHeld, false, false, false);
                _ticks ++;
            }
        }
    }
}