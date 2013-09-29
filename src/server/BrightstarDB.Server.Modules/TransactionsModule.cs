using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using Nancy;
using Nancy.ModelBinding;

namespace BrightstarDB.Server.Modules
{
    public class TransactionsModule : NancyModule
    {
        private const int DefaultPageSize = 10;

        public TransactionsModule(IBrightstarService brightstarService)
        {
            Get["/{storeName}/transactions"] = parameters =>
                {
                    var transactionsRequest = this.Bind<TransactionsRequestObject>();
                    var transactions = brightstarService.GetTransactions(transactionsRequest.StoreName,
                                                                                 transactionsRequest.Skip,
                                                                                 DefaultPageSize + 1);
                    return Negotiate.WithPagedList(transactions.Select(MakeResponseObject),
                                                   transactionsRequest.Skip, DefaultPageSize, DefaultPageSize,
                                                   "transactions");
                };

            Get["/{storeName}/transactions/byjob/{jobId}"] = parameters =>
                {
                    Guid jobId;
                    if (!Guid.TryParse(parameters["jobId"], out jobId))
                    {
                        return HttpStatusCode.NotFound;
                    }
                    var txn = brightstarService.GetTransaction(parameters["storeName"], jobId);
                    return txn == null ? HttpStatusCode.NotFound : MakeResponseObject(txn);
                };
        }

        private static TransactionResponseObject MakeResponseObject(ITransactionInfo transactionInfo)
        {
            return new TransactionResponseObject
                {
                    Id = transactionInfo.Id,
                    JobId = transactionInfo.JobId,
                    StoreName = transactionInfo.StoreName,
                    StartTime = transactionInfo.StartTime,
                    Status = transactionInfo.Status.ToString(),
                    TransactionType = transactionInfo.TransactionType.ToString()
                };
        }
    }
}
