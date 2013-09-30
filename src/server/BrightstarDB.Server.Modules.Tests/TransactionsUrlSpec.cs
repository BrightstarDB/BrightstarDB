using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class TransactionsUrlSpec
    {
        private static readonly MediaRange Json = MediaRange.FromString("application/json");

        [Test]
        public void TestGetTransactions()
        {
            var mockTranscations = MockTransactionInfo(11);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(b => b.GetTransactions("foo", 0, 11)).Returns((string storeName, int skip, int take) => mockTranscations.Skip(skip).Take(take)).Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/transactions", with => with.Accept(Json));
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var transactionList = response.Body.DeserializeJson<List<TransactionResponseObject>>();
            Assert.That(transactionList, Is.Not.Null);
            Assert.That(transactionList.Count, Is.EqualTo(10));
            brightstarService.Verify();
        }

        [Test]
        public void TestGetTransactionsPagingLinks()
        {
            var mockTranscations = MockTransactionInfo(21);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(b => b.GetTransactions("foo", 10, 11)).Returns((string storeName, int skip, int take) => mockTranscations.Skip(skip).Take(take)).Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/transactions", with =>
                {
                    with.Accept(Json);
                    with.Query("skip", "10");
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var transactionList = response.Body.DeserializeJson<List<TransactionResponseObject>>();
            Assert.That(transactionList, Is.Not.Null);
            Assert.That(transactionList.Count, Is.EqualTo(10));
            Assert.That(response.Headers["Link"], Is.Not.Null);
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("prev", "transactions"));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("first", "transactions"));
            Assert.That(response.Headers["Link"], new LinkExistsConstraint("next", "transactions?skip=20"));
            brightstarService.Verify();
        }

        [Test]
        public void TestFilterByJobId()
        {
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s=>s.GetTransaction("foo", It.IsAny<Guid>()))
                .Returns((string storeName, Guid jobId)=>
                    {
                        var mockTransaction = new Mock<ITransactionInfo>();
                        mockTransaction.Setup(m => m.JobId).Returns(jobId);
                        return mockTransaction.Object;
                    })
                    .Verifiable();
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/transactions/byjob/6100E798-EDB4-457B-AE33-640EF64BFA18", with => with.Accept(Json));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var transaction = response.Body.DeserializeJson<TransactionResponseObject>();
            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction.JobId, Is.EqualTo(Guid.Parse("6100E798-EDB4-457B-AE33-640EF64BFA18")));
            brightstarService.Verify();
        }

        [Test]
        public void TestTransactionResponseObject()
        {
            var mockTransaction = new Mock<ITransactionInfo>();
            mockTransaction.Setup(t => t.Id).Returns(123);
            mockTransaction.Setup(t => t.JobId).Returns(Guid.Parse("{6100E798-EDB4-457B-AE33-640EF64BFA18}"));
            mockTransaction.Setup(t => t.StoreName).Returns("foo");
            mockTransaction.Setup(t => t.StartTime).Returns(new DateTime(2013, 09, 29, 11, 38, 00, DateTimeKind.Utc));
            mockTransaction.Setup(t => t.TransactionType).Returns(BrightstarTransactionType.ImportJob);
            mockTransaction.Setup(t => t.Status).Returns(BrightstarTransactionStatus.CompletedOk);
            var brightstarService = new Mock<IBrightstarService>();
            brightstarService.Setup(s => s.GetTransactions("foo", 0, 11))
                             .Returns(new List<ITransactionInfo> {mockTransaction.Object});
            var browser = new Browser(new FakeNancyBootstrapper(brightstarService.Object));

            // Execute
            var response = browser.Get("/foo/transactions", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            brightstarService.Verify();
            var transactionList = response.Body.DeserializeJson<List<TransactionResponseObject>>();
            Assert.That(transactionList, Is.Not.Null);
            Assert.That(transactionList.Count, Is.EqualTo(1));
            var transaction = transactionList[0];
            Assert.That(transaction, Has.Property("Id").EqualTo(123));
            Assert.That(transaction, Has.Property("JobId").EqualTo(Guid.Parse("{6100E798-EDB4-457B-AE33-640EF64BFA18}")));
            Assert.That(transaction, Has.Property("StoreName").EqualTo("foo"));
            Assert.That(transaction, Has.Property("StartTime").EqualTo(new DateTime(2013, 09, 29, 11, 38, 00, DateTimeKind.Utc)));
            Assert.That(transaction, Has.Property("TransactionType").EqualTo("ImportJob"));
            Assert.That(transaction, Has.Property("Status").EqualTo("CompletedOk"));
        }

        [Test]
        public void TestGetTransactionsRequiresViewHistoryPermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissions = new Mock<AbstractStorePermissionsProvider>();
            permissions.Setup(s=>s.HasStorePermission(null, "foo", StorePermissions.ViewHistory)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissions.Object));

            // Execute
            var response = app.Get("/foo/transactions", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            permissions.Verify();
        }

        [Test]
        public void TestGetTransactionsByJobRequiresViewHistoryPermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissions = new Mock<AbstractStorePermissionsProvider>();
            permissions.Setup(s => s.HasStorePermission(null, "foo", StorePermissions.ViewHistory)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissions.Object));

            // Execute
            var response = app.Get("/foo/transactions/byjob/6100E798-EDB4-457B-AE33-640EF64BFA18", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            permissions.Verify();
            
        }

        private static IEnumerable<ITransactionInfo> MockTransactionInfo(int count)
        {
            var ret = new List<ITransactionInfo>();
            for (var i = 0; i < count; i++)
            {
                ret.Add(new Mock<ITransactionInfo>().Object);
            }
            return ret;
        }
    }
}
