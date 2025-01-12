using System;
using System.Collections.Generic;
using AnyPath.Graphs.HexGrid;
using AnyPath.Managed;
using AnyPath.Native.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace AnyPath.Examples
{
    /// <summary>
    /// This example demonstrates the usage of the built in hexagonal grid, as well as different OptionFinder types.
    /// The actual pathfinding is done in the <see cref="Snake"/> class.
    /// </summary>
    public class HexGridExample : MonoBehaviour
    {
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private TileBase terrainTile;
        [SerializeField] private Tilemap terrainMap;
        [SerializeField] private GameObject seekerPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private Color hoverColor;
        [SerializeField] private Color wallColor;
        [SerializeField] private int startSnakeCount = 1;
        
        private static HexGridExample instance;
        public static Tilemap TileMap => instance.terrainMap;
        public static TileBase Tile => instance.terrainTile;

        /// <summary>
        /// This defines the way the snakes behave in selecting which goal to seek
        /// </summary>
        public static GoalFindMethod GoalFindMethod { get; private set; }

        // Our hexagonal grid, the snakes use this for their pathfinding queries
        public static HexGrid HexGrid { get; private set; }
        
        // Every time the grid gets updated, this number gets incremented, causing the snakes to
        // refresh their paths
        public static int GridVersion { get; private set; }
        
        // Occurs when the grid has been updated, causes goals to remove when they're no longer on a valid position
        public static event Action Randomized;
        
        private Vector3Int prevCell;
        private List<Snake> snakes = new List<Snake>();
        
        private void Start()
        {
            instance = this;
            
            // Create a random grid
            HexGrid = GenerateGrid();
            
            SetSnakeCount(startSnakeCount);
            DisplayGrid();
            InitUI();
        }
        
        private void OnDestroy()
        {
            // don't forget to dispose our graph!
            HexGrid.DisposeGraph();
        }

        /// <summary>
        /// Generates a new random grid
        /// </summary>
        private HexGrid GenerateGrid()
        {
            // This derives the lower left and upper right coordinates of the grid
            // according to the camera's viewport
            Vector3Int minCell = GridExampleUtil.GetMinCell(terrainMap);
            Vector3Int maxCell = GridExampleUtil.GetMaxCell(terrainMap);

            // Some randomization values
            float perlinScale = .5f;
            float2 perlinOffset = new float2(Random.Range(0,100), Random.Range(0,100));
            
            // One way to construct the hex grid is by defining a list of cells that become "walls"
            List<HexGridCell> cells = new List<HexGridCell>();
            
            // Loop through the entire grid's boundary
            for (int x = minCell.x; x < maxCell.x; x++)
            {
                for (int y = minCell.y; y < maxCell.y; y++)
                {
                    float p = Mathf.PerlinNoise(
                        perlinOffset.x - minCell.x + x * perlinScale, 
                        perlinOffset.y - minCell.y + y * perlinScale);

                    // When the random value is below a threshold, we put a wall on that cell
                    if (p < .45f)
                    {
                        // We can define a wall on the hexgrid by setting a cost of infinity
                        cells.Add(new HexGridCell(new int2(x,y), float.PositiveInfinity));
                    }
                }
            }
            
            // Construct the hexgrid, we use the persistent allocator because we keep the grid around.
            return new HexGrid(minCell.ToInt2(), maxCell.ToInt2(),  cells, Allocator.Persistent);
        }
        
        private void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            
            Vector3Int tryMouseCell = GridExampleUtil.GetMouseCell(terrainMap);
            Vector3Int mouseCell = HexGrid.IsOpen(tryMouseCell.ToInt2()) 
                ? tryMouseCell : prevCell;

            // set Z at one so that this appears above path tiles
            mouseCell.z = 1;
            
            // display on change
            if (mouseCell != prevCell)
            {
                GridExampleUtil.SetTile(terrainMap, prevCell, terrainTile, Color.clear);
                prevCell = mouseCell;
                GridExampleUtil.SetTile(terrainMap, mouseCell, terrainTile, hoverColor);
            }

            if (Input.GetMouseButtonDown(0) && HexGrid.IsOpen(mouseCell.ToInt2())) // double checked for when prevCell wasn't initialized
            {
                CreateGoal(mouseCell.ToInt2());
            }
        }

        /// <summary>
        /// Returns a random cell location in the grid that is open/walkable
        /// </summary>
        public int2 GetRandomOpenCell()
        {
            // just try a 100 random cells and return the first one that is open (has a cost < infinity)
            for (int i = 0; i < 100; i++)
            {
                int x = Random.Range(HexGrid.min.x, HexGrid.max.x);
                int y = Random.Range(HexGrid.min.y, HexGrid.max.y);
                if (HexGrid.IsOpen(new int2(x, y)))
                {
                    return new int2(x, y);
                }
            }

            return int2.zero;
        }
        
        /// <summary>
        /// Adjusts the amount of snakes in our little world
        /// </summary>
        private void SetSnakeCount(int count)
        {
            while (snakes.Count < count)
                snakes.Add(CreateSnake());

            while (snakes.Count > count)
            {
                var seeker = snakes[snakes.Count - 1];
                snakes.RemoveAt(snakes.Count - 1);
                Destroy(seeker.gameObject);
            }
        }
        
        /// <summary>
        /// Instantiates a new snake and returns it
        /// </summary>
        private Snake CreateSnake()
        {
            var randomCell = GetRandomOpenCell();
            var seeker = Instantiate(seekerPrefab).GetComponent<Snake>();
            var seekerTail = seeker.GetComponent<SnakeTail>();
            seekerTail.Z = -snakes.Count;
            seeker.MoveTo(randomCell);
            return seeker;
        }

        /// <summary>
        /// Instantiates a new goal
        /// </summary>
        private void CreateGoal(int2 location)
        {
            var goal = Instantiate(goalPrefab).GetComponent<HexGridGoal>();
            goal.Initialize(location);
        }

        /// <summary>
        /// Utility to convert a transform's position into a hexgrid cell
        /// </summary>
        public static HexGridCell TransformToNode(Transform transform)
        {
            Vector3Int position = TileMap.WorldToCell(transform.position);
            return new HexGridCell(position);
        }
        
        /// <summary>
        /// Updates the tilemap to display our current grid
        /// </summary>
        void DisplayGrid()
        {
            terrainMap.ClearAllTiles();
            foreach (var cell in HexGrid.GetSetCells(Allocator.Temp))
            {
                GridExampleUtil.SetTile(
                    terrainMap, 
                    cell.Position.ToVector3Int(), 
                    terrainTile, 
                    wallColor);
            }
        }
        
        void InitUI()
        {
            // Create our menu
            settingsUI.AddDropdown(new List<string>() {"Priority", "Cheapest", "Any" }, OnDropdownChanged);
            settingsUI.AddButton("Randomize Grid", OnRandomizeButtonClicked);
            settingsUI.AddSlider("Seekers", 1, 50, snakes.Count, true, OnSeekerCountSliderChanged);
        }

        private void OnDropdownChanged(int value)
        {
            GoalFindMethod = (GoalFindMethod) value;
            Debug.Log("Find method: " + GoalFindMethod);
        }

        private void OnSeekerCountSliderChanged(float value)
        {
            SetSnakeCount(Mathf.FloorToInt(value));
        }
        
        /// <summary>
        /// Generates a new grid, disposes of the old one
        /// </summary>
        private void OnRandomizeButtonClicked()
        {
            // increment our grid version, this will invalidate paths set on the snakes
            GridVersion++;
            
            // dispose our old graph. using this extension method ensures that the actual disposal is done after
            // any possible pending queries are finished.
            HexGrid.DisposeGraph();
            
            // Generate a new grid and display it
            HexGrid = GenerateGrid();
            DisplayGrid();
            
            Randomized?.Invoke();
        }
    }
}