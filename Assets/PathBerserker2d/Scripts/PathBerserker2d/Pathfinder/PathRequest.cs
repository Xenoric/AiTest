using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Represents an async path request.
    /// </summary>
    public class PathRequest
    {
        public enum RequestState {
            Draft,
            Pending,
            Finished,
            Failed
        }

        public enum RequestFailReason {
            CouldntMapStart,
            CouldntMapGoal,
            MappedStartChanged,
            AllMappedGoalsChanged,
            NoPathFromStartToGoal,
            WorldWasDestroyed,
            ToFarFromStart,
        }

        /// <summary>
        /// Status of the processing of the request.
        /// </summary>
        public RequestState Status{ get { return status; } }
        /// <summary>
        /// If the request failed, it will set this field to the cause of failure.
        /// </summary>
        public RequestFailReason FailReason { get; private set; }
        /// <summary>
        /// If the request succeeded, this is found path.
        /// </summary>
        public Path Path { get; private set; }

        /// <summary>
        /// Start of the requested path
        /// </summary>
        public NavSegmentPositionPointer start;
        /// <summary>
        /// Goals of the requested path.
        /// </summary>
        public IList<NavSegmentPositionPointer> goals;
        /// <summary>
        /// NavAgent the calculated path should be usable by.
        /// </summary>
        public NavAgent client;

        /// <summary>
        /// If the request failed, this will contain the closest reachable position found to the goal. This does NOT work when multiple goals where specified.
        /// </summary>
        public NavSegmentPositionPointer closestReachablePosition;
        private volatile RequestState status = RequestState.Draft;

        public PathRequest(NavAgent client)
        {
            this.client = client;
            this.start = client.currentMappedPosition;
        }

        internal void SetToPending()
        {
            status = RequestState.Pending;
        }

        internal void Reset()
        {
            Debug.Assert(status != RequestState.Pending);
            status = RequestState.Draft;
        }

        internal void Fulfill(Path path) {
            this.Path = path;
            this.status = RequestState.Finished;
        }

        internal void Fail(RequestFailReason requestFailReason) {
            this.FailReason = requestFailReason;
            this.status = RequestState.Failed;
#if PBDEBUG
            Debug.Log("Pathrequest failed because " + requestFailReason);
#endif
        }
    }
}
