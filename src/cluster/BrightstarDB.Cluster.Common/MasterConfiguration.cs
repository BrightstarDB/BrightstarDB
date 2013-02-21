using System;
using System.IO;
using System.Text;

namespace BrightstarDB.Cluster.Common
{
    public class MasterConfiguration
    {
        public int WriteQuorum { get; set; }

        public static MasterConfiguration FromMessage(Message msg)
        {
            if (!msg.Header.StartsWith("master", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Message is not a 'master' message");
            }
            var ret = new MasterConfiguration();
            using (var reader = msg.GetContentReader())
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    var nvp = line.Split(':');
                    if (nvp.Length == 2)
                    {
                        switch (nvp[0].ToLowerInvariant())
                        {
                            case "writequorum":
                                ret.WriteQuorum = Int32.Parse(nvp[1]);
                                break;
                        }
                    }
                }
            }
            return ret;
        }

        public void WriteTo(TextWriter writer)
        {
            writer.WriteLine("writeQuorum:{0:G}", WriteQuorum);
        }

    }
}