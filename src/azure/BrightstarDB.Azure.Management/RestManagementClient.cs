using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using BrightstarDB.Client;

namespace BrightstarDB.Azure.Management
{
    public class RestManagementClient : IManagementServices
    {
        private Uri _serviceEndpoint;
        private string _accountId;
        private string _key;

        private const string JsonContentType = "application/json";
        private const string XmlContentType = "application/xml";
        private const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";

        public RestManagementClient(Uri serviceEndpoint, string accountId, string accessKey)
        {
            _serviceEndpoint = serviceEndpoint;
            _accountId = accountId;
            _key = accessKey;
        }

        #region Implementation of IManagementServices

        public SubscriptionDetails CreateSubscription(string accountId, SubscriptionDetails subscriptionDetails)
        {
            var postParams = new Dictionary<string, string>
                                 {
                                     {"accountId", accountId},
                                     {"details", Serialize(subscriptionDetails)}
                                 };
            var response = AuthenticatedPost("subscriptions/", postParams);
            return Deserialize<SubscriptionDetails>(response);
        }

        public void UpdateSubscription(string subscriptionId, SubscriptionDetails updatedDetails)
        {
            var postParams = new Dictionary<string, string>
                                 {
                                     {"details", Serialize(updatedDetails)}
                                 };
            AuthenticatedPost("subscriptions/" + subscriptionId, postParams);
        }

        public void DeleteSubscription(string subscriptionId)
        {
            throw new NotImplementedException();
        }

        #endregion

        private HttpWebResponse AuthenticatedGet(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var getRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            getRequest.ContentType = JsonContentType;
            getRequest.Date = DateTime.UtcNow;
            SignRequest(getRequest);
            var ret = getRequest.GetResponse() as HttpWebResponse;
            return ret;
        }

        private HttpWebResponse AuthenticatedHead(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var headRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            headRequest.Method = "HEAD";
            headRequest.Date = DateTime.UtcNow;
            SignRequest(headRequest);
            return headRequest.GetResponse() as HttpWebResponse;
        }

        private HttpWebResponse AuthenticatedPost(string relativePath, Dictionary<string, string> postBodyParameters)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
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
            var md5 = System.Security.Cryptography.MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
            postRequest.Headers[HttpRequestHeader.ContentMd5] = hashCode;

            SignRequest(postRequest);
            try
            {
                var ret = postRequest.GetResponse() as HttpWebResponse;
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

        private HttpWebResponse AuthenticatedDelete(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var deleteRequest = WebRequest.Create(uri) as HttpWebRequest;
            SignRequest(deleteRequest);
            deleteRequest.Method = "DELETE";
            var response = deleteRequest.GetResponse() as HttpWebResponse;
            return response;
        }
        /// <summary>
        /// Adds the required authentication and timestamp headers to the request
        /// </summary>
        /// <param name="request"></param>
        private void SignRequest(HttpWebRequest request)
        {
            request.Headers.Add(HttpRequestHeader.Authorization,
                "SharedKey " + _accountId + ":" + RestClientHelper.GenerateSignature(request, SignatureType.SharedKey, _key));
        }


        private T Deserialize<T>(HttpWebResponse response)
        {
            string jsonString;
            using (var rdr = new StreamReader(response.GetResponseStream()))
            {
                jsonString = rdr.ReadToEnd();
            }
            var ser = new JavaScriptSerializer();
            return ser.Deserialize<T>(jsonString);
        }

        private string Serialize<T>(T toSerialize)
        {
            var ser = new JavaScriptSerializer();
            return ser.Serialize(toSerialize);
        }
    }
}
