using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnyPath.Examples
{
    public class SettingsUISlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Text text;

        private Action<float> callback;
        private string label;

        public void Initialize(string label, float min, float max, float defaultValue, bool wholeNumbers, Action<float> callback)
        {
            this.label = label;
            
            this.slider.minValue = min;
            this.slider.maxValue = max;
            this.slider.value = defaultValue;
            this.slider.wholeNumbers = wholeNumbers;
            this.callback = callback;

            this.text.text = $"{this.label} ({this.slider.value})";
        }

        public void OnChange(float value)
        {
            this.text.text = $"{label} ({this.slider.value})";
            callback?.Invoke(value);
        }

        public float Value => slider.value;
    }
}