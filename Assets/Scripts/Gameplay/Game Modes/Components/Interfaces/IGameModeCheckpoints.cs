using System.Collections.Generic;

namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface IGameModeCheckpoints {
        // all checkpoints loaded in the scene
        public List<Checkpoint> Checkpoints { get; }

        // returns true if all checkpoints of type Check (not start or end) have been hit.
        public bool AllCheckpointsHit { get; }

        // reset the state of all checkpoints
        public void Reset();
    }
}