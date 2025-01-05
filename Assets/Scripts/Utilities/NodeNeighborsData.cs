using System;
using System.Collections.Generic;

[Serializable]
public class NodeNeighborsData
{
    public NodeData currentNode;
    public List<NodeData> neighborNodes = new List<NodeData>();
}