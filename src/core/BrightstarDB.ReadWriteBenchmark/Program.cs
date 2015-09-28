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
        private static void Main(string[] args)
        {
            var opts = new BenchmarkArgs();
            if (CommandLine.Parser.ParseArgumentsWithUsage(args, opts))
            {
                
                if (!string.IsNullOrEmpty(opts.LogFilePath))
                {
                    BenchmarkLogging.EnableFileLogging(opts.LogFilePath);
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
        }
    }
}
