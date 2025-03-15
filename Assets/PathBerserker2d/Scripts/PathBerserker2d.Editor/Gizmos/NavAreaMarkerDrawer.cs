using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class NavAreaMarkerDrawer
    {
        static Vector3[] worldCorners = new Vector3[4];

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawGizmos(NavAreaMarker src, GizmoType gizmoType)
        {
            if (!Application.IsPlaying(src) && ((gizmoType & GizmoType.Selected) != 0 || PathBerserker2dSettings.DrawUnselectedAreaMarkers))
            {
                var rT = src.GetComponent<RectTransform>();

                Color c = src.MarkerColor;
                c.a = 0.4f;

                SharedMaterials.UnlitTransparentTinted.SetColor(SharedMaterials.UnlitTransparentTinted_ColorId, c);
                SharedMaterials.UnlitTransparentTinted.SetPass(0);

                var m = rT.localToWorldMatrix * Matrix4x4.TRS(rT.rect.min, Quaternion.identity, rT.rect.size);
                m.m23 = 2;
                Graphics.DrawMeshNow(PrimitiveMesh.Quad, m);
            }
        }
    }
}
