using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface ITargetScore {
        float Score { get; }
        string Medal { get; }
        Color MedalColor { get; }
    }

    public interface IGameModeScore {
        public float CurrentBestScore { get; }
        public List<float> CurrentBestSplits { get; }
        public float ScoreForBronzeMedal { get; }
        public float ScoreForSilverMedal { get; }
        public float ScoreForGoldMedal { get; }
        public float ScoreForAuthorMedal { get; }
        public ITargetScore BronzeTargetScore { get; }
        public ITargetScore SilverTargetScore { get; }
        public ITargetScore GoldTargetScore { get; }
        public ITargetScore AuthorTargetScore { get; }
        public ITargetScore PersonalBestTargetScore { get; }

        //Record a new score, returns true if this is a new personal best
        public bool NewScore(float score, List<float> splits);
        public bool NewScore(float score);
        public ITargetScore NextTargetScore(bool includeAuthorScore = false);
    }
}