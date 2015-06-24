using System;
using System.Text;
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Message);
            var inner = InnerException;
            while (inner != null)
            {
                if (!string.IsNullOrEmpty(inner.Message))
                {
                    sb.AppendLine(inner.Message);
                }
                inner = inner.InnerException;
            }
            return sb.ToString();
        }
    }

}