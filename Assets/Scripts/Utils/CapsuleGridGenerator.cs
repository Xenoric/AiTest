using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;


public class CapsuleGridGenerator : MonoBehaviour
{
    [Header("Node Settings")]
    public GameObject nodePrefab;
    public float nodeRadius = 3f;
    public float nodeCenterDistance = 3f;

    [Header("Grid Settings")]
    public Vector2 gridBoundsMin;
    public Vector2 gridBoundsMax;
    public LayerMask boxColliderLayer;
    
    [Header("Debug Settings")]
    [HideInInspector]public bool showNodeNeighborsInEditor = false;

    private List<Vector2> borderNodePositions = new List<Vector2>();
    private List<Vector2> generatedNodePositions = new List<Vector2>();
    private List<NodeNeighborsData> nodesWithNeighbors = new List<NodeNeighborsData>(); // Поле для хранения соседей

    private string FilePath => Path.Combine(Application.dataPath, "grid_nodes.json");

    public void GenerateBorderNodes()
    {
        ClearAllNodes();
        var boxColliders = FindObjectsOfType<BoxCollider2D>()
            .Where(c => (1 << c.gameObject.layer & boxColliderLayer) != 0)
            .ToArray();

        foreach (var boxCollider in boxColliders)
        {
            GenerateNodesByColliderBorders(boxCollider);
        }
    }

    private void GenerateNodesByColliderBorders(BoxCollider2D boxCollider)
    {
        var colliderMatrix = boxCollider.transform.localToWorldMatrix;
        var size = boxCollider.size;
        var offset = boxCollider.offset;

        GenerateNodesOnBorder(
            new Vector2(offset.x - size.x / 2f, offset.y + size.y / 2f),
            new Vector2(offset.x + size.x / 2f, offset.y + size.y / 2f),
            colliderMatrix
        );
    }

    private void GenerateNodesOnBorder(Vector2 startLocal, Vector2 endLocal, Matrix4x4 matrix)
    {
        var startWorld = matrix.MultiplyPoint(startLocal);
        var endWorld = matrix.MultiplyPoint(endLocal);

        var borderLength = Vector2.Distance(startWorld, endWorld);
        Vector2 lineDirection = (endWorld - startWorld).normalized;

        int nodeCount = Mathf.FloorToInt(borderLength / nodeCenterDistance);
        bool hasCentralNode = nodeCount % 2 == 0;

        int startIndex = hasCentralNode ? -nodeCount / 2 : -Mathf.FloorToInt(nodeCount / 2);
        int endIndex = hasCentralNode ? nodeCount / 2 : Mathf.CeilToInt(nodeCount / 2);

        for (int i = startIndex; i <= endIndex; i++)
        {
            Vector2 offset = lineDirection * (i * nodeCenterDistance);
            Vector2 currentPos = startWorld + (endWorld - startWorld) / 2 + (Vector3)offset;

            if (IsPositionInGrid(currentPos))
            {
                CreateNodeAtPosition(currentPos, matrix, true);
            }
        }
    }

    private void CreateNodeAtPosition(Vector2 position, Matrix4x4 matrix, bool isBorderNode = false)
    {
        if (nodePrefab == null)
        {
            Debug.LogError("Node Prefab is not assigned!");
            return;
        }

        Vector2 roundedPosition = new Vector2(
            (float)Math.Round(position.x, 1),
            (float)Math.Round(position.y, 1)
        );

        if (isBorderNode)
        {
            roundedPosition += Vector2.up * (nodeRadius);
        }

        var node = Instantiate(nodePrefab, roundedPosition, Quaternion.identity, transform);
        ConfigureNodeComponents(node);

        if (isBorderNode)
        {
            borderNodePositions.Add(roundedPosition);
        }
        else
        {
            generatedNodePositions.Add(roundedPosition);
        }
    }

