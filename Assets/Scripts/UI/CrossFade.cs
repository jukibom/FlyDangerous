using System.Collections;
using UnityEngine;

/**
 * The animation tools in unity can eat the biggest bucket of dicks so let's just do this manually
 */
public class CrossFade : MonoBehaviour {
    [SerializeField] private CanvasGroup blackImage;
    [SerializeField] private float fadeTimeSeconds = 1;
    private Coroutine _fader;
    private float _screenAlpha = 1;

    public void FadeFromBlack() {
        if (_fader != null) StopCoroutine(_fader);
        _fader = StartCoroutine(FadeIn());
    }

    public void FadeToBlack() {
        if (_fader != null) StopCoroutine(_fader);
        _fader = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn() {
        _screenAlpha = 1;

        while (_screenAlpha > 0) {
            _screenAlpha -= 1 / fadeTimeSeconds * Time.deltaTime;
            blackImage.alpha = _screenAlpha;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator FadeOut() {
        _screenAlpha = 0;

        while (_screenAlpha < 1) {
            _screenAlpha += 1 / fadeTimeSeconds * Time.deltaTime;
            blackImage.alpha = _screenAlpha;
            yield return new WaitForEndOfFrame();
        }
    }
}