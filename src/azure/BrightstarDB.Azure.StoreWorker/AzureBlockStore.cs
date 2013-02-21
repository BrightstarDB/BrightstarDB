using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.StoreWorkerClient;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.StoreWorker
{
    public class AzureBlockStore : IBlockProvider, IDisposable
    {
#if BLOCKSTORECACHE
        private readonly BlockStoreCache _cache;
#else
        private readonly ICache _cache;
#endif
        private readonly Dictionary<string, long> _highestPageOffsetByPath;
        private readonly ConcurrentQueue<BlockInfo> _commitList; 
        private readonly CloudStorageAccount _storageAccount;
        private volatile bool _stopCommitThread;
        private readonly Thread _commitThread;
        private readonly bool _disconnected;
        public static AzureBlockStore Instance { get; private set; }
        public static int AzureBlockSize = 4194304;

        public static void Initialize(AzureBlockStoreConfiguration configuration)
        {
            Instance = new AzureBlockStore(configuration);
        }

        private AzureBlockStore(AzureBlockStoreConfiguration configuration)
        {
#if BLOCKSTORECACHE
            _cache = new BlockStoreCache((long) configuration.MemoryCacheInMB*1024*1024, configuration.LocalStorageKey);
#else
            ICache memoryCache = new MemoryCache((long)configuration.MemoryCacheInMB * 1024 * 1024, new LruCacheEvictionPolicy());
            if (!String.IsNullOrEmpty(configuration.LocalStorageKey))
            {
                var localDiskResource = RoleEnvironment.GetLocalResource(configuration.LocalStorageKey);
                ICache diskCache = new DirectoryCache(localDiskResource.RootPath,
                                                      ((long)localDiskResource.MaximumSizeInMegabytes - 1) * 1024 * 1024,
                                                      new LruCacheEvictionPolicy());
                _cache = new TwoLevelCache(memoryCache, diskCache);
            }
            else
            {
                _cache = memoryCache;
            }
#endif
            _highestPageOffsetByPath = new Dictionary<string, long>();
            _storageAccount = CloudStorageAccount.Parse(configuration.ConnectionString);
            _commitList = new ConcurrentQueue<BlockInfo>();
            _commitThread = new Thread(RunCommitThread);
            _commitThread.Start();
            _disconnected = configuration.Disconnected;
        }

        private void RunCommitThread()
        {
            while (!_stopCommitThread)
            {
                BlockInfo blockInfo = null;
                try
                {
                    if (_commitList.TryDequeue(out blockInfo))
                    {
                        //Trace.TraceInformation("AzureBlockStore.CommitThread: Attempting to commit block {0}", blockInfo);
                        if (blockInfo != null)
                        {
                            //Trace.TraceInformation("AzureBlockStore.CommitThread: Got block to commit {0}", blockInfo);
                            bool retry;
                            do
                            {
                                retry = false;
                                var pageBlob = GetPageBlob(blockInfo.StoreName);
                                if (pageBlob != null)
                                {
                                    Trace.TraceInformation("AzureBlockStore.CommitThread: Got target page blob: {0}", pageBlob.Uri);
                                    try
                                    {
                                        pageBlob.FetchAttributes();
                                    }
                                    catch (StorageClientException ex)
                                    {
                                        Trace.TraceWarning(
                                            "AzureBlockStore.CommitThread: Could not retrieve attributes for page blob: {0} due to {1}. Aborting update",
                                            pageBlob.Uri, ex);
                                        continue;
                                    }

                                    var dataLength =
                                        long.Parse(
                                            pageBlob.Attributes.Metadata[AzureConstants.BlobDataLengthPropertyName]);
                                    if (dataLength <= (blockInfo.Offset + blockInfo.Length))
                                    {
                                        try
                                        {
                                            Trace.TraceInformation(
                                                "AzureBlockStore.CommitThread: Attempting to write pages to page blob");
                                            var requestOptions = new BlobRequestOptions
                                                                     {
                                                                         AccessCondition =
                                                                             AccessCondition.IfMatch(
                                                                                 pageBlob.Attributes.Properties.ETag)
                                                                     };
                                            byte[] paddedBuffer;
                                            if (blockInfo.Data.Length%512 == 0)
                                            {
                                                paddedBuffer = blockInfo.Data;
                                            }
                                            else
                                            {
                                                paddedBuffer =
                                                    new byte[blockInfo.Data.Length + (512 - (blockInfo.Data.Length%512))
                                                        ];
                                                Buffer.BlockCopy(blockInfo.Data, 0, paddedBuffer, 0,
                                                                 blockInfo.Data.Length);
                                            }
                                            using (var ms = new MemoryStream(paddedBuffer, false))
                                            {
                                                pageBlob.WritePages(ms, blockInfo.Offset, requestOptions);
                                            }
                                            pageBlob.Metadata[AzureConstants.BlobDataLengthPropertyName] =
                                                (blockInfo.Offset + blockInfo.Length).ToString(
                                                    CultureInfo.InvariantCulture);
                                            Trace.TraceInformation("AzureBlockStore.CommitThread: Block data written. Now updating metadata.");
                                            pageBlob.SetMetadata();
                                            Trace.TraceInformation(
                                                "AzureBlockStore.CommitThread: Block written and pageBlob datalength updated to {0}",
                                                pageBlob.Metadata[AzureConstants.BlobDataLengthPropertyName]);
                                        }
                                        catch (StorageClientException ex)
                                        {
                                            if (ex.ErrorCode == StorageErrorCode.ConditionFailed)
                                            {
                                                Trace.TraceWarning(
                                                    "AzureBlockStore.CommitThread: page blob update failed with a ConditionFailed storage exception. Attempting retry.");
                                                retry = true;
                                            }
                                            else
                                            {
                                                Trace.TraceError(
                                                    "Commit of block {0} failed with StorageClientException {1} ({2}). Operation will not be retried.",
                                                    blockInfo, ex, ex.ErrorCode);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.TraceError("Commit of block {0} failed with exception {1}. Operation will not be retried.", blockInfo, ex);
                                        }
                                    }
                                    else
                                    {
                                        Trace.TraceInformation(
                                            "AzureBlockStore.CommitThread: page blob data length {0} already exceeeds commit block end offset of {1}. Update aborted.",
                                            dataLength, blockInfo.Offset + blockInfo.Length);
                                    }
                                }
                            } while (retry);
                        }
                    }
                    if (_commitList.IsEmpty)
                    {
                        //Trace.WriteLine("AzureBlockStore.CommitThread: Commit list is currently empty. Sleeping");
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    if (blockInfo != null)
                    {
                        // TODO : This should go into event logs too.
                        Trace.TraceError(
                            "AzureBlockStore.CommitThread: Unhandled exception {0}. Aborting update for block: {1}", ex,
                            blockInfo);
                    } else
                    {
                        Trace.TraceError(
                            "AzureBlockStore.CommitThread: Unhandled exception {0}", ex);
                    }
                }
            }
        }

        private static string MakeCacheKey(string path, long pageOffset)
        {
            return path + "_" + pageOffset;
        }

        #region Implementation of IBlockProvider

        /// <summary>
        /// Get the size of block that this provider works in
        /// </summary>
        public int BlockSize
        {
            get { return AzureBlockSize; } // 4MB
        }

        /// <summary>
        /// Gets the byte offset of the current active block
        /// </summary>
        /// <param name="path">The path to the block store</param>
        public long GetActiveBlockOffset(string path)
        {
            try
            {
                long ret;
                if (_highestPageOffsetByPath.TryGetValue(path, out ret))
                {
                    return ret;
                }
                var pageBlob = GetPageBlob(path);
                pageBlob.FetchAttributes();
                var dataLength = long.Parse(pageBlob.Attributes.Metadata[AzureConstants.BlobDataLengthPropertyName]);
                var activeBlockOffset = dataLength - (dataLength%BlockSize);
                _highestPageOffsetByPath[path] = activeBlockOffset;
                return activeBlockOffset;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetActiveBlockOffset {0} failed with exception {1}", path, ex);
                throw;
            }
        }

        /// <summary>
        /// Reads data from the block storage
        /// </summary>
        /// <param name="path">The path to the block store to read from</param>
        /// <param name="offset">The start offset of the block</param>
        /// <param name="createIfNotExists">Flag indicating if the requested block should be created as a new empty block if it is beyond the current end of file.</param>
        /// <returns>The block read</returns>
        public IBlockInfo ReadBlock(string path, long offset, bool createIfNotExists)
        {
            try
            {
                var block = GetBlock(path, offset, createIfNotExists);
                //Buffer.BlockCopy(block.Data, 0, buffer, 0, block.Length);
                //return block.Length;
                return block;
            }
            catch (Exception ex)
            {
                Trace.TraceError("ReadBlock {0}@{1}: Failed with exception {2}", path, offset, ex);
                throw;
            }
        }

        /// <summary>
        /// Determines if the block at the specified byte offset is the current active block
        /// </summary>
        /// <param name="path">The path to the block store to check</param>
        /// <param name="offset">The offset to check</param>
        /// <returns>True if the specified block is the current active block (the last bloock in the file), false otherwise.</returns>
        public bool IsActive(string path, long offset)
        {
            try
            {
                return GetActiveBlockOffset(path) == offset;
            }
            catch (Exception ex)
            {
                Trace.TraceError("IsActive: {0}, {1} failed with exception: {2}", path, offset, ex);
                throw;
            }
        }

        /// <summary>
        /// Writes data to block storage
        /// </summary>
        /// <param name="path">The path to the block store to write to</param>
        /// <param name="offset">The start offset of the block to write</param>
        /// <param name="buffer">The buffer containing the data to be written</param>
        /// <param name="count">The number of bytes to be written</param>
        /// <returns>True if the block was written successfully, false otherwise</returns>
        public void WriteBlock(string path, long offset, byte[] buffer, int count)
        {
            Trace.TraceInformation("WriteBlock {0}@{1}: Write of {2} bytes", path, offset, count);
            try
            {
                var block = new BlockInfo
                                {
                                    StoreName = path,
                                    Offset = offset,
                                    Length = count,
                                    Data = new byte[AzureBlockSize]
                                };
                Buffer.BlockCopy(buffer, 0, block.Data, 0, count);
#if BLOCKSTORECACHE
                _cache.Insert(block);
#else
                _cache.Insert(MakeCacheKey(path, offset), block, CachePriority.High);
#endif
                Trace.TraceInformation("WriteBlock {0}@{1}: Cache updated", path, offset);
                UpdateHighestOffset(path, offset);
                Trace.TraceInformation("WriteBlock {0}@{1}: Highest Offset Updated", path, offset);
                NotifyBlockUpdate(block);
                Trace.TraceInformation("WriteBlock {0}@{1}: Update notifications sent", path, offset);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WriteBlock {0}@{1}: Write of {2} bytes failed with exception: {3}", path, offset, count, ex);
                throw;
            }
        }

        /// <summary>
        /// Returns the total number of bytes available in the specified block store
        /// </summary>
        /// <param name="path">The path to the block store</param>
        /// <returns>The number of bytes in the block store</returns>
        public long GetLength(string path)
        {
            try
            {
                long highestOffset;
                if (_highestPageOffsetByPath.TryGetValue(path, out highestOffset))
                {
#if BLOCKSTORECACHE
                    var cachedBlock = _cache.Lookup(path, highestOffset);
#else
                    var cachedBlock = _cache.Lookup<BlockInfo>(MakeCacheKey(path, highestOffset));
#endif
                    if (cachedBlock != null)
                    {
                        var l = cachedBlock.Offset + cachedBlock.Length;
                        return l;
                    }
                }
                var pageBlob = GetPageBlob(path);
                pageBlob.FetchAttributes();
                var dataLength = long.Parse(pageBlob.Attributes.Metadata[AzureConstants.BlobDataLengthPropertyName]);
                return dataLength;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetLength {0} failed with exception {1}", path, ex);
                throw;
            }
        }

        #endregion

        private CloudPageBlob GetPageBlob(string path)
        {
            var client = _storageAccount.CreateCloudBlobClient();
            return client.GetPageBlobReference(path.Replace('\\', '/'));
        }

        private BlockInfo GetBlock(string path, long offset, bool createIfNotExists)
        {
            try
            {
#if BLOCKSTORECACHE
                var cachedBlock = _cache.Lookup(path, offset);
#else
                var cacheKey = MakeCacheKey(path, offset);
                var cachedBlock = _cache.Lookup<BlockInfo>(cacheKey);
#endif
                if (cachedBlock != null)
                {
                    //Trace.TraceInformation("GetBlock {0}, {1} returning cached block {2}", path, offset, cachedBlock);
                    return cachedBlock;
                }
                Trace.TraceInformation("GetBlock {0}@{1}: Reading from blob storage.", path, offset);
                var blob = GetPageBlob(path);
                blob.FetchAttributes();
                long blobLength = long.Parse(blob.Attributes.Metadata[AzureConstants.BlobDataLengthPropertyName]);
                if (blobLength <= offset)
                {
                    if (createIfNotExists)
                    {
                        var blockInfo = new BlockInfo
                                            {
                                                Data = new byte[BlockSize],
                                                Length = 0,
                                                Offset = offset,
                                                StoreName = path
                                            };
                        Trace.TraceInformation("GetBlock {0}@{1}: Created a new block {2}",
                                               path, offset, blockInfo);
#if BLOCKSTORECACHE
                        _cache.Insert(blockInfo);
#else
                        _cache.Insert(cacheKey, blockInfo, CachePriority.Normal);
#endif
                        _highestPageOffsetByPath[path] = offset;
                        Trace.TraceInformation("GetBlock {0}@{1}: Updated cache", path, offset);
                        return blockInfo;
                    }
                    else
                    {
                        Trace.TraceError(
                            "GetBlock {0}@{1}: Call failed as current blob length ({2}) < requested offset ({1})",
                            path, offset, blobLength);
                        throw new Exception("Attempt to read past end of file");
                    }
                }
                long tmp = blobLength - offset;
                int bytesToRead = BlockSize;
                if (tmp < BlockSize)
                {
                    bytesToRead = (int) tmp;
                }
                Trace.TraceInformation("GetBlock {0}@{1}: Blob size is {2}. Block read length is {3}", path, offset,
                                       blobLength, bytesToRead);
                using (var blobStream = blob.OpenRead())
                {
                    blobStream.Seek(offset, SeekOrigin.Begin);
                    var blockData = new byte[BlockSize];
                    if (bytesToRead > 0)
                    {
                        BlockInfo blockInfo;
                        int bytesRead = blobStream.Read(blockData, 0, bytesToRead);
                        blockInfo = new BlockInfo
                                        {
                                            Data = blockData,
                                            Length = bytesRead,
                                            Offset = offset,
                                            StoreName = path
                                        };
                        Trace.TraceInformation("GetBlock {0}@{1}: Read {2} bytes from blob", path, offset, bytesRead);
#if BLOCKSTORECACHE
                        _cache.Insert(blockInfo);
#else
                    _cache.Insert(cacheKey, blockInfo, CachePriority.Normal);
#endif
                        Trace.TraceInformation("GetBlock {0}@{1}: Cache updated", path, offset);
                        Trace.TraceInformation("GetBlock {0}@{1}: Returning block {2}", path, offset, blockInfo);
                        return blockInfo;
                    }
                    else
                    {
                        Trace.TraceError(
                            "GetBlock {0}@{1}: Blob length is {2} , so asked for <= {3} bytes. Returning a new block",
                            path, offset, blobLength, bytesToRead);
                        throw new Exception("Attemp to read past end of blob.");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetBlock {0}@{1}: Failed with exception {2}", path, offset, ex);
                throw;
            }
        }

        private void NotifyBlockUpdate(BlockInfo block)
        {
            if (_disconnected) return;
            // Send notification to all other workers
            foreach(var workerInstance in RoleEnvironment.Roles[AzureConstants.StoreWorkerRoleName].Instances)
            {
                if (!workerInstance.Id.Equals(RoleEnvironment.CurrentRoleInstance.Id))
                {
                    try
                    {
                        var client = GetBlockServiceClient(workerInstance);
                        client.UpdateBlock(block);
                        //Trace.TraceInformation("NotifyBlockUpdate: {0} sent to {1}", block, workerInstance.Id);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("NotifyBlockUpdate {0} failed to notify instance {1}. Cause: {2}",
                                         block, workerInstance.Id, ex);
                    }
                }
            }
            //Trace.WriteLine(String.Format("Enqueuing commit update for received block: {0}", /*cacheKey*/ block),"AzureBlockStore");
            _commitList.Enqueue(block.Copy());
        }

        private BlockUpdateServiceClient GetBlockServiceClient(RoleInstance workerInstance)
        {
            var binding = new BasicHttpContextBinding
            {
                TransferMode = TransferMode.StreamedResponse,
                MaxReceivedMessageSize = Int32.MaxValue,
                SendTimeout = TimeSpan.FromMinutes(10),
                ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                HostNameComparisonMode = HostNameComparisonMode.Exact
            };
            var endpointUri = String.Format("http://{0}", workerInstance.InstanceEndpoints[AzureConstants.BlockServiceEndpoint].IPEndpoint);
            var endpointAddress = new EndpointAddress(endpointUri);
            var client = new BlockUpdateServiceClient(binding, endpointAddress);
            return client;
        }

        internal void ReceiveBlock(BlockInfo block)
        {
            try
            {
                //Trace.TraceInformation("ReceiveBlock: {0} received update notification for block {1}", RoleEnvironment.CurrentRoleInstance.Id, block);
                var cacheKey = MakeCacheKey(block.StoreName, block.Offset);
#if BLOCKSTORECACHE
                _cache.Insert(block);
#else
                _cache.Insert(cacheKey, block, CachePriority.High);
#endif
                //Trace.TraceInformation("ReceiveBlock: Cache updated for block {0}", block);
                UpdateHighestOffset(block.StoreName, block.Offset);
                //Trace.TraceInformation("ReceiveBlock: Updated highest offset to {0}", block.Offset);
                // Update the list of blocks to be committed but with a delay to allow the originator time to do the update themselves.
                var updateTask = new Task(k =>
                                              {
                                                  Thread.Sleep(500);
                                                  _commitList.Enqueue(block.Copy());
                                              }, cacheKey);
                updateTask.Start();
            }
            catch (Exception ex)
            {
                Trace.TraceError("ReceiveBlock {0} failed with exception {1}", block, ex);
                throw;
            }
            //Trace.TraceInformation("ReceiveBlock {0} completed.", block);
        }

        private void UpdateHighestOffset(string storeName, long offset)
        {
            long higestOffset;
            if (_highestPageOffsetByPath.TryGetValue(storeName, out higestOffset))
            {
                if (offset > higestOffset)
                {
                    _highestPageOffsetByPath[storeName] =  offset;
                }
            }
            else
            {
                _highestPageOffsetByPath[storeName] = higestOffset;
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Shutdown();
        }

        #endregion

        public void InvalidateBlocks(string path)
        {
            _cache.InvalidateBlocks(path);
        }

        public void Shutdown()
        {
            _stopCommitThread = true;
            try
            {
                if (_commitThread.IsAlive)
                {
                    Trace.TraceInformation("AzureBlockStore.Shutdown: Waiting for commit thread to exit...");
                    _commitThread.Join(5000);
                    Trace.TraceInformation("AzureBlockStore.Shutdown: Commit thread has exited cleanly.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("AzureBlockStore.Shutdown: Error cleaning up commit thread: {0}. Trying to force abort instead.", ex);
                if (_commitThread.IsAlive)
                {
                    _commitThread.Abort();
                }
            }
        }
    }
}
