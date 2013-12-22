namespace BrightstarDB.Server
{
    internal class NoSuchStoreException : BrightstarInternalException
    {
        public string StoreName { get; private set; }
        public NoSuchStoreException(string storeName) : base("Store not found")
        {
            this.StoreName = storeName;
        }
    }
}