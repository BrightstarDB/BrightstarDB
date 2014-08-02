using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    public class InversePropertyTests
    {
        private readonly string _connectionString = "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=InversePropertyTests_" + DateTime.Now.Ticks;

        [Test]
        public void TestAddOne()
        {
            string productionId, performanceId;
            using (var context = new MyEntityContext(_connectionString))
            {
                var production = context.Productions.Create();
                var performance = context.Performances.Create();
                Assert.That(production.Performances.Count, Is.EqualTo(0));
                Assert.That(production.Photos.Count, Is.EqualTo(0));
                Assert.That(production.ProductionTeam.Count, Is.EqualTo(0));

                // Set the Production property on the performance
                performance.Production = production;

                Assert.That(production.Performances.Count, Is.EqualTo(1));
                Assert.That(production.Photos.Count, Is.EqualTo(0));
                Assert.That(production.ProductionTeam.Count, Is.EqualTo(0));
                context.SaveChanges();
                productionId = production.Id;
                performanceId = performance.Id;
            }

            using (var context = new MyEntityContext(_connectionString))
            {
                var production = context.Productions.FirstOrDefault(x => x.Id.Equals(productionId));
                Assert.That(production, Is.Not.Null);
                Assert.That(production.Performances.Count, Is.EqualTo(1));
                Assert.That(production.Performances.First().Id, Is.EqualTo(performanceId));
                Assert.That(production.Photos.Count, Is.EqualTo(0));
                Assert.That(production.ProductionTeam.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void TestAddToInverse()
        {
            string productionId, performanceId;
            using (var context = new MyEntityContext(_connectionString))
            {
                var production = context.Productions.Create();
                var performance = context.Performances.Create();
                Assert.That(production.Performances.Count, Is.EqualTo(0));
                Assert.That(production.Photos.Count, Is.EqualTo(0));
                Assert.That(production.ProductionTeam.Count, Is.EqualTo(0));

                // Add the performance to the production's perfomances collection
                production.Performances.Add(performance);

                Assert.That(production.Performances.Count, Is.EqualTo(1));
                Assert.That(production.Photos.Count, Is.EqualTo(0));
                Assert.That(production.ProductionTeam.Count, Is.EqualTo(0));
                context.SaveChanges();
                productionId = production.Id;
                performanceId = performance.Id;
            }

            using (var context = new MyEntityContext(_connectionString))
            {
                var production = context.Productions.FirstOrDefault(x => x.Id.Equals(productionId));
                Assert.That(production, Is.Not.Null);
                Assert.That(production.Performances.Count, Is.EqualTo(1));
                Assert.That(production.Performances.First().Id, Is.EqualTo(performanceId));
                Assert.That(production.Photos.Count, Is.EqualTo(0));
                Assert.That(production.ProductionTeam.Count, Is.EqualTo(0));
            }
        }
    }
}
