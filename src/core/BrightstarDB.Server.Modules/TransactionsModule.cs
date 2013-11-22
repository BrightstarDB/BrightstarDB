using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.ModelBinding;

namespace BrightstarDB.Server.Modules
{
    public class TransactionsModule : NancyModule
    {
        private const int DefaultPageSize = 10;

        public TransactionsModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, get:StorePermissions.ViewHistory);

            Get["/{storeName}/transactions"] = parameters =>
                {
                    var transactionsRequest = this.Bind<TransactionsRequestObject>();
                    if (transactionsRequest.Take <= 0) transactionsRequest.Take = DefaultPageSize;
                    var transactions = brightstarService.GetTransactions(transactionsRequest.StoreName,
                                                                                 transactionsRequest.Skip,
                                                                                 transactionsRequest.Take + 1);
                    return Negotiate.WithPagedList(transactionsRequest,
                                                   transactions.Select(MakeResponseObject),
                                                   transactionsRequest.Skip, transactionsRequest.Take, DefaultPageSize,
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

        private static TransactionResponseModel MakeResponseObject(ITransactionInfo transactionInfo)
        {
            return new TransactionResponseModel
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
