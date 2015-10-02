using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightstarDB.ReadWriteBenchmark
{
    internal class Program
    {
        public static TraceListener BrightstarListener;
        private static void Main(string[] args)
        {
            var opts = new BenchmarkArgs();
            if (CommandLine.Parser.ParseArgumentsWithUsage(args, opts))
            {
                
                if (!string.IsNullOrEmpty(opts.LogFilePath))
                {
                    BenchmarkLogging.EnableFileLogging(opts.LogFilePath);
                    var logStream = new FileStream(opts.LogFilePath + ".bslog", FileMode.Create);
                    BrightstarListener = new TextWriterTraceListener(logStream);
                    BrightstarDB.Logging.BrightstarTraceSource.Listeners.Add(BrightstarListener);
                    BrightstarDB.Logging.BrightstarTraceSource.Switch.Level = SourceLevels.All;
                }
            }
            else
            {
                var usage = CommandLine.Parser.ArgumentsUsage(typeof (BenchmarkArgs));
                Console.WriteLine(usage);
            }
            var runner = new BenchmarkRunner(opts);
            runner.Run();
            BenchmarkLogging.Close();
            BrightstarDB.Logging.BrightstarTraceSource.Close();
        }
    }
}
