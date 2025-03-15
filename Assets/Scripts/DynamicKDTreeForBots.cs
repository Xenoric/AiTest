using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Динамическое KD-дерево, оптимизированное для частых обновлений позиций ботов
/// </summary>
public class DynamicKDTreeForBots
{
    /// <summary>
    /// Узел KD-дерева, представляющий бота
    /// </summary>
    private class KDNode
    {
        public Bot Bot;             // Ссылка на бота
        public Vector2 Position;    // Текущая позиция бота
        public int Team;            // Команда бота
        public int Depth;           // Глубина узла в дереве
        public KDNode Left;         // Левое поддерево
        public KDNode Right;        // Правое поддерево
    }

    private KDNode root;
    private Dictionary<int, KDNode> botNodes = new Dictionary<int, KDNode>();
    private float rebuildThreshold = 0.25f;  // Порог для перестроения (доля перемещенных ботов)
    private int updateCount = 0;
    private int rebuildInterval = 100;       // Интервал проверки необходимости перестроения
    private int movedNodesCount = 0;         // Количество перемещенных узлов с момента последнего перестроения

    /// <summary>
    /// Устанавливает интервал проверки необходимости перестроения дерева
    /// </summary>
    public void SetRebuildInterval(int interval)
    {
        rebuildInterval = Mathf.Max(1, interval);
    }

    /// <summary>
    /// Устанавливает порог доли перемещенных ботов для перестроения дерева
    /// </summary>
    public void SetRebuildThreshold(float threshold)
    {
        rebuildThreshold = Mathf.Clamp01(threshold);
    }

    /// <summary>
    /// Добавляет или обновляет позицию бота в дереве
    /// </summary>
    public void UpdatePosition(Bot bot, Vector2 oldPosition, Vector2 newPosition)
    {
        if (bot == null) return;
        
        // Если позиция не изменилась, ничего не делаем
        if (oldPosition == newPosition) return;
        
        // Проверяем, есть ли бот уже в системе
        if (botNodes.TryGetValue(bot.GetInstanceID(), out KDNode node))
        {
            // Существенное перемещение бота (можно настроить порог)
            if (Vector2.SqrMagnitude(node.Position - newPosition) > 0.01f)
            {
                movedNodesCount++;
            }
            
            // Обновляем позицию в узле
            node.Position = newPosition;
            node.Team = bot.Team;
        }
        else
        {
            // Создаем новый узел и вставляем его в дерево
            var newNode = new KDNode 
            { 
                Bot = bot, 
                Position = newPosition, 
                Team = bot.Team,
                Depth = 0 
            };
            
            // Добавляем в словарь для быстрого доступа
            botNodes[bot.GetInstanceID()] = newNode;
            
            if (root == null)
            {
                root = newNode;
            }
            else
            {
                InsertNode(root, newNode);
            }
        }
        
        // Увеличиваем счетчик обновлений
        updateCount++;
        
        // Проверяем, нужно ли перестроить дерево
        if (updateCount >= rebuildInterval)
        {
            if (ShouldRebuildTree())
            {
                RebuildTree();
            }
            updateCount = 0;
            movedNodesCount = 0;
        }
    }

    /// <summary>
    /// Вставляет новый узел в дерево
    /// </summary>
    private void InsertNode(KDNode current, KDNode newNode)
    {
        int axis = current.Depth % 2; // 0 для X, 1 для Y
        
        // Получаем значения для сравнения по текущей оси
        float currentValue = (axis == 0) ? current.Position.x : current.Position.y;
        float newValue = (axis == 0) ? newNode.Position.x : newNode.Position.y;
        
        // Определяем, идти влево или вправо
        if (newValue < currentValue)
        {
            if (current.Left == null)
            {
                current.Left = newNode;
                newNode.Depth = current.Depth + 1;
            }
            else
            {
                InsertNode(current.Left, newNode);
            }
        }
        else
        {
            if (current.Right == null)
            {
                current.Right = newNode;
                newNode.Depth = current.Depth + 1;
            }
            else
            {
                InsertNode(current.Right, newNode);
            }
        }
    }

    /// <summary>
    /// Удаляет бота из дерева
    /// </summary>
    public void RemoveBot(Bot bot)
    {
        if (bot == null) return;
        
        int botId = bot.GetInstanceID();
        
        if (botNodes.TryGetValue(botId, out KDNode node))
        {
            botNodes.Remove(botId);
            
            // Изменения в дереве пометим флагом, чтобы перестроить его при следующей проверке
            movedNodesCount++;
        }
    }

