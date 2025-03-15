using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class NavSurfaceRecord
    {
        public B2DynamicTree<NavGraphNodeCluster> Clusters { get; private set; }
        public List<INavLinkInstance> SoftRefLinks { get; private set; }

        public Matrix4x4 LocalToWorld { get => localToWorld; set {
                localToWorld = value;
                worldToLocal = localToWorld.inverse;
            } }
        public Matrix4x4 WorldToLocal => worldToLocal;
        public readonly NavSurface navSurface;
        public readonly int bakeIteration;

        private Matrix4x4 localToWorld;
        private Matrix4x4 worldToLocal;

        public NavSurfaceRecord(B2DynamicTree<NavGraphNodeCluster> tree, Matrix4x4 localToWorld, NavSurface navSurface)
        {
            this.Clusters = tree;
            SoftRefLinks = new List<INavLinkInstance>();
            this.localToWorld = localToWorld;
            this.navSurface = navSurface;
            this.bakeIteration = navSurface.BakeIteration;
        }

        public void AddSoftRefLink(INavLinkInstance instance)
        {
            SoftRefLinks.Add(instance);
        }

        public void RemoveSoftRefLink(INavLinkInstance instance)
        {
            SoftRefLinks.Remove(instance);
        }

        public void Destroy(NavGraph graph)
        {
            foreach (var ls in SoftRefLinks)
            {
                ls.OnRemove();
            }
        }
    }
}
