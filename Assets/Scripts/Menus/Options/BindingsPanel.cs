using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Menus.Options {
    public class BindingsPanel : MonoBehaviour {
        public void ResetBindingsToDefault() {
            Preferences.Instance.SetString("inputBindings", "");
            Game.Instance.LoadBindings();
        }

        public void ClearAllBindings() {
            foreach (var rebindAction in GetComponentsInChildren<RebindAction>(true)) {
                var actionReference = rebindAction.actionReference;
                for (var i = 0; i < actionReference.action.bindings.Count; i++) {
                    var binding = actionReference.action.bindings[i];
                    binding.overridePath = "";
                    actionReference.action.ChangeBinding(binding);
                    actionReference.action.ApplyBindingOverride(binding);
                    actionReference.action.ApplyBindingOverride("");
                }

                rebindAction.UpdateBindingDisplay();
            }
        }
    }
}