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

        public StoreStatisticsRecord(ulong commitNumber, ulong tripleCount,
                                     Dictionary<string, ulong> predicateTripleCounts)
        {
            CommitNumber = commitNumber;
            TotalTripleCount = tripleCount;
            PredicateTripleCounts = predicateTripleCounts;
        }

        private StoreStatisticsRecord()
        {
            PredicateTripleCounts = new Dictionary<string, ulong>();
        }

        public static StoreStatisticsRecord Load(TextReader reader)
        {
            var ret = new StoreStatisticsRecord();
            ret.Read(reader);
            return ret;
        }

        public void Save(TextWriter writer)
        {
            writer.WriteLine(CommitNumber.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(TotalTripleCount.ToString(CultureInfo.InvariantCulture));
            foreach (var entry in PredicateTripleCounts)
            {
                writer.WriteLine("{0:G},{1}", entry.Value, entry.Key);
            }
            writer.WriteLine("END");
        }

        private void Read(TextReader reader)
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

            while (true)
            {
                line = reader.ReadLine();
                if (String.IsNullOrEmpty(line) || line.Equals("END")) break;
                var splitIx = line.IndexOf(',');
                if (UInt64.TryParse(line.Substring(0, splitIx), out tripleCount))
                {
                    string predicate = line.Substring(splitIx + 1);
                    PredicateTripleCounts[predicate] = tripleCount;
                }
            }
        }
    }
}
