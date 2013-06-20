using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class BPlusTreeBuilder
    {
        private readonly IPageStore _pageStore;
        private readonly BPlusTreeConfiguration _config;
        private readonly int _leafLoadFactor;
        private readonly int _internalBranchFactor;

        public BPlusTreeBuilder(IPageStore targetPageStore, BPlusTreeConfiguration targetTreeConfiguration)
        {
            _pageStore = targetPageStore;
            _config = targetTreeConfiguration;
            // These values are copied locally to allow us to later change the default loading from 100% to something lower if we want
            _leafLoadFactor = _config.LeafLoadFactor;
            _internalBranchFactor = _config.InternalBranchFactor;
        }
        
        public ulong Build(ulong txnId, IEnumerable<KeyValuePair<byte[], byte []>> orderedValues, BrightstarProfiler profiler = null)
        {
            var nodeList = MakeInternalNodes(txnId, MakeLeafNodes(txnId, orderedValues.GetEnumerator(), profiler), profiler).ToList();
            while(nodeList.Count > 1)
            {
                nodeList = MakeInternalNodes(txnId, nodeList, profiler).ToList();
            }
            return nodeList[0].Value;
        }

        private IEnumerable<KeyValuePair<byte[], ulong>>MakeInternalNodes(ulong txnId, IEnumerable<KeyValuePair<byte[], ulong >> children, BrightstarProfiler profiler)
        {
            var enumerator = children.GetEnumerator();
            var childList = enumerator.Next(_internalBranchFactor).ToList();
            if (childList.Count == 1)
            {
                yield return childList[0];
                yield break;
            }

            byte[] prevNodeKey = childList[0].Key;
            IInternalNode prevNode = MakeInternalNode(txnId, childList);
            childList = enumerator.Next(_internalBranchFactor).ToList();
            while(childList.Count > 0)
            {
                IInternalNode nextNode = MakeInternalNode(txnId, childList);
                var nextNodeKey = childList[0].Key;
                if (nextNode.NeedJoin)
                {
                    nextNodeKey = new byte[_config.KeySize];
                    nextNode.RedistributeFromLeft(txnId, prevNode, childList[0].Key, nextNodeKey);
                }
                yield return WriteNode(txnId, prevNode, prevNodeKey, profiler);
                prevNode = nextNode;
                prevNodeKey = nextNodeKey;
                childList = enumerator.Next(_internalBranchFactor).ToList();
            }
            
            yield return WriteNode(txnId, prevNode, prevNodeKey, profiler);
        }

        private IInternalNode MakeInternalNode(ulong txnId, KeyValuePair<byte[], ulong > onlyChild)
        {
            var nodePage = _pageStore.Create(txnId);
            return MakeInternalNode(nodePage, onlyChild.Value);
        }

        private IInternalNode MakeInternalNode(ulong txnId, List<KeyValuePair<byte[], ulong >> keyValuePairs)
        {
            if (keyValuePairs.Count == 1)
            {
                return MakeInternalNode(txnId, keyValuePairs[0]);
            }
            var nodePage = _pageStore.Create(txnId);
            var childPointers = keyValuePairs.Select(kvp => kvp.Value).ToList();
            var keys = keyValuePairs.Skip(1).Select(kvp =>
                                                {
                                                    var key = new byte[_config.KeySize];
                                                    Array.Copy(kvp.Key, key, _config.KeySize);
                                                    return key;
                                                }).ToList();
            return MakeInternalNode(nodePage, keys, childPointers);
        }

        private IEnumerable<KeyValuePair<byte[], ulong >> MakeLeafNodes(ulong txnId, IEnumerator<KeyValuePair<byte[], byte[]>> orderedValues, BrightstarProfiler profiler = null)
        {
            ILeafNode prevNode = MakeLeafNode(txnId, orderedValues.Next(_leafLoadFactor));
            if (prevNode.KeyCount < _leafLoadFactor)
            {
                // There were only enough values to fill a single leaf node
                yield return WriteNode(txnId, prevNode, profiler);
                yield break;
            }
            ILeafNode nextNode = MakeLeafNode(txnId, orderedValues.Next(_leafLoadFactor));
            do
            {
                if (nextNode.KeyCount >= _config.LeafSplitIndex)
                {
                    yield return WriteNode(txnId, prevNode, profiler);
                }
                else
                {
                    // Final leaf node needs to share some values from the previous node we created
                    nextNode.RedistributeFromLeft(txnId, prevNode);
                    yield return WriteNode(txnId, prevNode, profiler);
                }
                prevNode = nextNode;
                var nextKeys = orderedValues.Next(_leafLoadFactor).ToList();
                nextNode = nextKeys.Count > 0 ? MakeLeafNode(txnId, nextKeys) : null;
            } while (nextNode != null);

            yield return WriteNode(txnId, prevNode, profiler);
        }

        private KeyValuePair<byte[], ulong> WriteNode(ulong txnId, ILeafNode node, BrightstarProfiler profiler = null)
        {
            //_pageStore.Write(txnId, node.PageId, node.GetData(), profiler: profiler);
            return new KeyValuePair<byte[], ulong>(node.LeftmostKey, node.PageId);
        }

        private KeyValuePair<byte[], ulong > WriteNode(ulong  txnId, IInternalNode node, byte[] lowestLeafKey, BrightstarProfiler profiler)
        {
            //_pageStore.Write(txnId, node.PageId, node.GetData(), profiler:profiler);
            return new KeyValuePair<byte[], ulong>(lowestLeafKey, node.PageId);
        }

        private ILeafNode MakeLeafNode(ulong txnId, IEnumerable<KeyValuePair<byte [], byte []>> orderedValues)
        {
            var leafPage = _pageStore.Create(txnId);
            return MakeLeafNode(leafPage, orderedValues, _leafLoadFactor);
        }

        #region Node factory methods

        private ILeafNode MakeLeafNode(IPage leafPage, IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues, int numToLoad)
        {
            return new LeafNode(leafPage, 0, 0, _config, orderedValues, numToLoad);
        }

        private IInternalNode MakeInternalNode(IPage nodePage, ulong onlyChild)
        {
            return new InternalNode(nodePage, onlyChild, _config);
        }

        private IInternalNode MakeInternalNode(IPage nodePage, List<byte[]> keys, List<ulong> childPointers)
        {
            return new InternalNode(nodePage, keys, childPointers, _config);
        }

        #endregion
    }
}
