using System.Web.Mvc;
using System.Web.Routing;

namespace BrightstarDB.Azure.Gateway
{
    public static class HtmlHelperExtensions
    {
        public static string RouteUrl(this HtmlHelper htmlHelper, string routeName, RouteValueDictionary routeValues)
        {
            string url = UrlHelper.GenerateUrl(routeName, null, null, routeValues, htmlHelper.RouteCollection,
                                               htmlHelper.ViewContext.RequestContext, false);
            return url + "/";
        }

        public static string RouteUrl(this HtmlHelper htmlHelper, string routeName, object routeValues)
        {
            return htmlHelper.RouteUrl(routeName, new RouteValueDictionary(routeValues));
        }
    }

    
}