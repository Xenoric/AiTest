using UnityEngine;
using System.Collections.Generic;

namespace PathBerserker2d
{
    internal interface IColliderFilter
    {
        IEnumerable<Collider2D> Filter(IEnumerable<Collider2D> colliders);
    }
}
