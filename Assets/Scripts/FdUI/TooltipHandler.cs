using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace FdUI {
    public class TooltipHandler : MonoBehaviour {
        [SerializeField] private GameObject parentGameObject;
        [SerializeField] private TMPro.TMP_Text textField;
        [SerializeField] private float timeToDisplaySeconds;
        [SerializeField] private AnimationCurve animationCurve;
        private readonly Color _originalColor = Color.white;
        private Coroutine _tooltipAnimator;
        private List<FdTooltip> _tooltipHandlers;

        private string _tooltipText;

        private void OnEnable() {
            _tooltipHandlers = parentGameObject.GetComponentsInChildren<FdTooltip>(true).ToList();
            foreach (var tooltipHandler in _tooltipHandlers) tooltipHandler.OnTextChange += UpdateText;
        }

        private void OnDisable() {
            foreach (var tooltipHandler in _tooltipHandlers) tooltipHandler.OnTextChange -= UpdateText;
        }

        private void UpdateText(string toolTipText) {
            // ensure we don't repeat the animation for highlighting an element in a group with a global tooltop
            if (_tooltipText != toolTipText) {
                _tooltipText = toolTipText;
                if (_tooltipAnimator != null) StopCoroutine(_tooltipAnimator);
                _tooltipAnimator = StartCoroutine(TextUpdateAnimation());
            }
        }

        // Animated the text appearing in the tooltip over the specified timeToDisplaySeconds following the specified animationCurve.
        private IEnumerator TextUpdateAnimation() {
            textField.enableAutoSizing = true;
            textField.text = _tooltipText;
            textField.ForceMeshUpdate();

            float bestFitSize = textField.fontSize;

            textField.enableAutoSizing = false;
            textField.fontSize = bestFitSize;
            
            // if the text is going to span multiple lines, align it to the top such that it fills out smoothly rather than judder onto a new line.
            // yes, sometimes this is the dumb shit you find yourself doing at 1:40am don't you dare judge me
            // and again at 9pm at night while upgrading to TMP, apparently
            textField.alignment = textField.textInfo.lineCount > 1 ? TMPro.TextAlignmentOptions.TopLeft : TMPro.TextAlignmentOptions.Left;
            
            // Animate visibility one character at a time.
            textField.maxVisibleCharacters = 0;
            
            var time = 0f;
            while (time < timeToDisplaySeconds) {
                // get the character offset via the animation curve as a factor of time elapsed
                var charIndex = animationCurve.Evaluate(time / timeToDisplaySeconds).Remap(0, 1, 0, _tooltipText.Length);
                // textField.text = _tooltipText.Substring(0, (int)Mathf.Floor(charIndex));

                textField.maxVisibleCharacters = (int)Mathf.Floor(charIndex);
                
                // increment time (using unscaled - the game may be paused!) and wait one animation frame
                time += Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }

            // This is, believe it or not, the default. Curious.
            textField.maxVisibleCharacters = 99999;
            
            // just in case the animation curve, floor and int conversion caused us to miss a character or two, set the final string 
            textField.text = _tooltipText;
        }
    }
}

