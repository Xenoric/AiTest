using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AnyPath.Managed.Disposal
{
    public class ManagedDisposer : MonoBehaviour
    {
        private static ManagedDisposer instance;

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        private static List<IScheduledFinder> pathFinders = new List<IScheduledFinder>();
        private static List<JobHandle> aborted = new List<JobHandle>();

        private void OnDestroy()
        {
            for (int i = pathFinders.Count - 1; i >= 0; i--)
            {
                var handle = pathFinders[i];
                handle.Abort();
                aborted.Add(handle.JobHandle);
            }

            pathFinders.Clear();
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// Removes all completed jobhandles from the cache
        /// </summary>
        private void Update()
        {
            for (int i = pathFinders.Count - 1; i >= 0; i--)
            {
                var finder = pathFinders[i];
                if (finder.JobHandle.IsCompleted)
                {
                    finder.Complete();
                    pathFinders.RemoveAtSwapBack(i);
                }
            }
            
            for (int i = aborted.Count - 1; i >= 0; i--)
            {
                var handle = aborted[i];
                if (handle.IsCompleted)
                {
                    handle.Complete();
                    aborted.RemoveAtSwapBack(i);
                }
            }
        }

        internal static void Register(IScheduledFinder request)
        {
            pathFinders.Add(request);
            if (instance == null)
                new GameObject(nameof(ManagedDisposer), typeof(ManagedDisposer));
        }

        internal static void Abort(IScheduledFinder request)
        {
            for (int i = pathFinders.Count - 1; i >= 0; i--)
            {
                var finder = pathFinders[i];
                if (finder != request) continue;
                
                finder.Abort();
                pathFinders.RemoveAtSwapBack(i);
                aborted.Add(finder.JobHandle);
            }
        }

        /// <summary>
        /// Dispose a graph struct safely by waiting before all pathfinding jobs scheduled with the ...Safe() methods are finished
        /// </summary>
        public static void DisposeSafe<TGraph>(TGraph graph) where TGraph : INativeDisposable
        {
            var dependencies = new NativeArray<JobHandle>(pathFinders.Count + aborted.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            int finderCount = pathFinders.Count;
            for (int i = 0; i < finderCount; i++)
                dependencies[i] = pathFinders[i].JobHandle;
            
            for (int i = 0; i < aborted.Count; i++)
                dependencies[finderCount + i] = aborted[i];

            dependencies.Dispose(graph.Dispose(JobHandle.CombineDependencies(dependencies)));
        }
    }
}