using System;

namespace BrightstarDB.Storage
{
    [Serializable]
    internal abstract class StoreException : BrightstarException
    {
        protected StoreException(string message) : base(message){}
        protected StoreException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [Serializable]
    internal sealed class StoreManagerException : StoreException
    {
        /// <summary>
        /// Get the path of the store for which this exception was raised
        /// </summary>
        public string Store { get; private set; }

        public StoreManagerException(string storeLocation, string message) : base(message)
        {
            Store = storeLocation;
        }

    }

    [Serializable]
    internal sealed class StoreReadException : StoreException
    {
        public StoreReadException(string message, Exception innerException) : base(message, innerException){}
    }
     
}
