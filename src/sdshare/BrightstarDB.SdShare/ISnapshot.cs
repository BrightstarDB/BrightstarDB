using System;

namespace BrightstarDB.SdShare
{
    public interface ISnapshot  
    {
        /// <summary>
        /// Get the id of the snapshot. The serive provider must be able to find and return the snapshot data
        /// based on this id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The datetime that this snapshot was published
        /// </summary>
        DateTime PublishedDate { get; }
    }
}