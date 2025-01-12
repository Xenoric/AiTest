using UnityEngine;
using UnityEngine.EventSystems;

namespace AnyPath.Examples
{
    public class ExampleUtil
    {
        private static Camera _mainCam;
        private static Camera MainCam
        {
            get
            {
                if (_mainCam == null)
                    _mainCam = Camera.main;
                return _mainCam;
            }
        }

        public static Vector3 GetMaxWorldPos => MainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height));

        public static Vector3 GetMinWorldPos => MainCam.ScreenToWorldPoint(new Vector3(0, 0));
        
        public static Vector3 GetMouseWorldPos() => MainCam.ScreenToWorldPoint(Input.mousePosition);

        public static bool PointerOnUI()
        {
            return EventSystem.current != null && 
                   EventSystem.current.currentSelectedGameObject != null;
        }
    }
}