    private void ConfigureNodeComponents(GameObject node)
    {
        var spriteRenderer = node.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            float diameter = nodeRadius * 2f;
            float spriteWorldSize = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
            float scaleFactor = diameter / spriteWorldSize;
            node.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    private bool IsPositionInGrid(Vector2 position)
    {
        return position.x >= gridBoundsMin.x && 
               position.x <= gridBoundsMax.x && 
               position.y >= gridBoundsMin.y && 
               position.y <= gridBoundsMax.y;
    }

    public void ClearAllNodes()
    {
        var nodes = GameObject.FindGameObjectsWithTag("Node");
    
        foreach (var node in nodes)
        {
            DestroyImmediate(node);
        }

        borderNodePositions.Clear();
        generatedNodePositions.Clear();
    }

    public void SaveNodesToFile()
    {
        try 
        {
            var nodeData = new List<NodeData>();
            var nodes = GameObject.FindGameObjectsWithTag("Node");

            foreach (var node in nodes)
            {
                Vector2 position = node.transform.position;
                Vector2 roundedPosition = new Vector2(
                    (float)Math.Round(position.x, 1),
                    (float)Math.Round(position.y, 1)
                );

                nodeData.Add(new NodeData(roundedPosition) 
                { 
                    isBorderNode = IsBorderNode(position)
                });
            }

            string jsonData = JsonConvert.SerializeObject(nodeData, Formatting.Indented);
            File.WriteAllText(FilePath, jsonData);

            Debug.Log($"Nodes saved to {FilePath}. Total nodes: {nodeData.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving nodes: {ex.Message}");
        }
    }

    public void LoadNodesFromFile()
    {
        try 
        {
            if (!File.Exists(FilePath))
            {
                Debug.LogWarning($"File not found: {FilePath}");
                return;
            }

            ClearAllNodes();

            string jsonData = File.ReadAllText(FilePath);
            var nodeData = JsonConvert.DeserializeObject<List<NodeData>>(jsonData);

            foreach (var node in nodeData)
            {
                Vector2 nodePosition = node.ToVector2();
                var newNode = Instantiate(nodePrefab, nodePosition, Quaternion.identity, transform);
                ConfigureNodeComponents(newNode);
            }

            Debug.Log($"Nodes loaded from {FilePath}. Total nodes: {nodeData.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading nodes: {ex.Message}");
        }
    }

    private bool IsBorderNode(Vector2 position)
    {
        float raycastDistance = nodeRadius * 1.5f;

        RaycastHit2D hit = Physics2D.Raycast(
            position, 
            Vector2.down, 
            raycastDistance, 
            boxColliderLayer
        );

        // Визуальная отладка луча
        Debug.DrawRay(position, Vector2.down * raycastDistance, 
            hit.collider != null ? Color.red : Color.green, 0.1f);

        return hit.collider != null;
    }
    
    public void SaveNodesWithNeighbors()
    {
        try 
        {
            var nodesWithNeighbors = new List<NodeNeighborsData>();
            var nodes = GameObject.FindGameObjectsWithTag("Node");

            foreach (var currentNode in nodes)
            {
                Vector2 nodePosition = currentNode.transform.position;
            
                var nodeNeighborsData = new NodeNeighborsData
                {
                    currentNode = new NodeData(nodePosition)
                };

                // Проверяем пограничность текущей ноды
                nodeNeighborsData.currentNode.isBorderNode = IsBorderNode(nodePosition);

                Collider2D[] neighborColliders = Physics2D.OverlapCircleAll(
                    nodePosition, 
                    nodeRadius, 
                    LayerMask.GetMask("Node")
                );

                foreach (var neighborCollider in neighborColliders)
                {
                    if (neighborCollider.gameObject == currentNode) continue;

                    Vector2 neighborPosition = neighborCollider.transform.position;
                    var neighborNodeData = new NodeData(neighborPosition)
                    {
                        // Проверяем пограничность каждого соседа
                        isBorderNode = IsBorderNode(neighborPosition)
                    };

                    nodeNeighborsData.neighborNodes.Add(neighborNodeData);
                }

                nodesWithNeighbors.Add(nodeNeighborsData);
            }

            string filePath = Path.Combine(Application.dataPath, "Scripts", "nodes_neighbors.json");
            string jsonData = JsonConvert.SerializeObject(nodesWithNeighbors, Formatting.Indented);
            File.WriteAllText(filePath, jsonData);

            Debug.Log($"Nodes with neighbors saved. Total nodes: {nodesWithNeighbors.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving nodes with neighbors: {ex.Message}");
        }
    }

    private bool IsPointNearLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float tolerance)
    {
        Vector2 lineVector = lineEnd - lineStart;
        Vector2 pointVector = point - lineStart;

        float projection = Vector2.Dot(pointVector, lineVector.normalized);

        if (projection >= 0 && projection <= lineVector.magnitude)
        {
            Vector2 perpendicular = pointVector - projection * lineVector.normalized;
            return perpendicular.magnitude <= tolerance;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        DrawColliderGizmos();
        DrawNodesGizmos();
        DrawNodeNeighborsGizmos();
    }

    private void DrawNodeNeighborsGizmos()
    {
        // Проверяем флаг перед отрисовкой
        if (!showNodeNeighborsInEditor) return;

        // Заполняем список нод с соседями перед отрисовкой
        nodesWithNeighbors.Clear();
        var nodes = GameObject.FindGameObjectsWithTag("Node");

        foreach (var currentNode in nodes)
        {
            var nodeNeighborsData = new NodeNeighborsData
            {
                currentNode = new NodeData(currentNode.transform.position)
            };

            Collider2D[] neighborColliders = Physics2D.OverlapCircleAll(
                currentNode.transform.position, 
                nodeRadius, 
                LayerMask.GetMask("Node")
            );

            foreach (var neighborCollider in neighborColliders)
            {
                if (neighborCollider.gameObject == currentNode) continue;

                nodeNeighborsData.neighborNodes.Add(
                    new NodeData(neighborCollider.transform.position)
                );
            }

            nodesWithNeighbors.Add(nodeNeighborsData);
        }

        // Отрисовка линий между нодами
        foreach (var nodeData in nodesWithNeighbors)
        {
            Vector2 currentNodePosition = nodeData.currentNode.ToVector2();

            foreach (var neighbor in nodeData.neighborNodes)
            {
                Vector2 neighborNodePosition = neighbor.ToVector2();
                Gizmos.color = Color.red;
                Gizmos.DrawLine(currentNodePosition, neighborNodePosition);
            }
        }
    }

    private void DrawColliderGizmos()
    {
        var boxColliders = FindObjectsOfType<BoxCollider2D>()
            .Where(c => (1 << c.gameObject.layer & boxColliderLayer) != 0)
            .ToArray();

        foreach (var boxCollider in boxColliders)
        {
            Gizmos.color = Color.yellow;
            var matrix = boxCollider.transform.localToWorldMatrix;
            var size = boxCollider.size;
            var offset = boxCollider.offset;

            Vector2[] localCorners =
            {
                new Vector2(offset.x - size.x / 2f, offset.y - size.y / 2f),
                new Vector2(offset.x + size.x / 2f, offset.y - size.y / 2f),
                new Vector2(offset.x + size.x / 2f, offset.y + size.y / 2f),
                new Vector2(offset.x - size.x / 2f, offset.y + size.y / 2f)
            };

            Vector3[] worldCorners = localCorners.Select(corner => matrix.MultiplyPoint(corner)).ToArray();

            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(worldCorners[i], worldCorners[(i + 1) % 4]);
            }

            Vector2 worldCenter = matrix.MultiplyPoint(offset);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldCenter, 0.1f);
        }
    }

    private void DrawNodesGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var nodePos in borderNodePositions)
        {
            Gizmos.DrawWireSphere(nodePos, nodeRadius);
        }

        Gizmos.color = Color.blue;
        foreach (var nodePos in generatedNodePositions)
        {
            Gizmos.DrawWireSphere(nodePos, nodeRadius);
        }
    }
}