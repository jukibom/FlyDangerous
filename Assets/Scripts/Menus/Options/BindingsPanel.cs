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
            // clear all protected secondary bindings by reverting to default - gross but it works!
            ResetBindingsToDefault();

            foreach (var rebindAction in GetComponentsInChildren<RebindAction>(true)) {
                // ignore protected bindings (we should have removed secondary binds by setting to default anyway - it'll do)
                if (rebindAction.IsProtected) continue;

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