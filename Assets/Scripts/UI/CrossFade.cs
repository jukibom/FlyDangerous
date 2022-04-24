using Misc;
using UnityEngine;

/**
 * The animation tools in unity can eat the biggest bucket of dicks so let's just do this manually
 */
public class CrossFade : MonoBehaviour {
    [SerializeField] private CanvasGroup blackImage;
    [SerializeField] private float fadeTimeSeconds = 1;
    private Coroutine _fader;

    public void FadeFromBlack() {
        if (_fader != null) StopCoroutine(_fader);
        blackImage.alpha = 1;
        _fader = StartCoroutine(YieldExtensions.SimpleAnimationTween(val => blackImage.alpha = 1 - val, fadeTimeSeconds));
    }

    public void FadeToBlack() {
        if (_fader != null) StopCoroutine(_fader);
        blackImage.alpha = 0;
        _fader = StartCoroutine(YieldExtensions.SimpleAnimationTween(val => blackImage.alpha = val, fadeTimeSeconds));
    }
}