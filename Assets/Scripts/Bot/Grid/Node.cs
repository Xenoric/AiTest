using System;
using UnityEngine;

namespace Scripts.Bot.Grid
{
    [Serializable]
    public class Node : IHeapNode<Node>
    {
        public bool IsWalkable { get; private set; }
        public Vector2 Position { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int FCost => HCost + GCost;
        public int HCost { get; set; }
        public int GCost { get; set; }
        public Node Parent { get; set; }
        public int HeapIndex
        {
            get => _heapIndex;
            set => _heapIndex = value;
        }
        private int _heapIndex;

        public int CompareTo(Node other)
        {
            int compare = FCost.CompareTo(other.FCost);

            if (compare == 0)
                compare = HCost.CompareTo(other.HCost);

            return -compare;
        }
        
        public Node(bool isWalkable, Vector2 position, int gridX, int gridY)
        {
            IsWalkable = isWalkable;
            Position = position;
            GridX = gridX;
            GridY = gridY;
        }
    }
}