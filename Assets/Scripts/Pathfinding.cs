using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;

public class Pathfinding
{
    private static Graph graph = new();
    private const string neighborsFileName = "nodes_neighbors";

    [Header("Pathfinding Settings")]
    public float borderNodePriority = 0.5f;
    public float maxPathfindingTime = 2f;

    public Pathfinding()
    {
        LoadGraphFromResources();
    }
    
    private void LoadGraphFromResources()
    {
        try 
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(neighborsFileName);
            
            if (jsonFile == null)
            {
                Debug.LogError($"Graph file {neighborsFileName} not found in Resources folder");
                return;
            }

            graph.LoadGraph(jsonFile.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading graph: {e.Message}");
        }
    }

    public List<Vector2> GetPath(Vector2 start, Vector2 goal)
    {
        float startTime = Time.time;
        
        start = SnapToNearestNode(start);
        goal = SnapToNearestNode(goal);

        using (var openSet = new PriorityQueue(100))
        {
            var cameFrom = new Dictionary<Vector2, Vector2>();
            var gScore = new Dictionary<Vector2, float>();
            var fScore = new Dictionary<Vector2, float>();

            foreach (var node in graph.GetAllNodes())
            {
                gScore[node] = float.MaxValue;
                fScore[node] = float.MaxValue;
            }

            gScore[start] = 0;
            fScore[start] = CombinedHeuristic(start, goal);
            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                if (Time.time - startTime > maxPathfindingTime)
                {
                    Debug.LogWarning("Path finding timeout");
                    return null;
                }

                var current = openSet.Dequeue();

                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (var neighbor in graph.GetNeighbors(current))
                {
                    float movementCost = CalculateMovementCost(current, neighbor);
                    float tentativeGScore = gScore[current] + movementCost;

                    if (tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + CombinedHeuristic(neighbor, goal);

                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }

            Debug.LogWarning("No path found.");
            return null;
        }
    }

    private float CalculateMovementCost(Vector2 current, Vector2 neighbor)
    {
        float baseCost = Vector2.Distance(current, neighbor);
        float borderNodeCost = graph.IsBorderNode(neighbor) ? borderNodePriority : 1.0f;
        
        return baseCost * borderNodeCost;
    }

    private float CombinedHeuristic(Vector2 a, Vector2 b)
    {
        return Mathf.Min(ManhattanDistance(a, b), DiagonalDistance(a, b));
    }

    private float ManhattanDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private float DiagonalDistance(Vector2 a, Vector2 b)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + (Mathf.Sqrt(2) - 1) * Mathf.Min(dx, dy);
    }

    private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        List<Vector2> totalPath = new List<Vector2> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }

    public Vector2 SnapToNearestNode(Vector2 position)
    {
        if (graph.ContainsNode(position))
            return position;

        Vector2 nearestNode = Vector2.zero;
        float minDistanceSquared = float.MaxValue;

        foreach (var node in graph.GetAllNodes())
        {
            float distanceSquared = (node.x - position.x) * (node.x - position.x) + 
                                    (node.y - position.y) * (node.y - position.y);
            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                nearestNode = node;
            }
        }

        return nearestNode;
    }
}