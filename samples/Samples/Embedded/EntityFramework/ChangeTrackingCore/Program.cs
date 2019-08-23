using System;
using System.Linq;
using BrightstarDB.Client;
using ChangeTrackingCore;

namespace BrightstarDB.Samples.EntityFramework.ChangeTrackingCore
{
    /// <summary>
    /// This console application shows how the BrightStar entity framework can be used to track changes made to entities
    /// More information about this sample project is in the documentation located at [[INSTALLERDIR]]\Docs
    /// </summary>
    class Program
    {
        private static void Main(string[] args)
        {
            // Initialise license and stores directory location
            SamplesConfiguration.Register();

            //create a unique store name
            var storeName = "changetracking_" + Guid.NewGuid();

            //connection string to the BrightstarDB service
            var connectionString = String.Format(@"Type=embedded;storesDirectory={0};storeName={1}", SamplesConfiguration.StoresDirectory, storeName);

            // Create and modify an article to show that the tracking timestamps get updated
            var article = CreateArticle(connectionString);
            Console.WriteLine("Created new article: {0}", article);

            System.Threading.Thread.Sleep(1000);
            article = ModifyArticle(connectionString, article.Id);
            Console.WriteLine("Modified article: {0}", article);

            // Shutdown Brightstar processing threads.
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press the Return key to exit.");
            Console.ReadLine();
        }

        private static IArticle CreateArticle(string connectionString)
        {
            // Create new context
            var context = new EntityContext(connectionString);
            // Set the event handler to be invoked when SaveChanges is called
            context.SavingChanges += UpdateTrackables;
            var article = new Article {Title = "Sample Article", BodyText = "Sample article content"};
            context.Articles.Add(article);
            context.SaveChanges();
            return article;
        }

        private static IArticle ModifyArticle(string connectionString, string articleId)
        {
            var context = new EntityContext(connectionString);
            context.SavingChanges += UpdateTrackables;
            var article = context.Articles.FirstOrDefault(x => x.Id.Equals(articleId));
            article.BodyText = "Updated article content";
            context.SaveChanges();
            return article;
        }


        private static void UpdateTrackables(object sender, EventArgs e)
        {
            // This method is invoked by the context.
            // The sender object is the context itself
            var context = sender as EntityContext;

            // Iterate through just the tracked objects that implement the ITrackable interface
            foreach(var t in context.TrackedObjects.Where(x=>x is ITrackable && x.IsModified).Cast<ITrackable>())
            {
                // If the Created property is not yet set, it will have DateTime.MinValue as its defaulft value
                // We can use this fact to determine if the Created property needs setting.
                if (t.Created == DateTime.MinValue) t.Created = DateTime.Now;

                // The LastModified property should always be updated
                t.LastModified = DateTime.Now;
            }
        }

    }
}
