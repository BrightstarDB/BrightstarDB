using System;
using BrightstarDB.Caching;

namespace BrightstarDB.Storage.Persistence
{
    /// <summary>
    /// Wrapper class for an IBlockProvider that adds block caching
    /// </summary>
    [Obsolete("Not yet fully implemented")]
    public class CachingBlockProvider : IBlockProvider
    {
        private readonly ICache _cache;
        private readonly IBlockProvider _blockProvider;

        /// <summary>
        /// Creates a new caching block provider
        /// </summary>
        /// <param name="rawBlockProvider">The provider that is wrapped by this caching provider</param>
        /// <param name="cache">The cache used to store the cached blocks</param>
        public CachingBlockProvider(IBlockProvider rawBlockProvider, ICache cache)
        {
            _blockProvider = rawBlockProvider;
            _cache = cache;
        }

        #region Implementation of IBlockProvider

        /// <summary>
        /// Get the size of block that this provider works in
        /// </summary>
        public int BlockSize
        {
            get { return _blockProvider.BlockSize; }
        }

        /// <summary>
        /// Gets the byte offset of the current active block
        /// </summary>
        /// <param name="path">The path to the block store</param>
        public long GetActiveBlockOffset(string path)
        {
            return _blockProvider.GetActiveBlockOffset(path);
        }

        /// <summary>
        /// Reads data from the block storage
        /// </summary>
        /// <param name="path">The path to the block store to read from</param>
        /// <param name="offset">The start offset of the block</param>
        /// <param name="createIfNotExists"> </param>
        /// <returns>The block read</returns>
        public IBlockInfo ReadBlock(string path, long offset, bool createIfNotExists)
        {
            throw new NotImplementedException();
            /*
            string key = MakeKey(path, offset);
            byte[] ret = _cache.Lookup(key);
            int bytesRead;
            if (ret != null)
            {
                ret.CopyTo(buffer, 0);
                bytesRead = BlockSize;
                return bytesRead;
            }
            ret = new byte[BlockSize];
            bytesRead = _blockProvider.ReadBlock(path, offset, ret, createIfNotExists);
            if (!IsActive(path, offset))
            {
                _cache.Insert(key, ret, CachePriority.Normal);
            }
            return bytesRead;
             */
        }

        /// <summary>
        /// Determines if the block at the specified byte offset is the current active block
        /// </summary>
        /// <param name="path">The path to the block store to check</param>
        /// <param name="offset">The offset to check</param>
        /// <returns>True if the specified block is the current active block (the last bloock in the file), false otherwise.</returns>
        public bool IsActive(string path, long offset)
        {
            return _blockProvider.IsActive(path, offset);
        }

        /// <summary>
        /// Writes data to block storage
        /// </summary>
        /// <param name="path">The path to the block store to write to</param>
        /// <param name="offset">The start offset of the block to write</param>
        /// <param name="buffer">The buffer containing the data to be written</param>
        /// <param name="count">The number of bytes to be written</param>
        /// <returns>True if the block was written successfully, false otherwise</returns>
        public void WriteBlock(string path, long offset, byte[] buffer, int count)
        {
            _blockProvider.WriteBlock(path, offset, buffer, count);
            _cache.Remove(MakeKey(path, offset));
        }

        /// <summary>
        /// Returns the total number of bytes available in the specified block store
        /// </summary>
        /// <param name="path">The path to the block store</param>
        /// <returns>The number of bytes in the block store</returns>
        public long GetLength(string path)
        {
            return _blockProvider.GetLength(path);
        }

        #endregion

        private string MakeKey(string path, long offset)
        {
            return path + "_" + offset;
        }
    }
}
