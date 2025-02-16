using UnityEngine;

public interface IBot
{
    float MoveSpeed { get; set; }
    float WaypointThreshold { get; set; }
    Vector2 TargetPosition { get; set; }
    void UpdateBot();
    void SetTarget(Vector2 newTarget);
}