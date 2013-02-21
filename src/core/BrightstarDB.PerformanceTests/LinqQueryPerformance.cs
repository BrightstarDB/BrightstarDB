using System;
using System.Linq;
using BrightstarDB.PerformanceTests.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.PerformanceTests
{
    [TestClass]
    public class LinqQueryPerformance : PerformanceTestBase
    {
        private const string StoresDirectory = "C:\\brightstar";
        private const string StoreName = "BrightstarDB.PerformanceTests";
        private static string _embeddedConnectionString;
        private static MyEntityContext _context;

        [ClassInitialize]
        public static void StoreSetup(TestContext testContext)
        {
            _embeddedConnectionString = String.Format("type=embedded;storesdirectory={0};storename={1}",
                                                     StoresDirectory, StoreName);
            _context = CreateStore(_embeddedConnectionString, StoreName);
        }

        
        
        [TestMethod]
        public void CountInstances()
        {
            Assert.AreEqual(1000, _context.Skills.Count());
            Assert.AreEqual(1000, _context.Persons.Count());
            Assert.AreEqual(100, _context.Departments.Count());
            Assert.AreEqual(5, _context.JobRoles.Count());
            Assert.AreEqual(2, _context.Websites.Count());
            Assert.AreEqual(10000, _context.Articles.Count());
        }

        [TestMethod]
        public void TestQuery1()
        {
            var personsWithHighSalaryUnderAgeOf30 = (from person in _context.Persons where person.Salary > 300000 && person.Age < 30 select person);
            var results = personsWithHighSalaryUnderAgeOf30.ToList();
            Assert.AreEqual(49, results.Count);
        }

        [TestMethod]
        public void TestQuery1WithRetrieval()
        {
            var personsWithHighSalaryUnderAgeOf30 = (from person in _context.Persons where person.Salary > 300000 && person.Age < 30 select person);
            foreach(var p in personsWithHighSalaryUnderAgeOf30)
            {
                var pname = p.Fullname;
            }
        }

        [TestMethod]
        public void TestQuery2()
        {
            var personsOrdered = (from person in _context.Persons orderby person.Fullname descending select person);
            var results = personsOrdered.ToList();
            Assert.AreEqual(1000, results.Count);
        }

        [TestMethod]
        public void TestQuery2WithRetrieval()
        {
            var personsOrdered = (from person in _context.Persons orderby person.Fullname descending select person);
            foreach(var p in personsOrdered)
            {
                var pname = p.Fullname;
            }
        }

        [TestMethod]
        public void TestQuery3()
        {
            var personsHavingSkill = _context.Skills.Where(s => s.Title.Contains("7")).SelectMany(s => s.SkilledPeople);
            var results = personsHavingSkill.ToList();
            Assert.AreEqual(271, results.Count);
        }

        [TestMethod]
        public void TestQuery3WithRetrieval()
        {
            var personsHavingSkill = _context.Skills.Where(s => s.Title.Contains("7")).SelectMany(s => s.SkilledPeople);
            foreach(var p in personsHavingSkill)
            {
                var pname = p.Fullname;
            }
        }

        [TestMethod]
        public void TestQuery4()
        {
            var managers =
                        _context.JobRoles.Where(jr => jr.Description.Equals("management")).SelectMany(jr => jr.Persons).
                            OrderBy(p => p.Salary);
            var results = managers.ToList();
            Assert.AreEqual(200, results.Count);
        }

        [TestMethod]
        public void TestQuery4WithRetrieval()
        {
            var managers =
                        _context.JobRoles.Where(jr => jr.Description.Equals("management")).SelectMany(jr => jr.Persons).
                            OrderBy(p => p.Salary);
            foreach(var m in managers)
            {
                var mname = m.Fullname;
            }
        }

        [TestMethod]
        public void TestQuery5()
        {
            var articles = _context.Articles.Where(a => a.Publisher.Age > 30 && a.Website.Name.Equals("website2")).Distinct();
            var results = articles.ToList();
            Assert.AreEqual(3900, results.Count);
        }

        [TestMethod]
        public void TestQuery5WithRetrieval()
        {
            var articles = _context.Articles.Where(a => a.Publisher.Age > 30 && a.Website.Name.Equals("website2")).Distinct();
            foreach(var a in articles)
            {
                var atitle = a.Title;
            }
        }

        [TestMethod]
        public void TestQuery6()
        {
            var average = _context.Articles.Select(a => a.Publisher).Average(p => p.Age);
            Assert.AreEqual(44.5, average);
        }

        [TestMethod]
        public void TestQuery7()
        {
            var deptArticles = _context.Articles.Where(a => a.Publisher.Department.Name.Equals("Department4")).OrderBy(a => a.Title)
                            .ToList();
            Assert.AreEqual(100, deptArticles.Count);
        }

        [TestMethod]
        public void TestQuery7WithRetrieval()
        {
            var deptArticles =
                _context.Articles.Where(a => a.Publisher.Department.Name.Equals("Department4")).OrderBy(a => a.Title).
                    ToList();
            foreach (var group in deptArticles.GroupBy(da=>da.Publisher))
            {
                var publisher = group.Key.Fullname;
                foreach(var article in group)
                {
                    var atitle = article.Title;
                }
            }
        }

        [TestMethod]
        public void TestQuery8()
        {
            var articlesByOldPeople = _context.Articles.Where(a => a.Publisher.Age > 50).Distinct();
            var results = articlesByOldPeople.ToList();
            Assert.AreEqual(3800, results.Count);
        }

        [TestMethod]
        public void TestQuery8WithRetrieval()
        {
            var articlesByOldPeople = _context.Articles.Where(a => a.Publisher.Age > 50).Distinct();
            foreach(var a in articlesByOldPeople)
            {
                var atitle = a.Title;
            }
        }

        [TestMethod]
        public void TestQuery9()
        {
            var firstArticle = _context.Articles.First();
            var singleArticle = new { A = firstArticle.Title, B = firstArticle.Publisher.Fullname };
            string tests = singleArticle.A; //make sure it is retrieved and not lazy loaded cached somehow   
        }

        [TestMethod]
        public void TestQuery11()
        {
            var youngpeople = (from person in _context.Persons where person.Age < 30 select person);
            var results = youngpeople.ToList();
            Assert.AreEqual(200, results.Count);
        }

        [TestMethod]
        public void TestQuery11WithRetrieval()
        {
            var youngpeople = (from person in _context.Persons where person.Age < 30 select person);
            foreach(var p in youngpeople)
            {
                var pname = p.Fullname;
            }
        }
    }
}
