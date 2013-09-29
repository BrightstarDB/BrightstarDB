using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class StatisticsUrlSpec
    {
        private static readonly MediaRange Json = MediaRange.FromString("application/json");

        [Test]
        public void TestGetStatisticsWithoutOptions()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetStatistics("foo", DateTime.MaxValue, DateTime.MinValue, 0, 11))
                      .Returns(MockStatistics(11))
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics", with => with.Accept(Json));
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("next", "statistics?skip=10"));
            var results = response.Body.DeserializeJson<List<StatisticsResponseObject>>();
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(10));
            brightstar.Verify();
        }

        [Test]
        public void TestGetStatisticsWithDateRange()
        {
            // Setup
            DateTime now = DateTime.UtcNow;
            DateTime lastWeek = now.AddDays(-7);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(
                s => s.GetStatistics("foo",
                                     It.IsInRange(now.AddSeconds(-1.0), now.AddSeconds(1.0), Range.Inclusive),
                                     It.IsInRange(lastWeek.AddSeconds(-1.0), lastWeek.AddSeconds(1.0), Range.Inclusive),
                                     0, 11))
                      .Returns(MockStatistics(11))
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics", with =>
                {
                    with.Accept(Json);
                    with.Query("latest", now.ToString("s"));
                    with.Query("earliest", lastWeek.ToString("s"));
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("next", String.Format("statistics?latest={0}&earliest={1}&skip=10", now.ToString("s"), lastWeek.ToString("s"))));
            var results = response.Body.DeserializeJson<List<StatisticsResponseObject>>();
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(10));
            brightstar.Verify();
        }

        [Test]
        public void TestGetStatisticsWithEarliestFilterOnly()
        {
            // Setup
            DateTime now = DateTime.UtcNow;
            DateTime lastWeek = now.AddDays(-7);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetStatistics("foo", DateTime.MaxValue, It.IsInRange(lastWeek.AddSeconds(-1.0), lastWeek.AddSeconds(1.0), Range.Inclusive), 0, 11))
                .Returns(MockStatistics(11))
                .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics", with =>
            {
                with.Accept(Json);
                with.Query("earliest", lastWeek.ToString("s"));
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("next", String.Format("statistics?earliest={0}&skip=10", lastWeek.ToString("s"))));
            var results = response.Body.DeserializeJson<List<StatisticsResponseObject>>();
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(10));
            brightstar.Verify();
        }

        [Test]
        public void TestGetStatisticsWithLatestFilterOnly()
        {
            // Setup
            DateTime now = DateTime.UtcNow;
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetStatistics("foo", It.IsInRange(now.AddSeconds(-1.0), now.AddSeconds(1.0), Range.Inclusive), DateTime.MinValue, 0, 11))
                      .Returns(MockStatistics(11))
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics", with =>
                {
                    with.Accept(Json);
                    with.Query("latest", now.ToString("s"));
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("next", String.Format("statistics?latest={0}&skip=10", now.ToString("s"))));
            var results = response.Body.DeserializeJson<List<StatisticsResponseObject>>();
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(10));
            brightstar.Verify();
        }

        [Test]
        public void TestGetStatisticsWithPaging()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetStatistics("foo", DateTime.MaxValue, DateTime.MinValue, 10, 11))
                      .Returns(MockStatistics(11))
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics", with =>
            {
                with.Accept(Json);
                with.Query("skip", "10");
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("next", "statistics?skip=20"));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("prev", "statistics"));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("first", "statistics"));
            var results = response.Body.DeserializeJson<List<StatisticsResponseObject>>();
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(10));
            brightstar.Verify();
        }

        [Test]
        public void TestGetLatestStatistics()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s=>s.GetStatistics("foo")).Returns(MockStatistics(1).First()).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics/latest", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var result = response.Body.DeserializeJson<StatisticsResponseObject>();
            Console.WriteLine(response.Headers["Link"]);
            Assert.That(result, Is.Not.Null);
            brightstar.Verify();
        }

        private IEnumerable<IStoreStatistics> MockStatistics(int count)
        {
            var ret = new List<IStoreStatistics>();
            for (var i = 0; i < count; i++)
            {
                var mock = new Mock<IStoreStatistics>();
                mock.Setup(s => s.CommitId).Returns((ulong) (count - i)*10);
                mock.Setup(s => s.CommitTimestamp).Returns(DateTime.UtcNow.AddHours((count - i)*-1));
                mock.Setup(s => s.TotalTripleCount).Returns((ulong)(count - i)*1000);
                mock.Setup(s => s.PredicateTripleCounts)
                    .Returns(new Dictionary<string, ulong> {{"http://some/predicate", (ulong) (count - i)*1000}});
                ret.Add(mock.Object);
            }
            return ret;
        } 
    }
}
