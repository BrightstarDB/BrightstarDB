using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class DirectNodeFactory : INodeFactory
    {
        private readonly IPageStore _pageStore;
        private readonly BPlusTreeConfiguration _config;

        public DirectNodeFactory(IPageStore pageStore, BPlusTreeConfiguration treeConfiguration)
        {
            _pageStore = pageStore;
            _config = treeConfiguration;
        }

        public ILeafNode MakeLeafNode()
        {
            ulong pageId = _pageStore.Create();
            return new DirectLeafNode(pageId, _pageStore.Retrieve(pageId, null), 0, _config);
        }

        public ILeafNode MakeLeafNode(ulong nodeId, byte[] nodePage, int keyCount)
        {
            return new DirectLeafNode(nodeId, nodePage, keyCount, _config);
        }

        public ILeafNode MakeLeafNode(ulong leafPage, byte[] nodePage, IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues, int numToLoad)
        {
            return new DirectLeafNode(leafPage, nodePage, 0, 0, _config, orderedValues, numToLoad);
        }
    }

    internal class DefaultNodeFactory : INodeFactory
    {
        private readonly IPageStore _pageStore;
        private readonly BPlusTreeConfiguration _config;

        public DefaultNodeFactory(IPageStore pageStore, BPlusTreeConfiguration treeConfiguration)
        {
            _pageStore = pageStore;
            _config = treeConfiguration;
        }

        public ILeafNode MakeLeafNode()
        {
            return new LeafNode(_pageStore.Create(), 0, 0, _config);
        }

        public ILeafNode MakeLeafNode(ulong nodeId, byte[] nodePage, int keyCount)
        {
            return new LeafNode(nodeId, nodePage, keyCount, _config);
        }

        public ILeafNode MakeLeafNode(ulong leafPage, byte[] nodePage, IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues, int numToLoad)
        {
            return new LeafNode(leafPage, 0, 0, _config, orderedValues, numToLoad);
        }
    }
}
