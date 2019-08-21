using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.Samples.Rdf.GettingStarted
{
    class Program
    {
        private static void Main(string[] args)
        {
            SamplesConfiguration.Register();

            // Create a new service client using a connection string. 
            var connectionString = String.Format(@"Type=embedded;storesDirectory={0};",
                                                 SamplesConfiguration.StoresDirectory);
            var client = BrightstarService.GetClient(connectionString);

            // create a new store
            string storeName = "RdfGettingStarted_" + Guid.NewGuid();
            client.CreateStore(storeName);

            // Define some NTriples data to insert into the store.
            // NTriple is a line based format. One good way to create data is to use
            // the StringBuilder class and AppendLine.
            var data = new StringBuilder();

            // data about the BrightstarDB product; name, and categories.
            data.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/name> \"Brightstar DB\" .");
            data.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/category> <http://www.networkedplanet.com/categories/nosql> .");
            data.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/category> <http://www.networkedplanet.com/categories/.net> .");
            data.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/category> <http://www.networkedplanet.com/categories/rdf> .");

            // data about the Networked Planet Web3 product; name, and categories.
            data.AppendLine(
                "<http://www.networkedplanet.com/products/web3> <http://www.networkedplanet.com/schemas/product/name> \"Web3 Platform\" .");
            data.AppendLine(
                "<http://www.networkedplanet.com/products/web3> <http://www.networkedplanet.com/schemas/product/category> <http://www.networkedplanet.com/categories/.net> .");
            data.AppendLine(
                "<http://www.networkedplanet.com/products/web3> <http://www.networkedplanet.com/schemas/product/category> <http://www.networkedplanet.com/categories/rdf> .");
            data.AppendLine(
                "<http://www.networkedplanet.com/products/web3> <http://www.networkedplanet.com/schemas/product/category> <http://www.networkedplanet.com/categories/topicmaps> .");

            Console.WriteLine("Inserting RDF triples into store.");

            // execute a transaction to insert the data into the store
            client.ExecuteTransaction(storeName, null, null, data.ToString());

            // SPARQL query for all the categories connected to BrightstarDB
            var query =
                "SELECT ?category WHERE { <http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/category> ?category }";

            // Create an XDocument from the SPARQL Result XML.
            // See http://www.w3.org/TR/rdf-sparql-XMLres/ for the XML format returned.
            var result = XDocument.Load(client.ExecuteQuery(storeName, query));
            Console.WriteLine("Executing SPARQL query:");
            Console.WriteLine(query);
            Console.WriteLine();
            // Use BrightstarDB Extension methods to iterate the result rows 
            // and pull out column values
            foreach (var sparqlResultRow in result.SparqlResultRows())
            {
                var val = sparqlResultRow.GetColumnValue("category");
                Console.WriteLine("Category is " + val);
            }

            Console.WriteLine();
            Console.WriteLine();

            // Deletion is done by matching patterns of triples to be deleted. 
            // There is a special Resource that can be used as a wildcard.
            // The following example deletes all the category data about BrightstarDB.
            // Again we use the StringBuilder to create the delete pattern.
            var deletePatternsData = new StringBuilder();
            deletePatternsData.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/category> <http://www.brightstardb.com/.well-known/model/wildcard> .");

            Console.WriteLine("Executing SPARQL query for deletion against store.");
            Console.WriteLine(deletePatternsData);
            client.ExecuteTransaction(storeName, null, deletePatternsData.ToString(), null);

            // SPARQL query for all the categories connected to BrightstarDB
            query =
                "SELECT ?category WHERE { <http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/category> ?category }";
            result = XDocument.Load(client.ExecuteQuery(storeName, query));

            var numberOfCategories = result.SparqlResultRows().Count();
            Console.WriteLine("Category count after delete (should be 0) is " + numberOfCategories);
            Console.WriteLine();
            Console.WriteLine();

            // data about the BrightstarDB product with literals defined with XML Schema data types
            var literals = new StringBuilder();
            literals.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/code> \"123\"^^<http://www.w3.org/2001/XMLSchema#integer> .");
            literals.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/releaseDate> \"2011-11-11 12:00\"^^<http://www.w3.org/2001/XMLSchema#dateTime> .");
            literals.AppendLine(
                "<http://www.networkedplanet.com/products/brightstar> <http://www.networkedplanet.com/schemas/product/cost> \"0.00\"^^<http://www.w3.org/2001/XMLSchema#decimal> .");

            Console.WriteLine("Inserting RDF triples into store.");

            // execute a transaction to insert the data into the store
            client.ExecuteTransaction(storeName, null, null, literals.ToString());

            const string queryAll = "SELECT ?o ?p ?l WHERE { ?o ?p ?l }";

            // Create an XDocument from the SPARQL Result XML.
            // See http://www.w3.org/TR/rdf-sparql-XMLres/ for the XML format returned.
            var resultAll = XDocument.Load(client.ExecuteQuery(storeName, queryAll));
            Console.WriteLine("Executing SPARQL query: {0}", queryAll);
            Console.WriteLine();
            // Use BrightstarDB Extension methods to iterate the result rows 
            // and pull out column values
            foreach (var sparqlResultRow in resultAll.SparqlResultRows())
            {
                var o = sparqlResultRow.GetColumnValue("o");
                var p = sparqlResultRow.GetColumnValue("p");
                var l = sparqlResultRow.GetColumnValue("l");
                Console.WriteLine("o= {0}\tp={1}\tl={2}", o, p, l);
            }

            // Shutdown Brightstar processing threads
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press the Return key to exit.");
            Console.ReadLine();
        }
    }
}
