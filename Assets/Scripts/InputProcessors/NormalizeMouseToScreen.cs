using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class NormalizeMouseToScreen : InputProcessor<float> {
    public enum ScreenDimension {
        Width,
        Height
    }

    [Tooltip("Screen dimension to normalize by")]
    public ScreenDimension dimension = ScreenDimension.Width;

#if UNITY_EDITOR
    static NormalizeMouseToScreen() {
        Initialize();
    }
#endif

    public override float Process(float value, InputControl control) {
        var dimensionToNormalize = dimension == ScreenDimension.Width ? Screen.width : Screen.height;

        // Debug.Log("processor " + dimension + " (" + dimensionToNormalize + "), " + value + " => " + value / dimensionToNormalize);
        return value / dimensionToNormalize;
    }

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
        InputSystem.RegisterProcessor<NormalizeMouseToScreen>();
    }
}