using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BrightstarDB.Server
{
#if !PORTABLE && !SILVERLIGHT
    [Serializable]
#endif
    internal class QueryCacheEntry
    {
        public QueryCacheEntry(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; set; }

        public void WriteTo(Stream output)
        {
            output.Write(Data, 0, Data.Length);
        }
    }
}
