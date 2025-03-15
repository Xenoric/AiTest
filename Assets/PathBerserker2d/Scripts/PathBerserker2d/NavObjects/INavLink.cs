using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Internal instance of a NavLink.
    /// </summary>
    /// <remarks>
    /// An instance is deliberately kept very basic. It links from a given start to a given goal.
    /// </remarks>
    public interface INavLinkInstance
    {
        //NavSegmentPositionPointer Start { get; }
        //NavSegmentPositionPointer Goal { get; }
        int LinkType { get; }
        int NavTag { get; }

        bool IsTraversable { get; }

        string LinkTypeName { get; }

        GameObject GameObject { get; }

        float Clearance { get; }
        int PBComponentId { get; }

        float TravelCosts(Vector2 start, Vector2 goal);

        void OnRemove();
    }
}
