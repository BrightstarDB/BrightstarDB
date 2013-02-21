using System;
using NetworkedPlanet.Brightstar.LinkedDataServer.Codecs;
using NetworkedPlanet.Brightstar.LinkedDataServer.Handlers;
using NetworkedPlanet.Brightstar.LinkedDataServer.Resources;
using OpenRasta.Configuration;

namespace NetworkedPlanet.Brightstar.LinkedDataServer
{
    public class Configuration : IConfigurationSource
    {
        public void Configure()
        {
            using(OpenRastaConfiguration.Manual)
            {
                ResourceSpace.Has.ResourcesOfType<Home>()
                    .AtUri("/home")
                    .HandledBy<HomeHandler>();

                ResourceSpace.Has.ResourcesOfType<SparqlEndpoint>()
                    .AtUri("/{storeId}?query={sparqlQuery}")
                    .HandledBy<SparqlEndpointHandler>()
                    .TranscodedBy(typeof (SparqlResultCodec));
            }
        }
    }
}