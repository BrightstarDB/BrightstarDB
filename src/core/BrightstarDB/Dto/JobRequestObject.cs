﻿using System;
using System.Collections.Generic;
using System.Globalization;
using BrightstarDB.Storage;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// C# representation of the JSON object that must be posted to create a new job on the BrightstarDB server
    /// </summary>
    public class JobRequestObject
    {
        /// <summary>
        /// Get or set the name of the store that the job is to be executed against.
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// Get or set the string identifier for the type of job to be created
        /// </summary>
        public string JobType { get; set; }

        /// <summary>
        /// Get or set the string label for this job.
        /// </summary>
        public string Label { get; set; }

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
        /// <param name="jobLabel">An optional user-friendly label for the job</param>
        private JobRequestObject(string jobType, Dictionary<string, string> jobParameters, string jobLabel = null)
        {
            JobType = jobType;
            JobParameters = jobParameters;
            Label = jobLabel;
        }

        /// <summary>
        /// Creates a transaction update job request object
        /// </summary>
        /// <param name="preconditions">Transaction precondition triples encoded as N-Triples or N-Quads</param>
        /// <param name="deletes">Triples to be deleted encoded as N-Triples or N-Quads</param>
        /// <param name="inserts">Triples to be inserted encoded as N-Triples or N-Quads</param>
        /// <param name="defaultGraphUri">OPTIONAL: The default graph URI to apply to N-Triples. If not provided, the system default graph will be targetted.</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateTransactionJob(string preconditions, string deletes, string inserts,
                                                            string defaultGraphUri = null, string label = null)
        {
            return new JobRequestObject("Transaction",
                                        new Dictionary<string, string>
                                            {
                                                {"Preconditions", preconditions},
                                                {"Deletes", deletes},
                                                {"Inserts", inserts},
                                                {"DefaultGraphUri", defaultGraphUri}
                                            },
                                        label);
        }

        /// <summary>
        /// Creates a store consolidation job request object
        /// </summary>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateConsolidateJob(string label = null)
        {
            return new JobRequestObject("Consolidate", new Dictionary<string, string>(), label);
        }

        /// <summary>
        /// Creates a store export job request object
        /// </summary>
        /// <param name="exportFileName">The name of the file to export the data to</param>
        /// <param name="graphUri">OPTIONAL: The URI identifier of the graph to be exported. If not provided, all graphs in the store are exported</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateExportJob(string exportFileName, string graphUri = null, string label = null)
        {
            if (exportFileName == null) throw new ArgumentNullException("exportFileName");
            if (String.IsNullOrWhiteSpace(exportFileName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "exportFileName");
            if (graphUri != null && String.IsNullOrWhiteSpace(graphUri)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "graphUri");

            return new JobRequestObject("Export",
                                        new Dictionary<string, string>
                                            {
                                                {"FileName", exportFileName},
                                                {"GraphUri", graphUri}
                                            },
                                        label);
        }

        /// <summary>
        /// Creates a store import job request object
        /// </summary>
        /// <param name="importFileName">The name of the file to import the data from</param>
        /// <param name="defaultGraphUri">OPTIONAL: The default graph to apply to triples parsed from the import file. If not provided, defaults to the system default graph./</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateImportJob(string importFileName, string defaultGraphUri = null,
                                                       string label = null)
        {
            if (importFileName == null) throw new ArgumentNullException("importFileName");
            if (String.IsNullOrWhiteSpace(importFileName))
                throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "importFileName");
            if (defaultGraphUri != null && String.Empty.Equals(defaultGraphUri.Trim()))
                throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "defaultGraphUri");

            return new JobRequestObject("Import",
                                        new Dictionary<string, string>
                                            {
                                                {"FileName", importFileName},
                                                {"DefaultGraphUri", defaultGraphUri}
                                            },
                                        label);
        }

        /// <summary>
        /// Creates a SPARQL UPDATE job request object
        /// </summary>
        /// <param name="updateExpression">The SPARQL UPDATE expression to apply to the store</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateSparqlUpdateJob(string updateExpression, string label = null)
        {
            if (updateExpression == null) throw new ArgumentNullException("updateExpression");
            if (String.IsNullOrWhiteSpace(updateExpression)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "updateExpression");

            return new JobRequestObject("SparqlUpdate",
                                        new Dictionary<string, string> {{"UpdateExpression", updateExpression}},
                                        label);
        }

        /// <summary>
        /// Creates a Statistics update job request object
        /// </summary>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateUpdateStatsJob(string label = null)
        {
            return new JobRequestObject("UpdateStats", new Dictionary<string, string>(), label);
        }

        /// <summary>
        /// Creates a job that repeats a previously executed job
        /// </summary>
        /// <param name="jobId">The GUID identifier of the job to be repeated</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/> instance</returns>
        public static JobRequestObject CreateRepeatTransactionJob(Guid jobId, string label = null)
        {
            return new JobRequestObject("RepeatTransaction",
                                        new Dictionary<string, string> {{"JobId", jobId.ToString()}},
                                        label);
        }

        /// <summary>
        /// Create a request for a new Snapshot job
        /// </summary>
        /// <param name="targetStoreName"></param>
        /// <param name="persistenceType"></param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns></returns>
        [Obsolete(
            "Use the type-safe version of this method that takes a BrightstarDB.Storage.PersistenceType parameter instead"
            )]
        public static JobRequestObject CreateSnapshotJob(string targetStoreName, string persistenceType,
                                                         string label = null)
        {
            return new JobRequestObject("CreateSnapshot",
                                        new Dictionary<string, string>
                                            {
                                                {"TargetStoreName", targetStoreName},
                                                {"PersistenceType", persistenceType},
                                            },
                                        label);
        }

        /// <summary>
        /// Create a request for a new Snapshot job
        /// </summary>
        /// <param name="targetStoreName">The name of the store that will be created to hold the snapshot</param>
        /// <param name="persistenceType">The type of persistence to use for the snapshot store</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/></returns>
        public static JobRequestObject CreateSnapshotJob(string targetStoreName, PersistenceType persistenceType,
                                                         string label = null)
        {
            return new JobRequestObject("CreateSnapshot",
                                        new Dictionary<string, string>
                                            {
                                                {"TargetStoreName", targetStoreName},
                                                {"PersistenceType", persistenceType.ToString()},
                                            },
                                        label);
        }

        /// <summary>
        /// Create a request for a new Snapshot job
        /// </summary>
        /// <param name="targetStoreName">The name of the store that will be created to hold the snapshot</param>
        /// <param name="persistenceType">The type of persistence to use for the snapshot store</param>
        /// <param name="commitPointId">The commit point of the store to copy into the snapshot</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/></returns>
        [Obsolete(
            "Use the type-safe version of this method that takes a BrightstarDB.Storage.PersistenceType parameter instead"
            )]
        public static JobRequestObject CreateSnapshotJob(string targetStoreName, string persistenceType,
                                                         ulong commitPointId, string label = null)
        {
            return new JobRequestObject("CreateSnapshot",
                                        new Dictionary<string, string>
                                            {
                                                {"TargetStoreName", targetStoreName},
                                                {"PersistenceType", persistenceType},
                                                {"CommitId", commitPointId.ToString(CultureInfo.InvariantCulture)}
                                            }, label);
        }

        /// <summary>
        /// Create a request for a new Snapshot job
        /// </summary>
        /// <param name="targetStoreName">The name of the store that will be create to hold the snapshot</param>
        /// <param name="persistenceType">The type of persitence to use for the store created by the snapshot</param>
        /// <param name="commitPointId">The ID of the commit point to copy data from</param>
        /// <param name="label">A user-friendly label for the job</param>
        /// <returns>A new <see cref="JobRequestObject"/>.</returns>
        public static JobRequestObject CreateSnapshotJob(string targetStoreName, PersistenceType persistenceType,
                                                         ulong commitPointId, string label = null)
        {
            return new JobRequestObject("CreateSnapshot",
                                        new Dictionary<string, string>
                                            {
                                                {"TargetStoreName", targetStoreName},
                                                {"PersistenceType", persistenceType.ToString()},
                                                {"CommitId", commitPointId.ToString(CultureInfo.InvariantCulture)}
                                            },
                                        label);
        }
        /// <summary>
        /// A fluent API wrapper for setting the user-friendly label for a job
        /// </summary>
        /// <param name="jobLabel"></param>
        /// <returns>The modified <see cref="JobRequestObject"/></returns>
        public JobRequestObject WithLabel(string jobLabel)
        {
            Label = jobLabel;
            return this;
        }
    }
}
