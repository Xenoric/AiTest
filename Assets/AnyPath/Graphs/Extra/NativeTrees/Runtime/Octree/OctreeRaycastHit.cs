using Unity.Mathematics;

namespace AnyPath.NativeTrees
{
    public struct OctreeRaycastHit<T>
    {
        public float3 point;
        public T obj;
    }
}