using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    // Controller of boost modifier object, used to set the various params and serialise etc
    public class ModifierBoost : MonoBehaviour {
        [SerializeField] private ModifierBoostAttractor modifierBoostAttractor;
        [SerializeField] private ModifierBoostThrust modifierBoostThrust;
        [SerializeField] private ModifierBoostStream modifierBoostStream;
    }
}