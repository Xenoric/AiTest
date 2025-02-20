using System.Collections.Generic;
using UnityEngine;

public class Bot : MonoBehaviour, IBot
{
    private List<Vector2> path = new();
    private Vector2 lastPosition;
    private Vector2 lastEnemyPosition;
    private float enemyStationaryTimer = 0f;
    private const float ENEMY_STATIONARY_THRESHOLD = 0.5f; // Время для определения, что враг стоит на месте

    public float MoveSpeed { get; set; } = 5f;
    public float WaypointThreshold { get; set; } = 0.1f;
    public Vector2 TargetPosition { get; set; }
   
    public int Team { get; set; } = 0;
    public float DesiredDistanceToEnemy { get; set; } = 5f; // Желаемая дистанция до врага

    private void Start()
    {
        lastPosition = transform.position;
        OccupiedNodesSystem.UpdatePosition(lastPosition, lastPosition, Team);
    }

    public void UpdateBot()
    {
        Vector2 currentPosition = transform.position;
        
        // Получаем позицию ближайшего врага
        var nearestEnemyPosition = OccupiedNodesSystem.FindNearestEnemyPosition(currentPosition, Team);
        
        if (nearestEnemyPosition.HasValue)
        {
            float distanceToEnemy = Vector2.Distance(currentPosition, nearestEnemyPosition.Value);

            // Проверяем, двигается ли враг
            bool isEnemyStationary = IsEnemyStationary(nearestEnemyPosition.Value);

            if (distanceToEnemy > DesiredDistanceToEnemy)
            {
                // Если враг далеко, приближаемся к нему
                Vector2 directionToEnemy = (nearestEnemyPosition.Value - currentPosition).normalized;
                Vector2 targetPosition = nearestEnemyPosition.Value - directionToEnemy * DesiredDistanceToEnemy;
                path = FindPathAvoidingAllies(currentPosition, targetPosition);
            }
            else if (distanceToEnemy < DesiredDistanceToEnemy)
            {
                // Если враг близко, отходим
                Vector2 retreatDirection = (currentPosition - nearestEnemyPosition.Value).normalized;
                Vector2 retreatTarget = currentPosition + retreatDirection * (DesiredDistanceToEnemy - distanceToEnemy);
                path = FindPathAvoidingAllies(currentPosition, retreatTarget);
            }
            else if (!isEnemyStationary)
            {
                // Если враг двигается, корректируем позицию для поддержания дистанции
                Vector2 directionToEnemy = (nearestEnemyPosition.Value - currentPosition).normalized;
                Vector2 targetPosition = nearestEnemyPosition.Value - directionToEnemy * DesiredDistanceToEnemy;
                path = FindPathAvoidingAllies(currentPosition, targetPosition);
            }
            else
            {
                // Если враг стоит и мы на нужной дистанции, останавливаемся
                path = null;
            }

            lastEnemyPosition = nearestEnemyPosition.Value;
        }

        if (path != null && path.Count > 0)
        {
            MoveTowardsTarget();
        }

        // Обновляем позицию в системе
        if (Vector2.Distance(currentPosition, lastPosition) > WaypointThreshold)
        {
            OccupiedNodesSystem.UpdatePosition(lastPosition, currentPosition, Team);
            lastPosition = currentPosition;
        }
    }

    private bool IsEnemyStationary(Vector2 currentEnemyPosition)
    {
        if (Vector2.Distance(currentEnemyPosition, lastEnemyPosition) < WaypointThreshold)
        {
            enemyStationaryTimer += Time.deltaTime;
            return enemyStationaryTimer >= ENEMY_STATIONARY_THRESHOLD;
        }
        
        enemyStationaryTimer = 0f;
        return false;
    }

    private List<Vector2> FindPathAvoidingAllies(Vector2 start, Vector2 goal)
    {
        var path = Pathfinder.GetPath(start, goal, Team);
        
        // Если путь найден и следующая нода занята союзником
        if (path != null && path.Count > 1 && OccupiedNodesSystem.IsOccupiedByTeam(path[1], Team))
        {
            // Ищем свободную ноду среди соседей
            Vector2[] neighbors = Pathfinder.GetNeighbors(path[0]);
            foreach (var neighbor in neighbors)
            {
                if (!OccupiedNodesSystem.IsOccupiedByTeam(neighbor, Team))
                {
                    return Pathfinder.GetPath(start, neighbor, Team);
                }
            }

            // Если все соседние ноды заняты, ищем среди соседей соседей
            foreach (var neighbor in neighbors)
            {
                Vector2[] secondaryNeighbors = Pathfinder.GetNeighbors(neighbor);
                foreach (var secondaryNeighbor in secondaryNeighbors)
                {
                    if (!OccupiedNodesSystem.IsOccupiedByTeam(secondaryNeighbor, Team))
                    {
                        return Pathfinder.GetPath(start, secondaryNeighbor, Team);
                    }
                }
            }
        }

        return path;
    }

    private void MoveTowardsTarget()
    {
        if (path != null && path.Count > 1)
        {
            Vector2 next = path[1];
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
        path = FindPathAvoidingAllies(transform.position, newTarget);
    }

    private void OnDestroy()
    {
        OccupiedNodesSystem.UpdatePosition(lastPosition, Vector2.zero, Team);
    }
}