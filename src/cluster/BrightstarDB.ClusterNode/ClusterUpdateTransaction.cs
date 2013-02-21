using System;
using System.IO;
using System.Text;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterNode
{
    public class ClusterUpdateTransaction : ClusterTransaction
    {
        public string Preconditions { get; set; }
        public string Inserts { get; set; }
        public string Deletes { get; set; }

        public static ClusterUpdateTransaction FromMessage(Message msg)
        {
            var args = msg.Args.Split(' ');
            var txn = new ClusterUpdateTransaction
                          {
                              StoreId = args[0],
                              JobId = Guid.Parse(args[1]),
                              PrevTxnId = Guid.Parse(args[2]),
                          };
            using(var reader = msg.GetContentReader())
            {
                txn.Preconditions = ReadSection(reader);
                txn.Deletes = ReadSection(reader);
                txn.Inserts = ReadSection(reader);
            }
            return txn;
        }

        public override Message AsMessage()
        {
            const string header = "txn";
            var args = String.Join(" ", StoreId, JobId, PrevTxnId);
            var msg = new Message(header, args);
            using(var writer = msg.GetContentWriter())
            {
                if (!String.IsNullOrEmpty(Preconditions))
                {
                    writer.WriteLine(Preconditions);
                }
                writer.WriteLine("||");
                if (!String.IsNullOrEmpty(Deletes))
                {
                    writer.WriteLine(Deletes);
                }
                writer.WriteLine("||");
                if (!String.IsNullOrEmpty(Inserts))
                {
                    writer.WriteLine(Inserts);
                }
            }
            return msg;
        }

        private static string ReadSection(TextReader reader)
        {
            var section = new StringBuilder();
            string line = reader.ReadLine();
            while (line != null && !line.Equals("||"))
            {
                section.AppendLine(line);
                line = reader.ReadLine();
            }
            return section.ToString();
        }
    }
}