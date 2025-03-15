using UnityEngine;

namespace PathBerserker2d
{
    public interface IVelocityProvider
    {
        /// <summary>
        /// Current velocity relative to the world.
        /// </summary>
        Vector2 WorldVelocity { get; }
    }
}
