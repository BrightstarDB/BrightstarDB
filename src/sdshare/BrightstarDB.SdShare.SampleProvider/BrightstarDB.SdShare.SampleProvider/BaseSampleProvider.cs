using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.SampleProvider
{
    public abstract class BaseSampleProvider : ICollectionProvider
    {
        #region Implementation of ICollectionProvider

        private string _rawConfig;

        public void Initialize(XElement configRoot)
        {
            _rawConfig = configRoot.ToString();
        }

        public abstract IEnumerable<ISnapshot> GetSnapshots();
        public abstract IEnumerable<IFragment> GetFragments(DateTime since, DateTime before);
        public abstract Stream GetFragment(string id, string mimeType);
        public abstract Stream GetSnapshot(string id, string mimeType);

        public Stream GetSample(string mimeType)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { return "Base Sample Provider Default Name - Suggest Overriding"; }
        }

        public string Identity
        {
            get { return "http://www.brightstardb.com/please-override-me-in-provider"; }
        }

        public string Description
        {
            get { return "Base Sample Provider Default Description - Suggest Overriding"; }
        }

        public string RawConfig
        {
            get { return _rawConfig; }
            set { _rawConfig = value; }
        }

        public bool IsEnabled { get; set; }

        #endregion
    }
}
