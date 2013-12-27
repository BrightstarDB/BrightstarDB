using System;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Utils;

namespace BrightstarDB.Server.Modules
{
    public class SparqlQueryProcessingModel
    {
        private readonly string _storeName;
        private readonly IBrightstarService _service;
        private readonly SparqlRequestObject _sparqlRequest;
        private readonly ulong _commitId;

        public string StoreName { get { return _storeName; } }
        public ulong CommitId { get { return _commitId; } }
        public SparqlRequestObject SparqlRequest { get { return _sparqlRequest; } }

        public SparqlResultsFormat OverrideSparqlFormat { get; set; }
        public RdfFormat OverrideGraphFormat { get; set; }

        public SerializableModel ResultModel { get; set; }

        public SparqlQueryProcessingModel(string storeName, IBrightstarService service, SparqlRequestObject sparqlRequest)
        {
            _storeName = storeName;
            _service = service;
            _sparqlRequest = sparqlRequest;
            ResultModel = sparqlRequest.Query == null ? SerializableModel.None : SparqlQueryHelper.GetResultModel(sparqlRequest.Query);
        }

        public SparqlQueryProcessingModel(string storeName, ulong commitId, IBrightstarService service,
                                          SparqlRequestObject sparqlRequest)
        {
            _storeName = storeName;
            _commitId = commitId;
            _service = service;
            _sparqlRequest = sparqlRequest;
            ResultModel = sparqlRequest.Query == null ? SerializableModel.None : SparqlQueryHelper.GetResultModel(sparqlRequest.Query);
        }

        public Stream GetResultsStream(SparqlResultsFormat format, RdfFormat graphFormat, DateTime? ifNotModifiedSince, out ISerializationFormat streamFormat)
        {
            if (_commitId > 0)
            {
                var commitPointInfo = _service.GetCommitPoint(_storeName, _commitId);
                if (commitPointInfo == null) throw new InvalidCommitPointException();
                return _service.ExecuteQuery(commitPointInfo, _sparqlRequest.Query, _sparqlRequest.DefaultGraphUri, format, graphFormat, out streamFormat);
            }

            
            return _service.ExecuteQuery(_storeName, _sparqlRequest.Query, _sparqlRequest.DefaultGraphUri, ifNotModifiedSince, format, graphFormat, out streamFormat);
        }


    }
}