using System;
using System.Xml;
using Nancy;
using Nancy.Bootstrapper;

namespace BrightstarDB.Server.Modules.Authentication
{
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Receives the configuration for the authentication provider that is specified in the applciation configuration file.
        /// </summary>
        /// <param name="configurationElement">The configuration element for the authentication provider. This will be a reference
        /// to the &lt;add/&gt; element inside the authenticationProviders element in the application configuration file.</param>
        void Configure(XmlElement configurationElement);

        /// <summary>
        /// Method invoked to have this authentication provider hook itself to the Nancy application pipelines
        /// </summary>
        /// <param name="pipelines">Pipelines to add handlers to (usually "this")</param>
        void Enable(IPipelines pipelines);
    }
}
