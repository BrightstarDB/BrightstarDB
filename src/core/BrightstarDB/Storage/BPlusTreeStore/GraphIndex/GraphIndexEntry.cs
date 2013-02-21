namespace BrightstarDB.Storage.BPlusTreeStore.GraphIndex
{
    internal class GraphIndexEntry
    {
        /// <summary>
        /// Get the Graph ID
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// Get the graph URI
        /// </summary>
        public string Uri { get; private set; }
        /// <summary>
        /// Get the boolean flag that indicates if the graph is marked as deleted
        /// </summary>
        public bool IsDeleted { get; private set; }

        public GraphIndexEntry(int graphId, string uri, bool isDeleted)
        {
            Id = graphId;
            Uri = uri;
            IsDeleted = isDeleted;
        }

        
    }
}