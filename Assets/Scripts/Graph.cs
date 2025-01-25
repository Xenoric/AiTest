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

public class Graph
{
    public Dictionary<Vector2, NodeInfo> graph = new();

    public bool LoadGraph(string jsonData)
    {
        try
        {
            // Десериализация JSON-строки
            var nodeInfoList = JsonConvert.DeserializeObject<List<NodeInfo>>(jsonData);

            graph = nodeInfoList.ToDictionary(
                node => node.Position,
                node => node
            );

            Debug.Log($"Graph loaded successfully. Total nodes: {graph.Count}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading graph: {ex.Message}");
            return false;
        }
    }

    // Получение соседей
    public Vector2[] GetNeighbors(Vector2 nodePosition)
    {
        return graph.TryGetValue(nodePosition, out var nodeInfo) 
            ? nodeInfo.Neighbors 
            : Array.Empty<Vector2>();
    }

    // Проверка пограничности узла
    public bool IsBorderNode(Vector2 nodePosition)
    {
        return graph.TryGetValue(nodePosition, out var nodeInfo) && nodeInfo.IsBorderNode;
    }

    // Дополнительные методы для работы с графом
    public bool ContainsNode(Vector2 position)
    {
        return graph.ContainsKey(position);
    }

    public IEnumerable<Vector2> GetAllNodes()
    {
        return graph.Keys;
    }
}