using System.Web.Hosting;
using Nancy;

namespace BrightstarDB.Server.AspNet
{
    public class AspNetRootSourceProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            return HostingEnvironment.MapPath("~/");
        }
    }
}