using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Core.MapData;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Scores {
    public struct ScoreData {
        public float raceTime;
        public List<float> splits;
        public string hash;

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static ScoreData FromJsonString(string json) {
            return JsonConvert.DeserializeObject<ScoreData>(json);
        }
    }

    public class Score {
        private ScoreData _scoreData;

        private Score(LevelData levelData) {
            _scoreData = LoadJson(levelData);
        }

        private Score(float raceTimeMs, List<float> splitTimeMs) {
            _scoreData = new ScoreData { raceTime = raceTimeMs, splits = splitTimeMs };
        }

        public bool HasPlayedPreviously => _scoreData.raceTime > 0;
        public float PersonalBestTotalTime => _scoreData.raceTime;
        public List<float> PersonalBestTimeSplits => _scoreData.splits;

        public static Score ScoreForLevel(LevelData levelData) {
            return new Score(levelData);
        }

        public static Score FromRaceTime(float raceTime, List<float> splitTime) {
            return new Score(raceTime, splitTime);
        }

        public static float AuthorTimeTarget(LevelData level) {
            return level.authorTimeTarget;
        }

        public static float GoldTimeTarget(LevelData level) {
            return level.authorTimeTarget * 1.05f;
        }

        public static float SilverTimeTarget(LevelData level) {
            return level.authorTimeTarget * 1.25f;
        }

        public static float BronzeTimeTarget(LevelData level) {
            return level.authorTimeTarget * 1.7f;
        }

        private static ScoreData LoadJson(LevelData levelData) {
            // try to find file at save location
            try {
                var filename = levelData.LevelHash();
                var fileLoc = Path.Combine(Application.persistentDataPath, "Save", "Records", $"{filename}");
                using var file = new FileStream(fileLoc, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(file);
                var json = reader.ReadToEnd();
                var scoreData = ScoreData.FromJsonString(json);

                var scoreHash = ScoreHash(scoreData.raceTime, levelData);
                if (scoreHash != scoreData.hash) {
                    Debug.LogWarning("Failed integrity check on save data.");
                    return new ScoreData();
                }

                return scoreData;
            }
            catch {
                return new ScoreData();
            }
        }

        private static string ScoreHash(float score, LevelData levelData) {
            // generate the filename from a hash combination of score, checkpoints and location.
            var checkpoints =
                levelData.checkpoints.ConvertAll(checkpoint => checkpoint.position.ToString() + checkpoint.rotation);
            var checkpointText = "";
            foreach (var checkpoint in checkpoints) checkpointText += checkpoint;
            return HashGenerator.ComputeSha256Hash(score.ToString(CultureInfo.InvariantCulture) + checkpointText + levelData.location.Name);
        }

        public ScoreData Save(LevelData levelData) {
            _scoreData.hash = ScoreHash(_scoreData.raceTime, levelData);
            return _scoreData;
        }

        public static void SaveToDisk(ScoreData scoreData, LevelData levelData) {
            // Creates the path to the save file (make dir if needed).
            var filename = levelData.LevelHash();
            var saveLoc = Path.Combine(Application.persistentDataPath, "Save", "Records", $"{filename}");
            var directoryLoc = Path.GetDirectoryName(saveLoc);
            if (directoryLoc != null) Directory.CreateDirectory(directoryLoc);

            var json = scoreData.ToJsonString();

            using var file = new FileStream(saveLoc, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(file);
            writer.Write(json);
            writer.Flush();
        }
    }
}