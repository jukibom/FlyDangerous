using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    public class ModifierBoostAttractor : MonoBehaviour, IModifier {
        public void ApplyModifierEffect(Rigidbody ship, ref AppliedEffects effects) {
            var distance = transform.position - ship.transform.position;
            effects.shipForce += Vector3.Lerp(Vector3.zero, distance.normalized * 1000000, 1 - distance.magnitude / 1000f);
        }
    }
}