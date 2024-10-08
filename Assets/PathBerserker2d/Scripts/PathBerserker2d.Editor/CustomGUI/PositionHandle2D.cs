using UnityEngine;
using UnityEditor;

namespace PathBerserker2d
{
    internal class PositionHandle2D
    {
        private Vector3 startPos;
        private Vector2 currentMousePos;
        private Vector2 startMousePos;
        public Color primary;
        public Color hover;
        public Color selected;

        private int hash;

        public PositionHandle2D(Color primary, Color hover, Color selected)
        {
            this.primary = primary;
            this.hover = hover;
            this.selected = selected;

            hash = GetHashCode();
        }

        public Vector2 DrawHandle(Vector2 position)
        {
            int controlIdXArrow = EditorGUIUtility.GetControlID(hash, FocusType.Passive);
            int controlIdYArrow = EditorGUIUtility.GetControlID(hash, FocusType.Passive);
            int controlIdRect = EditorGUIUtility.GetControlID(hash, FocusType.Passive);

            bool selectedXArrow = GUIUtility.hotControl == controlIdXArrow;
            bool hoveredXArrow = HandleUtility.nearestControl == controlIdXArrow;

            bool selectedYArrow = GUIUtility.hotControl == controlIdYArrow;
            bool hoveredYArrow = HandleUtility.nearestControl == controlIdYArrow;

            bool selectedRect = GUIUtility.hotControl == controlIdRect;
            bool hoveredRect = HandleUtility.nearestControl == controlIdRect;

            var e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && GUIUtility.hotControl == 0 && !e.alt)
                    {
                        if (HandleUtility.nearestControl == controlIdXArrow)
                        {
                            GUIUtility.hotControl = controlIdXArrow;
                        }
                        else if (HandleUtility.nearestControl == controlIdYArrow)
                        {
                            GUIUtility.hotControl = controlIdYArrow;
                        }
                        else if (HandleUtility.nearestControl == controlIdRect)
                        {
                            GUIUtility.hotControl = controlIdRect;
                        }


                        if (HandleUtility.nearestControl == controlIdXArrow ||
                            HandleUtility.nearestControl == controlIdYArrow ||
                            HandleUtility.nearestControl == controlIdRect)
                        {
                            startPos = position;
                            currentMousePos = e.mousePosition;
                            startMousePos = e.mousePosition;
                            e.Use();
                        }
                    }

                    break;
                case EventType.MouseUp:
                    if (e.button == 0 || e.button == 2)
                    {
                        if (GUIUtility.hotControl == controlIdXArrow || GUIUtility.hotControl == controlIdYArrow || GUIUtility.hotControl == controlIdRect)
                        {
                            GUIUtility.hotControl = 0;
                            e.Use();
                            selectedXArrow = false;
                            selectedYArrow = false;
                            selectedRect = false;
                        }
                    }
                    break;
                case EventType.MouseDrag:

                    if (GUIUtility.hotControl == controlIdXArrow || GUIUtility.hotControl == controlIdYArrow || GUIUtility.hotControl == controlIdRect)
                    {
                        currentMousePos += new Vector2(e.delta.x, -e.delta.y) * EditorGUIUtility.pixelsPerPoint;

                        Vector3 screenPos = Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(startPos));
                        screenPos += (Vector3)(currentMousePos - startMousePos);
                        Vector2 newPos = Handles.inverseMatrix.MultiplyPoint(Camera.current.ScreenToWorldPoint(screenPos));

                        if (selectedXArrow)
                        {
                            newPos.y = startPos.y;
                        }
                        else if (selectedYArrow)
                        {
                            newPos.x = startPos.x;
                        }

                        if (newPos != position)
                        {
                            position = newPos;
                            GUI.changed = true;
                        }

                        e.Use();
                    }
                    break;
            }
            Handles.color = selectedRect || selectedXArrow ? selected : (hoveredXArrow ? hover : primary);
            Handles.ArrowHandleCap(controlIdXArrow, position, Quaternion.Euler(0, 90, 0), HandleUtility.GetHandleSize(position), e.type);

            Handles.color = selectedRect || selectedYArrow ? selected : (hoveredYArrow ? hover : primary);
            Handles.ArrowHandleCap(controlIdYArrow, position, Quaternion.Euler(-90, 0, 0), HandleUtility.GetHandleSize(position), e.type);

            Handles.color = selectedRect ? selected : (hoveredRect ? hover : primary);
            float rectSize = HandleUtility.GetHandleSize(position) * 0.14f;

            Vector2 rectPos = position + Vector2.one * rectSize;
            if (e.type == EventType.Repaint)
                Handles.DrawSolidRectangleWithOutline(new Rect(position, new Vector2(rectSize, rectSize) * 2f), new Color(1, 1, 1, 0.2f), new Color(1, 1, 1, 1));

            rectPos = Handles.Slider2D(rectPos, Vector3.forward, Vector3.right, Vector3.up, rectSize, Handles.RectangleHandleCap, 0);
            position = rectPos - Vector2.one * rectSize;



            return position;
        }
    }
}
