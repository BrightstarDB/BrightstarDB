using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    interface IPageStorePrefixManager : IPrefixManager, IPageStoreObject
    {
    }
}
