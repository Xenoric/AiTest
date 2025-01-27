using UnityEngine;
using System.Collections.Generic;

public class BotManager : MonoBehaviour
{
    [Header("Bot Settings")]
    public List<BotMovement> teamOneBots; // Список ботов первой команды
    public List<BotMovement> teamTwoBots; // Список ботов второй команды

    [Header("Pathfinding Settings")]
    public float borderNodePriority = 0.5f; // Приоритет пограничных узлов
    public float maxPathfindingTime = 2f; // Максимальное время поиска пути

    [Header("Bot Movement Settings")]
    public float moveSpeed = 5f; // Скорость движения ботов
    public float waypointThreshold = 0.1f; // Порог достижения точки пути

    private int frameCounter = 0; // Счетчик кадров
    public int updateTargetEveryNFrames = 2; // Обновлять цель каждые N кадров

    private IPathfinding pathfinding;

    void Start()
    {
        // Инициализация Pathfinding
        pathfinding = new Pathfinding
        {
            BorderNodePriority = borderNodePriority,
            MaxPathfindingTime = maxPathfindingTime
        };

        // Инициализация ботов
        InitializeBots(teamOneBots);
        InitializeBots(teamTwoBots);

        // Применяем настройки ко всем ботам
        ApplySettingsToBots();
    }

    void Update()
    {
        frameCounter++;

        // Обновляем цели только когда счетчик кадров достигает заданного значения
        if (frameCounter >= updateTargetEveryNFrames)
        {
            UpdateBotsTargets();
            frameCounter = 0; // Сбрасываем счетчик
        }

        // Обновляем движение ботов каждый кадр
        UpdateBotsMovement();
    }

    private void InitializeBots(List<BotMovement> bots)
    {
        foreach (var bot in bots)
        {
            if (bot != null)
            {
                bot.Initialize(pathfinding);
            }
        }
    }

    private void ApplySettingsToBots()
    {
        // Применяем настройки ко всем ботам
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
        // Обновляем цели для ботов первой команды
        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                Vector2 nearestBotPosition = FindNearestOpposingBot(bot, teamTwoBots);

                // Проверяем, изменилась ли цель
                if (bot.TargetPosition != nearestBotPosition)
                {
                    bot.SetTarget(nearestBotPosition); // Устанавливаем цель на ближайшего противника
                }
            }
        }
        // Обновляем цели для ботов второй команды
        foreach (var bot in teamTwoBots)
        {
            if (bot != null)
            {
                Vector2 nearestBotPosition = FindNearestOpposingBot(bot, teamOneBots);

                // Проверяем, изменилась ли цель
                if (bot.TargetPosition != nearestBotPosition)
                {
                    bot.SetTarget(nearestBotPosition); // Устанавливаем цель на ближайшего противника
                }
            }
        }
    }

    private void UpdateBotsMovement()
    {
        // Обновляем движение всех ботов
        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                bot.UpdateBot();
            }
        }
        foreach (var bot in teamTwoBots)
        {
            if (bot != null)
            {
                bot.UpdateBot();
            }
        }
    }

    private Vector2 FindNearestOpposingBot(BotMovement bot, List<BotMovement> opposingBots)
    {
        Vector2 nearestPosition = Vector2.zero;
        float nearestDistanceSquared = float.MaxValue;

        foreach (var opposingBot in opposingBots)
        {
            if (opposingBot == null) continue;

            float distanceSquared = (bot.transform.position.x - opposingBot.transform.position.x) * (bot.transform.position.x - opposingBot.transform.position.x) +
                                    (bot.transform.position.y - opposingBot.transform.position.y) * (bot.transform.position.y - opposingBot.transform.position.y);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestPosition = opposingBot.transform.position;
            }
        }
        return nearestPosition; // Возвращаем позицию ближайшего противника
    }
}