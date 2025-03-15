using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// KD-дерево для быстрого поиска ближайшей ноды в 2D пространстве с ограничением радиуса
/// </summary>
public class KDTree
{
    private class KDNode
    {
        public Vector2 Position;    // Позиция ноды
        public bool IsBorderNode;   // Является ли нода граничной
        public KDNode Left;         // Левое поддерево
        public KDNode Right;        // Правое поддерево
        public int Depth;           // Глубина ноды в дереве (для определения оси)

        public KDNode(Vector2 position, bool isBorderNode, int depth)
        {
            Position = position;
            IsBorderNode = isBorderNode;
            Depth = depth;
            Left = null;
            Right = null;
        }
    }

    private KDNode root;                      // Корень дерева
    private Dictionary<Vector2, bool> nodesMap; // Хеш-таблица для быстрой проверки наличия ноды в графе
    
    /// <summary>
    /// Создает экземпляр KD-дерева и строит его из заданного набора нод
    /// </summary>
    /// <param name="nodes">Массив позиций нод в графе</param>
    /// <param name="borderNodes">Словарь, определяющий, является ли нода граничной</param>
    public KDTree(Vector2[] nodes, Dictionary<Vector2, bool> borderNodes = null)
    {
        nodesMap = new Dictionary<Vector2, bool>();
        var nodesList = new List<NodeInfo>(nodes.Length);

        // Создаем список узлов с информацией о граничности
        for (int i = 0; i < nodes.Length; i++)
        {
            bool isBorder = borderNodes != null && borderNodes.TryGetValue(nodes[i], out bool value) && value;
            nodesList.Add(new NodeInfo { Position = nodes[i], IsBorderNode = isBorder });
            nodesMap[nodes[i]] = isBorder;
        }

        // Строим дерево
        root = BuildTree(nodesList, 0);
    }

    /// <summary>
    /// Вспомогательная структура для хранения информации о ноде при построении дерева
    /// </summary>
    private struct NodeInfo
    {
        public Vector2 Position;
        public bool IsBorderNode;
    }

    /// <summary>
    /// Строит KD-дерево из списка нод рекурсивно
    /// </summary>
    /// <param name="nodes">Список нод</param>
    /// <param name="depth">Текущая глубина</param>
    /// <returns>Корень построенного поддерева</returns>
    private KDNode BuildTree(List<NodeInfo> nodes, int depth)
    {
        if (nodes.Count == 0)
            return null;

        // Определяем ось разделения: чередуем X (0) и Y (1)
        int axis = depth % 2;

        // Сортируем точки по выбранной оси
        nodes.Sort((a, b) => 
        {
            if (axis == 0)
                return a.Position.x.CompareTo(b.Position.x);
            else
                return a.Position.y.CompareTo(b.Position.y);
        });

        // Выбираем медиану
        int medianIndex = nodes.Count / 2;
        
        // Создаем узел для медианы
        var nodeInfo = nodes[medianIndex];
        KDNode node = new KDNode(nodeInfo.Position, nodeInfo.IsBorderNode, depth);

        // Рекурсивно строим левое и правое поддеревья
        if (medianIndex > 0)
        {
            node.Left = BuildTree(nodes.GetRange(0, medianIndex), depth + 1);
        }

        if (medianIndex < nodes.Count - 1)
        {
            node.Right = BuildTree(nodes.GetRange(medianIndex + 1, nodes.Count - medianIndex - 1), depth + 1);
        }

        return node;
    }

    /// <summary>
    /// Находит ближайшую ноду к заданной позиции в пределах максимального радиуса
    /// </summary>
    /// <param name="position">Позиция, для которой ищем ближайшую ноду</param>
    /// <param name="maxRadius">Максимальный радиус поиска (если 0 или меньше, радиус не ограничен)</param>
    /// <param name="foundNode">Найденная ближайшая нода. Если нода не найдена, возвращается исходная позиция.</param>
    /// <returns>true, если нода найдена в пределах радиуса, false - если нет</returns>
    public bool FindNearest(Vector2 position, float maxRadius, out Vector2 foundNode)
    {
        // Если точка уже является нодой, возвращаем её
        if (nodesMap.ContainsKey(position))
        {
            foundNode = position;
            return true;
        }

        // Инициализируем переменные для поиска ближайшей точки
        float bestDistSq = maxRadius > 0 ? maxRadius * maxRadius : float.MaxValue;
        Vector2 bestPoint = position; // По умолчанию возвращаем исходную позицию, если ничего не найдено
        bool found = false;

        // Запускаем рекурсивный поиск
        SearchNearest(root, position, ref bestDistSq, ref bestPoint, ref found, maxRadius);

        foundNode = bestPoint;
        return found;
    }

    /// <summary>
    /// Рекурсивный метод поиска ближайшей ноды
    /// </summary>
    private void SearchNearest(KDNode node, Vector2 target, ref float bestDistSq, 
                               ref Vector2 bestPoint, ref bool found, float maxRadius)
    {
        if (node == null)
            return;

        // Вычисляем расстояние до текущего узла
        float distSq = (node.Position - target).sqrMagnitude;

        // Если расстояние меньше текущего лучшего и в пределах максимального радиуса
        if (distSq < bestDistSq)
        {
            bestDistSq = distSq;
            bestPoint = node.Position;
            found = true;
        }

        // Определяем ось (0 для X, 1 для Y)
        int axis = node.Depth % 2;

        // Определяем, нужно ли идти влево или вправо
        float targetValue = axis == 0 ? target.x : target.y;
        float nodeValue = axis == 0 ? node.Position.x : node.Position.y;
        
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

        // Сначала идем в направлении, где потенциально может быть ближайшая точка
        SearchNearest(firstBranch, target, ref bestDistSq, ref bestPoint, ref found, maxRadius);

        // Вычисляем расстояние до разделяющей плоскости
        float axisDistSq = (nodeValue - targetValue) * (nodeValue - targetValue);

        // Если другая ветвь может содержать точки ближе текущей лучшей, проверяем её
        if (axisDistSq < bestDistSq)
        {
            SearchNearest(secondBranch, target, ref bestDistSq, ref bestPoint, ref found, maxRadius);
        }
    }

    /// <summary>
    /// Привязывает позицию к ближайшей ноде в графе в пределах указанного радиуса
    /// </summary>
    /// <param name="position">Исходная позиция</param>
    /// <param name="maxRadius">Максимальный радиус поиска. Если нода не найдена в этом радиусе, возвращается исходная позиция</param>
    /// <returns>Позиция ближайшей ноды или исходная позиция, если нода не найдена в радиусе</returns>
    public Vector2 SnapToNearest(Vector2 position, float maxRadius)
    {
        if (FindNearest(position, maxRadius, out Vector2 nearestNode))
        {
            return nearestNode;
        }
        return position;
    }
}