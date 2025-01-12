using AnyPath.Graphs.SquareGrid;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Native;
using AnyPath.Native.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace AnyPath.Examples
{
    
    /*
     * Note we need to define our finder classes concrete. You can use the AnyPath code generator to do this for you
     */
    
    class SquareGridDijkstraFinder : DijkstraFinder<SquareGrid, SquareGridCell, NoEdgeMod<SquareGridCell>> {  }

    /// <summary>
    /// This example demonstrates the use of the built in Int2Grid and how a processor can be used to alter the costs of cells.
    /// It also demonstrates adding multiple stops to a request as wel as how ALT heuristics can be used.
    /// </summary>
    public class DijkstraExample : MonoBehaviour
    {
        [SerializeField] private SettingsUI settingsUI;

        [SerializeField] private Color pathColor = new Color(0, 0, 1, .75f);
        [SerializeField] private Color reachColor = new Color(0, 0, 1, .75f);

        [SerializeField] private Color mouseColor = new Color(1f, 0, 1, .75f);
        [SerializeField] private Color wallColor = new Color(0, 0, 0, 1);

        public TileBase terrainTile;
        public TileBase pathTile;
        public Tilemap tileMap;
        public Tilemap reachTilemap;
        public Tilemap pathTilemap;
     
        private SquareGrid grid;
        private Vector3Int prevMouseCell;
        private Vector3Int startingCell;

        // Note the usage of Reuse result. This will allocate less or no memory on subsequent requests.
        private SquareGridDijkstraFinder dijkstraFinder = new SquareGridDijkstraFinder() { ReuseResult = true };

        private float dijkstraMaxCostBudget = 75f;
        private float perlinScale = .18f;

        private const float ExtraCostMul = 5;
        private const int reachZ = -2;
        private const int mouseZ = -3;
        
        void Start()
        {
            CreateGrid();
           
            InitUI();

            // display it
            DisplayGrid();

            startingCell = new Vector3Int((grid.min.x + grid.max.x) / 2, (grid.min.y + grid.max.y) / 2, 0);
            TryPerformDijkstra();
            
            Debug.Log("Click on an open cell to run Dijkstra from that location");
        }
        
        private void OnDestroy()
        {
            // Dont forget to dispose our grid
            grid.DisposeGraph();
        }


        private void Update()
        {
            Vector3Int cell = GridExampleUtil.GetMouseCell(tileMap);
            
            bool mousePosChanged = cell != prevMouseCell;
            if (mousePosChanged)
            {
                // update mouse highlight tile
                GridExampleUtil.ClearTile(tileMap, new Vector3Int(prevMouseCell.x, prevMouseCell.y, mouseZ));
                GridExampleUtil.SetTile(tileMap, new Vector3Int(cell.x, cell.y, mouseZ), pathTile, mouseColor);
            }
            
            prevMouseCell = cell;
            
            // detect placing positions or avoid marker
            if (!ExampleUtil.PointerOnUI())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    startingCell = cell;
                    TryPerformDijkstra();
                }
                else if (dijkstraFinder.Result != null)
                {
                    // Try to display a path that is contained in the dijkstra result
                    TryDisplayPath(cell);
                }
            }
        }
        
        /*
         * Grid construction
         */
        
        void CreateGrid()
        {
            // get our current screen / world bounds
            int2 lower = ExampleUtil.GetMinWorldPos.RoundToInt2();
            int2 upper = ExampleUtil.GetMaxWorldPos.RoundToInt2();
            
            float2 perlinOffset = new float2(Random.Range(0, 100), Random.Range(0, 100));
            
            // fill the grid randomly
            this.grid = new SquareGrid(lower, upper, SquareGridType.FourNeighbours, 32, Allocator.Persistent);
            for (int x = lower.x; x <= upper.x; x++)
            {
                for (int y = lower.y; y <= upper.y; y++)
                {
                    float cost01 = Mathf.PerlinNoise(
                        perlinOffset.x - lower.x + x * perlinScale, 
                        perlinOffset.y - lower.y + y * perlinScale);
                    
                    // Make a wall, or create a little 'height' by adding some cost to our cell
                    // this makes the result of dijkstra a lot more interesting
                    grid.SetCell(new int2(x, y), cost01 > .66f ? float.PositiveInfinity : cost01 * ExtraCostMul);
                }
            }

            Debug.Log($"Grid size: {(upper - lower).ToString()}");
        }

        /// <summary>
        /// Displays the grid via the Tilemaps
        /// </summary>
        void DisplayGrid()
        {
            tileMap.ClearAllTiles();
            pathTilemap.ClearAllTiles();
            reachTilemap.ClearAllTiles();
            foreach (var cell in grid.GetSetCells(Allocator.Temp))
            {
                if(float.IsPositiveInfinity(cell.EnterCost))
                    GridExampleUtil.SetTile(tileMap, cell.ToVector3Int(), terrainTile, wallColor);
                else
                    GridExampleUtil.SetTile(tileMap, cell.ToVector3Int(), terrainTile, new Color(wallColor.r, wallColor.g, wallColor.b, cell.EnterCost / ExtraCostMul));
            }
        }

        void TryDisplayPath(Vector3Int cell)
        {
            var destCell = new SquareGridCell(cell);
            
            // See if the hovered cell is within the dijkstra result
            if (dijkstraFinder.Result.HasPath(destCell))
            {
                // Obtain the path form the result.
                pathTilemap.ClearAllTiles();
                
                foreach (var pathCell in dijkstraFinder.Result.GetPath(destCell, true))
                    GridExampleUtil.SetTile(pathTilemap, pathCell.ToVector3Int(), pathTile, pathColor);
                
                // Note, this example is not efficient since it re-creates the path each time!
                // You just store the result from GetPath if you're going to need it more than once.
                
                // Since version 1.4 there is now also an overload that accepts a Path<> container to be re-used
                // this will allow you to obtain paths without creating extra memory allocations.
            }
        }

        private void TryPerformDijkstra()
        {
            var cell = new SquareGridCell(startingCell);
            if (!grid.IsOpen(cell))
            {
                // Dont run when we've clicked on a blocked cell
                return;
            }
            
            // Clear before re-use
            dijkstraFinder.Clear();
            
            dijkstraFinder.Graph = this.grid;
            
            // Set our max cost budget. We will only find paths that are within this range. Limiting this also helps with performance.
            dijkstraFinder.MaxCost = dijkstraMaxCostBudget;
            
            // Our starting location. We can just convert it into a SquareGridCell as only the position is used for queries.
            dijkstraFinder.Start = new SquareGridCell(startingCell);
            
            // Just run our request on the main thread. You could also use Schedule() to run it on another thread and wait for the result.
            // The HexGrid example demonstrates this.
            dijkstraFinder.Run();

            // Display all of our paths
            reachTilemap.ClearAllTiles();
            foreach (var destinationCell in dijkstraFinder.Result.Goals)
            {
                GridExampleUtil.SetTile(reachTilemap, destinationCell.ToVector3Int(reachZ), pathTile, reachColor);
            }
        }
        
        /*
        * UI
        */
        
        void InitUI()
        {
            settingsUI.AddButton("Reset", OnReset);
            settingsUI.AddSlider("Max Cost", 1f, 300f, dijkstraMaxCostBudget, false, OnMaxCostChanged);
        }
        
        private void OnReset()
        {
            // erase all walls
            tileMap.ClearAllTiles();
                
            // dispose of our old grid
            grid.DisposeGraph();
         
            // create & display new grid
            CreateGrid();
            DisplayGrid();
        }

        private void OnMaxCostChanged(float value)
        {
            this.dijkstraMaxCostBudget = value;
            TryPerformDijkstra();
        }
    }
}