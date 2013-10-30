using System;
using BrightstarDB.Server.Modules;
using Nancy.Hosting.Aspnet;

namespace BrightstarDB.Server.AspNet
{
    public class DefaultBootstrapper : BrightstarBootstrapper
    {
        protected override Nancy.IRootPathProvider RootPathProvider
        {
            get
            {
                return new AspNetRootSourceProvider();
            }
        }
    }
}