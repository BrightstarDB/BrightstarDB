using System;
using BrightstarDB.Client;
using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class SparqlQueryResponse : Response
    {
        public SparqlQueryResponse(SparqlQueryProcessingModel model, DateTime? ifNotModifiedSince, SparqlResultsFormat format)
        {
            try
            {
                var resultStream = model.GetResultsStream(format, ifNotModifiedSince);
                Contents = resultStream.CopyTo;
                ContentType = format.MediaTypes[0];
                StatusCode = HttpStatusCode.OK;
            }
            catch (InvalidCommitPointException)
            {
                StatusCode = HttpStatusCode.NotFound;
            }
            catch (BrightstarStoreNotModifiedException)
            {
                StatusCode = HttpStatusCode.NotModified;
            }
        }
    }
}
