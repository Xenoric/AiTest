using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SpatialHash
{
    private readonly float cellSize;
    private readonly Dictionary<Vector2Int, HashSet<Bot>> grid;

    public SpatialHash(float cellSize = 5f)
    {
        this.cellSize = cellSize;
        grid = new Dictionary<Vector2Int, HashSet<Bot>>();
    }

    private Vector2Int GetCell(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize)
        );
    }

    public void UpdatePosition(Bot bot, Vector2 oldPosition, Vector2 newPosition)
    {
        if (bot == null) return;

        // Remove from old cell
        Vector2Int oldCell = GetCell(oldPosition);
        if (grid.TryGetValue(oldCell, out HashSet<Bot> oldCellBots))
        {
            oldCellBots.Remove(bot);
            if (oldCellBots.Count == 0)
            {
                grid.Remove(oldCell);
            }
        }

        // Add to new cell
        Vector2Int newCell = GetCell(newPosition);
        if (!grid.TryGetValue(newCell, out HashSet<Bot> newCellBots))
        {
            newCellBots = new HashSet<Bot>();
            grid[newCell] = newCellBots;
        }
        newCellBots.Add(bot);
    }

    public List<Bot> GetNearbyBots(Vector2 position, float radius)
    {
        List<Bot> nearbyBots = new List<Bot>();
        float squaredRadius = radius * radius;

        // Calculate the cell range to check
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        Vector2Int centerCell = GetCell(position);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                
                if (grid.TryGetValue(cell, out HashSet<Bot> cellBots))
                {
                    // Create a new list of valid bots and remove destroyed ones
                    var validBots = cellBots.Where(bot => bot != null).ToList();
                    
                    // If any bots were destroyed, update the cell
                    if (validBots.Count < cellBots.Count)
                    {
                        cellBots.Clear();
                        foreach (var bot in validBots)
                        {
                            cellBots.Add(bot);
                        }
                        
                        // Remove empty cells
                        if (cellBots.Count == 0)
                        {
                            grid.Remove(cell);
                            continue;
                        }
                    }

                    foreach (Bot bot in validBots)
                    {
                        float squaredDistance = Vector2.SqrMagnitude((Vector2)bot.transform.position - position);
                        if (squaredDistance <= squaredRadius)
                        {
                            nearbyBots.Add(bot);
                        }
                    }
                }
            }
        }

        return nearbyBots;
    }

    public void RemoveBot(Bot bot, Vector2 position)
    {
        if (bot == null) return;

        Vector2Int cell = GetCell(position);
        if (grid.TryGetValue(cell, out HashSet<Bot> cellBots))
        {
            cellBots.Remove(bot);
            if (cellBots.Count == 0)
            {
                grid.Remove(cell);
            }
        }
    }

    public void CleanupDestroyedBots()
    {
        var cellsToRemove = new List<Vector2Int>();

        foreach (var kvp in grid)
        {
            var validBots = kvp.Value.Where(bot => bot != null).ToList();
            
            if (validBots.Count < kvp.Value.Count)
            {
                kvp.Value.Clear();
                foreach (var bot in validBots)
                {
                    kvp.Value.Add(bot);
                }
                
                if (kvp.Value.Count == 0)
                {
                    cellsToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var cell in cellsToRemove)
        {
            grid.Remove(cell);
        }
    }
}
