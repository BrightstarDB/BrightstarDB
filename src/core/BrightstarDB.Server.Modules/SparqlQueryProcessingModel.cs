using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Utils;

namespace BrightstarDB.Server.Modules
{
    public class SparqlQueryProcessingModel : ISparqlResultModel
    {
        private readonly string _storeName;
        private readonly IBrightstarService _service;
        private readonly SparqlRequestObject _sparqlRequest;
        private readonly ulong _commitId;

        public string StoreName { get { return _storeName; } }
        public ulong CommitId { get { return _commitId; } }
        public bool HasCommitId { get { return CommitId > 0; } }
        public SparqlRequestObject SparqlRequest { get { return _sparqlRequest; } }

        public SparqlResultsFormat OverrideResultsFormat { get; set; }
        public RdfFormat OverrideGraphFormat { get; set; }

        public SerializableModel ResultModel { get; set; }
        public bool HasErrorMessage { get { return !String.IsNullOrEmpty(ErrorMessage); } }
        public string ErrorMessage { get; private set; }
        public bool HasFormattedResults { get; private set; }
        public string RawResults { get; private set; }
        public List<string> Variables { get; private set; }
        public List<object[]> Rows { get; private set; }

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

        public void ExecuteQueryForHtml(DateTime? ifNotModifiedSince)
        {
            Stream resultsStream;

            // No query => no results rather than triggering an error
            if (String.IsNullOrEmpty(_sparqlRequest.Query))
            {
                HasFormattedResults = false;
                RawResults = String.Empty;
                return;
            }

            ISerializationFormat returnFormat;
            try
            {
                resultsStream = GetResultsStream(
                    OverrideResultsFormat ?? SparqlResultsFormat.Xml,
                    OverrideGraphFormat ?? RdfFormat.RdfXml,
                    ifNotModifiedSince, out returnFormat);
            }
            catch (Exception ex)
            {
                ErrorMessage = FormatExceptionMessages(ex);
                return;
            }
            if ( returnFormat == SparqlResultsFormat.Xml)
            {
                XDocument resultsDoc = XDocument.Load(resultsStream);
                HasFormattedResults = true;
                Variables = resultsDoc.GetVariableNames().ToList();
                Rows = new List<object[]>();
                var varCount = Variables.Count;
                foreach (var resultRow in resultsDoc.SparqlResultRows())
                {
                    var row = new object[varCount];
                    for (int i = 0; i < varCount; i++)
                    {
                        row[i] = resultRow.GetColumnValue(Variables[i]);
                    }
                    Rows.Add(row);
                }
            }
            else
            {
                HasFormattedResults = false;
                using (var rdr = new StreamReader(resultsStream))
                {
                    RawResults = rdr.ReadToEnd();
                }
            }
        }

        private long _queryExecution = -1;
        public long QueryExecution
        {
            get
            {
                if (_queryExecution < 0)
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    ExecuteQueryForHtml(null);
                    timer.Stop();
                    _queryExecution = timer.ElapsedMilliseconds;
                }
                return _queryExecution;
            }
        }


        private static string FormatExceptionMessages(Exception e)
        {
            var messages = new StringBuilder();
            FormatExceptionMessages(e, 0, messages);
            return messages.ToString();
        }

        private static void FormatExceptionMessages(Exception e, int currentDepth, StringBuilder buffer)
        {
            buffer.Append(' ', currentDepth * 4);
            buffer.AppendLine(e.Message);
            if (e is AggregateException)
            {
                foreach (var inner in (e as AggregateException).InnerExceptions)
                {
                    FormatExceptionMessages(inner, currentDepth + 1, buffer);
                }
            }
            else if (e.InnerException != null)
            {
                FormatExceptionMessages(e.InnerException, currentDepth + 1, buffer);
            }
        }

    }
}