using System.Collections.Generic;
using UnityEngine;

public class BotMovement : MonoBehaviour, IBotMovement
{
    private List<Vector2> path = new();

    public float MoveSpeed { get; set; } = 5f;
    public float WaypointThreshold { get; set; } = 0.1f;
    public Vector2 TargetPosition { get; set; }

    private IPathfinding pathfinding;

    public void Initialize(IPathfinding pathfinding)
    {
        this.pathfinding = pathfinding;
    }

    public void UpdateBot()
    {
        MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        if (path != null && path.Count > 1)
        {
            Vector2 target = path[1];
            transform.position = Vector2.MoveTowards(transform.position, target, MoveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, target) < WaypointThreshold)
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
}

