using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using Nancy;

namespace BrightstarDB.Server.Modules.Tests
{
    public class FakeNancyBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IBrightstarService _brightstarService;

        public FakeNancyBootstrapper(IBrightstarService brightstarService) : base()
        {
            _brightstarService = brightstarService;
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register<IBrightstarService>(_brightstarService);
        }
    }
}
