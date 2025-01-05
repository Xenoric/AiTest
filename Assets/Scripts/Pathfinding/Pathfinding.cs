using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Pathfinding
{
    private Graph graph = new();
    private string neighborsFilePath => Path.Combine(Application.dataPath, "nodes_neighbors.json");

    [Header("Pathfinding Settings")]
    public float borderNodePriority = 0.5f;
    public float maxPathfindingTime = 2f;

    public Pathfinding()
    {
        graph.LoadGraph(neighborsFilePath);
    }

    public List<Vector2> GetPath(Vector2 start, Vector2 goal)
    {
       
        float startTime = Time.time;
        
            start = SnapToNearestNode(start);
            goal = SnapToNearestNode(goal);


        var openSet = new OptimizedPriorityQueue<Vector2>();
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
                //Debug.LogWarning("Path finding timeout");
                return null;
            }

            var current = openSet.Dequeue();

            if (current == goal)
            {
                //Debug.Log($"Path found to goal: {current}");
                return ReconstructPath(cameFrom, current);// Отладка
            }

            // Получаем соседей через новый метод GetNeighbors
            foreach (var neighbor in graph.GetNeighbors(current))
            {
                float movementCost = CalculateMovementCost(current, neighbor);
                float tentativeGScore = gScore[current] + movementCost;

                if (tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + CombinedHeuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
        }

        Debug.LogWarning("No path found.");
        return null;
    }

    // Обновленный метод расчета стоимости движения
    private float CalculateMovementCost(Vector2 current, Vector2 neighbor)
    {
        float baseCost = Vector2.Distance(current, neighbor);
        
        // Учитываем пограничность узла с помощью нового метода
        float borderNodeCost = graph.IsBorderNode(neighbor) ? borderNodePriority : 1.0f;
        
        return baseCost * borderNodeCost;
    }

    // Остальные методы остаются без изменений
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

    // Обновленный метод поиска ближайшего узла
public Vector2 SnapToNearestNode(Vector2 position)
{
    // Если точная позиция существует - возвращаем ее
    if (graph.ContainsNode(position))
        return position;

    // Находим ближайший узел по координатам
    Vector2 nearestNode = Vector2.zero;
    float minDistanceSquared = float.MaxValue;

    foreach (var node in graph.GetAllNodes())
    {
        float distanceSquared = (node.x - position.x) * (node.x - position.x) + (node.y - position.y) * (node.y - position.y);
        if (distanceSquared < minDistanceSquared)
        {
            minDistanceSquared = distanceSquared;
            nearestNode = node;
        }
    }

    return nearestNode;
}
}