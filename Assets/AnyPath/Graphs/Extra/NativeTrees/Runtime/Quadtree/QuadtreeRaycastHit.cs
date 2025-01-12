using Unity.Mathematics;

namespace AnyPath.NativeTrees
{
    public struct QuadtreeRaycastHit<T>
    {
        public float2 point;
        public T obj;
    }
}