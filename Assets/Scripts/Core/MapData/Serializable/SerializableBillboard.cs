using System;
using Gameplay;
using JetBrains.Annotations;

namespace Core.MapData.Serializable {
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

            if (!string.IsNullOrEmpty(billboardSpawner.Billboard.CustomMessage))
                serializableBillboard.customMessage = billboardSpawner.Billboard.CustomMessage;
            if (!billboardSpawner.BillboardData.Tint.Equals(billboardSpawner.Billboard.Tint))
                serializableBillboard.tintOverride = SerializableColor32.FromColor(billboardSpawner.Billboard.Tint);
            if (Math.Abs(billboardSpawner.BillboardData.ColorIntensity - billboardSpawner.Billboard.ColorIntensity) > 0.01f)
                serializableBillboard.tintIntensityOverride = billboardSpawner.Billboard.ColorIntensity;
            if (Math.Abs(billboardSpawner.BillboardData.ScrollSpeed - billboardSpawner.Billboard.ScrollSpeed) > 0.01f)
                serializableBillboard.scrollSpeedOverride = billboardSpawner.Billboard.ScrollSpeed;

            return serializableBillboard;
        }

        // convert this object into a string for hash generation purposes (that is, any information pertinent to the level format for competition purposes)
        public static string ToHashString(SerializableBillboard billboard) {
            return billboard.position.ToString() + billboard.rotation;
        }
    }
}