    /// <summary>
    /// Находит всех ботов в заданном радиусе от позиции
    /// </summary>
    public List<Bot> FindBotsInRadius(Vector2 position, float radius)
    {
        List<Bot> result = new List<Bot>();
        float radiusSquared = radius * radius;
        
        SearchBotsInRadius(root, position, radiusSquared, result);
        
        return result;
    }

    /// <summary>
    /// Рекурсивный поиск ботов в заданном радиусе
    /// </summary>
    private void SearchBotsInRadius(KDNode node, Vector2 position, float radiusSquared, List<Bot> result)
    {
        if (node == null) return;
        
        // Вычисляем расстояние до текущего узла
        float distSq = (node.Position - position).sqrMagnitude;
        
        // Если бот в радиусе, добавляем его в результат
        if (distSq <= radiusSquared && node.Bot != null)
        {
            result.Add(node.Bot);
        }
        
        // Определяем ось (0 для X, 1 для Y)
        int axis = node.Depth % 2;
        
        // Получаем значения для сравнения
        float nodeValue = (axis == 0) ? node.Position.x : node.Position.y;
        float targetValue = (axis == 0) ? position.x : position.y;
        
        // Вычисляем расстояние до разделяющей плоскости
        float axisDistSq = (targetValue - nodeValue) * (targetValue - nodeValue);
        
        // Определяем порядок обхода (сначала ветвь, где вероятнее найти результаты)
        KDNode firstBranch, secondBranch;
        
        if (targetValue < nodeValue)
        {
            firstBranch = node.Left;
            secondBranch = node.Right;
        }
        else
        {
            firstBranch = node.Right;
            secondBranch = node.Left;
        }
        
        // Ищем в первой ветви
        SearchBotsInRadius(firstBranch, position, radiusSquared, result);
        
        // Проверяем, может ли вторая ветвь содержать боты в радиусе
        if (axisDistSq <= radiusSquared)
        {
            SearchBotsInRadius(secondBranch, position, radiusSquared, result);
        }
    }
    
    /// <summary>
    /// Находит ботов заданной команды в радиусе
    /// </summary>
    public List<Bot> FindBotsInRadiusByTeam(Vector2 position, float radius, int team, bool sameTeam = true)
    {
        List<Bot> result = new List<Bot>();
        float radiusSquared = radius * radius;
        
        SearchBotsInRadiusByTeam(root, position, radiusSquared, team, sameTeam, result);
        
        return result;
    }
    
    /// <summary>
    /// Рекурсивный поиск ботов заданной команды в радиусе
    /// </summary>
    private void SearchBotsInRadiusByTeam(KDNode node, Vector2 position, float radiusSquared, 
                                          int team, bool sameTeam, List<Bot> result)
    {
        if (node == null) return;
        
        // Вычисляем расстояние до текущего узла
        float distSq = (node.Position - position).sqrMagnitude;
        
        // Если бот в радиусе и соответствует критерию команды, добавляем его в результат
        if (distSq <= radiusSquared && node.Bot != null)
        {
            bool teamMatch = sameTeam ? (node.Team == team) : (node.Team != team);
            if (teamMatch)
            {
                result.Add(node.Bot);
            }
        }
        
        // Определяем ось (0 для X, 1 для Y)
        int axis = node.Depth % 2;
        
        // Получаем значения для сравнения
        float nodeValue = (axis == 0) ? node.Position.x : node.Position.y;
        float targetValue = (axis == 0) ? position.x : position.y;
        
        // Вычисляем расстояние до разделяющей плоскости
        float axisDistSq = (targetValue - nodeValue) * (targetValue - nodeValue);
        
        // Определяем порядок обхода
        KDNode firstBranch, secondBranch;
        
        if (targetValue < nodeValue)
        {
            firstBranch = node.Left;
            secondBranch = node.Right;
        }
        else
        {
            firstBranch = node.Right;
            secondBranch = node.Left;
        }
        
        // Ищем в первой ветви
        SearchBotsInRadiusByTeam(firstBranch, position, radiusSquared, team, sameTeam, result);
        
        // Проверяем, может ли вторая ветвь содержать боты в радиусе
        if (axisDistSq <= radiusSquared)
        {
            SearchBotsInRadiusByTeam(secondBranch, position, radiusSquared, team, sameTeam, result);
        }
    }

    /// <summary>
    /// Находит позицию ближайшего бота противоположной команды
    /// </summary>
    public Vector2? FindNearestEnemyPosition(Vector2 position, int team)
    {
        Bot nearestBot = FindNearestEnemy(position, team);
        
        if (nearestBot != null)
        {
            return nearestBot.transform.position;
        }
        
        return null;
    }

