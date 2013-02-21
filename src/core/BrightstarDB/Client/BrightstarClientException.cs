using System;
using BrightstarDB.Rdf;
using SmartAssembly.Attributes;
#if !SILVERLIGHT
using System.ServiceModel;

#endif

namespace BrightstarDB.Client
{
#if !SILVERLIGHT
    /// <summary>
    /// Base class for client errors reported from Brightstar.
    /// </summary>
    [Serializable]
#endif
    [DoNotObfuscate]
    public sealed class BrightstarClientException : BrightstarException
    {
        private const string DefaultServiceErrorMessage = "Error executing client call.";

        /// <summary>
        /// Gets the detailed description of the exception that caused this client exception to be raised.
        /// </summary>
        public new ExceptionDetail InnerException { get; private set; }

#if !SILVERLIGHT
        internal BrightstarClientException(FaultException<ExceptionDetail> fault) 
            : base(fault.Detail!=null && fault.Detail.Message != null ? fault.Detail.Message : DefaultServiceErrorMessage)
        {
            InnerException = fault.Detail == null ? null : fault.Detail.InnerException;
        }
#endif

        internal BrightstarClientException(string message) : base(message){}

        internal BrightstarClientException(string message, ExceptionDetail detail) : base(message)
        {
            InnerException = detail;
        }

        internal BrightstarClientException(string message, RdfParserException parserException) : base(
            parserException.HaveLineNumber ?String.Format( "{0} Line {1}: {2}", message, parserException.LineNumber, parserException.Message):
            String.Format("{0} {1}", message, parserException.Message), parserException)
        {
            InnerException = new ExceptionDetail(parserException);
        }

        internal BrightstarClientException(string message, Exception inner) : base(message, inner)
        {
            InnerException = new ExceptionDetail(inner);
        }
    }
}
