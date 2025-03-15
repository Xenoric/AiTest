using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public struct PathNodeInfo
{
    public Vector2 CameFrom;
    public float GScore;
    public float FScore;
    
    public PathNodeInfo(Vector2 cameFrom, float gScore, float fScore)
    {
        CameFrom = cameFrom;
        GScore = gScore;
        FScore = fScore;
    }
}

public struct MovementCostData
{
    public float BaseCost;
    public float BorderFactor;
    public float TotalCost;
}

public struct HeuristicResult
{
    public float Manhattan;
    public float Diagonal;
    public float Combined;
}

public struct NodeDistanceInfo
{
    public Vector2 Node;
    public float DistanceSquared;
    
    public NodeDistanceInfo(Vector2 node, float distanceSquared)
    {
        Node = node;
        DistanceSquared = distanceSquared;
    }
}


public static class Pathfinder
{
    private const string neighborsFileName = "nodes_neighbors";

    public static float BorderNodePriority { get; set; }
    public static float MaxPathfindingTime { get; set; } 
    
    // Радиус для привязки к нодам (по умолчанию 3.0f - значение из CapsuleGridGenerator.nodeRadius)
    public static float NodeSnapRadius { get; set; } = 3.0f;

    private static readonly ObjectPool<Dictionary<Vector2, PathNodeInfo>> NodeInfoPool = 
        new(() => new Dictionary<Vector2, PathNodeInfo>(50));
    private static Vector2[] _nodes;
    private static KDTree _kdTree;

    static Pathfinder()
    {
        LoadGraphFromResources();
        _nodes = Graph.GetAllNodes()
            .OrderBy(n => n.x)
            .ThenBy(n => n.y)
            .ToArray();
        
        InitializeKDTree();
    }

    private static void InitializeKDTree()
    {
        try
        {
            Dictionary<Vector2, bool> borderNodes = new Dictionary<Vector2, bool>();
            
            foreach (var node in _nodes)
            {
                borderNodes[node] = Graph.IsBorderNode(node);
            }
            
            _kdTree = new KDTree(_nodes, borderNodes);
            Debug.Log($"KD-дерево успешно инициализировано. Количество узлов: {_nodes.Length}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при инициализации KD-дерева: {e.Message}");
        }
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

        // Привязываем исходную и целевую позиции к ближайшим нодам в рамках допустимого радиуса
        Vector2 startNode = SnapToNearestNode(start);
        Vector2 goalNode = SnapToNearestNode(goal);
        
        // Если не удалось привязать позиции к нодам, возвращаем null
        if (startNode == start && !Graph.ContainsNode(start) || 
            goalNode == goal && !Graph.ContainsNode(goal))
        {
            Debug.LogWarning($"Не удалось привязать позиции к нодам. Старт: {start}, цель: {goal}");
            return null;
        }

        var nodeInfo = NodeInfoPool.Get();
        var openSet = new PriorityQueue();
        
        for (int i = 0; i < _nodes.Length; i++)
        {
            nodeInfo[_nodes[i]] = new PathNodeInfo(
                Vector2.zero, 
                float.MaxValue, 
                float.MaxValue
            );
        }

        // Расчет начального эвристического расстояния
        HeuristicResult heuristic = CalculateHeuristic(startNode, goalNode);
        
        nodeInfo[startNode] = new PathNodeInfo(
            Vector2.zero, 
            0f, 
            heuristic.Combined
        );
        
        openSet.Enqueue(startNode, nodeInfo[startNode].FScore);

        while (openSet.Count > 0)
        {
            if (Time.time - startTime > MaxPathfindingTime)
            {
                NodeInfoPool.Release(nodeInfo);
                return null;
            }

            var current = openSet.Dequeue();

            if (current == goalNode)
            {
                var path = ReconstructPath(nodeInfo, current);
                NodeInfoPool.Release(nodeInfo);
                return path;
            }

            var neighbors = Graph.GetNeighbors(current);
            for (int i = 0; i < neighbors.Length; i++)
            {
                var neighbor = neighbors[i];
                
                // Пропускаем ноды, занятые своей командой
                if (OccupiedNodesSystem.IsOccupiedByTeam(neighbor, team))
                    continue;

                MovementCostData movementCost = CalculateMovementCost(current, neighbor);
                float tentativeGScore = nodeInfo[current].GScore + movementCost.TotalCost;

                if (tentativeGScore < nodeInfo[neighbor].GScore)
                {
                    // Расчет эвристики
                    HeuristicResult neighborHeuristic = CalculateHeuristic(neighbor, goalNode);
                    
                    nodeInfo[neighbor] = new PathNodeInfo(
                        current,
                        tentativeGScore,
                        tentativeGScore + neighborHeuristic.Combined
                    );
                    
                    openSet.Enqueue(neighbor, nodeInfo[neighbor].FScore);
                }
            }
        }

        NodeInfoPool.Release(nodeInfo);
        return null;
    }

