using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if PORTABLE 
using BrightstarDB.Portable.Compatibility;
using Array = BrightstarDB.Portable.Compatibility.Array;
#endif

namespace BrightstarDB.Storage
{
    internal class MasterFile
    {
        /// <summary>
        /// The default name for the master file
        /// </summary>
        public const string MasterFileName = "master.bs";

        /// <summary>
        /// The magic number written at the start of the header
        /// </summary>
        public const uint MagicNumber = 0x1FADE23;

        /// <summary>
        /// The current store format version
        /// </summary>
        public const int CurrentStoreFormatVersion = 1;

        /// <summary>
        /// Number of bytes reserved at the start of the master file for header information
        /// </summary>
        public const int HeaderSize = 256;

        private IPersistenceManager _persistenceManager;
        private string _directoryPath;
        private string _masterFilePath;

        /// <summary>
        /// Get the type of store
        /// </summary>
        public StoreType StoreType { get; private set; }

        /// <summary>
        /// Get the type of persistence used by the store data files
        /// </summary>
        public PersistenceType PersistenceType { get; private set; }

        /// <summary>
        /// Get the format version for the store data files
        /// </summary>
        public int StoreFormatVersion { get; private set; }

        /// <summary>
        /// Get the GUID identifier for the store set that this store belongs to
        /// </summary>
        /// <remarks>Multiple stores can belong to the same store set. E.g. when sharded</remarks>
        public Guid StoreSetId { get; private set; }

        /// <summary>
        /// Get the GUID identifier for the store
        /// </summary>
        /// <remarks>Each store has a unique store identifier generated when the store is created</remarks>
        public Guid StoreId { get; private set; }

        public static MasterFile Create(IPersistenceManager persistenceManager, string directoryPath,
                                        StoreConfiguration storeConfiguration, Guid storeSetId)
        {
            var masterFilePath = Path.Combine(directoryPath, MasterFileName);
            if (persistenceManager.FileExists(masterFilePath))
            {
                throw new StoreManagerException(directoryPath, "Master file already exists");
            }
            persistenceManager.CreateFile(masterFilePath);
            using (var stream = persistenceManager.GetOutputStream(masterFilePath, FileMode.Open))
            {
                var newMaster = new MasterFile(persistenceManager, directoryPath, masterFilePath, storeConfiguration,
                                               storeSetId);
                newMaster.Save(stream);
                return newMaster;
            }
        }

        public static MasterFile Open(IPersistenceManager persistenceManager, string directoryPath)
        {
            var masterFilePath = Path.Combine(directoryPath, MasterFileName);
            var mf = new MasterFile(persistenceManager, directoryPath, masterFilePath,
                                    StoreConfiguration.DefaultStoreConfiguration, Guid.Empty);
            try
            {
                using (var mfStream = persistenceManager.GetInputStream(masterFilePath))
                {
                    mf.Load(mfStream);
                }
            }
            catch (Exception ex)
            {
                throw new StoreManagerException(directoryPath, "Error opening master file. " + ex.Message);
            }
            return mf;
        }

        /*
        public MasterFile(IPersistenceManager persistenceManager, string directoryPath)
        {
            _persistenceManager = persistenceManager;
            _directoryPath = directoryPath;
            _masterFilePath = Path.Combine(directoryPath, MasterFileName);
            if (!File.Exists(_masterFilePath))
            {
                throw new StoreManagerException(directoryPath, "Master file not found");
            }
            try
            {
                using (var stream = _persistenceManager.GetInputStream(_masterFilePath))
                {
                    Load(stream);
                }
            }
            catch (StoreManagerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StoreManagerException(directoryPath,
                                                String.Format("Cannot read master file. Cause: {0}", ex.Message));
            }
        }
         */

        private MasterFile(IPersistenceManager persistenceManager, string directoryPath, string masterFilePath,
                           StoreConfiguration storeConfiguration, Guid storeSetId)
        {
            _persistenceManager = persistenceManager;
            _directoryPath = directoryPath;
            _masterFilePath = masterFilePath;
            StoreType = StoreType.Standard;
            PersistenceType = storeConfiguration.PersistenceType;
            StoreFormatVersion = CurrentStoreFormatVersion;
            StoreSetId = storeSetId;
            StoreId = Guid.NewGuid();
        }

        private void Save(Stream outputStream)
        {
            var header = new byte[HeaderSize];
            BitConverter.GetBytes(MagicNumber).CopyTo(header, 0);
            header[4] = (byte) StoreType;
            header[5] = (byte) PersistenceType;
            BitConverter.GetBytes(StoreFormatVersion).CopyTo(header, 6);
            StoreSetId.ToByteArray().CopyTo(header, 10);
            StoreId.ToByteArray().CopyTo(header, 26);
            outputStream.Write(header, 0, HeaderSize);
        }

