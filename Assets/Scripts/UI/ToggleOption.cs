using UI;
using UnityEngine;

public class ToggleOption : MonoBehaviour {
    public string Preference {
        get {
            var checkbox = GetComponentInChildren<Checkbox>(true);
            return checkbox ? checkbox.preference : "default-preference";
        }
    }

    public bool IsEnabled {
        get {
            var checkbox = GetComponentInChildren<Checkbox>(true);
            return checkbox ? checkbox.isChecked : false;
        }
        set {
            var checkbox = GetComponentInChildren<Checkbox>(true);
            if (checkbox != null) checkbox.isChecked = value;
        }
    }
}