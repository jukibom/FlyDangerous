using System;
using System.Collections.Generic;
using Audio;
using Core.MapData.Serializable;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.MapData {
    public class LevelData {
        public int version = 1;
        public string name = "";

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public GameType gameType = GameType.FreeRoam;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Environment environment = Environment.NoonClear;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Location location = Location.Space;

        [CanBeNull] public string terrainSeed = null;

        [CanBeNull] public SerializableVector3 gravity = null;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public MusicTrack musicTrack = MusicTrack.None;

        public float authorTimeTarget;


        public SerializableVector3 startPosition = new();
        public SerializableVector3 startRotation = new();

        [CanBeNull] public List<SerializableCheckpoint> checkpoints = null;

        [CanBeNull] public List<SerializableBillboard> billboards = null;


        public string LevelHash() {
            // generate the filename from a hash combination of name, checkpoints and location - this way they'll always be unique.
            var checkpointText = "";
            if (checkpoints != null) {
                var checkpointStrings =
                    checkpoints.ConvertAll(checkpoint => checkpoint.position.ToString() + checkpoint.rotation);
                foreach (var checkpointString in checkpointStrings) checkpointText += checkpointString;
            }

            // TODO: billboards, modifiers, geo

            return HashGenerator.ComputeSha256Hash(
                name + checkpointText + location.Name);
        }

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        [CanBeNull]
        public static LevelData FromJsonString(string json) {
            try {
                return JsonConvert.DeserializeObject<LevelData>(json,
                    new JsonSerializerSettings {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
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