using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    // Controller of boost modifier object, used to set the various params and serialise etc
    [ExecuteAlways]
    public class ModifierBoost : MonoBehaviour {
        [SerializeField] private ModifierBoostAttractor modifierBoostAttractor;
        [SerializeField] private ModifierBoostThrust modifierBoostThrust;
        [SerializeField] private ModifierBoostStream modifierBoostStream;

        [Range(1000, 50000)] [SerializeField] private float boostStreamLengthMeters;

        private void Awake() {
            boostStreamLengthMeters = modifierBoostStream.TrailLengthMeters;
        }

        private void OnValidate() {
            modifierBoostStream.TrailLengthMeters = boostStreamLengthMeters;
        }
    }
}