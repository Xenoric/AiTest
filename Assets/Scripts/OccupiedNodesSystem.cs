using UnityEngine;
using System;
using System.Collections.Generic;

public static class OccupiedNodesSystem
{
    private static Dictionary<Vector2, int> occupiedNodes = new Dictionary<Vector2, int>();
    private static DynamicKDTreeForBots botTree = new DynamicKDTreeForBots();
    
    // События для оповещения о занятии/освобождении нод
    public static Action<Vector2, int> NodeOccupied;
    public static Action<Vector2> NodeReleased;

    // Настройки для оптимизации производительности
    private static int rebuildInterval = 50; // Количество обновлений перед проверкой необходимости перестроения
    private static float rebuildThreshold = 0.2f; // Порог доли перемещений для перестроения (20%)

    /// <summary>
    /// Инициализация системы с настройками
    /// </summary>
    static OccupiedNodesSystem()
    {
        // Настройка параметров KD-дерева
        botTree.SetRebuildInterval(rebuildInterval);
        botTree.SetRebuildThreshold(rebuildThreshold);
    }

    /// <summary>
    /// Проверяет, занята ли нода ботом указанной команды
    /// </summary>
    public static bool IsOccupiedByTeam(Vector2 node, int team)
    {
        return occupiedNodes.TryGetValue(node, out int occupyingTeam) && occupyingTeam == team;
    }

    /// <summary>
    /// Проверяет, занята ли нода любой командой
    /// </summary>
    public static bool IsOccupied(Vector2 node)
    {
        return occupiedNodes.ContainsKey(node);
    }

    /// <summary>
    /// Получает команду, занимающую указанную ноду (0, если нода свободна)
    /// </summary>
    public static int GetOccupyingTeam(Vector2 node)
    {
        return occupiedNodes.TryGetValue(node, out int team) ? team : 0;
    }

    /// <summary>
    /// Находит всех ботов в указанном радиусе от позиции
    /// </summary>
    public static List<Bot> GetNearbyBots(Vector2 position, float radius)
    {
        return botTree.FindBotsInRadius(position, radius);
    }

    /// <summary>
    /// Находит ботов указанной команды в радиусе
    /// </summary>
    public static List<Bot> GetNearbyBotsByTeam(Vector2 position, float radius, int team, bool sameTeam = true)
    {
        return botTree.FindBotsInRadiusByTeam(position, radius, team, sameTeam);
    }

    /// <summary>
    /// Обновляет позицию бота в системе
    /// </summary>
    public static void UpdatePosition(Vector2 oldPosition, Vector2 newPosition, int team, Bot bot)
    {
        // Проверка на null, чтобы избежать ошибок
        if (bot == null) return;

        // Если позиция не изменилась, ничего не делаем
        if (oldPosition == newPosition) return;

        // Освобождаем старую позицию
        if (occupiedNodes.ContainsKey(oldPosition))
        {
            occupiedNodes.Remove(oldPosition);
            NodeReleased?.Invoke(oldPosition);
        }
        
        // Занимаем новую позицию
        occupiedNodes[newPosition] = team;
        NodeOccupied?.Invoke(newPosition, team);
        
        // Обновляем позицию в KD-дереве
        botTree.UpdatePosition(bot, oldPosition, newPosition);
    }

    /// <summary>
    /// Получает расстояние до ближайшего бота противоположной команды
    /// </summary>
    public static float GetDistanceToNearestEnemy(Vector2 position, int team)
    {
        Bot nearestEnemy = botTree.FindNearestEnemy(position, team);
        
        if (nearestEnemy != null)
        {
            return Vector2.Distance(position, nearestEnemy.transform.position);
        }
        
        return float.PositiveInfinity;
    }

    /// <summary>
    /// Находит позицию ближайшего бота противоположной команды
    /// </summary>
    public static Vector2? FindNearestEnemyPosition(Vector2 position, int team)
    {
        return botTree.FindNearestEnemyPosition(position, team);
    }

    /// <summary>
    /// Удаляет бота из системы при его уничтожении
    /// </summary>
    public static void RemoveBot(Bot bot, Vector2 position)
    {
        if (bot == null) return;
        
        // Освобождаем ноду
        if (occupiedNodes.ContainsKey(position))
        {
            occupiedNodes.Remove(position);
            NodeReleased?.Invoke(position);
        }
        
        // Удаляем из KD-дерева
        botTree.RemoveBot(bot);
    }

    /// <summary>
    /// Очищает систему
    /// </summary>
    public static void Clear()
    {
        occupiedNodes.Clear();
        botTree.Clear();
    }
    
    /// <summary>
    /// Возвращает количество ботов в системе
    /// </summary>
    public static int BotCount
    {
        get { return botTree.Count; }
    }
    
    /// <summary>
    /// Принудительно перестраивает KD-дерево
    /// </summary>
    public static void RebuildBotTree()
    {
        botTree.ForceRebuild();
    }
}