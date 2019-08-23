using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.TweetBoxCore
{
    /// <summary>
    /// This interface represents a short status update that is published by a user
    /// </summary>
    [Entity]
    public interface ITweet
    {
        /// <summary>
        /// Id of the tweet
        /// </summary>
        /// <remarks>This property is set by BrightStar, and so is a required property</remarks>
        string Id { get; }

        /// <summary>
        /// The text content of the tweet
        /// </summary>
        string Content { get; set; }

        /// <summary>
        /// The date the tweet was published
        /// </summary>
        DateTime DatePublished { get; set; }

        /// <summary>
        /// The user who published the tweet
        /// </summary>
        IUser Author { get; set; }

        /// <summary>
        /// The collection of hashtags that a tweet contains
        /// </summary>
        ICollection<IHashTag> HashTags { get; set; }

    }
}
