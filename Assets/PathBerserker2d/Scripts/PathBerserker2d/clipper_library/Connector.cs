using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class Connector
    {
        public void Add(Vector2 a, Vector2 b)
        {
            LinkedListNode<PointChain> current = openPolygons.First;

            while (current != null)
            {
                LinkedListNode<PointChain> next = current.Next;

                PointChain currentChain = current.Value;

                if (currentChain.LinkSegment(a, b))
                {
                    if (currentChain.IsClosed())
                    {
                        closedPolygons.AddLast(currentChain);
                        openPolygons.Remove(current);
                        return;
                    }
                    else
                    {
                        LinkedListNode<PointChain> innerCurrent = current.Next;

                        while (innerCurrent != null)
                        {
                            LinkedListNode<PointChain> innerNext = innerCurrent.Next;

                            if (currentChain.LinkPointChain(innerCurrent.Value))
                            {
                                openPolygons.Remove(innerCurrent);
                                break;
                            }
                            innerCurrent = innerNext;
                        }
                    }
                    return;
                }
                current = next;
            }
            openPolygons.AddLast(new PointChain(a, b));
            return;
        }

        LinkedListNode<PointChain> begin() { return closedPolygons.First; }
        LinkedListNode<PointChain> end() { return closedPolygons.Last; }

        public void Clear()
        {
            closedPolygons.Clear();
            openPolygons.Clear();
        }

        public int GetNumClosedPolygons() { return closedPolygons.Count; }
        public int GetNumOpenPolygons() { return openPolygons.Count; }

        public LinkedList<PointChain> openPolygons = new LinkedList<PointChain>();
        public LinkedList<PointChain> closedPolygons = new LinkedList<PointChain>();
    }
}