using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB;
using BrightstarDB.Client;

namespace RestClientTester
{
    public class SingleThreadStressTest  : RestClientTest
    {
        private IBrightstarService _client;
        private bool _endQueries;
        private string _storeName;
        private List<string> _importJobs;
        private System.Collections.Concurrent.ConcurrentQueue<Tuple<long, int>> _queryTimes; 
        private List<Task> _queryTasks;
        public SingleThreadStressTest(ConnectionString connectionString) : base(connectionString)
        {
            _client = BrightstarService.GetClient(connectionString);
            _storeName = connectionString.StoreName;
            _importJobs = new List<string>();
            _queryTimes = new ConcurrentQueue<Tuple<long, int>>();
            _queryTasks = new List<Task>();
        }

        public override void Run()
        {
            /*
            _storeName = "single-thread-test";
            if (_client.DoesStoreExist(_storeName))
            {
                _client.DeleteStore(_storeName);
            }
            _client.CreateStore(_storeName);
             */
            var importTask = new Task(AddData);
            for (int i = 0; i < 5; i++)
            {
                var queryTask = new Task(QueryData);
                _queryTasks.Add(queryTask);
                queryTask.Start();
            }
            importTask.RunSynchronously();
            _endQueries = true;
            Task.WaitAll(_queryTasks.ToArray());
            if (File.Exists("querytimes.txt")) File.Delete("querytimes.txt");
            using(var writer= new StreamWriter(File.OpenWrite("querytimes.txt")))
            {
                foreach(var qt in _queryTimes)
                {
                    writer.WriteLine("{0},{1}", qt.Item1, qt.Item2);
                }
                writer.Close();
            }
            Console.WriteLine("Ran {0} queries during import. Query times written to querytimes.txt", _queryTimes.Count);
        }

        private void AddData()
        {
            var rng = new Random();
            for (int batch = 0; batch < 25; batch++)
            {
                Console.WriteLine("Creating import batch #" + batch);
                var addTriples = new StringBuilder();
                for (int personCount = 0; personCount < 1000; personCount++)
                {
                    int personNum = batch*1000 + personCount;
                    string personId = String.Format("<http://example.org/person/{0}>", personNum);
                    addTriples.AppendFormat(
                        "{0} <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://purl.org/foaf/0.1/Person> .\n",
                        personId);
                    addTriples.AppendFormat(
                        "{0} <http://purl.org/foaf/0.1/name> \"Person #{1}\" .\n", personId, personNum
                        );
                    if (personNum > 10)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            addTriples.AppendFormat(
                                "{0} <http://purl.org/foaf/0.1/knows> <http://example.org/person/{1}> .\n",
                                personId, personNum - (i + 1));
                        }
                    }
                }
                Console.WriteLine("Submitting import batch #" + batch);
                _importJobs.Add(_client.ExecuteTransaction(_storeName, null, null, addTriples.ToString(), false).JobId);
            }
            while (_importJobs.Count > 0)
            {
                var jobId = _importJobs[0];
                Console.WriteLine("Waiting for job {0}...", jobId);
                IJobInfo jobInfo;
                do
                {
                    jobInfo = _client.GetJobInfo(_storeName, jobId);
                    Thread.Sleep(1000);
                } while (!(jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors));
                if (jobInfo.JobCompletedOk)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\tJob completed OK!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\tJob FAILED.");
                    Console.ResetColor();
                }
                _importJobs.RemoveAt(0);
            }
        }

        private void QueryData()
        {
            try
            {
                const string queryTemplate =
                    "SELECT ?friend WHERE {{ {{<{0}> <http://purl.org/foaf/0.1/knows> ?friend }} UNION {{ ?friend <http://purl.org/foaf/0.1/knows> <{0}> }} }}";
                var rng = new Random();
                var timer = new Stopwatch();
                do
                {
                    try
                    {
                        var randomPerson = String.Format("http://example.org/person/{0}", rng.Next(25000));
                        timer.Restart();
                        var query = String.Format(queryTemplate, randomPerson);
                        int rowCount;
                        using (var stream = _client.ExecuteQuery(_storeName, query))
                        {
                            var resultDoc = XDocument.Load(stream);
                            rowCount = resultDoc.SparqlResultRows().Count();
                        }
                        timer.Stop();
                        _queryTimes.Enqueue(new Tuple<long, int>(timer.ElapsedMilliseconds, rowCount));
                    } catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Query failed: " + ex.Message);
                        Console.ResetColor();
                        _queryTimes.Enqueue(new Tuple<long, int>(-1, 0));
                    }
                } while (!_endQueries);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
