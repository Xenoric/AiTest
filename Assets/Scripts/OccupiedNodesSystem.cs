using UnityEngine;
using System;
using System.Collections.Generic;

public static class OccupiedNodesSystem
{
    private static Dictionary<Vector2, int> occupiedNodes = new();
    
    public static Action<Vector2, int> NodeOccupied;
    public static Action<Vector2> NodeReleased;

    public static bool IsOccupiedByTeam(Vector2 node, int team)
    {
        return occupiedNodes.TryGetValue(node, out int occupyingTeam) && occupyingTeam == team;
    }

    public static void UpdatePosition(Vector2 oldPosition, Vector2 newPosition, int team)
    {
        if (oldPosition != newPosition)
        {
            if (occupiedNodes.ContainsKey(oldPosition))
            {
                occupiedNodes.Remove(oldPosition);
                NodeReleased?.Invoke(oldPosition);
            }
            
            occupiedNodes[newPosition] = team;
            NodeOccupied?.Invoke(newPosition, team);
        }
    }

    public static float GetDistanceToNearestEnemy(Vector2 position, int team)
    {
        float minDistance = float.MaxValue;

        foreach (var node in occupiedNodes)
        {
            if (node.Value != team)
            {
                float distance = Vector2.Distance(position, node.Key);
                minDistance = Mathf.Min(minDistance, distance);
            }
        }

        return minDistance == float.MaxValue ? float.PositiveInfinity : minDistance;
    }

    public static Vector2? FindNearestEnemyPosition(Vector2 position, int team)
    {
        float minDistance = float.MaxValue;
        Vector2? nearestEnemy = null;

        foreach (var node in occupiedNodes)
        {
            if (node.Value != team)
            {
                float distance = Vector2.Distance(position, node.Key);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = node.Key;
                }
            }
        }

        return nearestEnemy;
    }

    public static void Clear()
    {
        occupiedNodes.Clear();
    }
}