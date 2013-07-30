using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System;
using BrightstarDB;
using BrightstarDB.Rdf;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using System.Linq;
using BrightstarDB.Server;

namespace BrightstarDB.Tests.SparqlDawgTests {

    [TestFixture]
    [Ignore] // For now just run the SPARQL 1.1 tests that are in InternalTests
    public partial class ManifestAll : SparqlTest
    {
        [SetUp]
        public void SetUp()
        {
            CreateStore();

        }

        [TearDown]
        public void TearDown()
        {
            DeleteStore();

        }

        #region Test Methods

        [Test]
        public void Count1()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg01.srx", false);


        }

        [Test]
        public void Count2()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg02.srx", false);


        }

        [Test]
        public void Count3()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg03.srx", false);


        }

        [Test]
        public void Count4()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg04.srx", false);


        }

        [Test]
        public void Count5()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg05.srx", false);


        }

        [Test]
        public void Count6()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg06.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg06.srx", false);


        }

        [Test]
        public void Count7()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg07.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg07.srx", false);


        }

        [Test]
        public void Count8b()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg08.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg08b.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg08b.srx", false);


        }

        [Test]
        public void ErrorInAvg()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-err-01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-err-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-err-01.srx", false);


        }

        [Test]
        public void ProtectFromErrorInAvg()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-err-02.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-err-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-err-02.srx", false);


        }

        [Test]
        public void Constructwhere01ConstructWhere()
        {

            ImportData(@"sparqlDawgTests/construct/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/construct/constructwhere01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/construct/constructwhere01result.ttl", false);


        }

        [Test]
        public void Constructwhere02ConstructWhere()
        {

            ImportData(@"sparqlDawgTests/construct/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/construct/constructwhere02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/construct/constructwhere02result.ttl", false);


        }

        [Test]
        public void Constructwhere03ConstructWhere()
        {

            ImportData(@"sparqlDawgTests/construct/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/construct/constructwhere03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/construct/constructwhere03result.ttl", false);


        }

        [Test]
        public void Csv01CsvResultFormat()
        {

            ImportData(@"sparqlDawgTests/csv-tsv-results/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/csv-tsv-results/csvtsv01.rq", SparqlResultsFormat.Csv);
            CheckResult(result, @"sparqlDawgTests/csv-tsv-results/csvtsv01.csv", false);


        }

        [Test]
        public void Cvs02CsvResultFormat()
        {

            ImportData(@"sparqlDawgTests/csv-tsv-results/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/csv-tsv-results/csvtsv02.rq", SparqlResultsFormat.Csv);
            CheckResult(result, @"sparqlDawgTests/csv-tsv-results/csvtsv02.csv", false);


        }

        [Test]
        public void Csv03CsvResultFormat()
        {

            ImportData(@"sparqlDawgTests/csv-tsv-results/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/csv-tsv-results/csvtsv01.rq", SparqlResultsFormat.Csv);
            CheckResult(result, @"sparqlDawgTests/csv-tsv-results/csvtsv03.csv", false);


        }

        [Test]
        public void Tsv01TsvResultFormat()
        {

            ImportData(@"sparqlDawgTests/csv-tsv-results/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/csv-tsv-results/csvtsv01.rq", SparqlResultsFormat.Tsv);
            CheckResult(result, @"sparqlDawgTests/csv-tsv-results/csvtsv01.tsv", false);


        }

        [Test]
        public void Tvs02TsvResultFormat()
        {

            ImportData(@"sparqlDawgTests/csv-tsv-results/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/csv-tsv-results/csvtsv02.rq", SparqlResultsFormat.Tsv);
            CheckResult(result, @"sparqlDawgTests/csv-tsv-results/csvtsv02.tsv", false);


        }

        [Test]
        public void Tsv03TsvResultFormat()
        {

            ImportData(@"sparqlDawgTests/csv-tsv-results/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/csv-tsv-results/csvtsv01.rq", SparqlResultsFormat.Tsv);
            CheckResult(result, @"sparqlDawgTests/csv-tsv-results/csvtsv03.tsv", false);


        }

        [Test]
        public void ExistsWithinGraphPattern()
        {

            ImportData(@"sparqlDawgTests/exists/exists01.ttl");

            ImportGraph("sparqlDawgTests/exists/exists02.ttl",
                        new Uri(
                            @"file:///D:/Projects/brightstar/working/src/core/BrightstarDB.Tests/Data/sparql/sparqlDawgTests/exists/exists02.ttl"));

            var result = ExecuteQuery(@"sparqlDawgTests/exists/exists03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/exists/exists03.srx", false);


        }

        [Test]
        public void Plus1()
        {

            ImportData(@"sparqlDawgTests/functions/data-builtin-3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/plus-1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/plus-1.srx", false);


        }

        [Test]
        public void Plus2()
        {

            ImportData(@"sparqlDawgTests/functions/data-builtin-3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/plus-2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/plus-2.srx", false);


        }

        [Test]
        public void Group1()
        {

            ImportData(@"sparqlDawgTests/grouping/group-data-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/grouping/group01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/grouping/group01.srx", false);


        }

        [Test]
        public void Group2()
        {

            ImportData(@"sparqlDawgTests/grouping/group-data-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/grouping/group02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/grouping/group02.srx", false);


        }

        [Test]
        public void Group3()
        {

            ImportData(@"sparqlDawgTests/grouping/group-data-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/grouping/group03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/grouping/group03.srx", false);


        }

        [Test]
        public void Group4()
        {

            ImportData(@"sparqlDawgTests/grouping/group-data-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/grouping/group04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/grouping/group04.srx", false);


        }

        [Test]
        public void Group5()
        {

            ImportData(@"sparqlDawgTests/grouping/group-data-2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/grouping/group05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/grouping/group05.srx", false);


        }

        [Test]
        public void Jsonres01JsonResultFormat()
        {

            ImportData(@"sparqlDawgTests/json-res/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/json-res/jsonres01.rq", SparqlResultsFormat.Json);
            CheckResult(result, @"sparqlDawgTests/json-res/jsonres01.srj", false);


        }

        [Test]
        public void Jsonres02JsonResultFormat()
        {

            ImportData(@"sparqlDawgTests/json-res/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/json-res/jsonres02.rq", SparqlResultsFormat.Json);
            CheckResult(result, @"sparqlDawgTests/json-res/jsonres02.srj", false);


        }

        [Test]
        public void Jsonres03JsonResultFormat()
        {

            ImportData(@"sparqlDawgTests/json-res/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/json-res/jsonres03.rq", SparqlResultsFormat.Json);
            CheckResult(result, @"sparqlDawgTests/json-res/jsonres03.srj", false);


        }

        [Test]
        public void Jsonres04JsonResultFormat()
        {

            ImportData(@"sparqlDawgTests/json-res/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/json-res/jsonres04.rq", SparqlResultsFormat.Json);
            CheckResult(result, @"sparqlDawgTests/json-res/jsonres04.srj", false);


        }

        [Test]
        public void Pp37Nested_Asterix__Asterix_()
        {

            ImportData(@"sparqlDawgTests/property-path/pp37.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp37.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp37.srx", false);


        }

        [Test]
        public void Sq11SubqueryLimitPerResource()
        {

            ImportData(@"sparqlDawgTests/subquery/sq11.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq11.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq11.srx", false);


        }

        [Test]
        public void Sq12SubqueryInConstructWithBuiltIns()
        {

            ImportData(@"sparqlDawgTests/subquery/sq12.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq12.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq12_out.ttl", false);


        }

        [Test]
        public void Sq13SubqueriesDonTInjectBindings()
        {

            ImportData(@"sparqlDawgTests/subquery/sq11.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq11.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq11.srx", false);


        }

        [Test]
        public void Group_concat1()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-groupconcat-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-groupconcat-1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-groupconcat-1.srx", false);


        }

        [Test]
        public void Group_concat2()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-groupconcat-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-groupconcat-2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-groupconcat-2.srx", false);


        }

        [Test]
        public void Group_concatWithSeparator()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-groupconcat-1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-groupconcat-3.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-groupconcat-3.srx", false);


        }

        [Test]
        public void Avg()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-avg-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-avg-01.srx", false);


        }

        [Test]
        public void AvgWithGroupBy()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-avg-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-avg-02.srx", false);


        }

        [Test]
        public void Min()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-min-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-min-01.srx", false);


        }

        [Test]
        public void MinWithGroupBy()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-min-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-min-02.srx", false);


        }

        [Test]
        public void Max()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-max-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-max-01.srx", false);


        }

        [Test]
        public void MaxWithGroupBy()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-max-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-max-02.srx", false);


        }

        [Test]
        public void Sum()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-sum-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-sum-01.srx", false);


        }

        [Test]
        public void SumWithGroupBy()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-sum-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-sum-02.srx", false);


        }

        [Test]
        public void Sample()
        {

            ImportData(@"sparqlDawgTests/aggregates/agg-numeric.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-sample-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-sample-01.srx", false);


        }

        [Test]
        public void AggEmptyGroup()
        {

            ImportData(@"sparqlDawgTests/aggregates/empty.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-empty-group.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-empty-group.srx", false);


        }

        [Test]
        public void AggregateOverEmptyGroupResultingInARowWithUnboundVariables()
        {

            ImportData(@"sparqlDawgTests/aggregates/empty.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/aggregates/agg-empty-group.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/aggregates/agg-empty-group.srx", false);


        }

        [Test]
        public void Bind01Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind01.srx", false);


        }

        [Test]
        public void Bind02Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind02.srx", false);


        }

        [Test]
        public void Bind03Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind03.srx", false);


        }

        [Test]
        public void Bind04Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind04.srx", false);


        }

        [Test]
        public void Bind05Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind05.srx", false);


        }

        [Test]
        public void Bind06Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind06.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind06.srx", false);


        }

        [Test]
        public void Bind07Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind07.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind07.srx", false);


        }

        [Test]
        public void Bind08Bind()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind08.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind08.srx", false);


        }

        [Test]
        public void Bind10BindScopingVariableInFilterNotInScope()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind10.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind10.srx", false);


        }

        [Test]
        public void Bind11BindScopingVariableInFilterInScope()
        {

            ImportData(@"sparqlDawgTests/bind/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bind/bind11.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bind/bind11.srx", false);


        }

        [Test]
        public void PostQueryValuesWithSubjVar1Row()
        {

            ImportData(@"sparqlDawgTests/bindings/data01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values01.srx", false);


        }

        [Test]
        public void PostQueryValuesWithObjVar1Row()
        {

            ImportData(@"sparqlDawgTests/bindings/data02.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values02.srx", false);


        }

        [Test]
        public void PostQueryValuesWith2ObjVars1Row()
        {

            ImportData(@"sparqlDawgTests/bindings/data03.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values03.srx", false);


        }

        [Test]
        public void PostQueryValuesWith2ObjVars1RowWithUndef()
        {

            ImportData(@"sparqlDawgTests/bindings/data04.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values04.srx", false);


        }

        [Test]
        public void PostQueryValuesWith2ObjVars2RowsWithUndef()
        {

            ImportData(@"sparqlDawgTests/bindings/data05.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values05.srx", false);


        }

        [Test]
        public void PostQueryValuesWithPredVar1Row()
        {

            ImportData(@"sparqlDawgTests/bindings/data06.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values06.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values06.srx", false);


        }

        [Test]
        public void PostQueryValuesWithOptionalObjVar1Row()
        {

            ImportData(@"sparqlDawgTests/bindings/data07.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values07.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values07.srx", false);


        }

        [Test]
        public void PostQueryValuesWithSubjObjVars2RowsWithUndef()
        {

            ImportData(@"sparqlDawgTests/bindings/data08.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/values08.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/values08.srx", false);


        }

        [Test]
        public void InlineValuesGraphPattern()
        {

            ImportData(@"sparqlDawgTests/bindings/data01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/inline01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/inline01.srx", false);


        }

        [Test]
        public void PostSubqueryValues()
        {

            ImportData(@"sparqlDawgTests/bindings/data02.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/bindings/inline02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/bindings/inline02.srx", false);


        }

        [Test]
        public void ExistsWithOneConstant()
        {

            ImportData(@"sparqlDawgTests/exists/exists01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/exists/exists01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/exists/exists01.srx", false);


        }

        [Test]
        public void ExistsWithGroundTriple()
        {

            ImportData(@"sparqlDawgTests/exists/exists01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/exists/exists02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/exists/exists02.srx", false);


        }

        [Test]
        public void NestedPositiveExists()
        {

            ImportData(@"sparqlDawgTests/exists/exists01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/exists/exists04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/exists/exists04.srx", false);


        }

        [Test]
        public void NestedNegativeExistsInPositiveExists()
        {

            ImportData(@"sparqlDawgTests/exists/exists01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/exists/exists05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/exists/exists05.srx", false);


        }

        [Test]
        public void Strdt()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strdt01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strdt01.srx", false);


        }

        [Test]
        public void StrdtStr()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strdt02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strdt02.srx", false);


        }

        [Test]
        public void StrdtTypeerrors()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strdt03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strdt03.srx", false);


        }

        [Test]
        public void Strlang()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strlang01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strlang01.srx", false);


        }

        [Test]
        public void StrlangStr()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strlang02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strlang02.srx", false);


        }

        [Test]
        public void StrlangTypeerrors()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strlang03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strlang03.srx", false);


        }

        [Test]
        public void Isnumeric()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/isnumeric01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/isnumeric01.srx", false);


        }

        [Test]
        public void Abs()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/abs01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/abs01.srx", false);


        }

        [Test]
        public void Ceil()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/ceil01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/ceil01.srx", false);


        }

        [Test]
        public void Floor()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/floor01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/floor01.srx", false);


        }

        [Test]
        public void Round()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/round01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/round01.srx", false);


        }

        [Test]
        public void Concat()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/concat01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/concat01.srx", false);


        }

        [Test]
        public void Concat2()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/concat02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/concat02.srx", false);


        }

        [Test]
        public void Substr3Argument()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/substring01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/substring01.srx", false);


        }

        [Test]
        public void Substr2Argument()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/substring02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/substring02.srx", false);


        }

        [Test]
        public void Strlen()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/length01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/length01.srx", false);


        }

        [Test]
        public void Ucase()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/ucase01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/ucase01.srx", false);


        }

        [Test]
        public void Lcase()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/lcase01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/lcase01.srx", false);


        }

        [Test]
        public void Encode_for_uri()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/encode01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/encode01.srx", false);


        }

        [Test]
        public void Contains()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/contains01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/contains01.srx", false);


        }

        [Test]
        public void Strstarts()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/starts01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/starts01.srx", false);


        }

        [Test]
        public void Strends()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/ends01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/ends01.srx", false);


        }

        [Test]
        public void Md5()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/md5-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/md5-01.srx", false);


        }

        [Test]
        public void Md5OverUnicodeData()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/md5-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/md5-02.srx", false);


        }

        [Test]
        public void Sha1()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/sha1-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/sha1-01.srx", false);


        }

        [Test]
        public void Sha1OnUnicodeData()
        {

            ImportData(@"sparqlDawgTests/functions/hash-unicode.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/sha1-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/sha1-02.srx", false);


        }

        [Test]
        public void Sha256()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/sha256-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/sha256-01.srx", false);


        }

        [Test]
        public void Sha256OnUnicodeData()
        {

            ImportData(@"sparqlDawgTests/functions/hash-unicode.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/sha256-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/sha256-02.srx", false);


        }

        [Test]
        public void Sha512()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/sha512-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/sha512-01.srx", false);


        }

        [Test]
        public void Sha512OnUnicodeData()
        {

            ImportData(@"sparqlDawgTests/functions/hash-unicode.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/sha512-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/sha512-02.srx", false);


        }

        [Test]
        public void Hours()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/hours-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/hours-01.srx", false);


        }

        [Test]
        public void Minutes()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/minutes-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/minutes-01.srx", false);


        }

        [Test]
        public void Seconds()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/seconds-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/seconds-01.srx", false);


        }

        [Test]
        public void Year()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/year-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/year-01.srx", false);


        }

        [Test]
        public void Month()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/month-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/month-01.srx", false);


        }

        [Test]
        public void Day()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/day-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/day-01.srx", false);


        }

        [Test]
        public void Timezone()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/timezone-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/timezone-01.srx", false);


        }

        [Test]
        public void Tz()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/tz-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/tz-01.srx", false);


        }

        [Test]
        public void BnodeStr()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/bnode01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/bnode01.srx", false);


        }

        [Test]
        public void In1()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/in01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/in01.srx", false);


        }

        [Test]
        public void In2()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/in02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/in02.srx", false);


        }

        [Test]
        public void NotIn1()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/notin01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/notin01.srx", false);


        }

        [Test]
        public void NotIn2()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/notin02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/notin02.srx", false);


        }

        [Test]
        public void Now()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/now01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/now01.srx", false);


        }

        [Test]
        public void Rand()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/rand01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/rand01.srx", false);


        }

        [Test]
        public void Bnode()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/bnode02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/bnode02.srx", false);


        }

        [Test]
        public void IriUri()
        {

            ImportData(@"sparqlDawgTests/functions/data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/iri01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/iri01.srx", false);


        }

        [Test]
        public void If()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/if01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/if01.srx", false);


        }

        [Test]
        public void IfErrorPropogation()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/if02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/if02.srx", false);


        }

        [Test]
        public void Coalesce()
        {

            ImportData(@"sparqlDawgTests/functions/data-coalesce.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/coalesce01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/coalesce01.srx", false);


        }

        [Test]
        public void Strbefore()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strbefore01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strbefore01.srx", false);


        }

        [Test]
        public void StrbeforeAlt()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strbefore01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strbefore01a.srx", false);


        }

        [Test]
        public void StrbeforeDatatyping()
        {

            ImportData(@"sparqlDawgTests/functions/data4.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strbefore02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strbefore02.srx", false);


        }

        [Test]
        public void Strafter()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strafter01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strafter01.srx", false);


        }

        [Test]
        public void StrafterAlt()
        {

            ImportData(@"sparqlDawgTests/functions/data2.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strafter01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strafter01a.srx", false);


        }

        [Test]
        public void StrafterDatatyping()
        {

            ImportData(@"sparqlDawgTests/functions/data4.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/strafter02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/strafter02.srx", false);


        }

        [Test]
        public void Replace()
        {

            ImportData(@"sparqlDawgTests/functions/data3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/replace01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/replace01.srx", false);


        }

        [Test]
        public void ReplaceWithOverlappingPattern()
        {

            ImportData(@"sparqlDawgTests/functions/data3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/replace02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/replace02.srx", false);


        }

        [Test]
        public void ReplaceWithCapturedSubstring()
        {

            ImportData(@"sparqlDawgTests/functions/data3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/replace03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/replace03.srx", false);


        }

        [Test]
        public void UuidPatternMatch()
        {

            ImportData(@"sparqlDawgTests/functions/data-empty.nt");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/uuid01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/uuid01.srx", false);


        }

        [Test]
        public void StruuidPatternMatch()
        {

            ImportData(@"sparqlDawgTests/functions/data-empty.nt");


            var result = ExecuteQuery(@"sparqlDawgTests/functions/struuid01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/functions/struuid01.srx", false);


        }

        [Test]
        public void SubsetsByExclusionNotExists()
        {

            ImportData(@"sparqlDawgTests/negation/subsetByExcl.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/subsetByExcl01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/subsetByExcl01.srx", false);


        }

        [Test]
        public void SubsetsByExclusionMinus()
        {

            ImportData(@"sparqlDawgTests/negation/subsetByExcl.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/subsetByExcl02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/subsetByExcl02.srx", false);


        }

        [Test]
        public void MedicalTemporalProximityByExclusionNotExists()
        {

            ImportData(@"sparqlDawgTests/negation/temporalProximity01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/temporalProximity01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/temporalProximity01.srx", false);


        }

        [Test]
        public void MedicalTemporalProximityByExclusionMinus()
        {

            ImportData(@"sparqlDawgTests/negation/temporalProximity02.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/temporalProximity02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/temporalProximity02.srx", false);


        }

        [Test]
        public void CalculateWhichSetsAreSubsetsOfOthersIncludeASubsetofA()
        {

            ImportData(@"sparqlDawgTests/negation/set-data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/subset-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/subset-01.srx", false);


        }

        [Test]
        public void CalculateWhichSetsAreSubsetsOfOthersExcludeASubsetofA()
        {

            ImportData(@"sparqlDawgTests/negation/set-data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/subset-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/subset-02.srx", false);


        }

        [Test]
        public void CalculateWhichSetsHaveTheSameElements()
        {

            ImportData(@"sparqlDawgTests/negation/set-data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/set-equals-1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/set-equals-1.srx", false);


        }

        [Test]
        public void CalculateProperSubset()
        {

            ImportData(@"sparqlDawgTests/negation/set-data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/subset-03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/subset-03.srx", false);


        }

        [Test]
        public void PositiveExists1()
        {

            ImportData(@"sparqlDawgTests/negation/set-data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/exists-01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/exists-01.srx", false);


        }

        [Test]
        public void PositiveExists2()
        {

            ImportData(@"sparqlDawgTests/negation/set-data.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/exists-02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/exists-02.srx", false);


        }

        [Test]
        public void SubtractionWithMinusFromAFullyBoundMinuend()
        {

            ImportData(@"sparqlDawgTests/negation/full-minuend.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/full-minuend.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/full-minuend.srx", false);


        }

        [Test]
        public void SubtractionWithMinusFromAPartiallyBoundMinuend()
        {

            ImportData(@"sparqlDawgTests/negation/part-minuend.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/negation/part-minuend.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/negation/part-minuend.srx", false);


        }

        [Test]
        public void ExpressionIsEquality()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp01.srx", false);


        }

        [Test]
        public void ExpressionRaiseAnError()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp02.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp02.srx", false);


        }

        [Test]
        public void ReuseAProjectExpressionVariableInSelect()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp03.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp03.srx", false);


        }

        [Test]
        public void ReuseAProjectExpressionVariableInOrderBy()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp04.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp04.srx", false);


        }

        [Test]
        public void ExpressionMayReturnNoValue()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp05.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp05.srx", false);


        }

        [Test]
        public void ExpressionHasUndefinedVariable()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp06.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp06.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp06.srx", false);


        }

        [Test]
        public void ExpressionHasVariableThatMayBeUnbound()
        {

            ImportData(@"sparqlDawgTests/project-expression/projexp07.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/project-expression/projexp07.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/project-expression/projexp07.srx", false);


        }

        [Test]
        public void Pp01SimplePath()
        {

            ImportData(@"sparqlDawgTests/property-path/pp01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp01.srx", false);


        }

        [Test]
        public void Pp02StarPath()
        {

            ImportData(@"sparqlDawgTests/property-path/pp01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp02.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp02.srx", false);


        }

        [Test]
        public void Pp03SimplePathWithLoop()
        {

            ImportData(@"sparqlDawgTests/property-path/pp03.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp03.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp03.srx", false);


        }

        [Test]
        public void Pp04VariableLengthPathWithLoop()
        {

            ImportData(@"sparqlDawgTests/property-path/pp03.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp04.srx", false);


        }

        [Test]
        public void Pp05ZeroLengthPath()
        {

            ImportData(@"sparqlDawgTests/property-path/pp05.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp05.srx", false);


        }

        [Test]
        public void Pp08ReversePath()
        {

            ImportData(@"sparqlDawgTests/property-path/pp08.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp08.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp08.srx", false);


        }

        [Test]
        public void Pp09ReverseSequencePath()
        {

            ImportData(@"sparqlDawgTests/property-path/pp09.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp09.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp09.srx", false);


        }

        [Test]
        public void Pp10PathWithNegation()
        {

            ImportData(@"sparqlDawgTests/property-path/pp10.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp10.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp10.srx", false);


        }

        [Test]
        public void Pp11SimplePathAndTwoPathsToSameTargetNode()
        {

            ImportData(@"sparqlDawgTests/property-path/pp11.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp11.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp11.srx", false);


        }

        [Test]
        public void Pp12VariableLengthPathAndTwoPathsToSameTargetNode()
        {

            ImportData(@"sparqlDawgTests/property-path/pp11.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp12.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp12.srx", false);


        }

        [Test]
        public void Pp13ZeroLengthPathsWithLiterals()
        {

            ImportData(@"sparqlDawgTests/property-path/pp13.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp13.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp13.srx", false);


        }

        [Test]
        public void Pp14StarPathOverFoafKnows()
        {

            ImportData(@"sparqlDawgTests/property-path/pp14.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp14.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp14.srx", false);


        }

        [Test]
        public void Pp15ZeroLengthPathsOnAnEmptyGraph()
        {

            ImportData(@"sparqlDawgTests/property-path/empty.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp15.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp15.srx", false);


        }

        [Test]
        public void Pp16DuplicatePathsAndCyclesThroughFoafKnows_Asterix_()
        {

            ImportData(@"sparqlDawgTests/property-path/pp16.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp14.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp16.srx", false);


        }

        [Test]
        public void Pp20DiamondP2()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-2-1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-1.srx", false);


        }

        [Test]
        public void Pp21DiamondP_Plus_()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-2-2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-2.srx", false);


        }

        [Test]
        public void Pp22DiamondWithTailP3()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-tail.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-2-3.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-tail-1.srx", false);


        }

        [Test]
        public void Pp23DiamondWithTailP_Plus_()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-tail.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-2-2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-tail-2.srx", false);


        }

        [Test]
        public void Pp24DiamondWithLoopP2()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-loop.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-2-1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-loop-1.srx", false);


        }

        [Test]
        public void Pp25DiamondWithLoopP_Plus_()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-loop.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-2-2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-loop-2.srx", false);


        }

        [Test]
        public void Pp26DiamondWithLoopP24()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-loop.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-3-1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-loop-3.srx", false);


        }

        [Test]
        public void Pp27DiamondWithLoopP3()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-loop.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-3-2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-loop-4.srx", false);


        }

        [Test]
        public void Pp28aDiamondWithLoopPP_QuestionMark_()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-loop.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-3-3.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-loop-5a.srx", false);


        }

        [Test]
        public void Pp29DiamondWithLoopP2()
        {

            ImportData(@"sparqlDawgTests/property-path/data-diamond-loop.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-3-4.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/diamond-loop-6.srx", false);


        }

        [Test]
        public void Pp30OperatorPrecedence1()
        {

            ImportData(@"sparqlDawgTests/property-path/path-p1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-p1.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/path-p1.srx", false);


        }

        [Test]
        public void Pp31OperatorPrecedence2()
        {

            ImportData(@"sparqlDawgTests/property-path/path-p1.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-p2.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/path-p2.srx", false);


        }

        [Test]
        public void Pp32OperatorPrecedence3()
        {

            ImportData(@"sparqlDawgTests/property-path/path-p3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-p3.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/path-p3.srx", false);


        }

        [Test]
        public void Pp33OperatorPrecedence4()
        {

            ImportData(@"sparqlDawgTests/property-path/path-p3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/path-p4.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/path-p4.srx", false);


        }

        [Test]
        public void Pp36ArbitraryPathWithBoundEndpoints()
        {

            ImportData(@"sparqlDawgTests/property-path/clique3.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/property-path/pp36.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/property-path/pp36.srx", false);


        }

        [Test]
        public void ServiceTest1()
        {

            ImportData(@"sparqlDawgTests/service/data01.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/service/service01.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/service/service01.srx", false);


        }

        [Test]
        public void ServiceTest4aWithValuesClause()
        {

            ImportData(@"sparqlDawgTests/service/data04.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/service/service04a.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/service/service04.srx", false);


        }

        [Test]
        public void ServiceTest5()
        {

            ImportData(@"sparqlDawgTests/service/data05.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/service/service05.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/service/service05.srx", false);


        }

        [Test]
        public void ServiceTest7()
        {

            ImportData(@"sparqlDawgTests/service/data07.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/service/service07.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/service/service07.srx", false);


        }

        [Test]
        public void Sq04SubqueryWithinGraphPatternDefaultGraphDoesNotApply()
        {

            ImportData(@"sparqlDawgTests/subquery/sq04.rdf");

            ImportGraph("sparqlDawgTests/subquery/sq01.rdf",
                        new Uri(
                            @"file:///D:/Projects/brightstar/working/src/core/BrightstarDB.Tests/Data/sparql/sparqlDawgTests/subquery/sq01.rdf"));

            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq04.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq04.srx", false);


        }

        [Test]
        public void Sq06SubqueryWithGraphPatternFromNamedApplies()
        {

            ImportData(@"sparqlDawgTests/subquery/sq05.rdf");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq06.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq06.srx", false);


        }

        [Test]
        public void Sq08SubqueryWithAggregate()
        {

            ImportData(@"sparqlDawgTests/subquery/sq08.rdf");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq08.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq08.srx", false);


        }

        [Test]
        public void Sq09NestedSubqueries()
        {

            ImportData(@"sparqlDawgTests/subquery/sq09.rdf");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq09.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq09.srx", false);


        }

        [Test]
        public void Sq10SubqueryWithExists()
        {

            ImportData(@"sparqlDawgTests/subquery/sq10.rdf");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq10.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq10.srx", false);


        }

        [Test]
        public void Sq14LimitByResource()
        {

            ImportData(@"sparqlDawgTests/subquery/sq14.ttl");


            var result = ExecuteQuery(@"sparqlDawgTests/subquery/sq14.rq", SparqlResultsFormat.Xml);
            CheckResult(result, @"sparqlDawgTests/subquery/sq14-out.ttl", false);


        }

        [Test]
        public void ClearDefault()
        {
            ImportData(@"sparqlDawgTests/clear/clear-default.ttl");
            ImportGraph(@"sparqlDawgTests/clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/clear/clear-default-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/clear/empty.ttl");
            ValidateGraph(@"sparqlDawgTests/clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void ClearGraph()
        {
            ImportData(@"sparqlDawgTests/clear/clear-default.ttl");
            ImportGraph(@"sparqlDawgTests/clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/clear/clear-graph-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/clear/clear-default.ttl");
            ValidateGraph(@"sparqlDawgTests/clear/empty.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void ClearNamed()
        {
            ImportData(@"sparqlDawgTests/clear/clear-default.ttl");
            ImportGraph(@"sparqlDawgTests/clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/clear/clear-named-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/clear/clear-default.ttl");
            ValidateGraph(@"sparqlDawgTests/clear/empty.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/clear/empty.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void ClearAll()
        {
            ImportData(@"sparqlDawgTests/clear/clear-default.ttl");
            ImportGraph(@"sparqlDawgTests/clear/clear-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/clear/clear-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/clear/clear-all-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/clear/empty.ttl");
            ValidateGraph(@"sparqlDawgTests/clear/empty.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/clear/empty.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void SimpleDeleteData1()
        {
            ImportData(@"sparqlDawgTests/delete-data/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-data/delete-data-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-data/delete-post-01s.ttl");

        }

        [Test]
        public void SimpleDeleteData2()
        {
            ImportGraph(@"sparqlDawgTests/delete-data/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete-data/delete-data-02.ru");
            ValidateGraph(@"sparqlDawgTests/delete-data/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void SimpleDeleteData3()
        {
            ImportData(@"sparqlDawgTests/delete-data/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-data/delete-data-03.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-data/delete-post-01f.ttl");

        }

        [Test]
        public void SimpleDeleteData4()
        {
            ImportGraph(@"sparqlDawgTests/delete-data/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete-data/delete-data-04.ru");
            ValidateGraph(@"sparqlDawgTests/delete-data/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void GraphSpecificDeleteData1()
        {
            ImportData(@"sparqlDawgTests/delete-data/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete-data/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete-data/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete-data/delete-data-05.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-data/delete-post-01s.ttl");
            ValidateGraph(@"sparqlDawgTests/delete-data/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete-data/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void GraphSpecificDeleteData2()
        {
            ImportData(@"sparqlDawgTests/delete-data/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete-data/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete-data/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete-data/delete-data-06.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-data/delete-post-01f.ttl");
            ValidateGraph(@"sparqlDawgTests/delete-data/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete-data/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void DeleteInsert1()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-01.ttl");

        }

        [Test]
        public void DeleteInsert1b()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-01b.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-01b.ttl");

        }

        [Test]
        public void DeleteInsert1c()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-01c.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-01b.ttl");

        }

        [Test]
        public void DeleteInsert2()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-02.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-02.ttl");

        }

        [Test]
        public void DeleteInsert4()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-04.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-02.ttl");

        }

        [Test]
        public void DeleteInsert4b()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-04b.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-02.ttl");

        }

        [Test]
        public void DeleteInsert5b()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-05b.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-post-05.ttl");

        }

        [Test]
        public void DeleteInsert6b()
        {
            ImportData(@"sparqlDawgTests/delete-insert/delete-insert-pre-06.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-insert/delete-insert-05b.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-insert/delete-insert-pre-06.ttl");

        }

        [Test]
        public void SimpleDeleteWhere1()
        {
            ImportData(@"sparqlDawgTests/delete-where/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-where/delete-where-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-where/delete-post-01s.ttl");

        }

        [Test]
        public void SimpleDeleteWhere2()
        {
            ImportGraph(@"sparqlDawgTests/delete-where/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete-where/delete-where-02.ru");
            ValidateGraph(@"sparqlDawgTests/delete-where/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void SimpleDeleteWhere3()
        {
            ImportData(@"sparqlDawgTests/delete-where/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete-where/delete-where-03.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-where/delete-post-01f.ttl");

        }

        [Test]
        public void SimpleDeleteWhere4()
        {
            ImportGraph(@"sparqlDawgTests/delete-where/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete-where/delete-where-04.ru");
            ValidateGraph(@"sparqlDawgTests/delete-where/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void GraphSpecificDeleteWhere1()
        {
            ImportData(@"sparqlDawgTests/delete-where/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete-where/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete-where/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete-where/delete-where-05.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-where/delete-post-01s.ttl");
            ValidateGraph(@"sparqlDawgTests/delete-where/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete-where/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void GraphSpecificDeleteWhere2()
        {
            ImportData(@"sparqlDawgTests/delete-where/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete-where/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete-where/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete-where/delete-where-06.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete-where/delete-post-01f.ttl");
            ValidateGraph(@"sparqlDawgTests/delete-where/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete-where/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void SimpleDelete1()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl");

        }

        [Test]
        public void SimpleDelete2()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-02.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void SimpleDelete3()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-03.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl");

        }

        [Test]
        public void SimpleDelete4()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-04.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void GraphSpecificDelete1()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-05.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void GraphSpecificDelete2()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-06.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void SimpleDelete7()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-07.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl");

        }

        [Test]
        public void SimpleDelete1With()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-with-01.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void SimpleDelete2With()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-with-02.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void SimpleDelete3With()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-with-03.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void SimpleDelete4With()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-with-04.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void GraphSpecificDelete1With()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-with-05.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01s2.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void GraphSpecificDelete2With()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-with-06.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void SimpleDelete1Using()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-using-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void SimpleDelete2Using()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-using-02.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01s.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void SimpleDelete3Using()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-01.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-using-03.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void SimpleDelete4Using()
        {
            ImportData(@"sparqlDawgTests/delete/delete-pre-03.ttl");
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-using-04.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void GraphSpecificDelete1Using()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-using-05.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01s2.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02f.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void GraphSpecificDelete2Using()
        {
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-01.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-02.ttl", new Uri(@"http://example.org/g2"));
            ImportGraph(@"sparqlDawgTests/delete/delete-pre-03.ttl", new Uri(@"http://example.org/g3"));
            ExecuteUpdate(@"sparqlDawgTests/delete/delete-using-06.ru");
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-01f.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-02s.ttl", new Uri(@"http://example.org/g2"));
            ValidateGraph(@"sparqlDawgTests/delete/delete-post-03f.ttl", new Uri(@"http://example.org/g3"));

        }

        [Test]
        public void DropDefault()
        {
            ImportData(@"sparqlDawgTests/drop/drop-default.ttl");
            ImportGraph(@"sparqlDawgTests/drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/drop/drop-default-01.ru");
            ValidateGraph(@"sparqlDawgTests/drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
            ValidateGraph(@"sparqlDawgTests/drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void DropGraph()
        {
            ImportData(@"sparqlDawgTests/drop/drop-default.ttl");
            ImportGraph(@"sparqlDawgTests/drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/drop/drop-graph-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/drop/drop-default.ttl");
            ValidateGraph(@"sparqlDawgTests/drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void DropNamed()
        {
            ImportData(@"sparqlDawgTests/drop/drop-default.ttl");
            ImportGraph(@"sparqlDawgTests/drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/drop/drop-named-01.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/drop/drop-default.ttl");

        }

        [Test]
        public void DropAll()
        {
            ImportData(@"sparqlDawgTests/drop/drop-default.ttl");
            ImportGraph(@"sparqlDawgTests/drop/drop-g1.ttl", new Uri(@"http://example.org/g1"));
            ImportGraph(@"sparqlDawgTests/drop/drop-g2.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/drop/drop-all-01.ru");

        }

        [Test]
        public void LoadSilent()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/load-silent.ru");

        }

        [Test]
        public void LoadSilentInto()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/load-silent-into.ru");

        }

        [Test]
        public void ClearSilentGraphIri()
        {
            ImportData(@"sparqlDawgTests/update-silent/spo.ttl");
            ExecuteUpdate(@"sparqlDawgTests/update-silent/clear-silent.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/update-silent/spo.ttl");

        }

        [Test]
        public void ClearSilentDefault()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/clear-default-silent.ru");

        }

        [Test]
        public void CreateSilentIri()
        {
            ImportGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g1"));
            ExecuteUpdate(@"sparqlDawgTests/update-silent/create-silent.ru");
            ValidateGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g1"));

        }

        [Test]
        public void DropSilentGraphIri()
        {
            ImportData(@"sparqlDawgTests/update-silent/spo.ttl");
            ExecuteUpdate(@"sparqlDawgTests/update-silent/drop-silent.ru");
            ValidateUnamedGraph(@"sparqlDawgTests/update-silent/spo.ttl");

        }

        [Test]
        public void DropSilentDefault()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/drop-default-silent.ru");

        }

        [Test]
        public void CopySilent()
        {
            ImportGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/update-silent/copy-silent.ru");
            ValidateGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void CopySilentToDefault()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/copy-to-default-silent.ru");

        }

        [Test]
        public void MoveSilent()
        {
            ImportGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/update-silent/move-silent.ru");
            ValidateGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void MoveSilentToDefault()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/move-to-default-silent.ru");

        }

        [Test]
        public void AddSilent()
        {
            ImportGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g2"));
            ExecuteUpdate(@"sparqlDawgTests/update-silent/add-silent.ru");
            ValidateGraph(@"sparqlDawgTests/update-silent/spo.ttl", new Uri(@"http://example.org/g2"));

        }

        [Test]
        public void AddSilentToDefault()
        {
            ExecuteUpdate(@"sparqlDawgTests/update-silent/add-to-default-silent.ru");

        }

        #endregion


    }
}