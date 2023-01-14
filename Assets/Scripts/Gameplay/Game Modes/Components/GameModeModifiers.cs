using System.Collections.Generic;
using System.Linq;
using Core.MapData.Serializable;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    public class GameModeModifiers : MonoBehaviour {
        [SerializeField] private ModifierSpawner modifierPrefab;

        public List<ModifierSpawner> ModifierSpawners { get; private set; } = new();

        public void RefreshModifierSpawners() {
            ModifierSpawners.Clear();
            ModifierSpawners = GetComponentsInChildren<ModifierSpawner>().ToList();
        }

        public ModifierSpawner AddModifier(SerializableModifier serializableModifier) {
            var modifierSpawner = Instantiate(modifierPrefab, transform);
            modifierSpawner.Deserialize(serializableModifier);
            ModifierSpawners.Add(modifierSpawner);
            return modifierSpawner;
        }
    }
}