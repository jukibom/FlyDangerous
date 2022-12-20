using System;
using System.Collections.Generic;
using Gameplay;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.MapData {
    public class SerializeableCheckpoint {
        public SerializableVector3 position;
        public SerializableVector3 rotation;
        public CheckpointType type;

        public static SerializeableCheckpoint FromCheckpoint(Checkpoint checkpoint) {
            var checkpointLocation = new SerializeableCheckpoint();
            var transform = checkpoint.transform;
            checkpointLocation.position = SerializableVector3.FromVector3(transform.localPosition);
            checkpointLocation.rotation = SerializableVector3.FromVector3(transform.rotation.eulerAngles);
            checkpointLocation.type = checkpoint.Type;
            return checkpointLocation;
        }
    }

    public class LevelData {
        public float authorTimeTarget = 0f;

        public List<SerializeableCheckpoint> checkpoints = new();

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Environment environment = Environment.NoonClear;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public GameType gameType = GameType.FreeRoam;

        public SerializableVector3 gravity = new(0, 0, 0);

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Location location = Location.Space;

        public string musicTrack = "";

        public string name = "";
        public SerializableVector3 startPosition = new();
        public SerializableVector3 startRotation = new();

        public string terrainSeed = "";
        public int version = 1;

        public string LevelHash() {
            // generate the filename from a hash combination of name, checkpoints and location - this way they'll always be unique.
            var checkpointStrings =
                checkpoints.ConvertAll(checkpoint => checkpoint.position.ToString() + checkpoint.rotation);
            var checkpointText = "";
            foreach (var checkpointString in checkpointStrings) checkpointText += checkpointString;
            return HashGenerator.ComputeSha256Hash(
                name + checkpointText + location.Name);
        }

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static LevelData FromJsonString(string json) {
            try {
                return JsonConvert.DeserializeObject<LevelData>(json);
            }
            catch (Exception e) {
#if UNITY_EDITOR
                Debug.LogWarning(e.Message);
#endif
                return null;
            }
        }
    }

    // Level data network serialisation 
    public static class LevelDataReaderWriter {
        [UsedImplicitly]
        public static void WriteLevelData(this NetworkWriter writer, LevelData levelData) {
            writer.WriteString(levelData.ToJsonString());
        }

        [UsedImplicitly]
        public static LevelData ReadLevelData(this NetworkReader reader) {
            return LevelData.FromJsonString(reader.ReadString());
        }
    }
}