using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    public class ModifierBoostStream : MonoBehaviour, IModifier {
        public void ApplyModifierEffect(Rigidbody ship, ref AppliedEffects effects) {
            var streamTransform = transform;
            var distance = streamTransform.position - ship.transform.position;
            effects.shipForce += Vector3.Lerp(Vector3.zero, streamTransform.forward * 500000, 1 - distance.magnitude / 14500f);
        }
    }
}