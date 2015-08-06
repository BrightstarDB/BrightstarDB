using System;
using System.Collections.Generic;
using BrightstarDB.Server.Modules.Model;
using Nancy;
using Nancy.Responses;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class GraphListProcessor : IResponseProcessor
    {
        private static readonly MediaRange JsonMediaRange = new MediaRange("application/json");
        private static readonly List<Tuple<string, MediaRange>> GraphListExtensionMappings = new List<Tuple<string, MediaRange>>
        {
            new Tuple<string, MediaRange>("json", JsonMediaRange)
        };

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            var graphListModel = model as GraphListModel;
            if (graphListModel != null)
            {
                if (requestedMediaRange.Matches(JsonMediaRange))
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
                RequestedContentTypeResult = MatchResult.NoMatch
            };
        }

        public Response Process(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            var graphListModel = model as GraphListModel;
            if (graphListModel == null) throw new ArgumentException("Unexpected model type: " + model.GetType(), "model");
            return new JsonResponse(graphListModel.Graphs, new DefaultJsonSerializer());
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings { get {return GraphListExtensionMappings;} }
    }
}
