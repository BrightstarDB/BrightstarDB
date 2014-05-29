using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Client;

namespace BrightstarDB.PerformanceBenchmarks
{
    public abstract class BenchmarkBase
    {
        private StreamWriter ReportStream { get; set; } 
        protected IBrightstarService Service { get; private set; }
        protected string StoreName { get; private set; } 

        protected BenchmarkBase(string filename, string connectionString)
        {
            ReportStream = new StreamWriter(new FileStream(filename, FileMode.Create));
            Service = BrightstarService.GetClient(connectionString);
            
            StoreName = Guid.NewGuid().ToString();
            Service.CreateStore(StoreName);

            // write xml header
            ReportStream.WriteLine("<benchmarkreport>");
        }

        private const string OperationTemplate = @"<operation><name>{0}</name><desc>{1}</desc><duration>{2}</duration><exception>{3}<exception>";

        protected void LogOperation(string name, string description, string duration, string exception)
        {
            ReportStream.WriteLine(OperationTemplate, name, description, duration, exception);
        }

        /// <summary>
        /// Setup is used to create internal data structures and 
        /// populate the store with initial data.
        /// The setup operation should write to the results file using the
        /// same structure as for the mixin operations.
        /// </summary>
        public abstract void Setup();

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
        public virtual void CleanUp()
        {
            ReportStream.WriteLine("</benchmarkreport>");
            ReportStream.Close();
        }
    }
}
