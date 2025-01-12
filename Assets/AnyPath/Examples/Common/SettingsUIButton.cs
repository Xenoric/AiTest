using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnyPath.Examples
{
    public class SettingsUIButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text text;

        private Action callback;

        public void Initialize(string text, Action callback)
        {
            this.text.text = text;
            this.callback = callback;
        }

        public void OnClick()
        {
            callback?.Invoke();
        }
    }
}