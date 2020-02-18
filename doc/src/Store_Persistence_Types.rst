.. _Store_Persistence_Types:

************************
 Store Persistence Types
************************

BrightstarDB supports two different file formats for storing its index information. The main 
difference between the two formats is the way in which modified pages of the index are written 
to the index file.


Append-Only
===========

The Append-Only format means that BrightstarDB will write modified pages to the end of the 
index file. This approach has a number of benefits:

  1. Writers never block readers, so any number of read operations (typically SPARQL queries) 
     can be executed in parallel with updates to the index. Each reader accesses the store in the 
     state that it was when their operation began.

  #. Reads can access any previous state of the store. This is because the full history of 
     updates to pages is maintained by the store.

  #. Writes are faster - because they only append to the end of the file rather than needing 
     to seek to a location within the file to be updated.

The down-side of this format is that the index file will grow not only as more data is added 
but also with every update operation applied to the store. BrightstarDB does provide a way to 
:ref:`consolidate the data in a store to just its latest state <Admin_Consolidate_Store>`, removing all the previous historical page states so 
this operation executed periodically can help to keep the file size under control.

In general the Append-Only format is recommended for most systems as long as disk space is not 
constrained.


Rewritable
==========

The Rewriteable store format manages an active and a shadow copy of each page in the index. 
Writes are directed to the shadow copy while readers can access the current committed state of 
the store by reading from the active copy. On a commit, the shadow copy becomes the active and 
vice-versa. This approach keeps file size under control as changes to an index page are always 
written to one of the two copies of the page. However this format has some disadvantages 
compared to the append-only store.

  1. Readers that take a long time to complete can get blocked by writers. In general if a 
     reader completes in the time taken for a write to complete, the two operations can execute 
     in parallel, however in the case that a reader requires access to the store across two 
     successive reads, there is the potential that index pages could be modified. To avoid 
     inconsistent results due to dirty reads, when a reader detects this it will automatically 
     retry its current operation. This means that in stores where there are frequent, small 
     updates readers can potentially be blocked for a long time as new writes keep forcing the 
     read operation to be retried.

  #. Write operations can be a bit slower - this is because pages are written to a fixed 
     location within the index file, requiring a disk seek before each page write.

In general the Rewritable store format is recommended for embedded applications; for mobile 
devices that have space constraints to consider; or for server applications that are only 
required to support infrequent and/or large updates.



Specifying the Store Persistence Type
=====================================

The persistence type to use for a store must be specified when the store is created and cannot 
be changed after the store has been created. The default persistence type is configured in the 
application configuration file for the application (or the web.config for web applications). 
To configure the default, you must add an entry to the appSetting section of the application 
configuration file with the key ``BrightstarDB.PersistenceType`` and the value ``appendonly`` 
for an Append-Only store or ``rewrite`` for a Rewriteable store (in both cases the values are 
case-insensitive). 

It is also possible to override the default persistence type at runtime by calling the 
appropriate ``CreateStore()`` operation on the BrighstarDB service client API. If no default value 
is defined in the application configuration file and no override value is passed to the 
``CreateStore()`` method, the default persistence type used by BrightstarDB is the Append-Only 
persistence type.







