using System;
using Gameplay;

namespace Core.MapData.Serializable {
    public class SerializableModifier {
        public SerializableVector3 position;
        public SerializableVector3 rotation;
        public string type;

        public float? boostTrailLengthMeters;

        public static SerializableModifier FromModifierSpawner(ModifierSpawner modifierSpawner) {
            SerializableModifier serializableModifier = new();
            var transform = modifierSpawner.transform;
            serializableModifier.position = SerializableVector3.FromVector3(transform.localPosition);
            serializableModifier.rotation = SerializableVector3.FromVector3(transform.rotation.eulerAngles);
            serializableModifier.type = modifierSpawner.ModifierData.Name;

            if (modifierSpawner.ModifierData is BoostModifierData boostModifierData &&
                Math.Abs(boostModifierData.BoostLengthMeters - modifierSpawner.BoostTrailLength) > 1)
                serializableModifier.boostTrailLengthMeters = modifierSpawner.BoostTrailLength;
            return serializableModifier;
        }

        // convert this object into a string for hash generation purposes (that is, any information pertinent to the level format for competition purposes)
        public static string ToHashString(SerializableModifier modifier) {
            return modifier.position.ToString() + modifier.rotation + modifier.type + modifier.boostTrailLengthMeters;
        }
    }
}