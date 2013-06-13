/**
 * Creates the job queue table and stored procedures in the database
 */

DROP ROLE brightstar_role_gateway
GO

DROP PROCEDURE [dbo].[Brightstar_CompleteJob]
GO
DROP PROCEDURE [dbo].[Brightstar_GetJob]
GO
DROP PROCEDURE [dbo].[Brightstar_GetJobDetail]
GO
DROP PROCEDURE [dbo].[Brightstar_GetStoreJobs]
GO
DROP PROCEDURE [dbo].[Brightstar_JobException]
GO
DROP PROCEDURE [dbo].[Brightstar_NextJob]
GO
DROP PROCEDURE [dbo].[Brightstar_QueueJob]
GO
DROP PROCEDURE [dbo].[Brightstar_StartCommit]
GO
DROP PROCEDURE [dbo].[Brightstar_UpdateStatus]
GO
DROP PROCEDURE [dbo].[Brightstar_ClearAllJobs]
GO
DROP PROCEDURE [dbo].[Brightstar_Cleanup]
GO
DROP PROCEDURE [dbo].[Brightstar_GetActiveJobs]
GO
DROP PROCEDURE [dbo].[Brightstar_ReleaseJob]
GO
DROP PROCEDURE [dbo].[Brightstar_GetLastCommit]
GO
DROP TABLE [dbo].[Brightstar_Jobs]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE ROLE brightstar_role_gateway
GO

CREATE TABLE [dbo].[Brightstar_Jobs](
 [Id] [varchar](255) NOT NULL,
 [StoreId] [nvarchar](255) NOT NULL,
 [JobType] [int] NOT NULL,
 [JobStatus] [int] NOT NULL,
 [StatusMessage] [nvarchar](2000) NULL,
 [JobData] [nvarchar](max) NULL,
 [QueuedTime] [datetime] NOT NULL,
 [ScheduledRunTime] [datetime] NULL,
 [ProcessingStarted] [datetime] NULL,
 [Processor] [varchar](255) NULL,
 [ProcessingCompleted] [datetime] NULL,
 [ProcessingException] [nvarchar](max) NULL,
 [RetryCount] [int] NOT NULL DEFAULT 0
 CONSTRAINT [PK_Brightstar_Jobs] PRIMARY KEY NONCLUSTERED ([Id] ASC))
GO
SET ANSI_PADDING OFF
GO
GRANT INSERT, DELETE, SELECT, UPDATE ON [dbo].[Brightstar_Jobs] TO brightstar_role_gateway
GO

CREATE UNIQUE CLUSTERED INDEX [IX_Brightstar_Jobs_QueuedTime] ON [dbo].[Brightstar_Jobs] 
(
 [QueuedTime] ASC,
 [Id] ASC
)
GO

CREATE NONCLUSTERED INDEX [IX_Brightstar_Jobs_StoreId] ON [dbo].[Brightstar_Jobs] 
(
 [StoreId] ASC
)
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- QueueJob
--   @id - the new job id. must be unique
--   @storeId - the id of the target store for the job. Empty string for jobs that don't affect individual stores
--   @jobType - the new job type
--   @jobData - input data for the job
--   @scheduledStartTime - the earliest start time for the job.
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_QueueJob]
 @id varchar(255),
 @storeId nvarchar(255),
 @jobType int,
 @jobData nvarchar(max),
 @scheduledRunTime datetime
AS
BEGIN
 SET NOCOUNT OFF;
 SET @scheduledRunTime = COALESCE(@scheduledRunTime, CURRENT_TIMESTAMP)
 INSERT INTO Brightstar_Jobs(Id, StoreId, JobType, JobStatus, JobData, QueuedTime, ScheduledRunTime) 
 VALUES (@id, @storeId, @jobType, 0, @jobData, CURRENT_TIMESTAMP, @scheduledRunTime)
END
GO
GRANT EXECUTE ON [dbo].[Brightstar_QueueJob] TO brightstar_role_gateway
GO

-- ======================================
-- GetActiveJobs
--  @workerId the ID of the processor whose active jobs are to be returned
-- Selects all jobs from the job table where the Processor is @workerId
-- and the ProcessingCompleted timestamp is not set
-- ======================================
CREATE PROCEDURE [dbo].[Brightstar_GetActiveJobs]
 @workerId varchar(255)
AS
BEGIN
 SELECT Id, StoreId, JobType, JobStatus, JobData, StatusMessage, RetryCount 
 FROM Brightstar_Jobs
 WHERE Processor = @workerId AND ProcessingCompleted IS NULL
END
GO
GRANT EXECUTE ON [dbo].[Brightstar_GetActiveJobs] TO brightstar_role_gateway
GO

