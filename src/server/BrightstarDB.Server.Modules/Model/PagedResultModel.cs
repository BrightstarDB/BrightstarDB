using System.Collections.Generic;

namespace BrightstarDB.Server.Modules.Model
{
    public class PagedResultModel<T> : IPagedResultModel
    {
        public PagedResultModel(string linkFirst, string linkPrev, string linkNext, List<T> returnItems, dynamic requestProperties)
        {
            FirstPageLink = linkFirst;
            PreviousPageLink = linkPrev;
            NextPageLink = linkNext;
            Items = returnItems;
            RequestProperties = requestProperties;
        }

        public string FirstPageLink { get; set; }
        public string PreviousPageLink { get; set; }
        public string NextPageLink { get; set; }
        public dynamic RequestProperties { get; set; }
        public List<T> Items { get; set; } 
    }
}
