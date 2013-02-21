using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NetworkedPlanet.Brightstar;
using NetworkedPlanet.Brightstar.Storage;
using NetworkedPlanet.Rdf;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using System.Linq;

namespace NetworkedPlanet.Brightstar.Tests.Sparql11TestSuite {
    [TestClass]
	public partial class Functions : SparqlTest {

        public Functions() : base()
        {
            
        }

		[TestInitialize]
		public void SetUp()
		{
			CreateStore();
		    
		}

        [TestCleanup]
        public void TearDown()
        {
			DeleteStore();
            
        }

		#region Test Methods

		[TestMethod]
		public void Plus1() {
	
					ImportData(@"functions/data-builtin-3.ttl");
		
		
			var result = ExecuteQuery(@"functions/plus-1.rq");
			CheckResult(result, @"functions/plus-1.srx", false);

	
		}

		[TestMethod]
		public void Plus2() {
	
					ImportData(@"functions/data-builtin-3.ttl");
		
		
			var result = ExecuteQuery(@"functions/plus-2.rq");
			CheckResult(result, @"functions/plus-2.srx", false);

	
		}

		[TestMethod]
		public void Strdt() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/strdt01.rq");
			CheckResult(result, @"functions/strdt01.srx", false);

	
		}

		[TestMethod]
		public void StrdtStr() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/strdt02.rq");
			CheckResult(result, @"functions/strdt02.srx", false);

	
		}

		[TestMethod]
		public void StrdtTypeerrors() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/strdt03.rq");
			CheckResult(result, @"functions/strdt03.srx", false);

	
		}

		[TestMethod]
		public void Strlang() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/strlang01.rq");
			CheckResult(result, @"functions/strlang01.srx", false);

	
		}

		[TestMethod]
		public void StrlangStr() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/strlang02.rq");
			CheckResult(result, @"functions/strlang02.srx", false);

	
		}

		[TestMethod]
		public void StrlangTypeerrors() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/strlang03.rq");
			CheckResult(result, @"functions/strlang03.srx", false);

	
		}

		[TestMethod]
		public void Isnumeric() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/isnumeric01.rq");
			CheckResult(result, @"functions/isnumeric01.srx", false);

	
		}

		[TestMethod]
		public void Abs() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/abs01.rq");
			CheckResult(result, @"functions/abs01.srx", false);

	
		}

		[TestMethod]
		public void Ceil() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/ceil01.rq");
			CheckResult(result, @"functions/ceil01.srx", false);

	
		}

		[TestMethod]
		public void Floor() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/floor01.rq");
			CheckResult(result, @"functions/floor01.srx", false);

	
		}

		[TestMethod]
		public void Round() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/round01.rq");
			CheckResult(result, @"functions/round01.srx", false);

	
		}

		[TestMethod]
		public void Concat() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/concat01.rq");
			CheckResult(result, @"functions/concat01.srx", false);

	
		}

		[TestMethod]
		public void Concat2() {
	
					ImportData(@"functions/data2.ttl");
		
		
			var result = ExecuteQuery(@"functions/concat02.rq");
			CheckResult(result, @"functions/concat02.srx", false);

	
		}

		[TestMethod]
		public void Substr3Argument() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/substring01.rq");
			CheckResult(result, @"functions/substring01.srx", false);

	
		}

		[TestMethod]
		public void Substr2Argument() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/substring02.rq");
			CheckResult(result, @"functions/substring02.srx", false);

	
		}

		[TestMethod]
		public void Strlen() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/length01.rq");
			CheckResult(result, @"functions/length01.srx", false);

	
		}

		[TestMethod]
		public void Ucase() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/ucase01.rq");
			CheckResult(result, @"functions/ucase01.srx", false);

	
		}

		[TestMethod]
		public void Lcase() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/lcase01.rq");
			CheckResult(result, @"functions/lcase01.srx", false);

	
		}

		[TestMethod]
		public void Encode_for_uri() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/encode01.rq");
			CheckResult(result, @"functions/encode01.srx", false);

	
		}

		[TestMethod]
		public void Contains() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/contains01.rq");
			CheckResult(result, @"functions/contains01.srx", false);

	
		}

		[TestMethod]
		public void Strstarts() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/starts01.rq");
			CheckResult(result, @"functions/starts01.srx", false);

	
		}

		[TestMethod]
		public void Strends() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/ends01.rq");
			CheckResult(result, @"functions/ends01.srx", false);

	
		}

		[TestMethod]
		public void Md5() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/md5-01.rq");
			CheckResult(result, @"functions/md5-01.srx", false);

	
		}

		[TestMethod]
		public void Md5OverUnicodeData() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/md5-02.rq");
			CheckResult(result, @"functions/md5-02.srx", false);

	
		}

		[TestMethod]
		public void Sha1() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/sha1-01.rq");
			CheckResult(result, @"functions/sha1-01.srx", false);

	
		}

		[TestMethod]
		public void Sha1OnUnicodeData() {
	
					ImportData(@"functions/hash-unicode.ttl");
		
		
			var result = ExecuteQuery(@"functions/sha1-02.rq");
			CheckResult(result, @"functions/sha1-02.srx", false);

	
		}

		[TestMethod]
		public void Sha256() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/sha256-01.rq");
			CheckResult(result, @"functions/sha256-01.srx", false);

	
		}

		[TestMethod]
		public void Sha256OnUnicodeData() {
	
					ImportData(@"functions/hash-unicode.ttl");
		
		
			var result = ExecuteQuery(@"functions/sha256-02.rq");
			CheckResult(result, @"functions/sha256-02.srx", false);

	
		}

		[TestMethod]
		public void Sha512() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/sha512-01.rq");
			CheckResult(result, @"functions/sha512-01.srx", false);

	
		}

		[TestMethod]
		public void Sha512OnUnicodeData() {
	
					ImportData(@"functions/hash-unicode.ttl");
		
		
			var result = ExecuteQuery(@"functions/sha512-02.rq");
			CheckResult(result, @"functions/sha512-02.srx", false);

	
		}

		[TestMethod]
		public void Hours() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/hours-01.rq");
			CheckResult(result, @"functions/hours-01.srx", false);

	
		}

		[TestMethod]
		public void Minutes() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/minutes-01.rq");
			CheckResult(result, @"functions/minutes-01.srx", false);

	
		}

		[TestMethod]
		public void Seconds() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/seconds-01.rq");
			CheckResult(result, @"functions/seconds-01.srx", false);

	
		}

		[TestMethod]
		public void Year() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/year-01.rq");
			CheckResult(result, @"functions/year-01.srx", false);

	
		}

		[TestMethod]
		public void Month() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/month-01.rq");
			CheckResult(result, @"functions/month-01.srx", false);

	
		}

		[TestMethod]
		public void Day() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/day-01.rq");
			CheckResult(result, @"functions/day-01.srx", false);

	
		}

		[TestMethod]
		public void Timezone() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/timezone-01.rq");
			CheckResult(result, @"functions/timezone-01.srx", false);

	
		}

		[TestMethod]
		public void Tz() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/tz-01.rq");
			CheckResult(result, @"functions/tz-01.srx", false);

	
		}

		[TestMethod]
		public void BnodeStr() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/bnode01.rq");
			CheckResult(result, @"functions/bnode01.srx", false);

	
		}

		[TestMethod]
		public void In1() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/in01.rq");
			CheckResult(result, @"functions/in01.srx", false);

	
		}

		[TestMethod]
		public void In2() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/in02.rq");
			CheckResult(result, @"functions/in02.srx", false);

	
		}

		[TestMethod]
		public void NotIn1() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/notin01.rq");
			CheckResult(result, @"functions/notin01.srx", false);

	
		}

		[TestMethod]
		public void NotIn2() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/notin02.rq");
			CheckResult(result, @"functions/notin02.srx", false);

	
		}

		[TestMethod]
		public void Now() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/now01.rq");
			CheckResult(result, @"functions/now01.srx", false);

	
		}

		[TestMethod]
		public void Rand() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/rand01.rq");
			CheckResult(result, @"functions/rand01.srx", false);

	
		}

		[TestMethod]
		public void Bnode() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/bnode02.rq");
			CheckResult(result, @"functions/bnode02.srx", false);

	
		}

		[TestMethod]
		public void IriUri() {
	
					ImportData(@"functions/data.ttl");
		
		
			var result = ExecuteQuery(@"functions/iri01.rq");
			CheckResult(result, @"functions/iri01.srx", false);

	
		}

		[TestMethod]
		public void If() {
	
					ImportData(@"functions/data2.ttl");
		
		
			var result = ExecuteQuery(@"functions/if01.rq");
			CheckResult(result, @"functions/if01.srx", false);

	
		}

		[TestMethod]
		public void IfErrorPropogation() {
	
					ImportData(@"functions/data2.ttl");
		
		
			var result = ExecuteQuery(@"functions/if02.rq");
			CheckResult(result, @"functions/if02.srx", false);

	
		}

		[TestMethod]
		public void Coalesce() {
	
					ImportData(@"functions/data-coalesce.ttl");
		
		
			var result = ExecuteQuery(@"functions/coalesce01.rq");
			CheckResult(result, @"functions/coalesce01.srx", false);

	
		}

		[TestMethod]
		public void Strbefore() {
	
					ImportData(@"functions/data2.ttl");
		
		
			var result = ExecuteQuery(@"functions/strbefore01.rq");
			CheckResult(result, @"functions/strbefore01.srx", false);

	
		}

		[TestMethod]
		public void Strafter() {
	
					ImportData(@"functions/data2.ttl");
		
		
			var result = ExecuteQuery(@"functions/strafter01.rq");
			CheckResult(result, @"functions/strafter01.srx", false);

	
		}

		[TestMethod]
		public void Replace() {
	
					ImportData(@"functions/data3.ttl");
		
		
			var result = ExecuteQuery(@"functions/replace01.rq");
			CheckResult(result, @"functions/replace01.srx", false);

	
		}

		[TestMethod]
		public void ReplaceWithOverlappingPattern() {
	
					ImportData(@"functions/data3.ttl");
		
		
			var result = ExecuteQuery(@"functions/replace02.rq");
			CheckResult(result, @"functions/replace02.srx", false);

	
		}

		[TestMethod]
		public void ReplaceWithCapturedSubstring() {
	
					ImportData(@"functions/data3.ttl");
		
		
			var result = ExecuteQuery(@"functions/replace03.rq");
			CheckResult(result, @"functions/replace03.srx", false);

	
		}

		#endregion

		
	}
}