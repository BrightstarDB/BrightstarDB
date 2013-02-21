using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BrightstarDB.Azure.AdminCommandLine
{
    /// <summary>
    /// Provides helper methods for generation and handling B* REST API requests
    /// </summary>
    public class RestClient
    {
        private Uri _serviceEndpoint;
        private string _accountId;
        private string _authKey;
        private const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";
        private const string JsonContentType = "application/json";

        /// <summary>
        /// Returns the timestamp provided by the server on its last response.
        /// </summary>
        /// <remarks>This property will be null if no operation has been invoked, or 
        /// if the client is an embedded client.</remarks>
        public DateTime? LastResponseTimestamp { get; private set; }


        public RestClient()
        {
            _serviceEndpoint = new Uri(Settings.Default.Endpoint);
            _accountId = Settings.Default.SuperUserAccount;
            _authKey = Settings.Default.SuperUserKey;
#if DEBUG
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
#endif
        }

        public HttpWebResponse AuthenticatedPost(string relativePath, Dictionary<string, string> postBodyParameters)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            Console.WriteLine("POST request to " + uri);
            var postRequest = WebRequest.Create(uri) as HttpWebRequest;
            postRequest.Method = "POST";
            postRequest.Date = DateTime.UtcNow;
            postRequest.ContentType = UrlEncodedFormContentType;
            var contentBuilder = new StringBuilder();
            foreach (var bodyParam in postBodyParameters)
            {
                contentBuilder.AppendFormat("{0}={1}", EscapeDataString(bodyParam.Key),
                                            EscapeDataString(bodyParam.Value));
                contentBuilder.Append("&");
            }
            var content = contentBuilder.ToString().TrimEnd('&');
            using (var writer = new StreamWriter(postRequest.GetRequestStream()))
            {
                writer.Write(content);
            }
            var md5 = MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
            postRequest.Headers[HttpRequestHeader.ContentMd5] = hashCode;
            //postRequest.ContentLength = contentBytes.Length;

            SignRequest(postRequest);
            try
            {
                var ret = postRequest.GetResponse() as HttpWebResponse;
                if (ret != null)
                {
                    LastResponseTimestamp = DateTime.Now;
                }
                return ret;
            }
            catch (WebException wex)
            {
                string responseContent;
                if (wex.Response != null)
                {
                    using (var rdr = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        responseContent = rdr.ReadToEnd();
                    }
                }
                throw;
            }
        }

        public HttpWebResponse AuthenticatedGet(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            Console.WriteLine("GET: " + uri);
            var getRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            getRequest.ContentType = JsonContentType;
            getRequest.Date = DateTime.UtcNow;
            SignRequest(getRequest);
            var ret = getRequest.GetResponse() as HttpWebResponse;
            if (ret != null)
            {
                LastResponseTimestamp = DateTime.Now;
            }
            return ret;
        }

        private static readonly byte[] Mark = new byte[] { (byte)'-', (byte)'_', (byte)'.', (byte)'~' };

        private static String EscapeDataString(string value)
        {
            var escapeBuilder = new StringBuilder();
            var bytes = Encoding.UTF8.GetBytes(value);
            foreach (var octet in bytes)
            {

                if ((octet > 47 && octet < 58) ||
                    (octet > 64 && octet < 91) ||
                    (octet > 96 && octet < 123) ||
                    Mark.Contains(octet))
                {
                    escapeBuilder.Append((char)octet);
                }
                else
                {
                    escapeBuilder.AppendFormat("%{0:X2}", octet);
                }
            }
            return escapeBuilder.ToString();
        }

        /// <summary>
        /// Adds the required authentication and timestamp headers to the request
        /// </summary>
        /// <param name="request"></param>
        private void SignRequest(HttpWebRequest request)
        {
            request.Headers.Add(HttpRequestHeader.Authorization,
                "SharedKey " + _accountId + ":" + GenerateSignature(request, SignatureType.SharedKey, _authKey));
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
            if (signatureType == SignatureType.Unknown) throw new ArgumentException("Invalid signature type", "signatureType");
            if (signatureType == SignatureType.PlainText)
            {
                // Just passes back the shared secret key in plain text
                return secret;
            }
            var stringToSign = new StringBuilder();
            stringToSign.AppendLine(request.Method.ToUpperInvariant())
                .AppendLine(request.Headers[HttpRequestHeader.ContentEncoding])
                .AppendLine(request.Headers[HttpRequestHeader.ContentLanguage])
                .AppendLine(request.ContentLength >= 0 ? request.ContentLength.ToString(CultureInfo.InvariantCulture) : String.Empty)
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
                        queryParams[k] = new List<string> { tmp[1] };
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
