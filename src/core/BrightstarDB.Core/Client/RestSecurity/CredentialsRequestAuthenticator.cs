using System.Net;

namespace BrightstarDB.Client.RestSecurity
{
    /// <summary>
    /// A <see cref="IRequestAuthenticator"/> that adds credentials to an outgoing request
    /// </summary>
    public class CredentialsRequestAuthenticator : IRequestAuthenticator
    {
        private readonly ICredentials _credentials;

        /// <summary>
        /// Creates a new instance of the <see cref="CredentialsRequestAuthenticator"/>
        /// that authenticates requests with a specific set of credentials
        /// </summary>
        /// <param name="credentials">The credentials to use to authorize outgoing requests</param>
        public CredentialsRequestAuthenticator(ICredentials credentials)
        {
            _credentials = credentials;
        }

        /// <summary>
        /// Invoked by the REST client framework to add authentication information to an outgoing request
        /// </summary>
        /// <param name="request">The request to be updated with authentication information</param>
        public void Authenticate(HttpWebRequest request)
        {
            request.Credentials = _credentials;
        }
    }
}
