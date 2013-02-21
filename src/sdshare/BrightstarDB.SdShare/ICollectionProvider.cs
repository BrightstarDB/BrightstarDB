using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace BrightstarDB.SdShare
{
    public interface ICollectionProvider
    {
        void Initialize(XElement configRoot);
        string Name { get; }
        string Identity { get; }
        string Description { get; }
        IEnumerable<ISnapshot> GetSnapshots();
        IEnumerable<IFragment> GetFragments(DateTime since, DateTime before);
        Stream GetFragment(string id, string mimeType);
        Stream GetSnapshot(string id, string mimeType);
        string RawConfig { get; set; }
        bool IsEnabled { get; set; }
        Stream GetSample(string mimeType);
    }
}
