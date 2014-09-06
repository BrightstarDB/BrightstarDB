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
                var processingModel = model as SparqlQueryProcessingModel;
                if (processingModel.SparqlRequest.Format != null)
                {
                    var sparqlFormat =
                        processingModel.SparqlRequest.Format.Select(SparqlResultsFormat.GetResultsFormat)
                                       .FirstOrDefault();
                    var graphFormat =
                        processingModel.SparqlRequest.Format.Select(RdfFormat.GetResultsFormat)
                                       .FirstOrDefault();
                    processingModel.OverrideResultsFormat = sparqlFormat;
                    processingModel.OverrideGraphFormat = graphFormat;
                    if (sparqlFormat != null || graphFormat != null)
                    {
                        return new ProcessorMatch
                            {
                                ModelResult = MatchResult.ExactMatch,
                                RequestedContentTypeResult = MatchResult.ExactMatch
                            };
                    }
                }

                if ((processingModel.ResultModel == SerializableModel.SparqlResultSet) &&
                    (SparqlResultsFormat.AllMediaTypes.Any(m => requestedMediaRange.Matches(MediaRange.FromString(m)))))
                {
                    return new ProcessorMatch
                        {
                            ModelResult = MatchResult.ExactMatch,
                            RequestedContentTypeResult = MatchResult.ExactMatch
                        };
                }
                if ((processingModel.ResultModel == SerializableModel.RdfGraph) &&
                    (RdfFormat.AllMediaTypes.Any(m => requestedMediaRange.Matches(MediaRange.FromString(m)))))
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
            var queryModel = model as SparqlQueryProcessingModel;
            var format = (queryModel.OverrideResultsFormat ?? 
                SparqlResultsFormat.AllFormats.FirstOrDefault(f => f.MediaTypes.Any(m => requestedMediaRange.Matches(m)))) ?? 
                SparqlResultsFormat.Xml;
            var graphFormat =
                (queryModel.OverrideGraphFormat ??
                RdfFormat.AllFormats.FirstOrDefault(f => f.MediaTypes.Any(m => requestedMediaRange.Matches(m)))) ??
                RdfFormat.RdfXml;
            
            return new SparqlQueryResponse(queryModel, context.Request.Headers.IfModifiedSince, format, graphFormat);
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings { get { return SparqlExtensionMappings; } }
    }
}