    public static Vector2[] GetNeighbors(Vector2 nodePosition)
    {
        return Graph.GetNeighbors(nodePosition);
    }

    private static MovementCostData CalculateMovementCost(Vector2 current, Vector2 neighbor)
    {
        MovementCostData cost = new MovementCostData();
        cost.BaseCost = Vector2.Distance(current, neighbor);
        cost.BorderFactor = Graph.IsBorderNode(neighbor) ? BorderNodePriority : 1.0f;
        cost.TotalCost = cost.BaseCost * cost.BorderFactor;
        return cost;
    }

    private static HeuristicResult CalculateHeuristic(Vector2 a, Vector2 b)
    {
        HeuristicResult result = new HeuristicResult();
        result.Manhattan = ManhattanDistance(a, b);
        result.Diagonal = DiagonalDistance(a, b);
        result.Combined = Mathf.Min(result.Manhattan, result.Diagonal);
        return result;
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

    private static List<Vector2> ReconstructPath(Dictionary<Vector2, PathNodeInfo> nodeInfo, Vector2 current)
    {
        var totalPath = ListPool<Vector2>.Get();
        totalPath.Add(current);

        while (nodeInfo[current].CameFrom != Vector2.zero)
        {
            current = nodeInfo[current].CameFrom;
            totalPath.Add(current);
        }

        totalPath.Reverse();
        return totalPath;
    }

    /// <summary>
    /// Привязывает позицию к ближайшей ноде в пределах заданного радиуса
    /// </summary>
    /// <param name="position">Позиция для привязки</param>
    /// <param name="radius">Радиус поиска (если не указан, используется NodeSnapRadius)</param>
    /// <returns>Позиция ближайшей ноды в пределах радиуса или исходная позиция, если нода не найдена</returns>
    public static Vector2 SnapToNearestNode(Vector2 position, float radius = -1)
    {
        // Если граф содержит эту точку, возвращаем её как есть
        if (Graph.ContainsNode(position))
            return position;

        // Используем заданный радиус или значение по умолчанию
        float searchRadius = radius > 0 ? radius : NodeSnapRadius;

        if (_kdTree != null)
        {
            // Используем KD-дерево для быстрого поиска с ограничением радиуса
            return _kdTree.SnapToNearest(position, searchRadius);
        }
        else
        {
            // Запасной вариант с линейным поиском
            Debug.LogWarning("KD-дерево не инициализировано, используется линейный поиск");
            return SnapToNearestNodeLinear(position, searchRadius);
        }
    }
    
    /// <summary>
    /// Линейный поиск ближайшего узла в заданном радиусе (запасной вариант)
    /// </summary>
    private static Vector2 SnapToNearestNodeLinear(Vector2 position, float radius)
    {
        // Если точка уже узел, возвращаем её
        if (Graph.ContainsNode(position))
            return position;

        // Максимальное расстояние для поиска
        float maxDistSq = radius * radius;
        NodeDistanceInfo nearest = new NodeDistanceInfo(position, maxDistSq + 1); // Изначально за пределами радиуса
        
        for (int i = 0; i < _nodes.Length; i++)
        {
            var node = _nodes[i];
            float distanceSquared = (node.x - position.x) * (node.x - position.x) +
                                   (node.y - position.y) * (node.y - position.y);
                                
            if (distanceSquared < nearest.DistanceSquared && distanceSquared <= maxDistSq)
            {
                nearest = new NodeDistanceInfo(node, distanceSquared);
            }
        }

        // Если нашли ноду в радиусе, возвращаем её, иначе - исходную позицию
        return nearest.DistanceSquared <= maxDistSq ? nearest.Node : position;
    }
}