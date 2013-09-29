using System;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;

namespace BrightstarDB.Server.Modules
{
    public class SparqlQueryProcessingModel
    {
        private readonly string _storeName;
        private readonly IBrightstarService _service;
        private readonly SparqlRequestObject _sparqlRequest;
        private readonly ulong _commitId;

        public SparqlQueryProcessingModel(string storeName, IBrightstarService service, SparqlRequestObject sparqlRequest)
        {
            _storeName = storeName;
            _service = service;
            _sparqlRequest = sparqlRequest;
        }

        public SparqlQueryProcessingModel(string storeName, ulong commitId, IBrightstarService service,
                                          SparqlRequestObject sparqlRequest)
        {
            _storeName = storeName;
            _commitId = commitId;
            _service = service;
            _sparqlRequest = sparqlRequest;
        }

        public Stream GetResultsStream(SparqlResultsFormat format, DateTime? ifNotModifiedSince)
        {
            if (_commitId > 0)
            {
                var commitPointInfo = _service.GetCommitPoint(_storeName, _commitId);
                if (commitPointInfo == null) throw new InvalidCommitPointException();
                return _service.ExecuteQuery(commitPointInfo, _sparqlRequest.Query, _sparqlRequest.DefaultGraphUri, format);
            }

            return _service.ExecuteQuery(_storeName, _sparqlRequest.Query, _sparqlRequest.DefaultGraphUri, ifNotModifiedSince, format);
        }
    }
}