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

namespace BrightstarDB.SdShare
{
    public class Snapshot : ISnapshot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime PublishedDate { get; set; }   
    }
}
