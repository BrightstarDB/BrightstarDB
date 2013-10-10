using System.Collections.Generic;

namespace BrightstarDB.Server.Modules.Model
{
    public class PagedResultModel<T>
    {
        public PagedResultModel(string linkFirst, string linkPrev, string linkNext, List<T> returnItems)
        {
            FirstPageLink = linkFirst;
            PreviousPageLink = linkPrev;
            NextPageLink = linkNext;
            Items = returnItems;
        }

        public string FirstPageLink { get; set; }
        public string PreviousPageLink { get; set; }
        public string NextPageLink { get; set; }

        public List<T> Items { get; set; } 
    }
}
