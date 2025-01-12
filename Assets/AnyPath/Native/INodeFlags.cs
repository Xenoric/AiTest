namespace AnyPath.Native
{
    /// <summary>
    /// Specifies that a node can be used by the FlagBitmask modifier
    /// </summary>
    public interface INodeFlags
    {
        int Flags { get; }
    }
}