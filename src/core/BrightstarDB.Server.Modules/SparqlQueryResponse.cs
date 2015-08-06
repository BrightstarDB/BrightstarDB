using System;
using BrightstarDB.Client;
using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class SparqlQueryResponse : Response
    {
        public SparqlQueryResponse(SparqlQueryProcessingModel model, DateTime? ifNotModifiedSince, SparqlResultsFormat format, RdfFormat graphFormat)
        {
            try
            {
                ISerializationFormat streamFormat;
                var resultStream = model.GetResultsStream(format, graphFormat, ifNotModifiedSince, out streamFormat);
                Contents = resultStream.CopyTo;
                ContentType = streamFormat.ToString();
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
            catch (NoSuchStoreException)
            {
                StatusCode = HttpStatusCode.NotFound;
            }
        }
    }
}
