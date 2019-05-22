using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Server;
using NUnit.Framework;
using BrightstarDB.Utils;

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
                                     using (var streamWriter = new StreamWriter(producerStream))
                                     {
                                         streamWriter.WriteLine("stream content");
                                         streamWriter.Close();
                                     }
                                 });

            t.Start();

            var sr = new StreamReader(consumerStream);
            var result = sr.ReadToEnd();
            Assert.AreEqual("stream content\r\n", result);

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
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("stream content " + i);
                streamWriter.Close();
            });

            t.Start();

            Thread.Sleep(1000);

            var sr = new StreamReader(consumerStream);                   
            var result = sr.ReadToEnd();
            Assert.IsTrue(result.StartsWith("stream content 0"));
            Assert.IsTrue(result.EndsWith("stream content 999\r\n"));
            
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
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("stream content " + i);
                streamWriter.Close();
            });

            t.Start();

            Thread.Sleep(1000);

            int j = 0;
            using (var sr = new StreamReader(consumerStream))
            {
                var result = sr.ReadLine();
                Assert.AreEqual(result, "stream content " + j);
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
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("stream content " + i);
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
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("stream content " + i);
                streamWriter.Close();
            });

            t.Start();

            using (var sr = new StreamReader(consumerStream))
            {
                // read one line
                var line = sr.ReadLine();
                Assert.AreEqual("stream content 0", line);
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
                for (int i = 0; i < 1000; i++) streamWriter.WriteLine("stream content " + i);
                streamWriter.Close();
            });

            t.Start();
            Task.WaitAll(t);

            using (var sr = new StreamReader(consumerStream))
            {
                // read one line
                var line = sr.ReadLine();
                Assert.AreEqual("stream content 0", line);
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
