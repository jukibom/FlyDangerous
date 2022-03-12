using UnityEngine;
using UnityEngine.UI;

public class DropdownOption : MonoBehaviour {
    public string Preference = "default-preference";
    public bool savePreference = true;

    [SerializeField] private Dropdown dropdown;

    public string Value {
        get => dropdown.options[dropdown.value].text.ToLower();
        set { dropdown.value = dropdown.options.FindIndex(val => val.text.ToLower() == value.ToLower()); }
    }
}