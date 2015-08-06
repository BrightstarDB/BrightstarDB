using System;
using System.Collections.Generic;

namespace BrightstarDB.Server.Modules.Model
{
    public interface ISparqlResultModel
    {
        string StoreName { get; }
        ulong CommitId { get; }
        bool HasCommitId { get; }
        SparqlRequestObject SparqlRequest { get; }
        SparqlResultsFormat OverrideResultsFormat { get; set; }
        RdfFormat OverrideGraphFormat { get; set; }
        string ErrorMessage { get; }
        bool HasErrorMessage { get; }
        bool HasFormattedResults { get; }
        string RawResults { get; }
        List<string> Variables { get; }
        List<object[]> Rows { get; }
        void ExecuteQueryForHtml(DateTime? ifNotModifiedSince);
        long QueryExecution { get; }
    }
}