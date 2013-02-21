using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.SdShare;

namespace BrightstarDB.SdShare.SampleProvider
{

    /*
  The .dll file from this project should be placed in the bin folder where SdShare is running.
  The following configuration should be added to the sdshare.config file.    
      
  <CollectionProvider name="Sample"
                      identifier="http://psi.brightstardb.com/feeds/sample"
                      Assembly="BrightstarDB.SdShare.SampleProvider"
                      Type="BrightstarDB.SdShare.SampleProvider.SampleProvider">
  </CollectionProvider> 
     
     */

    public class SampleProvider : BaseSampleProvider
    {
        #region Implementation of ICollectionProvider

        public void Initialize(XElement configRoot)
        {
            // Set Name, Identity, and Description in here

            // call base class to look after rawconfig
            base.Initialize(configRoot);
        }

        public override IEnumerable<ISnapshot> GetSnapshots()
        {
            // make use of the snapshot class to create this collection
            var snapShot = new Snapshot();
            snapShot.Id = "everything"; // this is the id returned to you in GetSnapshot.
            snapShot.Name = "All data we have"; // the name of the snapshot
            snapShot.PublishedDate = DateTime.UtcNow; // the time the snapshot was created, use now for most recent one.
            return new List<ISnapshot> {snapShot};
        }

        public override IEnumerable<IFragment> GetFragments(DateTime since, DateTime before)
        {
            // make use of the fragment class to create this collection
            var fragment = new Fragment();
            fragment.PublishDate = DateTime.UtcNow; // the date the entity was updated
            fragment.ResourceId = "http://www.brightstardb.com/sample/person/1"; // the value to be returned in GetFragment id
            fragment.ResourceUri = "http://www.brightstardb.com/sample/person/1"; // the value to of the ResourceUri in the fragment ATOM. Usually the same as ResourceId
            fragment.ResourceName = "The Fragment Name"; // the name to updated entity
            return new List<IFragment> {fragment};
        }

        public override Stream GetFragment(string id, string mimeType)
        {
            // mimeType comes through as 'nt' or 'xml'
            // use of dotNetRdf can be useful here.
            throw new NotImplementedException();
        }

        public override Stream GetSnapshot(string id, string mimeType)
        {
            // mimeType comes through as 'nt' or 'xml'
            throw new NotImplementedException();
        }

        #endregion
    }
}
