using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class OccupiedNodesSystem
{
    private static Dictionary<Vector2, int> occupiedNodes = new();
    
    public static Action<Vector2, int> NodeOccupied;
    public static Action<Vector2> NodeReleased;

    public static bool IsOccupiedByTeam(Vector2 node, int team)
    {
        return occupiedNodes.TryGetValue(node, out int occupyingTeam) && occupyingTeam == team;
    }

    public static bool IsOccupiedByEnemy(Vector2 node, int team)
    {
        return occupiedNodes.TryGetValue(node, out int occupyingTeam) && occupyingTeam != team;
    }

    public static Vector2? FindNearestEnemyPosition(Vector2 position, int team)
    {
        return occupiedNodes
            .Where(node => node.Value != team)
            .OrderBy(node => Vector2.Distance(position, node.Key))
            .Select(node => (Vector2?)node.Key)
            .FirstOrDefault();
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

    public static void Clear()
    {
        occupiedNodes.Clear();
    }
}