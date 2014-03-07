using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Nancy;
using Nancy.Bootstrapper;

namespace BrightstarDB.Server.Modules.Authentication
{
    /// <summary>
    /// A pass-through implementation of the IAuthenticationProvider interface
    /// </summary>
    public class NullAuthenticationProvider : IAuthenticationProvider
    {
        public void Configure(XmlElement configurationElement)
        {
            // Nothing to configure
        }

        public void Enable(IPipelines pipelines)
        {
            // Nothing to register
        }
    }
}
