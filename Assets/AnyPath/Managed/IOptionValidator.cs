namespace AnyPath.Managed
{
    /// <summary>
    /// Can be used in conjunction with an OptionFinder (First and Cheapest) to validate and/or reserve options.
    /// </summary>
    /// <typeparam name="TOption">The type of target</typeparam>
    public interface IOptionValidator<in TOption>
    {
        /// <summary>
        /// Return wether this target is eligable for a result. This is called before the request is made and once again
        /// after a path associated with the option object has been found. The second call is to account for the fact that the option's
        /// state may have changed in the time it took to perform the request on another thread. If the second call returns falls, the request
        /// is retried and other options are considered.
        /// </summary>
        /// <param name="option">The option to validate</param>
        /// <returns>True if the option is eligable</returns>
        bool Validate(TOption option);
    }
}