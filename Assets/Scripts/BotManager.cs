using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotManager : MonoBehaviour
{
    [Header("Bot Settings")]
    public List<Bot> teamOneBots;
    public List<Bot> teamTwoBots;

    [Header("Pathfinding Settings")]
    public float borderNodePriority = 0.5f;
    public float maxPathfindingTime = 2f;

    [Header("Bot Movement Settings")]
    public float moveSpeed = 5f;
    public float waypointThreshold = 0.1f;

    [Header("Distance Settings")]
    [Tooltip("Минимальное количество нод между ботами одной команды")]
    public int minNodesToAlly = 3;
    [Tooltip("Желаемая дистанция до вражеских ботов")]
    public float desiredDistanceToEnemy = 5f;

    private int frameCounter = 0;
    public int updateTargetEveryNFrames = 2;

    void Start()
    {
        Pathfinder.BorderNodePriority = borderNodePriority;
        Pathfinder.MaxPathfindingTime = maxPathfindingTime;

        InitializeBots(teamOneBots, 1);
        InitializeBots(teamTwoBots, 2);
        ApplySettingsToBots();
    }

    void Update()
    {
        frameCounter++;

        if (frameCounter >= updateTargetEveryNFrames)
        {
            UpdateBotsTargets();
            frameCounter = 0;
        }

        UpdateBotsMovement();
    }

    private void InitializeBots(List<Bot> bots, int teamId)
    {
        foreach (var bot in bots)
        {
            if (bot != null)
            {
                bot.Team = teamId;
                bot.DesiredDistanceToEnemy = desiredDistanceToEnemy;
            }
        }
    }

    private void ApplySettingsToBots()
    {
        foreach (var bot in teamOneBots.Concat(teamTwoBots))
        {
            if (bot != null)
            {
                bot.MoveSpeed = moveSpeed;
                bot.WaypointThreshold = waypointThreshold;
            }
        }
    }

    private void UpdateBotsTargets()
    {
        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                Vector2 targetPosition = FindNearestEnemyPosition(bot, teamTwoBots);
                bot.SetTarget(targetPosition);
            }
        }

        foreach (var bot in teamTwoBots)
        {
            if (bot != null)
            {
                Vector2 targetPosition = FindNearestEnemyPosition(bot, teamOneBots);
                bot.SetTarget(targetPosition);
            }
        }
    }

    private Vector2 FindNearestEnemyPosition(Bot currentBot, List<Bot> enemyTeam)
    {
        float minDistance = float.MaxValue;
        Vector2 nearestEnemyPosition = currentBot.transform.position;

        foreach (var enemyBot in enemyTeam)
        {
            if (enemyBot != null)
            {
                float distance = Vector2.Distance(currentBot.transform.position, enemyBot.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemyPosition = enemyBot.transform.position;
                }
            }
        }

        return nearestEnemyPosition;
    }

    private void UpdateBotsMovement()
    {
        foreach (var bot in teamOneBots.Concat(teamTwoBots))
        {
            bot?.UpdateBot();
        }
    }

    private void OnDestroy()
    {
        OccupiedNodesSystem.Clear();
    }
}