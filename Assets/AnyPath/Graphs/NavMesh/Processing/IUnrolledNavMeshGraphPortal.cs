using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// <para>
    /// Data needed for SSFA to work in curved worlds
    /// </para>
    /// <para>
    /// If the path is already in a flat plane, you can feed it directly to <see cref="SSFA.AppendCorners{TProj}"/>.
    /// If the path is curved however, you must first pass it to <see cref="UnrolledNavMeshGraphPortal.Unroll{T}"/>, which will
    /// 'unroll' it into a flat plane. The output of which can then be fed into <see cref="SSFA.GetSteerTargetPosition{T}"/> or <see cref="SSFA.AppendCornersUnrolled{T}"/>
    /// for path smoothing in 3D. 
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is not strictly confined to the NavMesh. If your path segment implements this interface, you can perform the path straightening on it.
    /// </para>
    /// <para>
    /// You may notice the <see cref="NavMeshGraphLocation"/> also implements this, allowing it to be fed directly into any of the <see cref="SSFA"/> methods.
    /// The reason is, if you know your mesh is already flat, you can skip unrolling it and use it directly.
    /// </para>
    /// </remarks>
    public interface IUnrolledNavMeshGraphPortal
    {
        /// <summary>
        /// The left side of the portal in 2D. This should be in the XZ plane.
        /// </summary>
        public float2 Left2D { get; }
        
        /// <summary>
        /// The right side of the portal in 2D. This should be in the XZ plane.
        /// </summary>
        public float2 Right2D { get; }
        
        /// <summary>
        /// The left side of the portal
        /// </summary>
        public float3 Left3D { get; }
        
        /// <summary>
        /// The right side of the portal
        /// </summary>
        public float3 Right3D { get; }
        
        /// <summary>
        /// Normal of the plane the portal is in (e.g. 0, 1, 0 for a flat XZ plane)
        /// </summary>
        public float3 Normal { get; }
    }
}