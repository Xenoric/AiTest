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
    
    // Точность округления координат в графе
    private const float precision = 0.1f;
    
    public static void LoadGraph(string jsonData)
    {
        try
        {
            neighbors.Clear();
            borderNodes.Clear();
            nodeSet.Clear();
            
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