using System;
using System.Collections.Generic;
using Core.MapData;
using Core.Scores;
using Gameplay.Game_Modes.Components.Interfaces;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    public class TargetScore : ITargetScore {
        public float Score { get; }
        public string Medal { get; }
        public Color MedalColor { get; }

        public TargetScore(float score, string medal, Color medalColor) {
            Score = score;
            Medal = medal;
            MedalColor = medalColor;
        }
    }

    public class GameModeScore : IGameModeScore {
        protected static readonly Color BronzeMedalColor = new(1, 0.43f, 0, 1);
        protected static readonly Color SilverMedalColor = new(0.6f, 0.6f, 0.6f, 1);
        protected static readonly Color GoldMedalColor = new(1, 0.98f, 0.4f, 1);
        protected static readonly Color AuthorMedalColor = new(0.3f, 0.6f, 0, 1);
        protected static readonly Color PersonalBestMedalColor = Color.white;

        public float CurrentBestScore => Score.PersonalBestScore;
        public List<float> CurrentBestSplits => Score.PersonalBestSplits;
        public float ScoreForBronzeMedal => Score.BronzeTimeTarget(_levelData);
        public float ScoreForSilverMedal => Score.SilverTimeTarget(_levelData);
        public float ScoreForGoldMedal => Score.GoldTimeTarget(_levelData);
        public float ScoreForAuthorMedal => Score.AuthorTimeTarget(_levelData);

        public ITargetScore BronzeTargetScore => new TargetScore(ScoreForBronzeMedal, "Bronze", BronzeMedalColor);
        public ITargetScore SilverTargetScore => new TargetScore(ScoreForSilverMedal, "Silver", SilverMedalColor);
        public ITargetScore GoldTargetScore => new TargetScore(ScoreForGoldMedal, "Gold", GoldMedalColor);
        public ITargetScore AuthorTargetScore => new TargetScore(ScoreForAuthorMedal, "Author", AuthorMedalColor);
        public ITargetScore PersonalBestTargetScore => new TargetScore(Score.PersonalBestScore, "Personal Best", PersonalBestMedalColor);

        public Score Score { get; private set; }

        public Score PreviousScore { get; private set; }

        private readonly IGameMode _gameMode;
        private readonly LevelData _levelData;

        public GameModeScore(IGameMode gameMode, LevelData levelData) {
            _gameMode = gameMode;
            _levelData = levelData;
            Score = Score.ScoreForLevel(levelData);
            PreviousScore = Score;
        }

        public void Reset() {
            Score = Score.ScoreForLevel(_levelData);
            PreviousScore = Score;
            if (_gameMode is IGameModeWithScore gameModeWithScore)
                gameModeWithScore.SetCurrentPersonalBest(Score.PersonalBestScore, Score.PersonalBestSplits);
        }

        public bool NewScore(float score) {
            return NewScore(score, new List<float>());
        }

        public bool NewScore(float score, List<float> splits) {
            if (_gameMode is IGameModeWithScore gameModeWithScore) {
                var previousScore = Score.PersonalBestScore;
                Score = Score.FromRaceTime(score, splits);
                var isNewBestScore = gameModeWithScore.GameModeScoreType == GameModeScoreType.Score ? score > previousScore : score < previousScore;
                if (isNewBestScore) return true;
            }

            return false;
        }

        public ITargetScore NextTargetScore(bool includeAuthorScore = false) {
            if (_gameMode is IGameModeWithScore gameModeWithScore) {
                if (!Score.HasPlayedPreviously)
                    return BronzeTargetScore;

                var currentScore = Score.PersonalBestScore;

                // more is better
                if (gameModeWithScore.GameModeScoreType == GameModeScoreType.Score) {
                    if (currentScore < ScoreForBronzeMedal) return BronzeTargetScore;
                    if (currentScore < ScoreForSilverMedal) return SilverTargetScore;
                    if (currentScore < ScoreForGoldMedal) return GoldTargetScore;
                    if (currentScore < ScoreForAuthorMedal && includeAuthorScore) return AuthorTargetScore;
                    return PersonalBestTargetScore;
                }

                // less is better
                if (gameModeWithScore.GameModeScoreType == GameModeScoreType.Time) {
                    if (currentScore > ScoreForBronzeMedal) return BronzeTargetScore;
                    if (currentScore > ScoreForSilverMedal) return SilverTargetScore;
                    if (currentScore > ScoreForGoldMedal) return GoldTargetScore;
                    if (currentScore > ScoreForAuthorMedal && includeAuthorScore) return AuthorTargetScore;
                    return PersonalBestTargetScore;
                }
            }

            throw new Exception("Unimplemented game mode score type or somehow requested a score without a game mode which uses a score?!");
        }
    }
}