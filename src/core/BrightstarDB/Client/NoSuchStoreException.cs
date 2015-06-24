namespace BrightstarDB.Client
{
    /// <summary>
    /// Exception raised when an attempt is made to access a non-existent store
    /// </summary>
    public class NoSuchStoreException : BrightstarInternalException
    {
        /// <summary>
        /// Get the name of the store that the client attempted to access
        /// </summary>
        public string StoreName { get; private set; }

        /// <summary>
        /// Create a new NoSuchStoreException
        /// </summary>
        /// <param name="storeName">The name of the store that the client attempted to access</param>
        public NoSuchStoreException(string storeName) : base("Store not found")
        {
            StoreName = storeName;
        }
    }
}