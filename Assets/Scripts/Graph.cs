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
    private static Vector2[] allNodesCache;
    private static bool isDirty = true;

    public static void LoadGraph(string jsonData)
    {
        try
        {
            var nodeNeighborsData = JsonConvert.DeserializeObject<List<NodeNeighborsData>>(jsonData);
            neighbors.Clear();
            borderNodes.Clear();

            foreach (var nodeData in nodeNeighborsData)
            {
                Vector2 currentNode = new Vector2(nodeData.currentNode.x, nodeData.currentNode.y);
                
                if (nodeData.currentNode.isBorderNode)
                {
                    borderNodes.Add(currentNode);
                }

                Vector2[] neighborNodes = new Vector2[nodeData.neighborNodes.Count];
                for (int i = 0; i < nodeData.neighborNodes.Count; i++)
                {
                    neighborNodes[i] = new Vector2(nodeData.neighborNodes[i].x, nodeData.neighborNodes[i].y);
                }

                neighbors[currentNode] = neighborNodes;
            }
            
            isDirty = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading graph: {e.Message}");
        }
    }

    public static Vector2[] GetNeighbors(Vector2 node)
    {
        return neighbors.TryGetValue(node, out var nodeNeighbors) ? nodeNeighbors : Array.Empty<Vector2>();
    }

    public static Vector2[] GetAllNodes()
    {
        if (isDirty || allNodesCache == null)
        {
            allNodesCache = new Vector2[neighbors.Count];
            int index = 0;
            foreach (var key in neighbors.Keys)
            {
                allNodesCache[index++] = key;
            }
            isDirty = false;
        }
        
        return allNodesCache;
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