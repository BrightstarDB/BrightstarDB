using System;

namespace BrightstarDB.Storage.Persistence
{
    /// <summary>
    /// Exceptions of this type are thrown if an attempt is made to retrieve the read buffer for a page
    /// and both halves of the page have a transaction id greater than the one used for the read.
    /// </summary>
    /// <remarks>This exception indicates that the store has been concurrently modified at least
    /// twice since it was opened by the client. The client should close and reopen the store</remarks>
    internal class ReadWriteStoreModifiedException : Exception
    {
    }
}