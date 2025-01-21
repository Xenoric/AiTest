using System;
using UnityEngine;

[Serializable]
public class NodeData
{
    public float x;
    public float y;
    public bool isBorderNode;

    public NodeData(Vector2 position)
    {
        x = position.x;
        y = position.y;
    }
    public Vector2 ToVector2() => new(x, y);
}