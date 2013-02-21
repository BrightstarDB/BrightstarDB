using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    interface IPageStoreRelatedResourceIndex : IRelatedResourceIndex, IPageStoreObject
    {
        void FlushCache();
    }
}
