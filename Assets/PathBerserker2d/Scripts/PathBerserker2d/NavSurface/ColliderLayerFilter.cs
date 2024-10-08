using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PathBerserker2d
{
    internal class ColliderLayerFilter : IColliderFilter
    {
        private LayerMask layerMask;
        private bool onlyStatic;

        public ColliderLayerFilter(LayerMask layerMask, bool onlyStatic)
        {
            this.layerMask = layerMask;
            this.onlyStatic = onlyStatic;
        }

        public IEnumerable<Collider2D> Filter(IEnumerable<Collider2D> colliders)
        {
            return colliders.Where(
                (col) => layerMask.IsLayerWithinMask(col.gameObject.layer) &&
                !col.GetComponent<DynamicObstacle>() &&
                (!onlyStatic || col.gameObject.isStatic));
        }
    }
}
