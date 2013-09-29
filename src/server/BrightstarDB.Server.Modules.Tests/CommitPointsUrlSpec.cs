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
    public class CommitPointsUrlSpec
    {
        private static readonly MediaRange Json = MediaRange.FromString("application/json");

        [Test]
        [Description("Test retrieving the first page of commit point info")]
        public void TestGetCommitPoints()
        {
            var commitPoints = MockCommitPoints("foo", 10);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetCommitPoints("foo", It.IsAny<int>(), It.IsAny<int>()))
                             .Returns((string storeName, int skip, int take) =>
                                 {
                                     return commitPoints.Skip(skip).Take(take);
                                 })
                             .Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/commits", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseList = response.Body.DeserializeJson<List<CommitPointResponseObject>>();
            Assert.That(responseList.Count, Is.EqualTo(10));
            brightstarService.Verify();
        }

        [Test]
        public void TestLinkExistsConstraint()
        {
            Assert.That("<hello>;rel=wassup", new LinkExistsConstraint("wassup", "hello"));
            Assert.That("<hello>; rel=first, <yello.xml>; rel=second", Is.Not.Null.And.Matches(new LinkExistsConstraint("second", "yello.xml")).And.Matches(new LinkExistsConstraint("first", "hello")));
            Assert.That("<hello>;rel=wassup", Is.Not.Null.And.Not.Matches(new LinkExistsConstraint("first", "hello")));
        }

        [Test]
        [Description("When a store has more than 10 commit points, the first page response should include a Link header pointing to the next page")]
        public void TestCommitPointResultHasNextPageLink()
        {
            var commitPoints = MockCommitPoints("foo", 11);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetCommitPoints("foo", It.IsAny<int>(), It.IsAny<int>()))
                             .Returns((string storeName, int skip, int take) => commitPoints.Skip(skip).Take(take))
                             .Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/commits", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseList = response.Body.DeserializeJson<List<CommitPointResponseObject>>();
            Assert.That(responseList.Count, Is.EqualTo(10));
            Assert.That(response.Headers.ContainsKey("Link"));
            Assert.That(response.Headers["Link"], Is.Not.Null.And.Matches(new LinkExistsConstraint("next", "commits?skip=10")));
            brightstarService.Verify();
        }

        [Test]
        [Description(
            "When a store has more than 10 commit points, the second and subsequent page responses should include a Link header pointing to the previous page and to the first page"
            )]
        public void TestCommitPointResultHasPreviousAndFirstPageLink()
        {
            var commitPoints = MockCommitPoints("foo", 21);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetCommitPoints("foo", It.IsAny<int>(), It.IsAny<int>()))
                             .Returns((string storeName, int skip, int take) => commitPoints.Skip(skip).Take(take))
                             .Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/commits", with =>
                {
                    with.Accept(Json);
                    with.Query("skip", "10");
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseList = response.Body.DeserializeJson<List<CommitPointResponseObject>>();
            Assert.That(responseList.Count, Is.EqualTo(10));
            Assert.That(response.Headers.ContainsKey("Link"));
            Assert.That(response.Headers["Link"], Is.Not.Null.And.Matches(new LinkExistsConstraint("next", "commits?skip=20")));
            Assert.That(response.Headers["Link"], Is.Not.Null.And.Matches(new LinkExistsConstraint("prev", "commits")));
            Assert.That(response.Headers["Link"], Is.Not.Null.And.Matches(new LinkExistsConstraint("first", "commits")));
            brightstarService.Verify();
        }

        [Test]
        [Description("When a store has more than 10 commit points, the final page should have no next page link")]
        public void TestFinalPageOfCommitPointResultsHasNoNextPageLink()
        {
            var commitPoints = MockCommitPoints("foo", 21);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetCommitPoints("foo", It.IsAny<int>(), It.IsAny<int>()))
                             .Returns((string storeName, int skip, int take) => commitPoints.Skip(skip).Take(take))
                             .Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/commits", with =>
                {
                    with.Accept(Json);
                    with.Query("skip", "20");
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseList = response.Body.DeserializeJson<List<CommitPointResponseObject>>();
            Assert.That(responseList.Count, Is.EqualTo(1));
            Assert.That(response.Headers.ContainsKey("Link"));
            Assert.That(response.Headers["Link"],
                        Is.Not.Null.And.Matches(new LinkExistsConstraint("prev", "commits?skip=10")));
            Assert.That(response.Headers["Link"], Is.Not.Null.And.Matches(new LinkExistsConstraint("first", "commits")));
            brightstarService.Verify();
        }

        [Test]
        public void TestGetCommitPointByTimestamp()
        {
            var mockCommitPoint = MockCommitPoints("foo", 1).First();
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetCommitPoint("foo", It.IsAny<DateTime>())).Returns(mockCommitPoint).Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/commits", with =>
                {
                    with.Accept(Json);
                    with.Query("timestamp", DateTime.UtcNow.ToString("s"));
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseObject = response.Body.DeserializeJson<CommitPointResponseObject>();
            Assert.That(responseObject.Id, Is.EqualTo(10UL));
            brightstarService.Verify();
        }

        [Test]
        public void TestGetCommitPointRangeByDate()
        {
            var commitPoints = MockCommitPoints("foo", 11);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetCommitPoints("foo", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
                             .Returns((string storeName, DateTime l, DateTime e, int skip, int take) => commitPoints.Skip(skip).Take(take))
                             .Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            var latest = DateTime.UtcNow.ToString("s");
            var earliest = DateTime.UtcNow.AddDays(-1.0).ToString("s");
            // Execute
            var response = browser.Get("/foo/commits", with =>
            {
                with.Accept(Json);
                with.Query("latest", latest);
                with.Query("earliest", earliest);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseList = response.Body.DeserializeJson<List<CommitPointResponseObject>>();
            Assert.That(responseList.Count, Is.EqualTo(10));
            Assert.That(response.Headers.ContainsKey("Link"));
            Assert.That(response.Headers["Link"],
                        Is.Not.Null.And.Matches(new LinkExistsConstraint("next", String.Format("commits?latest={0}&earliest={1}&skip=10", latest, earliest))));
            brightstarService.Verify();
        }


        [Test]
        public void TestPostCommitPointToRevert()
        {
            var brightstarService = new Mock<IBrightstarService>();
            var mockCommitPoint = MockCommitPoints("foo", 1).First();
            brightstarService.Setup(s => s.GetCommitPoint("foo", 123)).Returns(mockCommitPoint).Verifiable();
            brightstarService.Setup(s=>s.RevertToCommitPoint("foo", It.IsAny<ICommitPointInfo>())).Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            var response = browser.Post("/foo/commits", with =>
                {
                    with.JsonBody(new CommitPointResponseObject {Id = 123, StoreName = "foo"});
                    with.Accept(Json);
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            brightstarService.Verify();
        }

        [Test]
        public void TestCannotRevertIfCommitPointNotFound()
        {
            var brightstarService = new Mock<IBrightstarService>();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            var response = browser.Post("/foo/commits", with =>
            {
                with.JsonBody(new CommitPointResponseObject { Id = 123, StoreName = "foo" });
                with.Accept(Json);
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        private IEnumerable<ICommitPointInfo> MockCommitPoints(string storeName, int count)
        {
            var ret = new List<ICommitPointInfo>();
            for (int i = 0; i < count; i++)
            {
                var mock = new Mock<ICommitPointInfo>();
                mock.Setup(m => m.Id).Returns((ulong) (count - i)*10);
                mock.Setup(m => m.StoreName).Returns(storeName);
                mock.Setup(m => m.JobId).Returns(Guid.Empty);
                mock.Setup(m => m.CommitTime).Returns(DateTime.Now.Subtract(TimeSpan.FromMinutes(count - i)));
                ret.Add(mock.Object);
            }
            return ret;
        } 
    }
}
