using System.ServiceModel;
using BrightstarDB.Azure.Common;

namespace BrightstarDB.Azure.StoreWorker
{
    [ServiceContract(Namespace = "http://www.networkedplanet.com/services/brightstar/azureblockupdateservice")]
    public interface IBlockUpdateService
    {
        [OperationContract]
        void UpdateBlock(BlockInfo block);

        [OperationContract]
        void InvalidateBlocks(string path);
    }
}
