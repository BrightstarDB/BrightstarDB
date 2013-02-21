using System;

namespace BrightstarDB.Polaris.ViewModel
{
    internal class SparqlQueryException : Exception
    {
        public SparqlQueryException(Exception innerException) : base("An error occurred while executing the SPARQL query.", innerException)
        {
        }
    }
}