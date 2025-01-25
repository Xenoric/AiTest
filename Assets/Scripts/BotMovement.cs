using System.Collections.Generic;
using UnityEngine;

public class BotMovement : MonoBehaviour
{
    private static Pathfinding pathfinding;
    private List<Vector2> path = new();
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float waypointThreshold = 0.1f;
    public Vector2 targetPosition;

    void Awake()
    {
        // Безопасная инициализация Pathfinding
        if (pathfinding == null)
        {
            pathfinding = new Pathfinding();
        }
    }

    public void UpdateBot()
    {
        MoveTowardsTarget();
    }
    
    private void MoveTowardsTarget()
    {
        // Проверяем, что в пути больше одной ноды
        if (path != null && path.Count > 1)
        {
            Vector2 target = path[1];
            
            // Плавное движение к целевой позиции
            transform.position = Vector2.MoveTowards(
                transform.position, 
                target, 
                moveSpeed * Time.deltaTime
            ); 

            // Проверяем, достигнут ли целевой пункт
            if (Vector2.Distance(transform.position, target) < waypointThreshold)
            {
                path.RemoveAt(0); // Удаляем достигнутую ноду из пути
            }
        }
    }

    public void SetTarget(Vector2 newTarget)
    {
        targetPosition = newTarget;
        path = pathfinding.GetPath(transform.position, targetPosition);
    }
}