        private void Load(Stream inputStream)
        {
            var header = new byte[HeaderSize];
            inputStream.Read(header, 0, HeaderSize);
            uint magicNumber = BitConverter.ToUInt32(header, 0);
            if (magicNumber != MagicNumber)
            {
                throw new Exception("Invalid master file. Magic number does not match expected value");
            }
            StoreType = (StoreType) header[4];
            PersistenceType = (PersistenceType) header[5];
            StoreFormatVersion = BitConverter.ToInt32(header, 6);
            var guidBytes = new byte[16];
            Array.Copy(header, 10, guidBytes, 0, 16);
            StoreSetId = new Guid(guidBytes);
            Array.Copy(header, 26, guidBytes, 0, 16);
            StoreId = new Guid(guidBytes);
        }

        public IEnumerable<CommitPoint> GetCommitPoints()
        {
            using (var inputStream = _persistenceManager.GetInputStream(_masterFilePath))
            {
                if (inputStream.Length <= HeaderSize)
                {
                    yield break;
                }
                long recordPos = inputStream.Length - CommitPoint.RecordSize;
                while (recordPos >= HeaderSize)
                {
                    inputStream.Seek(recordPos, SeekOrigin.Begin);
                    CommitPoint commitPoint = null;
                    try
                    {
                        commitPoint = CommitPoint.Load(inputStream);
                    }
                    catch (InvalidCommitPointException)
                    {
                        Logging.LogError(BrightstarEventId.CommitPointReadError,
                                         "Could not read commit point at offset {0} in master file '{1}'",
                                         inputStream.Position, _masterFilePath);
                    }
                    if (commitPoint != null)
                    {
                        yield return commitPoint;
                    }
                    recordPos = recordPos - CommitPoint.RecordSize;
                }
            }
        }

        public CommitPoint GetCommitPoint(ulong commitPointLocation)
        {
            if (commitPointLocation < HeaderSize ||
                ((commitPointLocation - HeaderSize)%(ulong) CommitPoint.RecordSize) != 0)
            {
                throw new ArgumentException("Invalid commit point offset", "commitPointLocation");
            }
            using (var inputStream = _persistenceManager.GetInputStream(_masterFilePath))
            {
                inputStream.Seek((long) commitPointLocation, SeekOrigin.Begin);
                return CommitPoint.Load(inputStream);
            }
        }

        public CommitPoint GetLatestCommitPoint(int skipRecords = 0)
        {
            try
            {
                using (var stream = _persistenceManager.GetInputStream(_masterFilePath))
                {
                    if (stream.Length == HeaderSize)
                    {
                        if (skipRecords > 0)
                        {
                            throw new StoreManagerException(_directoryPath,
                                                            "Master file is corrupt and no valid commit point information could be read.");
                        }
                        return null;
                    }
                    long recordStart;
                    if ((stream.Length - HeaderSize)%CommitPoint.RecordSize != 0)
                    {
                        recordStart = stream.Length - ((stream.Length - HeaderSize)%CommitPoint.RecordSize) -
                                      ((skipRecords + 1)*CommitPoint.RecordSize);
                    }
                    else
                    {
                        recordStart = stream.Length-((skipRecords + 1)*CommitPoint.RecordSize);
                    }
                    stream.Seek(recordStart, SeekOrigin.Begin);
                    var commitPoint = CommitPoint.Load(stream);
                    if (skipRecords > 0)
                    {
                        // We skipped over one or more invalid records, so we record a higher next transaction id number in the commit point we return
                        commitPoint.NextCommitNumber = commitPoint.NextCommitNumber + (ulong) skipRecords;
                        // Master file is not truncated here like in the old implementation, 
                        // because this could potentially interfere with a master file write operation for a store commit
                        // instead we will need to just always skip corrupt transactions.
                    }
                    return commitPoint;
                }
            }
            catch (InvalidCommitPointException)
            {
                Logging.LogWarning(BrightstarEventId.CommitPointReadError,
                                   String.Format(
                                       "Failed to read valid commit point from master file '{0}'. Rewinding to previous commit point",
                                       _masterFilePath));
                return GetLatestCommitPoint(skipRecords + 1);
            }
        }

        public ulong AppendCommitPoint(CommitPoint newCommitPoint, bool overwrite = false)
        {
            if (overwrite)
            {
                Logging.LogDebug("AppendCommitPoint: Overwrite requested. Deleting existing master file at '{0}'",
                                 _masterFilePath);
                _persistenceManager.DeleteFile(_masterFilePath);
            }
            if (!_persistenceManager.FileExists(_masterFilePath))
            {
                Logging.LogDebug("AppendCommitPoint: Master file not found at '{0}'. Creating new master file.",
                                 _masterFilePath);
                using (var stream = _persistenceManager.GetOutputStream(_masterFilePath, FileMode.Create))
                {
                    Save(stream);
                }
            }
            using (var stream = _persistenceManager.GetOutputStream(_masterFilePath, FileMode.Open))
            {
                stream.Seek(0, SeekOrigin.End);
                newCommitPoint.Save(stream);
                return newCommitPoint.CommitNumber;
            }
        }

    }
}
