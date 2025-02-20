using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    private const string neighborsFileName = "nodes_neighbors";

    public static float BorderNodePriority { get; set; }
    public static float MaxPathfindingTime { get; set; } 

    private static readonly ObjectPool<Dictionary<Vector2, Vector2>> CameFromPool = new(() => new Dictionary<Vector2, Vector2>(100));
    private static readonly ObjectPool<Dictionary<Vector2, float>> GScorePool = new(() => new Dictionary<Vector2, float>(100));
    private static readonly ObjectPool<Dictionary<Vector2, float>> FScorePool = new(() => new Dictionary<Vector2, float>(100));
    private static Vector2[] _nodes;

    static Pathfinder()
    {
        LoadGraphFromResources();
    }

    private static void LoadGraphFromResources()
    {
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(neighborsFileName);
            if (jsonFile == null)
            {
                Debug.LogError($"Graph file {neighborsFileName} not found in Resources folder");
                return;
            }
            Graph.LoadGraph(jsonFile.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading graph: {e.Message}");
        }
    }

    public static List<Vector2> GetPath(Vector2 start, Vector2 goal, int team)
    {
        float startTime = Time.time;

        start = SnapToNearestNode(start);
        goal = SnapToNearestNode(goal);

        var cameFrom = CameFromPool.Get();
        var gScore = GScorePool.Get();
        var fScore = FScorePool.Get();

        var openSet = new PriorityQueue();

        var nodes = Graph.GetAllNodes().ToArray();
        for (int i = 0; i < nodes.Length; i++)
        {
            gScore[nodes[i]] = float.MaxValue;
            fScore[nodes[i]] = float.MaxValue;
        }

        gScore[start] = 0;
        fScore[start] = CombinedHeuristic(start, goal);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            if (Time.time - startTime > MaxPathfindingTime)
            {
                ReleasePools(cameFrom, gScore, fScore);
                return null;
            }

            var current = openSet.Dequeue();

            if (current == goal)
            {
                var path = ReconstructPath(cameFrom, current);
                ReleasePools(cameFrom, gScore, fScore);
                return path;
            }

            var neighbors = Graph.GetNeighbors(current);
            for (int i = 0; i < neighbors.Length; i++)
            {
                var neighbor = neighbors[i];
                
                // Пропускаем ноды, занятые своей командой
                if (OccupiedNodesSystem.IsOccupiedByTeam(neighbor, team))
                    continue;

                float movementCost = CalculateMovementCost(current, neighbor);
                float tentativeGScore = gScore[current] + movementCost;

                if (tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + CombinedHeuristic(neighbor, goal);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        ReleasePools(cameFrom, gScore, fScore);
        return null;
    }

    public static Vector2[] GetNeighbors(Vector2 nodePosition)
    {
        return Graph.GetNeighbors(nodePosition);
    }

    private static float CalculateMovementCost(Vector2 current, Vector2 neighbor)
    {
        float baseCost = Vector2.Distance(current, neighbor);
        float borderNodeCost = Graph.IsBorderNode(neighbor) ? BorderNodePriority : 1.0f;
        return baseCost * borderNodeCost;
    }

    private static float CombinedHeuristic(Vector2 a, Vector2 b)
    {
        return Mathf.Min(ManhattanDistance(a, b), DiagonalDistance(a, b));
    }

    private static float ManhattanDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static float DiagonalDistance(Vector2 a, Vector2 b)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + (Mathf.Sqrt(2) - 1) * Mathf.Min(dx, dy);
    }

    private static List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        var totalPath = ListPool<Vector2>.Get();
        totalPath.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }

        totalPath.Reverse();
        return totalPath;
    }

    private static void ReleasePools(Dictionary<Vector2, Vector2> cameFrom, 
                                   Dictionary<Vector2, float> gScore, 
                                   Dictionary<Vector2, float> fScore)
    {
        CameFromPool.Release(cameFrom);
        GScorePool.Release(gScore);
        FScorePool.Release(fScore);
    }

    public static Vector2 SnapToNearestNode(Vector2 position)
    {
        if (Graph.ContainsNode(position))
            return position;

        Vector2 nearestNode = Vector2.zero;
        float minDistanceSquared = float.MaxValue;

        _nodes = Graph.GetAllNodes().ToArray();
        for (int i = 0; i < _nodes.Length; i++)
        {
            var node = _nodes[i];
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