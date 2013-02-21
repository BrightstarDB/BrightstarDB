using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace BrightstarDB.SdShare
{
    public abstract class CollectionProviderBase : ICollectionProvider
    {
        public abstract void Initialize(XElement configRoot);
        public string Name { get; set; }
        public string Identity { get; set; }
        public string Description { get; set; }
        public abstract IEnumerable<ISnapshot> GetSnapshots();
        public abstract IEnumerable<IFragment> GetFragments(DateTime since, DateTime before);
        public abstract Stream GetFragment(string id, string mimeType);
        public abstract Stream GetSnapshot(string id, string mimeType);
        public string RawConfig { get; set; }
        public bool IsEnabled { get; set; }
        public abstract Stream GetSample(string mimeType);
    }
}
