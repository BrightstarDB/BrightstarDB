using System;
using System.Collections.Generic;

namespace BrightstarDB.SdShare
{
    /// <summary>
    /// This class defines how a resource is exposed over sdshare
    /// </summary>
    public class ResourcePublishingDefinition
    {
        public ResourcePublishingDefinition()
        {
            FragmentGenerationDefinitions = new List<FragmentGenerationDefinition>();
        }

        // a manager for getting the last updated times of data with no date stamp
        public DataSourceManager DataSourceManager { get; set; }
        public bool EncodeIdForResourceId { get; set; }
        public Uri ResourcePrefix { get; set; }
        public UriTemplate UriTemplate { get; set; }
        public string FragmentsQuery { get; set; }
        // if this is set then we will check against a value hash.
        public bool NoTimeStampInData { get; set; }
        public string EntityIdColumn { get; set; }

        // todo: this should be hash value query.
        public string HashValueTable { get; set; }
        // Axel was here
        public string HashValueFileName { get; set; }
        public List<string> HashValueKeyColumns { get; set; }
        public string ValueCheckInterval { get; set; }
        public List<FragmentGenerationDefinition> FragmentGenerationDefinitions { get; set; }
        public bool SuppressFragmentsFeed { get; set; }
        public bool SourceDataInLocalTime { get; set; }
    }
}
