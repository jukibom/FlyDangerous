using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Core.MapData;
using Core.Player;
using Core.Scores;
using Core.ShipModel;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Core.Replays {
    /**
     * Replays are too large an unwieldy to be loaded into memory, instead we stream the important stuff to / from
     * the individual files.
     * Replays are made up of 4 files:
     * - Initial Data - starting location, ship flight parameters, full level json etc
     * - Score Data - whatever was uploaded to the leaderboard but in full - all the checkpoint splits etc that we
     * can trigger at appropriate times
     * - Input Data - individual axes and important functions in use each frame which we can apply to an identical
     * physics object
     * - Key Frame Data - occasional key frame to reset the the location, velocity etc at a given tick. This is to
     * get around physics determinism to some extent.
     */
    public class Replay {
        private const string ReplayMetaFileName = "replayMeta.json";
        private const string ShipParameterFileName = "shipParameters.json";
        private const string LevelDataFileName = "levelData.json";
        private const string ShipProfileFileName = "shipProfile.json";
        private const string ScoreDataFileName = "scoreData.json";
        private const string InputFrameFileName = "input.bin";
        private const string KeyFrameFileName = "keyFrames.bin";
        private const string ArchiveFileName = "archive.zip";
        public static readonly string ReplayDirectory = Path.Combine(Application.persistentDataPath, "Save", "Records", "Replays");
        private static readonly string tmpSaveDirectory = Path.Combine(ReplayDirectory, "tmp");
        private static readonly string tmpReplayMetaSaveLoc = Path.Combine(tmpSaveDirectory, ReplayMetaFileName);
        private static readonly string tmpShipParameterSaveLoc = Path.Combine(tmpSaveDirectory, ShipParameterFileName);
        private static readonly string tmpLevelDataSaveLoc = Path.Combine(tmpSaveDirectory, LevelDataFileName);
        private static readonly string tmpShipProfileSaveLoc = Path.Combine(tmpSaveDirectory, ShipProfileFileName);
        private static readonly string tmpScoreDataSaveLoc = Path.Combine(tmpSaveDirectory, ScoreDataFileName);
        private static readonly string tmpInputDataSaveLoc = Path.Combine(tmpSaveDirectory, InputFrameFileName);
        private static readonly string tmpKeyFrameDataSaveLoc = Path.Combine(tmpSaveDirectory, KeyFrameFileName);
        private static readonly string tmpArchiveDataSaveLoc = Path.Combine(ReplayDirectory, ArchiveFileName);

        [CanBeNull] private string _replayFilePath;

        private Replay(ReplayMeta replayMeta, ShipParameters shipParameters, LevelData levelData, ShipProfile shipProfile, ScoreData scoreData,
            Stream inputFrameStream, Stream keyFrameStream, [CanBeNull] string filePath) {
            ReplayMeta = replayMeta;
            ShipParameters = shipParameters;
            LevelData = levelData;
            ShipProfile = shipProfile;
            ScoreData = scoreData;
            InputFrameStream = inputFrameStream;
            KeyFrameStream = keyFrameStream;
            _replayFilePath = filePath;
        }

        public ReplayMeta ReplayMeta { get; }
        public ShipParameters ShipParameters { get; }
        public LevelData LevelData { get; }
        public ShipProfile ShipProfile { get; }
        public ScoreData ScoreData { get; private set; }
        public Stream InputFrameStream { get; }
        public Stream KeyFrameStream { get; }

        public bool CanWrite => InputFrameStream.CanWrite && KeyFrameStream.CanWrite;

        // generate a hash from the score hash and the creation date
        public string Hash => HashGenerator.ComputeSha256Hash(ScoreData.hash + ReplayMeta.CreationDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK"));


        /**
         * Write the replay to a new folder with the completed score.
         * This may fail!
         */
        public string Save(ScoreData scoreData) {
            ScoreData = scoreData;

            InputFrameStream.Close();
            KeyFrameStream.Close();
            InputFrameStream.Dispose();
            KeyFrameStream.Dispose();

            var replayMetaJson = ReplayMeta.ToJsonString();
            var shipParameterJson = ShipParameters.ToJsonString();
            var levelDataJson = LevelData.ToJsonString();
            var shipProfileJson = ShipProfile.ToJsonString();
            var scoreDataJson = ScoreData.ToJsonString();

            var writeFile = new Action<string, string>((directory, json) => {
                using var file = new FileStream(directory, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(file);
                writer.Write(json);
                writer.Flush();
            });

            writeFile(tmpReplayMetaSaveLoc, replayMetaJson);
            writeFile(tmpShipParameterSaveLoc, shipParameterJson);
            writeFile(tmpLevelDataSaveLoc, levelDataJson);
            writeFile(tmpShipProfileSaveLoc, shipProfileJson);
            writeFile(tmpScoreDataSaveLoc, scoreDataJson);

            // pack files in tmp into zip file
            // name the archive the SHA256 hash of the zipped file 
            // move the file into a folder named the same as the level hash

            if (File.Exists(tmpArchiveDataSaveLoc)) File.Delete(tmpArchiveDataSaveLoc);
            ZipFile.CreateFromDirectory(tmpSaveDirectory, tmpArchiveDataSaveLoc);

            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(tmpArchiveDataSaveLoc);

            using var reader = new StreamReader(fileStream);

            var fileName = HashGenerator.ComputeSha256Hash(reader.ReadToEnd()) + ".fdr";
            var folder = LevelData.LevelHash();
            var filePath = Path.Combine(ReplayDirectory, folder, fileName);
            fileStream.Close();
            fileStream.Dispose();

            // CreateDirectory
            Directory.CreateDirectory(Path.Combine(ReplayDirectory, folder));

            File.Move(tmpArchiveDataSaveLoc, filePath);
            Directory.Delete(tmpSaveDirectory, true);
            _replayFilePath = filePath;

            return fileName;
        }

        public void Delete() {
            if (_replayFilePath != null && File.Exists(_replayFilePath)) File.Delete(_replayFilePath);
        }

        /**
         * Load an existing replay from a replay archive .fdr file.
         * @throws This may fail with a DataException if archive is invalid or does not exist among all other file read access exceptions.
         */
        public static Replay LoadFromFilepath(string replayFilePath) {
            var archive = ZipFile.Open(replayFilePath, ZipArchiveMode.Read);

            if (archive == null) throw new DataException("Replay archive does not exist.");

            var readArchiveEntry = new Func<ZipArchive, string, string>((fromArchive, filename) => {
                var entry = fromArchive.GetEntry(filename);
                if (entry != null) {
                    var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }

                throw new DataException("Replay archive is not valid.");
            });

            var replayMeta = ReplayMeta.FromJsonString(readArchiveEntry(archive, ReplayMetaFileName));
            var shipParameters = ShipParameters.FromJsonString(readArchiveEntry(archive, ShipParameterFileName));
            var levelData = LevelData.FromJsonString(readArchiveEntry(archive, LevelDataFileName));
            var shipProfile = ShipProfile.FromJsonString(readArchiveEntry(archive, ShipProfileFileName));
            var scoreData = ScoreData.FromJsonString(readArchiveEntry(archive, ScoreDataFileName));

            var inputFrameEntry = archive.GetEntry(InputFrameFileName);
            var keyFrameEntry = archive.GetEntry(KeyFrameFileName);

            if (inputFrameEntry == null || keyFrameEntry == null) throw new DataException("Replay archive is not valid");

            var inputMemoryStream = new MemoryStream();
            inputFrameEntry.Open().CopyTo(inputMemoryStream);
            inputMemoryStream.Position = 0;

            var keyFrameMemoryStream = new MemoryStream();
            keyFrameEntry.Open().CopyTo(keyFrameMemoryStream);
            keyFrameMemoryStream.Position = 0;

            archive.Dispose();
            return new Replay(replayMeta, shipParameters, levelData, shipProfile, scoreData, inputMemoryStream, keyFrameMemoryStream, replayFilePath);
        }

        /**
         * Create a writeable stream in a temporary directory
         */
        public static Replay CreateNewWritable(ShipParameters shipParameters, LevelData levelData, ShipProfile shipProfile) {
            if (Directory.Exists(tmpSaveDirectory)) Directory.Delete(tmpSaveDirectory, true);
            Directory.CreateDirectory(tmpSaveDirectory);

            // V1 replay data
            var replayMeta = ReplayMeta.Version100(levelData);

            if (File.Exists(tmpInputDataSaveLoc)) File.Delete(tmpInputDataSaveLoc);
            var inputFileStream = new FileStream(tmpInputDataSaveLoc, FileMode.Append, FileAccess.Write, FileShare.Read);

            if (File.Exists(tmpKeyFrameDataSaveLoc)) File.Delete(tmpKeyFrameDataSaveLoc);
            var keyFrameFileStream = new FileStream(tmpKeyFrameDataSaveLoc, FileMode.Append, FileAccess.Write, FileShare.Read);

            return new Replay(replayMeta, shipParameters, levelData, shipProfile, new ScoreData(), inputFileStream, keyFrameFileStream, null);
        }

        /**
         * Fetch all replays on the file system for the specified level
         */
        public static List<Replay> ReplaysForLevel(LevelData levelData) {
            if (Directory.Exists(Path.Combine(ReplayDirectory, levelData.LevelHash())))
                return Directory.GetFiles(Path.Combine(ReplayDirectory, levelData.LevelHash()))
                    .ToList()
                    .ConvertAll(LoadFromFilepath);
            return new List<Replay>();
        }
    }
}