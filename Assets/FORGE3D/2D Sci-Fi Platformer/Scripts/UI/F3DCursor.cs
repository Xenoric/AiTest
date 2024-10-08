using UnityEngine;
using System.Collections;

public class F3DCursor : MonoBehaviour
{
    public Texture2D cursorTexture;
    public CursorMode cursorMode = CursorMode.Auto;
    public Vector2 hotSpot = Vector2.zero;
    // Use this for initialization
    void Awake()
    {

 
    Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);

    //void OnMouseExit()
    //    Cursor.SetCursor(null, Vector2.zero, cursorMode);
}


}
