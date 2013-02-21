using System;

namespace BrightstarDB.SdShare
{
    public class Fragment : IFragment
    {
        /// <summary>
        /// This is the uri inserted into the TopicSI / ResourceUri property in the atom feed 
        /// </summary>
        public string ResourceUri { get; set; }
        public string ResourceId { get; set; }
        public DateTime PublishDate { get; set; }
        public string ResourceName { get; set; }
    }
}
