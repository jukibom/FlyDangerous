using System.Linq;
using Core;
using Core.MapData;
using Gameplay.Game_Modes.Components;
using Misc;
using UnityEngine;

namespace Gameplay {
    public class Track : MonoBehaviour {
        public delegate void CheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds);

        public event CheckpointHit OnCheckpointHit;

        [SerializeField] private GameModeCheckpoints checkpointContainer;
        [SerializeField] private Transform modifierContainer;
        [SerializeField] private Transform billboardContainer;
        [SerializeField] private Transform geometryContainer;

        public GameModeCheckpoints GameModeCheckpoints => checkpointContainer;

        private void OnDestroy() {
            foreach (var checkpoint in checkpointContainer.Checkpoints)
                checkpoint.OnHit -= HandleOnCheckpointHit;
        }

        private void HandleOnCheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds) {
            OnCheckpointHit?.Invoke(checkpoint, excessTimeToHitSeconds);
        }

        public LevelData Serialize(Vector3 startPosition, Quaternion startRotation) {
            var loadedLevelData = Game.Instance.LoadedLevelData;

            var levelData = new LevelData {
                name = loadedLevelData.name,
                gameType = loadedLevelData.gameType, // TODO: this should come from the game mode initialised here!
                location = loadedLevelData.location,
                musicTrack = loadedLevelData.musicTrack,
                environment = loadedLevelData.environment,
                terrainSeed = loadedLevelData.terrainSeed,
                startPosition = SerializableVector3.FromVector3(startPosition),
                startRotation = SerializableVector3.FromVector3(startRotation.eulerAngles)
            };

            levelData.checkpoints = checkpointContainer
                .GetComponentsInChildren<Checkpoint>()
                .ToList()
                .ConvertAll(SerializeableCheckpoint.FromCheckpoint);

            // TODO: modifiers
            // TODO: billboards
            // TODO: geometry

            return levelData;
        }

        // Build level geometry from json
        public void Deserialize(LevelData levelData) {
            if (levelData.checkpoints?.Count > 0)
                levelData.checkpoints.ForEach(c => {
                    var checkpoint = checkpointContainer.AddCheckpoint(c);
                    checkpoint.OnHit += HandleOnCheckpointHit;
                });

            // TODO: modifiers
            // TODO: billboards
            // TODO: geometry
        }
    }
}