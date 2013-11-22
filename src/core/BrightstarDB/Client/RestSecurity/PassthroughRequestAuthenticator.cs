using System.Net;

namespace BrightstarDB.Client.RestSecurity
{
    /// <summary>
    /// A <see cref="IRequestAuthenticator"/> class that adds no authentication information to an outgoing request
    /// </summary>
    public class PassthroughRequestAuthenticator : IRequestAuthenticator
    {
        /// <summary>
        /// Invoked by the REST client framework to add authentication information to an outgoing request
        /// </summary>
        /// <param name="request">The request to be updated with authentication information</param>
        public void Authenticate(HttpWebRequest request)
        {
        }
    }
}
