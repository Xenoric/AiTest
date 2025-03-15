using UnityEngine;

namespace PathBerserker2d
{
    internal static class NavGraphDrawer
    {
        public static void Draw(NavGraph graph)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;

            SharedMaterials.UnlitStripped.SetFloat(SharedMaterials.UnlitStripped_SegmentSizeId, 0.08f);
            SharedMaterials.UnlitStripped.SetFloat(SharedMaterials.UnlitStripped_PauseSizeId, 0.08f * (PathBerserker2dSettings.NavTags.Length - 2));

            // draw segments
            foreach (var pair in graph.segmentTrees)
            {
                foreach (var cluster in pair.Value.Clusters)
                {
                    DrawCluster(cluster, pair.Value.WorldToLocal.inverse);
                }

                // navsurface can be destroyed before onDisable on navsurface is called
                if (pair.Key != null)
                    NavSurfaceDrawer.DrawNavSurface(pair.Key);
            }
            Gizmos.matrix = oldMatrix;
        }

        private static void DrawCluster(NavGraphNodeCluster cluster, Matrix4x4 clusterLocalToWorld)
        {
            float areaMarkerLineWidth = PathBerserker2dSettings.NavAreaMarkerLineWidth;
            foreach (var mod in cluster.modifiers)
            {
                Vector2 a = cluster.GetPositionAlongSegment(mod.T);
                Vector2 b = cluster.GetPositionAlongSegment(mod.T + mod.Length);
                Vector2 tangent = b - a;
                Quaternion rot = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, tangent));
                SharedMaterials.UnlitStripped.SetFloat(SharedMaterials.UnlitStripped_XOffsetId, 0.08f * mod.NavTag);
                SharedMaterials.UnlitStripped.SetColor(SharedMaterials.UnlitStripped_ColorId, PathBerserker2dSettings.GetNavTagColor(mod.NavTag));
                SharedMaterials.UnlitStripped.SetPass(0);
                Graphics.DrawMeshNow(PrimitiveMesh.Quad, clusterLocalToWorld * Matrix4x4.TRS(a, rot, new Vector3(tangent.magnitude, areaMarkerLineWidth)));
            }

            for (int iNode = 0; iNode < cluster.nodes.Count; iNode++)
            {
                var node = cluster.nodes[iNode];
                var link = node.link;

                if (link.LinkType > 0)
                    NavLinkInstanceDrawer.Draw(link, cluster.owner.LocalToWorld.MultiplyPoint3x4(cluster.GetPositionAlongSegment(node.t)),
                        node.LinkTarget.owner.LocalToWorld.MultiplyPoint3x4(node.LinkTarget.GetPositionAlongSegment(node.LinkTargetT)));
            }
        }
    }
}
