.. _Admin_API:

**********
 Admin API
**********


In addition to the APIs already covered for updating and querying stores, there are a number 
of useful administration APIs also provided by BrightstarDB. A Visual Studio solution file 
containing some sample applications that use these APIs can be found in 
[INSTALLDIR]/Samples/StoreAdmin.


Managing Commit Points
======================


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


**To Retrieve a List of Commit Points**

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
=======================

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


Consolidate The Store
=====================

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

