using System.Net;

namespace BrightstarDB.Client.RestSecurity
{
    /// <summary>
    /// Interface for a module that adds authentication information to an outgoing HTTP request
    /// </summary>
    public interface IRequestAuthenticator
    {
        /// <summary>
        /// Invoked by the REST client framework to add authentication information to an outgoing request
        /// </summary>
        /// <param name="request">The request to be updated with authentication information</param>
        void Authenticate(HttpWebRequest request);
    }
}
