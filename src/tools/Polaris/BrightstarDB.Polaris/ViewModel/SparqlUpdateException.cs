using System;
using System.ServiceModel;

namespace BrightstarDB.Polaris.ViewModel
{
    internal class SparqlUpdateException : Exception
    {
        public SparqlUpdateException(ExceptionDetail innerDetail)
            : base("An error occurred while executing the SPARQL update: " + innerDetail.Message) { }
        public SparqlUpdateException(string msg)
            : base("An error occurred while executing the SPARQL update: " + msg) { }
        public SparqlUpdateException(Exception innerException)
            : base("An error occurred while executing the SPARQL update.", innerException) { }
    }
}