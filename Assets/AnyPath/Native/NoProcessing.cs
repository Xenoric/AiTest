using System;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Default processing that just copies the path to the buffer without any modifications
    /// </summary>
    public struct NoProcessing<TNode> : IPathProcessor<TNode, TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        [ExcludeFromDocs]
        public void ProcessPath(TNode queryStart, TNode queryGoal, NativeList<TNode> pathNodes, NativeList<TNode> appendTo)
        {
            appendTo.AddRange(pathNodes.AsArray());
        }

        [ExcludeFromDocs]
        public bool InsertQueryStart => false;
    }
}