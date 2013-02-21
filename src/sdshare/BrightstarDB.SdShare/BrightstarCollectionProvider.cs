using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BrightstarDB.SdShare
{
    public class BrightstarCollectionProvider : CollectionProviderBase
    {
        private string _connectionString;
        private string _baseLocation;
        private string _storeName;

        public override void Initialize(XElement configRoot)
        {
            var connectionStringElem = configRoot.Descendants("ConnectionString").FirstOrDefault();
            if (connectionStringElem == null) throw new Exception("Missing 'ConnectionString' element.");
            _connectionString = connectionStringElem.Value;

            var locationStringElem = configRoot.Descendants("BaseLocation").FirstOrDefault();
            if (locationStringElem == null) throw new Exception("Missing 'BaseLocation' element.");
            _baseLocation = locationStringElem.Value;

        }

        public override IEnumerable<ISnapshot> GetSnapshots()
        {
            return new List<Snapshot> {new Snapshot {Id = "everything", Name = "Latest Data", PublishedDate = DateTime.UtcNow} };
        }

        public override IEnumerable<IFragment> GetFragments(DateTime since, DateTime before)
        {
            // get all txns since date
            // var client = BrightstarService.GetClient(_connectionString);
            // client.
            return null;
        }

        public override Stream GetFragment(string id, string mimeType)
        {
            throw new NotImplementedException();
        }

        public override Stream GetSnapshot(string id, string mimeType)
        {
            throw new NotImplementedException();
        }

        public override Stream GetSample(string mimeType)
        {
            throw new NotImplementedException();
        }
    }
}
