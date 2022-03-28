using System;
using System.IO;
using Core.MapData;
using Core.Player;
using Core.Scores;
using Core.ShipModel;
using UnityEngine;

namespace Core.Replays {
    
    public enum ReplayMode {
        Record,
        Playback
    }

    /**
     * Replays are too large an unwieldy to be loaded into memory, instead we stream the important stuff to / from
     * the individual files.
     * Replays are made up of 4 files:
     *  - Initial Data - starting location, ship flight parameters, full level json etc
     *  - Score Data - whatever was uploaded to the leaderboard but in full - all the checkpoint splits etc that we
     *      can trigger at appropriate times
     *  - Input Data - individual axes and important functions in use each frame which we can apply to an identical
     *      physics object
     *  - Key Frame Data - occasional key frame to reset the the location, velocity etc at a given tick. This is to
     *      get around physics determinism to some extent.
     */
    public class Replay {
        public static readonly string TMPSaveDirectory = Path.Combine(Application.persistentDataPath, "Save", "Records", "Replays", "tmp");
        private static readonly string tmpShipParameterSaveLoc = Path.Combine(TMPSaveDirectory, "shipParameters.json");
        private static readonly string tmpLevelDataSaveLoc = Path.Combine(TMPSaveDirectory, "levelData.json");
        private static readonly string tmpShipProfileSaveLoc = Path.Combine(TMPSaveDirectory, "shipProfile.json");
        private static readonly string tmpScoreDataSaveLoc = Path.Combine(TMPSaveDirectory, "scoreData.json");
        private static readonly string tmpInputDataSaveLoc = Path.Combine(TMPSaveDirectory, "input.bin");
        private static readonly string tmpKeyFrameDataSaveLoc = Path.Combine(TMPSaveDirectory, "keyFrames.bin");
        
        private ReplayMode _mode;
        public ShipParameters ShipParameters { get; }
        public LevelData LevelData { get; }
        public ShipProfile ShipProfile { get; }
        public ScoreData ScoreData { get; private set; }
        
        // TODO: Can we abstract this better?
        public FileStream InputFileStream { get; }
        public FileStream KeyFrameFileStream { get; }

        public bool CanWrite => InputFileStream.CanWrite && KeyFrameFileStream.CanWrite;
        
        private Replay(ShipParameters shipParameters, LevelData levelData, ShipProfile shipProfile, ScoreData scoreData, FileStream inputFileStream, FileStream keyFrameFileStream) {
            ShipParameters = shipParameters;
            LevelData = levelData;
            ShipProfile = shipProfile;
            ScoreData = scoreData;
            InputFileStream = inputFileStream;
            KeyFrameFileStream = keyFrameFileStream;
        }

        /** Write the replay to a new folder with the completed score */
        public string Save(ScoreData scoreData) {
            ScoreData = scoreData;
            
            InputFileStream.Close();
            KeyFrameFileStream.Close();
            
            var shipParameterJson = ShipParameters.ToJsonString();
            var levelDataJson = LevelData.ToJsonString();
            var shipProfileJson = ShipProfile.ToJsonString();
            var scoreDataJson = ScoreData.ToJsonString();
            
            var writeFile = new Action<string, string>((string directory, string json) => {
                using var file = new FileStream(directory, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(file);
                writer.Write(json);
                writer.Flush();
            });

            writeFile(tmpShipParameterSaveLoc, shipParameterJson);
            writeFile(tmpLevelDataSaveLoc, levelDataJson);
            writeFile(tmpShipProfileSaveLoc, shipProfileJson);
            writeFile(tmpScoreDataSaveLoc, scoreDataJson);

            // TODO: pack into zipped container, copy to hashed folder, return file location
            return "C:/myshinyreplay.fdr";
        }

        /** Load an existing replay from a file */
        public static Replay LoadFromFilepath(string replayFilePath) {
            // TODO: Unpack to tmp folder, read in data
            
            var readFile = new Func<string, string>((string filename) => {
                var fileLoc = Path.Combine(Application.persistentDataPath, "Save", "Records", $"{filename}");
                using var file = new FileStream(fileLoc, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(file); 
                return reader.ReadToEnd();
            });

            var shipParameters = ShipModel.ShipParameters.FromJsonString(readFile(tmpShipParameterSaveLoc));
            var levelData = LevelData.FromJsonString(readFile(tmpLevelDataSaveLoc));
            var shipProfile = Player.ShipProfile.FromJsonString(readFile(tmpShipProfileSaveLoc));
            var scoreData = ScoreData.FromJsonString(readFile(tmpScoreDataSaveLoc));
            var inputFileStream = new FileStream(tmpInputDataSaveLoc, FileMode.Open, FileAccess.Read, FileShare.Read);
            var keyFrameFileStream = new FileStream(tmpKeyFrameDataSaveLoc, FileMode.Open, FileAccess.Read, FileShare.None);

            
            return new Replay(shipParameters, levelData, shipProfile, scoreData, inputFileStream, keyFrameFileStream);
        }

        /** Create a writeable stream in a temporary directory */
        public static Replay CreateNewWritable(ShipParameters shipParameters, LevelData levelData, ShipProfile shipProfile) {
            Directory.CreateDirectory(TMPSaveDirectory);
            
            if (File.Exists(tmpInputDataSaveLoc)) File.Delete(tmpInputDataSaveLoc);
            var inputFileStream= new FileStream(tmpInputDataSaveLoc, FileMode.Append, FileAccess.Write, FileShare.Read);
            
            if (File.Exists(tmpKeyFrameDataSaveLoc)) File.Delete(tmpKeyFrameDataSaveLoc);
            var keyFrameFileStream = new FileStream(tmpKeyFrameDataSaveLoc, FileMode.Append, FileAccess.Write, FileShare.Read);

            return new Replay(shipParameters, levelData, shipProfile, new ScoreData(), inputFileStream, keyFrameFileStream);
        }
    }
}