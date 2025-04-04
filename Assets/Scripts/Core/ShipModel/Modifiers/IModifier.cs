using UnityEngine;

namespace Core.ShipModel.Modifiers {
    public interface IModifier {
        public void ApplyModifierEffect(Rigidbody ship, ref AppliedEffects effects);
        public void ApplyInitialEffect(Rigidbody ship, ref AppliedEffects effects);
    }
}