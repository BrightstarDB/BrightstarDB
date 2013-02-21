using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace BrightstarDB.SdShare.Test
{
    [TestClass]
    public class ConsumerTests
    {
        [TestMethod]
        public void TestTMCoreTopicIteration()
        {
        }

        [TestMethod]
        public void TestXmlConvert()
        {
            var dt = DateTime.UtcNow.ToString();
            var dt1 = DateTime.UtcNow.ToString("o"); 
            Assert.AreNotEqual(dt, dt1);
        }

        [TestMethod]
        public void TestGetData()
        {
            var wr = WebRequest.Create("http://localhost:9090/sdshare/collections/TMCore/snapshots/everything?format=nt");
            var resp = wr.GetResponse().GetResponseStream();
            var buffer = new Byte[1024];
            while (resp.Read(buffer, 0, 1024) > 0)
            {
                // do something
            }
            resp.Close();
        }
    }
}