    /// <summary>
    /// Находит ближайшего бота противоположной команды
    /// </summary>
    public Bot FindNearestEnemy(Vector2 position, int team)
    {
        if (root == null) return null;
        
        Bot bestBot = null;
        float bestDistSq = float.MaxValue;
        
        SearchNearestEnemy(root, position, team, ref bestBot, ref bestDistSq);
        
        return bestBot;
    }

    /// <summary>
    /// Рекурсивный поиск ближайшего бота противоположной команды
    /// </summary>
    private void SearchNearestEnemy(KDNode node, Vector2 position, int team, ref Bot bestBot, ref float bestDistSq)
    {
        if (node == null) return;
        
        // Вычисляем расстояние до текущего узла
        float distSq = (node.Position - position).sqrMagnitude;
        
        // Если узел содержит бота из другой команды и он ближе
        if (node.Bot != null && node.Team != team && distSq < bestDistSq)
        {
            bestDistSq = distSq;
            bestBot = node.Bot;
        }
        
        // Определяем ось (0 для X, 1 для Y)
        int axis = node.Depth % 2;
        
        // Получаем значения для сравнения
        float nodeValue = (axis == 0) ? node.Position.x : node.Position.y;
        float targetValue = (axis == 0) ? position.x : position.y;
        
        // Вычисляем расстояние до разделяющей плоскости
        float axisDistSq = (targetValue - nodeValue) * (targetValue - nodeValue);
        
        // Определяем порядок обхода
        KDNode firstBranch, secondBranch;
        
        if (targetValue < nodeValue)
        {
            firstBranch = node.Left;
            secondBranch = node.Right;
        }
        else
        {
            firstBranch = node.Right;
            secondBranch = node.Left;
        }
        
        // Ищем в первой ветви
        SearchNearestEnemy(firstBranch, position, team, ref bestBot, ref bestDistSq);
        
        // Проверяем, может ли вторая ветвь содержать более близкого бота
        if (axisDistSq < bestDistSq)
        {
            SearchNearestEnemy(secondBranch, position, team, ref bestBot, ref bestDistSq);
        }
    }

    /// <summary>
    /// Определяет, нужно ли перестраивать дерево
    /// </summary>
    private bool ShouldRebuildTree()
    {
        if (botNodes.Count == 0) return false;
        
        float movedRatio = (float)movedNodesCount / botNodes.Count;
        return movedRatio >= rebuildThreshold;
    }

    /// <summary>
    /// Перестраивает KD-дерево для оптимизации
    /// </summary>
    private void RebuildTree()
    {
        if (botNodes.Count == 0)
        {
            root = null;
            return;
        }
        
        // Собираем все узлы
        List<KDNode> allNodes = new List<KDNode>(botNodes.Values);
        
        // Перестраиваем дерево
        root = BuildBalancedTree(allNodes, 0);
        
        Debug.Log($"KD-дерево перестроено. Количество ботов: {botNodes.Count}");
    }

    /// <summary>
    /// Строит сбалансированное KD-дерево из списка узлов
    /// </summary>
    private KDNode BuildBalancedTree(List<KDNode> nodes, int depth)
    {
        if (nodes.Count == 0) return null;
        
        // Определяем ось (0 для X, 1 для Y)
        int axis = depth % 2;
        
        // Сортируем узлы по текущей оси
        nodes.Sort((a, b) => 
        {
            if (axis == 0)
                return a.Position.x.CompareTo(b.Position.x);
            else
                return a.Position.y.CompareTo(b.Position.y);
        });
        
        // Находим медиану
        int medianIndex = nodes.Count / 2;
        
        // Создаем узел для медианы
        KDNode node = nodes[medianIndex];
        node.Depth = depth;
        
        // Рекурсивно строим левое поддерево
        if (medianIndex > 0)
        {
            node.Left = BuildBalancedTree(nodes.GetRange(0, medianIndex), depth + 1);
        }
        else
        {
            node.Left = null;
        }
        
        // Рекурсивно строим правое поддерево
        if (medianIndex < nodes.Count - 1)
        {
            node.Right = BuildBalancedTree(nodes.GetRange(medianIndex + 1, nodes.Count - medianIndex - 1), depth + 1);
        }
        else
        {
            node.Right = null;
        }
        
        return node;
    }

    /// <summary>
    /// Возвращает количество ботов в дереве
    /// </summary>
    public int Count
    {
        get { return botNodes.Count; }
    }

    /// <summary>
    /// Очищает дерево
    /// </summary>
    public void Clear()
    {
        botNodes.Clear();
        root = null;
        updateCount = 0;
        movedNodesCount = 0;
    }
    
    /// <summary>
    /// Принудительно перестраивает дерево
    /// </summary>
    public void ForceRebuild()
    {
        RebuildTree();
        updateCount = 0;
        movedNodesCount = 0;
    }
}