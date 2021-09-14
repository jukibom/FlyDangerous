using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Core {
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

    public enum Location {
        [Description("Space")]
        [IgnoreDataMember]
        NullSpace,
        
        [Description("Test Space Station")]
        TestSpaceStation,
        
        [Description("Terrain Flat")]
        TerrainV1,
        
        [Description("Terrain Canyons")]
        TerrainV2,
        
        [Description("Terrain Biome")]
        TerrainV3
    }

    public enum Environment {
        [Description("Planet Orbit (Top)")]
        PlanetOrbitBottom,
        
        [Description("Planet Orbit (Bottom)")]
        PlanetOrbitTop,
        
        [Description("Sunrise Clear")]
        SunriseClear,
        
        [Description("Noon Clear")]
        NoonClear,
        
        [Description("Noon Cloudy")]
        NoonCloudy,
        
        [Description("Noon Stormy")]
        NoonStormy,
        
        [Description("Sunset Clear")]
        SunsetClear,
        
        [Description("Sunset Cloudy")]
        SunsetCloudy,
        
        [Description("Night Clear")]
        NightClear,
        
        [Description("Night Cloudy")]
        NightCloudy,
    }

    public enum GameType {
        [Description("Free Roam")]
        FreeRoam,
        
        [Description("Time Trial")]
        TimeTrial,
        
        [Description("Race (Sprint)")]
        RaceSprint,
        
        [Description("Race (Laps)")]
        RaceLaps,
        
        [Description("Hoon Attack")]
        HoonAttack,
    }

    public class CheckpointLocation {
        public CheckpointType type;
        public LevelDataVector3<float> position;
        public LevelDataVector3<float> rotation;
    }
    
    public class LevelData {
        public int version = 1;     // if the version is not this then we'll use the legacy terrain
        public string name = "";
        public Location location = Location.NullSpace;
        public Environment environment = Environment.NoonClear;
        public string terrainSeed = "";
        public LevelDataVector3<float> startPosition = new LevelDataVector3<float>();
        public LevelDataVector3<float> startRotation = new LevelDataVector3<float>();
        public GameType gameType = GameType.FreeRoam;
        [CanBeNull] public List<CheckpointLocation> checkpoints;

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