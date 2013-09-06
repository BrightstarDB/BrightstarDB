using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.Statistics
{
    internal class PersistentStatisticsLog : IStoreStatisticsLog
    {
        private readonly IPersistenceManager _persistenceManager;
        private readonly string _storeLocation;

        public PersistentStatisticsLog(IPersistenceManager persistenceManager, string storeLocation)
        {
            _persistenceManager = persistenceManager;
            _storeLocation = storeLocation;
        }

        private string GetStatisticsLogFile()
        {
            return Path.Combine(_storeLocation, "stats.bs");
        }

        private string GetStatisticsHeaderFile()
        {
            return Path.Combine(_storeLocation, "statsheaders.bs");
        }

        public IEnumerable<StoreStatistics> GetStatistics()
        {
            if (!_persistenceManager.FileExists(GetStatisticsHeaderFile())) yield break;
            using (var headerStream = _persistenceManager.GetInputStream(GetStatisticsHeaderFile()))
            {
                using (var headerReader = new BinaryReader(headerStream))
                {
                    using (
                        var recordReader =
                            new StreamReader(_persistenceManager.GetInputStream(GetStatisticsLogFile())))
                    {
                        long offset = headerStream.Length - StoreStatisticsHeaderRecord.RecordSize;
                        while (offset >= 0)
                        {
                            headerReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                            var header = StoreStatisticsHeaderRecord.Load(headerReader);
                            recordReader.BaseStream.Seek(header.StartOffset, SeekOrigin.Begin);
                            var record = StoreStatisticsRecord.Load(recordReader);
                            yield return
                                new StoreStatistics(header.CommitNumber, header.Timestamp, record.TotalTripleCount,
                                                    record.PredicateTripleCounts);
                            offset -= StoreStatisticsHeaderRecord.RecordSize;
                        }
                    }
                }
            }
        }

        public void AppendStatistics(StoreStatistics statistics)
        {
            using (
                var headerWriter =
                    new BinaryWriter(_persistenceManager.GetOutputStream(GetStatisticsHeaderFile(), FileMode.Append)))
            {
                using (
                    var recordWriter =
                        new StreamWriter(_persistenceManager.GetOutputStream(GetStatisticsLogFile(),
                                                                             FileMode.Append)))
                {
                    var recordStart = recordWriter.BaseStream.Position;
                    var record = new StoreStatisticsRecord(statistics.CommitNumber, statistics.TripleCount,
                                                           statistics.PredicateTripleCounts);
                    var header = new StoreStatisticsHeaderRecord(statistics.CommitNumber, statistics.CommitTime,
                                                                 recordStart);
                    record.Save(recordWriter);
                    header.Save(headerWriter);
                    recordWriter.Flush();
                    headerWriter.Flush();
                    recordWriter.Close();
                    headerWriter.Close();
                }
            }
        }
    }
}