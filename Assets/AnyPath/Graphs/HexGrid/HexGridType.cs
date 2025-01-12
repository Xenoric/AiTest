namespace AnyPath.Graphs.HexGrid
{
    /// <summary>
    /// Different cell layouts for the <see cref="HexGrid"/>
    /// </summary>
    public enum HexGridType
    {
        OddR = 0,
        PointyTop = 0,
        
        /// <summary>
        /// Equal to OddR and PointyTop
        /// Unity tilemap uses this configuration
        /// </summary>
        UnityTilemap = 0,
        
        EvenR = 12,
        

        OddQ = 24,
        /// <summary>
        /// Equal top OddQ. Note that Unity's flat top is just a rotated PointyTop
        /// (xy is swizzled to yx). So for that configuration you should still use UnityTilemap
        /// </summary>
        FlatTop = 24,
        
        EvenQ = 36
    }
}