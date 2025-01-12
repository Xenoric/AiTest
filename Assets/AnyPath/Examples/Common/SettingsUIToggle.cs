using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnyPath.Examples
{
    public class SettingsUIToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Text text;

        private Action<bool> callback;

        public void Initialize(string text, bool defaultValue, Action<bool> callback)
        {
            this.text.text = text;
            this.toggle.isOn = defaultValue;
            this.callback = callback;
        }

        public void OnClick()
        {
            callback?.Invoke(toggle.isOn);
        }

        public bool Value => toggle.isOn;
    }
}