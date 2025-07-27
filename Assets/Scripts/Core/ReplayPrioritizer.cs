using System;
using System.Collections.Generic;
using System.Linq;
using Core.Player;
using Core.Replays;
using JetBrains.Annotations;
using Mirror;
using Misc;
using NaughtyAttributes;
using UnityEngine;

namespace Core {
    public class ReplayPrioritizer : Singleton<ReplayPrioritizer> {
        private readonly List<ReplayTimeline> _replays = new();
        
        public List<ReplayTimeline> Replays => _replays;
        
        public bool IsSpectating => _replays.Any(replay => replay.ShipReplayObject is { SpectatorActive: true });
        [CanBeNull] public IReplayShip ActiveSpectatedShip => _replays.FirstOrDefault(replay => replay.ShipReplayObject is { SpectatorActive: true })?.ShipReplayObject;
        
        public void RegisterReplay(ReplayTimeline replay) {
            _replays.Add(replay);
        }
        
        public void UnregisterReplay(ReplayTimeline replay) {
            _replays.Remove(replay);
        }

        public void SpectateGhost(ShipGhost ghost) {
            if (ActiveSpectatedShip != null) {
                ActiveSpectatedShip.ShipPhysics.ShipModel?.SetVisible(false);
            }
            
            foreach (var replayTimeline in _replays) {
                if (replayTimeline.ShipReplayObject == null) 
                    continue;
                replayTimeline.ShipReplayObject.SpectatorActive = false;
            }
            
            ghost.SpectatorActive = true;
            ghost.ShipPhysics.ShipModel?.SetVisible(true);
            
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.ShipPhysics.ShipModel?.SetVisible(false);
                player.User.TargetTransform = ghost.transform;
            }
            else {
                Debug.LogWarning("Failed to set ghost spectator, player does not exist!");
            }
        }

        [Button]
        public void StopSpectating() {
            foreach (var replayTimeline in _replays) {
                if (replayTimeline.ShipReplayObject == null) 
                    continue;
                replayTimeline.ShipReplayObject.SpectatorActive = false;
            }
            
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.ShipPhysics.ShipModel?.SetVisible(true);
                player.User.TargetTransform = player.transform;
            }
            else {
                Debug.LogWarning("Failed to restore player logic from ghost spectator, player does not exist!");
            }
        }

        private void FixedUpdate() {
            if (_replays.Count == 0) return;
            
            ReplayTimeline spectatedReplay = null;
            foreach (var replayTimeline in _replays) {
                if (replayTimeline.ShipReplayObject != null && replayTimeline.ShipReplayObject.SpectatorActive) {
                    spectatedReplay = replayTimeline;
                    break;
                }
            }
            
            foreach (var replayTimeline in _replays) {
                if (replayTimeline == spectatedReplay)
                    continue;
                replayTimeline.UpdateReplay();
            }
            
            if (spectatedReplay != null) spectatedReplay.UpdateReplay();
        }
    }
}