using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using BrightstarDB.Azure.Common.Logging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.Common
{
    public class SqlJobQueue : IJobQueue
    {
        private readonly string _connectionString;
        private readonly string _workerId;

        public SqlJobQueue(string connectionString, string workerId)
        {
            _connectionString = connectionString;
            _workerId = workerId;
        }

        #region Implementation of IJobQueue

        public string QueueJob(string storeId, JobType jobType, string jobData, DateTime? scheduledRunTime)
        {
            string jobId = Guid.NewGuid().ToString("N");
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_QueueJob";
                        cmd.Parameters.AddWithValue("id", jobId);
                        cmd.Parameters.AddWithValue("storeId", storeId);
                        cmd.Parameters.AddWithValue("jobType", (short) jobType);
                        cmd.Parameters.AddWithValue("jobData", jobData);
                        cmd.Parameters.AddWithValue("scheduledRunTime",
                                                    scheduledRunTime.HasValue
                                                        ? (object) scheduledRunTime.Value
                                                        : DBNull.Value);
                        var rowCount = cmd.ExecuteNonQuery();
                        if (rowCount == 1) return jobId;
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
            return null;
        }


        public JobInfo NextJob(string preferredStoreId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_NextJob";
                        cmd.Parameters.AddWithValue("storeId",
                                                    String.IsNullOrEmpty(preferredStoreId)
                                                        ? (object) DBNull.Value
                                                        : preferredStoreId);
                        cmd.Parameters.AddWithValue("workerId", _workerId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            JobInfo ret = null;
                            if (reader.Read())
                            {
                                var jobId = reader.GetString(0);
                                var storeId = reader.GetString(1);
                                ret = new JobInfo(jobId, storeId)
                                          {
                                              JobType = (JobType) reader.GetInt32(2),
                                              Status = (JobStatus) reader.GetInt32(3),
                                              Data = reader.IsDBNull(4) ? null : reader.GetString(4),
                                              RetryCount = reader.GetInt32(5)
                                          };
                            }
                            return ret;
                        }
                    }
                } finally
                {
                    conn.Close();
                }
            }
        }

        public void UpdateStatus(string jobId, string statusMessage)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_UpdateStatus";
                        cmd.Parameters.AddWithValue("jobId", jobId);
                        cmd.Parameters.AddWithValue("statusMessage", StatusMessageValue(statusMessage));
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public void StartCommit(string jobId, string statusMessage)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_StartCommit";
                        cmd.Parameters.AddWithValue("jobId", jobId);
                        cmd.Parameters.AddWithValue("statusMessage", StatusMessageValue(statusMessage));
                        cmd.ExecuteNonQuery();
                    }
                } finally
                {
                    conn.Close();
                }
            }
        }

        public void CompleteJob(string jobId, JobStatus finalStatus, string finalStatusMessage)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_CompleteJob";
                        cmd.Parameters.AddWithValue("jobId", jobId);
                        cmd.Parameters.AddWithValue("finalStatus", (int) finalStatus);
                        cmd.Parameters.AddWithValue("finalStatusMessage", StatusMessageValue(finalStatusMessage));
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
            LogJob(jobId);
        }

        public void FailWithException(string jobId, string failureMessage, Exception ex)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_JobException";
                        cmd.Parameters.AddWithValue("jobId", jobId);
                        cmd.Parameters.AddWithValue("statusMessage", StatusMessageValue(failureMessage));
                        cmd.Parameters.AddWithValue("processingException", ex.ToString());
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    conn.Close();
                }
            }

            LogJob(jobId);
        }

        public void ReleaseJob(string jobId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_ReleaseJob";
                        cmd.Parameters.AddWithValue("jobId", jobId);
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        // Not currently used
        //public IEnumerable<JobInfo> GetActiveJobs()
        //{
        //    using (var conn = new SqlConnection(_connectionString))
        //    {
        //        conn.Open();
        //        try
        //        {
        //            using (var cmd = conn.CreateCommand())
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.CommandText = "GetActiveJobs";
        //                cmd.Parameters.AddWithValue("workerId", _workerId);
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        yield return new JobInfo(reader.GetString(0), reader.GetString(1))
        //                                         {
        //                                             JobType = (JobType) reader.GetInt32(2),
        //                                             Status = (JobStatus) reader.GetInt32(3),
        //                                             StatusMessage =
        //                                                 reader.IsDBNull(4) ? String.Empty : reader.GetString(4),
        //                                             RetryCount = reader.GetInt32(5)
        //                                         };
        //                    }
        //                }
        //            }
        //        }
        //        finally
        //        {
        //            conn.Close();
        //        }
        //    }
        //}

        public JobInfo GetJob(string storeId, string jobId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.CommandText = "Brightstar_GetJob";
                        cmd.Parameters.AddWithValue("jobId", jobId);
                        cmd.Parameters.AddWithValue("storeId", storeId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            JobInfo ret = null;
                            if (reader.Read())
                            {
                                ret = new JobInfo(jobId, storeId)
                                          {
                                              JobType = (JobType) reader.GetInt32(2),
                                              Status = (JobStatus) reader.GetInt32(3),
                                              StatusMessage = reader.IsDBNull(4) ? String.Empty : reader.GetString(4)
                                          };
                            }
                            return ret;
                        }
                    }
                } finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Cleans up the store by deleting all completed jobs that were finished
        /// before the current date/time less maxJobAge
        /// </summary>
        /// <param name="maxJobAge"></param>
        public void Cleanup(TimeSpan maxJobAge)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_Cleanup";
                        cmd.Parameters.AddWithValue("maxJobAge", (int) maxJobAge.TotalSeconds);
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Removes all jobs (completed and uncompleted) from the queue
        /// </summary>
        public void ClearAll()
        {
            using(var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_ClearAllJobs";
                        cmd.ExecuteNonQuery();
                    }
                } finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Returns the info for the last job that committed to the specified store
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        public JobInfo GetLastCommit(string storeId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_GetLastCommit";
                        cmd.Parameters.AddWithValue("storeId", storeId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            JobInfo ret = null;
                            if (reader.Read())
                            {
                                var jobId = reader.GetString(0);
                                ret = new JobInfo(jobId, storeId)
                                          {
                                              JobType = (JobType) reader.GetInt32(2),
                                              Status = (JobStatus) reader.GetInt32(3),
                                              StatusMessage = reader.IsDBNull(4) ? String.Empty : reader.GetString(4),
                                          };
                                if (!reader.IsDBNull(5)) ret.ProcessingCompleted = reader.GetDateTime(5);
                            }
                            return ret;
                        }
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Returns an enumeration over all jobs in the store that are to work on the specified store
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        public IEnumerable<JobInfo> GetJobs(string storeId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Brightstar_GetStoreJobs";
                        cmd.Parameters.AddWithValue("storeId", storeId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var ret = new List<JobInfo>();
                            while (reader.Read())
                            {
                                ret.Add(ReadJobInfo(reader));
                            }
                            return ret;
                        }
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        #endregion

        private void LogJob(string jobId)
        {
            try
            {
                JobInfo jobInfo = null;
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "Brightstar_GetJobDetail";
                            cmd.Parameters.AddWithValue("jobId", jobId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    jobInfo = new JobInfo(reader.GetString(0), reader.GetString(1));
                                    jobInfo.JobType = (JobType) reader.GetInt32(2);
                                    jobInfo.Status = (JobStatus) reader.GetInt32(3);
                                    jobInfo.StatusMessage = reader.IsDBNull(4) ? String.Empty : reader.GetString(4);
                                    jobInfo.ScheduledRunTime =
                                        reader.IsDBNull(5) ? (DateTime?) null : reader.GetDateTime(5);
                                    jobInfo.StartTime = reader.IsDBNull(6) ? (DateTime?) null : reader.GetDateTime(6);
                                    jobInfo.ProcessorId = reader.IsDBNull(7) ? String.Empty : reader.GetString(7);
                                    jobInfo.ProcessingCompleted =
                                        reader.IsDBNull(8) ? (DateTime?) null : reader.GetDateTime(8);
                                    jobInfo.ProcessingException =
                                        reader.IsDBNull(9) ? String.Empty : reader.GetString(9);
                                    jobInfo.RetryCount = reader.GetInt32(10);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error retrieving job detail for job {0}. Cause: {1}", jobId, ex);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
                if (jobInfo != null)
                {
                    var connectionString =
                        RoleEnvironment.GetConfigurationSettingValue(AzureConstants.DiagnosticsConnectionStringName);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    tableClient.CreateTableIfNotExist("jobs");
                    TableServiceContext serviceContext = tableClient.GetDataServiceContext();
                    JobLogEntity jobLogEntity = new JobLogEntity(jobInfo);
                    serviceContext.AddObject("jobs", jobLogEntity);
                    serviceContext.SaveChangesWithRetries();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error logging detail for job {0}. Cause: {1}", jobId, ex);
            }
        }

        private static JobInfo ReadJobInfo(IDataRecord reader)
        {
            return new JobInfo(reader.GetString(0), reader.GetString(1))
                       {
                           JobType = (JobType) reader.GetInt32(2),
                           Status = (JobStatus) reader.GetInt32(3),
                           StatusMessage = reader.IsDBNull(4) ? String.Empty : reader.GetString(4),
                           ScheduledRunTime = reader.IsDBNull(5) ? (DateTime?) null : reader.GetDateTime(5),
                           StartTime = reader.IsDBNull(6) ? (DateTime?) null : reader.GetDateTime(6),
                           ProcessingCompleted = reader.IsDBNull(7) ? (DateTime?) null : reader.GetDateTime(7)
                       };
        }

        private object StatusMessageValue(string statusMessage)
        {
            return statusMessage == null
                       ? (object) DBNull.Value
                       : statusMessage.Length <= 2000 ? statusMessage : statusMessage.Substring(0, 2000);
        }
    }
}
