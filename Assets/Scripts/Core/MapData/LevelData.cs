using System;
using System.Collections.Generic;
using Gameplay;
using JetBrains.Annotations;
using Mirror;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.MapData {
    public class SerializebleCheckpoint {
        public SerializableVector3 position;
        public SerializableVector3 rotation;
        public CheckpointType type;

        public static SerializebleCheckpoint FromCheckpoint(Checkpoint checkpoint) {
            var checkpointLocation = new SerializebleCheckpoint();
            var transform = checkpoint.transform;
            checkpointLocation.position = SerializableVector3.FromVector3(transform.localPosition);
            checkpointLocation.rotation = SerializableVector3.FromVector3(transform.rotation.eulerAngles);
            checkpointLocation.type = checkpoint.Type;
            return checkpointLocation;
        }
    }

    public class SerializableBillboard {
        public SerializableVector3 position;
        public SerializableVector3 rotation;
        public string type;

        [CanBeNull] public SerializableColor32 tintOverride;
        public float? tintIntensityOverride;
        [CanBeNull] public string customMessage;
        public float? scrollSpeedOverride;

        public static SerializableBillboard FromBillboardSpawner(BillboardSpawner billboardSpawner) {
            var serializableBillboard = new SerializableBillboard();
            var transform = billboardSpawner.transform;
            serializableBillboard.position = SerializableVector3.FromVector3(transform.localPosition);
            serializableBillboard.rotation = SerializableVector3.FromVector3(transform.rotation.eulerAngles);
            serializableBillboard.type = billboardSpawner.BillboardData.Name;

            if (billboardSpawner.BillboardData.Message != "")
                serializableBillboard.customMessage = billboardSpawner.Billboard.CustomMessage;
            if (!billboardSpawner.BillboardData.Tint.Equals(billboardSpawner.Billboard.Tint))
                serializableBillboard.tintOverride = SerializableColor32.FromColor(billboardSpawner.Billboard.Tint);
            if (Math.Abs(billboardSpawner.BillboardData.ColorIntensity - billboardSpawner.Billboard.ColorIntensity) > 0.01f)
                serializableBillboard.tintIntensityOverride = billboardSpawner.Billboard.ColorIntensity;
            if (Math.Abs(billboardSpawner.BillboardData.ScrollSpeed - billboardSpawner.Billboard.ScrollSpeed) > 0.01f)
                serializableBillboard.scrollSpeedOverride = billboardSpawner.Billboard.ScrollSpeed;

            return serializableBillboard;
        }
    }

    public class LevelData {
        public float authorTimeTarget = 0f;

        public SerializableVector3 startPosition = new();
        public SerializableVector3 startRotation = new();

        public string musicTrack = "";
        public string name = "";

        [CanBeNull] public List<SerializebleCheckpoint> checkpoints = null;

        [CanBeNull] public List<SerializableBillboard> billboards = null;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Environment environment = Environment.NoonClear;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public GameType gameType = GameType.FreeRoam;

        [JsonConverter(typeof(FdEnumJsonConverter))]
        public Location location = Location.Space;

        [CanBeNull] public SerializableVector3 gravity = null;
        [CanBeNull] public string terrainSeed = null;
        public int version = 1;

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