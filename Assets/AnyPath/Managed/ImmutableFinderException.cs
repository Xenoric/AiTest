using System;

namespace AnyPath.Managed
{
    /// <summary>
    /// Occurs when an attempt is being made to modify the data on a Finder when a request is in flight.
    /// </summary>
    public class ImmutableFinderException : Exception
    {
        public override string Message { get; } = "Finder cannot be modified as long as it is not completed and cleared";
    }
}