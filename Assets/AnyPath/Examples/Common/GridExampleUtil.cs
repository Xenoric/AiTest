using UnityEngine;
using UnityEngine.Tilemaps;

namespace AnyPath.Examples
{
    public class GridExampleUtil
    {
        public static Vector3Int GetMaxCell(Tilemap tileMap) => tileMap.WorldToCell(ExampleUtil.GetMaxWorldPos);
        public static  Vector3Int GetMinCell(Tilemap tileMap) => tileMap.WorldToCell(ExampleUtil.GetMinWorldPos);
        public static  Vector3Int GetMouseCell(Tilemap tileMap) => tileMap.WorldToCell(ExampleUtil.GetMouseWorldPos());
        
        /// <summary>
        /// Gets the bounds of the area for a Z value in the tilemap
        /// Useful for clearing one 'layer' only
        /// </summary>
        public static BoundsInt GetAreaCellBounds(Tilemap tileMap, int z)
        {
            return new BoundsInt(
                GetMinCell(tileMap).x, 
                GetMinCell(tileMap).y, z, 
                GetMaxCell(tileMap).x - GetMinCell(tileMap).x, 
                GetMaxCell(tileMap).y - GetMinCell(tileMap).y, 1);
        }
        
        public static void SetTile(Tilemap tileMap, Vector3Int cell, TileBase tile, Color color)
        {
            tileMap.SetTile(cell, tile);
            tileMap.SetTileFlags(cell, TileFlags.None);
            tileMap.SetColor(cell, color);
        }

        public static void ClearTile(Tilemap tileMap, Vector3Int cell)
        {
            tileMap.SetTile(cell, null);
        }

        public static void ScaleTile(Tilemap tileMap, Vector3Int cell, Vector3 scale)
        {
            tileMap.SetTransformMatrix(cell, Matrix4x4.Scale(scale));
        }
    }
}