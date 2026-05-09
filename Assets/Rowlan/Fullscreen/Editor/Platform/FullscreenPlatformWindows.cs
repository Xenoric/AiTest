#if UNITY_EDITOR_WIN

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Windows-specific fullscreen logic.
    /// Uses Win32 to strip window borders and force the popup topmost over the taskbar.
    /// </summary>
    public static class FullscreenPlatformWindows
    {
        #region Win32 Imports

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        #endregion

        #region Win32 Constants

        private const int GWL_STYLE = -16;
        private const int WS_BORDER = 0x00800000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        #endregion

        #region Private State

        private static IntPtr popupHwnd = IntPtr.Zero;

        #endregion

        #region Public API

        /// <summary>
        /// Strips remaining border styles from the popup window and forces it
        /// topmost at position (0,0) covering the full screen including the taskbar.
        /// Uses a delayed call to ensure the window handle is available.
        /// </summary>
        /// <param name="popup">The fullscreen popup EditorWindow.</param>
        /// <param name="pixelW">Screen width in physical pixels.</param>
        /// <param name="pixelH">Screen height in physical pixels.</param>
        public static void OnEnterFullscreen(EditorWindow popup, int pixelW, int pixelH)
        {
            EditorApplication.delayCall += () =>
            {
                if (popup == null) return;
                popup.Focus();

                EditorApplication.delayCall += () =>
                {
                    if (popup == null) return;

                    popupHwnd = GetActiveWindow();

                    if (popupHwnd == IntPtr.Zero)
                    {
                        Debug.LogWarning("[Fullscreen/Win] Could not acquire window handle.");
                        return;
                    }

                    // Strip any remaining border styles
                    int style = GetWindowLong(popupHwnd, GWL_STYLE);
                    style &= ~WS_BORDER;
                    style &= ~WS_CAPTION;
                    style &= ~WS_THICKFRAME;
                    SetWindowLong(popupHwnd, GWL_STYLE, style);

                    // Force topmost and cover the entire screen including the taskbar
                    SetWindowPos(popupHwnd, HWND_TOPMOST,
                        0, 0, pixelW, pixelH,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                };
            };
        }

        /// <summary>
        /// Removes the topmost flag from the popup window so it no longer covers the taskbar.
        /// </summary>
        public static void OnExitFullscreen()
        {
            if (popupHwnd != IntPtr.Zero)
            {
                SetWindowPos(popupHwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                popupHwnd = IntPtr.Zero;
            }
        }

        #endregion
    }
}
#endif