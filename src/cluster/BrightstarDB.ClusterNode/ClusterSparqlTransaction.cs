using System;
using System.IO;
using System.Text;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterNode
{
    public class ClusterSparqlTransaction : ClusterTransaction
    {
        public string Expression { get; set; }

        public static ClusterSparqlTransaction FromMessage(Message msg)
        {
            var args = msg.Args.Split(' ');
            var txn = new ClusterSparqlTransaction
                          {
                              StoreId = args[0],
                              JobId = Guid.Parse(args[1]),
                              PrevTxnId = Guid.Parse(args[2]),
                          };
            using(var reader = msg.GetContentReader())
            {
                txn.Expression = ReadSection(reader);
            }
            return txn;
        }

        public override Message AsMessage()
        {
            const string header = "spu";
            var args = String.Join(" ", StoreId, JobId, PrevTxnId);
            var msg = new Message(header, args);
            using(var writer = msg.GetContentWriter())
            {
                if (!String.IsNullOrEmpty(Expression))
                {
                    writer.WriteLine(Expression);
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