using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Server.Modules.Model;
using Nancy;
using Nancy.Responses;
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
            var processingModel = model as SparqlQueryProcessingModel;
            if (processingModel != null)
            {
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
                    (SparqlResultsFormat.AllMediaTypes.Any(m => requestedMediaRange.Matches(new MediaRange(m)))))
                {
                    return new ProcessorMatch
                        {
                            ModelResult = MatchResult.ExactMatch,
                            RequestedContentTypeResult = MatchResult.ExactMatch
                        };
                }
                if ((processingModel.ResultModel == SerializableModel.RdfGraph) &&
                    (RdfFormat.AllMediaTypes.Any(m => requestedMediaRange.Matches(new MediaRange(m)))))
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
            else if (model is GraphListModel)
            {
                return new ProcessorMatch
                {
                    ModelResult = MatchResult.ExactMatch,
                    RequestedContentTypeResult =
                        SparqlResultsFormat.AllMediaTypes.Any(m => requestedMediaRange.Matches(new MediaRange(m)))
                            ? MatchResult.ExactMatch
                            : MatchResult.NoMatch
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
            var processingModel = model as SparqlQueryProcessingModel;
            if (processingModel != null)
            {
                var format = (processingModel.OverrideResultsFormat ??
                              SparqlResultsFormat.AllFormats.FirstOrDefault(
                                  f => f.MediaTypes.Any(m => requestedMediaRange.Matches(m)))) ??
                             SparqlResultsFormat.Xml;
                var graphFormat =
                    (processingModel.OverrideGraphFormat ??
                     RdfFormat.AllFormats.FirstOrDefault(f => f.MediaTypes.Any(m => requestedMediaRange.Matches(m)))) ??
                    RdfFormat.RdfXml;

                return new SparqlQueryResponse(processingModel, context.Request.Headers.IfModifiedSince, format, graphFormat);
            }
            var graphList = model as GraphListModel;
            if (graphList != null)
            {
                var format =
                    SparqlResultsFormat.AllFormats.FirstOrDefault(
                        f => f.MediaTypes.Any(m => requestedMediaRange.Matches(m))) ?? SparqlResultsFormat.Xml;
                return new TextResponse(
                    graphList.AsString(format), format.MediaTypes[0]);
            }
            throw new ArgumentException("Unexpected model type: " + model.GetType());
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings { get { return SparqlExtensionMappings; } }
    }
}