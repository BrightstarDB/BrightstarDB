using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public static class NegotiatorExtensions
    {
        public static Negotiator WithPagedList<T>(this Negotiator negotiator, IEnumerable<T> items, int skip, int take, int defaultPageSize, string resourceUri)
        {
            var links = new List<string>();
            var queryParamsSeparator = resourceUri.Contains("?") ? "&" : "?";

            var fullList = items.ToList();
            var returnList = fullList.Count > take ? fullList.Take(take).ToList() : fullList;
            var negotiate = negotiator.WithModel(returnList);
            if (skip > 0)
            {
                links.Add(String.Format("<{0}>;rel=first", resourceUri));
                var prevPage = skip - defaultPageSize;
                if (prevPage <= 0)
                {
                    links.Add(String.Format("<{0}>;rel=prev", resourceUri));
                }
                else
                {
                    links.Add(String.Format("<{0}{1}skip={2}>;rel=prev", 
                        resourceUri, queryParamsSeparator, prevPage));
                }
            }
            if (fullList.Count > take)
            {
                links.Add(String.Format("<{0}{1}skip={2}>;rel=next",
                        resourceUri, queryParamsSeparator, skip + take));
            }
            if (links.Count > 0)
            {
                return negotiate.WithModel(returnList).WithHeader("Link", String.Join(",", links));
            }
            return negotiate.WithModel(returnList);
        }
    }
}