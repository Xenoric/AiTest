using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AnyPath.Examples
{
    /// <summary>
    /// Handles the visual part of a seeker. Set's tilemap's cells based on the position of a seeker.
    /// Smoothly fades out cells that lie in the past. 
    /// </summary>
    public class SnakeTail : MonoBehaviour
    {
        // Assigned on instantiation, prevents fighting for cells when tails overlap
        public int Z { get; set; }
        
        [SerializeField] private Snake seeker;
        [SerializeField] private TileBase tile;
        [SerializeField] private Color color;
        
        private List<Vector3Int> tail = new List<Vector3Int>();
        public int Length => tail.Count;

        private void Start()
        {
            //this.color = new Color(Random.value, Random.value, Random.value, .66f);
            seeker.Moved += SeekerOnMoved;
            seeker.ReachedGoal += SeekerOnReachedGoal;
            tail.Add(Vector3Int.zero);
        }

        private void OnDestroy()
        {
            seeker.Moved -= SeekerOnMoved;
            seeker.ReachedGoal -= SeekerOnReachedGoal;

            if (HexGridExample.TileMap != null)
            {
                for (int i = 0; i < tail.Count; i++)
                    GridExampleUtil.ClearTile(HexGridExample.TileMap, tail[i]);
            }
        }

        private void SeekerOnMoved(Vector3Int currentCell)
        {
            currentCell.z = Z;
            this.transform.position = HexGridExample.TileMap.CellToWorld(currentCell);
            
            if (tail.Count > 0)
                GridExampleUtil.ClearTile(HexGridExample.TileMap, tail[tail.Count - 1]);
            GridExampleUtil.SetTile(HexGridExample.TileMap, currentCell, tile, color);
            
            // shift every part
            for (int i = tail.Count - 1; i > 0; i--)
                tail[i] = tail[i - 1];
            tail[0] = currentCell;
        }
        
        private void SeekerOnReachedGoal()
        {
            tail.Add(Vector3Int.zero);
        }
    }
}