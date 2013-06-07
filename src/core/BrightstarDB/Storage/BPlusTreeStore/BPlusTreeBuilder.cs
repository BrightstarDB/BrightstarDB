using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private INodeFactory _nodeFactory;

        public BPlusTreeBuilder(IPageStore targetPageStore, BPlusTreeConfiguration targetTreeConfiguration, Type nodeFactoryType = null)
        {
            _pageStore = targetPageStore;
            _config = targetTreeConfiguration;
            CreateNodeFactory(nodeFactoryType);
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

        private void CreateNodeFactory(Type nodeFactoryType)
        {
            if (nodeFactoryType == null)
            {
                //_nodeFactory = new DefaultNodeFactory(_pageStore, _config);
                _nodeFactory = new DirectNodeFactory(_pageStore, _config);
            }
            else
            {
                _nodeFactory = Activator.CreateInstance(nodeFactoryType, _pageStore, _config) as INodeFactory;
            }
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
            InternalNode prevNode = MakeInternalNode(childList);
            childList = enumerator.Next(_internalBranchFactor).ToList();
            while(childList.Count > 0)
            {
                InternalNode nextNode = MakeInternalNode(childList);
                var nextNodeKey = childList[0].Key;
                if (nextNode.NeedJoin)
                {
                    nextNodeKey = new byte[_config.KeySize];
                    nextNode.RedistributeFromLeft(prevNode, childList[0].Key, nextNodeKey);
                }
                yield return WriteNode(txnId, prevNode, prevNodeKey, profiler);
                prevNode = nextNode;
                prevNodeKey = nextNodeKey;
                childList = enumerator.Next(_internalBranchFactor).ToList();
            }
            /*
            if (enumerator.MoveNext())
            {
                firstChild = enumerator.Current;
                InternalNode nextNode;
                var nextNodeKey = firstChild.Key;
                if (enumerator.MoveNext())
                {
                    nextNode = MakeInternalNode(firstChild, enumerator, _internalBranchFactor);
                }
                else
                {
                    nextNode = MakeInternalNode(firstChild);
                }
                if (nextNode.NeedJoin)
                {
                    nextNodeKey = new byte[_config.KeySize];
                    nextNode.RedistributeFromLeft(prevNode, firstChild.Key, nextNodeKey);
                }
                yield return WriteNode(txnId, prevNode, prevNodeKey, profiler);
                prevNode = nextNode;
                prevNodeKey = nextNodeKey;
            }
             */
            yield return WriteNode(txnId, prevNode, prevNodeKey, profiler);
        }

        private InternalNode MakeInternalNode(KeyValuePair<byte[], ulong > onlyChild)
        {
            var nodePage = _pageStore.Create();
            return new InternalNode(nodePage, onlyChild.Value, _config);
        }

        private InternalNode MakeInternalNode(List<KeyValuePair<byte[], ulong >> keyValuePairs)
        {
            if (keyValuePairs.Count == 1)
            {
                return MakeInternalNode(keyValuePairs[0]);
            }
            var nodePage = _pageStore.Create();
            var childPointers = keyValuePairs.Select(kvp => kvp.Value).ToList();
            var keys = keyValuePairs.Skip(1).Select(kvp =>
                                                {
                                                    var key = new byte[_config.KeySize];
                                                    Array.Copy(kvp.Key, key, _config.KeySize);
                                                    return key;
                                                }).ToList();
            return new InternalNode(nodePage, keys, childPointers, _config);
        }

        private InternalNode MakeInternalNode(KeyValuePair<byte[], ulong> firstChild, IEnumerator<KeyValuePair<byte[], ulong>> enumerator, int internalBranchFactor)
        {
            var nodePage = _pageStore.Create();
            var childPointers = new List<ulong>(internalBranchFactor + 1) {firstChild.Value};
            var keys = new List<byte[]>();
            var kvpList = enumerator.Next(_internalBranchFactor).ToList();
            foreach (var keyValuePair in kvpList)
            {
                childPointers.Add(keyValuePair.Value);
                var key = new byte[_config.KeySize];
                Array.Copy(keyValuePair.Key, key, _config.KeySize);
                keys.Add(key);
            }
            /*
            for (int i = 0; i < kvpList.Count; i++)
            {
                childPointers.Add(enumerator.Current.Value);
                keys.Add(new byte[_config.KeySize]);
                Array.Copy(enumerator.Current.Key, keys[i], _config.KeySize);
            }
             */
            return new InternalNode(nodePage, keys, childPointers, _config);
        }

        private IEnumerable<KeyValuePair<byte[], ulong >> MakeLeafNodes(ulong txnId, IEnumerator<KeyValuePair<byte[], byte[]>> orderedValues, BrightstarProfiler profiler = null)
        {
            ILeafNode prevNode = MakeLeafNode(orderedValues.Next(_leafLoadFactor));
            if (prevNode.KeyCount < _leafLoadFactor)
            {
                // There were only enough values to fill a single leaf node
                yield return WriteNode(txnId, prevNode, profiler);
                yield break;
            }
            ILeafNode nextNode = MakeLeafNode(orderedValues.Next(_leafLoadFactor));
            do
            {
                if (nextNode.KeyCount >= _config.LeafSplitIndex)
                {
                    yield return WriteNode(txnId, prevNode, profiler);
                }
                else
                {
                    // Final leaf node needs to share some values from the previous node we created
                    nextNode.RedistributeFromLeft(prevNode);
                    yield return WriteNode(txnId, prevNode, profiler);
                }
                prevNode = nextNode;
                var nextKeys = orderedValues.Next(_leafLoadFactor).ToList();
                nextNode = nextKeys.Count > 0 ? MakeLeafNode(nextKeys) : null;
            } while (nextNode != null);

            yield return WriteNode(txnId, prevNode, profiler);
        }

        private KeyValuePair<byte[], ulong> WriteNode(ulong txnId, ILeafNode node, BrightstarProfiler profiler = null)
        {
            _pageStore.Write(txnId, node.PageId, node.GetData(), profiler: profiler);
            return new KeyValuePair<byte[], ulong>(node.LeftmostKey, node.PageId);
        }

        private KeyValuePair<byte[], ulong > WriteNode(ulong  txnId, InternalNode node, byte[] lowestLeafKey, BrightstarProfiler profiler)
        {
            _pageStore.Write(txnId, node.PageId, node.GetData(), profiler:profiler);
            return new KeyValuePair<byte[], ulong>(lowestLeafKey, node.PageId);
        }

        private ILeafNode MakeLeafNode(IEnumerable<KeyValuePair<byte [], byte []>> orderedValues)
        {
            var leafPage = _pageStore.Create();
            return _nodeFactory.MakeLeafNode(leafPage, _pageStore.Retrieve(leafPage, null), orderedValues,
                                             _leafLoadFactor);
            //return new LeafNode(leafPage, 0, 0, _config, orderedValues, _leafLoadFactor);
        }
    }
}
