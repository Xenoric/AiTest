#if UNITY_EDITOR_OSX

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// macOS-specific fullscreen logic.
    /// Uses the Objective-C runtime to raise the window above the Dock and menu bar,
    /// and hides them via NSApplication presentation options.
    /// </summary>
    public static class FullscreenPlatformMac
    {
        #region Objective-C Runtime Imports

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        private static extern IntPtr objc_getClass(string className);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_void_long(IntPtr receiver, IntPtr selector, long arg);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern long objc_msgSend_long(IntPtr receiver, IntPtr selector);

        #endregion

        #region Constants

        /// <summary>
        /// NSWindow level high enough to sit above the Dock and menu bar.
        /// </summary>
        private const long NSScreenSaverWindowLevel = 1000;

        // NSApplicationPresentationOptions flags
        private const long PresentationHideDock = 1 << 1;
        private const long PresentationHideMenuBar = 1 << 3;
        private const long PresentationDefault = 0;

        /// <summary>
        /// NSWindowCollectionBehaviorStationary — prevents the window from moving during Exposé/Spaces.
        /// </summary>
        private const long CollectionBehaviorStationary = 1 << 4;

        #endregion

        #region Private State

        private static IntPtr nsWindow = IntPtr.Zero;
        private static long originalWindowLevel;

        #endregion

        #region Public API

        /// <summary>
        /// Acquires the key NSWindow, raises it above the Dock and menu bar,
        /// and sets presentation options to hide them entirely.
        /// Uses a delayed call to ensure the window is focused and accessible.
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

                    try
                    {
                        IntPtr nsApp = objc_getClass("NSApplication");
                        IntPtr sharedApp = objc_msgSend_IntPtr(nsApp, sel_registerName("sharedApplication"));
                        nsWindow = objc_msgSend_IntPtr(sharedApp, sel_registerName("keyWindow"));

                        if (nsWindow == IntPtr.Zero)
                        {
                            Debug.LogWarning("[Fullscreen/Mac] Could not acquire NSWindow.");
                            return;
                        }

                        // Save original level for restoration
                        originalWindowLevel = objc_msgSend_long(nsWindow, sel_registerName("level"));

                        // Raise window above Dock and menu bar
                        objc_msgSend_void_long(nsWindow, sel_registerName("setLevel:"),
                            NSScreenSaverWindowLevel);

                        // Keep window stationary during Exposé and Spaces
                        objc_msgSend_void_long(nsWindow, sel_registerName("setCollectionBehavior:"),
                            CollectionBehaviorStationary);

                        // Hide Dock and menu bar
                        objc_msgSend_void_long(sharedApp, sel_registerName("setPresentationOptions:"),
                            PresentationHideDock | PresentationHideMenuBar);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Fullscreen/Mac] Platform call failed: {e.Message}");
                    }
                };
            };
        }

        /// <summary>
        /// Restores the original NSWindow level and re-shows the Dock and menu bar
        /// by resetting NSApplication presentation options to default.
        /// </summary>
        public static void OnExitFullscreen()
        {
            try
            {
                if (nsWindow != IntPtr.Zero)
                {
                    objc_msgSend_void_long(nsWindow, sel_registerName("setLevel:"), originalWindowLevel);
                    nsWindow = IntPtr.Zero;
                }

                // Restore Dock and menu bar
                IntPtr nsApp = objc_getClass("NSApplication");
                IntPtr sharedApp = objc_msgSend_IntPtr(nsApp, sel_registerName("sharedApplication"));
                objc_msgSend_void_long(sharedApp, sel_registerName("setPresentationOptions:"),
                    PresentationDefault);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Fullscreen/Mac] Restore failed: {e.Message}");
            }
        }

        #endregion
    }
}
#endif