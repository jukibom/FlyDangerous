using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.MapData {
    public class LevelDataVector3 {
        public float x;
        public float y;
        public float z;

        public LevelDataVector3() {
        }

        public LevelDataVector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3() {
            return new Vector3(
                x,
                y,
                z
            );
        }

        public override string ToString() {
            return "[ " +
                   x.ToString(CultureInfo.InvariantCulture) + ", " +
                   y.ToString(CultureInfo.InvariantCulture) + ", " +
                   z.ToString(CultureInfo.InvariantCulture) +
                   " ]";
        }

        public static LevelDataVector3 FromVector3(Vector3 value) {
            return new LevelDataVector3(value.x, value.y, value.z);
        }
    }

    public class CheckpointLocation {
        public LevelDataVector3 position;
        public LevelDataVector3 rotation;
        public CheckpointType type;
    }

    public class LevelData {
        public float authorTimeTarget = 0f;

        public List<CheckpointLocation> checkpoints = new();

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Environment environment = Environment.NoonClear;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public GameType gameType = GameType.FreeRoam;

        public LevelDataVector3 gravity = new(0, 0, 0);

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Location location = Location.Space;

        public string musicTrack = "";

        public string name = "";
        public LevelDataVector3 startPosition = new();
        public LevelDataVector3 startRotation = new();

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
        public static void WriteLevelData(this NetworkWriter writer, LevelData levelData) {
            writer.WriteString(levelData.ToJsonString());
        }

        public static LevelData ReadLevelData(this NetworkReader reader) {
            return LevelData.FromJsonString(reader.ReadString());
        }
    }
}