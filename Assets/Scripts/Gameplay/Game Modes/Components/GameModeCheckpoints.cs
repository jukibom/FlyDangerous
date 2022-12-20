using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Gameplay.Game_Modes.Components.Interfaces;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    public class GameModeCheckpoints : MonoBehaviour, IGameModeCheckpoints {
        [SerializeField] private Checkpoint checkpointPrefab;

        public List<Checkpoint> Checkpoints { get; } = new();

        // returns true if all checkpoints of type Check (not start or end) have been hit.
        public bool AllCheckpointsHit => Checkpoints
            .Where(c => c.Type == CheckpointType.Check)
            .All(c => c.IsHit);

        public void Reset() {
            Checkpoints.ForEach(c => c.Reset());
        }

        public Checkpoint AddCheckpoint(SerializeableCheckpoint serializeableCheckpoint) {
            var checkpoint = Instantiate(checkpointPrefab, transform);

            checkpoint.Type = serializeableCheckpoint.type;

            var checkpointObjectTransform = checkpoint.transform;
            checkpointObjectTransform.position = serializeableCheckpoint.position.ToVector3();
            checkpointObjectTransform.rotation = Quaternion.Euler(serializeableCheckpoint.rotation.ToVector3());

            Checkpoints.Add(checkpoint);

            return checkpoint;
        }
    }
}