using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bot : MonoBehaviour, IBot
{
    private List<Vector2> path = new();
    private Vector2 lastPosition;
    private Vector2 currentNode;

    public float MoveSpeed { get; set; } = 5f;
    public float WaypointThreshold { get; set; } = 0.1f;
    public Vector2 TargetPosition { get; set; }
    public int Team { get; set; } = 0;

    public int MinNodesToOtherBots { get; set; } = 3;
    public float MinDistanceToEnemy { get; set; } = 5f;

    private void Start()
    {
        // Смещаем бота на ближайшую ноду только при инициализации
        currentNode = Pathfinder.SnapToNearestNode((Vector2)transform.position);
        transform.position = currentNode;
        lastPosition = currentNode;
        OccupiedNodesSystem.UpdatePosition(lastPosition, lastPosition, Team);
    }

    public void UpdateBot()
    {
        // Проверяем дистанцию до ближайшего врага
        float distanceToEnemy = OccupiedNodesSystem.GetDistanceToNearestEnemy(currentNode, Team);
        
        // Если враг слишком близко, отходим
        if (distanceToEnemy < MinDistanceToEnemy)
        {
            Vector2? nearestEnemyPosition = OccupiedNodesSystem.FindNearestEnemyPosition(currentNode, Team);
            if (nearestEnemyPosition.HasValue)
            {
                Vector2 retreatDirection = (currentNode - nearestEnemyPosition.Value).normalized;
                Vector2 retreatTarget = currentNode + retreatDirection * MinDistanceToEnemy;
                Vector2 targetNode = Pathfinder.SnapToNearestNode(retreatTarget);
                path = Pathfinder.GetPath(currentNode, targetNode, Team);
            }
        }
        else
        {
            MoveTowardsTarget();
        }

        // Проверка на минимальное количество узлов до других ботов
        CheckNodesToOtherBots();
    }

    private void CheckNodesToOtherBots()
    {
        Bot[] otherTeamBots = FindObjectsOfType<Bot>()
            .Where(bot => bot != this && bot.Team == this.Team)
            .ToArray();

        foreach (var otherBot in otherTeamBots)
        {
            float distance = Vector2.Distance(currentNode, otherBot.transform.position);
            if (distance < MinNodesToOtherBots)
            {
                Vector2 direction = (currentNode - (Vector2)otherBot.transform.position).normalized;
                Vector2 targetPosition = currentNode + direction * MinNodesToOtherBots;
                Vector2 targetNode = Pathfinder.SnapToNearestNode(targetPosition);
                path = Pathfinder.GetPath(currentNode, targetNode, Team);
            }
        }
    }

    private void MoveTowardsTarget()
    {
        if (path == null || path.Count < 2)
        {
            return;
        }

        Vector2 next = path[1];

        // Проверяем, не занята ли следующая нода союзником
        if (OccupiedNodesSystem.IsOccupiedByTeam(next, Team))
        {
            // Ищем свободную ноду среди соседей текущей ноды
            Vector2[] neighbors = Pathfinder.GetNeighbors(currentNode);
            foreach (var neighbor in neighbors)
            {
                if (!OccupiedNodesSystem.IsOccupiedByTeam(neighbor, Team))
                {
                    path = Pathfinder.GetPath(currentNode, neighbor, Team);
                    return;
                }
            }
            return;
        }

        // Плавно перемещаемся к следующей ноде
        Vector2 newPosition = Vector2.MoveTowards(transform.position, next, MoveSpeed * Time.deltaTime);
        transform.position = newPosition;

        // Если достигли следующей ноды
        if (Vector2.Distance(transform.position, next) < WaypointThreshold)
        {
            path.RemoveAt(0);
            lastPosition = currentNode;
            currentNode = next;
            OccupiedNodesSystem.UpdatePosition(lastPosition, currentNode, Team);
        }
    }

    public void SetTarget(Vector2 newTarget)
    {
        // Находим ближайшую ноду для цели
        Vector2 targetNode = Pathfinder.SnapToNearestNode(newTarget);
        path = Pathfinder.GetPath(currentNode, targetNode, Team);
        
        if (path != null)
        {
            TargetPosition = targetNode;
        }
    }

    private void OnDestroy()
    {
        OccupiedNodesSystem.UpdatePosition(lastPosition, Vector2.zero, Team);
    }
}