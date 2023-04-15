using System.Collections.Generic;
using Cinemachine;
using Core.Player;
using Core.Replays;
using GameUI.Components;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.U2D;

namespace Core.ShipModel {
    public class TargettingSystem : MonoBehaviour {
        [SerializeField] private Target targetPrefab;
        [SerializeField] private SpriteAtlas flags;
        private readonly Dictionary<IReplayShip, Target> _ghosts = new();
        private readonly Dictionary<ShipPlayer, Target> _players = new();
        private Camera _mainCamera;

        // TODO: manual targetting system
        [CanBeNull] private Target _activeTarget;

        private void OnEnable() {
            Game.OnVRStatus += OnVRStatusChanged;
            Game.OnPlayerJoin += ResetTargets;
            Game.OnPlayerLeave += ResetTargets;
            Game.OnGhostAdded += ResetTargets;
            Game.OnGhostRemoved += ResetTargets;
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCinemachineUpdate);
            _mainCamera = Camera.main;
        }

        private void OnDisable() {
            Game.OnVRStatus -= OnVRStatusChanged;
            Game.OnPlayerJoin -= ResetTargets;
            Game.OnPlayerLeave -= ResetTargets;
            Game.OnGhostAdded -= ResetTargets;
            Game.OnGhostRemoved -= ResetTargets;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCinemachineUpdate);
        }

        private void Update() {
            if (Game.IsVREnabled) CustomUpdate();
        }

        private void OnCinemachineUpdate(CinemachineBrain _) {
            if (!Game.IsVREnabled) CustomUpdate();
        }

        private void CustomUpdate() {
            // update camera if needed
            if (_mainCamera == null || _mainCamera.enabled == false || _mainCamera.gameObject.activeSelf == false)
                _mainCamera = Camera.main;

            if (_mainCamera != null) {
                FindClosestTarget();

                // update target objects for players
                foreach (var keyValuePair in _players) {
                    var player = keyValuePair.Key;
                    if (player == null) continue;
                    var target = keyValuePair.Value;
                    var targetName = player.playerName;
                    var icon = player.PlayerFlag != null ? flags.GetSprite(player.PlayerFlag.Filename) : null;
                    UpdateTarget(target, player.User.transform, targetName, icon);
                }

                foreach (var keyValuePair in _ghosts) {
                    var ghost = keyValuePair.Key;
                    if (ghost.Transform == null) continue;
                    var target = keyValuePair.Value;
                    var targetName = ghost.PlayerName;
                    var icon = ghost.PlayerFlag != null ? flags.GetSprite(ghost.PlayerFlag.Filename) : null;
                    UpdateTarget(target, ghost.Transform, targetName, icon);
                }
            }
        }

        private void UpdateTarget(Target target, Transform targetTransform, string targetName, Sprite icon) {
            var targetPosition = targetTransform.position;

            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                var origin = player.User.UserCameraTransform;
                var shipPosition = player.transform.position;

                var distanceToShip = Vector3.Distance(shipPosition, targetPosition);
                var direction = (targetPosition - origin.position).normalized;

                target.Name = targetName;
                target.DistanceMeters = distanceToShip;
                target.Icon = icon;

                var minDistance = 10f;
                var maxDistance = 30f + minDistance;

                var targetIndicatorTransform = target.transform;
                var lookAtTransform = Game.IsVREnabled
                    ? player.transform
                    : origin;

                targetIndicatorTransform.position = Vector3.MoveTowards(origin.position, targetPosition + direction * minDistance, maxDistance);
                targetIndicatorTransform.LookAt(origin.transform, lookAtTransform.up);

                target.Opacity = distanceToShip.Remap(5, minDistance, 0, 1);
                target.Update3dIndicatorFromOrientation(targetTransform, lookAtTransform);

                target.Toggle3dIndicator(target == _activeTarget);
            }
        }

        // auto-select a single target to show indicators (the one closest to center of screen)
        private void FindClosestTarget() {
            Target closest = null;
            var distance = Mathf.Infinity;
            foreach (var keyValuePair in _players) {
                var targetPositionViewport = _mainCamera.WorldToViewportPoint(keyValuePair.Value.transform.position);
                var targetPositionScreen = new Vector2(targetPositionViewport.x - 0.5f, targetPositionViewport.y - 0.5f);
                var targetDistanceFromCenter = targetPositionScreen.magnitude;
                if (targetDistanceFromCenter < distance) {
                    distance = targetDistanceFromCenter;
                    closest = keyValuePair.Value;
                }
            }

            foreach (var keyValuePair in _ghosts) {
                var targetPositionViewport = _mainCamera.WorldToViewportPoint(keyValuePair.Value.transform.position);
                var targetPositionScreen = new Vector2(targetPositionViewport.x - 0.5f, targetPositionViewport.y - 0.5f);
                var targetDistanceFromCenter = targetPositionScreen.magnitude;
                if (targetDistanceFromCenter < distance) {
                    distance = targetDistanceFromCenter;
                    closest = keyValuePair.Value;
                }
            }

            if (closest != null)
                // only update the active target if we're *actually* looking at them (unless we don't have a target)
                if (_activeTarget == null || distance < 0.05f)
                    _activeTarget = closest;
        }

        public void ResetTargets() {
            foreach (var keyValuePair in _players) Destroy(keyValuePair.Value.gameObject);
            foreach (var keyValuePair in _ghosts) Destroy(keyValuePair.Value.gameObject);
            _players.Clear();
            _ghosts.Clear();
            _activeTarget = null;

            var targetHtmlColor = Preferences.Instance.GetString("hudIndicatorColor");
            var targetColor = ColorExtensions.ParseHtmlColor(targetHtmlColor);

            foreach (var shipPlayer in FdNetworkManager.Instance.ShipPlayers)
                if (!shipPlayer.isLocalPlayer) {
                    var target = Instantiate(targetPrefab, transform);
                    target.SetColor(targetColor);
                    _players.Add(shipPlayer, target);
                }

            foreach (var replayShip in FindObjectsOfType<ShipGhost>()) {
                var target = Instantiate(targetPrefab, transform);
                target.SetColor(targetColor);
                _ghosts.Add(replayShip, target);
            }
        }

        private void OnVRStatusChanged(bool vrEnabled) {
            // rebuild targets to reset all rotations
            if (!vrEnabled) ResetTargets();
        }

        private void OnGameSettingsApplied() {
            ResetTargets();
        }
    }
}