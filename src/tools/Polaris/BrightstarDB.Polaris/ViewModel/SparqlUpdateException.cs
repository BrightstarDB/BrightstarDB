using System;
using BrightstarDB.Dto;

namespace BrightstarDB.Polaris.ViewModel
{
    internal class SparqlUpdateException : Exception
    {
        public SparqlUpdateException(ExceptionDetailObject innerDetail)
            : base("An error occurred while executing the SPARQL update: " + innerDetail.Message) { }
        public SparqlUpdateException(string msg)
            : base("An error occurred while executing the SPARQL update: " + msg) { }
        public SparqlUpdateException(Exception innerException)
            : base("An error occurred while executing the SPARQL update.", innerException) { }
    }
}