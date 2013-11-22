using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Storage.Statistics;
using BrightstarDB.Storage.TransactionLog;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage.BTreeStore
{
    internal abstract class AbstractStoreManager : IStoreManager
    {
        public const string DataFileName = "data.bs";
        public const string ConsolidateDataFileName = "consolidatedata.bs";
        public const string MasterFileName = "masterfile.bs";
        private readonly IPersistenceManager _persistenceManager;
        private readonly StoreConfiguration _configuration;
        internal const int MasterfileHeaderLongCount = 32; // number of long values that comprise the header.
        internal const int MasterfileHeaderSize = MasterfileHeaderLongCount*8;

        // maps the types we persist to unique ids.
        // messing with these except for adding new ones is very dangerous.
        internal static Dictionary<Type, ulong> PersistantTypeIdentifiers = new Dictionary<Type, ulong>()
                                                                               {
                                                                                    { typeof(Store), 0},
                                                                                    { typeof(RelatedResourceList), 1},
                                                                                    { typeof(Node<ObjectRef>), 2},
                                                                                    { typeof(Node<Bucket>), 3},
                                                                                    { typeof(Node<RelatedResource>), 4},
                                                                                    { typeof(PersistentBTree<Bucket>), 5},
                                                                                    { typeof(PersistentBTree<ObjectRef>), 6}
                                                                                };

        internal AbstractStoreManager(StoreConfiguration configuration, IPersistenceManager persistenceManager)
        {
            _configuration = configuration;
            _persistenceManager = persistenceManager;
        }

        public StoreConfiguration Configuration { get { return _configuration; } }

        #region Implementation of IStoreManager

        public IEnumerable<string> ListStores(string baseLocation)
        {
            var directories = _persistenceManager.ListSubDirectories(baseLocation);
            foreach (var directory in directories)
            {
                if (_persistenceManager.FileExists(baseLocation + "\\" + directory + "\\masterfile.bs"))
                {
                    yield return directory;
                }
            }
        }

        public IStore CreateStore(string storeLocation, bool readOnly = false)
        {
            Logging.LogInfo("Create Store {0}", storeLocation);
            if (_persistenceManager.DirectoryExists(storeLocation))
            {
                throw new StoreManagerException(storeLocation, "Store already exists");
            }

            _persistenceManager.CreateDirectory(storeLocation);
           
            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            _persistenceManager.CreateFile(dataFilePath);

            var store = new Store(storeLocation, readOnly);
            store.Commit(Guid.Empty);

            Logging.LogInfo("Store created at {0}", storeLocation);
            return store;
        }

        public IStore CreateStore(string storeLocation, PersistenceType persistenceType, bool readOnly = false)
        {
            if (persistenceType != PersistenceType.AppendOnly)
            {
                throw new NotSupportedException(String.Format("The store does not support the persistence type {0}.",
                                                              persistenceType));
            }
            return CreateStore(storeLocation, readOnly);
        }

        public IStore OpenStore(string storeLocation, bool readOnly)
        {
            var masterFilePath = Path.Combine(storeLocation, MasterFileName);
            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            if (_persistenceManager.FileExists(masterFilePath))
            {
                var storeOffset = GetLatestStorePositionFromMasterFile(masterFilePath);
                if (_persistenceManager.FileExists(dataFilePath))
                {
                    var store = ReadObject<Store>(dataFilePath, storeOffset);
                    store.DirectoryPath = storeLocation;
                    store.IsReadOnly = readOnly;
                    return store;
                }
                throw new StoreManagerException(storeLocation, "Data file not found");
            }
            throw new StoreManagerException(storeLocation, "Master file not found");
        }

        public IStore OpenStore(string storeLocation, ulong storeOffset)
        {
            var masterFilePath = Path.Combine(storeLocation, MasterFileName);
            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            if (_persistenceManager.FileExists(masterFilePath))
            {
                if (_persistenceManager.FileExists(dataFilePath))
                {
                    var store = ReadObject<Store>(dataFilePath, storeOffset);
                    store.DirectoryPath = storeLocation;
                    store.IsReadOnly = true;
                    return store;
                }
                throw new StoreManagerException(storeLocation, "Data file not found");
            }
            throw new StoreManagerException(storeLocation, "Master file not found");
        }

        public bool DoesStoreExist(string storeLocation)
        {
            return _persistenceManager.DirectoryExists(storeLocation);
        }

        public void DeleteStore(string storeLocation)
        {
            // var serverCore = ServerCoreManager.GetServerCore(Configuration.StoreLocation);
            // serverCore.ShutdownStore(storeLocation.Substring(Configuration.StoreLocation.Length + 1), true);
            if (_persistenceManager.DirectoryExists(storeLocation))
            {
                _persistenceManager.DeleteDirectory(storeLocation);
            }
            else
            {
                throw new StoreManagerException(storeLocation, "Store does not exist");
            }
        }
        
        private static Type GetObjectType(ulong typeId)
        {
            return PersistantTypeIdentifiers.Keys.ToList()[(int)typeId];
        }

        public void ConsolidateStore(Store store, string storeLocation, Guid jobId)
        {
            // delete consolidate file if for some reason it was still there
            if (_persistenceManager.FileExists(store.StoreConsolidateFile))
            {
                _persistenceManager.DeleteFile(store.StoreConsolidateFile);
            }

            // var inputStream = _persistenceManager.GetInputStream(store.StoreDataFile);
            var inputStream = store.InputDataStream;
            var outputStream = _persistenceManager.GetOutputStream(store.StoreConsolidateFile, FileMode.CreateNew);
            ulong offset = 0;

            try
            {
                using (var writer = new BinaryWriter(outputStream))
                {
                    var objectLocationManager = store.ObjectLocationManager;
                    foreach (var container in objectLocationManager.Containers)
                    {
                        foreach (var objLoc in container.ObjectOffsets)
                        {
                            // load object
                            var objType = GetObjectType(objLoc.Type);
                            if (objType.Equals(typeof (Store)))
                            {
                                // dont write the store.
                                continue;
                            }

                            var obj = ReadObject(inputStream, objLoc.Offset, GetObjectType(objLoc.Type));

                            // save object and update location manager
                            var bytes = obj.Save(writer, offset);

                            // manage offsets
                            objectLocationManager.SetObjectOffset(obj.ObjectId, offset,
                                                                  PersistantTypeIdentifiers[obj.GetType()], 1);
                            offset += (ulong) bytes;
                        }
                    }

                    // write the store
                    objectLocationManager.SetObjectOffset(store.ObjectId, offset,
                                                          PersistantTypeIdentifiers[store.GetType()], 1);
                    store.Save(writer, offset);

                    // delete store
                    inputStream.Close();
                    inputStream.Dispose();
                    _persistenceManager.DeleteFile(store.StoreDataFile);
                }

                // rename to new
                _persistenceManager.RenameFile(store.StoreConsolidateFile, store.StoreDataFile);

                // update the masterfile
                AppendCommitPoint(storeLocation, new CommitPoint(offset, 0ul, DateTime.UtcNow, jobId), true);

            }
            catch (Exception ex)
            {
                Logging.LogError(new BrightstarEventId(), "Unable to consolidate store. Error was " + ex.Message);
                throw;
            }
            finally
            {
                _persistenceManager.DeleteFile(store.StoreConsolidateFile);
            }
        }

        public void StoreObjects(IEnumerable<IPersistable> objects, ObjectLocationManager objectLocationManager, string storeLocation)
        {
            Stream fs = null;
            try
            {
                var fileName = Path.Combine(storeLocation, DataFileName);
                if (!_persistenceManager.FileExists(fileName))
                {
                    throw new StoreManagerException(storeLocation, "Data file not found");
                }

                fs = _persistenceManager.GetOutputStream(fileName, FileMode.Append);
                var offset = (ulong) fs.Length;
                using (var writer = new BinaryWriter(fs))
                {
                    fs = null;
                    foreach (var obj in objects)
                    {
                        try
                        {
                            // mark each as no longer pending to commit
                            obj.ScheduledForCommit = false;

                            // store object
                            var bytes = obj.Save(writer, offset);

                            // manage offsets
                            objectLocationManager.SetObjectOffset(obj.ObjectId, offset,
                                                                  PersistantTypeIdentifiers[obj.GetType()], 1);
                            offset += (ulong) bytes;
                        }
                        catch (Exception ex)
                        {
#if WINDOWS_PHONE || PORTABLE
                            Logging.LogError(BrightstarEventId.ObjectWriteError,
                 "Error writing object {0} with Id {1} and type {2}. Cause: {3}.",
                 obj, obj.ObjectId, obj.GetType().FullName, ex);
#else
                            Logging.LogError(BrightstarEventId.ObjectWriteError,
                                             "Error writing object {0} with Id {1} and type {2}. Cause: {3}. Call stack: {4}",
                                             obj, obj.ObjectId, obj.GetType().FullName, ex,
                                             Environment.StackTrace);
#endif
                            throw;
                        }
                    }
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ObjectWriteError, "Error storing objects {0} {1}", ex.Message, ex.StackTrace);
                throw;
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
        }

        public TObjType ReadObject<TObjType>(Stream dataStream, ulong offset) where TObjType : class, IStorable
        {
            dataStream.Seek((long)offset, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(dataStream);
            var obj = Activator.CreateInstance<TObjType>();
            obj.Read(binaryReader);
            return obj;
        }

        public IPersistable ReadObject(Stream dataStream, ulong offset, Type type)
        {
            dataStream.Seek((long)offset, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(dataStream);
            var obj = Activator.CreateInstance(type) as IPersistable;            
            obj.Read(binaryReader);
            return obj;
        }



        public TObjType ReadObject<TObjType>(string fileName, ulong offset) where TObjType : class, IStorable
        {
            Stream fs = null;
            try
            {
                fs = _persistenceManager.GetInputStream(fileName);
                fs.Seek((long) offset, SeekOrigin.Begin);
                using (var binaryReader = new BinaryReader(fs))
                {
                    fs = null;
                    var obj = Activator.CreateInstance<TObjType>();
                    obj.Read(binaryReader);
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ObjectReadError, 
                    "Error reading object of type {0} from {1} @ {2}. Cause: {3} ", 
                    typeof(TObjType).FullName, fileName, offset, ex);
                throw new StoreReadException(
                    String.Format("Error reading object of type {0} from {1} @ {2}.",
                                  typeof (TObjType).FullName, fileName, offset),
                    ex);
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
        }

        public virtual ITransactionLog GetTransactionLog(string storeLocation)
        {
            return new PersistentTransactionLog(_persistenceManager, storeLocation);
        }

        public virtual IStoreStatisticsLog GetStatisticsLog(string storeLocation)
        {
            return new PersistentStatisticsLog(_persistenceManager, storeLocation);
        }

        public void CreateSnapshot(string srcStoreLocation, string destStoreLocation, PersistenceType storePersistenceType,
                                   ulong commitPointId = StoreConstants.NullUlong)
        {
            throw new NotImplementedException();
        }

        public MasterFile GetMasterFile(string storeLocation)
        {
            return MasterFile.Open(_persistenceManager, storeLocation);
        }

        public IPageStore CreateConsolidationStore(string storeLocation)
        {
            throw new NotImplementedException();
        }

        public void ActivateConsolidationStore(string storeLocation)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CommitPoint> GetCommitPoints(string storeLocation)
        {
            var masterFileLocation = Path.Combine(storeLocation, MasterFileName);
            var pos = 1;
            using (var fs = _persistenceManager.GetInputStream(masterFileLocation))
            {
                while ((pos * CommitPoint.RecordSize) + MasterfileHeaderSize <= fs.Length)
                {
                    fs.Seek(-(pos*CommitPoint.RecordSize), SeekOrigin.End);
                    var commitPoint = CommitPoint.Load(fs);
                    pos++;
                    yield return commitPoint;
                }
            }
        }

        public CommitPoint GetCommitPoint(string storeLocation, ulong offset)
        {
            var masterFileLocation = Path.Combine(storeLocation, MasterFileName);
            using (var fs = _persistenceManager.GetInputStream(masterFileLocation))
            {
                fs.Seek((long)offset, SeekOrigin.Begin);
                var commitPoint = CommitPoint.Load(fs);
                return commitPoint;
            }
        }

        public Stream GetInputStream(string storeLocation)
        {
            return _persistenceManager.GetInputStream(storeLocation);
        }

  
        #endregion

        /// <summary>
        /// Adds the specified commit point to the master file
        /// </summary>
        /// <param name="storeLocation">The path to the store directory</param>
        /// <param name="commitPoint">The commit point to add</param>
        /// <param name="overwrite">Specifies if the master file should be overwritten with just this commit point. Defaults to false.</param>
        /// <remarks>The <paramref name="overwrite"/> parameter is provided to enable store consolidation operations.</remarks>
        public void AppendCommitPoint(string storeLocation, CommitPoint commitPoint, bool overwrite = false)
        {
            Logging.LogDebug("AbstractStoreManager.AppendCommitPoint {0}, overwrite={1}", storeLocation, overwrite);
            var masterFileLocation = Path.Combine(storeLocation, MasterFileName);
            if (overwrite)
            {
                // Use a file delete operation to try to avoid file locking issues.
                Logging.LogDebug("AbstractStoreManager: Overwrite enabled. Deleting existing master file");
                _persistenceManager.DeleteFile(masterFileLocation);
            }
            if (!_persistenceManager.FileExists(masterFileLocation))
            {
                Logging.LogDebug("AbstractStoreManager: Master file not found at {0}. Creating new master file.", masterFileLocation);
                _persistenceManager.CreateFile(masterFileLocation);
            }
            using (var fs = _persistenceManager.GetOutputStream(masterFileLocation, FileMode.Append))
            {
                var binaryWriter = new BinaryWriter(fs);
                ulong val = 0;
                if (_persistenceManager.GetFileLength(masterFileLocation) == 0)
                {
                    // new master file so add header
                    for (int i = 0; i < MasterfileHeaderLongCount;i++)
                    {
                        binaryWriter.Write(val);                        
                    }
                }
                commitPoint.Save(fs);
            }
            Logging.LogDebug("AbstractStoreManager.UpdateMsterFile {0} completed.", storeLocation);
        }

        internal ulong GetLatestStorePositionFromMasterFile(string masterFilePath)
        {
            try
            {
                Logging.LogDebug("Retrieving latest store position from masterfile : {0}",masterFilePath);
                using (var fs = _persistenceManager.GetInputStream(masterFilePath))
                {
                    Logging.LogDebug("Masterfile stream length is {0}",  fs.Length);
                    Logging.LogDebug("Attempting to seek to {0} bytes from end of stream.", CommitPoint.RecordSize);
                    fs.Seek(-CommitPoint.RecordSize, SeekOrigin.End);
                    Logging.LogDebug("Seek completed ok. Attempting to load commit point");
                    var commitPoint = CommitPoint.Load(fs);
                    Logging.LogDebug("Commit point load completed OK. Returning commit point offset as {0}", commitPoint.LocationOffset);
                    return commitPoint.LocationOffset;
                }
            }
            catch (InvalidCommitPointException icp)
            {
                Logging.LogInfo("Caught InvalidCommitPointException: {0}", icp);
                // start reading from the start of the file until we get to the dud one.
                // truncate the file at this point. log it and try again.
                var count = 0;
                CommitPoint validCommitPoint = null;

                const int headerSize = MasterfileHeaderLongCount * 8;
                while (true)
                {
                    using (var fs = _persistenceManager.GetInputStream(masterFilePath))
                    {
                        try
                        {
                            Logging.LogInfo("Reading commit point at " + CommitPoint.RecordSize * count);
                            fs.Seek((CommitPoint.RecordSize * count) + headerSize, SeekOrigin.Begin);
                            var commitPoint = CommitPoint.Load(fs);
                            validCommitPoint = commitPoint;
                            count++;
                        }
                        catch (BrightstarInternalException)
                        {
                            var startOfBadCommit = (CommitPoint.RecordSize * count) + headerSize;
                            Logging.LogInfo("Truncating file at " + startOfBadCommit);

                            // truncate file.
                            using (var stream = _persistenceManager.GetOutputStream(masterFilePath, FileMode.Truncate))
                            {
                                stream.SetLength(startOfBadCommit);
                            }

                            // return last good commit
                            return validCommitPoint.LocationOffset;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new BrightstarInternalException("Error while trying to recover to last valid commit point.", ex);
            }
        }
    }
}
