using System;
using System.Collections.Generic;
using System.ComponentModel;
using Audio;
using Core.MapData.Serializable;
using Core.ShipModel;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.MapData {
    public class LevelData {
        [DefaultValue(1)] public int version = 1;
        public string name = "";

        public string author = "";

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

        public ShipParameters shipParameters = ShipParameters.Defaults;

        public SerializableVector3 startPosition = new();
        public SerializableVector3 startRotation = new();

        [CanBeNull] public List<SerializableCheckpoint> checkpoints = null;

        [CanBeNull] public List<SerializableBillboard> billboards = null;

        [CanBeNull] public List<SerializableModifier> modifiers = null;


        public string LevelHash() {
            // generate the filename from a hash combination of name, checkpoints and location - this way they'll always be unique.
            var checkpointText = "";
            checkpoints?.ConvertAll(SerializableCheckpoint.ToHashString)
                .ForEach(checkpointString => checkpointText += checkpointString);

            var billboardsText = "";
            billboards?.ConvertAll(SerializableBillboard.ToHashString)
                .ForEach(billboardString => billboardsText += billboardString);

            var modifierText = "";
            modifiers?.ConvertAll(SerializableModifier.ToHashString)
                .ForEach(modifierString => modifierText += modifierString);

            var shipParametersText = ShipParameters.ToHashString(shipParameters);

            // TODO: geometry v_v
            var geometryText = "";
            // geometry?.ConvertAll(SerializableGeometry.ToHashString)
            //     .ForEach(geometryString => geometryText += geometryString);

            var hash = HashGenerator.ComputeSha256Hash(
                checkpointText + billboardsText + modifierText + shipParametersText + geometryText + location.Name);

            // Map lookup for old hash algorithm
            return LevelDataHelper.OldMapLookup.TryGetValue(hash, out var oldHash) ? oldHash : hash;
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