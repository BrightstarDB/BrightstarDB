using System;
#if !SILVERLIGHT && !PORTABLE
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

namespace BrightstarDB.Storage
{
#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    internal abstract class StoreException : BrightstarException
    {
        protected StoreException(string message) : base(message){}
        protected StoreException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
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

#if !SILVERLIGHT && !PORTABLE
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Store", Store);
        }
#endif
    }


#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    internal sealed class StoreReadException : StoreException
    {
        public StoreReadException(string message, Exception innerException) : base(message, innerException){}
    }
     
}
