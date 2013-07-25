using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Server;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class StreamTests
    {
        [Test]
        public void TestConnectionStream()
        {
            var connStream = new ConnectionStream();
            var producerStream = new ProducerStream(connStream);
            var consumerStream = new ConsumerStream(connStream);

            var t = new Task(() =>
                                 {
                                     var streamWriter = new StreamWriter(producerStream);
                                     streamWriter.WriteLine("mother fucking stream crap");
                                     streamWriter.Close();
                                 });

            t.Start();

            var sr = new StreamReader(consumerStream);
            var result = sr.ReadToEnd();
            Assert.AreEqual("mother fucking stream crap\r\n", result);

            Task.WaitAll(t);
        }

        [Test]
        public void TestConnectionStreamLotsOfData()
        {
            var connStream = new ConnectionStream();
            var producerStream = new ProducerStream(connStream);
            var consumerStream = new ConsumerStream(connStream);
            var t = new Task(() =>
            {
                var streamWriter = new StreamWriter(producerStream);
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("mother fucking stream crap " + i);
                streamWriter.Close();
            });

            t.Start();

            Thread.Sleep(1000);

            var sr = new StreamReader(consumerStream);                   
            var result = sr.ReadToEnd();
            Assert.IsTrue(result.StartsWith("mother fucking stream crap 0"));
            Assert.IsTrue(result.EndsWith("mother fucking stream crap 999\r\n"));
            
            Task.WaitAll(t);
        }

        [Test]
        public void TestConnectionStreamLotsOfDataAndCheckAllReads()
        {
            var connStream = new ConnectionStream();
            var producerStream = new ProducerStream(connStream);
            var consumerStream = new ConsumerStream(connStream);
            var t = new Task(() =>
            {
                var streamWriter = new StreamWriter(producerStream);
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("mother fucking stream crap " + i);
                streamWriter.Close();
            });

            t.Start();

            Thread.Sleep(1000);

            int j = 0;
            using (var sr = new StreamReader(consumerStream))
            {
                var result = sr.ReadLine();
                Assert.AreEqual(result, "mother fucking stream crap " + j);
                j++;
            }

            Task.WaitAll(t);
        }


        [Test]
        public void TestConnectionClientTermination()
        {
            var connStream = new ConnectionStream();
            var producerStream = new ProducerStream(connStream);
            var consumerStream = new ConsumerStream(connStream);
            var t = new Task(() =>
            {
                var streamWriter = new StreamWriter(producerStream);
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("mother fucking stream crap " + i);
                streamWriter.Close();
            });

            t.Start();

            // Thread.Sleep(1000);

            var sr = new StreamReader(consumerStream);
            sr.Close();

            Task.WaitAll(t);
        }

        [Test]
        public void TestConnectionClientUsingStatement()
        {
            var connStream = new ConnectionStream();
            var producerStream = new ProducerStream(connStream);
            var consumerStream = new ConsumerStream(connStream);
            var t = new Task(() =>
            {
                var streamWriter = new StreamWriter(producerStream);
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("mother fucking stream crap " + i);
                streamWriter.Close();
            });

            t.Start();

            using (var sr = new StreamReader(consumerStream))
            {
                // read one line
                var line = sr.ReadLine();
                Assert.AreEqual("mother fucking stream crap 0", line);
            }

            Task.WaitAll(t);
        }

        [Test]
        public void TestConnectionStreamBuffering()
        {
            var connStream = new ConnectionStream();
            var producerStream = new ProducerStream(connStream);
            var consumerStream = new ConsumerStream(connStream);
            var t = new Task(() =>
            {
                var streamWriter = new StreamWriter(producerStream);
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("mother fucking stream crap " + i);
                streamWriter.Close();
            });

            t.Start();
            Task.WaitAll(t);

            using (var sr = new StreamReader(consumerStream))
            {
                // read one line
                var line = sr.ReadLine();
                Assert.AreEqual("mother fucking stream crap 0", line);
            }

        }

#if !SILVERLIGHT
        // Cannot run this test under SL as it uses the FileInfo and FileStream classes
        [Test]
        public void TestConcurrentReadWriteOfFileStream()
        {
            // create the file
            var fid = Configuration.StoreLocation + "\\" + Guid.NewGuid() + ".txt";
            var finfo = new FileInfo(fid);
            using (var fs = new StreamWriter(finfo.Create()))
            {
                fs.WriteLine("hello world");
            }

            // open for append and read
            using (var writeStream = new FileStream(fid, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                var sw = new StreamWriter(writeStream);
                sw.WriteLine("hello world");

                using (var readStream = new FileStream(fid, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var sr = new StreamReader(readStream);
                    var line = sr.ReadLine();
                    line = sr.ReadLine();
                    line = sr.ReadLine();
                    line = sr.ReadLine();
                }

                sw.WriteLine("hello world");
            }

            // open for read then read           
            using (var readStream = new FileStream(fid, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sr = new StreamReader(readStream);
                var line = sr.ReadLine();
                line = sr.ReadLine();

                using (var readStream1 = new FileStream(fid, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var sr1 = new StreamReader(readStream1);
                    line = sr1.ReadLine();
                    line = sr1.ReadLine();
                }

                line = sr.ReadLine();
                line = sr.ReadLine();
            }


            // open for read then append
            using (var readStream = new FileStream(fid, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sr = new StreamReader(readStream);
                var line = sr.ReadLine();
                line = sr.ReadLine();

                using (var writeStream = new FileStream(fid, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    var sw = new StreamWriter(writeStream);
                    sw.WriteLine("hello world");
                }

                line = sr.ReadLine();
                line = sr.ReadLine();
            }


            // open for read then append then read
        }
#endif
    }
}
