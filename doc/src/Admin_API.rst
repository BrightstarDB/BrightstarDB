.. _Admin_API:

**********
 Admin API
**********

    In addition to the APIs already covered for updating and querying stores, there are a number 
    of useful administration APIs also provided by BrightstarDB. A Visual Studio solution file 
    containing some sample applications that use these APIs can be found in 
    [INSTALLDIR]/Samples/StoreAdmin.

.. _Admin_API_Jobs:

Jobs
====

    When a new job is passed to a store, the job information is added to a queue. Jobs are queued
    and executed in the order they are received. Through the BrightstarDB APIs you can retrieve
    that list of jobs and monitor the state of a given job.
    
.. _Admin_API_IJobInfo:

IJobInfo 
--------

Job information retrieved from BrightstarDB is represented by an instance of the ``BrightstarDB.Client.IJobInfo``
interface. This interface exposes the following properties:

    - ``JobId``: The unique identifier for the job.
    - ``Label``: An optional user-friendly name for the job. The label is set by passing it in with the optional
      ``label`` parameter on methods that start a job.
    - ``JobPending``: A boolean flag. If true, the job is in the queue but has not yet been executed.
    - ``JobStarted``: A boolean flag. If true, the job is in the queue and is currently being executed.
    - ``JobCompletedWithErrors``: A boolean flag. If true, the processing of the job completed but the job itself failed for some reason.
      More information can be found by examining the ``StatusMessage`` and ``ExceptionInfo`` properties
    - ``JobCompletedOk``: A boolean flag. If true the job has completed processing successfully.
    - ``StatusMessage``: The current job status message. For some long-running jobs such as RDF import,
      this message will be updated as the job runs. For other types of job the status message may only
      be updated on completion or failure of the job.
    - ``ExceptionInfo``: If an error is raised internally as a job is run, the exception information wil be
      recorded in this property. The value is a ``BrightstarDB.Dto.ExceptionDetailObject`` which provides
      access to the exception type, message and any inner exceptions.
    - ``QueuedTime``: The date/time when the job was queued to be processed.
    - ``StartTime``: The date/time when the job started processing.
    - ``EndTime``: The date/time when the job completed processing.
    
    .. note::
        Timestamps are all provided in UTC and are serialized with a resolution of 1 second.

Retrieving the Jobs List
------------------------

    The method to retrieve a list of jobs from a store is ``GetJobInfo(string storeName, int skip, int take)``.
    The ``storeName`` parameter specifies the name of the store to retrieve job information from.
    The ``skip`` and ``take`` parameters can be used for paging long lists of jobs.
    The return values is and enumerable of :ref:`Admin_API_IJobInfo` instances.

    .. note::
        The list of jobs maintained by the BrightstarDB server is not persistent. This means that the 
        jobs list is reset whenever the server gets restarted so if you were to retrieve the list of
        jobs immediately after starting the server you would get an empty list.
        
Monitoring Individual Jobs
--------------------------

    The methods in the BrightstarDB API that queue long running jobs all return an instance of the 
    :ref:`Admin_API_IJobInfo` interface. To check on the status of a job, you can use the method
    ``GetJobInfo(string storeName, string jobId)``. The ``storeName`` parameter is the name
    of the store that the job runs against and ``jobId`` is the unique identifier for the job (which
    is provided in the ``JobId`` property of the :ref:`Admin_API_IJobInfo` object). The return value
    of this method is an :ref:`Admin_API_IJobInfo` instance that represents the current state of the job.
    
    Monitoring the status of a job is then a question of simply polling the server by calling the
    ``GetJobInfo(string,string)`` method until either the ``JobCompletedOk`` or ``JobCompletedWithErrors``
    property on the returned :ref:`Admin_API_IJobInfo` instance gets set to true.
    
    When polling status in this way you should be aware of the following:
    
    1. Polling for status does require some (fairly minimal) server resources, so you should avoid
       polling in a very tight loop.
    2. If the server gets reset before your job has a chance to execute, the job information will
       be lost and a ``BrightstarClientException`` will get thrown. In this case your code should
       either notify the user of the failure or you could opt to simply resubmit the job.
    
    .. note::
        Job IDs are assigned by the server using GUIDs so even if the server gets reset it is not possible 
        to end up monitoring a different job with the same JobId.
        
