using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.TweetBoxCore
{
    /// <summary>
    /// This interface represents a hashtag
    /// </summary>
    /// <remarks>A hashtag is a single word that is used in a tweet to denote a keyword or topic</remarks>
    [Entity]
    public interface IHashTag
    {
        /// <summary>
        /// Id of the hashtag
        /// </summary>
        /// <remarks>This property is set by BrightStar, and so is a required property</remarks>
        string Id { get; }

        /// <summary>
        /// The hashtag value
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// The collection of tweets that use the hashtag
        /// </summary>
        [InverseProperty("HashTags")]
        ICollection<ITweet> Tweets { get; set; } 
    }
}
