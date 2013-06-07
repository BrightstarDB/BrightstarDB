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
    }
}
