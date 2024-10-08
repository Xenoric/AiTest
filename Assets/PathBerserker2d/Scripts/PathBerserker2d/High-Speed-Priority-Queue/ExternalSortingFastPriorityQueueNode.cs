namespace Priority_Queue
{
    internal abstract class ExternalSortingFastPriorityQueueNode : System.IComparable<ExternalSortingFastPriorityQueueNode>
    {
        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex { get; internal set; }

        public abstract int CompareTo(ExternalSortingFastPriorityQueueNode other);
    }
}
