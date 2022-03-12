using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.MapData {
    public class LevelDataVector3<T> {
        public T x;
        public T y;
        public T z;

        public LevelDataVector3() {
        }

        public LevelDataVector3(T x, T y, T z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString() {
            return "[ " + x + ", " + y + ", " + z + " ]";
        }
    }

    public class CheckpointLocation {
        public LevelDataVector3<float> position;
        public LevelDataVector3<float> rotation;
        public CheckpointType type;
    }

    public class LevelData {
        public float authorTimeTarget;

        public List<CheckpointLocation> checkpoints;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Environment environment = Environment.NoonClear;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public GameType gameType = GameType.FreeRoam;

        public LevelDataVector3<float> gravity = new(0, 0, 0);

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Location location = Location.Space;

        public string name = "";
        public LevelDataVector3<float> startPosition = new();
        public LevelDataVector3<float> startRotation = new();

        public string terrainSeed = "";
        public int version = 1;

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