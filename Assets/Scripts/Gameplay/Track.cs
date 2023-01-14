using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using Core.MapData;
using Core.MapData.Serializable;
using Core.Player;
using Core.Scores;
using Gameplay.Game_Modes.Components;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay {
    public class Track : MonoBehaviour {
        public delegate void CheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds);

        public event CheckpointHit OnCheckpointHit;

        [SerializeField] private GameModeCheckpoints checkpointContainer;
        [SerializeField] private GameModeModifiers modifierContainer;
        [SerializeField] private GameModeBillboards billboardContainer;
        [SerializeField] private Transform geometryContainer;

        [HorizontalLine] [SerializeField] private string trackName;

        [Dropdown("GetGameModes")] [OnValueChanged("SetGameMode")] [SerializeField]
        private string gameMode;

        [Dropdown("GetMusicTracks")] [OnValueChanged("PlayMusicTrack")] [SerializeField]
        private string musicTrack;

        [SerializeField] [OnValueChanged("UpdateTimesFromAuthor")]
        private float authorTimeTarget;

        [ReadOnly] [SerializeField] private float goldTime;
        [ReadOnly] [SerializeField] private float silverTime;
        [ReadOnly] [SerializeField] private float bronzeTime;

        [HorizontalLine] [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 startRotation;

        [Button("Set from ship position")]
        [UsedImplicitly]
        private void SetFromShip() {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Set player transform");
#endif
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                startPosition = player.AbsoluteWorldPosition;
                startRotation = player.transform.rotation.eulerAngles;
            }
        }

        public GameModeCheckpoints GameModeCheckpoints => checkpointContainer;

        private void OnDestroy() {
            if (checkpointContainer != null)
                foreach (var checkpoint in checkpointContainer.Checkpoints)
                    checkpoint.OnHit -= HandleOnCheckpointHit;
        }

        public LevelData Serialize(Vector3? overrideStartPosition = null, Quaternion? overrideStartRotation = null) {
            var loadedLevelData = Game.Instance.LoadedLevelData;

            var levelData = new LevelData {
                name = trackName,
                authorTimeTarget = authorTimeTarget,
                gameType = GameType.FromString(gameMode),
                location = loadedLevelData.location,
                musicTrack = MusicTrack.FromString(musicTrack),
                environment = loadedLevelData.environment,
                terrainSeed = string.IsNullOrEmpty(loadedLevelData.terrainSeed) ? null : loadedLevelData.terrainSeed,
                startPosition = SerializableVector3.FromVector3(overrideStartPosition ?? startPosition),
                startRotation = SerializableVector3.FromVector3(overrideStartRotation?.eulerAngles ?? startRotation)
            };

            checkpointContainer.RefreshCheckpoints();
            if (checkpointContainer.Checkpoints.Count > 0)
                levelData.checkpoints = checkpointContainer
                    .Checkpoints
                    .ConvertAll(SerializableCheckpoint.FromCheckpoint);

            billboardContainer.RefreshBillboardSpawners();
            if (billboardContainer.BillboardSpawners.Count > 0)
                levelData.billboards = billboardContainer.BillboardSpawners
                    .ConvertAll(SerializableBillboard.FromBillboardSpawner);

            modifierContainer.RefreshModifierSpawners();
            if (modifierContainer.ModifierSpawners.Count > 0)
                levelData.modifiers = modifierContainer.ModifierSpawners
                    .ConvertAll(SerializableModifier.FromModifierSpawner);

            // TODO: geometry

            return levelData;
        }

        // Build level geometry from json
        public void Deserialize(LevelData levelData) {
            trackName = levelData.name;
            gameMode = levelData.gameType.Name;
            authorTimeTarget = levelData.authorTimeTarget;
            musicTrack = levelData.musicTrack.Name;

            startPosition = levelData.startPosition.ToVector3();
            startPosition = levelData.startRotation.ToVector3();

            if (levelData.checkpoints?.Count > 0)
                levelData.checkpoints.ForEach(c => {
                    var checkpoint = checkpointContainer.AddCheckpoint(c);
                    checkpoint.OnHit += HandleOnCheckpointHit;
                });

            if (levelData.billboards?.Count > 0)
                levelData.billboards.ForEach(b => billboardContainer.AddBillboard(b));

            if (levelData.modifiers?.Count > 0)
                levelData.modifiers.ForEach(m => modifierContainer.AddModifier(m));

            // TODO: geometry
        }

        private void HandleOnCheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds) {
            OnCheckpointHit?.Invoke(checkpoint, excessTimeToHitSeconds);
        }

        #region Editor Hooks

        [UsedImplicitly]
        private List<string> GetGameModes() {
            return GameType.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private void SetGameMode() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                Game.Instance.GameModeHandler.InitialiseGameMode(player, Serialize(), GameType.FromString(gameMode).GameMode, player.User.InGameUI, this);
                Game.Instance.RestartSession();
            }
        }

        [UsedImplicitly]
        private List<string> GetMusicTracks() {
            return MusicTrack.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private void PlayMusicTrack() {
            MusicManager.Instance.PlayMusic(MusicTrack.FromString(musicTrack), true, true, false);
        }

        [UsedImplicitly]
        private void UpdateTimesFromAuthor() {
            var levelData = Serialize();
            goldTime = Score.GoldTimeTarget(levelData);
            silverTime = Score.SilverTimeTarget(levelData);
            bronzeTime = Score.BronzeTimeTarget(levelData);
        }

        [Button("Copy to Clipboard")]
        private void CopyToClipboard() {
            GUIUtility.systemCopyBuffer = Serialize().ToJsonString();
        }

        #endregion
    }
}