using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    /// <summary>
    /// Interface for all objects that can store and reconstitute themselves from a data stream
    /// </summary>
    internal interface IStorable
    {
        /// <summary>
        /// Stores the objects into the stream returning the number of bytes written
        /// </summary>
        /// <param name="dataStream">The stream</param>
        /// <param name="offset">Passed through in some situations where the internal serialiser needs to know</param>
        /// <returns>Total number of bytes written</returns>
        int Save(BinaryWriter dataStream, ulong offset = 0ul);

        /// <summary>
        /// Load the state data from the stream provided.
        /// </summary>
        /// <param name="dataStream">Datastream containing the data</param>
        void Read(BinaryReader dataStream);

    }
}
