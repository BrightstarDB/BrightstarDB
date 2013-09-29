using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Server.Modules.Model;
using NUnit.Framework;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class JobRequestObjectSpec
    {
        [Test]
        public void TestCreateConsolidateJob()
        {
            var request = JobRequestObject.CreateConsolidateJob();
            Assert.That(request, Has.Property("JobType").EqualTo("Consolidate"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>()));
        }

        [Test]
        public void TestCreateExportJob()
        {
            var request = JobRequestObject.CreateExportJob("exportFile.nt");
            Assert.That(request, Has.Property("JobType").EqualTo("Export"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>{{"FileName", "exportFile.nt"}, {"GraphUri", null}}));

            request = JobRequestObject.CreateExportJob("exportFile.rdf", "http://some/graph/uri");
            Assert.That(request, Has.Property("JobType").EqualTo("Export"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>{{"FileName", "exportFile.rdf"}, {"GraphUri", "http://some/graph/uri"}}));

            Assert.That(()=>JobRequestObject.CreateExportJob(null), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("exportFileName"));
            Assert.That(()=>JobRequestObject.CreateExportJob(""), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("exportFileName"));
            Assert.That(()=>JobRequestObject.CreateExportJob("foo", ""), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("graphUri"));
        }

        [Test]
        public void TestCreateImportJob()
        {
            var request = JobRequestObject.CreateImportJob("importFile.nt");
            Assert.That(request, Has.Property("JobType").EqualTo("Import"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>{ {"FileName", "importFile.nt"}, {"DefaultGraphUri", null}}));

            request = JobRequestObject.CreateImportJob("importFile.rdf", "http://some/graph/uri");
            Assert.That(request, Has.Property("JobType").EqualTo("Import"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string> { { "FileName", "importFile.rdf" }, { "DefaultGraphUri", "http://some/graph/uri" } }));

            Assert.That(()=>JobRequestObject.CreateImportJob(null), Throws.TypeOf(typeof(ArgumentNullException)).With.Property("ParamName").EqualTo("importFileName"));
            Assert.That(()=>JobRequestObject.CreateImportJob(""), Throws.TypeOf(typeof(ArgumentException)).With.Property("ParamName").EqualTo("importFileName"));
            Assert.That(()=>JobRequestObject.CreateImportJob("foo", ""), Throws.TypeOf(typeof(ArgumentException)).With.Property("ParamName").EqualTo("defaultGraphUri"));
        }

        [Test]
        public void TestCreateSparqlUpdateJob()
        {
            var request = JobRequestObject.CreateSparqlUpdateJob("expression");
            Assert.That(request, Has.Property("JobType").EqualTo("SparqlUpdate"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>{{"UpdateExpression", "expression"}}));

            Assert.That(()=>JobRequestObject.CreateSparqlUpdateJob(null), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("updateExpression"));
            Assert.That(()=>JobRequestObject.CreateSparqlUpdateJob(""), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("updateExpression"));
            Assert.That(() => JobRequestObject.CreateSparqlUpdateJob(" "), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("updateExpression"));
        }

        [Test]
        public void TestCreateTransactionJob()
        {
            var request = JobRequestObject.CreateTransactionJob("preconditions", "deletes", "inserts");
            Assert.That(request, Has.Property("JobType").EqualTo("Transaction"));
            Assert.That(request,
                        Has.Property("JobParameters")
                           .EqualTo(new Dictionary<string, string>
                               {
                                   {"Preconditions", "preconditions"},
                                   {"Deletes", "deletes"},
                                   {"Inserts", "inserts"},
                                   {"DefaultGraphUri", null}
                               }));
        }

        [Test]
        public void TestCreateUpdateStatisticsJob()
        {
            var request = JobRequestObject.CreateUpdateStatsJob();
            Assert.That(request, Has.Property("JobType").EqualTo("UpdateStats"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>()));
        }

    }
}
