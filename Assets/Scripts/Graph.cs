using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

public struct NodeInfo
{
    public Vector2 Position;
    public Vector2[] Neighbors;
    public bool IsBorderNode;

    // Конструктор для создания
    public NodeInfo(Vector2 position, Vector2[] neighbors, bool isBorderNode)
    {
        Position = position;
        Neighbors = neighbors;
        IsBorderNode = isBorderNode;
    }
}

public class Graph
{
    // Словарь с новой структурой
    public Dictionary<Vector2, NodeInfo> graph = new();

    public void LoadGraph(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Neighbors file not found: {filePath}");
            return;
        }

        try
        {
            // Десериализация с учетом новой структуры
            string jsonData = File.ReadAllText(filePath);
            var nodeNeighborsData = JsonConvert.DeserializeObject<List<NodeNeighborsData>>(jsonData);

            foreach (var nodeData in nodeNeighborsData)
            {
                // Преобразование данных в новую структуру
                Vector2 currentNodePos = nodeData.currentNode.ToVector2();
                
                // Преобразование соседей в массив позиций
                Vector2[] neighborPositions = nodeData
                    .neighborNodes
                    .Select(n => n.ToVector2())
                    .ToArray();

                // Создание NodeInfo
                var nodeInfo = new NodeInfo(
                    currentNodePos, 
                    neighborPositions, 
                    nodeData.currentNode.isBorderNode
                );

                graph[currentNodePos] = nodeInfo;
            }

            
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading graph: {ex.Message}");
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