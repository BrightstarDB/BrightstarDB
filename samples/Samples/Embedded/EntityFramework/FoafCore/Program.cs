using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using FoafCore;

namespace BrightstarDB.Samples.EntityFramework.FoafCore
{
    /// <summary>
    /// This console application shows how the BrightStar entity framework can be mapped onto existing RDF data
    /// More information about this sample project is in the documentation located at [[INSTALLERDIR]]\Docs
    /// </summary>
    class Program
    {
        private static void Main()
        {
            // Initialise license and stores directory location
            SamplesConfiguration.Register();

            //create a unique store name
            var storeName = "foaf_" + Guid.NewGuid();

            //connection string to the BrightstarDB service
            var connectionString = String.Format(@"Type=embedded;storesDirectory={0};",
                                                 SamplesConfiguration.StoresDirectory);

            //Load some RDF data into the store
            LoadRdfData(connectionString, storeName);

            //Connect to the store via the entity framework and loop through the entities
            PrintOutUsingEntityFramework(connectionString, storeName);

            // Shutdown Brightstar processing threads.
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press the Return key to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Load some RDF data into the store
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="storeName"></param>
        static void LoadRdfData(string connectionString, string storeName)
        {
            Console.WriteLine("Creating store...");
            //create a new service client using the connection string
            var client = BrightstarService.GetClient(connectionString);
            //create a new store
            client.CreateStore(storeName);

            Console.WriteLine("Adding RDF triples to store...");
            var triples = new StringBuilder();
            var r = new Random();
            //Add RDF triples for 25 people
            for(var i = 0; i < 25; i++)
            {
                var name = Firstnames[i];
                var fullname = Firstnames[i] + " " + Surnames[i];
                var employer = Employers[r.Next(4)];
                triples.AppendLine(string.Format(@"<http://www.brightstardb.com/people/{0}> <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://xmlns.com/foaf/0.1/Person> .", name));
                triples.AppendLine(string.Format(@"<http://www.brightstardb.com/people/{0}> <http://xmlns.com/foaf/0.1/nick> ""{0}"" .", name));
                triples.AppendLine(string.Format(@"<http://www.brightstardb.com/people/{0}> <http://xmlns.com/foaf/0.1/name> ""{1}"" .", name, fullname));
                triples.AppendLine(string.Format(@"<http://www.brightstardb.com/people/{0}> <http://xmlns.com/foaf/0.1/Organization> ""{1}"" .", name, employer));
            }

            //Link each person to 5 random others via the "knows" predicate
            for(var i = 0; i < 25; i++)
            {
                var name = Firstnames[i];
                for(var x =0; x<5; x++)
                {
                    var otherPerson = Firstnames[r.Next(24)];
                    var knows =
                        string.Format(
                            @"<http://www.brightstardb.com/people/{0}> <http://xmlns.com/foaf/0.1/knows> <http://www.brightstardb.com/people/{1}> .",
                            name, otherPerson);
                    triples.AppendLine(knows);
                }
            }

            client.ExecuteTransaction(storeName, new UpdateTransactionData{InsertData = triples.ToString()});
        }

        /// <summary>
        /// Access the RDF data using the entity framework
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="storeName"></param>
        static void PrintOutUsingEntityFramework(string connectionString, string storeName)
        {
            Console.WriteLine("Print out store data using Entity Framework...");
            //connect to the same store using the BrightstarDB entity framework context
            var context = new EntityContext(connectionString + "StoreName=" + storeName);
            
            //loop through all the Person entities and print out their properties and other people that they 'know'
            Console.WriteLine(@"{0} people found in raw RDF data", context.Persons.Count());
            Console.WriteLine();
            foreach (var person in context.Persons.ToList())
            {
                Console.WriteLine("PERSON ID: {0}", person.Id);
                var knows = new List<IPerson>();
                knows.AddRange(person.Knows);
                knows.AddRange(person.KnownBy);

                Console.WriteLine(@"Full Name: {0}. Nickname: {1}. Employer: {2}", person.Name, person.Nickname, person.Organisation);
                Console.WriteLine(knows.Count == 1
                                      ? string.Format(@"{0} knows 1 other person", person.Nickname)
                                      : string.Format(@"{0} knows {1} other people", person.Nickname, knows.Count));
                foreach(var other in knows)
                {
                    Console.WriteLine(@"    {0} at {1}", other.Name, other.Organisation);
                }
                Console.WriteLine();
            }
        }

        #region Names
        private static readonly List<string> Firstnames = new List<string>
                                 {
                                     "Jen",
                                     "Kal",
                                     "Gra",
                                     "Andy",
                                     "Jessica",
                                     "Adam",
                                     "Trevor",
                                     "Morris",
                                     "Paul",
                                     "Jane",
                                     "Elliot",
                                     "Annie",
                                     "Rob",
                                     "Mark",
                                     "Tim",
                                     "Gemma",
                                     "Clare",
                                     "Anna",
                                     "Tessa",
                                     "Julia",
                                     "David",
                                     "Andrew",
                                     "Charlie",
                                     "Aled",
                                     "Alex"
                                 };
        private static readonly List<string> Surnames = new List<string>
                                 {
                                     "Wilson",
                                     "Foster",
                                     "Green",
                                     "Fahy",
                                     "Goldsack",
                                     "Webb",
                                     "Fernley",
                                     "McKee",
                                     "Hughes",
                                     "Wong",
                                     "Sully",
                                     "Hague",
                                     "Boyce",
                                     "Pegeot",
                                     "Chappell",
                                     "East",
                                     "Tate",
                                     "Wade",
                                     "Lloyd",
                                     "Hopwseith",
                                     "Matthews",
                                     "Lacey",
                                     "Skipper",
                                     "Chandler",
                                     "Jones"
                                 };

        private static readonly List<string> Employers = new List<string>{"Networked Planet", "Microsoft", "BBC", "Oxford University", "Wicked Skatewear"};
        #endregion
    }
}
