// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

using System;
using System.Collections.Specialized;
using BrightstarDB.Azure.Gateway.Util;

namespace BrightstarDB.Azure.Gateway.Hrd
{
    /// <summary>
    /// Represents a home realm discovery request
    /// </summary>
    public class HrdRequest
    {
        public const string HrdPath = "v2/metadata/IdentityProviders.js";
        public const string Version = "1.0";

        public enum Protocol
        {
            wsfederation,
            javascriptnotify
        }

        private NameValueCollection parameters = new NameValueCollection();
        private UriBuilder uriBuilder;

        /// <summary>
        /// Constructs HrdRequest
        /// </summary>
        /// <param name="issuer">The issuer url</param>
        /// <param name="realm">Realm of the relying party</param>
        /// <param name="protocol">Relying party protocol</param>
        /// <param name="replyTo">Optional reply_to for the relying party</param>
        /// <param name="context">Optional context for the request</param>
        /// <param name="callback">Optional callback method. When specified the response will include java script to call this method.</param>
        public HrdRequest(string issuer,
            string realm,
            Protocol protocol = Protocol.wsfederation,
            string replyTo = null,
            string context = null,
            string callback = null)
        {
            uriBuilder = new UriBuilder(issuer);

            parameters["protocol"] = protocol.ToString();
            parameters["realm"] = realm;
            parameters["version"] = Version;

            AddParameterIfNotNull("reply_to", replyTo);
            AddParameterIfNotNull("context", context);
            AddParameterIfNotNull("callback", callback);
        }

        private void AddParameterIfNotNull(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                parameters[name] = value;
            }
        }

        /// <summary>
        /// Gets the url with its query string representing this request
        /// </summary>
        /// <returns></returns>
        public string GetUrlWithQueryString()
        {
            uriBuilder.Path = HrdPath;
            uriBuilder.Query = parameters.ToQueryString();

            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}