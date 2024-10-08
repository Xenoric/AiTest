using UnityEngine;
using UnityEditor;

namespace PathBerserker2d
{
    internal class IconHandle2D
    {
        private static readonly int controlHashIcon = "IconHandle2D_icon".GetHashCode();

        public static void DrawHandle(Vector3 position, Texture icon, float size, Object objectToSelect)
        {
            int iconId = EditorGUIUtility.GetControlID(controlHashIcon, FocusType.Passive);

            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && HandleUtility.nearestControl == iconId)
                    {
                        Selection.activeObject = objectToSelect;
                        e.Use();
                    }

                    break;
                case EventType.Layout:
                    float distance = HandleUtility.DistanceToRectangle(position, Camera.current.transform.rotation, size);
                    HandleUtility.AddControl(iconId, distance);
                    break;
                case EventType.Repaint:
                    Vector3 up = Camera.current.transform.up * size;
                    float aspectRatio = (float)icon.width / (float)icon.height;
                    Vector3 right = Camera.current.transform.right * size * aspectRatio;
                    SharedMaterials.UnlitTexture.SetTexture("_MainTex", icon);
                    SharedMaterials.UnlitTexture.SetPass(0);
                    GL.Begin(GL.QUADS);
                    {
                        GL.TexCoord2(1, 1);
                        GL.Vertex(position + right + up);
                        GL.TexCoord2(1, 0);
                        GL.Vertex(position + right - up);
                        GL.TexCoord2(0, 0);
                        GL.Vertex(position - right - up);
                        GL.TexCoord2(0, 1);
                        GL.Vertex(position - right + up);
                    }
                    GL.End();
                    break;
            }
        }
    }
}
