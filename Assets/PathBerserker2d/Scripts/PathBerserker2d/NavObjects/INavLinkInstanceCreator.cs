using UnityEngine;

namespace PathBerserker2d
{
    internal interface INavLinkInstanceCreator
    {
        int LinkType { get; }
        int NavTag { get; }
        int PBComponentId { get; }

        GameObject GameObject { get; }
        float Clearance { get; }
        float TravelCosts(Vector2 start, Vector2 goal);
        float CostOverride { get; }

        float AvgWaitTime { get; }
        float MaxTraversableDistance { get; }
    }
}
