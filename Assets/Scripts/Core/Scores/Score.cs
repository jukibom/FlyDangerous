using System;
using System.Collections.Generic;
using System.IO;
using Core.MapData;
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
    }
    
    public class Score {

        public static Score ScoreForLevel(LevelData levelData) {
            return new Score(levelData);
        }

        public static Score NewPersonalBest(LevelData levelData, float raceTime, List<float> splitTime) {
            return new Score(levelData, raceTime, splitTime);
        }
        
        private ScoreData _scoreData;
        private LevelData _levelData;

        public bool HasPlayedPreviously => _scoreData.raceTime > 0;
        public float PersonalBestTotalTime => _scoreData.raceTime;
        public List<float> PersonalBestTimeSplits => _scoreData.splits;

        private Score(LevelData levelData) {
            _levelData = levelData;
            _scoreData = LoadJson(levelData);
        }
        
        private Score(LevelData levelData, float raceTimeMs, List<float> splitTimeMs) {
            _levelData = levelData;
            _scoreData = new ScoreData { raceTime = raceTimeMs, splits = splitTimeMs };
        }
        
        private static ScoreData LoadJson(LevelData levelData) {
            // try to find file at save location
            try {
                var fileLoc = Path.Combine(Application.persistentDataPath, "Save", "Records", $"{levelData.NameSlug}.json");
                Debug.Log("Loading from" + fileLoc);
                using var file = new FileStream(fileLoc, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(file);
                var json = reader.ReadToEnd();
                var saveData = JsonConvert.DeserializeObject<ScoreData>(json);
                
                // TODO: hash check
                return saveData;
            }
            catch {
                Debug.Log("Loading level score preferences");
                return new ScoreData();
            }
        }

        public void Save() {
            // Creates the path to the save file (make dir if needed).
            var saveLoc = Path.Combine(Application.persistentDataPath, "Save", "Records", $"{_levelData.NameSlug}.json");
            var directoryLoc = Path.GetDirectoryName(saveLoc);
            if (directoryLoc != null) {
                Directory.CreateDirectory(directoryLoc);
            }

            var json = _scoreData.ToJsonString();
            Debug.Log("Saving to " + saveLoc);

            using (var file = new FileStream(saveLoc, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                /* Another using block, this is because StreamWriter extends IDisposable,
                   Which means that it will need to be disposed of later. */
                using (var writer = new StreamWriter(file)) {
                    // StreamWriter is able to write strings out to streams.
                    writer.Write(json);
                    // Flush the data within the underlaying buffer to it's end point.
                    writer.Flush();
                }
            }
        }
    }
}