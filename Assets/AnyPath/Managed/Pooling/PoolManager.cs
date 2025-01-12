using System.Collections.Generic;
using UnityEngine;

namespace AnyPath.Managed.Pooling
{
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;
        private static List<Pool> pools = new List<Pool>();

        public static void RegisterPool(Pool pool)
        {
            pools.Add(pool);
            if (instance == null)
                new GameObject(nameof(PoolManager), typeof(PoolManager));
        }

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDestroy()
        {
            foreach (var pool in pools)
                pool.UnregisterAndDispose();
            pools.Clear();
            if (instance == this) instance = null;
        }
    }
}