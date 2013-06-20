using System;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.Persistence
{
    interface IPageStore : IDisposable
    {
        /// <summary>
        /// Retrieves the data for the specified page
        /// </summary>
        /// <param name="pageId">The ID of the page</param>
        /// <param name="profiler"></param>
        /// <returns>The data buffer for the page</returns>
        IPage Retrieve(ulong pageId, BrightstarProfiler profiler);

        /// <summary>
        /// Creates a new empty page in the page store
        /// </summary>
        /// <param name="commitId">The transaction identifier for the update</param>
        /// <returns>The new page</returns>
        IPage Create(ulong commitId);

        /// <summary>
        /// Commits all changed and new pages to the page store
        /// </summary>
        /// <param name="commitId">The transaction identifier for the commit</param>
        /// <param name="profiler"></param>
        void Commit(ulong commitId, BrightstarProfiler profiler);

        /// <summary>
        /// Writes data to the specified page
        /// </summary>
        /// <param name="commitId">The transaction identifier for the update</param>
        /// <param name="pageId">The ID of the page to write to</param>
        /// <param name="buff">The data to be written</param>
        /// <param name="srcOffset">The offset into <paramref name="buff"/> from which to start copying bytes. Defaults to 0</param>
        /// <param name="pageOffset">The offset into the page data buffer to start writing to. Defaults to 0</param>
        /// <param name="len">The number of bytes to write. Defaults to all bytes in <paramref name="buff"/> from the specified <paramref name="srcOffset"/></param>
        /// <param name="profiler"></param>
        void Write(ulong commitId, ulong pageId, byte[] buff, int srcOffset=0, int pageOffset = 0, int len = -1, BrightstarProfiler profiler= null);

        /// <summary>
        /// Returns a boolean flag indicating if the page with the specified page ID is writeable
        /// </summary>
        /// <param name="page">The page to test</param>
        /// <returns>True if the page is writeable, false otherwise</returns>
        /// <remarks>In an append-only store, only pages created since the last commit are writeable. In a binary-page store, all pages are always writeable. 
        /// Client code should use this method to determine if an update to a page can be done by a call to Write() or if a new page needs to be created using Create()</remarks>
        bool IsWriteable(IPage page);

        /// <summary>
        /// Returns a writeable copy of the specified page
        /// </summary>
        /// <param name="commitId">The transaction identifier for the update</param>
        /// <param name="page">The page to be copied</param>
        /// <returns>If <paramref name="page"/> is writeable, it is returned. Otherwise a new writeable copy of <paramref name="page"/> is returned.</returns>
        IPage GetWriteablePage(ulong commitId, IPage page);

        /// <summary>
        /// Get the size (in bytes) of each data page
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Get the flag that indicates if the store can be read from
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// Get the flag that indicates if the store can be written to
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// Close the store, releasing any resources (such as file handles) it may be using
        /// </summary>
        void Close();
    }
}