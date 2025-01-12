using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnyPath.Examples
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private GameObject togglePrefab;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private GameObject sliderPrefab;
        [SerializeField] private GameObject dropdownPrefab;
        [SerializeField] private Transform root;


        private void Awake()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        public void AddButton(string key, Action callback)
        {
            Instantiate(buttonPrefab, root).GetComponent<SettingsUIButton>().Initialize(key, callback);
        }
        
        public void AddDropdown(List<string> options, Action<int> callback, int defaultValue = 0)
        {
            Instantiate(dropdownPrefab, root).GetComponent<SettingsUIDropdown>().Initialize(options, callback, defaultValue);
        }

        public void AddToggle(string key, bool defaultValue, Action<bool> callback)
        {
            var toggle = Instantiate(togglePrefab, root).GetComponent<SettingsUIToggle>();
            toggle.Initialize(key, defaultValue, callback);
        }

        public void AddSlider(string key, float min, float max, float defaultValue, bool wholeNumbers, Action<float> callback)
        {
            var slider = Instantiate(sliderPrefab, root).GetComponent<SettingsUISlider>();
            slider.Initialize(key, min, max, defaultValue, wholeNumbers, callback);
        }
    }
}