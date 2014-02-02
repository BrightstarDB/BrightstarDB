using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture("type=embedded;StoresDirectory={1};storeName={2}")]
    [TestFixture("type=dotnetrdf;configuration={0}dataObjectStoreConfig.ttl;storeName=http://www.brightstardb.com/tests#empty")]
    public class LinqTests
    {
        private readonly string _connectionStringTemplate;

        public LinqTests(string connectionStringTemplate)
        {
            _connectionStringTemplate = connectionStringTemplate;
        }

        private string GetConnectionString(string testName)
        {
            return String.Format(_connectionStringTemplate,
                                 Configuration.DataLocation,
                                 Configuration.StoreLocation,
                                 testName + "_" + DateTime.Now.Ticks);
        }

        [Test]
        public void TestLinqCount()
        {
            var connectionString = GetConnectionString("TestLinqCount");
            var context = new MyEntityContext(connectionString);
            for(var i = 0; i<100; i++)
            {
                var entity = context.Entities.Create();
                entity.SomeString = "Entity " + i;
                entity.SomeInt = i;
            }
            context.SaveChanges();

            var count = context.Entities.Count();

            Assert.IsNotNull(count);
            Assert.AreEqual(100, count);

            for (var j = 0; j < 100; j++)
            {
                var entity = context.Entities.Create();
                entity.SomeString = "Entity " + j;
                entity.SomeInt = j;
            }
            context.SaveChanges();

            var count2 = context.Entities.Count();

            Assert.IsNotNull(count2);
            Assert.AreEqual(200, count2);
        }

        [Test]
        public void TestLinqLongCount()
        {
            var connectionString = GetConnectionString("TestLinqLongCount");
            var context = new MyEntityContext(connectionString);
            for (var i = 0; i < 100; i++)
            {
                var entity = context.Entities.Create();
                entity.SomeString = "Entity " + i;
            }
            context.SaveChanges();

            var count = context.Entities.LongCount();

            Assert.IsNotNull(count);
            Assert.AreEqual(100, count);
        }

        [Test]
        public void TestLinqAverage()
        {
            var connectionString = GetConnectionString("TestLinqAverage");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10;
            e1.SomeDouble = 10;
            
            var e2 = context.Entities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12;
            e2.SomeDouble = 12;
            
            var e3 = context.Entities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15;
            e3.SomeDouble = 15;
            
            var e4 = context.Entities.Create();
            e4.SomeInt = 10;
            e4.SomeDecimal = 10;
            e4.SomeDouble = 10;
            
            var e5 = context.Entities.Create();
            e5.SomeInt = 11;
            e5.SomeDecimal = 11;
            e5.SomeDouble = 11;


            context.SaveChanges();
            Assert.AreEqual(5, context.Entities.Count());

            var avInt = context.Entities.Average(e => e.SomeInt);
            Assert.IsNotNull(avInt);
            Assert.AreEqual(11.6, avInt);

            var avDec = context.Entities.Average(e => e.SomeDecimal);
            Assert.IsNotNull(avDec);
            Assert.AreEqual(11.6m, avDec);

            var avDbl = context.Entities.Average(e => e.SomeDouble);
            Assert.IsNotNull(avDbl);
            Assert.AreEqual(11.6, avDbl);

        }

        [Test]
        public void TestLinqAverage2()
        {
            var connectionString = GetConnectionString("TestLinqAverage2");
            var context = new MyEntityContext(connectionString);
            var ages = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                var entity = context.Entities.Create();
                entity.SomeString = "Person" + i;
                int age = 20 + (i / 20);
                entity.SomeInt = age;
                ages.Add(age);
            }
            context.SaveChanges();

            var total1 = context.Entities.Sum(e => e.SomeInt);
            var total2 = ages.Sum();

            var q1 = context.Entities.Count();
            var q2 = ages.Count;

            Assert.AreEqual(total2 / q2, total1 / q1);

            Assert.AreEqual(1000, context.Entities.Count());

            Assert.AreEqual(ages.Average(), context.Entities.Average(e => e.SomeInt));
        }

        [Test]
        public void TestLinqSum()
        {
            var connectionString = GetConnectionString("TestLinqSum");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10.1m;
            e1.SomeDouble = 10.2;

            var e2 = context.Entities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12.1m;
            e2.SomeDouble = 12.2;

            var e3 = context.Entities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15.1m;
            e3.SomeDouble = 15.2;

            var e4 = context.Entities.Create();
            e4.SomeInt = 10;
            e4.SomeDecimal = 10.1m;
            e4.SomeDouble = 10.2;

            var e5 = context.Entities.Create();
            e5.SomeInt = 11;
            e5.SomeDecimal = 11.1m;
            e5.SomeDouble = 11.2;


            context.SaveChanges();
            Assert.AreEqual(5, context.Entities.Count());

            var sumInt = context.Entities.Sum(e => e.SomeInt);
            Assert.IsNotNull(sumInt);
            Assert.AreEqual(58, sumInt);

            var sumDec = context.Entities.Sum(e => e.SomeDecimal);
            Assert.IsNotNull(sumDec);
            Assert.AreEqual(58.5m, sumDec);

            var sumDbl = context.Entities.Sum(e => e.SomeDouble);
            Assert.IsNotNull(sumDbl);
            Assert.AreEqual(59.0, sumDbl);

        }
        
        public void TestLinqCast()
        {
        }

        [Test]
        public void TestLinqContainsString()
        {
            var connectionString = GetConnectionString("TestLinqContainsString");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfStrings = new List<string> {"Jen", "Kal", "Gra", "Andy"};
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfStrings = new List<string> {"Miranda", "Sadik", "Tobey", "Ian"};
            
            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());
            
            var containsString = context.Entities.Where(e => e.CollectionOfStrings.Contains("Jen")).ToList();
            Assert.IsNotNull(containsString);
            Assert.AreEqual(1, containsString.Count());
            Assert.AreEqual("Networked Planet", containsString.First().SomeString);

            var matchTargets = new List<string> {"Samarind", "IBM", "Microsoft"};
            var matchCompanies = context.Entities.Where(e => matchTargets.Contains(e.SomeString)).ToList();
            Assert.IsNotNull(matchCompanies);
            Assert.AreEqual(1, matchCompanies.Count);
            Assert.AreEqual("Samarind", matchCompanies.First().SomeString);
        }

        [Test]
        public void TestLinqContainsInt()
        {
            var connectionString = GetConnectionString("TestLinqContainsInt");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfInts = new List<int>() { 2, 4, 6, 8, 10 };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfInts = new List<int>() { 1, 3, 5, 7, 9 };
            
            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsInt = context.Entities.Where(e => e.CollectionOfInts.Contains(3)).ToList();
            Assert.IsNotNull(containsInt);
            Assert.AreEqual(1, containsInt.Count);
            Assert.AreEqual("Samarind", containsInt.First().SomeString);

        }

        [Test]
        public void TestLinqContainsDateTime()
        {
            var connectionString = GetConnectionString("TestLinqContainsDateTime");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            var now = DateTime.Now;

            e1.SomeString = "Networked Planet";
            e1.CollectionOfStrings = new List<string> { "Jen", "Kal", "Gra", "Andy" };
            e1.CollectionOfDateTimes = new List<DateTime>() { now.AddYears(2), now.AddYears(4) };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfStrings = new List<string> { "Miranda", "Sadik", "Tobey", "Ian" };
            e2.CollectionOfDateTimes = new List<DateTime>() { now.AddYears(1), now.AddYears(3) };
            
            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsDateTime =
                context.Entities.Where(e => e.CollectionOfDateTimes.Contains(now.AddYears(2))).ToList();
            Assert.IsNotNull(containsDateTime);
            Assert.AreEqual(1, containsDateTime.Count);
            Assert.AreEqual("Networked Planet", containsDateTime.First().SomeString);

        }

        [Test]
        public void TestLinqContainsDouble()
        {
            var connectionString = GetConnectionString("TestLinqContainsDouble");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfDoubles = new List<double>() { 2.5, 4.5, 6.5, 8.5, 10.5 };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfDoubles = new List<double>() { 1.5, 3.5, 5.5, 7.5, 9.5 };

            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsDouble = context.Entities.Where(e => e.CollectionOfDoubles.Contains(8.5)).ToList();
            Assert.IsNotNull(containsDouble);
            Assert.AreEqual(1, containsDouble.Count);
            Assert.AreEqual("Networked Planet", containsDouble.First().SomeString);

        }

        [Test]
        public void TestLinqContainsFloat()
        {
            var connectionString = GetConnectionString("TestLinqContainsFloat");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfFloats = new List<float> { 2.5F, 4.5F, 6.5F, 8.5F, 10.5F };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfFloats = new List<float> { 1.5F, 3.5F, 5.5F, 7.5F, 9.5F };

            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsFloat = context.Entities.Where(e => e.CollectionOfFloats.Contains(6.5F)).ToList();
            Assert.IsNotNull(containsFloat);
            Assert.AreEqual(1, containsFloat.Count);
            Assert.AreEqual("Networked Planet", containsFloat.First().SomeString);

        }

        [Test]
        public void TestLinqContainsDecimal()
        {
            var connectionString = GetConnectionString("TestLinqContainsDecimal");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfDecimals = new List<decimal> { 2.5M, 4.5M, 6.5M, 8.5M, 10.5M };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfDecimals = new List<decimal> { 1.5M, 3.5M, 5.5M, 7.5M, 9.5M };

            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsDecimal = context.Entities.Where(e => e.CollectionOfDecimals.Contains(9.5M)).ToList();
            Assert.IsNotNull(containsDecimal);
            Assert.AreEqual(1, containsDecimal.Count);
            Assert.AreEqual("Samarind", containsDecimal.First().SomeString);

        }

        [Test]
        public void TestLinqContainsBool()
        {
            var connectionString = GetConnectionString("TestLinqContainsBool");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfBools = new List<bool>() { true };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfBools = new List<bool>() { false };

            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsBool = context.Entities.Where(e => e.CollectionOfBools.Contains(false)).ToList();
            Assert.IsNotNull(containsBool);
            Assert.AreEqual(1, containsBool.Count);
            Assert.AreEqual("Samarind", containsBool.First().SomeString);

        }

        [Test]
        public void TestLinqContainsLong()
        {
            var connectionString = GetConnectionString("TestLinqContainsLong");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeString = "Networked Planet";
            e1.CollectionOfLong = new List<long>() { 2000000000000, 4000000000000 };
            var e2 = context.Entities.Create();
            e2.SomeString = "Samarind";
            e2.CollectionOfLong = new List<long>() { 3000000000000, 5000000000000 };

            context.SaveChanges();

            Assert.AreEqual(2, context.Entities.Count());

            var containsLong = context.Entities.Where(e => e.CollectionOfLong.Contains(2000000000000)).ToList();
            Assert.IsNotNull(containsLong);
            Assert.AreEqual(1, containsLong.Count);
            Assert.AreEqual("Networked Planet", containsLong.First().SomeString);

        }


        [Test]
        public void TestLinqDistinct()
        {
            var connectionString = GetConnectionString("TestLinqDistinct");
            var context = new MyEntityContext(connectionString);
            
             var entity1 = context.Entities.Create();
            entity1.SomeString = "Apples";
            entity1.SomeInt = 2;

            var entity2 = context.Entities.Create();
            entity2.SomeString = "Bananas";
            entity2.SomeInt = 2;

            var entity3 = context.Entities.Create();
            entity3.SomeString = "Carrots";
            entity3.SomeInt = 10;

            var entity4 = context.Entities.Create();
            entity4.SomeString = "Apples";
            entity4.SomeInt = 10;

            var entity5 = context.Entities.Create();
            entity5.SomeString = "Apples";
            entity5.SomeInt = 2;

            context.SaveChanges();

            var categories = context.Entities.Select(x => x.SomeString).Distinct().ToList();
            Assert.AreEqual(3, categories.Count());
            Assert.IsTrue(categories.Contains("Apples"));
            Assert.IsTrue(categories.Contains("Bananas"));
            Assert.IsTrue(categories.Contains("Carrots"));
        }

        [Test]
        public void TestLinqFirst()
        {
            var connectionString = GetConnectionString("TestLinqFirst");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name);
            Assert.IsNotNull(orderedByName);
            Assert.AreEqual(6, orderedByName.Count());

            var first = orderedByName.First();
            Assert.IsNotNull(first);
            Assert.AreEqual("Annie", first.Name);
        }

        [Test]
        public void TestLinqFirstOrDefault()
        {
            var connectionString = GetConnectionString("TestLinqFirstOrDefault");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());


            var first = context.Persons.Where(p => p.Name.Equals("Annie")).FirstOrDefault();
            Assert.IsNotNull(first);
            Assert.AreEqual("Annie", first.Name);

            var notfound = context.Persons.Where(p => p.Name.Equals("Jo")).FirstOrDefault();
            Assert.IsNull(notfound);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestLinqFirstFail()
        {
            var connectionString = GetConnectionString("TestLinqFirstFail");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());


            var first = context.Persons.Where(p => p.Name.Equals("Annie")).FirstOrDefault();
            Assert.IsNotNull(first);
            Assert.AreEqual("Annie", first.Name);

            var notfound = context.Persons.Where(p => p.Name.Equals("Jo")).First();
            Assert.IsNull(notfound);
        }

        [Ignore]
        [Test]
        public void TestLinqGroupBy()
        {
            var connectionString = GetConnectionString("TestLinqGroupBy");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Bill";
            pe.Age = 51;
            
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 51;

            var pf = context.Persons.Create();
            pf.Name = "Bill";
            pf.Age = 47;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 47;

            var pc = context.Persons.Create();
            pc.Name = "Dennis";
            pc.Age = 20;

            var pa = context.Persons.Create();
            pa.Name = "Dennis";
            pb.Age = 28;

            context.SaveChanges();

            Assert.AreEqual(6, context.Persons.Count());

            var grpByAge = context.Persons.GroupBy(people => people.Age);
            foreach(var item in grpByAge)
            {
                var age = item.Key;
                var count = item.Count();
            }

            var grpNyName = from p in context.Persons
                           group p by p.Name into g
                           orderby g.Key
                           select new { Name = g.Key, Count = g.Count() };

            foreach (var item in grpNyName)
            {
                var age = item.Name;
                var numInGroup = item.Count;
            }

            var grpByAge2 = from p in context.Persons
                           group p by p.Age into g
                           orderby g.Key
                           select new { Age = g.Key, Count = g.Count() };

            foreach (var item in grpByAge2)
            {
                var age = item.Age;
                var numInGroup = item.Count;
            }
        }
        
        [Test]
        public void TestLinqMax()
        {
            var connectionString = GetConnectionString("TestLinqMax");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10.21m;
            e1.SomeDouble = 10.21;

            var e2 = context.Entities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12.56m;
            e2.SomeDouble = 12.56;

            var e3 = context.Entities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15.45m;
            e3.SomeDouble = 15.45;

            var e4 = context.Entities.Create();
            e4.SomeInt = 9;
            e4.SomeDecimal = 10.11m;
            e4.SomeDouble = 10.11;

            var e5 = context.Entities.Create();
            e5.SomeInt = 16;
            e5.SomeDecimal = 15.99m;
            e5.SomeDouble = 15.99;


            context.SaveChanges();
            Assert.AreEqual(5, context.Entities.Count());

            var maxInt = context.Entities.Max(e => e.SomeInt);
            Assert.IsNotNull(maxInt);
            Assert.AreEqual(16, maxInt);
            var maxDec = context.Entities.Max(e => e.SomeDecimal);
            Assert.IsNotNull(maxDec);
            Assert.AreEqual(15.99m, maxDec);
            var maxDbl = context.Entities.Max(e => e.SomeDouble);
            Assert.IsNotNull(maxDbl);
            Assert.AreEqual(15.99, maxDbl);
        }

        [Test]
        public void TestLinqMin()
        {
            var connectionString = GetConnectionString("TestLinqMin");
            var context = new MyEntityContext(connectionString);

            var e1 = context.Entities.Create();
            e1.SomeInt = 10;
            e1.SomeDecimal = 10.21m;
            e1.SomeDouble = 10.21;

            var e2 = context.Entities.Create();
            e2.SomeInt = 12;
            e2.SomeDecimal = 12.56m;
            e2.SomeDouble = 12.56;

            var e3 = context.Entities.Create();
            e3.SomeInt = 15;
            e3.SomeDecimal = 15.45m;
            e3.SomeDouble = 15.45;

            var e4 = context.Entities.Create();
            e4.SomeInt = 9;
            e4.SomeDecimal = 10.11m;
            e4.SomeDouble = 10.11;

            var e5 = context.Entities.Create();
            e5.SomeInt = 16;
            e5.SomeDecimal = 15.99m;
            e5.SomeDouble = 15.99;


            context.SaveChanges();
            Assert.AreEqual(5, context.Entities.Count());

            var minInt = context.Entities.Min(e => e.SomeInt);
            Assert.IsNotNull(minInt);
            Assert.AreEqual(9, minInt);
            var minDec = context.Entities.Min(e => e.SomeDecimal);
            Assert.IsNotNull(minDec);
            Assert.AreEqual(10.11m, minDec);
            var minDbl = context.Entities.Min(e => e.SomeDouble);
            Assert.IsNotNull(minDbl);
            Assert.AreEqual(10.11, minDbl);
        }

        [Test]
        public void TestLinqOrderByString()
        {
            var connectionString = GetConnectionString("TestLinqOrderByString");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name);
            Assert.IsNotNull(orderedByName);
            Assert.AreEqual(6, orderedByName.Count());
            var i = 0;
            foreach (var p in orderedByName)
            {
                Assert.IsNotNull(p.Name);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 5:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                }
                i++;
            }

            var orderedByNameDesc = context.Persons.OrderByDescending(p => p.Name);
            Assert.IsNotNull(orderedByNameDesc);
            Assert.AreEqual(6, orderedByNameDesc.Count());
            var j = 0;
            foreach (var p in orderedByNameDesc)
            {
                Assert.IsNotNull(p.Name);
                switch (j)
                {
                    case 5:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 0:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                }
                j++;
            }
        }

        [Test]
        public void TestLinqOrderByDate()
        {
            var connectionString = GetConnectionString("TestLinqOrderByDate");
            var context = new MyEntityContext(connectionString);
            
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.DateOfBirth = new DateTime(1969, 8, 8, 4, 5, 30);

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.DateOfBirth = new DateTime(1900, 1, 12);
            
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.DateOfBirth = new DateTime(1969, 8, 8, 4, 6, 30);

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.DateOfBirth = new DateTime(1962, 4, 20);

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.DateOfBirth = new DateTime(1962, 3, 11);
            
            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.DateOfBirth = new DateTime(1950, 2, 2);

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByDob = context.Persons.OrderBy(p => p.DateOfBirth);
            Assert.IsNotNull(orderedByDob);
            Assert.AreEqual(6, orderedByDob.Count());
            var i = 0;
            foreach (var p in orderedByDob)
            {
                Assert.IsNotNull(p.Name);
                Assert.IsNotNull(p.DateOfBirth);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 5:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                }
                i++;
            }

            var orderedByDobDesc = context.Persons.OrderByDescending(p => p.DateOfBirth);
            Assert.IsNotNull(orderedByDobDesc);
            Assert.AreEqual(6, orderedByDobDesc.Count());
            var j = 0;
            foreach (var p in orderedByDobDesc)
            {
                Assert.IsNotNull(p.Name);
                Assert.IsNotNull(p.DateOfBirth);
                switch (j)
                {
                    case 5:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 0:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                }
                j++;
            }
        }

        [Test]
        public void TestLinqOrderByInteger()
        {
            var connectionString = GetConnectionString("TestLinqOrderByInteger");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.Age = 51;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 111;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 47;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 32;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 18;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 28;

            context.SaveChanges();

            Assert.AreEqual(6, context.Persons.Count());

            var orderedByAge = context.Persons.OrderBy(p => p.Age);
            Assert.IsNotNull(orderedByAge);
            Assert.AreEqual(6, orderedByAge.Count());
            var i = 0;
            foreach (var p in orderedByAge)
            {
                Assert.IsNotNull(p.Name);
                Assert.IsNotNull(p.Age);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 5:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                }
                i++;
            }

            var orderedByAgeDesc = context.Persons.OrderByDescending(p => p.Age);
            Assert.IsNotNull(orderedByAgeDesc);
            Assert.AreEqual(6, orderedByAgeDesc.Count());
            var j = 0;
            foreach (var p in orderedByAgeDesc)
            {
                Assert.IsNotNull(p.Name);
                Assert.IsNotNull(p.Age);
                switch (j)
                {
                    case 5:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 0:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                }
                j++;
            }
            
        }

        [Test]
        //note - not sure if this is an adequate test of Select()
        public void TestLinqSelect()
        {
            var connectionString = GetConnectionString("TestLinqSelect");
            var context = new MyEntityContext(connectionString);

            for (var i = 1; i < 11; i++ )
            {
                var entity = context.Entities.Create();
                entity.SomeInt = i;
            }
            context.SaveChanges();
            Assert.AreEqual(10, context.Entities.Count());

            var select = context.Entities.Select(e => e);
            Assert.IsNotNull(select);
            Assert.AreEqual(10, select.Count());
        }

        [Test]
        public void TestLinqSelectMany()
        {
            var connectionString = GetConnectionString("TestLinqSelectMany");
            var context = new MyEntityContext(connectionString);
            
            var skill1 = context.Skills.Create();
            skill1.Name = "C#";
            var skill2 = context.Skills.Create();
            skill2.Name = "HTML";
            var p1 = context.Persons.Create();
            p1.Name = "Jane";
            p1.Skills.Add(skill1);
            p1.Skills.Add(skill2);

            var skill3 = context.Skills.Create();
            skill3.Name = "SQL";
            var skill4 = context.Skills.Create();
            skill4.Name = "NoSQL";
            var p2 = context.Persons.Create();
            p2.Name = "Bob";
            p2.Skills.Add(skill3);
            p2.Skills.Add(skill4);

            var skill5 = context.Skills.Create();
            skill5.Name = "Graphics";
            var p3 = context.Persons.Create();
            p3.Name = "Jez";
            p3.Skills.Add(skill5);

            var skill6 = context.Skills.Create();
            skill6.Name = "CSS";
            
            context.SaveChanges();

            Assert.AreEqual(3, context.Persons.Count());
            Assert.AreEqual(6, context.Skills.Count());

            var daskillz = context.Persons.SelectMany(owners => owners.Skills);
            var i = 0;
            foreach(var s in daskillz)
            {
                i++;
                Assert.IsNotNull(s.Name);
            }
            Assert.AreEqual(5, i);
            Assert.AreEqual(5, daskillz.Count());
        }

        [Test]
        public void TestLinqSingle()
        {
            var connectionString = GetConnectionString("TestLinqSingle");
            var context = new MyEntityContext(connectionString);
            
            var entity = context.Entities.Create();
            entity.SomeString = "An entity";
            context.SaveChanges();
            Assert.AreEqual(1, context.Entities.Count());

            var single = context.Entities.Single();
            Assert.IsNotNull(single);
            Assert.AreEqual("An entity", single.SomeString);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestLinqSingleFail()
        {
            var connectionString = GetConnectionString("TestLinqSingleFail");
            var context = new MyEntityContext(connectionString);
            var sod = context.Entities.Single();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestLinqSingleFail2()
        {
            var connectionString = GetConnectionString("TestLinqSingleFail2");
            var context = new MyEntityContext(connectionString);

            for (var i = 1; i < 11; i++)
            {
                var entity = context.Entities.Create();
                entity.SomeInt = i;
            }
            context.SaveChanges();
            Assert.AreEqual(10, context.Entities.Count());

            var singleFail = context.Entities.Single();
        }

        [Test]
        public void TestLinqSingleOrDefault()
        {
            var connectionString = GetConnectionString("TestLinqSingleOrDefault");
            var context = new MyEntityContext(connectionString);

            var sod = context.Entities.SingleOrDefault();
            Assert.IsNull(sod);

            var entity = context.Entities.Create();
            entity.SomeString = "An entity";
            context.SaveChanges();
            Assert.AreEqual(1, context.Entities.Count());

            var single = context.Entities.SingleOrDefault();
            Assert.IsNotNull(single);
            Assert.AreEqual("An entity", single.SomeString);

            for (var i = 1; i < 10; i++)
            {
                var e = context.Entities.Create();
                e.SomeInt = i;
            }
            context.SaveChanges();
            Assert.AreEqual(10, context.Entities.Count());

            //var sod = context.Entities.SingleOrDefault();
            //Assert.IsNull(sod);

        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestLinqSingleOrDefaultFail()
        {
            var connectionString = GetConnectionString("TestLinqSingleOrDefaultFail");
            var context = new MyEntityContext(connectionString);

            var sod = context.Entities.SingleOrDefault();
            Assert.IsNull(sod);

            for (var i = 0; i < 10; i++)
            {
                var e = context.Entities.Create();
                e.SomeInt = i;
            }
            context.SaveChanges();
            Assert.AreEqual(10, context.Entities.Count());

            var sod2 = context.Entities.SingleOrDefault();
            
        }

        [Test]
        public void TestLinqSkip()
        {
            var connectionString = GetConnectionString("TestLinqSkip");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name).Skip(2);
            Assert.IsNotNull(orderedByName);
            Assert.AreEqual(4, orderedByName.ToList().Count());
            var i = 0;
            foreach (var p in orderedByName)
            {
                Assert.IsNotNull(p.Name);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                }
                i++;
            }
            Assert.AreEqual(4, i);
        }

        [Test]
        public void TestLinqTake()
        {
            var connectionString = GetConnectionString("TestLinqTake");
            var context = new MyEntityContext(connectionString);
            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            var pb = context.Persons.Create();
            pb.Name = "Bill";
            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            var pc = context.Persons.Create();
            pc.Name = "Carole";
            var pa = context.Persons.Create();
            pa.Name = "Annie";

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByName = context.Persons.OrderBy(p => p.Name).Skip(3).Take(2);
            Assert.IsNotNull(orderedByName);
            //Assert.AreEqual(2, orderedByName.Count());
            var i = 0;
            foreach (var p in orderedByName)
            {
                Assert.IsNotNull(p.Name);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                }
                i++;
            }
            Assert.AreEqual(2, i);
        }

        [Test]
        public void TestLinqThenBy()
        {
            var connectionString = GetConnectionString("TestLinqThenBy");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.Age = 30;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 30;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 30;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 29;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 29;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 35;

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByAgeThenName = context.Persons.OrderBy(p => p.Age).ThenBy(p => p.Name);
            Assert.IsNotNull(orderedByAgeThenName);
            Assert.AreEqual(6, orderedByAgeThenName.Count());
            var i = 0;
            foreach (var p in orderedByAgeThenName)
            {
                Assert.IsNotNull(p.Name);
                Assert.IsNotNull(p.Age);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                    case 5:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                }
                i++;
            }

        }

        [Test]
        public void TestLinqThenByDescending()
        {
            var connectionString = GetConnectionString("TestLinqThenByDescending");
            var context = new MyEntityContext(connectionString);

            var pe = context.Persons.Create();
            pe.Name = "Eddie";
            pe.Age = 30;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 30;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 30;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 29;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 29;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 35;

            context.SaveChanges();
            Assert.AreEqual(6, context.Persons.Count());

            var orderedByAgeThenName = context.Persons.OrderBy(p => p.Age).ThenByDescending(p => p.Name);
            Assert.IsNotNull(orderedByAgeThenName);
            Assert.AreEqual(6, orderedByAgeThenName.Count());
            var i = 0;
            foreach (var p in orderedByAgeThenName)
            {
                Assert.IsNotNull(p.Name);
                Assert.IsNotNull(p.Age);
                switch (i)
                {
                    case 0:
                        Assert.AreEqual("Dennis", p.Name);
                        break;
                    case 1:
                        Assert.AreEqual("Carole", p.Name);
                        break;
                    case 2:
                        Assert.AreEqual("Freddie", p.Name);
                        break;
                    case 3:
                        Assert.AreEqual("Eddie", p.Name);
                        break;
                    case 4:
                        Assert.AreEqual("Bill", p.Name);
                        break;
                    case 5:
                        Assert.AreEqual("Annie", p.Name);
                        break;
                }
                i++;
            }
        }

        [Test]
        public void TestLinqWhere()
        {
            var connectionString = GetConnectionString("TestLinqWhere");
            var context = new MyEntityContext(connectionString);

            // Setup
            var programming = context.Skills.Create();
            programming.Name = "Programming";
            var projectManagement = context.Skills.Create();
            projectManagement.Name = "Project Management";
            var graphicDesign = context.Skills.Create();
            graphicDesign.Name = "Graphic Design";

            var pe = context.Persons.Create();
            pe.Name = "Alex";
            pe.Age = 30;
            pe.MainSkill = programming;

            var pb = context.Persons.Create();
            pb.Name = "Bill";
            pb.Age = 30;
            pb.MainSkill = projectManagement;

            var pf = context.Persons.Create();
            pf.Name = "Freddie";
            pf.Age = 30;
            pf.MainSkill = graphicDesign;

            var pd = context.Persons.Create();
            pd.Name = "Dennis";
            pd.Age = 29;
            pd.Friends.Add(pe);
            pd.MainSkill = programming;

            var pc = context.Persons.Create();
            pc.Name = "Carole";
            pc.Age = 29;
            pc.MainSkill = projectManagement;

            var pa = context.Persons.Create();
            pa.Name = "Annie";
            pa.Age = 35;
            pa.MainSkill = graphicDesign;

            context.SaveChanges();

            // Assert
            Assert.AreEqual(6, context.Persons.Count());

            var age30 = context.Persons.Where(p => p.Age.Equals(30));
            Assert.AreEqual(3, age30.Count());
            var older = context.Persons.Where(p => p.Age > 30);
            Assert.AreEqual(1, older.Count());
            var younger = context.Persons.Where(p => p.Age < 30);
            Assert.AreEqual(2, younger.Count());

            var startswithA = context.Persons.Where(p => p.Name.StartsWith("A"));
            Assert.AreEqual(2, startswithA.Count());

            var endsWithE = context.Persons.Where(p => p.Name.EndsWith("e"));
            Assert.AreEqual(3, endsWithE.Count());

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", true, CultureInfo.CurrentUICulture));
            Assert.AreEqual(3, endsWithE.Count());

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", false, CultureInfo.CurrentUICulture));
            Assert.AreEqual(0, endsWithE.Count());

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", StringComparison.CurrentCultureIgnoreCase));
            Assert.AreEqual(3, endsWithE.Count());

            endsWithE = context.Persons.Where(p => p.Name.EndsWith("E", StringComparison.CurrentCulture));
            Assert.AreEqual(0, endsWithE.Count());

            var containsNi = context.Persons.Where(p => p.Name.Contains("ni"));
            Assert.AreEqual(2, containsNi.Count());

            var x = context.Persons.Where(p => Regex.IsMatch(p.Name, "^a.*e$", RegexOptions.IgnoreCase));
            Assert.AreEqual(1, x.Count());
            Assert.AreEqual("Annie", x.First().Name);

            var annie = context.Persons.Where(p => p.Name.Equals("Annie")).SingleOrDefault();
            Assert.IsNotNull(annie);

            var mainSkillsOfPeopleOver30 = from s in context.Skills where s.Expert.Age > 30 select s;
            var results = mainSkillsOfPeopleOver30.ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Graphic Design", results.First().Name);

            //note - startswith and getchar are not supported

            //note null is not supported
            //not Count() is not supported
            //var hasFriends = context.Persons.Where(p => p.Friends.Count() > 0);
            //Assert.AreEqual(1, hasFriends.Count());

            //note length is not supported
            //var longNames = context.Persons.Where(p => p.Name.Length > 6);
            //Assert.AreEqual(1, longNames.Count());


        }

        [Test]
        public void TestLinqRelatedWhere()
        {
            var connectionString = GetConnectionString("TestLinqRelatedWhere");
            var context = new MyEntityContext(connectionString);

            // Setup
            for (var i = 0; i < 10; i++)
            {
                var p = context.Persons.Create();
                p.Name = "Person" + i;
                var age = (i + 1)*10;
                p.Age = age;
                var s = context.Skills.Create();
                s.Name = "Skill" +i;
                s.Expert = p;
            }
            context.SaveChanges();

            // Assert
            Assert.AreEqual(10, context.Persons.Count());
            Assert.AreEqual(10, context.Skills.Count());

            var skills = from s in context.Skills select s;
            Assert.AreEqual(10, skills.Count());

            var peopleOver30 = context.Persons.Where(p => p.Age > 30);
            Assert.AreEqual(7, peopleOver30.Count());

            peopleOver30 = from p in context.Persons where p.Age > 30 select p;
            Assert.AreEqual(7, peopleOver30.Count());


            var mainSkillsOfPeopleOver30 = from s in context.Skills where s.Expert.Age > 30 select s;
            Assert.AreEqual(7, mainSkillsOfPeopleOver30.Count());


        }

        [Test]
        public void TestLinqQuery1()
        {
            var connectionString = GetConnectionString("TestLinqQuery1");
            var context = new MyEntityContext(connectionString);

            var jr1 = context.JobRoles.Create();
            jr1.Description = "development";

            var jr2 = context.JobRoles.Create();
            jr2.Description = "sales";

            var jr3 = context.JobRoles.Create();
            jr3.Description = "marketing";

            var jr4 = context.JobRoles.Create();
            jr4.Description = "management";

            var jr5 = context.JobRoles.Create();
            jr5.Description = "administration";

            context.SaveChanges();

            var roles = new IJobRole[] {jr1, jr2, jr3, jr4, jr5};

            for (var i = 0; i < 100; i++)
            {
                var p = context.Persons.Create();
                p.Name = "Person" + i;
                p.EmployeeId = i;
                p.JobRole = roles[i%5];
            }

            context.SaveChanges();

            // Assert
            Assert.AreEqual(100, context.Persons.Count());
            Assert.AreEqual(5, context.JobRoles.Count());

            var management = context.JobRoles.Where(s => s.Description.Equals("management")).First();
            Assert.IsNotNull(management);
            var managers = management.Persons;
            Assert.IsNotNull(managers);
            Assert.AreEqual(20, managers.Count);
        }


        [Test]
        public void TestLinqJoin1()
        {
            var connectionString = GetConnectionString("TestLinqJoin1");
            var context = new MyEntityContext(connectionString);

            for(var i = 0; i<3; i++)
            {
                var jobrole = context.JobRoles.Create();
                jobrole.Description = "JobRole " + i;
                if (i <= 0) continue;
                for (var j = 0; j < 50; j++)
                {
                    var person = context.Persons.Create();
                    person.Name = "Person " + j;
                    jobrole.Persons.Add(person);
                }
            }
            context.SaveChanges();

            Assert.AreEqual(3, context.JobRoles.Count());
            Assert.AreEqual(100, context.Persons.Count());

            var rolesThatHavePeople = (from jobrole in context.JobRoles
                                  join person in context.Persons on jobrole.Id equals person.JobRole.Id
                                  select jobrole).Distinct().ToList();
            Assert.AreEqual(2, rolesThatHavePeople.Count);
        }

        [Test]
        public void TestLinqJoinOnProperty()
        {
            var connectionString = GetConnectionString("TestLinqJoinOnProperty");
            var context = new MyEntityContext(connectionString);

            var people = new List<IPerson>();
            for (var i = 0; i < 100; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == i).SingleOrDefault();
                Assert.IsNotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            Assert.AreEqual(100, context.Persons.Count());
            Assert.AreEqual(100, context.Articles.Count());

            var allArticlesWithPublishers = (from article in context.Articles
                                             join person in context.Persons on article.Publisher.EmployeeId equals
                                                 person.EmployeeId
                                             select article).ToList();
            Assert.AreEqual(100, allArticlesWithPublishers.Count);


            var allPublishersWithArticles = (from person in context.Persons
                                 join article in context.Articles on person.EmployeeId equals
                                     article.Publisher.EmployeeId
                                 select person).ToList();
            Assert.AreEqual(100, allPublishersWithArticles.Count);
        }

        [Test]
        public void TestLinqJoinOnId()
        {
            var connectionString = GetConnectionString("TestLinqJoinOnId");
            var context = new MyEntityContext(connectionString);

            var people = new List<IPerson>();
            for (var i = 0; i < 100; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == i).SingleOrDefault();
                Assert.IsNotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            Assert.AreEqual(100, context.Persons.Count());
            Assert.AreEqual(100, context.Articles.Count());

            var test = context.Articles.Count(a => a.Publisher != null);
            Assert.AreEqual(100, test);

            var allArticlesWithPublishers = (from article in context.Articles
                                             join person in context.Persons on article.Publisher.Id equals
                                                 person.Id
                                             select article).ToList();
            Assert.AreEqual(100, allArticlesWithPublishers.Count);


            var allPublishersWithArticles = (from person in context.Persons
                                             join article in context.Articles on person.Id equals
                                                 article.Publisher.Id
                                             select person).ToList();
            Assert.AreEqual(100, allPublishersWithArticles.Count);
        }


        [Test]
        public void TestLinqJoinOnId2()
        {
            var connectionString = GetConnectionString("TestLinqJoinOnId2");
            var context = new MyEntityContext(connectionString);

            var people = new List<IPerson>();
            for (var i = 0; i < 11; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == (i/10)).SingleOrDefault();
                Assert.IsNotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            Assert.AreEqual(11, context.Persons.Count());
            Assert.AreEqual(100, context.Articles.Count());


            var allArticlesWithPublishers = (from article in context.Articles
                                             join person in context.Persons on article.Publisher.Id equals
                                                 person.Id
                                             select article).ToList();
            Assert.AreEqual(100, allArticlesWithPublishers.Count);


            var allPublishersWithArticles = (from person in context.Persons
                                             join article in context.Articles on person.Id equals
                                                 article.Publisher.Id
                                             select person).Distinct().ToList();
            Assert.AreEqual(10, allPublishersWithArticles.Count);
        }

        [Test]
        public void TestLinqJoinWithFilter()
        {
            var connectionString = GetConnectionString("TestLinqJoinWithFilter");
            var context = new MyEntityContext(connectionString);

            // Setup
            var people = new List<IPerson>();
            for (var i = 0; i < 10; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                var age = (i + 2) * 10;
                person.Age = age;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == (i / 10)).SingleOrDefault();
                Assert.IsNotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            // Assert
            Assert.AreEqual(10, context.Persons.Count());
            Assert.AreEqual(100, context.Articles.Count());

            var articlesByOldPeopleCount = Enumerable.Count(context.Articles, a => a.Publisher.Age > 50);
            Assert.AreEqual(60, articlesByOldPeopleCount);

            var articlesByOldPeople = (from person in context.Persons
                                       join article in context.Articles on person.Id equals article.Publisher.Id
                                       where person.Age > 50
                                       select article).ToList();

            Assert.AreEqual(60, articlesByOldPeople.Count);
        }

        [Test]
        public void TestLinqRelatedCount()
        {
            var connectionString = GetConnectionString("TestLinqRelatedCount");
            var context = new MyEntityContext(connectionString);

            // Setup
            var people = new List<IPerson>();
            for (var i = 0; i < 11; i++)
            {
                var person = context.Persons.Create();
                person.Name = "Person " + i;
                person.EmployeeId = i;
                var age = (i + 2) * 10;
                person.Age = age;
                people.Add(person);
            }

            for (var i = 0; i < 100; i++)
            {
                var article = context.Articles.Create();
                article.Title = "Article " + i;

                var publisher = people.Where(p => p.EmployeeId == (i / 10)).SingleOrDefault();
                Assert.IsNotNull(publisher);

                article.Publisher = publisher;
            }
            context.SaveChanges();

            // Assert
            Assert.AreEqual(11, context.Persons.Count());
            Assert.AreEqual(100, context.Articles.Count());

            var publishers = context.Articles.Select(a => a.Publisher).Distinct().ToList();
            Assert.AreEqual(10, publishers.Count);
        }


        [Test]
       public void TestLinqAny()
        {
            var connectionString = GetConnectionString("TestLinqAny");
           var context = new MyEntityContext(connectionString);
           var deptA = context.Departments.Create();
           deptA.Name = "Department A";
           var deptB = context.Departments.Create();
           deptB.Name = "Department B";
           var alice = context.Persons.Create();
           alice.Age = 25;
           var bob = context.Persons.Create();
           bob.Age = 29;
           var charlie = context.Persons.Create();
           charlie.Age = 21;
           var dave = context.Persons.Create();
           dave.Age = 35;
           deptA.Persons.Add(alice);
           deptA.Persons.Add(bob);
           deptB.Persons.Add(charlie);
           deptB.Persons.Add(dave);
           context.SaveChanges();

           var departmentsWithOldies = context.Departments.Where(d => d.Persons.Any(p => p.Age > 30)).ToList();
           Assert.AreEqual(1, departmentsWithOldies.Count);
           Assert.AreEqual(deptB.Id, departmentsWithOldies[0].Id);
       }

        [Test]
        public void TestLinqAll()
        {
            var connectionString = GetConnectionString("TestLinqAll");
            if (connectionString.Contains("dotnetrdf"))
            {
                Assert.Inconclusive("Test known to fail due to bug in current build of DotNetRDF.");
            }
            var context = new MyEntityContext(connectionString);
            var alice = context.Persons.Create();
            alice.Name = "Alice";
            alice.Age = 18;
            var bob = context.Persons.Create();
            bob.Name = "Bob";
            bob.Age = 20;
            var carol = context.Persons.Create();
            carol.Age = 20;
            carol.Name = "Carol";
            var dave = context.Persons.Create();
            dave.Age = 22;
            dave.Name = "Dave";
            var edith = context.Persons.Create();
            edith.Age = 21;
            edith.Name = "Edith";
            alice.Friends.Add(bob);
            alice.Friends.Add(carol);
            bob.Friends.Add(alice);
            bob.Friends.Add(carol);
            carol.Friends.Add(alice);
            carol.Friends.Add(bob);
            carol.Friends.Add(dave);
            dave.Friends.Add(edith);
            context.SaveChanges();

            var results = context.Persons.Where(p => p.Friends.All(f => f.Age < 21)).Select(f=>f.Name).ToList();
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Contains("Alice"));
            Assert.IsTrue(results.Contains("Bob"));
            Assert.IsTrue(results.Contains("Edith"));
        }
        
        [Test]
        public void TestLinqQueryEnum()
        {
            var connectionString = GetConnectionString("TestLinqQueryEnum");
            var context = new MyEntityContext(connectionString);
            var entity1 = context.Entities.Create();
            entity1.SomeEnumeration = TestEnumeration.Second;
            entity1.SomeNullableEnumeration = TestEnumeration.Third;
            entity1.SomeNullableFlagsEnumeration = TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB;
            context.SaveChanges();
            /*
            // Find by single flag
            IList<IEntity> results = context.Entities.Where(e => e.SomeEnumeration == TestEnumeration.Second).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x=>x.Id.Equals(entity1.Id)));


            // Find by flag combo
            results =
                context.Entities.Where(
                    e => e.SomeNullableFlagsEnumeration == (TestFlagsEnumeration.FlagB | TestFlagsEnumeration.FlagA)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x => x.Id.Equals(entity1.Id)));

            // Find by one flag of combo
            results = context.Entities.Where(
                e => ((e.SomeNullableFlagsEnumeration & TestFlagsEnumeration.FlagB) == TestFlagsEnumeration.FlagB)).
                ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x => x.Id.Equals(entity1.Id)));

            // Find by one flag not set on combo
            results = context.Entities.Where(
                e => ((e.SomeNullableFlagsEnumeration & TestFlagsEnumeration.FlagC) == TestFlagsEnumeration.NoFlags)).
                ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x => x.Id.Equals(entity1.Id)));

            // Find by both flags set on combo
            results = context.Entities.Where(
                e =>
                ((e.SomeNullableFlagsEnumeration & (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB)) ==
                 (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB))).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x => x.Id.Equals(entity1.Id)));

            results = context.Entities.Where(
                e =>
                ((e.SomeNullableFlagsEnumeration & (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagC)) ==
                 (TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagC))).ToList();
            Assert.AreEqual(0, results.Count);
            */

            // Find by NoFlags
            var results =
                context.Entities.Where(
                    e => e.SomeFlagsEnumeration == TestFlagsEnumeration.NoFlags).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x => x.Id.Equals(entity1.Id)));
        }

        [Test]
        public void TestLinqNullComparison()
        {
            var connectionString = GetConnectionString("TestLinqNullComparison");
            var context = new MyEntityContext(connectionString);
            var alice = context.Persons.Create();
            alice.Name = "Alice";
            alice.Age = 18;
            var bob = context.Persons.Create();
            bob.Name = "Bob";
            bob.Age = 20;
            var carol = context.Persons.Create();
            carol.Age = 20;
            carol.Name = "Carol";
            var dave = context.Persons.Create();
            dave.Age = 22;
            dave.Name = "Dave";
            var edith = context.Persons.Create();
            edith.Age = 21;
            edith.Name = null;
            alice.Friends.Add(bob);
            alice.Friends.Add(carol);
            bob.Friends.Add(alice);
            bob.Friends.Add(carol);
            carol.Friends.Add(alice);
            carol.Friends.Add(bob);
            carol.Friends.Add(dave);
            dave.Friends.Add(edith);
            context.SaveChanges();

            var count = context.Persons.Count(e => e.Name == null);
            Assert.AreEqual(1, count);

            var count2 = context.Persons.Count(e => null == e.Name);
            Assert.AreEqual(1, count2);
        }

    }

}
