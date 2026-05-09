using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Invisible MonoBehaviour that catches keybind input when the Game View has focus
    /// during Play Mode. Spawned automatically by FullscreenGameView when Play Mode starts.
    ///
    /// Reads keybinds from FullscreenKeybinds (populated by the editor at spawn time).
    /// Communicates back to the editor via static delegates to avoid a compile-time
    /// dependency on the Editor assembly.
    /// </summary>
    public class FullscreenKeyListener : MonoBehaviour
    {
        #region Editor Callbacks

        /// <summary>
        /// Delegate invoked when the toggle or exit key is pressed.
        /// Assigned by FullscreenGameView (editor side) before this component is spawned.
        /// </summary>
        public static Action OnTogglePressed;

        /// <summary>
        /// Delegate invoked to check whether fullscreen is currently active.
        /// Assigned by FullscreenGameView (editor side) before this component is spawned.
        /// </summary>
        public static Func<bool> IsFullscreen;

        #endregion

        #region Cached Keybinds

        private Key toggleKey;
        private Key exitKey;

        #endregion

        #region Initialization

        /// <summary>
        /// Caches the current keybind settings at spawn time.
        /// </summary>
        private void Awake()
        {
            toggleKey = FullscreenKeybinds.ToggleKey;
            exitKey = FullscreenKeybinds.ExitKey;
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Checks for toggle and exit key presses each frame
        /// and forwards them to the editor via the delegate.
        /// </summary>
        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                OnTogglePressed?.Invoke();
            }
            else if (Keyboard.current[exitKey].wasPressedThisFrame)
            {
                if (IsFullscreen != null && IsFullscreen())
                    OnTogglePressed?.Invoke();
            }
        }

        #endregion
    }
}
