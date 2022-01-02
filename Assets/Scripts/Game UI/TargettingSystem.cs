using System;
using System.Collections.Generic;
using Core;
using Core.Player;
using UnityEngine;

namespace Game_UI {
    public class TargettingSystem : MonoBehaviour {
        [SerializeField] private Target targetPrefab;
        private readonly Dictionary<ShipPlayer, Target> _players = new();

        private void OnEnable() {
            Game.OnVRStatus += OnVRStatusChanged;
            Game.OnPlayerLoaded += OnPlayerLoaded;
            Game.OnPlayerLeave += OnPlayerLeave;
        }
        
        private void OnDisable() {
            Game.OnVRStatus -= OnVRStatusChanged;
            Game.OnPlayerLoaded -= OnPlayerLoaded;
            Game.OnPlayerLeave -= OnPlayerLeave;
        }

        // Update is called once per frame
        void Update() {

            // update target objects for players
            foreach (var keyValuePair in _players) {
                var player = keyValuePair.Key;
                var target = keyValuePair.Value;
                
                var playerName = player.playerName;
                var originPosition = ShipPlayer.FindLocal ? ShipPlayer.FindLocal.User.UserHeadTransform.position : Vector3.zero;
                
                var targetPosition = player.User.transform.position;
                
                var distance = Vector3.Distance(originPosition, targetPosition);
                var direction = (targetPosition - originPosition).normalized;

                target.Name = playerName;
                target.DistanceMeters = distance;

                var minDistance = 10f;
                var maxDistance = 30f + minDistance;
                
                target.transform.position = Vector3.MoveTowards(originPosition, targetPosition + (direction * minDistance), maxDistance);
            }
        }

        private void ResetTargets() {
            var players = FdNetworkManager.Instance.ShipPlayers;
            foreach (var keyValuePair in _players) {
                Destroy(keyValuePair.Value.gameObject);
            }
            _players.Clear();
            foreach (var shipPlayer in players) {
                if (!shipPlayer.isLocalPlayer) {
                    var target = Instantiate(targetPrefab, transform);
                    _players.Add(shipPlayer, target);
                }
            }
        }
        
        private void OnPlayerLoaded() {
            ResetTargets();
        }

        private void OnPlayerLeave() {
            ResetTargets();
        }

        private void OnVRStatusChanged(bool vrEnabled) {
            // rebuild targets to reset all rotations
            if (!vrEnabled) {
                ResetTargets();
            }
        }
    }
}
