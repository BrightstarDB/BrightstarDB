using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BrightstarDB.Storage.Statistics
{
    internal class StoreStatisticsRecord
    {
        public ulong CommitNumber { get; private set; }
        public ulong TotalTripleCount { get; private set; }
        public Dictionary<string, ulong> PredicateTripleCounts { get; private set; }
        public Dictionary<string, PredicateStatistics> PredicateStatistics { get; private set; } 

        public StoreStatisticsRecord(ulong commitNumber, ulong tripleCount,
            Dictionary<string, PredicateStatistics> predicateStatistics)
        {
            CommitNumber = commitNumber;
            TotalTripleCount = tripleCount;
            PredicateTripleCounts = null;
            PredicateStatistics = predicateStatistics;
        }

        private StoreStatisticsRecord()
        {
        }

        public static StoreStatisticsRecord Load(TextReader reader, int recordVersion)
        {
            var ret = new StoreStatisticsRecord();
            ret.Read(reader, recordVersion);
            return ret;
        }

        public void Save(TextWriter writer)
        {
            writer.WriteLine(CommitNumber.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(TotalTripleCount.ToString(CultureInfo.InvariantCulture));
            foreach (var entry in PredicateStatistics)
            {
                writer.WriteLine("{0},{1:G},{2:G},{3:G}", entry.Key, entry.Value.TripleCount, entry.Value.DistinctSubjectCount, entry.Value.DistinctObjectCount);
            }
            writer.WriteLine("END");
        }

        private void Read(TextReader reader, int recordVersion)
        {
            var line = reader.ReadLine();
            ulong commitNumber, tripleCount;
            if (!UInt64.TryParse(line, out commitNumber))
            {
                throw new BrightstarInternalException(
                    "Error reading statistics record. Could not parse commit number line.");
            }
            CommitNumber = commitNumber;

            line = reader.ReadLine();
            if (!UInt64.TryParse(line, out tripleCount))
            {
                throw new BrightstarInternalException(
                    "Error reading statistics record. Could not parse triple count line.");
            }
            TotalTripleCount = tripleCount;

            if (recordVersion == 1)
            {
                PredicateTripleCounts = new Dictionary<string, ulong>();
            }
            else
            {
                PredicateStatistics = new Dictionary<string, PredicateStatistics>();
            }

            while (true)
            {
                line = reader.ReadLine();
                if (String.IsNullOrEmpty(line) || line.Equals("END")) break;
                if (recordVersion == 1)
                {
                    var splitIx = line.IndexOf(',');
                    if (UInt64.TryParse(line.Substring(0, splitIx), out tripleCount))
                    {
                        string predicate = line.Substring(splitIx + 1);
                        PredicateTripleCounts[predicate] = tripleCount;
                    }
                }
                else
                {
                    ulong predicateTripleCount, distinctSubjectCount, distinctObjectCount;
                    var tokens = line.Split(',');
                    if (ulong.TryParse(tokens[1], out predicateTripleCount) &&
                        ulong.TryParse(tokens[2], out distinctSubjectCount) &&
                        ulong.TryParse(tokens[3], out distinctObjectCount))
                    {
                        PredicateStatistics[tokens[0]] = new PredicateStatistics(predicateTripleCount, distinctSubjectCount, distinctObjectCount);
                    }
                }
            }
        }
    }
}
