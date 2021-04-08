using UnityEngine;
using UnityEngine.InputSystem;

public class RebindSaveLoad : MonoBehaviour {
    public InputActionAsset actions;

    public void OnEnable()
    {
        var rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
            InputActionRebindingExtensions.LoadBindingOverridesFromJson(actions, rebinds);
    }

    public void OnDisable()
    {
        var rebinds = InputActionRebindingExtensions.SaveBindingOverridesAsJson(actions);
        PlayerPrefs.SetString("rebinds", rebinds);
    }
}
