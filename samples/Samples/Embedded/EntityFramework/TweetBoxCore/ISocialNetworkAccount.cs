using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.TweetBoxCore
{
    /// <summary>
    /// This interface represents authorization between the twitter account and some other social networking site
    /// </summary>
    [Entity]
    public interface ISocialNetworkAccount
    {
        /// <summary>
        /// Id of the account
        /// </summary>
        /// <remarks>This property is set by Brightstar, and so is a required property</remarks>
        string Id { get; }

        /// <summary>
        /// The account name
        /// </summary>
        string AccountName { get; set; }

        /// <summary>
        /// The authorization key for cross posting between twitter and the other account
        /// </summary>
        string AuthorizationKey { get; set; }

        /// <summary>
        /// The twiiter account that this is linked to
        /// </summary>
        [InverseProperty("SocialNetworkAccount")]
        IUser TwitterAccount { get; set; }
    }
}
