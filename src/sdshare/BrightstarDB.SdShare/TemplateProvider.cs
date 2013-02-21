using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace BrightstarDB.SdShare
{
    public class TemplateProvider : CollectionProviderBase
    {

        private BaseDataSourceManager _dsm;

        public override void Initialize(XElement configRoot)
        {
            // do normal init

            // create hash provider and register
           _dsm = new DataSourceManagerTemplate("a unique but consistent name - probably taken from config");
            const int waitPeriod = 3000; // ms delay between checks 
            EntityChangeManager.Instance.AddJob(_dsm, _dsm.ProcessDataSource, 0, waitPeriod);
        }

        public override IEnumerable<IFragment> GetFragments(DateTime since, DateTime before)
        {
            // use the dsm to get fragments
            var updates = _dsm.ListLastUpdated(since);

            // create fragments 
            var seenupdated = new Dictionary<string, string>(); // use this to prevent repeats in th feed.
            foreach (var u in updates)
            {
                if (seenupdated.ContainsKey(u.Id)) continue;
                seenupdated.Add(u.Id, u.Id);
                var url = "http://www.example.no/sdshare/system/" + u.Id;
                yield return new Fragment { PublishDate = DateTime.UtcNow, ResourceId = url, ResourceUri = url, ResourceName = u.Id };
            }

            // any other fragments from this provider...

        }

        public override IEnumerable<ISnapshot> GetSnapshots()
        {
            throw new NotImplementedException();
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

    public class DataSourceManagerTemplate : BaseDataSourceManager
    {
        public DataSourceManagerTemplate(string hashValueFileName)
            : base(hashValueFileName)
        {
        }

        public override IEnumerable<EntityInfo> GetEntityInfos()
        {
            throw new NotImplementedException();
        }
    }
}
