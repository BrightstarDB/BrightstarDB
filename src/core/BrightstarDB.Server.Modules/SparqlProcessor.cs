using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class SparqlProcessor : IResponseProcessor
    {
        private static readonly List<Tuple<string, MediaRange>> SparqlExtensionMappings =
            SparqlResultsFormat.AllFormats.SelectMany(
                f =>
                f.MediaTypes.Select(m => new Tuple<string, MediaRange>(f.DefaultExtension, m)))
                               .ToList();

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (model is SparqlQueryProcessingModel)
            {
                if (SparqlResultsFormat.AllMediaTypes.Any(m => requestedMediaRange.Matches(m)))
                {
                    return new ProcessorMatch
                        {
                            ModelResult = MatchResult.ExactMatch,
                            RequestedContentTypeResult = MatchResult.ExactMatch
                        };
                }
                return new ProcessorMatch
                    {
                        ModelResult = MatchResult.ExactMatch,
                        RequestedContentTypeResult = MatchResult.NoMatch
                    };
            }
            return new ProcessorMatch
                {
                    ModelResult = MatchResult.NoMatch,
                    RequestedContentTypeResult = MatchResult.DontCare
                };
        }

        public Response Process(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            var format =
                SparqlResultsFormat.AllFormats.FirstOrDefault(f => f.MediaTypes.Any(m => requestedMediaRange.Matches(m)));
            var queryModel = model as SparqlQueryProcessingModel;
            return new SparqlQueryResponse(queryModel, context.Request.Headers.IfModifiedSince, format);
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings { get { return SparqlExtensionMappings; } }
    }
}