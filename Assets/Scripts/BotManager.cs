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

    private int frameCounter = 0;
    public int updateTargetEveryNFrames = 2;

    void Start()
    {
        // Настройка статического Pathfinder
        Pathfinder.BorderNodePriority = borderNodePriority;
        Pathfinder.MaxPathfindingTime = maxPathfindingTime;

        // Инициализация ботов с указанием команды
        InitializeBots(teamOneBots, 1);
        InitializeBots(teamTwoBots, 2);

        // Применяем настройки ко всем ботам
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

        UpdateOccupiedNodes();
        UpdateBotsMovement();
    }

    private void InitializeBots(List<Bot> bots, int teamId)
    {
        foreach (var bot in bots)
        {
            if (bot != null)
            {
                bot.Team = teamId;
            }
        }
    }

    private void ApplySettingsToBots()
    {
        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                bot.MoveSpeed = moveSpeed;
                bot.WaypointThreshold = waypointThreshold;
            }
        }
        foreach (var bot in teamTwoBots)
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
        // Обновление целей для первой команды
        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                Vector2 targetPosition = FindNearestEnemyPosition(bot, teamTwoBots);
                bot.SetTarget(targetPosition);
            }
        }

        // Обновление целей для второй команды
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
        foreach (var bot in teamOneBots)
        {
            bot?.UpdateBot();
        }
        foreach (var bot in teamTwoBots)
        {
            bot?.UpdateBot();
        }
    }

    public void UpdateOccupiedNodes()
    {
        HashSet<Vector2> occupiedNodes = new();

        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                occupiedNodes.Add(bot.transform.position);
            }
        }

        foreach (var bot in teamTwoBots)
        {
            if (bot != null)
            {
                occupiedNodes.Add(bot.transform.position);
            }
        }

        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                bot.UpdateOccupiedNodes(occupiedNodes);
            }
        }

        foreach (var bot in teamTwoBots)
        {
            if (bot != null)
            {
                bot.UpdateOccupiedNodes(occupiedNodes);
            }
        }
    }
}