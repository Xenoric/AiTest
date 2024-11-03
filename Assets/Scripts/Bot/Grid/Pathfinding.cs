using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Bot.Grid
{
    public class Pathfinding
    {
        private Grid _grid;

        public Pathfinding(Grid grid) => _grid = grid;

        public List<Node> FindPath(Node startNode, Node targetNode)
        {
            Heap<Node> openSet = new ((int)(_grid.Size.x * _grid.Size.y));
            HashSet<Node> closedSet = new();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var currentNode = openSet.RemoveFirst();
                
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                    return RetracePath(startNode, targetNode);

                foreach (var neighbour in _grid.GetNeighbours(currentNode))
                {
                    if (!neighbour.IsWalkable || closedSet.Contains(neighbour))
                        continue;

                    var newMovementCost = currentNode.GCost + GetDistance(currentNode, neighbour);

                    if (newMovementCost < neighbour.GCost || !openSet.Contains(neighbour))
                    {
                        neighbour.GCost = newMovementCost;
                        neighbour.HCost = GetDistance(neighbour, targetNode);
                        neighbour.Parent = currentNode;
                        
                        if(!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }

            return null;
        }

        private List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private int GetDistance(Node node1, Node node2)
        {
            int distanceX = Mathf.Abs(node1.GridX - node2.GridX);
            int distanceY = Mathf.Abs(node1.GridY - node2.GridY);
            var difference = Mathf.Abs(distanceX - distanceY);
            
            if (distanceX > distanceY)
                return 14 * distanceY + 10 * difference;
            
            return 14 * distanceX + 10 * difference;
        }
    }
}