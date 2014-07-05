using System;
using BrightstarDB.Client;

namespace BrightstarDB.PerformanceBenchmarks
{
    public abstract class BenchmarkBase
    {
        public BenchmarkReport Report { get; private set; }
        protected IBrightstarService Service { get; private set; }
        protected string StoreName { get; private set; }
        public int TestScale { get; set; } 

        protected BenchmarkBase()
        {
            Report = new BenchmarkReport();
        }


        /// <summary>
        /// Initialize is called before any other methods. The base implementation 
        /// creates a client connection using the provided connection string and
        /// initializes a new store using the name returned by <see cref="MakeStoreName"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to use</param>
        /// <param name="testScale">The scale parameter passed to the test framework</param>
        public virtual void Initialize(string connectionString, int testScale)
        {
            Service = BrightstarService.GetClient(connectionString);
            StoreName = MakeStoreName();
            Service.CreateStore(StoreName);
            TestScale = testScale;
        }

        /// <summary>
        /// Setup is used to create the target store, internal data structures and 
        /// populate the store with initial data.
        /// </summary>
        public abstract void Setup();

        public virtual string MakeStoreName()
        {
            // Return a name for the store where the benchmark will be run
            return GetType().Name + DateTime.Now.ToString("_yyyyMMdd_HHmmss");
        }

        /// <summary>
        /// Run mix is used to execute different mix patterns. Each specialisation
        /// of BenchmarkBase should call to each mix method.
        /// Each mix method should write to the results file in the form
        /// <example>
        ///     <operation>
        ///         <desc></desc>
        ///         <duration></duration>
        ///         <exception></exception>
        ///     </operation>
        /// </example>
        /// todo: allow a subclass to mark methods with an attribute
        /// BrightstarDB.PerformanceBenchmarks.Mix(string name, string descrption)
        /// </summary>
        public abstract void RunMix();


        /// <summary>
        /// Cleanup is used to tidy up any temporary files and store data
        /// </summary>
        public abstract void CleanUp();
    }
}
