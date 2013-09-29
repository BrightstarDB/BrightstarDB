using System;
using System.Collections.Generic;
using System.Globalization;

namespace BrightstarDB.Server.Modules.Model
{
    /// <summary>
    /// C# representation of the JSON object that must be posted to create a new job on the BrightstarDB server
    /// </summary>
    public class JobRequestObject
    {
        /// <summary>
        /// Get or set the string identifier for the type of job to be created
        /// </summary>
        public string JobType { get; set; }

        /// <summary>
        /// Get or set the parameters to pass into the job
        /// </summary>
        public Dictionary<string, string> JobParameters { get; set; } 

        /// <summary>
        /// Create an empty request object. For deserialization purposes only.
        /// </summary>
        [Obsolete("Provided for serialization purposes only.")]
        public JobRequestObject(){}

        /// <summary>
        /// Create a new request object
        /// </summary>
        /// <param name="jobType">The string identifier for the type of job to be created</param>
        /// <param name="jobParameters">The parameters to pass into the job</param>
        private JobRequestObject(string jobType, Dictionary<string, string> jobParameters)
        {
            JobType = jobType;
            JobParameters = jobParameters;
        }

        /// <summary>
        /// Creates a transaction update job request object
        /// </summary>
        /// <param name="preconditions">Transaction precondition triples encoded as N-Triples or N-Quads</param>
        /// <param name="deletes">Triples to be deleted encoded as N-Triples or N-Quads</param>
        /// <param name="inserts">Triples to be inserted encoded as N-Triples or N-Quads</param>
        /// <param name="defaultGraphUri">OPTIONAL: The default graph URI to apply to N-Triples. If not provided, the system default graph will be targetted.</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateTransactionJob(string preconditions, string deletes, string inserts, string defaultGraphUri = null)
        {
            return new JobRequestObject("Transaction", new Dictionary<string, string>
                {
                    {"Preconditions", preconditions},
                    {"Deletes", deletes},
                    {"Inserts", inserts},
                    {"DefaultGraphUri", defaultGraphUri}
                });
        }

        /// <summary>
        /// Creates a store consolidation job request object
        /// </summary>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateConsolidateJob()
        {
            return new JobRequestObject("Consolidate", new Dictionary<string, string>());
        }

        /// <summary>
        /// Creates a store export job request object
        /// </summary>
        /// <param name="exportFileName">The name of the file to export the data to</param>
        /// <param name="graphUri">OPTIONAL: The URI identifier of the graph to be exported. If not provided, all graphs in the store are exported</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateExportJob(string exportFileName, string graphUri = null)
        {
            if (exportFileName == null) throw new ArgumentNullException("exportFileName");
            if (String.IsNullOrWhiteSpace(exportFileName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "exportFileName");
            if (graphUri != null && String.IsNullOrWhiteSpace(graphUri)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "graphUri");

            return new JobRequestObject("Export",
                                        new Dictionary<string, string>
                                            {
                                                {"FileName", exportFileName},
                                                {"GraphUri", graphUri}
                                            });
        }

        /// <summary>
        /// Creates a store import job request object
        /// </summary>
        /// <param name="importFileName">The name of the file to import the data from</param>
        /// <param name="defaultGraphUri">OPTIONAL: The default graph to apply to triples parsed from the import file. If not provided, defaults to the system default graph./</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateImportJob(string importFileName, string defaultGraphUri = null)
        {
            if (importFileName == null) throw new ArgumentNullException("importFileName");
            if (String.IsNullOrWhiteSpace(importFileName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "importFileName");
            if (defaultGraphUri != null && String.Empty.Equals(defaultGraphUri.Trim())) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "defaultGraphUri");

            return new JobRequestObject("Import", new Dictionary<string, string>
                {
                    {"FileName", importFileName},
                    {"DefaultGraphUri", defaultGraphUri}
                });
        }

        /// <summary>
        /// Creates a SPARQL UPDATE job request object
        /// </summary>
        /// <param name="updateExpression">The SPARQL UPDATE expression to apply to the store</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateSparqlUpdateJob(string updateExpression)
        {
            if (updateExpression == null) throw new ArgumentNullException("updateExpression");
            if (String.IsNullOrWhiteSpace(updateExpression)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "updateExpression");

            return new JobRequestObject("SparqlUpdate",
                                        new Dictionary<string, string> {{"UpdateExpression", updateExpression}});
        }

        /// <summary>
        /// Creates a Statistics update job request object
        /// </summary>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateUpdateStatsJob()
        {
            return new JobRequestObject("UpdateStats", new Dictionary<string, string>());
        }

        /// <summary>
        /// Creates a job that repeats a previously executed job
        /// </summary>
        /// <param name="jobId">The GUID identifier of the job to be repeated</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateRepeatTransactionJob(Guid jobId)
        {
            return new JobRequestObject("RepeatTransaction",
                                        new Dictionary<string, string> {{"JobId", jobId.ToString()}});
        }

        public static JobRequestObject CreateSnapshotJob(string targetStoreName, string persistenceType)
        {
            return new JobRequestObject("CreateSnapshot", new Dictionary<string, string>
                {
                    {"TargetStoreName", targetStoreName},
                    {"PersistenceType", persistenceType},
                });
        }

        public static JobRequestObject CreateSnapshotJob(string targetStoreName, string persistenceType, ulong commitPointId)
        {
            return new JobRequestObject("CreateSnapshot", new Dictionary<string, string>
                {
                    {"TargetStoreName", targetStoreName},
                    {"PersistenceType", persistenceType},
                    {"CommitId", commitPointId.ToString(CultureInfo.InvariantCulture)}
                });
        }
    }
}
