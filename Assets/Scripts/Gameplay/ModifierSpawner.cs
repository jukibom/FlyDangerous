using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.ShipModel.Modifiers.Boost;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;

namespace Gameplay {
    public class ModifierSpawner : MonoBehaviour {
        [Dropdown("GetModifierTypes")] [OnValueChanged("RefreshFromModifierData")] [SerializeField]
        private string modifierType;

        [SerializeField] private ModifierBoost modifierBoost;

        private ModifierData _modifierData;

        public ModifierData ModifierData {
            get {
                if (_modifierData == null) _modifierData = ModifierType.FromString(modifierType).ModifierData;
                return _modifierData;
            }
            private set {
                _modifierData = value;
                modifierType = ModifierType.FromString(_modifierData.Name).Name;

                if (_modifierData is BoostModifierData boostModifier) {
                    // TODO: boost specific crap
                }
            }
        }

        private void ResetAll() {
            // TODO
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
    }
}