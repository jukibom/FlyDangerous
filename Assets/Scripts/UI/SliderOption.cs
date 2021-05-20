using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderOption : MonoBehaviour {
    public string preference = "default-preference";

    [SerializeField] private InputField numberTextBox;
    [SerializeField] private Slider slider;
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 0f;
    
    public float Value {
        get => slider.value;
        set => slider.value = value;
    }
    public void OnEnable() {
        slider.minValue = minValue;
        slider.maxValue = maxValue;
    }

    public void OnSliderChanged() {
        numberTextBox.text = slider.value.ToString("0.00");
    }

    public void OnTextEntryChanged() {
        try {
            var value = Math.Min(Math.Max(float.Parse(numberTextBox.text), minValue), maxValue);
            slider.value = value;
            numberTextBox.text = value.ToString("0.00");
        }
        catch {
            slider.value = 0;
            numberTextBox.text = "0";
        }
    }
}
