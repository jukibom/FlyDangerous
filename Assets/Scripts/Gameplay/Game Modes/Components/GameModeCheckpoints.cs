using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Gameplay.Game_Modes.Components.Interfaces;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    public class GameModeCheckpoints : MonoBehaviour, IGameModeCheckpoints {
        [SerializeField] private Checkpoint checkpointPrefab;

        public List<Checkpoint> Checkpoints { get; private set; } = new();

        // returns true if all checkpoints of type Check (not start or end) have been hit.
        public bool AllCheckpointsHit => Checkpoints
            .Where(c => c.Type == CheckpointType.Check)
            .All(c => c.IsHit);

        public void Reset() {
            Checkpoints.ForEach(c => c.Reset());
        }

        public void RefreshCheckpoints() {
            Checkpoints.Clear();
            Checkpoints = GetComponentsInChildren<Checkpoint>().ToList();
        }

        public Checkpoint AddCheckpoint(SerializebleCheckpoint serializableCheckpoint) {
            var checkpoint = Instantiate(checkpointPrefab, transform);

            checkpoint.Type = serializableCheckpoint.type;

            var checkpointObjectTransform = checkpoint.transform;
            checkpointObjectTransform.position = serializableCheckpoint.position.ToVector3();
            checkpointObjectTransform.rotation = Quaternion.Euler(serializableCheckpoint.rotation.ToVector3());

            Checkpoints.Add(checkpoint);

            return checkpoint;
        }
    }
}