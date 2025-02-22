using UnityEngine;
using System;
using System.Collections.Generic;

public static class OccupiedNodesSystem
{
    private static Dictionary<Vector2, int> occupiedNodes = new();
    private static SpatialHash spatialHash = new SpatialHash(5f);
    
    public static Action<Vector2, int> NodeOccupied;
    public static Action<Vector2> NodeReleased;

    public static bool IsOccupiedByTeam(Vector2 node, int team)
    {
        return occupiedNodes.TryGetValue(node, out int occupyingTeam) && occupyingTeam == team;
    }

    public static List<Bot> GetNearbyBots(Vector2 position, float radius)
    {
        return spatialHash.GetNearbyBots(position, radius);
    }

    public static void UpdatePosition(Vector2 oldPosition, Vector2 newPosition, int team, Bot bot)
    {
        if (oldPosition != newPosition)
        {
            if (occupiedNodes.ContainsKey(oldPosition))
            {
                occupiedNodes.Remove(oldPosition);
                NodeReleased?.Invoke(oldPosition);
                spatialHash.RemoveBot(bot, oldPosition);
            }
            
            occupiedNodes[newPosition] = team;
            NodeOccupied?.Invoke(newPosition, team);
            spatialHash.UpdatePosition(bot, oldPosition, newPosition);
        }
    }

    public static float GetDistanceToNearestEnemy(Vector2 position, int team)
    {
        List<Bot> nearbyBots = spatialHash.GetNearbyBots(position, 20f);
        float minDistance = float.MaxValue;

        foreach (var bot in nearbyBots)
        {
            if (bot.Team != team)
            {
                float distance = Vector2.Distance(position, bot.transform.position);
                minDistance = Mathf.Min(minDistance, distance);
            }
        }

        return minDistance == float.MaxValue ? float.PositiveInfinity : minDistance;
    }

    public static Vector2? FindNearestEnemyPosition(Vector2 position, int team)
    {
        List<Bot> nearbyBots = spatialHash.GetNearbyBots(position, 20f);
        float minDistance = float.MaxValue;
        Vector2? nearestEnemy = null;

        foreach (var bot in nearbyBots)
        {
            if (bot.Team != team)
            {
                float distance = Vector2.Distance(position, bot.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = bot.transform.position;
                }
            }
        }

        return nearestEnemy;
    }

    public static void Clear()
    {
        occupiedNodes.Clear();
        spatialHash = new SpatialHash(5f);
    }
}
