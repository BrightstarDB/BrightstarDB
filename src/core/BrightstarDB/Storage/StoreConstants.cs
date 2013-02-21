using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage
{
    internal static class StoreConstants
    {
        /// <summary>
        /// The value to use for the parent node id of root nodes in btrees
        /// </summary>
        internal const ulong NullUlong = UInt64.MinValue;
    }
}
