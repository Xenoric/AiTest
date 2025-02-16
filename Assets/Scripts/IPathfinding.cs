using System.Collections.Generic;
using UnityEngine;

public interface IPathfinding
{
    float BorderNodePriority { get; set; }
    float MaxPathfindingTime { get; set; }
    List<Vector2> GetPath(Vector2 start, Vector2 goal);
    Vector2[] GetNeighbors(Vector2 nodePosition); // Новый метод
}