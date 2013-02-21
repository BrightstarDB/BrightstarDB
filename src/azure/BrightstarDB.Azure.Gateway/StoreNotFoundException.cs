using System;

namespace BrightstarDB.Azure.Gateway
{
    public class StoreNotFoundException : Exception
    {
        public StoreNotFoundException(string storeName) : base("Invalid store name: " + storeName){}
    }
}