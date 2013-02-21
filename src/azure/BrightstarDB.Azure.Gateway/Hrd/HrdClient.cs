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

using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using BrightstarDB.Azure.Gateway.Models;

namespace BrightstarDB.Azure.Gateway.Hrd
{
    /// <summary>
    /// Abstracts the acquisition of the HRD JSON Feed
    /// </summary>
    public class HrdClient
    {
        public HrdClient()
        {
        }

        public virtual IEnumerable<HrdIdentityProvider> GetHrdResponse(HrdRequest request)
        {
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;

            string response = client.DownloadString(request.GetUrlWithQueryString());

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Deserialize<List<HrdIdentityProvider>>(response);
        }
    }
}