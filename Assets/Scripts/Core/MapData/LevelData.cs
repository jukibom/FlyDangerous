using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public LevelDataVector3() {}
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
        public CheckpointType type;
        public LevelDataVector3<float> position;
        public LevelDataVector3<float> rotation;
    }
    
    public class LevelData {
        public int version = 1;
        public string name = "";
        
        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Location location = Location.Space;
        
        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Environment environment = Environment.NoonClear;
        
        [JsonConverter(typeof(FdEnumJsonConverter))]
        public GameType gameType = GameType.FreeRoam;
        
        public string terrainSeed = "";
        public LevelDataVector3<float> gravity = new LevelDataVector3<float>(0, 0, 0);
        public LevelDataVector3<float> startPosition = new LevelDataVector3<float>();
        public LevelDataVector3<float> startRotation = new LevelDataVector3<float>();

        public float authorTimeTarget;
        
        [CanBeNull] public List<CheckpointLocation> checkpoints;
        
        public string NameSlug => name.Replace(" ", "-").ToLower();

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