using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using SmartAssembly.Attributes;

#if !SILVERLIGHT

#endif

namespace BrightstarDB.Storage
{
#if !SILVERLIGHT
    [Serializable]
#endif
    [DoNotObfuscate]
    internal abstract class StoreException : BrightstarException
    {
        protected StoreException(string message) : base(message){}
        protected StoreException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

#if !SILVERLIGHT
    [Serializable]
#endif
    [DoNotObfuscate]
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

#if !SILVERLIGHT
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Store", Store);
        }
#endif
    }


#if !SILVERLIGHT
    [Serializable]
#endif
    [DoNotObfuscate]
    internal sealed class StoreReadException : StoreException
    {
        public StoreReadException(string message, Exception innerException) : base(message, innerException){}
    }
     
}
