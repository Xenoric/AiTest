using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public struct NodeInfo
{
    [JsonProperty("currentNode")]
    public Vector2 Position;

    [JsonProperty("neighborNodes")]
    public Vector2[] Neighbors;

    [JsonProperty("currentNode.isBorderNode")]
    public bool IsBorderNode;

    public NodeInfo(Vector2 position, Vector2[] neighbors, bool isBorderNode)
    {
        Position = position;
        Neighbors = neighbors ?? Array.Empty<Vector2>();
        IsBorderNode = isBorderNode;
    }
}



public static class Graph
{
    private static Dictionary<Vector2, Vector2[]> neighbors = new();
    private static HashSet<Vector2> borderNodes = new();
    private static HashSet<Vector2> nodeSet = new();
    
    // Кэш последней позиции для каждого бота
    private static Dictionary<int, Vector2> botLastNode = new();
    
    // Точность округления координат в графе
    private const float precision = 0.1f;
    
    public static void LoadGraph(string jsonData)
    {
        try
        {
            neighbors.Clear();
            borderNodes.Clear();
            nodeSet.Clear();
            botLastNode.Clear();
            
            var nodeNeighborsData = JsonConvert.DeserializeObject<List<NodeNeighborsData>>(jsonData);

            foreach (var nodeData in nodeNeighborsData)
            {
                Vector2 currentNode = new Vector2(nodeData.currentNode.x, nodeData.currentNode.y);
                
                if (nodeData.currentNode.isBorderNode)
                {
                    borderNodes.Add(currentNode);
                }

                Vector2[] neighborNodes = nodeData.neighborNodes
                    .Select(n => new Vector2(n.x, n.y))
                    .ToArray();

                neighbors[currentNode] = neighborNodes;
            }
            
            // Заполняем сет всех узлов для быстрой проверки
            nodeSet = new HashSet<Vector2>(neighbors.Keys);
            
            Debug.Log($"Граф загружен: {nodeSet.Count} узлов, {borderNodes.Count} граничных узлов");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка загрузки графа: {e.Message}");
        }
    }
    
    public static Vector2 SnapToNearestNode(Vector2 position, int botId = -1)
    {
        // Точная проверка - если точка уже узел
        if (nodeSet.Contains(position))
            return position;
            
        // Округляем до десятых как в графе
        Vector2 rounded = new Vector2(
            Mathf.Round(position.x / precision) * precision,
            Mathf.Round(position.y / precision) * precision
        );
        
        // Проверяем, является ли округленная точка узлом
        if (nodeSet.Contains(rounded))
            return rounded;
            
        // Проверяем кэш для конкретного бота
        if (botId >= 0 && botLastNode.TryGetValue(botId, out Vector2 lastNode))
        {
            // Если бот недалеко от последней известной ноды
            float distSq = (lastNode - position).sqrMagnitude;
            if (distSq < 4f) // Порог ~2 юнита
            {
                // Проверяем соседей последней ноды
                if (neighbors.TryGetValue(lastNode, out Vector2[] nodeNeighbors))
                {
                    foreach (var neighbor in nodeNeighbors)
                    {
                        float neighborDistSq = (neighbor - position).sqrMagnitude;
                        if (neighborDistSq < distSq)
                        {
                            botLastNode[botId] = neighbor;
                            return neighbor;
                        }
                    }
                }
                
                return lastNode; // Возвращаем последнюю ноду, если ничего ближе не нашли
            }
        }
        
        // Проверяем 8 соседних ячеек (с шагом 0.1)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                Vector2 check = new Vector2(
                    rounded.x + dx * precision,
                    rounded.y + dy * precision
                );
                
                if (nodeSet.Contains(check))
                {
                    if (botId >= 0)
                        botLastNode[botId] = check;
                    return check;
                }
            }
        }
        
        // Полный поиск по всем узлам (медленный, но гарантированный)
        Vector2 closest = Vector2.zero;
        float minDistSq = float.MaxValue;
        
        foreach (var node in nodeSet)
        {
            float distSq = (node - position).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = node;
            }
        }
        
        if (botId >= 0)
            botLastNode[botId] = closest;
            
        return closest;
    }

    public static Vector2[] GetNeighbors(Vector2 node)
    {
        return neighbors.TryGetValue(node, out var nodeNeighbors) ? nodeNeighbors : Array.Empty<Vector2>();
    }

    public static IEnumerable<Vector2> GetAllNodes()
    {
        return neighbors.Keys;
    }

    public static bool ContainsNode(Vector2 node)
    {
        return neighbors.ContainsKey(node);
    }

    public static bool IsBorderNode(Vector2 node)
    {
        return borderNodes.Contains(node);
    }
}