using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class GraphIndex : IStorable
    {
        private List<string> _graphs;

        public GraphIndex()
        {
            _graphs = new List<string>();
        }

        public string GetGraphUri(int graphId)
        {
            return _graphs[graphId-1];
        }

        public ulong LookupGraphId(string graph)
        {
            for (int i = 0; i < _graphs.Count; i++)
            {
                if (_graphs[i].Equals(graph)) return (ulong)(i+1);
            }
            return StoreConstants.NullUlong;
        }

        public ulong AssertInIndex(string graph)
        {
            for (int i = 0; i < _graphs.Count; i++)
            {
                if (_graphs[i].Equals(graph)) return (ulong)(i + 1);    
            }

            // add to graph
            _graphs.Add(graph);
            return (ulong) _graphs.Count;
        } 

        public int Save(BinaryWriter dataStream, ulong offset)
        {
            var count = SerializationUtils.WriteVarint(dataStream, (ulong) _graphs.Count);
            foreach (var graph in _graphs)
            {
                var lexValueBytes = Encoding.UTF8.GetBytes(graph);
                count += SerializationUtils.WriteVarint(dataStream, (ulong)lexValueBytes.Count());
                dataStream.Write(lexValueBytes);
            }

            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            var graphCount = (int) SerializationUtils.ReadVarint(dataStream);
            for (int i = 0; i < graphCount; i++)
            {
                var byteCount = (int)SerializationUtils.ReadVarint(dataStream);
                _graphs.Add(Encoding.UTF8.GetString(dataStream.ReadBytes(byteCount), 0, byteCount));
            }
        }

        public IEnumerable<string> GetGraphUris()
        {
            return _graphs;
        }
    }
}
