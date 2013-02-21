using NetworkedPlanet.Brightstar.LinkedDataServer.Resources;

namespace NetworkedPlanet.Brightstar.LinkedDataServer.Handlers
{
    public class SparqlEndpointHandler
    {
        public SparqlEndpoint GetSparqlResult(string storeId, string sparqlQuery)
        {
            return new SparqlEndpoint
                       {
                           Store = storeId,
                           SparqlQuery = sparqlQuery
                       };
        }
    }
}