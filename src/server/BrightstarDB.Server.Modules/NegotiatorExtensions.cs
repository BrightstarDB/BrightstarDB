using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Server.Modules.Model;
using Nancy;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public static class NegotiatorExtensions
    {
        /// <summary>
        /// Extends the content negotiation to render a page in a longer set of results
        /// </summary>
        /// <typeparam name="T">The type of item to be returned in the page</typeparam>
        /// <param name="negotiator">The content negotiator object</param>
        /// <param name="items">The full set of items from which the page is to be selected</param>
        /// <param name="skip">Offset into the list for the start of the page</param>
        /// <param name="take">The number of items to include on the page</param>
        /// <param name="defaultPageSize">The default page size, used for generating the extents of the previous and next pages</param>
        /// <param name="resourceUri">The base URI that generates the list of items</param>
        /// <returns>The updated negotiator</returns>
        /// <remarks>
        /// <para>The negotiator will be configured with a return value for text/html and for application/json.</para>
        /// <para>For text/html the response model will be an instance of <see cref="PagedResultModel{T}"/>, with the link URIs included in the model,
        /// the response view name will be set to Paged{NameOfItemModel} with the word "Model" stripped from the end.</para>
        /// <para>For application/json, the response model will simply be a <see cref="List{T}"/>.</para>
        /// <para>In either case the HTTP Links header will be populated with first, prev and next links as appropriate. No
        /// first or prev link is generated if the request is for the first page. No next link is generated if the page includes
        /// the final item in the list.</para>
        /// </remarks>
        public static Negotiator WithPagedList<T>(this Negotiator negotiator, dynamic requestObject, IEnumerable<T> items, int skip, int take, int defaultPageSize, string resourceUri)
        {
            var links = new List<string>();
            var queryParamsSeparator = resourceUri.Contains("?") ? "&" : "?";

            var fullList = items.ToList();
            var returnList = fullList.Count > take ? fullList.Take(take).ToList() : fullList;
            var negotiate = negotiator.WithModel(returnList);
            string first = null, prev = null, next = null;

            if (skip > 0)
            {
                first = resourceUri;
                var prevPage = skip - defaultPageSize;
                prev = prevPage <= 0 ? resourceUri : String.Format("{0}{1}skip={2}", resourceUri, queryParamsSeparator, prevPage);
            }
            if (fullList.Count > take)
            {
                next = String.Format("{0}{1}skip={2}", resourceUri, queryParamsSeparator, skip + take);
            }

            if (first != null) links.Add(MakeLink(first, "first"));
            if (prev != null) links.Add(MakeLink(prev, "prev"));
            if (next != null) links.Add(MakeLink(next, "next"));

            if (links.Count > 0)
            {
                return negotiate.WithModel(returnList).WithHeader("Link", String.Join(",", links));
            }


            var pagedView = "Paged" + typeof (T).Name;
            if (pagedView.EndsWith("Model"))
            {
                pagedView = pagedView.Substring(0, pagedView.Length - 5);
            }
            return negotiate.WithMediaRangeModel(MediaRange.FromString("text/html"),
                new PagedResultModel<T>(first, prev, next, returnList, requestObject))
                                          .WithView(pagedView)
                     .WithMediaRangeModel("application/json", returnList);
        }

        private static string MakeLink(string uri, string rel)
        {
            return String.Format("<{0}>;rel={1}", uri, rel);
        }
    }
}