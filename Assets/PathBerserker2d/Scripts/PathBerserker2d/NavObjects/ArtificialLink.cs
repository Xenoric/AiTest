using System;
using UnityEngine;

namespace PathBerserker2d
{
    internal class ArtificialLink : INavLinkInstance
    {
        public NavSegmentPositionPointer Start => throw new NotImplementedException();

        public NavSegmentPositionPointer Goal => throw new NotImplementedException();

        public int LinkType => linkType;

        public string LinkTypeName => throw new NotImplementedException();

        public GameObject GameObject => throw new NotImplementedException();

        public float Clearance => float.MaxValue;

        public int NavTag => 0;

        public bool IsTraversable => true;
        public int PBComponentId => 0;

        private int linkType;

        public ArtificialLink(int linkType)
        {
            this.linkType = linkType;
        }

        public float TravelCosts(Vector2 start, Vector2 goal)
        {
            return 0;
        }

        public void OnRemove()
        {
        }
    }
}
