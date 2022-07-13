using UnityEngine;

namespace FdUI {
    public class SliderOption : MonoBehaviour {
        public string preference = "default-preference";
        [SerializeField] private FdSlider slider;

        public float Value {
            get => slider.Value;
            set => slider.Value = value;
        }
    }
}