using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkedPlanet.BrightStar.Client;

namespace NetworkedPlanet.BrightStar.Samples
{
    public class SemanticWebServerExample
    {
        /// <summary>
        /// Simple example that uses the basic client to connect to a BrightStar server, list the stores present and then create a new store.
        /// Assumes the BrightStar server is running on its default port.
        /// </summary>
        public static void UsingBasicClientToListStoresAndCreateStore()
        {
            // a new brightstar basic client
            var bc = new BasicClient();

            // list stores returns the URLs of each store.
            // todo: why isnt the hyperserver url in the BasicClient constructor?
            var stores = bc.ListStores(new Uri("http://localhost:8090/brightstar")); 

            // iterate the store URLs 
            foreach (var store in stores)
            {
                Console.WriteLine("store is " + store);
            }

            // create a new store with guarenteed unique name
            var newStoreUri = bc.CreateStore(new Uri("http://localhost:8090/brightstar"), "mystore" + Guid.NewGuid());

            // get the data from the new store using the store uri. Note this will be empty.
            var storeData = bc.GetStoreData(newStoreUri);
        }

        /// <summary>
        /// Simple example of how to create a new store, insert some simple data in the Ntriples (link) format, and then query
        /// using SPARQL.
        /// </summary>
        public static void UsingBasicClientToInsertAndQueryData()
        {
            var bc = new BasicClient();

            // create a store
            var storeUri = bc.CreateStore(new Uri("http://localhost:8090/brightstar"), "mystore" + Guid.NewGuid());

            // all data is in NTriples format (see link for format).  
            var dataToInsert = @"
                # data in Ntriple format.
                <http://www.networkedplanet.com/people/bob> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/networkedplanet> .
                <http://www.networkedplanet.com/people/jill> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/networkedplanet> .
                <http://www.networkedplanet.com/people/john> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/networkedplanet> .
                ";

            // create a simple transaction that adds triples to the store
            var txn = new Transaction {TriplesToAdd = dataToInsert};

            // post the transaction and wait for it to complete. The alternative is to fire and forget, or pass in a call back. 
            // todo: these other options arent available yet.
            var jobUri = bc.PostTransaction(storeUri, txn);

            // query for data
            const string query = "select ?person where { ?people <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/networkedplanet> . }";

            // execute sparql query and get SPARQL result XML.
            var result = bc.Query(storeUri, query);

            // Here we use a couple of XElement SPARQL result set specific extension methods, Rows and GetColumnValue
            //foreach (var resultRow in result.Rows)
            //{
            //    Console.WriteLine(resultRow.GetColumnValue("person"));
            //}
        }
    }
}
