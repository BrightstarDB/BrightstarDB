using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.Samples.DataObjectLayerSampleCore
{
    class Program
    {
        private static Dictionary<string, string> _namespaceMappings;
 
        static void Main(string[] args)
        {
            SamplesConfiguration.Register();

            // Create a new service context using a connection string. 
            var context = BrightstarService.GetDataObjectContext(@"Type=embedded;storesDirectory="+SamplesConfiguration.StoresDirectory);
            
            // create a new store
            string storeName = "DataObjectLayerSample_" + Guid.NewGuid();
            var store = context.CreateStore(storeName);

            //In order to use simpler identities, we set up some namespace mappings to pass through when we open a store
            _namespaceMappings = new Dictionary<string, string>()
                                        {
                                            {"people", "http://example.org/people/"},
                                            {"skills", "http://example.org/skills/"},
                                            {"schema", "http://example.org/schema/"}
                                        };

            //Open a Data Object Store passing through the namespace mappings
            store = context.OpenStore(storeName, _namespaceMappings);

            var skillType = store.MakeDataObject("schema:skill");
            //use namespace mappings to create a number of skills
            var csharp = store.MakeDataObject("skills:csharp");
            csharp.SetType(skillType);
            var html = store.MakeDataObject("skills:html");
            html.SetType(skillType);
            var css = store.MakeDataObject("skills:css");
            css.SetType(skillType);
            var javascript = store.MakeDataObject("skills:javascript");
            javascript.SetType(skillType);

            //create a data objects for people
            var personType = store.MakeDataObject("schema:person");
            var fred = store.MakeDataObject("people:fred");
            fred.SetType(personType);
            var william = store.MakeDataObject("people:william");
            william.SetType(personType);

            //create objects for property types for name and category
            var fullname = store.MakeDataObject("schema:person/fullName");
            var skill = store.MakeDataObject("schema:person/skill");

            //Set the name property
            fred.SetProperty(fullname, "Fred Evans");
            //Add the skills
            fred.AddProperty(skill, csharp);
            fred.AddProperty(skill, html);
            fred.AddProperty(skill, css);

            //Set the name property
            william.SetProperty(fullname, "William Turner");
            //Add the skills
            william.AddProperty(skill, html);
            william.AddProperty(skill, css);
            william.AddProperty(skill, javascript);

            //save the changes to the store
            store.SaveChanges();

            Console.WriteLine("Added 2 data objects to store.");
            Console.WriteLine("Identity: {0}", fred.Identity);
            Console.WriteLine("Name: {0}", fred.GetPropertyValue(fullname));
            Console.WriteLine("Identity: {0}", william.Identity);
            Console.WriteLine("Name: {0}", william.GetPropertyValue(fullname));
            Console.WriteLine();

            var employeeNumber = store.MakeDataObject("schema:person/employeeNumber");
            var dateOfBirth = store.MakeDataObject("schema:person/dateOfBirth");
            var salary = store.MakeDataObject("schema:person/salary");

            //adding literal properties to a data object
            fred = store.GetDataObject("people:fred");
            //the datatypes are auto detected
            Console.WriteLine("Adding literal data to the person data object");
            fred.SetProperty(employeeNumber, 123);
            fred.SetProperty(dateOfBirth, DateTime.Now.AddYears(-30));
            fred.SetProperty(salary, 18000.00);

            store.SaveChanges();

            store = context.OpenStore(storeName, _namespaceMappings);

            fred = store.GetDataObject("people:fred");
            Console.WriteLine("Name: {0}", fred.GetPropertyValue(fullname));
            Console.WriteLine("Employee Number: {0}", fred.GetPropertyValue(employeeNumber));
            Console.WriteLine("Date of Birth: {0}", fred.GetPropertyValue(dateOfBirth));
            Console.WriteLine("Salary: {0:C}", fred.GetPropertyValue(salary));
            Console.WriteLine();

            // SPARQL query for all the categories connected to BrightstarDB
            const string getPersonSkillsQuery = "SELECT ?skill WHERE { <http://example.org/people/fred> <http://example.org/schema/person/skill> ?skill }";

            Console.WriteLine("Executing SPARQL query to return Fred's skills:");
            Console.WriteLine(getPersonSkillsQuery);
            var sparqlResult = store.ExecuteSparql(getPersonSkillsQuery);

            var result = sparqlResult.ResultSet;
            foreach (var sparqlResultRow in result)
            {
                var val = sparqlResultRow["skill"];
                Console.WriteLine("Skill is " + val);
            }

            Console.WriteLine();

            //connect to the store again to make sure cache is flushed
            store = context.OpenStore(storeName, _namespaceMappings);

            // SPARQL query to return URIs of all objects of the 'category' type
            const string skillsQuery = "SELECT ?skill WHERE {?skill a <http://example.org/schema/skill>}";
            //Use the BindDataObjectsWithSparql Method to pull a collection of data objects from the store that match the SPARQL query supplied
            var allSkills = store.BindDataObjectsWithSparql(skillsQuery).ToList();
            Console.WriteLine("Binding data objects with query for all skills in the store:");
            Console.WriteLine(skillsQuery);
            foreach (var s in allSkills)
            {
                Console.WriteLine("Skill is " + s.Identity);
            }

            //Delete all categories found
            Console.WriteLine();
            Console.WriteLine("Deleting all skill data objects");
            foreach (var s in allSkills)
            {
                s.Delete();
            }
            store.SaveChanges();
            
            //connect to the store again to make sure cache is flushed
            store = context.OpenStore(storeName, _namespaceMappings);
            allSkills = store.BindDataObjectsWithSparql(skillsQuery).ToList();
            var skillCount = allSkills.Count;
            Console.WriteLine("Skill count after delete is " + skillCount);

            // Shutdown Brightstar processing threads.
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press Return key to exit.");
            Console.ReadLine();

        }
    }
}
