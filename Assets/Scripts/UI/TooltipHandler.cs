using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Misc;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class TooltipHandler : MonoBehaviour {
    [SerializeField] private GameObject parentGameObject;
    [SerializeField] private Text textField;
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
        yield return SetBestFitFontStyle();

        // if the text is going to span multiple lines, align it to the top such that it fills out smoothly rather than judder onto a new line.
        // yes sometimes this is the dumb shit you find yourself doing at 1:40am don't you dare judge me
        textField.alignment = textField.cachedTextGenerator.lines.Count > 1 ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;

        var time = 0f;
        while (time < timeToDisplaySeconds) {
            // get the character offset via the animation curve as a factor of time elapsed
            var charIndex = MathfExtensions.Remap(0, 1, 0, _tooltipText.Length, animationCurve.Evaluate(time / timeToDisplaySeconds));
            textField.text = _tooltipText.Substring(0, (int)Mathf.Floor(charIndex));

            // increment time (using unscaled - the game may be paused!) and wait one animation frame
            time += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        // just in case the animation curve, floor and int conversion caused us to miss a character or two, set the final string 
        textField.text = _tooltipText;
    }

    // set the text field font size required to calculated best fit such that we can animate in the text without the font size changing as text fills in.
    // not gonna lie this is pretty gross! If there's a better way I'd love to know (this sets and hides the text for onw frame, pulls the size then resets)
    private IEnumerator SetBestFitFontStyle() {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) {
            textField.resizeTextForBestFit = true;
            textField.text = _tooltipText;
            textField.color = new Color(0, 0, 0, 0);
            yield return new WaitForEndOfFrame();
            textField.color = _originalColor;
            var fontSize = textField.cachedTextGenerator.fontSizeUsedForBestFit;
            textField.fontSize = (int)(fontSize / canvas.scaleFactor);
            textField.resizeTextForBestFit = false;
            textField.text = "";
        }
    }
}