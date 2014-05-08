using System;
using System.Collections.Generic;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Storage;
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
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>{{"FileName", "exportFile.nt"}, {"GraphUri", null}, {"Format", RdfFormat.NQuads.MediaTypes[0]}}));

            request = JobRequestObject.CreateExportJob("exportFile.rdf", "http://some/graph/uri");
            Assert.That(request, Has.Property("JobType").EqualTo("Export"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string>{{"FileName", "exportFile.rdf"}, {"GraphUri", "http://some/graph/uri"}, {"Format", RdfFormat.NQuads.MediaTypes[0]}}));

            request = JobRequestObject.CreateExportJob("exportFile.rdf", "http://some/graph/uri", RdfFormat.NTriples);
            Assert.That(request, Has.Property("JobType").EqualTo("Export"));
            Assert.That(request, Has.Property("JobParameters").EqualTo(new Dictionary<string, string> { { "FileName", "exportFile.rdf" }, { "GraphUri", "http://some/graph/uri" }, {"Format", RdfFormat.NTriples.MediaTypes[0]}}));

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
            var request = JobRequestObject.CreateTransactionJob(new UpdateTransactionData
                {
                    ExistencePreconditions = "preconditions",
                    NonexistencePreconditions = "nonexistencePreconditions",
                    DeletePatterns = "deletes",
                    InsertData = "inserts",
                    DefaultGraphUri = null
                }, null);
            Assert.That(request, Has.Property("JobType").EqualTo("Transaction"));
            Assert.That(request,
                        Has.Property("JobParameters")
                           .EqualTo(new Dictionary<string, string>
                               {
                                   {"Preconditions", "preconditions"},
                                   {"NonexistencePreconditions", "nonexistencePreconditions"},
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

        [Test]
        public void TestCreateJobWithLabel()
        {
            var request = JobRequestObject.CreateConsolidateJob().WithLabel("My Consolidation");
            Assert.That(request, Has.Property("Label").EqualTo("My Consolidation"));
        }

        [Test]
        public void TestCreateJobWithLabelInConstructor()
        {
            // Consolidate
            var consolidateRequest = JobRequestObject.CreateConsolidateJob(label:"ConsolidationJob");
            Assert.That(consolidateRequest, Has.Property("Label").EqualTo("ConsolidationJob"));
            // export
            var exportRequest = JobRequestObject.CreateExportJob("export.nt", label:"ExportJob");
            Assert.That(exportRequest, Has.Property("Label").EqualTo("ExportJob"));
            // import
            var importRequest = JobRequestObject.CreateImportJob("import.nt", label:"ImportJob");
            Assert.That(importRequest, Has.Property("Label").EqualTo("ImportJob"));
            // repeat transaction
            var repeatRequest = JobRequestObject.CreateRepeatTransactionJob(Guid.Empty, label:"RepeatJob");
            Assert.That(repeatRequest, Has.Property("Label").EqualTo("RepeatJob"));
            // Snapshot
            var snapshotRequest = JobRequestObject.CreateSnapshotJob("storeToSnapshot", PersistenceType.AppendOnly, label:"SnapshotJob");
            Assert.That(snapshotRequest, Has.Property("Label").EqualTo("SnapshotJob"));
            // Sparql Update
            var sparqlUpdateRequest = JobRequestObject.CreateSparqlUpdateJob("update expression", label:"SparqlUpdateJob");
            Assert.That(sparqlUpdateRequest, Has.Property("Label").EqualTo("SparqlUpdateJob"));
            // Transaction
            var transactionJob = JobRequestObject.CreateTransactionJob(
                new UpdateTransactionData
                    {
                        ExistencePreconditions = "precon",
                        NonexistencePreconditions = "nexist",
                        DeletePatterns = "delete",
                        InsertData = "insert",
                        DefaultGraphUri = null,
                    }, "TransactionJob");
            Assert.That(transactionJob, Has.Property("Label").EqualTo("TransactionJob"));
            // Update Statistics
            var updateStatsRequest = JobRequestObject.CreateUpdateStatsJob(label:"UpdateStats");
            Assert.That(updateStatsRequest, Has.Property("Label").EqualTo("UpdateStats"));
        }

        [Test]
        public void TestSetJobLabel()
        {
            var request = JobRequestObject.CreateExportJob("test.nt");
            request.Label = "My Export";
            Assert.That(request, Has.Property("Label").EqualTo("My Export"));
        }
    }
}
