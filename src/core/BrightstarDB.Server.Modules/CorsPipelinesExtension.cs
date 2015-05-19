using System.Linq;
using BrightstarDB.Server.Modules.Configuration;
using Nancy;
using Nancy.Bootstrapper;

namespace BrightstarDB.Server.Modules
{
    public static class CorsPipelinesExtension
    {
        public static void EnableCors(this IPipelines pipelines, CorsConfiguration corsConfiguration)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                if (ctx.Request.Headers.Keys.Contains("Origin"))
                {
                    ctx.Response.WithHeader("Access-Control-Allow-Origin", corsConfiguration.AllowOrigin);
                    if (ctx.Request.Method.Equals("OPTIONS"))
                    {
                        ctx.Response
                            .WithHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS")
                            .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");
                    }
                }
            });
        }
    }
}
