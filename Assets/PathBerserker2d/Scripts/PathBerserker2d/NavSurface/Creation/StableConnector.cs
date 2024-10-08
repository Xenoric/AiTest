using System.Collections.Generic;

namespace PathBerserker2d
{
    internal class StableConnector<T> where T : System.IEquatable<T>
    {
        public void Add(T a, T b)
        {
            LinkedListNode<StablePointChain<T>> current = openPolygons.First;

            while (current != null)
            {
                LinkedListNode<StablePointChain<T>> next = current.Next;

                StablePointChain<T> currentChain = current.Value;

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
                        LinkedListNode<StablePointChain<T>> innerCurrent = current.Next;

                        while (innerCurrent != null)
                        {
                            LinkedListNode<StablePointChain<T>> innerNext = innerCurrent.Next;

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
            openPolygons.AddLast(new StablePointChain<T>(a, b));
            return;
        }

        LinkedListNode<StablePointChain<T>> begin() { return closedPolygons.First; }
        LinkedListNode<StablePointChain<T>> end() { return closedPolygons.Last; }

        public void Clear()
        {
            closedPolygons.Clear();
            openPolygons.Clear();
        }

        public int GetNumClosedPolygons() { return closedPolygons.Count; }

        LinkedList<StablePointChain<T>> openPolygons = new LinkedList<StablePointChain<T>>();
        public LinkedList<StablePointChain<T>> closedPolygons = new LinkedList<StablePointChain<T>>();
    }
}