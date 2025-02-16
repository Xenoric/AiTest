using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bot : MonoBehaviour, IBot
{
    private List<Vector2> path = new();
    private HashSet<Vector2> occupiedNodes = new(); // Хранит занятые узлы

    public float MoveSpeed { get; set; } = 5f;
    public float WaypointThreshold { get; set; } = 0.1f;
    public Vector2 TargetPosition { get; set; }

    // Новое свойство для хранения минимального количества узлов до других ботов
    public int MinNodesToOtherBots { get; set; } = 3;

    // Новое свойство для хранения команды бота
    public int Team { get; set; } = 0;

    private IPathfinding pathfinding;

    public void Initialize(IPathfinding pathfinding)
    {
        this.pathfinding = pathfinding;
    }

    public void UpdateBot()
    {
        // Проверка количества оставшихся узлов до других ботов
        CheckNodesToOtherBots();

        MoveTowardsTarget();
    }

    private void CheckNodesToOtherBots()
    {
        // Получаем все боты из другой команды
        Bot[] otherTeamBots = FindObjectsOfType<Bot>()
            .Where(bot => bot != this && bot.Team != this.Team) // Исключаем текущего бота и ботов из своей команды
            .ToArray();

        foreach (var otherBot in otherTeamBots)
        {
            // Получаем количество оставшихся узлов до другого бота
            int remainingNodes = GetRemainingNodesTo(otherBot);

            if (remainingNodes < MinNodesToOtherBots)
            {
                // Логика для удержания дистанции
                Vector2 direction = (transform.position - otherBot.transform.position).normalized;
                TargetPosition = (Vector2)transform.position + direction * MinNodesToOtherBots; // Изменяем цель
                path = pathfinding.GetPath(transform.position, TargetPosition); // Перестраиваем путь
            }
        }
    }

    private int GetRemainingNodesTo(Bot otherBot)
    {
        // Получаем путь до цели другого бота
        List<Vector2> otherBotPath = otherBot.GetPath();

        // Возвращаем количество оставшихся узлов
        return otherBotPath.Count;
    }

    private void MoveTowardsTarget()
    {
        if (path != null && path.Count > 1)
        {
            Vector2 current = path[0];
            Vector2 next = path[1];

            // Проверка занятости следующего узла
            if (occupiedNodes.Contains(next))
            {
                // Найти свободного соседа текущего узла
                Vector2[] neighbors = pathfinding.GetNeighbors(current);
                Vector2 newStart = current;

                foreach (var neighbor in neighbors)
                {
                    if (!occupiedNodes.Contains(neighbor))
                    {
                        newStart = neighbor;
                        break;
                    }
                }

                // Перестроить путь от нового узла
                path = pathfinding.GetPath(newStart, TargetPosition);
                return;
            }

            // Движение к следующему узлу
            transform.position = Vector2.MoveTowards(transform.position, next, MoveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, next) < WaypointThreshold)
            {
                path.RemoveAt(0);
            }
        }
    }

    public void SetTarget(Vector2 newTarget)
    {
        TargetPosition = newTarget;
        path = pathfinding.GetPath(transform.position, newTarget);
    }

    public void UpdateOccupiedNodes(HashSet<Vector2> newOccupiedNodes)
    {
        occupiedNodes = newOccupiedNodes;
    }

    // Метод для получения текущего пути
    public List<Vector2> GetPath()
    {
        return path;
    }
}