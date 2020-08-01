namespace BrightstarDB.Storage.Persistence
{
    /// <summary>
    /// The interface for a service that provides access to a collection of named block stores
    /// </summary>
    public interface IBlockProvider
    {
        /// <summary>
        /// Get the size of block that this provider works in
        /// </summary>
        int BlockSize { get; }

        /// <summary>
        /// Gets the byte offset of the current active block
        /// </summary>
        /// <param name="path">The path to the block store</param>
        long GetActiveBlockOffset(string path);

        /// <summary>
        /// Reads data from the block storage
        /// </summary>
        /// <param name="path">The path to the block store to read from</param>
        /// <param name="offset">The start offset of the block</param>
        /// <param name="createIfNotExists">Indicates if a the requested block should be created if it is beyond the current end of file.</param>
        /// <returns>The block read</returns>
        IBlockInfo ReadBlock(string path, long offset, bool createIfNotExists);

        /// <summary>
        /// Determines if the block at the specified byte offset is the current active block
        /// </summary>
        /// <param name="path">The path to the block store to check</param>
        /// <param name="offset">The offset to check</param>
        /// <returns>True if the specified block is the current active block (the last bloock in the file), false otherwise.</returns>
        bool IsActive(string path, long offset);

        /// <summary>
        /// Writes data to block storage
        /// </summary>
        /// <param name="path">The path to the block store to write to</param>
        /// <param name="offset">The start offset of the block to write</param>
        /// <param name="buffer">The buffer containing the data to be written</param>
        /// <param name="count">The number of bytes to be written</param>
        /// <returns>True if the block was written successfully, false otherwise</returns>
        void WriteBlock(string path, long offset, byte[] buffer, int count);

        /// <summary>
        /// Returns the total number of bytes available in the specified block store
        /// </summary>
        /// <param name="path">The path to the block store</param>
        /// <returns>The number of bytes in the block store</returns>
        long GetLength(string path);
    }
}