Commit Points
=============

    .. note::

        Commit Points are a feature that is only available with the Append-Only store persistence 
        type. If you are accessing a store that uses the Rewrite persistence type, operations on a 
        Commit Points are not supported and will raise a BrightstarClientException if an attempt is 
        made to query against or revert to a previous Commit Point.


    Each time a transaction is committed to a BrightstarDB store, a new commit point is written. 
    Unlike a traditional database log file, a commit point provides a complete snapshot of the 
    state of the BrightstarDB store immediately after the commit took place. This means that it is 
    possible to query the BrightstarDB store as it existed at some previous point in time. It is 
    also possible to revert the store to a previous commit point, but in keeping with the 
    BrightstarDB architecture, this operation doesn't actually delete the commit points that 
    followed, but instead makes a new commit point which duplicates the commit point selected for 
    the revert.


Retrieving Commit Points
------------------------

    The method to retrieve a list of commit points from a store is ``GetCommitPoints()`` on the 
    ``IBrightstarService`` interface. There are two versions of this method. The first takes a store 
    name and skip and take parameters to define a subrange of commit points to retrieve, the 
    second adds a date/time range in the form of two date time parameters to allow more specific 
    selection of a particular commit point range. The code below shows an example of using the 
    first of these methods::

        // Create a client - the connection string used is configured in the App.config file.
        var client = BrightstarService.GetClient();
        foreach(var commitPointInfo in client.GetCommitPoints(storeName, 0, 10))
        {
           // Do something with each commit point
        }


    To avoid operations that return potentially very large results sets, the server will not 
    return more than 100 commit points at a time, attempting to set the take parameter higher than 
    100 will result in an ``ArgumentException`` being raised.

    The structures returned by the ``GetCommitPoints()`` method implement the ``ICommitPointInfo`` 
    interface, this interface provides access to the following properties:

        ``StoreName``
            the name of the store that the commit point is associated with.

        ``Id``
            the commit point identifier. This identifier is unique amongst all commit points in the same store.

        ``CommitTime``
            the UTC date/time when the commit was made.

        ``JobId``
            the GUID identifier of the transaction job that resulted in the commit. The value 
            of this property may be Guid.Empty for operations that were not associated with a 
            transaction job (e.g initial store creation).

Querying A Commit Point
-----------------------

    To execute a SPARQL query against a particular commit point of a store, use the overload of 
    the ``ExecuteQuery()`` method that takes an ``ICommitPointInfo`` parameter rather than a store name 
    string parameter::

        var resultsStream = client.ExecuteQuery(commitPointInfo, sparqlQuery);


    The resulting stream can be processed in exactly the same way as if you had queried the 
    current state of the store.


Reverting The Store
===================

    Reverting the store takes a copy of an old commit point and pushes it to the top of the commit 
    point list for the store. Queries and updates are then applied to the store as normal, and the 
    data modified by commit points since the reverted one is effectively hidden. 

    This operation does not delete the commit points added since the reverted one, those commit 
    points are still there as long as a Coalesce operation is not performed, meaning that it is 
    possible to "re-revert" the store to its state before the revert was applied. The method to 
    revert a store is also on the ``IBrightstarService`` interface and is shown below::

        var client = BrightstarService.GetClient();
        ICommitPointInfo commitPointInfo = ... ; // Code to get the commit point we want to revert to
        client.RevertToCommitPoint(storeName, commitPointInfo); // Reverts the store


Consolidating The Store
=======================

    Over time the size of the BrightstarDB store will grow. Each separate commit adds new data to 
    the store, even if the commit deletes triples from the store the commit itself will extend the 
    store file. The ``ConsolidateStore()`` operation enables the BrightstarDB store to be compressed, 
    removing all commit point history. The operation rewrites the store data file to a shadow file 
    and then replaces the existing data file with the new compressed data file and updates the 
    master file. The consolidate operation blocks new writers, but allows readers to continue 
    accessing the data file up until the shadow file is prepared. The code required to start a 
    consolidate operation is shown below::

        var client = BrightstarService.GetClient();
        var consolidateJob = client.ConsolidateStore(storeName);

    This method submits the consolidate operation to the store as a long-running job. Because this 
    operation may take some time to complete the call does not block, but instead returns an 
    ``IJobInfo`` structure which can be used to monitor the job. The code below shows a typical loop 
    for monitoring the consolidate job::

        while (!(consolidateJob.JobCompletedOk || consolidateJob.JobCompletedWithErrors))
        {
           System.Threading.Thread.Sleep(500);
           consolidateJob = client.GetJobInfo(storeName, consolidateJob.JobId);
        }

.. _Admin_Snapshots:

