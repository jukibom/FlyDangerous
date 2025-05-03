using Core.Player;
using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    public class ModifierBoostAttractor : MonoBehaviour, IModifier {
        public void ApplyModifierEffect(Rigidbody ship, ref AppliedEffects effects) {
            var distance = transform.position - ship.transform.position;
            var parameters = ship.gameObject.GetComponent<ShipPlayer>().ShipPhysics.FlightParameters;
            if (parameters.useAltBoosters) {
                var massNorm = parameters.mass / ShipParameters.Defaults.mass;
                effects.shipForce += Vector3.Lerp(Vector3.zero, massNorm * parameters.boosterForceMultiplier * distance.normalized * 1000000, 1 - distance.magnitude / 1000f);
            }
            else {
                effects.shipForce += Vector3.Lerp(Vector3.zero, distance.normalized * 1000000, 1 - distance.magnitude / 1000f);
            }
        }

        public void ApplyInitialEffect(Rigidbody ship, ref AppliedEffects effects) {}
    }
}