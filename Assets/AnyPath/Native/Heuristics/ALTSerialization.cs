using System;
using System.IO;
using Unity.Collections;

namespace AnyPath.Native.Heuristics
{
    public static class ALTSerialization
    {
        /// <summary>
        /// Populates this ALT heuristic provider with binary data.
        /// </summary>
        /// <param name="alt">The ALT provider to populate</param>
        /// <param name="reader">Binary reader to read the data from. Consider using a compressed stream source for large graphs.</param>
        /// <param name="readNode">A function that reads a node from the reader</param>
        /// <typeparam name="TNode">The type of node that was used</typeparam>
        public static void ReadFrom<TNode>(this ALT<TNode> alt, BinaryReader reader, Func<BinaryReader, TNode> readNode) where TNode : unmanaged, IEquatable<TNode>
        {
            alt.GetNativeContainers(
                out var fromLandmarks, 
                out var toLandmarks, 
                out var landmarks, 
                out var isDirected);
            
            fromLandmarks.Clear();
            toLandmarks.Clear();
            landmarks.Clear();

            isDirected.Value = reader.ReadBoolean();
            int landmarkCount = reader.ReadInt32();
            for (int i = 0; i < landmarkCount; i++)
            {
                var node = readNode(reader);
                landmarks.Add(node);
            }

            int fromCount = reader.ReadInt32();
            for (int i = 0; i < fromCount; i++)
            {
                // read location
                var node = readNode(reader);
                FixedList128Bytes<float> distances = new FixedList128Bytes<float>();
                
                // read distances
                for (int j = 0; j < landmarkCount; j++)
                    distances.Add(reader.ReadSingle());
                
                // add
                fromLandmarks.Add(node, distances);
            }

            // if the graph isn't directed, we're done
            if (!isDirected.Value)
                return;
            
            // do the same for TO distances in case graph is directed
            int toCount = reader.ReadInt32();
            for (int i = 0; i < toCount; i++)
            {
                // read location
                var node = readNode(reader);
                FixedList128Bytes<float> distances = new FixedList128Bytes<float>();
                
                // read distances
                for (int j = 0; j < landmarkCount; j++)
                    distances.Add(reader.ReadSingle());
                
                // add
                toLandmarks.Add(node, distances);
            }
        }
        
        /// <summary>
        /// Writes this ALT heuristics into a stream.
        /// </summary>
        /// <param name="alt">ALT provider to serialize</param>
        /// <param name="writer">BinaryWriter to use for writing</param>
        /// <param name="writeNode">Provide a function that serializes a single graph node</param>
        /// <typeparam name="TNode">Type of nodes used</typeparam>
        /// <remarks>Consider writing into a compressed stream if your graph is large.</remarks>
        public static void WriteTo<TNode>(this ALT<TNode> alt, BinaryWriter writer, Action<BinaryWriter, TNode> writeNode) where TNode : unmanaged, IEquatable<TNode>
        {
            // for completeness, we write if the source graph is directed, but in this case, the Int2Grid isn't so this is not strictly neccessary here
            writer.Write(alt.IsDirected);
            
            // write the amount of landmarks
            writer.Write(alt.LandmarkCount);
            
            // write the position of each landmark
            for (int i = 0; i < alt.LandmarkCount; i++)
                writeNode(writer, alt.GetLandmarkLocation(i));

            var kv = alt.GetFromKeyValueArrays(Allocator.Temp);
            writer.Write(kv.Length);
            for (int i = 0; i < kv.Length; i++)
            {
                // write node
                writeNode(writer, kv.Keys[i]);
                
                // write distances
                var distances = kv.Values[i];
                for (int j = 0; j < alt.LandmarkCount; j++)
                {
                    writer.Write(distances[j]);
                }
            }
            kv.Dispose();

            if (alt.IsDirected)
            {
                kv = alt.GetToKeyValueArrays(Allocator.Temp);
                for (int i = 0; i < kv.Length; i++)
                {
                    // write node
                    writeNode(writer, kv.Keys[i]);

                    var distances = kv.Values[i];
                    for (int j = 0; j < alt.LandmarkCount; j++)
                    {
                        writer.Write(distances[j]);
                    }
                }
                kv.Dispose();
            }
        }
    }
}