-- =============================================
-- UpdateStatus
--  @jobId the ID of the job to update
--  @statusMessage - the new status message for the job
-- Updates the user-friendly status message on the job
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_UpdateStatus]
 @jobId varchar(255),
 @statusMessage nvarchar(2000)
AS
BEGIN
 SET NOCOUNT ON;
 UPDATE Brightstar_Jobs SET StatusMessage=@statusMessage WHERE Id=@jobId
END
GO
GRANT EXECUTE ON [dbo].[Brightstar_UpdateStatus] TO brightstar_role_gateway
GO

-- =============================================
-- StartCommit
--  @jobId - the ID of the job to update
--  @statusMessage - the user-friendly status message for the job
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_StartCommit]
 @jobId varchar(255),
 @statusMessage nvarchar(2000)
AS
BEGIN
 SET NOCOUNT ON;
 UPDATE Brightstar_Jobs SET JobStatus=2, StatusMessage=@statusMessage WHERE Id=@jobId
END
GO
GRANT EXECUTE ON [dbo].[Brightstar_StartCommit] to brightstar_role_gateway
GO

-- =============================================
-- NextJob
-- Acquires a job for processing by a specific worker
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_NextJob] 
 @workerId varchar(255),
 @storeId nvarchar(255)
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @jobId varchar(50)
 DECLARE @now datetime
 SET @now = CURRENT_TIMESTAMP

 -- First check to see if there is a Job already running for the worker
 -- If so, this job needs to be re-run
 SET @jobId = (
  SELECT TOP 1 Id FROM Brightstar_Jobs j WHERE
	j.Processor=@workerId AND
	(j.JobStatus = 1 OR j.JobStatus = 2)
 )
 IF (@jobId IS NULL)
 BEGIN
	 -- If storeId is NULL, get next scheduled pending job that is not
	 -- on a store that has an active job on it already
	 IF (@storeId IS NULL)
	 BEGIN
	  SET @jobId = (
	   SELECT TOP 1 Id FROM
	   Brightstar_Jobs j WHERE 
		(j.ScheduledRunTime IS NULL OR j.ScheduledRunTime <= @now) AND 
		j.JobStatus=0 
		AND NOT EXISTS(
		 SELECT Id FROM Brightstar_Jobs w 
		 WHERE 
		  w.StoreId=j.StoreId 
		  AND w.JobStatus > 0
		  AND w.ProcessingCompleted IS NULL)
	   ORDER BY QueuedTime ASC
	  )
	 END
	 ELSE 
	 BEGIN
	  SET @jobId = (
	   SELECT TOP 1 Id
	   FROM Brightstar_Jobs
	   WHERE StoreId=@storeId AND Processor=@workerId AND ProcessingCompleted IS NULL
	   ORDER BY QueuedTime ASC
	  )
	  IF @jobId IS NULL AND NOT EXISTS (SELECT Id FROM Brightstar_Jobs WHERE Processor IS NOT NULL AND ProcessingCompleted IS NULL AND StoreId=@storeId)
	   SET @jobId = (
		SELECT TOP 1 Id 
		FROM Brightstar_Jobs 
		WHERE JobStatus = 0 AND StoreId=@storeId 
		ORDER BY QueuedTime ASC
	   )
	 END
 END
 
 IF (@jobId IS NOT NULL)
 BEGIN
  UPDATE Brightstar_Jobs SET JobStatus = 1, ProcessingStarted=CURRENT_TIMESTAMP, Processor=@workerId WHERE Id=@jobId
  SELECT Id, StoreId, JobType, JobStatus, JobData, RetryCount FROM Brightstar_Jobs WHERE Id=@jobId
 END
END

GRANT EXECUTE ON [dbo].[Brightstar_NextJob] to brightstar_role_gateway
GO

-- =============================================
-- JobException
--  @jobId - the ID of the job that failed
--  @statusMessage - user friendly error message
--  @processingException - error stack trace for admin/devs
--
--  This store proc should be invoked when the processing of a job
--  fails with an exception. It makes the following changes to the
--  job record:
--    Set JobStatus to 0 (pending)
--    Set StatusMessage to @statusMessage
--    Append @processingException to ProcessingException
--    Increment RetryCount
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_JobException]
 @jobId varchar(255),
 @statusMessage nvarchar(2000),
 @processingException nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 
 DECLARE @linebreak as varchar(2)
 SET @linebreak = CHAR(13) + CHAR(10)
 
 UPDATE Brightstar_Jobs 
 SET JobStatus=0, 
  StatusMessage=@statusMessage, 
  ProcessingException=COALESCE(ProcessingException, '') +  '-----' + @linebreak + COALESCE(@processingException, '') + @linebreak,
  RetryCount=RetryCount+1
 WHERE
  Id=@jobId and RetryCount <= 3

 UPDATE Brightstar_Jobs 
 SET JobStatus=99,
	ProcessingCompleted=CURRENT_TIMESTAMP
 WHERE
 Id=@jobId and RetryCount > 3

