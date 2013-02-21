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

using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;

namespace BrightstarDB.Azure.Gateway.Util
{
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Greates query string using the name value collection
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static string ToQueryString(this NameValueCollection collection)
        {
            return string.Join("&",
                from k in collection.AllKeys
                select string.Format(CultureInfo.InvariantCulture,
                    "{0}={1}",
                    k,
                    HttpUtility.UrlEncode(collection.GetValues(k)[0])));
        }
    }
}