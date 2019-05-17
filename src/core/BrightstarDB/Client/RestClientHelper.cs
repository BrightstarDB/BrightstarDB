using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Provides helper methods for generation and handling B* REST API requests
    /// </summary>
    public static class RestClientHelper
    {
        /// <summary>
        /// Extracts and parses the Authorization header from a B* REST API request
        /// </summary>
        /// <param name="request">The incoming request</param>
        /// <param name="authType">The authentication type specified in the request</param>
        /// <param name="account">The account ID specified in the request</param>
        /// <param name="signature">The request signature</param>
        /// <returns>True if the request has an Authorization header that was parsed successfully, false otherwise.</returns>
        public static bool GetAuthorizationInfo(WebRequest request, out SignatureType authType, out string account, out string signature)
        {
            authType = SignatureType.Unknown;
            string authHeader = request.Headers["Authorization"];
            if (!String.IsNullOrEmpty(authHeader))
            {
                var tokens = authHeader.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 2)
                {
                    if (tokens[0].Equals("SharedKey"))
                    {
                        authType = SignatureType.SharedKey;
                    }
                    if (tokens[0].Equals("PlainText"))
                    {
                        authType = SignatureType.PlainText;
                    }
                    if (authType != SignatureType.Unknown)
                    {
                        var keyAndSig = tokens[1].Split(':');
                        if (keyAndSig.Length == 2)
                        {
                            account = keyAndSig[0];
                            signature = keyAndSig[1];
                            return true;
                        }
                    }
                }
            }
            authType = SignatureType.Unknown;
            account = null;
            signature = null;
            return false;
        }

        /// <summary>
        /// Generates the authorization signatures for a B* REST API request
        /// </summary>
        /// <param name="request">The HTTP request object</param>
        /// <param name="signatureType">The type of signature to apply.</param>
        /// <param name="secret">The shared secret used to generate the signature</param>
        /// <returns></returns>
        public static string GenerateSignature(HttpWebRequest request, SignatureType signatureType, string secret)
        {
            if(signatureType == SignatureType.Unknown) throw new ArgumentException("Invalid signature type", "signatureType");
            if (signatureType == SignatureType.PlainText)
            {
                // Just passes back the shared secret key in plain text
                return secret;
            }
            var stringToSign = new StringBuilder();
            stringToSign.AppendLine(request.Method.ToUpperInvariant())
                .AppendLine(request.Headers[HttpRequestHeader.ContentEncoding])
                .AppendLine(request.Headers[HttpRequestHeader.ContentLanguage])
                .AppendLine(request.ContentLength >= 0 ? request.ContentLength.ToString(CultureInfo.InvariantCulture) : string.Empty)
                .AppendLine(request.Headers[HttpRequestHeader.ContentMd5])
                .AppendLine(request.Headers[HttpRequestHeader.ContentType])
                .AppendLine(request.Headers[HttpRequestHeader.Date])
                .AppendLine(request.Headers[HttpRequestHeader.IfModifiedSince])
                .AppendLine(request.Headers[HttpRequestHeader.IfMatch])
                .AppendLine(request.Headers[HttpRequestHeader.IfNoneMatch])
                .AppendLine(request.Headers[HttpRequestHeader.IfUnmodifiedSince])
                .AppendLine(request.Headers[HttpRequestHeader.Range])
                .Append(CanonicalizedResource(request.RequestUri));
            
            var key = Convert.FromBase64String(secret);

            var hmac = new HMACSHA256(key);
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign.ToString())));
            return signature;
        }

        /// <summary>
        /// Generates the authorization signatures for a B* REST API request
        /// </summary>
        /// <param name="request">The HTTP request object</param>
        /// <param name="signatureType">The type of signature to apply.</param>
        /// <param name="secret">The shared secret used to generate the signature</param>
        /// <returns></returns>
        public static string GenerateSignature(WebRequest request, SignatureType signatureType, string secret)
        {
            if (signatureType == SignatureType.Unknown) throw new ArgumentException("Invalid signature type", "signatureType");
            if (signatureType == SignatureType.PlainText)
            {
                return secret;
            }
            var stringToSign = new StringBuilder();
            stringToSign.AppendLine(request.Method.ToUpperInvariant())
                .AppendLine(request.Headers["Content-Encoding"])
                .AppendLine(request.Headers["Content-Language"])
                .AppendLine(request.Headers["Content-Length"])
                .AppendLine(request.Headers["Content-MD5"])
                .AppendLine(request.Headers["Content-Type"])
                .AppendLine(request.Headers["Date"])
                .AppendLine(request.Headers["If-Modified-Since"])
                .AppendLine(request.Headers["If-Match"])
                .AppendLine(request.Headers["If-None-Match"])
                .AppendLine(request.Headers["If-Unmodified-Since"])
                .AppendLine(request.Headers["Range"])
                .Append(CanonicalizedResource(request.RequestUri));
            var key = Convert.FromBase64String(secret);
            var hmac = new HMACSHA256(key);
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign.ToString())));
            return signature;
        }

        /// <summary>
        /// Constructs the canonicalized resource string for B* REST API requests
        /// </summary>
        /// <param name="requestUri">The URI to be canonicalized</param>
        /// <returns>The canonicalized resource string for the input request URI</returns>
        public static string CanonicalizedResource(Uri requestUri)
        {
            var resourceString = new StringBuilder();
            resourceString.Append(requestUri.AbsolutePath);

            if (!String.IsNullOrEmpty(requestUri.Query))
            {
                var queryParams = new Dictionary<string, List<string>>();
                foreach (var nvpair in requestUri.Query.Split('&'))
                {
                    var tmp = nvpair.Split('=');
                    var k = tmp[0].ToLowerInvariant();
                    List<string> values;
                    if (queryParams.TryGetValue(k, out values))
                    {
                        values.Add(tmp[1]);
                    }
                    else
                    {
                        queryParams[k] = new List<string> {tmp[1]};
                    }
                }
                foreach (var k in queryParams.Keys.OrderBy(k => k))
                {
                    resourceString.Append(k);
                    resourceString.Append(':');
                    var values = queryParams[k].OrderBy(v => v).ToList();
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (i > 0) resourceString.Append(',');
                        resourceString.Append(values[i]);
                    }
                    resourceString.Append('\n');
                }
            }
            return resourceString.ToString();
        }
    }
}
