.. _Concurrency:

===============================
Concurrency and Multi-threading
===============================

This section covers the use of BrightstarDB in a variety of concurrent-access and multi-threading scenarios and describes BrightstarDB's 


Concurrent Access to Stores
===========================

A BrightstarDB service of type "embedded" assumes that it has sole access to the store data files and the stores directory. You should
not attempt to run two embedded instances of BrightstarDB concurrently with the same stores directory. If you want multiple applications
to concurrently access the same collection of BrightstarDB stores you should instead run a BrightstarDB service that provides access to
the store and then change you applications to use the "rest" connection string and connect to the server.

On a single store, BrightstarDB supports single-threaded writes and multi-threaded reads. Write operations are serialized (and are executed
in the order that they are received), with read operations being executed in parallel with the writes. The isolation level for reads is 
"read committed" - in other words a read will see the state of the last successful commit of the store, even if a write is in progress or
if a write starts while the read is being executed. 

.. warning::

    The current re-writeable store implementation is not structured to hold on to commit points while reads are being executed.
    If a single read operation spans multiple write operations, the commit point that the read is using will be removed from
    the store. If this happens, the read request is automatically retried using the latest commit point.
    
    This scenario never occurs with the append-only store implementation as that store structure is designed to keep all
    previous commits.

Thread-safety
=============

All implementations of IBrightstarService are thread-safe, this means you can use the low-level :ref:`RDF API <RDF_Client_API>` safely
in a multi-threaded application. However, the IDataObjectContext, IDataObjectStore and the Entity Framework
contexts are not. Multi-threaded applications that use either the :ref:`Data Objects API <Data_Object_Layer>` or the 
:ref:`Entity Framework <Entity_Framework>` should ensure that each thread uses its own context and store instance for these API calls.
