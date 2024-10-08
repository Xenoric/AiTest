using UnityEngine;

namespace PathBerserker2d
{
    internal static class NavLinkInstanceDrawer
    {
        public static void Draw(INavLinkInstance link, Vector2 worldStartPos, Vector2 worldGoalPos)
        {
            Gizmos.color = PathBerserker2dSettings.GetLinkTypeColor(link.LinkType);
            GizmosDrawingExtensions.DrawArrow(worldStartPos, worldGoalPos, 0.2f);
        }
    }
}
