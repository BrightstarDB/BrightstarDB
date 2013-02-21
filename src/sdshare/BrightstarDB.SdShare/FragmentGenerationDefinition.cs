using System.Collections.Generic;

namespace BrightstarDB.SdShare
{
    public class FragmentGenerationDefinition
    {
        public string SnapshotQuery { get; set; }
        public string FragmentQuery { get; set; }
        public List<string> RdfTemplateLines { get; set; }
        public List<string> GenericTemplateExcludeColumns { get; set; }
    }
}