using AnyPath.Managed;

namespace AnyPath.Examples
{
    // This class is used to ensure every seeker has a unique goal it will move to.
    public class HexGridGoalValidator : IOptionValidator<HexGridGoal>, IOptionReserver<HexGridGoal>
    {
        public static HexGridGoalValidator Instance { get; } = new HexGridGoalValidator();
        
        // A seeker may only consider goals that aren't already seeked
        // This function is called before any attempt at finding a path to this target is made. When a path is found
        // this is called again to ensure no other seeker reserved the goal in the time it took to run the algorithm on another thread.
        // If the target was reserved in that time, the request is restarted and other options will be considered.
        public bool Validate(HexGridGoal option)
        {
            return !option.IsSeeked;
        }

        // Reserves a goal so that no other seekers may target it
        public void Reserve(HexGridGoal option)
        {
            option.IsSeeked = true;
        }
    }
}