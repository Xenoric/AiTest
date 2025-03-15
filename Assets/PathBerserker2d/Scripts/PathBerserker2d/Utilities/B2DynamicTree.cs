
// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// ported by me, orignal source: https://github.com/erincatto/box2d/blob/master/src/collision/b2_dynamic_tree.cpp

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    /// A node in the dynamic tree. The client does not interact with this directly.
    internal struct B2TreeNode<T>
    {
        /// Enlarged AABB
        public Rect aabb;

        public T userData;

        public int parent;
        public int child1;
        public int child2;
        public int next;

        // leaf = 0, free node = -1
        public int height;

        public bool moved;

        public bool IsLeaf()
        {
            return child1 == -1;
        }
    }

    internal class B2DynamicTree<T> : IEnumerable<T>
    {
        public int NodeCount => nodeCount;

        private readonly float AABBExtension = 0.1f;
        private readonly float AABBMultiplier = 4.0f;

        int root = -1;
        B2TreeNode<T>[] nodes;
        int nodeCount = 0;
        int nextFreeNode = 0;

        public B2DynamicTree(int capacity = 16)
        {
            nodes = new B2TreeNode<T>[capacity];

            // Build a linked list for the free list.
            for (int i = 0; i < nodes.Length - 1; ++i)
            {
                nodes[i].next = i + 1;
                nodes[i].height = -1;
            }
            nodes[nodes.Length - 1].next = -1;
            nodes[nodes.Length - 1].height = -1;
        }

        // Allocate a node from the pool. Grow the pool if necessary.
        private int AllocateNode()
        {
            // Expand the node pool as needed.
            if (nextFreeNode == -1)
            {
#if PBDEBUG
                Debug.Assert(nodes.Length == nodeCount);
#endif

                // The free list is empty. Rebuild a bigger pool.
                Array.Resize(ref nodes, nodes.Length * 2);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                for (int i = nodeCount; i < nodes.Length - 1; ++i)
                {
                    nodes[i].next = i + 1;
                    nodes[i].height = -1;
                }
                nodes[nodes.Length - 1].next = -1;
                nodes[nodes.Length - 1].height = -1;
                nextFreeNode = nodeCount;
            }

            // Peel a node off the free list.
            int nodeId = nextFreeNode;
            nextFreeNode = nodes[nodeId].next;
            nodes[nodeId].parent = -1;
            nodes[nodeId].child1 = -1;
            nodes[nodeId].child2 = -1;
            nodes[nodeId].height = 0;
            nodes[nodeId].userData = default(T);
            nodes[nodeId].moved = false;
            ++nodeCount;
            return nodeId;
        }

        // Return a node to the pool.
        private void FreeNode(int nodeId)
        {
#if PBDEBUG
            Debug.Assert(0 <= nodeId && nodeId < nodes.Length);
            Debug.Assert(0 < nodeCount);
#endif
            nodes[nodeId].next = nextFreeNode;
            nodes[nodeId].height = -1;
            nextFreeNode = nodeId;
            --nodeCount;
        }

        // Create a proxy in the tree as a leaf node. We return the index
        // of the node instead of a pointer so that we can grow
        // the node pool.
        public int CreateProxy(Rect aabb, T userData)
        {
            int proxyId = AllocateNode();

            // Fatten the aabb.
            Vector2 r = new Vector2(AABBExtension, AABBExtension);
            nodes[proxyId].aabb.min = aabb.min - r;
            nodes[proxyId].aabb.max = aabb.max + r;
            nodes[proxyId].userData = userData;
            nodes[proxyId].height = 0;
            nodes[proxyId].moved = true;

            InsertLeaf(proxyId);

            return proxyId;
        }

        public void RemoveProxy(int proxyId)
        {
#if PBDEBUG
            Debug.Assert(0 <= proxyId && proxyId < nodes.Length);
            Debug.Assert(nodes[proxyId].IsLeaf());
#endif

            if (!nodes[proxyId].IsLeaf())
                throw new Exception("Tried to remove non leaf node");

            RemoveLeaf(proxyId);
            FreeNode(proxyId);
        }

        public bool MoveProxy(int proxyId, Rect aabb, Vector2 displacement)
        {
#if PBDEBUG
            Debug.Assert(0 <= proxyId && proxyId < nodes.Length);
            Debug.Assert(nodes[proxyId].IsLeaf());
#endif

            // Extend AABB

            Vector2 r = new Vector2(AABBExtension, AABBExtension);
            Rect fatAABB = new Rect(aabb.min - r, aabb.size + 2 * r);

            // Predict AABB movement
            Vector2 d = AABBMultiplier * displacement;

            if (d.x < 0.0f)
            {
                fatAABB.xMin += d.x;
            }
            else
            {
                fatAABB.xMax += d.x;
            }

            if (d.y < 0.0f)
            {
                fatAABB.yMin += d.y;
            }
            else
            {
                fatAABB.yMin += d.y;
            }

            Rect treeAABB = nodes[proxyId].aabb;
            if (treeAABB.ContainsBounds(aabb))
            {
                // The tree AABB still contains the object, but it might be too large.
                // Perhaps the object was moving fast but has since gone to sleep.
                // The huge AABB is larger than the new fat AABB.
                Rect hugeAABB = new Rect(fatAABB.min - 4.0f * r, fatAABB.size + 8 * r);

                if (hugeAABB.ContainsBounds(treeAABB))
                {
                    // The tree AABB contains the object AABB and the tree AABB is
                    // not too large. No tree update needed.
                    return false;
                }

                // Otherwise the tree AABB is huge and needs to be shrunk
            }

            RemoveLeaf(proxyId);

            nodes[proxyId].aabb = fatAABB;

            InsertLeaf(proxyId);

            nodes[proxyId].moved = true;

            return true;
        }


        private void InsertLeaf(int leaf)
        {
            if (root == -1)
            {
                root = leaf;
                nodes[root].parent = -1;
                return;
            }

            // Find the best sibling for this node
            Rect leafAABB = nodes[leaf].aabb;
            int index = root;
            while (nodes[index].IsLeaf() == false)
            {
                int child1 = nodes[index].child1;
                int child2 = nodes[index].child2;

                float area = nodes[index].aabb.Area();

                Rect combinedAABB = leafAABB.CombineWith(nodes[index].aabb);
                float combinedArea = combinedAABB.Area();

                // Cost of creating a new parent for this node and the new leaf
                float cost = 2.0f * combinedArea;

                // Minimum cost of pushing the leaf further down the tree
                float inheritanceCost = 2.0f * (combinedArea - area);

                // Cost of descending into child1
                float cost1;
                if (nodes[child1].IsLeaf())
                {
                    Rect aabb = leafAABB.CombineWith(nodes[child1].aabb);
                    cost1 = aabb.Area() + inheritanceCost;
                }
                else
                {
                    Rect aabb = leafAABB.CombineWith(nodes[child1].aabb);
                    float oldArea = nodes[child1].aabb.Area();
                    float newArea = aabb.Area();
                    cost1 = (newArea - oldArea) + inheritanceCost;
                }

                // Cost of descending into child2
                float cost2;
                if (nodes[child2].IsLeaf())
                {
                    Rect aabb = leafAABB.CombineWith(nodes[child2].aabb);
                    cost2 = aabb.Area() + inheritanceCost;
                }
                else
                {
                    Rect aabb = leafAABB.CombineWith(nodes[child2].aabb);
                    float oldArea = nodes[child2].aabb.Area();
                    float newArea = aabb.Area();
                    cost2 = newArea - oldArea + inheritanceCost;
                }

                // Descend according to the minimum cost.
                if (cost < cost1 && cost < cost2)
                {
                    break;
                }

                // Descend
                if (cost1 < cost2)
                {
                    index = child1;
                }
                else
                {
                    index = child2;
                }
            }

            int sibling = index;

            // Create a new parent.
            int oldParent = nodes[sibling].parent;
            int newParent = AllocateNode();
            nodes[newParent].parent = oldParent;
            nodes[newParent].userData = default(T);
            nodes[newParent].aabb = leafAABB.CombineWith(nodes[sibling].aabb);
            nodes[newParent].height = nodes[sibling].height + 1;

            if (oldParent != -1)
            {
                // The sibling was not the root.
                if (nodes[oldParent].child1 == sibling)
                {
                    nodes[oldParent].child1 = newParent;
                }
                else
                {
                    nodes[oldParent].child2 = newParent;
                }

                nodes[newParent].child1 = sibling;
                nodes[newParent].child2 = leaf;
                nodes[sibling].parent = newParent;
                nodes[leaf].parent = newParent;
            }
            else
            {
                // The sibling was the root.
                nodes[newParent].child1 = sibling;
                nodes[newParent].child2 = leaf;
                nodes[sibling].parent = newParent;
                nodes[leaf].parent = newParent;
                root = newParent;
            }

            // Walk back up the tree fixing heights and AABBs
            index = nodes[leaf].parent;
            while (index != -1)
            {
                index = Balance(index);

                int child1 = nodes[index].child1;
                int child2 = nodes[index].child2;

#if PBDEBUG
                Debug.Assert(child1 != -1);
                Debug.Assert(child2 != -1);
#endif

                nodes[index].height = 1 + Mathf.Max(nodes[child1].height, nodes[child2].height);
                nodes[index].aabb = nodes[child1].aabb.CombineWith(nodes[child2].aabb);

                index = nodes[index].parent;
            }

            //Validate();
        }

        private void RemoveLeaf(int leaf)
        {
            if (leaf == root)
            {
                root = -1;
                return;
            }

            int parent = nodes[leaf].parent;
            int grandParent = nodes[parent].parent;
            int sibling;
            if (nodes[parent].child1 == leaf)
            {
                sibling = nodes[parent].child2;
            }
            else
            {
                sibling = nodes[parent].child1;
            }

            if (grandParent != -1)
            {
                // Destroy parent and connect sibling to grandParent.
                if (nodes[grandParent].child1 == parent)
                {
                    nodes[grandParent].child1 = sibling;
                }
                else
                {
                    nodes[grandParent].child2 = sibling;
                }
                nodes[sibling].parent = grandParent;
                FreeNode(parent);

                // Adjust ancestor bounds.
                int index = grandParent;
                while (index != -1)
                {
                    index = Balance(index);

                    int child1 = nodes[index].child1;
                    int child2 = nodes[index].child2;

                    nodes[index].aabb = nodes[child1].aabb.CombineWith(nodes[child2].aabb);
                    nodes[index].height = 1 + Mathf.Max(nodes[child1].height, nodes[child2].height);

                    index = nodes[index].parent;
                }
            }
            else
            {
                root = sibling;
                nodes[sibling].parent = -1;
                FreeNode(parent);
            }

            //Validate();
        }

        // Perform a left or right rotation if node A is imbalanced.
        // Returns the new root index.
        private int Balance(int iA)
        {
#if PBDEBUG
            Debug.Assert(iA != -1);
#endif

            if (nodes[iA].IsLeaf() || nodes[iA].height < 2)
            {
                return iA;
            }

            int iB = nodes[iA].child1;
            int iC = nodes[iA].child2;
#if PBDEBUG
            Debug.Assert(0 <= iB && iB < nodes.Length);
            Debug.Assert(0 <= iC && iC < nodes.Length);
#endif

            int balance = nodes[iC].height - nodes[iB].height;

            // Rotate C up
            if (balance > 1)
            {
                int iF = nodes[iC].child1;
                int iG = nodes[iC].child2;

#if PBDEBUG
                Debug.Assert(0 <= iF && iF < nodes.Length);
                Debug.Assert(0 <= iG && iG < nodes.Length);
#endif

                // Swap A and C
                nodes[iC].child1 = iA;
                nodes[iC].parent = nodes[iA].parent;
                nodes[iA].parent = iC;

                // A's old parent should point to C
                if (nodes[iC].parent != -1)
                {
                    if (nodes[nodes[iC].parent].child1 == iA)
                    {
                        nodes[nodes[iC].parent].child1 = iC;
                    }
                    else
                    {
#if PBDEBUG
                        Debug.Assert(nodes[nodes[iC].parent].child2 == iA);
#endif
                        nodes[nodes[iC].parent].child2 = iC;
                    }
                }
                else
                {
                    root = iC;
                }

                // Rotate
                if (nodes[iF].height > nodes[iG].height)
                {
                    nodes[iC].child2 = iF;
                    nodes[iA].child2 = iG;
                    nodes[iG].parent = iA;
                    nodes[iA].aabb = nodes[iB].aabb.CombineWith(nodes[iG].aabb);
                    nodes[iC].aabb = nodes[iA].aabb.CombineWith(nodes[iF].aabb);

                    nodes[iA].height = 1 + Mathf.Max(nodes[iB].height, nodes[iG].height);
                    nodes[iC].height = 1 + Mathf.Max(nodes[iA].height, nodes[iF].height);
                }
                else
                {
                    nodes[iC].child2 = iG;
                    nodes[iA].child2 = iF;
                    nodes[iF].parent = iA;
                    nodes[iA].aabb = nodes[iB].aabb.CombineWith(nodes[iF].aabb);
                    nodes[iC].aabb = nodes[iA].aabb.CombineWith(nodes[iG].aabb);

                    nodes[iA].height = 1 + Mathf.Max(nodes[iB].height, nodes[iF].height);
                    nodes[iC].height = 1 + Mathf.Max(nodes[iA].height, nodes[iG].height);
                }

                return iC;
            }

            // Rotate B up
            if (balance < -1)
            {
                int iD = nodes[iB].child1;
                int iE = nodes[iB].child2;

#if PBDEBUG
                Debug.Assert(0 <= iD && iD < nodes.Length);
                Debug.Assert(0 <= iE && iE < nodes.Length);
#endif

                // Swap A and B
                nodes[iB].child1 = iA;
                nodes[iB].parent = nodes[iA].parent;
                nodes[iA].parent = iB;

                // A's old parent should point to B
                if (nodes[iB].parent != -1)
                {
                    if (nodes[nodes[iB].parent].child1 == iA)
                    {
                        nodes[nodes[iB].parent].child1 = iB;
                    }
                    else
                    {
#if PBDEBUG
                        Debug.Assert(nodes[nodes[iB].parent].child2 == iA);
#endif
                        nodes[nodes[iB].parent].child2 = iB;
                    }
                }
                else
                {
                    root = iB;
                }

                // Rotate
                if (nodes[iD].height > nodes[iE].height)
                {
                    nodes[iB].child2 = iD;
                    nodes[iA].child1 = iE;
                    nodes[iE].parent = iA;
                    nodes[iA].aabb = nodes[iC].aabb.CombineWith(nodes[iE].aabb);
                    nodes[iB].aabb = nodes[iA].aabb.CombineWith(nodes[iD].aabb);

                    nodes[iA].height = 1 + Mathf.Max(nodes[iC].height, nodes[iE].height);
                    nodes[iB].height = 1 + Mathf.Max(nodes[iA].height, nodes[iD].height);
                }
                else
                {
                    nodes[iB].child2 = iE;
                    nodes[iA].child1 = iD;
                    nodes[iD].parent = iA;
                    nodes[iA].aabb = nodes[iC].aabb.CombineWith(nodes[iD].aabb);
                    nodes[iB].aabb = nodes[iA].aabb.CombineWith(nodes[iE].aabb);

                    nodes[iA].height = 1 + Mathf.Max(nodes[iC].height, nodes[iD].height);
                    nodes[iB].height = 1 + Mathf.Max(nodes[iA].height, nodes[iE].height);
                }

                return iB;
            }

            return iA;
        }

        public int GetHeight()
        {
            if (root == -1)
            {
                return 0;
            }

            return nodes[root].height;
        }

        //
        public float GetAreaRatio()
        {
            if (root == -1)
            {
                return 0.0f;
            }

            float rootArea = nodes[root].aabb.Area();

            float totalArea = 0.0f;
            for (int i = 0; i < nodes.Length; ++i)
            {
                if (nodes[i].height < 0)
                {
                    // Free node in pool
                    continue;
                }

                totalArea += nodes[i].aabb.Area();
            }

            return totalArea / rootArea;
        }

        // Compute the height of a sub-tree.
        private int ComputeHeight(int nodeId)
        {
#if PBDEBUG
            Debug.Assert(0 <= nodeId && nodeId < nodes.Length);
#endif

            if (nodes[nodeId].IsLeaf())
            {
                return 0;
            }

            int height1 = ComputeHeight(nodes[nodeId].child1);
            int height2 = ComputeHeight(nodes[nodeId].child2);
            return 1 + Mathf.Max(height1, height2);
        }

        private int ComputeHeight()
        {
            int height = ComputeHeight(root);
            return height;
        }

        private void ValidateStructure(int index)
        {
            if (index == -1)
            {
                return;
            }

            if (index == root)
            {
                Debug.Assert(nodes[index].parent == -1);
            }

            int child1 = nodes[index].child1;
            int child2 = nodes[index].child2;

            if (nodes[index].IsLeaf())
            {
                Debug.Assert(child1 == -1);
                Debug.Assert(child2 == -1);
                Debug.Assert(nodes[index].height == 0);
                return;
            }

            Debug.Assert(0 <= child1 && child1 < nodes.Length);
            Debug.Assert(0 <= child2 && child2 < nodes.Length);

            Debug.Assert(nodes[child1].parent == index);
            Debug.Assert(nodes[child2].parent == index);

            ValidateStructure(child1);
            ValidateStructure(child2);
        }

        private void ValidateMetrics(int index)
        {
            if (index == -1)
            {
                return;
            }

            int child1 = nodes[index].child1;
            int child2 = nodes[index].child2;

            if (nodes[index].IsLeaf())
            {
                Debug.Assert(child1 == -1);
                Debug.Assert(child2 == -1);
                Debug.Assert(nodes[index].height == 0);
                return;
            }

            Debug.Assert(0 <= child1 && child1 < nodes.Length);
            Debug.Assert(0 <= child2 && child2 < nodes.Length);

            int height1 = nodes[child1].height;
            int height2 = nodes[child2].height;
            int height;
            height = 1 + Mathf.Max(height1, height2);
            Debug.Assert(nodes[index].height == height);

            Rect aabb = nodes[child1].aabb.CombineWith(nodes[child2].aabb);

            Debug.Assert(aabb.min == nodes[index].aabb.min);
            Debug.Assert(aabb.max == nodes[index].aabb.max);

            ValidateMetrics(child1);
            ValidateMetrics(child2);
        }

        public void Validate()
        {
#if b2DEBUG
            ValidateStructure(root);
            ValidateMetrics(root);

            int freeCount = 0;
            int freeIndex = nextFreeNode;
            while (freeIndex != -1)
            {
                Debug.Assert(0 <= freeIndex && freeIndex < nodes.Length);
                freeIndex = nodes[freeIndex].next;
                ++freeCount;
            }

            Debug.Assert(GetHeight() == ComputeHeight());

            Debug.Assert(nodeCount + freeCount == nodes.Length);
#endif
        }

        public int GetMaxBalance()
        {
            int maxBalance = 0;
            for (int i = 0; i < nodes.Length; ++i)
            {
                if (nodes[i].height <= 1)
                {
                    continue;
                }

#if PBDEBUG
                Debug.Assert(nodes[i].IsLeaf() == false);
#endif

                int child1 = nodes[i].child1;
                int child2 = nodes[i].child2;
                int balance = Mathf.Abs(nodes[child2].height - nodes[child1].height);
                maxBalance = Mathf.Max(maxBalance, balance);
            }

            return maxBalance;
        }

        public void RebuildBottomUp()
        {
            int[] nodeIndecies = new int[nodeCount];
            int count = 0;

            // Build array of leaves. Free the rest.
            for (int i = 0; i < nodes.Length; ++i)
            {
                if (nodes[i].height < 0)
                {
                    // free node in pool
                    continue;
                }

                if (nodes[i].IsLeaf())
                {
                    nodes[i].parent = -1;
                    nodeIndecies[count] = i;
                    ++count;
                }
                else
                {
                    FreeNode(i);
                }
            }

            while (count > 1)
            {
                float minCost = float.MaxValue;
                int iMin = -1, jMin = -1;
                for (int i = 0; i < count; ++i)
                {
                    Rect aabbi = nodes[nodeIndecies[i]].aabb;

                    for (int j = i + 1; j < count; ++j)
                    {
                        Rect aabbj = nodes[nodeIndecies[j]].aabb;
                        Rect b = aabbi.CombineWith(aabbj);
                        float cost = b.Area();
                        if (cost < minCost)
                        {
                            iMin = i;
                            jMin = j;
                            minCost = cost;
                        }
                    }
                }

                int index1 = nodeIndecies[iMin];
                int index2 = nodeIndecies[jMin];

                int parentIndex = AllocateNode();
                nodes[parentIndex].child1 = index1;
                nodes[parentIndex].child2 = index2;
                nodes[parentIndex].height = 1 + Mathf.Max(nodes[index1].height, nodes[index2].height);
                nodes[parentIndex].aabb = nodes[index1].aabb.CombineWith(nodes[index2].aabb);
                nodes[parentIndex].parent = -1;

                nodes[index1].parent = parentIndex;
                nodes[index2].parent = parentIndex;

                nodeIndecies[jMin] = nodeIndecies[count - 1];
                nodeIndecies[iMin] = parentIndex;
                --count;
            }

            root = nodeIndecies[0];

            Validate();
        }

        public void ShiftOrigin(Vector2 newOrigin)
        {
            // Build array of leaves. Free the rest.
            for (int i = 0; i < nodes.Length; ++i)
            {
                nodes[i].aabb.min -= newOrigin;
                nodes[i].aabb.max -= newOrigin;
            }
        }

        public T GetUserData(int proxyId)
        {
#if PBDEBUG
            Debug.Assert(0 <= proxyId && proxyId < nodes.Length);
#endif
            return nodes[proxyId].userData;
        }

        public bool TryGetUserData(int proxyId, out T data)
        {
            if (proxyId < 0 || proxyId >= nodes.Length)
            {
                data = default(T);
                return false;
            }
            data = nodes[proxyId].userData;
            return true;
        }

        public bool WasMoved(int proxyId)
        {
#if PBDEBUG
            Debug.Assert(0 <= proxyId && proxyId < nodes.Length);
#endif
            return nodes[proxyId].moved;
        }

        public void ClearMoved(int proxyId)
        {
#if PBDEBUG
            Debug.Assert(0 <= proxyId && proxyId < nodes.Length);
#endif
            nodes[proxyId].moved = false;
        }

        public Rect GetFatAABB(int proxyId)
        {
#if PBDEBUG
            Debug.Assert(0 <= proxyId && proxyId < nodes.Length);
#endif
            return nodes[proxyId].aabb;
        }

        Stack<int> queryStack = new Stack<int>();
        public QueryEnumerator Query(Rect aabb)
        {
            return new QueryEnumerator(this, aabb);
        }

        public IEnumerator<T> GetEnumerator()
        {
            // Build array of leaves. Free the rest.
            for (int i = 0; i < nodes.Length; ++i)
            {
                if (nodes[i].height >= 0 && nodes[i].IsLeaf())
                {
                    yield return nodes[i].userData;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Draw()
        {
            Draw(root);
        }

        private void Draw(int index)
        {
            if (index == -1)
            {
                return;
            }

            Gizmos.color = nodes[index].IsLeaf() ? Color.green : Color.red;
            DebugDrawingExtensions.DrawRect(nodes[index].aabb);

            Draw(nodes[index].child1);
            Draw(nodes[index].child2);
        }

        public struct QueryEnumerator {

            B2DynamicTree<T> tree;
            Rect aabb;

            public QueryEnumerator(B2DynamicTree<T> tree, Rect aabb)
            {
                this.tree = tree;
                this.aabb = aabb;
                Current = 0;
                tree.queryStack.Push(tree.root);
            }

            public int Current {
                get;
                private set;
            }

            public bool MoveNext()
            {
                while (tree.queryStack.Count > 0)
                {
                    int nodeId = tree.queryStack.Pop();
                    if (nodeId == -1)
                    {
                        continue;
                    }

                    if (tree.nodes[nodeId].aabb.Overlaps(aabb))
                    {
                        if (tree.nodes[nodeId].IsLeaf())
                        {
                            Current = nodeId;
                            return true;
                        }
                        else
                        {
                            tree.queryStack.Push(tree.nodes[nodeId].child1);
                            tree.queryStack.Push(tree.nodes[nodeId].child2);
                        }
                    }
                }
                return false;
            }
        }
    }
}