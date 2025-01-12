using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Output of the <see cref="NavMeshGraphCorners3D"/> path processor. This gives you a list of points and their up vector your agent can follow.
    /// </summary>
    public struct CornerAndNormal
    {
        public float3 position;
        public float3 normal;
    }
}