using System.Collections.Generic;
using UnityEngine;

public interface IPathfinding
{
    List<Vector2> FindPath(Vector2 start, Vector2 goal);
}