Creating Store Snapshots
========================

    From version 1.4, BrightstarDB now provides an API to allow you to create an independent
    snapshot of a store. A snapshot is an entirely separate store that contains a consolidated
    version of the data in the source store. You can use snapshots for a number of purposes,
    for example creating replicas for query or branching the data in a store to allow two
    different parallel modifications to the data.
    
    The API for creating a store snapshot is quite simple::
    
        var snapshotJob = client.CreateSnapshot(sourceStoreName, targetStoreName, 
            persistenceType, commitPoint);
            
    The ``sourceStoreName`` and ``targetStoreName`` parameters name the source for the 
    snapshot and the store that will be created by the snapshot respectively. The store
    named by ``targetStoreName`` must not exist (the method will not overwrite existing
    stores). The ``persistenceType`` parameter can be one of ``PersistenceType.AppendOnly``
    or ``PersistenceType.Rewrite`` and specifies the type of persistence used by the 
    target store. The target store can use a different persistence type to the source store.
    The commitPointId parameter is optional. If it is not specified or if you pass null, 
    the snapshot will be created from the most recent commit of the source store. If you
    want to create a snapshot from a previous commit of the source store, you can pass
    the ``ICommitPointInfo`` instance for that commit.
    
    ..note:
    
        A snapshot can be created from a previous commit point only if the source store
        persistence type is ``PersistenceType.AppendOnly``
        
.. _Admin_Stats:
   
Store Statistics
================

    From version 1.4, BrightstarDB can now optionally maintain some basic triple-count statistics.
    The statistics kept are the total number of triples in the store, and the total number of
    triples for each distinct predicate. Statistics can be maintained automatically by the
    store or updated using an API call. As with transaction logs, BrightstarDB will maintain
    historical stats, allowing you to analyse the changes in a store over time if you wish.


Retrieving Statistics
---------------------

    The API provides two methods for retrieving statistics. To retrieve just the most recently
    generated statistics you can use code like this::

        var client = BrightstarService.GetClient();
        var stats = client.GetStatistics(storeName);
        
    This method will return an ``IStoreStatistics`` instance which represents the most recent
    statistics for the store. The ``IStoreStatistics`` interface defines the following properties:

        *   CommitId and CommitTimestamp: The identifier and timestamp of the database commit
            that the statistics relate to. This information enables you to relate statistics
            to a commit point.
        *   TotalTripleCount: The total number of triples in the store
        *   PredicateTripleCounts: A dictionary of entries in which the key is a predicate URI
            and the value is the count of the number of triples using that predicate in the store.
            
    If you want to analyse the changes in statistics over a period of time, there is an
    alternate method that retrieves multiple statistics records in one call::

        DateTime fromDate = DateTime.UtcNow.Subtract(Timespan.FromDays(10));
        DateTime toDate = DateTime.UtcNow();
        IEnumerable<IStoreStatistics> allStats = 
            client.GetStatistics(storeName, fromDate, toDate, 0, 100);
        
    As you can see from the example above, this method takes a date range allowing you to select
    the period in time you want stats for. The final two parameters are a skip and take that is
    applied to the list of statistics after the date range filter. A BrightstarDB server will not
    return more than 100 statistics records at a time, so if your date range covers a period
    with more statistics in it than this you will need to make multiple calls using the 
    skip and take parameters for paging.


.. _Admin_Stats_Update:

Updating Statistics
-------------------

    Statistics can be updated automatically by the store if it is configured to do so (see the
    next section for details). However you can also use the API to request an update of the
    statistics. Statistics updates are processed as a long running job as for large stores
    the process may take some time::

        IJobInfo statsUpdateJob = client.UpdateStatistics(storeName);
        
    This method call will queue the update job and return a structure that you can use to poll 
    until the job is completed (or you can simply call the method in a fire-and-forget manner).


.. _Admin_Stats_AutomaticUpdate:

Automatic Update of Statistics
------------------------------

    The BrighstarDB server process can automatically update statistics. This is done by 
    periodically queuing a job to update statistics. The period between updates is controlled
    by two configuration settings in the application configuration file for your BrightstarDB
    service (or other BrightstarDB application if you are using the embedded store). 

    The setting ``BrightstarDB.StatsUpdate.Timespan`` specifies the minimum number of seconds
    that must pass between executions of the statistics update job.

    The setting ``BrightstarDB.StatsUpdate.TransactionCount`` specifies the minimum number of
    other transaction or update jobs that must be queued between executions of the statistics
    update job.

    These conditions are only checked after a job is placed in the queue, so during quiet 
    periods when there is no activity statistics will not be unnecessarily updated. 
    Both conditions have to be met before a statistics update job will be queued. 
    Normally it makes sense to set both of these properties to a non-zero value to ensure that
    both sufficient time has passed and sufficient changes have been made to the store to
    justify the overhead of running a statistics update. However, you can set either one
    of these properties to zero (which is the default value) to only take account of the 
    other. Setting both of these configuration properties to zero (or leaving them out
    of the configuration file) results in automatic statistics update being disabled.
