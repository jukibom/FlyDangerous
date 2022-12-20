using System;
using System.Collections;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace GameUI.GameModes {
    [RequireComponent(typeof(CanvasGroup))]
    public class HoldBoostButtonText : MonoBehaviour {
        private CanvasGroup _canvasGroup;
        private Coroutine _holdBoostTextFadeCoroutine;

        private void Awake() {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }

        public void Reset() {
            _canvasGroup.alpha = 0;
        }

        public void ShowHoldBoostText() {
            if (_holdBoostTextFadeCoroutine != null) StopCoroutine(_holdBoostTextFadeCoroutine);
            _holdBoostTextFadeCoroutine = StartCoroutine(
                FadeText(
                    _canvasGroup.alpha,
                    1,
                    0.75f
                )
            );
        }

        public void HideHoldBoostText() {
            if (_holdBoostTextFadeCoroutine != null) StopCoroutine(_holdBoostTextFadeCoroutine);
            _holdBoostTextFadeCoroutine = StartCoroutine(
                FadeText(
                    _canvasGroup.alpha,
                    0,
                    0.25f
                )
            );
        }

        private IEnumerator FadeText(float alphaStart, float alphaEnd, float time, [CanBeNull] Action onComplete = null) {
            float t = 0;
            while (t <= time) {
                t += Time.deltaTime;
                _canvasGroup.alpha = t.Remap(0, time, alphaStart, alphaEnd);
                yield return new WaitForEndOfFrame();
            }

            onComplete?.Invoke();
        }
    }
}