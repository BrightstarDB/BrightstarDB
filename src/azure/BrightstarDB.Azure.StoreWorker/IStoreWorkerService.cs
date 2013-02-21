using System.ServiceModel;

namespace BrightstarDB.Azure.StoreWorker
{
    [ServiceContract(Namespace = "http://www.networkedplanet.com/services/brightstar/azurestoreworkerservice/")]
    public interface IStoreWorkerService
    {
        /// <summary>
        /// Create a new B* store.
        /// </summary>
        /// <param name="storeId">The new store ID</param>
        /// <returns>True if the store was created successfully, false otherwise</returns>
        [OperationContract]
        bool CreateStore(string storeId);


        /// <summary>
        /// Execute a SPARQL query against a B* store
        /// </summary>
        /// <param name="storeId">The ID of the store to be queried</param>
        /// <param name="query">The SPARQL query to be executed.</param>
        /// <returns>The SPARQL query result</returns>
        [OperationContract]
        string ExecuteQuery(string storeId, string query);

        [OperationContract]
        bool DeleteStore(string storeId);
    }
}
