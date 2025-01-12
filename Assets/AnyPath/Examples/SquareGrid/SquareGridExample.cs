using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AnyPath.Graphs.SquareGrid;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Native;
using AnyPath.Native.Heuristics;
using AnyPath.Native.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace AnyPath.Examples
{
    
    /*
     * Note we need to define our finder classes concrete. You can use the AnyPath code generator to do this for you
     */
    
    class SquareGridDebugFinderALT : DebugFinder<SquareGrid, SquareGridCell, ALT<SquareGridCell>, NoEdgeMod<SquareGridCell>, NoProcessing<SquareGridCell>, SquareGridCell> { }
    class SquareGridDebugFinder : DebugFinder<SquareGrid, SquareGridCell, SquareGridHeuristicProvider, NoEdgeMod<SquareGridCell>, NoProcessing<SquareGridCell>, SquareGridCell> { }
    class SquareGridMultiPathFinder : MultiPathFinder<SquareGrid, SquareGridCell, SquareGridHeuristicProvider, AvoidModifier, NoProcessing<SquareGridCell>, SquareGridCell> { }

    /// <summary>
    /// This example demonstrates the use of the built in SquarGrid and how a processor can be used to alter the costs of cells.
    /// It also demonstrates adding multiple stops to a request as well as how ALT heuristics can be used.
    /// </summary>
    public class SquareGridExample : MonoBehaviour
    {
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private GameObject avoidMarker;
        [SerializeField] private Color pathColor = new Color(0, 0, 1, .75f);
        [SerializeField] private Color mouseColor = new Color(1f, 0, 1, .75f);
        [SerializeField] private Color wallColor = new Color(0, 0, 0, 1);
        [SerializeField] private Color landmarkColor = new Color(0, 1, 0, 1);
        [SerializeField] private Color regularExpansionColor;
        [SerializeField] private Color altExpansionColor;


        [SerializeField] private bool displayExpansion;

        private const int MaxBatchSize = 100; // max amount of paths we find

        public TileBase terrainTile;
        public TileBase pathTile;
        public Tilemap tileMap;
        public Tilemap pathTilemap;
        public int randomStartPositionCount = 25;

        private SquareGridType neighbourMode = SquareGridType.FourNeighbours;
        private SquareGrid grid;
        private ALT<SquareGridCell> alt;
        private Vector3Int prevMouseCell;
        private bool shouldCompose;
        private List<SquareGridCell> positions = new List<SquareGridCell>();
        
        // We're directly accessing the result from our multi path finder, so we can re-use the same result instance
        // for every query. this effectively reduces memory allocations to zero.
        private SquareGridMultiPathFinder multiPathFinder = new SquareGridMultiPathFinder() { ReuseResult = true };

        private float gridDensity = .33f;
        private float gridScale = .5f;
        private Vector3Int avoidCell;
        private float AvoidSeverity => avoidMarker.transform.localScale.x * avoidMarker.transform.localScale.x;
        
        // Z offset for position and mouse tiles so we can easily remove them
        private const int positionZ = -1;
        private const int mouseZ = -2;
        
        void Start()
        {
            CreateGrid();
            if (randomStartPositionCount > 0)
                CreateRandomStartPositions();
            
            PlaceAvoidMarker(((grid.min + grid.max) / 2).ToVector3Int());

            InitUI();

            // display it
            DisplayGrid();
        }
        
        private void OnDestroy()
        {
            // Dont forget to dispose our ALT heuristics and grid
            alt.Dispose();
            grid.DisposeGraph();
        }


        private void Update()
        {
            Vector3Int cell = GridExampleUtil.GetMouseCell(tileMap);
            
            bool mousePosChanged = cell != prevMouseCell;
            bool positionAddedOrMarkerChanged = false;
            
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
                    AddPosition(cell);
                    positionAddedOrMarkerChanged = true;
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    PlaceAvoidMarker(cell);
                    positionAddedOrMarkerChanged = true;
                }
                else if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
                {
                    OnScaleMarker(Input.mouseScrollDelta.y * Time.deltaTime);
                    positionAddedOrMarkerChanged = true;
                }
            }
            
            if (shouldCompose)
            {
                // only re-eval composed path if user placed a new position or replaced the marker
                if (!positionAddedOrMarkerChanged)
                    return;
                
                if (!FindComposedPath())
                {
                    // a composed path couldn't be found, clear set positions
                    Debug.Log("No path available that visits all of the positions. Try again!");
                    ClearPositions();
                }
            }
            // multi path finding on every mouse change
            else if (mousePosChanged)
                FindPathsMulti(cell);
        }

        /// <summary>
        /// Finds a path from all currently set locations to the mouse position.
        /// </summary>
        void FindPathsMulti(Vector3Int cell)
        {
            // bail on no positions or when it's out of bounds
            if (positions.Count == 0)
                return;
            if (!grid.InBounds(cell.ToInt2()) || float.IsInfinity(grid.GetCost(cell.ToInt2())))
                return;

            // Clear our multi finder for the next request
            multiPathFinder.Clear();
            
            // Add all clicked positions to our request
            foreach (var position in positions)
                multiPathFinder.AddRequest(new SquareGridCell(position), new SquareGridCell(cell));

            var prov = multiPathFinder.HeuristicProvider;
            multiPathFinder.HeuristicProvider = prov;
            multiPathFinder
                .SetGraph(grid) // set graph to perform the pathfinding query on
                // add our modifier, with a cost cap of grid width/4
                .SetEdgeMod(new AvoidModifier(avoidCell.ToInt2(), AvoidSeverity, (grid.max.x - grid.min.x) / 4f))
                .Run(); // run it on the main thread


            // display our paths
            pathTilemap.ClearAllTiles();

            foreach (var path in multiPathFinder.Result) // a multi path finder has an array containing all of our sepearate paths
            {
                if (!path.HasPath)
                    continue;
    
                foreach (var pathCell in path)
                    GridExampleUtil.SetTile(pathTilemap, pathCell.ToVector3Int(), pathTile, pathColor);
            }
        }

        /// <summary>
        /// Constructs a path that visits all set locations in order, does 2 requests, one with regular heuristics and one with the ALT
        /// heuristics.
        /// </summary>
        bool FindComposedPath()
        {
            // must have at a minimum 2
            if (positions.Count <= 1)
                return true;

            // We use 2 debug finders to demonstrate the difference between using a regular heuristic and ALT heuristics.
            // the debugfinder allows us to see all of the nodes A* expanded into. We want to keep this to a minimum for best performance.
            
            var altPathFinder = new SquareGridDebugFinderALT();  
            var pathFinder = new SquareGridDebugFinder(); 

            // create our query with regular heurstics
            pathFinder
                .SetGraph(grid)
                .SetHeuristicProvider(new SquareGridHeuristicProvider(grid.neighbourMode))
                .AddStops(positions);
                
            var stopwatch = Stopwatch.StartNew();
            pathFinder.Run();
            stopwatch.Stop();
            double regularTime = stopwatch.Elapsed.TotalMilliseconds;

            // same path but using ALT heuristics
            altPathFinder
                .SetGraph(grid)
                .SetHeuristicProvider(alt)
                .AddStops(positions);
                
            stopwatch.Restart();
            altPathFinder.Run();
            stopwatch.Stop();
            double altTime = stopwatch.Elapsed.TotalMilliseconds;
            
            // composed path fails if it can't visit all stops in order
            if (!pathFinder.Result.HasPath)
            {
                Debug.Log("No path!");
                return false;
            }
            
            // Log our times
            Debug.Log($"Regular heuristic time: {regularTime}ms, " +
                      $"ALT heurstic time: {altTime}ms");

            // NOTE:
            // the AllExpanded array only gives us the expansion count from the last 2 stops
            Debug.Log($"Regular heurstic expansion count: {pathFinder.Result.AllExpanded.Length}, " +
                      $"ALT heuristic expansion count: {altPathFinder.Result.AllExpanded.Length}");
            
            pathTilemap.ClearAllTiles();

            if (displayExpansion)
            {
                // Display expansion of regular and ALT
                foreach (var edge in pathFinder.Result.AllExpanded)
                    GridExampleUtil.SetTile(pathTilemap, edge.ToVector3Int(), pathTile, regularExpansionColor);
                foreach (var edge in altPathFinder.Result.AllExpanded)
                    GridExampleUtil.SetTile(pathTilemap, edge.ToVector3Int(1), pathTile, altExpansionColor);
            }

 
            // display our path
            foreach (var edge in pathFinder.Result)
                GridExampleUtil.SetTile(pathTilemap, edge.ToVector3Int(2), pathTile, pathColor);
            
            // validate, both regular and ALT should have the same path length
            Debug.Assert(pathFinder.Result.Length == altPathFinder.Result.Length, $"Length: {pathFinder.Result.Length} / {altPathFinder.Result.Length}");

            return true;
        }

        void CreateRandomStartPositions()
        {
            // use landmark selection process to pick some suitable evenly spaced starting positions
            ClearPositions();
            SquareGridCell[] randomCells = new SquareGridCell[randomStartPositionCount];
            
            // Note that we must explicity tell the types for the landmark selection job.
            // This is due to a limitation by the burst compiler. For all included graph types this is fairly straightforward.
            // Provide the type of graph, node and the enumerator type, which is usually GRAPH.Enumerator
            // For more information see: https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-generic-jobs.html
            LandmarkSelection<SquareGrid, SquareGridCell, SquareGrid.Enumerator>.SelectFarthestLandmarksUndirected(ref grid, grid.GetEnumerator(), randomCells);
            
            foreach (var cell in randomCells)
                AddPosition(cell.ToVector3Int());
        }
        
        /*
         * Grid construction
         */
        
        
        void CreateGrid()
        {
            // get our current screen / world bounds
            int2 lower = ExampleUtil.GetMinWorldPos.RoundToInt2();
            int2 upper = ExampleUtil.GetMaxWorldPos.RoundToInt2();

            float perlinScale = gridScale;
            float2 perlinOffset = new float2(Random.Range(0, 100), Random.Range(0, 100));
            
            // fill the grid randomly
            this.grid = new SquareGrid(lower, upper, neighbourMode, 32, Allocator.Persistent);
            for (int x = lower.x; x <= upper.x; x++)
            {
                for (int y = lower.y; y <= upper.y; y++)
                {
                    float cost01 = Mathf.PerlinNoise(
                        perlinOffset.x - lower.x + x * perlinScale, 
                        perlinOffset.y - lower.y + y * perlinScale);
                    
                    grid.SetCell(new int2(x, y), cost01 < gridDensity ? float.PositiveInfinity : 0);
                }
            }

            Debug.Log($"Grid size: {(upper - lower).ToString()}");

            /*
             * Landmark selection and ALT heuristics
             * note that using ALT heuristic can take quite some time to initialize and is in no way neccessary for pathfinding.
             * It is merely here for demonstration purposes.
             * If you're not sure if you should use it, you probably shouldn't. It may only be benificial on very large graphs with
             * complicated layouts.
             */
            
            var stopwatch = Stopwatch.StartNew();
            
            // Create a container for our landmarks
            // The max amount of supported landmarks is currently 31
            SquareGridCell[] landmarks = new SquareGridCell[31];
            
            // Note that we must explicity tell the types for the landmark selection job.
            // This is due to a limitation by the burst compiler. For all included graph types this is fairly straightforward.
            // Provide the type of graph, node and the enumerator type, which is usually GRAPH.Enumerator
            // For more information see: https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-generic-jobs.html
            LandmarkSelection<SquareGrid, SquareGridCell, SquareGrid.Enumerator>.SelectFarthestLandmarksUndirected(ref grid, grid.GetEnumerator(), landmarks);

            // Allocate our ALT heuristic provider
            alt = new ALT<SquareGridCell>(Allocator.Persistent);
            
            // compute using the landmarks we've selected
            // again, we must explicity give the types, as explained here: https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-generic-jobs.html
            ALTCompute<SquareGrid, SquareGridCell>.ComputeUndirected(alt, ref grid, landmarks);
            Debug.Log($"Landmarks computed in {stopwatch.ElapsedMilliseconds}ms");
            
            // Uncomment to test ALT serialization
            // WriteReadTest();
        }

        /// <summary>
        /// Shows how to serialize the ALT heuristics and read it back.
        /// </summary>
        void WriteReadTest()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                {
                    alt.WriteTo(writer, (w, node) =>
                    {
                        // We need to tell how to write the node
                        
                        w.Write(node.Position.x);
                        w.Write(node.Position.y);
                        w.Write(node.EnterCost);
                    });
                }
                
                Debug.Log($"ALT size: {(stream.Length / 1024)}KiB");
       
                stream.Seek(0, SeekOrigin.Begin);
                BinaryReader reader = new BinaryReader(stream);
                alt.ReadFrom(reader, (r) =>
                {
                    // And we need to tell how to read the nodes back
                    return new SquareGridCell(
                        new int2(
                            r.ReadInt32(), 
                            r.ReadInt32()), 
                            r.ReadSingle());
                });
            }
        }

        /// <summary>
        /// Displays the grid via the Tilemaps
        /// </summary>
        void DisplayGrid()
        {
            tileMap.ClearAllTiles();
            pathTilemap.ClearAllTiles();
            foreach (var cell in grid.GetSetCells(Allocator.Temp))
            {
                if(float.IsPositiveInfinity(cell.EnterCost))
                    GridExampleUtil.SetTile(tileMap, cell.ToVector3Int(), terrainTile, wallColor);
                else
                    GridExampleUtil.SetTile(tileMap, cell.ToVector3Int(), terrainTile, new Color(wallColor.r, wallColor.g, wallColor.b, cell.EnterCost));
            }
            
            // Place green tiles at the landmark locations
            for (int i = 0; i < alt.LandmarkCount; i++)
            {
                var landmark = alt.GetLandmarkLocation(i);
                GridExampleUtil.SetTile(tileMap, landmark.ToVector3Int(1), terrainTile, landmarkColor);
            }
        }

        void ClearPositions()
        {
            foreach (var position in positions)
                GridExampleUtil.ClearTile(tileMap, position.ToVector3Int(positionZ));
            positions.Clear();
        }

        private void PlaceAvoidMarker(Vector3Int cell)
        {
            avoidCell = cell;
            avoidMarker.transform.position = tileMap.CellToWorld(cell);
        }
        
        private void AddPosition(Vector3Int cell)
        {
            if (positions.Count >= MaxBatchSize)
                return;
            
            if (!grid.InBounds(cell.ToInt2()) || float.IsInfinity(grid.GetCost(cell.ToInt2())))
                return;
            
            positions.Add(new SquareGridCell(cell));
            GridExampleUtil.SetTile(tileMap, new Vector3Int(cell.x, cell.y, positionZ), pathTile, pathColor);
        }

        
        /*
         * UI
         */
        
        void InitUI()
        {
            settingsUI.AddButton("Reset", OnReset);
            settingsUI.AddToggle("Compose/ALT", shouldCompose, OnComposeChanged);
            settingsUI.AddSlider("Density", .01f, .95f, gridDensity, false, OnDensityChanged);
            settingsUI.AddSlider("Scale", .1f, 10f, gridScale, false, OnScaleChanged);
            settingsUI.AddDropdown(new List<string>() {"Four Neighbours", "Eight Neighbours"}, OnNeighbourModeChanged); 
        }
        
        private void OnReset()
        {
            // erase all plans
            positions.Clear();
                
            // erase all displayed paths
            tileMap.ClearAllTiles();
                
            // dispose of our old grid
            grid.DisposeGraph();
            alt.Dispose();
            
            // create & display new grid
            CreateGrid();
            DisplayGrid();
        }

        public void OnComposeChanged(bool value)
        {
            shouldCompose = value;
        }
        
        private void OnDensityChanged(float value)
        {
            this.gridDensity = value;
            OnReset();
        }
        
        private void OnScaleChanged(float value)
        {
            this.gridScale = value;
            OnReset();
        }

        private void OnScaleMarker(float delta)
        {
            float scale = avoidMarker.transform.localScale.x;
            scale = Mathf.Clamp(scale + delta * 4, .25f, 50f);
            avoidMarker.transform.localScale = new Vector3(scale, scale, scale);
        }
        
        private void OnNeighbourModeChanged(int mode)
        {
            // we can't directly map the dropdown index value to the enum value
            // because the enum values correspond to the amount of neighbours per cell (4 and 8)
            this.neighbourMode = mode == 0 ? SquareGridType.FourNeighbours : SquareGridType.EightNeighbours;
            OnReset();
        }
    }
}