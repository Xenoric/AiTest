using UnityEngine;

namespace PathBerserker2d
{
    internal interface INavSegmentCreationParamProvider
    {
        float MaxClearance { get; }
        float MinClearance { get; }
        float CellSize { get; }
        LayerMask ColliderMask { get; }
    }
}
