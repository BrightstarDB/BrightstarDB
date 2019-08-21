using System;
using System.Linq;
using System.ServiceModel;
using BrightstarDB.Client;

namespace BrightstarDB.Samples.EntityFramework.GettingStartedCore
{
    /// <summary>
    /// This console application shows how to quickly get started with BrightstarDB
    /// More information about this sample project is in the documentation located at [[INSTALLERDIR]]\Docs
    /// </summary>
    class Program
    {
        private static void Main(string[] args)
        { /*
            // Initialise license and stores directory location
            SamplesConfiguration.Register();

            //create a unique store name
            var storeName = "Films_" + Guid.NewGuid().ToString();

            //connection string to the BrightstarDB service
            string connectionString =
                string.Format(@"Type=embedded;storesDirectory={0};StoreName={1};", SamplesConfiguration.StoresDirectory,
                              storeName);

            // if the store does not exist it will be automatically 
            // created when a context is created
            var ctx = new EntityContext(connectionString);

            // create some films
            var bladeRunner = ctx.Films.Create();
            bladeRunner.Name = "BladeRunner";

            var starWars = ctx.Films.Create();
            starWars.Name = "Star Wars";

            // create some actors and connect them to films
            var ford = ctx.Actors.Create();
            ford.Name = "Harrison Ford";
            ford.DateOfBirth = new DateTime(1942, 7, 13);
            ford.Films.Add(starWars);
            ford.Films.Add(bladeRunner);

            var hamill = ctx.Actors.Create();
            hamill.Name = "Mark Hamill";
            hamill.DateOfBirth = new DateTime(1951, 9, 25);
            hamill.Films.Add(starWars);

            // save the data
            ctx.SaveChanges();

            // open a new context
            ctx = new MyEntityContext(connectionString);

            // find an actor via LINQ
            ford = ctx.Actors.FirstOrDefault(a => a.Name.Equals("Harrison Ford"));

            // get his films
            var films = ford.Films;

            // get star wars
            var sw = films.FirstOrDefault(f => f.Name.Equals("Star Wars"));

            // list actors in star wars
            foreach (var actor in sw.Actors)
            {
                var actorName = actor.Name;
                Console.WriteLine(actorName);
            }

            foreach (var actor in ctx.Actors.Where(a => a.Name.Equals("Mark Hamill")))
            {
                Console.WriteLine(actor.Name + " born " + actor.DateOfBirth);
            }

            Console.WriteLine();

            Console.WriteLine("Making changes to the store with optismistic locking enabled");
            ctx = new EntityContext(connectionString, true);
            var newFilm = ctx.Films.Create();
            ctx.SaveChanges();

            var newFilmId = newFilm.Id;

            //use optimistic locking when creating a new context
            var ctx1 = new EntityContext(connectionString, true);
            var ctx2 = new EntityContext(connectionString, true);

            //create a film in the first context
            var film1 = ctx1.Films.FirstOrDefault(f => f.Id.Equals(newFilmId));
            Console.WriteLine("First context has film with ID '{0}'", film1.Id);

            //create a film in the second context
            var film2 = ctx2.Films.FirstOrDefault(f => f.Id.Equals(newFilmId));
            Console.WriteLine("Second context has film with ID '{0}'", film2.Id);

            //attempt to change the data from both contexts
            film1.Name = "Raiders of the Lost Ark";
            film2.Name = "American Graffiti";

            //save the data to the store
            try
            {
                Console.WriteLine("Attempting update from one context");
                ctx1.SaveChanges();
                Console.WriteLine("Successfully updated the film to '{0}' in the store", film1.Name);

                Console.WriteLine("Attempting a conflicting update from a different context - this should fail.");
                ctx2.SaveChanges();
            }
            catch (TransactionPreconditionsFailedException)
            {
                Console.WriteLine(
                    "Optimistic locking enabled: the conflicting update has not been processed as the underlying data has been modified.");
            }

            // Shutdown Brightstar processing threads.
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press the Return key to exit.");
            Console.ReadLine();
        */
        }

    }
}
