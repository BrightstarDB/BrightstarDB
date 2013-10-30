#if !WINDOWS_PHONE
using System.Net;

namespace BrightstarDB.Client.RestSecurity
{
    /// <summary>
    /// A request authenticator that uses a shared secret key to sign outgoing requests
    /// </summary>
    public class SharedSecretAuthenticator : IRequestAuthenticator
    {
        private readonly string _accountId;
        private readonly string _authKey;

        /// <summary>
        /// Creates a new <see cref="SharedSecretAuthenticator"/> with a specified
        /// account ID and authentication key pair
        /// </summary>
        /// <param name="accountId">The ID of the account associated with the authentication key</param>
        /// <param name="authenticationKey">The authentication key</param>
        public SharedSecretAuthenticator(string accountId, string authenticationKey)
        {
            _accountId = accountId;
            _authKey = authenticationKey;
        }

        /// <summary>
        /// Invoked by the REST client framework to add authentication information to an outgoing request
        /// </summary>
        /// <param name="request">The request to be updated with authentication information</param>
        public void Authenticate(HttpWebRequest request)
        {
#if PORTABLE || WINDOWS_PHONE
            request.Headers[HttpRequestHeader.Authorization] =
                "SharedKey " + _accountId + ":" +
                RestClientHelper.GenerateSignature(request, SignatureType.SharedKey, _authKey);
#else
            request.Headers.Add(HttpRequestHeader.Authorization,
              "SharedKey " + _accountId + ":" + RestClientHelper.GenerateSignature(request, SignatureType.SharedKey, _authKey));
#endif
        }
    }
}
#endif