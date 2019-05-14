using System;
using BrightstarDB.Rdf;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Base class for client errors reported from Brightstar.
    /// </summary>
    public class BrightstarClientException : BrightstarException
    {

        internal BrightstarClientException(string message) : base(message){}

        internal BrightstarClientException(string message, RdfParserException parserException) : base(
            parserException.HaveLineNumber ?String.Format( "{0} Line {1}: {2}", message, parserException.LineNumber, parserException.Message):
            String.Format("{0} {1}", message, parserException.Message), parserException)
        {
            
        }

        internal BrightstarClientException(string message, Exception inner) : base(message, inner)
        {
            
        }
    }
}