END
GO
GRANT EXECUTE ON [dbo].[Brightstar_JobException] to brightstar_role_gateway
GO

-- =============================================
-- Author:  <Author,,Name>
-- Create date: <Create Date,,>
-- Description: <Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_GetJob]
 @jobId varchar(255),
 @storeId nvarchar(255)
AS
BEGIN
 -- SET NOCOUNT ON added to prevent extra result sets from
 -- interfering with SELECT statements.
 SET NOCOUNT ON;

 SELECT TOP 1 Id, StoreId, JobType, JobStatus, StatusMessage, ScheduledRunTime, ProcessingStarted, ProcessingCompleted FROM Brightstar_Jobs WHERE Id=@jobId AND StoreId=@storeId
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_GetJob] to brightstar_role_gateway
GO

CREATE PROCEDURE [dbo].[Brightstar_GetJobDetail]
  @jobId varchar(255)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT TOP 1 Id, StoreId, JobType, JobStatus, StatusMessage, ScheduledRunTime, ProcessingStarted, Processor, ProcessingCompleted, ProcessingException, RetryCount
	FROM [Brightstar_Jobs]
	WHERE Id = @jobId
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_GetJob] to brightstar_role_gateway
GO

-- =======================================
-- GetStoreJobs
-- Returns the details for all jobs targetting the specified store
-- @storeId - The ID of the target store
-- ========================================
CREATE PROCEDURE [dbo].[Brightstar_GetStoreJobs]
	@storeId nvarchar(255)
AS
BEGIN
SELECT Id, StoreId, JobType, JobStatus, StatusMessage, ScheduledRunTime, ProcessingStarted, ProcessingCompleted FROM Brightstar_Jobs WHERE StoreId=@storeId ORDER BY ScheduledRunTime
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_GetStoreJobs] to brightstar_role_gateway
GO

-- ==================================
-- GetLastCommit
-- Returns the details for the last job that
-- committed to a particular store
-- 
-- @storeId - the id of the store
-- ===================================
CREATE PROCEDURE [dbo].[Brightstar_GetLastCommit]
	@storeId nvarchar(255)
AS
BEGIN
	SELECT TOP 1 Id, StoreId, JobType, JobStatus, StatusMessage, ProcessingCompleted 
	FROM Brightstar_Jobs
	WHERE ProcessingCompleted IS NOT NULL
	ORDER BY ProcessingCompleted DESC
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_GetLastCommit] to brightstar_role_gateway
GO

-- =============================================
-- ReleaseJob
--  @jobId - the ID of the job to be released
-- Returns the specified job to the queue
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_ReleaseJob]
  @jobId varchar(255)
AS
BEGIN
 SET NOCOUNT ON
 UPDATE Brightstar_Jobs 
 SET Processor=NULL, JobStatus=0 
 WHERE Id=@jobId
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_ReleaseJob] to brightstar_role_gateway
GO

-- =============================================
-- CompleteJob
-- @jobId - the ID of the job to be marked as completed
-- @finalStatus - the status code to record for the completed job
-- @finalStatusMessage - the user friendly message to record for the completed job
-- =============================================
CREATE PROCEDURE [dbo].[Brightstar_CompleteJob]
 @jobId varchar(255),
 @finalStatus int,
 @finalStatusMessage nvarchar(2000)
AS
BEGIN
 SET NOCOUNT ON;
 UPDATE Brightstar_Jobs 
 SET JobStatus=@finalStatus, StatusMessage=@finalStatusMessage, ProcessingCompleted=CURRENT_TIMESTAMP
 WHERE Id=@jobId
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_CompleteJob] to brightstar_role_gateway
GO

/**
 * Deletes all jobs (regardless of their status) from the queue.
 * This is intended only for testing purposes.
 */
CREATE PROCEDURE [dbo].[Brightstar_ClearAllJobs]
AS
BEGIN
 DELETE FROM Brightstar_Jobs
END
GO

GRANT EXECUTE ON [dbo].[Brightstar_ClearAllJobs] to brightstar_role_gateway
GO

/**
 * Deletes all jobs that have a ProcessingCompleted timestamp
 * that is earlier than the current date/time less @maxJobAge (in seconds)
 */
CREATE PROCEDURE [dbo].[Brightstar_Cleanup]
 @maxJobAge int
AS
BEGIN
 DECLARE @cutOffDate datetime
 SET @cutOffDate = DATEADD(second, 0 - @maxJobAge, CURRENT_TIMESTAMP)
 DELETE FROM Brightstar_Jobs WHERE 
  ProcessingCompleted IS NOT NULL AND
  ProcessingCompleted < @cutOffDate
END

GRANT EXECUTE ON [dbo].[Brightstar_Cleanup] to brightstar_role_gateway
GO
