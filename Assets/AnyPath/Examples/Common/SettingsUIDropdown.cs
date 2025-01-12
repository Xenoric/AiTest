using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnyPath.Examples
{
    public class SettingsUIDropdown : MonoBehaviour
    {
        [SerializeField] private Dropdown dropdown;

        private Action<int> callback;

        public void Initialize(List<string> options, Action<int> callback,  int defaultValue = 0)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = defaultValue;
            this.callback = callback;
        }

        public void OnValueChanged(int value)
        {
            callback?.Invoke(value);
        }
    }
}