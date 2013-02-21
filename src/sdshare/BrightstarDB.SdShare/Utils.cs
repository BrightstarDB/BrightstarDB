/*
Copyright Networked Planet Ltd 2004-12
contact@networkedplanet.com

------------------------------------------------------------------------

This code is made available to Hafslund ASA to be used as seen fit on
internal projects only. 

Any changes made to this code should be reported to Networked Planet Ltd.  
  
This code is made available as is, in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  

------------------------------------------------------------------------
*/

using System;
using System.Linq;
using System.Net.Mime;

namespace BrightstarDB.SdShare
{
    public static class Utils
    {
        public static bool Matches(this ContentType self, ContentType other)
        {
            string[] selfParts = self.MediaType.Split('/', '+');
            string[] otherParts = other.MediaType.Split('/', '+');
            if ((selfParts[0].Equals("*") || selfParts[0].Equals(otherParts[0], StringComparison.InvariantCultureIgnoreCase)) &&
                (selfParts[1].Equals("*") || selfParts[1].Equals(otherParts[1], StringComparison.InvariantCultureIgnoreCase)) &&
                (selfParts.Length == 2 || ((otherParts.Length > 2) && (selfParts[2].Equals(otherParts[2], StringComparison.InvariantCultureIgnoreCase)))))
            {
                // Media type matches, now ensure that each parameter in self is matched in other
                foreach (var paramKey in self.Parameters.Keys.Cast<string>())
                {
                    if (!other.Parameters.ContainsKey(paramKey)) return false;
                    if (!other.Parameters[paramKey].Equals(self.Parameters[paramKey])) return false;
                }
                return true;
            }
            return false;
        }
    }
}
