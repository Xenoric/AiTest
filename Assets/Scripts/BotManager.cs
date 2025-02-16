using System.Collections.Generic;
using UnityEngine;

public class BotManager : MonoBehaviour
{
    [Header("Bot Settings")]
    public List<Bot> teamOneBots; // Список ботов первой команды
    public List<Bot> teamTwoBots; // Список ботов второй команды

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

        // Обновляем занятость узлов
        UpdateOccupiedNodes();

        // Обновляем движение ботов каждый кадр
        UpdateBotsMovement();
    }

    private void InitializeBots(List<Bot> bots)
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
        // Логика обновления целей для ботов
        foreach (var bot in teamOneBots)
        {
            if (bot != null)
            {
                // Установите новую цель для бота
                bot.SetTarget(GetRandomTargetPosition());
            }
        }
        foreach (var bot in teamTwoBots)
        {
            if (bot != null)
            {
                // Установите новую цель для бота
                bot.SetTarget(GetRandomTargetPosition());
            }
        }
    }

    private Vector2 GetRandomTargetPosition()
    {
        // Генерация случайной позиции цели
        return new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
    }

    private void UpdateBotsMovement()
    {
        // Обновление движения всех ботов
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