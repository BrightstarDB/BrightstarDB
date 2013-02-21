using System;
using System.IO;

namespace BrightstarDB.Storage.Persistence
{
    internal interface IPage : IPageCacheItem
    {
        /// <summary>
        /// Get the current page data
        /// </summary>
        byte[] Data { get; }

        /// <summary>
        /// Get a boolean flag indicating if the page data is different from that originally read from disk
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Get the timestamp when the page data was last modified
        /// </summary>
        long Modified { get; }

        /// <summary>
        /// Get a boolean flag indicating if the page is marked as deleted
        /// </summary>
        bool Deleted { get; }

        /// <summary>
        /// Update the page data
        /// </summary>
        /// <param name="data">The data buffer to copy from</param>
        /// <param name="srcOffset">The offset in <paramref name="data"/> to start copying from </param>
        /// <param name="pageOffset">The offset in the page to start copying to</param>
        /// <param name="len">The number of bytes to copy</param>
        void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1);

        /// <summary>
        /// Begins an asynchronous write operation
        /// </summary>
        /// <param name="outputStream">The stream to write to</param>
        /// <param name="transactionId">The transaction to write</param>
        /// <returns>The timestamp associated with the page when the write started</returns>
        long Write(Stream outputStream, ulong transactionId);

        long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId);
    }
}
