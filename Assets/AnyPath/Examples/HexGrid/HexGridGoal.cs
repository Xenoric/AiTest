using System.Collections;
using System.Collections.Generic;
using AnyPath.Native.Util;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AnyPath.Examples
{
    /// <summary>
    /// A goal that the snakes capture
    /// </summary>
    public class HexGridGoal : MonoBehaviour
    {
        [SerializeField] private Color color;
        
        public Text priorityText;
        
        public static List<HexGridGoal> All { get; } = new List<HexGridGoal>();
        public int Priority { get; private set; }
        public bool IsSeeked { get; set; }

        private const int Z = 2;
        
        public void Initialize(int2 cell)
        {
            transform.position = HexGridExample.TileMap.CellToWorld(cell.ToVector3Int());
            Priority = Random.Range(0, 100);
            priorityText.text = Priority.ToString();
        }
        
        private void Start()
        {
            StartCoroutine(Blink());
            All.Add(this);
            HexGridExample.Randomized += OnGridRandomized;
        }

        private void OnDestroy()
        {
            All.Remove(this);
            HexGridExample.Randomized -= OnGridRandomized;
        }

        private void OnGridRandomized()
        {
            StopAllCoroutines();
            
            if (HexGridExample.HexGrid.IsOpen(HexGridExample.TransformToNode(transform)))
            {
                // flash again since all tiles are cleared
                StartCoroutine(Blink());
            }
            else
            {
                // destroy if we're on an invalid position
                GridExampleUtil.ClearTile(HexGridExample.TileMap, HexGridExample.TransformToNode(transform).ToVector3Int(Z));
                Destroy(this.gameObject);
            }
        }

        public void OnReached()
        {
            // clear tile
            GridExampleUtil.ClearTile(HexGridExample.TileMap, HexGridExample.TransformToNode(transform).ToVector3Int(Z));
            Destroy(this.gameObject);
        }
        
        
        IEnumerator Blink()
        {
            float t = 0;
            float d = .5f;
            Color targetColor = color;
            Vector3 startScale = new Vector3(5,5,1);
            
            while (t < d)
            {
                var cell = HexGridExample.TransformToNode(transform).ToVector3Int(Z);
                
                t += Time.deltaTime;
                float v = 1 - Mathf.Clamp01(t / d);
                v *= v;
                v = 1 - v;
                
                GridExampleUtil.SetTile(
                    HexGridExample.TileMap, 
                    cell, 
                    HexGridExample.Tile, 
                    Color.Lerp(Color.clear, targetColor, v));
                
                GridExampleUtil.ScaleTile(
                    HexGridExample.TileMap,
                    cell,
                    Vector3.Lerp(startScale, Vector3.one, v));
                
                yield return null;
            }
        }
    }
}