using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bot : MonoBehaviour, IBot
{
    private List<Vector2> path = new();
    private HashSet<Vector2> occupiedNodes = new();

    public float MoveSpeed { get; set; } = 5f;
    public float WaypointThreshold { get; set; } = 0.1f;
    public Vector2 TargetPosition { get; set; }
    public int MinNodesToOtherBots { get; set; } = 3;
    public int Team { get; set; } = 0;
    
    public void UpdateBot()
    {
        CheckNodesToOtherBots();
        MoveTowardsTarget();
    }

    private void CheckNodesToOtherBots()
    {
        Bot[] otherTeamBots = FindObjectsOfType<Bot>()
            .Where(bot => bot != this && bot.Team != this.Team)
            .ToArray();

        foreach (var otherBot in otherTeamBots)
        {
            int remainingNodes = GetRemainingNodesTo(otherBot);

            if (remainingNodes < MinNodesToOtherBots)
            {
                Vector2 direction = (transform.position - otherBot.transform.position).normalized;
                TargetPosition = (Vector2)transform.position + direction * MinNodesToOtherBots;
                path = Pathfinder.GetPath(transform.position, TargetPosition);
            }
        }
    }

    private int GetRemainingNodesTo(Bot otherBot)
    {
        List<Vector2> otherBotPath = path;
        return otherBotPath.Count;
    }

    private void MoveTowardsTarget()
    {
        if (path != null && path.Count > 1)
        {
            Vector2 current = path[0];
            Vector2 next = path[1];

            if (occupiedNodes.Contains(next))
            {
                Vector2[] neighbors = Pathfinder.GetNeighbors(current);
                Vector2 newStart = current;

                foreach (var neighbor in neighbors)
                {
                    if (!occupiedNodes.Contains(neighbor))
                    {
                        newStart = neighbor;
                        break;
                    }
                }

                path = Pathfinder.GetPath(newStart, TargetPosition);
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
        path = Pathfinder.GetPath(transform.position, newTarget);
    }

    public void UpdateOccupiedNodes(HashSet<Vector2> newOccupiedNodes)
    {
        occupiedNodes = newOccupiedNodes;
    }

    public List<Vector2> GetPath()
    {
        return path;
    }
}