using System;
using BrightstarDB.Dto;
using BrightstarDB.Rdf;
#if !SILVERLIGHT
using System.ServiceModel;

#endif

namespace BrightstarDB.Client
{
#if !SILVERLIGHT && !PORTABLE
    /// <summary>
    /// Base class for client errors reported from Brightstar.
    /// </summary>
    [Serializable]
#endif
    public class BrightstarClientException : BrightstarException
    {
        private const string DefaultServiceErrorMessage = "Error executing client call.";

        /// <summary>
        /// Gets the detailed description of the exception that caused this client exception to be raised.
        /// </summary>
        public new ExceptionDetailObject InnerException { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string InnerExceptionType { get; private set; }

        internal BrightstarClientException(string message) : base(message){}

        internal BrightstarClientException(string message, ExceptionDetailObject detail) : base(message)
        {
            InnerException = detail;
            InnerExceptionType = detail.Type;
        }

        internal BrightstarClientException(string message, RdfParserException parserException) : base(
            parserException.HaveLineNumber ?String.Format( "{0} Line {1}: {2}", message, parserException.LineNumber, parserException.Message):
            String.Format("{0} {1}", message, parserException.Message), parserException)
        {
            InnerException = new ExceptionDetailObject(parserException);
        }

        internal BrightstarClientException(string message, Exception inner) : base(message, inner)
        {
            InnerException = new ExceptionDetailObject(inner);
        }
    }
}
