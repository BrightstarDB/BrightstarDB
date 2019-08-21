using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.TweetBox
{
    /// <summary>
    /// This interface represents a twitter user
    /// </summary>
    [Entity]
    public interface IUser
    {
        /// <summary>
        /// Id of the user
        /// </summary>
        /// <remarks>This property is set by Brightstar, and so is a required property</remarks>
        string Id { get; }

        /// <summary>
        /// The twitter username of the user
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// A short string for biog text for the user's bio
        /// </summary>
        string Bio { get; set; }

        /// <summary>
        /// The date the user registered
        /// </summary>
        DateTime DateRegistered { get; set; }

        /// <summary>
        /// The collection of users that this user is following
        /// </summary>
        ICollection<IUser> Following { get; set; }

        /// <summary>
        /// The collection of users that are following this user
        /// </summary>
        [InverseProperty("Following")]
        ICollection<IUser> Followers { get; set; }

        /// <summary>
        /// The collection of short status updates (tweets) that this user has published
        /// </summary>
        [InverseProperty("Author")]
        ICollection<ITweet> Tweets { get; set; }

        /// <summary>
        /// Authorization details between the user and another social networking site
        /// </summary>
        ISocialNetworkAccount SocialNetworkAccount { get; set; }
        
    }
}
