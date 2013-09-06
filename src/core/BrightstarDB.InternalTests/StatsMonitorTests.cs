using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BrightstarDB.Server;
using BrightstarDB.Storage.Statistics;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class StatsMonitorTests
    {
        [Test]
       public void TestTriggerByTransaction()
       {
           Configuration.StatsUpdateTransactionCount = 10;
            Configuration.StatsUpdateTimespan = 0;
           bool triggered = false;
           var statsMonitor = new StatsMonitor();
           statsMonitor.Initialize(new StoreStatistics(10UL, DateTime.UtcNow, 0UL, new Dictionary<string, ulong>()),
               18UL, () => { triggered = true; });
           statsMonitor.OnJobScheduled();
           Assert.IsFalse(triggered);
           statsMonitor.OnJobScheduled();
           Assert.IsTrue(triggered);
            triggered = false;
            statsMonitor.OnJobScheduled();
            Assert.IsFalse(triggered, "Monitor should not retrigger on txn immediately after firing.");
       }

        [Test]
        public void TestTriggerByTimespan()
        {
            Configuration.StatsUpdateTimespan = 60;
            Configuration.StatsUpdateTransactionCount = 0;
            bool triggered = false;
            var statsMonitor = new StatsMonitor();
            statsMonitor.Initialize(new StoreStatistics(10UL, DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(59)),0UL, new Dictionary<string, ulong>()),
                12UL, () => { triggered = true; });
            statsMonitor.OnJobScheduled();
            Assert.IsFalse(triggered);
            Thread.Sleep(TimeSpan.FromSeconds(2.0));
            statsMonitor.OnJobScheduled();
            Assert.IsTrue(triggered);
            triggered = false;
            statsMonitor.OnJobScheduled();
            Assert.IsFalse(triggered, "Monitor should not retrigger on time immediately after firing.");
        }

        [Test]
        public void TestTriggerByBoth()
        {
            Configuration.StatsUpdateTimespan = 60;
            Configuration.StatsUpdateTransactionCount = 6;
            bool triggered = false;
            var statsMonitor = new StatsMonitor();
            statsMonitor.Initialize(new StoreStatistics(10UL, DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(59)), 0UL, new Dictionary<string, ulong>()),
                12UL, () => { triggered = true; });
            statsMonitor.OnJobScheduled(); // 13
            Assert.IsFalse(triggered);
            Thread.Sleep(TimeSpan.FromSeconds(2.0));
            statsMonitor.OnJobScheduled(); // 14
            Assert.IsFalse(triggered);
            statsMonitor.OnJobScheduled(); // 15
            Assert.IsFalse(triggered);
            statsMonitor.OnJobScheduled(); // 16 - should trigger udpate
            Assert.IsTrue(triggered);
            triggered = false;
            // Subsequent job should not re-trigger
            statsMonitor.OnJobScheduled();
            Assert.IsFalse(triggered);
        }
    }
}
