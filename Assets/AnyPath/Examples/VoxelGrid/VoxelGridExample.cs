using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnyPath.Graphs.VoxelGrid;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Managed.Results;
using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AnyPath.Examples
{
    class VoxelGridPathFinder : PathFinder<VoxelGrid, VoxelGridCell, VoxelGridHeuristicProvider, NoEdgeMod<VoxelGridCell>, NoProcessing<VoxelGridCell>, VoxelGridCell>, IPathFinder<VoxelGrid, VoxelGridCell, Path<VoxelGridCell>>  { }
    class VoxelGridPathFinder_DirectionMod : PathFinder<VoxelGrid, VoxelGridCell, VoxelGridHeuristicProvider, VoxelGridDirectionMod, NoProcessing<VoxelGridCell>, VoxelGridCell>, IPathFinder<VoxelGrid, VoxelGridCell, Path<VoxelGridCell>>   { }

    public class VoxelGridExample : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject terrainCubePrefab;
        [SerializeField] private GameObject pathCubePrefab;
        
        [Header("Refs")]
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private GameObject startCube;
        [SerializeField] private GameObject endCube;
        [SerializeField] private Transform gridRoot;
        [SerializeField] private Transform pathRoot;
        [SerializeField] private Transform gridLookAt;
        
        private VoxelGrid grid;
        private Dictionary<int2, int> heightMap = new Dictionary<int2, int>();
        private static readonly int3 GridSize = new int3(50, 50, 50);
        private const float perlinScale = .12f;
        private const float heightScale = 6;

        private ExampleMode exampleMode;
        private Vector2 perlinOffset;

        public enum ExampleMode
        {
            Gravity_StairsUpAndDown,
            Gravity_AllDirections,
            NoGravity_SixDirections,
            NoGravity_AllDirections,
        }

        private List<GameObject> gridCubes = new List<GameObject>();
        private List<GameObject> pathCubes = new List<GameObject>();

        private void Start()
        {
            CreateAndDisplayGrid();
            InitUI();
            
            Debug.Log("Watch random paths being generated, or manually drag the start or end cube in the scene view.");
        }
        
        private void OnDestroy()
        {
            grid.DisposeGraph();
        }

        void CreateAndDisplayGrid()
        {
            if (grid.IsCreated)
                grid.DisposeGraph();

            // This code is a bit ugly but for demonstration purposes only.
            // In a real world application all of this could be done in a way more efficient manner.
            
            VoxelGrid.DirCost[] directionsArray;
            switch (exampleMode)
            {
                default:
                case ExampleMode.NoGravity_SixDirections:
                    directionsArray = VoxelGrid.Foward_Right_Back_Left_Up_Down;
                    break;
                case ExampleMode.Gravity_AllDirections:
                case ExampleMode.NoGravity_AllDirections:
                    directionsArray = VoxelGrid.All26;
                    break;
                case ExampleMode.Gravity_StairsUpAndDown:
                    directionsArray = VoxelGrid.Foward_Right_Back_Left_Down_StairsUp_StairsDown;
                    break;
            }
            
            heightMap.Clear(); // not used for actual pathfinding but just for snapping our start and end cubes
           
            grid = new VoxelGrid(int3.zero, GridSize, directionsArray, Allocator.Persistent,
                // For all unset cells, only allow downwards movement (Gravity)
                // We can totally ignore these flags depending on the type of pathfinder we use.
                // So we can allow for agents that can only navigate on the ground
                // or agents that can fly though the world.
                defaultFlags: (int)VoxelGridDirectionFlags.Down);
            
            
            // Look at the center of our grid
            gridLookAt.position = (grid.min + grid.max) * new float3(.5f, 0, .5f);
            
            for (int x = grid.min.x; x <= grid.max.x; x++)
            {
                for (int z = grid.min.z; z <= grid.max.z; z++)
                {
                    int height = Mathf.FloorToInt(heightScale * Mathf.PerlinNoise(
                        perlinOffset.x + (float)x * perlinScale, perlinOffset.x + (float)z * perlinScale));
                    heightMap[new int2(x, z)] = height;
                    
                    for (int y = grid.min.y; y <= math.min(grid.max.y, grid.min.y + height); y++)
                    {
                        // Add our terrain
                        grid.SetCell(new int3(x,y,z), float.PositiveInfinity);
                    }

                    if (exampleMode == ExampleMode.Gravity_AllDirections || 
                        exampleMode == ExampleMode.Gravity_StairsUpAndDown)
                    {
                        // The following extra cells are for demonstrating how you could approach gravity in voxel pathfinding.
                        // If you just want to "fly" through the world, then these aren't neccessary.
                        // On top of our terrain, we add articial "ground" cells which allow movement in all directions
                        // All unset sells are set to only support Down direction.
                        // Our supported movement directions of the grid (set above) allow us to do one diagonal stairs step up!
                        grid.SetCell(new int3(x, grid.min.y + height + 1, z), 0, (int)VoxelGridDirectionFlags.All);
                    }
                }
            }
            
            foreach (var cube in gridCubes)
                Destroy(cube);
            gridCubes.Clear();

            foreach (var cell in grid.GetSetCells(Allocator.Temp))
            {
                // Only display the terrain (infinite cost voxels)
                if (!cell.IsOpen)
                {
                    var cube = Instantiate(terrainCubePrefab, gridRoot);
                    cube.transform.position = cell.ToVector3();
                    gridCubes.Add(cube);
                }
            }

            forceChange = true;
        }
        
        /*
         * Pathfinding
         */

        private int3 startPosition;
        private int3 endPosition;
        private bool forceChange;
        
        private void LateUpdate()
        {
            int3 newStartPosition = (int3)math.round(startCube.transform.position);
            int3 newEndPosition = (int3)math.round(endCube.transform.position);

            // Snap cubes to terrain:
            if (heightMap.TryGetValue(newStartPosition.xz, out int y))
            {
                newStartPosition.y = y + 1;
                startCube.transform.position = (float3)newStartPosition;
            }
                
            if (heightMap.TryGetValue(newEndPosition.xz, out y))
            {
                newEndPosition.y = y + 1;
                endCube.transform.position = (float3)newEndPosition;
            }
            
            // Clamp our cubes to the boundary of the grid
            newStartPosition = math.clamp(newStartPosition, grid.min, grid.max);
            newEndPosition = math.clamp(newEndPosition, grid.min, grid.max);
            
            // Don't do anything if nothing has changed
            if (!forceChange && startPosition.Equals(newStartPosition) && endPosition.Equals(newEndPosition))
                return;

            forceChange = false;
            startPosition = newStartPosition;
            endPosition = newEndPosition;

            // Select suitable pathfinder
            IPathFinder<VoxelGrid, VoxelGridCell, Path<VoxelGridCell>> finder;
            switch (exampleMode)
            {
                default:
                case ExampleMode.NoGravity_SixDirections:
                case ExampleMode.NoGravity_AllDirections:
                    finder = new VoxelGridPathFinder();
                    break;
                case ExampleMode.Gravity_AllDirections:
                case ExampleMode.Gravity_StairsUpAndDown:
                    // For our gravity simulation, we use the VoxelGridDirectionMod
                    // See the grid generation for an explanation.
                    finder = new VoxelGridPathFinder_DirectionMod();
                    break;
            }

            finder.Graph = grid;
            finder.Stops.SetStartAndGoal(new VoxelGridCell(startPosition, flags: -1), new VoxelGridCell(endPosition, flags: -1));
            finder.Run();
            
            // Clear old path cubes
            foreach(var cube in pathCubes)
                Destroy(cube);
            pathCubes.Clear();
            if (finder.Result.HasPath)
            {
                for (int i = 0; i < finder.Result.Length - 1; i++) // -1 because the last cell is the end location which already has a cube
                {
                    var cell = finder.Result[i];
                    var cube = Instantiate(pathCubePrefab, pathRoot);
                    cube.transform.position = cell.ToVector3();
                    pathCubes.Add(cube);
                }
            }
            else
            {
                Debug.Log("No path!");
            }
        }

        /*
         * UI
         */

        void InitUI()
        {
            settingsUI.AddToggle("Animate Paths", true, OnAnimateChanged);
            settingsUI.AddDropdown(Enum.GetNames(typeof(ExampleMode)).ToList(), OnGravitySimulationModeChanged, (int)exampleMode);
            settingsUI.AddButton("Randomize Grid", OnRandomize);
            StartCoroutine(Animate());
        }

        private void OnRandomize()
        {
            perlinOffset = new Vector2(Random.Range(0, 100), Random.Range(0, 100));
            CreateAndDisplayGrid();
        }

        private void OnAnimateChanged(bool animate)
        {
            StopAllCoroutines();
            if (animate)
                StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            while (true)
            {
                // Just pick a random position for the start and end
                // LateUpdate will stick them to the terrain.
                startCube.transform.position = new Vector3(
                    Random.Range(grid.min.x, grid.max.x),
                    Random.Range(grid.min.y, grid.max.y), 
                    Random.Range(grid.min.z, grid.max.z));
                
                endCube.transform.position = new Vector3(
                    Random.Range(grid.min.x, grid.max.x),
                    Random.Range(grid.min.y, grid.max.y), 
                    Random.Range(grid.min.z, grid.max.z));
                
                yield return new WaitForSeconds(1);
            }
        }

        private void OnGravitySimulationModeChanged(int value)
        {
            exampleMode = (ExampleMode)value;
            CreateAndDisplayGrid();
            forceChange = true;
        }
    }
}