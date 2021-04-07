using UnityEngine;
using UnityEngine.InputSystem;

public class RebindSaveLoad : MonoBehaviour
{
    private InputActionAsset m_Actions;
    void Start() {
        m_Actions = GlobalGameState.Actions.asset;
    }

    public void OnEnable()
    {
        var rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
            m_Actions.LoadBindingOverridesFromJson(rebinds);
    }

    public void OnDisable()
    {
        var rebinds = m_Actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
    }
}
