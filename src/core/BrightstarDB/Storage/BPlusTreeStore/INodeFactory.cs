using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal interface INodeFactory
    {
        ILeafNode MakeLeafNode();
        ILeafNode MakeLeafNode(ulong nodeId, byte[] nodePage, int keyCount);
        ILeafNode MakeLeafNode(ulong leafPage, byte[] nodePage, IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues, int numToLoad);
        INode MakeInternalNode(ulong nodeId, byte[] nodePage, int keyCount);
        INode MakeInternalNode(ulong nodeId, byte[] rootSplitKey, ulong rootPageId, ulong rightPageId);
        IInternalNode MakeInternalNode(ulong nodeId, ulong onlyChild);
        IInternalNode MakeInternalNode(ulong nodeId, List<byte[]> keys, List<ulong> childPointers);
    }
}
