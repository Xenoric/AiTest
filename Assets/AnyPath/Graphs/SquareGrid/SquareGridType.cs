namespace AnyPath.Graphs.SquareGrid
{
    /// <summary>
    /// Specifies how many neighbours every location has.
    /// Four neighbours only allows for straight movement.
    /// Eight neighbours includes diagonal movement.
    /// </summary>
    public enum SquareGridType
    {
        FourNeighbours = 4,
        EightNeighbours = 8
    }
}