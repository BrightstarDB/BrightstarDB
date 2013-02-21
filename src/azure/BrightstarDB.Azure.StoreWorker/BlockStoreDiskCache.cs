using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BrightstarDB.Azure.StoreWorker
{
    public interface IBlockStoreDiskCache
    {
        bool TryGetData(string blockKey, byte[] buffer);
        void PutData(string blockKey, byte[] buffer);
    }

    public class BlockStoreDiskCache : IBlockStoreDiskCache
    {
        private readonly int _blockSize;
        private readonly int _capacity;
        private readonly string _path;
        private readonly Stream _fileStream;
        private readonly long _fileLength;
        private readonly Dictionary<string, int> _blockLookup;
        private int _insertPoint;
        private readonly Mutex _accessLock;

        /// <summary>
        /// Creates a new disk cache with the specified file name and capacity
        /// </summary>
        /// <param name="path">The full path to the disk cache file to be created</param>
        /// <param name="capacity">The number of blocks capacity for the store</param>
        /// <param name="blockSize">The size of individual blocks in the store</param>
        public BlockStoreDiskCache(string path, int capacity, int blockSize)
        {
            _blockSize = blockSize;
            _capacity = capacity;
            _path = path;
            bool retry = false;
            _fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            do
            {
                try
                {
                    _fileLength = (long) _capacity*blockSize;
                    _fileStream.SetLength(_fileLength);
                    _blockLookup = new Dictionary<string, int>(_capacity);
                    retry = false;
                }
                catch (IOException e)
                {
                    Logging.LogWarning(BrightstarEventId.BlockProviderError,
                                       "Could not create block cache file with capacity {0} due to exception {1}. Retrying with smaller capacity",
                                       _capacity, e);
                    _capacity = _capacity/2;
                    retry = true;
                }
            } while (retry);
            Logging.LogInfo("Created block cache with capacity {0}", _capacity);
            _accessLock = new Mutex();
            _insertPoint = 0;
        }

        public bool TryGetData(string blockKey, byte[] buffer)
        {
            if (_blockLookup.ContainsKey(blockKey))
            {
                _accessLock.WaitOne();
                try
                {
                    int slotNumber = _blockLookup[blockKey];
                    if (slotNumber != _insertPoint)
                    {
                        _fileStream.Seek((long) slotNumber*_blockSize, SeekOrigin.Begin);
                        int bytesRead = _fileStream.Read(buffer, 0, _blockSize);
                        return bytesRead == _blockSize;
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWarning(BrightstarEventId.BlockProviderError,
                                       "BlockStoreDiskCache.TryGetData failed with exception: {0}", ex);
                }
                finally
                {
                    _accessLock.ReleaseMutex();
                }
            }
            return false;
        }

        public void PutData(string blockKey, byte[] buffer)
        {
            _accessLock.WaitOne();
            try
            {
                _fileStream.Seek((long) _insertPoint*_blockSize, SeekOrigin.Begin);
                _fileStream.Write(buffer, 0, _blockSize);
                _blockLookup[blockKey] = _insertPoint;
                _insertPoint++;
            }
                catch(Exception ex)
                {
                    Logging.LogWarning(BrightstarEventId.BlockProviderError, "BlockStoreDiskCache.PutData failed with exception: {0}", ex);
                }
            finally
            {
                _accessLock.ReleaseMutex();
            }
        }

    }
}
