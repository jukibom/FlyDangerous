using System.Collections.Generic;

namespace Gameplay.Game_Modes.Components.Interfaces {
    public enum GameModeScoreType {
        Time,
        Score
    }

    public interface IGameModeWithScore {
        public IGameModeScore GameModeScore { set; }
        public void SetCurrentPersonalBest(float score, List<float> splits);
        public GameModeScoreType GameModeScoreType { get; }
    }
}