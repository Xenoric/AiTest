using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bot : MonoBehaviour, IBot
{
    private List<Vector2> path = new();
    private Vector2 lastPosition;

    public float MoveSpeed { get; set; } = 5f;
    public float WaypointThreshold { get; set; } = 0.1f;
    public Vector2 TargetPosition { get; set; }
    public int Team { get; set; } = 0;

    public int MinNodesToOtherBots { get; set; } = 3; // Минимальное количество узлов до других ботов
    public float MinDistanceToEnemy { get; set; } = 5f; // Минимальная дистанция до врагов

    private void Start()
    {
        lastPosition = transform.position;
        OccupiedNodesSystem.UpdatePosition(lastPosition, lastPosition, Team);
    }

    public void UpdateBot()
    {
        Vector2 currentPosition = transform.position;

        // Проверяем дистанцию до ближайшего врага
        float distanceToEnemy = OccupiedNodesSystem.GetDistanceToNearestEnemy(currentPosition, Team);
        
        // Если враг слишком близко, отходим
        if (distanceToEnemy < MinDistanceToEnemy)
        {
            Vector2? nearestEnemyPosition = OccupiedNodesSystem.FindNearestEnemyPosition(currentPosition, Team);
            if (nearestEnemyPosition.HasValue)
            {
                Vector2 retreatDirection = (currentPosition - nearestEnemyPosition.Value).normalized;
                Vector2 retreatTarget = currentPosition + retreatDirection * MinDistanceToEnemy;
                path = Pathfinder.GetPath(currentPosition, retreatTarget, Team);
            }
        }
        else
        {
            MoveTowardsTarget();
        }

        // Проверка на минимальное количество узлов до других ботов
        CheckNodesToOtherBots(currentPosition);
        
        // Обновляем позицию в системе
        if (Vector2.Distance(currentPosition, lastPosition) > WaypointThreshold)
        {
            OccupiedNodesSystem.UpdatePosition(lastPosition, currentPosition, Team);
            lastPosition = currentPosition;
        }
    }

    private void CheckNodesToOtherBots(Vector2 currentPosition)
    {
        Bot[] otherTeamBots = FindObjectsOfType<Bot>()
            .Where(bot => bot != this && bot.Team == this.Team)
            .ToArray();

        foreach (var otherBot in otherTeamBots)
        {
            float distance = Vector2.Distance(currentPosition, otherBot.transform.position);
            if (distance < MinNodesToOtherBots)
            {
                Vector2 direction = (currentPosition - (Vector2)otherBot.transform.position).normalized;
                Vector2 targetPosition = (Vector2)transform.position + direction * MinNodesToOtherBots;
                path = Pathfinder.GetPath(currentPosition, targetPosition, Team);
            }
        }
    }

    private void MoveTowardsTarget()
    {
        if (path != null && path.Count > 1)
        {
            Vector2 current = path[0];
            Vector2 next = path[1];

            // Проверяем, не занята ли следующая нода союзником
            if (OccupiedNodesSystem.IsOccupiedByTeam(next, Team))
            {
                // Ищем свободную ноду среди соседей
                Vector2[] neighbors = Pathfinder.GetNeighbors(current);
                foreach (var neighbor in neighbors)
                {
                    if (!OccupiedNodesSystem.IsOccupiedByTeam(neighbor, Team))
                    {
                        path = Pathfinder.GetPath(transform.position, neighbor, Team);
                        Debug.Log($"Moving to free neighbor: {neighbor}");
                        break;
                    }
                }
                return;
            }

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
        path = Pathfinder.GetPath(transform.position, newTarget, Team);
    }

    private void OnDestroy()
    {
        OccupiedNodesSystem.UpdatePosition(lastPosition, Vector2.zero, Team);
    }
}