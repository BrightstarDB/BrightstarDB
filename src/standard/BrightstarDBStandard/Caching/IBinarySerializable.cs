using System.IO;

namespace BrightstarDB.Caching
{
    /// <summary>
    /// This interface can be implemented by cached objects that want to control their serialization
    /// </summary>
    public interface IBinarySerializable
    {
        /// <summary>
        /// Writes the binary serialization of the object to <paramref name="dataStream"/>
        /// </summary>
        /// <param name="dataStream"></param>
        /// <returns>The number of bytes written</returns>
        int Save(Stream dataStream);

        /// <summary>
        /// Reads the binary serialization of the object from <paramref name="dataStream"/>
        /// </summary>
        /// <param name="dataStream"></param>
        void Read(Stream dataStream);
    }
}
