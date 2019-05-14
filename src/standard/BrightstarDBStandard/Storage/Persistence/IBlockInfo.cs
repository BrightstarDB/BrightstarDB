namespace BrightstarDB.Storage.Persistence
{
    /// <summary>
    /// Interface that represents a page of data from a data store
    /// </summary>
    public interface IBlockInfo
    {
        /// <summary>
        /// The page data as a byte array
        /// </summary>
        byte[] Data { get; }
        /// <summary>
        /// The page length in bytes
        /// </summary>
        int Length { get; }
        /// <summary>
        /// The page offset from the start of the file in bytes
        /// </summary>
        long Offset { get; }
    }
}
