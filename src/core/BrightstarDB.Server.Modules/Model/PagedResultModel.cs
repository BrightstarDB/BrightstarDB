using System;
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
        public bool HasFirstPageLink { get { return !String.IsNullOrEmpty(FirstPageLink); } }
        public string PreviousPageLink { get; set; }
        public bool HasPreviousPageLink { get { return !String.IsNullOrEmpty(PreviousPageLink); } }
        public string NextPageLink { get; set; }
        public bool HasNextPageLink { get { return !String.IsNullOrEmpty(NextPageLink); } }
        public dynamic RequestProperties { get; set; }
        public List<T> Items { get; set; } 
    }
}
