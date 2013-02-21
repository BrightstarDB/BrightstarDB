using System;

namespace BrightstarDB.SdShare
{
    public interface IFragment  
    {
        /// <summary>
        /// This is the uri inserted into the TopicSI / ResourceUri property in the atom feed 
        /// </summary>
        string ResourceUri { get; }

        // This is the uri used as the request parameter back to the service.
        string ResourceId { get; }

        DateTime PublishDate { get; }
        string ResourceName { get; }
    }
}