using System.Collections.Generic;

namespace AnyPath.Managed.Pooling
{
    public abstract class Pool
    {
        public abstract void UnregisterAndDispose();
    }

    public abstract class Pool<T> : Pool
    {
        private Stack<T> free = new Stack<T>();
        private bool isRegistered;

        public T Get()
        {
            if (!isRegistered)
            {
                PoolManager.RegisterPool(this);
                isRegistered = true;
            }
            
            if (free.Count > 0)
                return free.Pop();
            return Create();
        }

        public void Return(T unit)
        {
            if (!isRegistered)
            {
                DisposeUnit(unit);
                return;
            }
            
            Clear(unit);
            free.Push(unit);
        }

        public override void UnregisterAndDispose()
        {
            isRegistered = false;
            foreach (var unit in free)
                DisposeUnit(unit);
            free.Clear();
        }

        protected abstract void Clear(T unit);
        protected abstract T Create();
        protected abstract void DisposeUnit(T unit);
    }
}