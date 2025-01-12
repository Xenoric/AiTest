namespace AnyPath.Managed
{
    /// <summary>
    /// Can be used in conjunction with a IOptionValidator to reserve options
    /// </summary>
    /// <typeparam name="TOption">The type of target</typeparam>
    public interface IOptionReserver<in TOption>
    {
        /// <summary>
        /// Gets called for the eligable option that is part of the result.
        /// </summary>
        /// <param name="option">The option that is part of the result</param>
        /// <remarks>
        /// You can use this callback to make sure this target doesn't get validated by other target validators.
        /// </remarks>
        void Reserve(TOption option);
    }
}