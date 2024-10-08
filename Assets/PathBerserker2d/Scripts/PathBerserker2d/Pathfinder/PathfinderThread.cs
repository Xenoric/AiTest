using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace PathBerserker2d
{
    internal class PathfinderThread
    {
        CancellationToken cancelToken;
        ConcurrentQueue<PathRequest> pathRequestQueue;
        Pathfinder pathfinder;

        public PathfinderThread(CancellationToken cancelToken,
               ConcurrentQueue<PathRequest> pathRequestQueue, NavGraph navGraph, int id)
        {
            this.cancelToken = cancelToken;
            this.pathRequestQueue = pathRequestQueue;
            this.pathfinder = new Pathfinder(navGraph, id);
        }

        public void Run()
        {
            while (!cancelToken.IsCancellationRequested)
            {
                // will process a path or sleep for 0.1sec
                PathRequest request;
                if (pathRequestQueue.TryDequeue(out request))
                {
                    pathfinder.ProcessPathRequest(request);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public IEnumerator CoroutineRun()
        {
            while (!cancelToken.IsCancellationRequested)
            {
                // will process a path or sleep for 0.1sec
                PathRequest request;
                if (pathRequestQueue.TryDequeue(out request))
                {
                    pathfinder.ProcessPathRequest(request);
                    yield return null;
                }
                else
                    yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }
}
