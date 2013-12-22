using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.Server.Modules.Model
{
    public class SparqlResultModel
    {
        private readonly string _storeName;
        private readonly IBrightstarService _service;
        private readonly SparqlRequestObject _sparqlRequest;
        private readonly ulong _commitId;

        public string StoreName { get { return _storeName; } }
        public ulong CommitId { get { return _commitId; } }
        public SparqlRequestObject SparqlRequest { get { return _sparqlRequest; } }
        public SparqlResultsFormat ResultsFormat { get; set; }
        public string ErrorMessage { get; private set; }

        public bool HasFormattedResults { get; private set; }
        public string RawResults { get; private set; }
        public List<string> Variables { get; set; }
        public List<object[]> Rows { get; set; } 

        public SparqlResultModel(string storeName, IBrightstarService service, SparqlRequestObject sparqlRequest, SparqlResultsFormat resultsFormat)
        {
            _storeName = storeName;
            _sparqlRequest = sparqlRequest;
            _service = service;
            ResultsFormat = resultsFormat;
        }

        public SparqlResultModel(string storeName, ulong commitId, IBrightstarService service,
                                 SparqlRequestObject sparqlRequest, SparqlResultsFormat resultsFormat)
        {
            _storeName = storeName;
            _commitId = commitId;
            _sparqlRequest = sparqlRequest;
            _service = service;
            ResultsFormat = resultsFormat;
        }

        public void ExecuteQueryForHtml(DateTime? ifNotModifiedSince)
        {
            Stream resultsStream;
            try
            {
                if (_commitId > 0)
                {
                    var commitPointInfo = _service.GetCommitPoint(_storeName, _commitId);
                    if (commitPointInfo == null) throw new InvalidCommitPointException();
                    resultsStream = _service.ExecuteQuery(commitPointInfo, _sparqlRequest.Query,
                                                          _sparqlRequest.DefaultGraphUri, ResultsFormat);
                }
                else
                {
                    resultsStream = _service.ExecuteQuery(_storeName, _sparqlRequest.Query,
                                                          _sparqlRequest.DefaultGraphUri,
                                                          ifNotModifiedSince, ResultsFormat);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = FormatExceptionMessages(ex);
                return;
            }
            if (ResultsFormat == SparqlResultsFormat.Xml)
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


        private static string FormatExceptionMessages(Exception e)
        {
            var messages = new StringBuilder();
            FormatExceptionMessages(e, 0, messages);
            return messages.ToString();
        }

        private static void FormatExceptionMessages(Exception e, int currentDepth, StringBuilder buffer)
        {
            buffer.Append(' ', currentDepth*4);
            buffer.AppendLine(e.Message);
            if (e is AggregateException)
            {
                foreach (var inner in (e as AggregateException).InnerExceptions)
                {
                    FormatExceptionMessages(inner, currentDepth + 1, buffer);
                }
            } else if (e.InnerException != null)
            {
                FormatExceptionMessages(e.InnerException, currentDepth + 1, buffer);
            }
        }
    }
}