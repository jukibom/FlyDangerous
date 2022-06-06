using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI {
    public class FdSlider : MonoBehaviour {
        [SerializeField] private InputField numberTextBox;
        [SerializeField] private Slider slider;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;

        [SerializeField] public UnityEvent<float> onValueChanged;


        public float Value {
            get => slider.value;
            set {
                UpdateSliderConstraints();
                slider.value = Math.Min(Math.Max(value, minValue), maxValue);

                // Apparently this doesn't trigger if the value is ONE?! SERIOUSLY? 
                OnSliderChanged();
            }
        }

        public void OnEnable() {
            UpdateSliderConstraints();
        }

        public void OnSliderChanged() {
            numberTextBox.text = slider.wholeNumbers
                ? slider.value.ToString("0")
                : slider.value.ToString("0.00");
            OnValueChanged();
        }

        public void OnTextEntryChanged() {
            try {
                var value = Math.Min(Math.Max(float.Parse(numberTextBox.text, CultureInfo.InvariantCulture), minValue), maxValue);
                slider.value = slider.wholeNumbers ? (int)value : value;
                numberTextBox.text = slider.wholeNumbers
                    ? value.ToString("0")
                    : value.ToString("0.00");
            }
            catch {
                slider.value = 0;
                numberTextBox.text = "0";
            }
            finally {
                OnValueChanged();
            }
        }

        private void UpdateSliderConstraints() {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
        }

        private void OnValueChanged() {
            onValueChanged.Invoke(Value);
        }
    }
}