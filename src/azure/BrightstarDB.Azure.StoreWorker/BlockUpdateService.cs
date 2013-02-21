using System;
using System.Diagnostics;
using System.ServiceModel;
using BrightstarDB.Azure.Common;

namespace BrightstarDB.Azure.StoreWorker
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class BlockUpdateService : IBlockUpdateService
    {
        #region Implementation of IBlockUpdateService

        public void UpdateBlock(BlockInfo block)
        {
            if (block != null)
            {
                try
                {
                    Trace.TraceInformation("Received update for block {0}", block);
                    AzureBlockStore.Instance.ReceiveBlock(block);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error receiving block {0}. Cause: {1}", block, ex);
                    // TODO: Should we drop some/all cached blocks for the same store to force a re-read from blobstore?
                    /*
                    AzureBlockStore.Instance.DropBlock(block.StoreName, block.Offset);
                    if (block.Offset > AzureBlockStore.Instance.BlockSize)
                    {
                        AzureBlockStore.Instance.DropBlock(block.StoreName,
                                                           block.Offset - AzureBlockStore.Instance.BlockSize);
                    }
                     */
                    // Rethrow the exception as a service fault so that the sender knows this receiver has a problem.
                    throw;
                }
            }
        }

        public void InvalidateBlocks(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                try
                {
                    Trace.TraceInformation("Received invalidate for path {0}", path);
                    AzureBlockStore.Instance.InvalidateBlocks(path);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error invalidating blocks for path {0}. Cause: {1}", path, ex);
                    throw;
                }
            }
        }

        #endregion
    }
}
