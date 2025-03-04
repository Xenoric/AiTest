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
        currentNode = Pathfinder.SnapToNearestNode((Vector2)transform.position);
        transform.position = currentNode;
        lastPosition = currentNode;
        OccupiedNodesSystem.UpdatePosition(lastPosition, lastPosition, Team, this);
    }

    public void UpdateBot()
    {
        // Get distance to nearest enemy using spatial hash
        float distanceToEnemy = OccupiedNodesSystem.GetDistanceToNearestEnemy(currentNode, Team);
        
        // If enemy is too close, retreat
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

        // Check minimum distance to other friendly bots
        CheckNodesToOtherBots();
    }

    private void CheckNodesToOtherBots()
    {
        List<Bot> nearbyBots = OccupiedNodesSystem.GetNearbyBots(currentNode, MinNodesToOtherBots);
        foreach (var otherBot in nearbyBots)
        {
            if (otherBot != this && otherBot.Team == this.Team)
            {
                float distance = Vector2.Distance(currentNode, otherBot.transform.position);
                if (distance < MinNodesToOtherBots)
                {
                    Vector2 direction = (currentNode - (Vector2)otherBot.transform.position).normalized;
                    Vector2 targetPosition = currentNode + direction * MinNodesToOtherBots;
                    Vector2 targetNode = Pathfinder.SnapToNearestNode(targetPosition);
                    path = Pathfinder.GetPath(currentNode, targetNode, Team);
                    break;
                }
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

        // Check if next node is occupied by friendly bot
        if (OccupiedNodesSystem.IsOccupiedByTeam(next, Team))
        {
            // Find free node among neighbors
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

        // Move towards next node
        Vector2 newPosition = Vector2.MoveTowards(transform.position, next, MoveSpeed * Time.deltaTime);
        transform.position = newPosition;

        // If reached next node
        if (Vector2.Distance(transform.position, next) < WaypointThreshold)
        {
            path.RemoveAt(0);
            lastPosition = currentNode;
            currentNode = next;
            OccupiedNodesSystem.UpdatePosition(lastPosition, currentNode, Team, this);
        }
    }

    public void SetTarget(Vector2 newTarget)
    {
        Vector2 targetNode = Pathfinder.SnapToNearestNode(newTarget);
        path = Pathfinder.GetPath(currentNode, targetNode, Team);
        
        if (path != null)
        {
            TargetPosition = targetNode;
        }
    }

    private void OnDestroy()
    {
        OccupiedNodesSystem.UpdatePosition(lastPosition, Vector2.zero, Team, this);
    }

    void OnDrawGizmos()
    {
        if (path != null && path.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i+1]);
            }
        }
    }
}
