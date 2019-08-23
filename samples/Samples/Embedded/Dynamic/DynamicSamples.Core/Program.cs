using System;
using BrightstarDB.Dynamic;
using BrightstarDB.Client;
using System.ServiceModel;


namespace BrightstarDB.Samples.DynamicSamples.Core
{
    public class Program
    {
        private static void Main(string[] args)
        {
            SamplesConfiguration.Register();
            Console.WriteLine("BrightstarDB Dynamic Objects Example");
            Console.WriteLine("Creating and populating store using dynamic objects");
            // gets a new BrightstarDB DataObjectContext
            var dataObjectContext =
                BrightstarService.GetDataObjectContext("type=embedded;storesDirectory=" +
                                                       SamplesConfiguration.StoresDirectory);

            // create a dynamic context
            var dynaContext = new BrightstarDynamicContext(dataObjectContext);

            // open a new store
            var storeId = "DynamicSample" + Guid.NewGuid().ToString();
            var dynaStore = dynaContext.CreateStore(storeId);

            // create some dynamic objects. 
            dynamic brightstar = dynaStore.MakeNewObject();
            dynamic product = dynaStore.MakeNewObject();

            // set some properties
            brightstar.name = "BrightstarDB";
            product.rdfs__label = "Product";
            var id = brightstar.Identity;

            // use namespace mapping (RDF and RDFS are defined by default)
            // Assigning a list creates repeated RDF properties.
            brightstar.rdfs__label = new[] {"BrightstarDB", "NoSQL Database"};

            // objects are connected together in the same way
            brightstar.rdfs__type = product;

            dynaStore.SaveChanges();

            Console.WriteLine("Reading dynamic object from BrightstarDB");
            // open store and read some data
            dynaStore = dynaContext.OpenStore(storeId);
            brightstar = dynaStore.GetDataObject(brightstar.Identity);

            Console.WriteLine("Got item with identity: {0}", brightstar.Identity);
            // property values are ALWAYS collections.
            var name = brightstar.name.FirstOrDefault();
            Console.WriteLine("\tName = {0}", name);

            // property can also be accessed by index
            var nameByIndex = brightstar.name[0];
            Console.WriteLine("\tName (using indexed property) = {0}", nameByIndex);

            // they can be enumerated without a cast
            Console.WriteLine("Enumerating rdfs:label values");
            foreach (var l in brightstar.rdfs__label)
            {
                Console.WriteLine("\tLabel = {0}", l);
            }

            // object relationships are navigated in the same way
            Console.WriteLine("Retrieving rdfs:type relationship");
            var p = brightstar.rdfs__type.FirstOrDefault();
            Console.WriteLine("\tType object ID = {0}", p.Identity);
            Console.WriteLine("\tType object label = {0}", p.rdfs__label.FirstOrDefault());

            // dynamic objects can also be loaded via sparql
            dynaStore = dynaContext.OpenStore(storeId);
            Console.WriteLine("Binding SPARQL query to dynamic objects");
            var objects = dynaStore.BindObjectsWithSparql("select distinct ?dy where { ?dy ?p ?o }");
            foreach (var obj in objects)
            {
                Console.WriteLine("\tItem Label: {0}", obj.rdfs__label[0]);
            }

            // Shutdown Brightstar processing threads.
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Example complete. Press return to exit.");
            Console.ReadLine();
        }
    }
}
