using System;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    /// <summary>
    /// These are the flags written into the first byte of the btree value for a resource
    /// </summary>
    [Flags]
    internal enum ResourceHeaderFlags
    {
        IsLiteral = 0x1,
        IsShort = 0x2
    }
}
