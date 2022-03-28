using System.Collections.Generic;
using Core.Player;
using Core.Replays;
using Game_UI;
using UnityEngine;

namespace Core.ShipModel {
    public class TargettingSystem : MonoBehaviour {
        [SerializeField] private Target targetPrefab;
        private readonly Dictionary<ShipGhost, Target> _ghosts = new();
        private readonly Dictionary<ShipPlayer, Target> _players = new();
        private Camera _mainCamera;

        private void Update() {
            // update target objects for players
            foreach (var keyValuePair in _players) {
                var player = keyValuePair.Key;
                var target = keyValuePair.Value;
                var targetName = player.playerName;
                var targetPosition = player.User.transform.position;
                UpdateTarget(target, targetPosition, targetName);
            }

            foreach (var keyValuePair in _ghosts) {
                var ghost = keyValuePair.Key;
                var target = keyValuePair.Value;
                var targetName = ghost.PlayerName;
                var targetPosition = ghost.transform.position;
                UpdateTarget(target, targetPosition, targetName);
            }
        }

        private void OnEnable() {
            Game.OnVRStatus += OnVRStatusChanged;
            Game.OnPlayerLoaded += ResetTargets;
            Game.OnPlayerLeave += ResetTargets;
            Game.OnGhostAdded += ResetTargets;
            Game.OnGhostRemoved += ResetTargets;
            _mainCamera = Camera.main;
        }

        private void OnDisable() {
            Game.OnVRStatus -= OnVRStatusChanged;
            Game.OnPlayerLoaded -= ResetTargets;
            Game.OnPlayerLeave -= ResetTargets;
            Game.OnGhostAdded -= ResetTargets;
            Game.OnGhostRemoved -= ResetTargets;
        }

        private void UpdateTarget(Target target, Vector3 targetPosition, string targetName) {
            var originPosition = FdPlayer.FindLocalShipPlayer ? FdPlayer.FindLocalShipPlayer.User.UserCameraPosition : Vector3.zero;

            var distance = Vector3.Distance(originPosition, targetPosition);
            var direction = (targetPosition - originPosition).normalized;

            target.Name = targetName;
            target.DistanceMeters = distance;

            var minDistance = 10f;
            var maxDistance = 30f + minDistance;

            var targetTransform = target.transform;
            targetTransform.position = Vector3.MoveTowards(originPosition, targetPosition + direction * minDistance, maxDistance);
            targetTransform.rotation = _mainCamera.transform.rotation;
        }

        private void ResetTargets() {
            var players = FdNetworkManager.Instance.ShipPlayers;
            foreach (var keyValuePair in _players) Destroy(keyValuePair.Value.gameObject);
            _players.Clear();
            foreach (var shipPlayer in players)
                if (!shipPlayer.isLocalPlayer) {
                    var target = Instantiate(targetPrefab, transform);
                    _players.Add(shipPlayer, target);
                }

            foreach (var replayShip in FindObjectsOfType<ShipGhost>()) {
                var target = Instantiate(targetPrefab, transform);
                _ghosts.Add(replayShip, target);
            }
        }

        private void OnVRStatusChanged(bool vrEnabled) {
            // rebuild targets to reset all rotations
            if (!vrEnabled) ResetTargets();
        }
    }
}