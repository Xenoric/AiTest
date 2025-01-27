using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pathfinding : IPathfinding
{
    private static Graph graph = new();
    private const string neighborsFileName = "nodes_neighbors";

    public float BorderNodePriority { get; set; } = 0.5f;
    public float MaxPathfindingTime { get; set; } = 2f;

    // Пул для словарей
    private static readonly ObjectPool<Dictionary<Vector2, Vector2>> CameFromPool = new(() => new Dictionary<Vector2, Vector2>(100));
    private static readonly ObjectPool<Dictionary<Vector2, float>> GScorePool = new(() => new Dictionary<Vector2, float>(100));
    private static readonly ObjectPool<Dictionary<Vector2, float>> FScorePool = new(() => new Dictionary<Vector2, float>(100));

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

        // Используем пул для словарей
        var cameFrom = CameFromPool.Get();
        var gScore = GScorePool.Get();
        var fScore = FScorePool.Get();

        var openSet = new PriorityQueue();

        // Инициализация gScore и fScore
        var nodes = graph.GetAllNodes().ToArray();
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
                Debug.LogWarning("Path finding timeout");

                // Возвращаем словари в пул перед выходом
                CameFromPool.Release(cameFrom);
                GScorePool.Release(gScore);
                FScorePool.Release(fScore);

                return null;
            }

            var current = openSet.Dequeue();

            if (current == goal)
            {
                var path = ReconstructPath(cameFrom, current);

                // Возвращаем словари в пул после использования
                CameFromPool.Release(cameFrom);
                GScorePool.Release(gScore);
                FScorePool.Release(fScore);

                return path;
            }

            var neighbors = graph.GetNeighbors(current);
            for (int i = 0; i < neighbors.Length; i++)
            {
                var neighbor = neighbors[i];
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

        Debug.LogWarning("No path found.");

        // Возвращаем словари в пул перед выходом
        CameFromPool.Release(cameFrom);
        GScorePool.Release(gScore);
        FScorePool.Release(fScore);

        return null;
    }

    private float CalculateMovementCost(Vector2 current, Vector2 neighbor)
    {
        float baseCost = Vector2.Distance(current, neighbor);
        float borderNodeCost = graph.IsBorderNode(neighbor) ? BorderNodePriority : 1.0f;

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

    public Vector2 SnapToNearestNode(Vector2 position)
    {
        if (graph.ContainsNode(position))
            return position;

        Vector2 nearestNode = Vector2.zero;
        float minDistanceSquared = float.MaxValue;

        var nodes = graph.GetAllNodes().ToArray();
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
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

// Реализация пула объектов
public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _pool = new();
    private readonly System.Func<T> _createFunc;

    public ObjectPool(System.Func<T> createFunc)
    {
        _createFunc = createFunc;
    }

    public T Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }
        return _createFunc();
    }

    public void Release(T obj)
    {
        if (obj is System.Collections.IDictionary dictionary)
        {
            dictionary.Clear();
        }
        _pool.Push(obj);
    }
}