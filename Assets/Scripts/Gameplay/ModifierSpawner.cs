using System;
using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.MapData.Serializable;
using Core.ShipModel.Modifiers.Boost;
using Core.ShipModel.Modifiers.Water;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;

namespace Gameplay {
    public class ModifierSpawner : MonoBehaviour {
        [Dropdown("GetModifierTypes")] [OnValueChanged("RefreshFromModifierData")] [SerializeField]
        private string modifierType;

        [SerializeField] private ModifierBoost modifierBoost;

        [ShowIf("ShowBoostAttributes")] [Range(1000, 50000)] [OnValueChanged("SetModifierAttributes")] [SerializeField]
        private float boostTrailLength = 15000;

        private ModifierData _modifierData;

        public float BoostTrailLength => boostTrailLength;

        public ModifierData ModifierData {
            get {
                if (_modifierData == null) _modifierData = ModifierType.FromString(modifierType).ModifierData;
                return _modifierData;
            }
            private set {
                _modifierData = value;
                modifierType = ModifierType.FromString(_modifierData.Name).Name;

                if (_modifierData is BoostModifierData boostModifierData) boostTrailLength = boostModifierData.BoostLengthMeters;

                ResetAll();
            }
        }

        public void Deserialize(SerializableModifier serializedData) {
            var serializedModifierType = ModifierType.FromString(serializedData.type);
            ModifierData = serializedModifierType.ModifierData;

            var modifierTransform = transform;
            modifierTransform.position = serializedData.position.ToVector3();
            modifierTransform.rotation = Quaternion.Euler(serializedData.rotation.ToVector3());

            // overrides
            if (ModifierData is BoostModifierData boostModifierData)
                boostTrailLength = serializedData.boostTrailLengthMeters != null &&
                                   Math.Abs(serializedData.boostTrailLengthMeters.Value - boostModifierData.BoostLengthMeters) > 0.01f
                    ? serializedData.boostTrailLengthMeters.Value
                    : boostModifierData.BoostLengthMeters;

            ResetAll();
            SetModifierAttributes();
        }

        [UsedImplicitly]
        private List<string> GetModifierTypes() {
            return ModifierType.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private void RefreshFromModifierData() {
            ModifierData = ModifierType.FromString(modifierType).ModifierData;
            ResetAll();
        }

        [UsedImplicitly]
        private bool ShowBoostAttributes() {
            return modifierType.Equals(ModifierType.BoostModifierType.Name);
        }

        [Button("Reset to type default")]
        private void ResetAll() {
            modifierBoost.gameObject.SetActive(false);

            switch (modifierType) {
                case "Boost":
                    modifierBoost.gameObject.SetActive(true);
                    modifierBoost.UseDistortion = FindObjectOfType<ModifierWater>() == null;
                    break;
            }
        }

        private void SetModifierAttributes() {
            switch (modifierType) {
                case "Boost":
                    modifierBoost.BoostStreamLengthMeters = boostTrailLength;
                    break;
            }
        }
    }
}