.. _HTTP_API:

***********
 HTTP API
***********

The HTTP API is the network interface to a BrightstarDB server. It allows access to BrightstarDB
from just about any programming language using basic HTTP operations and JSON data structures.

The following documentation assumes that you have a basic understanding of the HTTP protocol 
and JSON. Where URLs are specified it is assumed that the BrightstarDB server base address
is http://localhost:8090/brightstar/ (this is the default address for a BrightstarDB server
running on your local machine). Where a URL contains a value in curly braces {likeThis}, it indicates
a replaceable part of the URL - you would replace the value (and the curly braces) with some other string
as indicated in the description text.

The BrightstarDB API is broadly RESTful in its approach, it exposes a number of uniquely addressable
endpoints each of which support a variety of HTTP operations. In general GET will retrieve some
representation of the resource, POST (if supported) will allow an update of the resource (or the addition
of an item to a collection if the resource represents a collection), DELETE is used to remove the resource.
PUT and HEAD operations are also supported for a small subset of the resources.


Swagger API Documentation
=========================

.. _Swagger: http://swagger.io/

BrightstarDB provides a `Swagger`_ API description at http://localhost:8090/brightstar/documentation, 
and a Swagger JSON API descriptor at http://localhost:8090/brightstar/assets/swagger.json. The 
API description provides a quick overview of the APIs exposed by BrightstarDB and allow you try them
out from the browser interface. The JSON descriptor can be used by a variety of tools to generate
client code for a number of different programming languages - however the use of these tools are outside
the scope of this documentation - instead please refer to the `Swagger`_ site.

.. note::

	Swagger takes a fairly opinionated stance on what an HTTP API should look like, this unfortunately
	means that it is not possible to document all of the API endpoints that are exposed by BrightstarDB
	as some (in particular the standard SPARQL query, update and graph protocol endpoints) cannot be
	completely documented in Swagger.


.. _Supported_Media_Types:

Supported Media Types
=====================

The following media types are supported for operations that return RDF for a single RDF graph.

========== =============
Format     Media Type(s)
========== =============
RDF/XML    application/rdf+xml, application/xml
N-Triples  text/ntriples, text/ntriples+turtle, application/rdf-triples, application/x-ntriples
Turtle     application/x-turtle, application/turtle
N3         text/rdf+n3
RDF JSON   text/json, application/rdf+json
========== =============

The following export formats are supported for operations that return a single graph or multiple graphs. These formats preserve the 
source graph URIs:

========== =============
Format     Media Type(s)
========== =============
N-Quads    text/n-quads
TriG       application/x-trig
TriX       application/trix
========== =============


Stores Resource
===============

The **Stores Resource** is at ``http://localhost:8090/brightstar/``. It represents the list of stores
available on the BrightstarDB server.

GET
---

A GET operation will return a list of the stores available on the server. The response is a JSON object
with a single "stores" property whose value is a list of the names of the stores available on the server
as an array of strings::

	{
		"stores": [ "store1", "store2", "store3" ]
	}
	
POST
----

A POST operation creates a new store on the server. The body of the POST must be a JSON object that matches the following
pattern::

	{
		"StoreName": "string",
		"PersistenceType": 0
	}
	
The StoreName property is the name of the store to create. The new store name must not match the name of any existing store on the server.

The PersistenceType property defines the type of store persistence to use. 0 to create an Append-Only store; 1 to create a Rewritable store.
For more information about persistence types please refer to :ref:`Store_Persistence_Types`.

A 200 response indicates that the store has been created successfully. A 409 (Conflict) response indicates that the store name provided conflicts
with the name of an existing store - try changing the store name and then retry the operation. A 400 (Bad Request) response indicates a problem
with one of the parameters - check that the PersistenceType property is in the allowed range (0, 1).



Store Resource
==============

The **Store Resource** is at ``http://localhost:8090/brightstar/{storeName}``, where {storeName} is the name of the store to access.

GET
---

A GET operation returns an object that provides the store name and the sub-resources available for the store. Sub-resources are provided
as a URL relative to the base address of the BrightstarDB server. For example, the commits resource in this example is at http://localhost:8090/brightstar/mystore/commits::

	{
        "name": "mystore",
        "commits": "mystore/commits",
        "jobs": "mystore/jobs",
        "transactions": "mystore/transactions",
        "statistics": "mystore/statistics",
        "sparqlQuery": "mystore/sparql",
        "sparqlUpdate": "mystore/update"
	}
	
DELETE
------

A DELETE operation on this resource deletes the store from the server. This is a permanent deletion of the store and all of its data files, so use this operation
with caution!

A 200 response indicates that the store has been deleted.



SPARQL Query Endpoint
=====================

.. _SPARQL 1.1 Protocol: http://www.w3.org/TR/sparql11-protocol/

The **SPARQL Query Endpoint** for a store is at ``http://localhost:8090/brightstar/{storeName}/sparql``, where {storeName} is the name of the store to be queried.
This endpoint implements the query operations defined in the W3C `SPARQL 1.1 Protocol`_ specification. For more detail and examples please refer to that document.

GET
---

A GET operation can be used to execute a SPARQL query. The GET query supports the following query parameters.

	* **query**: The SPARQL query to be executed. This parameter is required.
	* **default-graph-uri**: The URI of a graph to be added to the default graph of the RDF Dataset to be queried. This parameter is optional and repeatable.
	* **named-graph-uri**: The URI of a graph to be added to the named graphs of the RDF Dataset to be queried. This parameter is optional and repeatable.
	
Use the Accept header to specify the format of the SPARQL results. The following media types are supported for SELECT or ASK queries:

======================== ===================================================================================
Format                   Media Type(s)
======================== ===================================================================================
SPARQL Results XML       application/sparql-results+xml, application/xml
SPARQL Results JSON      application/sparql-results+json, application/json
Tab-Separated Values     text/tab-separated-values
Comma-Separated Values   text/csv
======================== ===================================================================================

DESCRIBE or CONSTRUCT queries support the RDF media types described in :ref:`Supported_Media_Types`


POST
----

A POST operation can be used to execute a SPARQL query. There are two options for a POST:

	1. POST the URL encoded parameters (the same parameters as supported by GET) and set the content type of the request body to application/x-www-form-urlencoded
	
	2. POST the SPARQL query string in the body of the request, setting the content type of the request to application/sparql-query. You may optionally include the 
	   default-graph-uri and named-graph-uri parameters in the HTTP query string.
	   
Use the Accept header on the request to specify the results format to be returned (these are the same as for the GET operation described above)

.. note::
	The Swagger API documentation does not document all of these options as it is not possible to document two different POST options on a single Swagger API endpoint.
	

SPARQL Update Endpoint
======================

The **SPARQL Update Endpoint** for a store is at ``http://localhost:8090/brightstar/{storeName}/update``, where {storeName} is the name of the store to be updated.
This endpoint implements the update operations defined in the W3C `SPARQL 1.1 Protocol`_ specification. For more detail and examples please refer to that document.

POST
----

A SPARQL Update operation accepts the following parameters:

	* **update**: The SPARQL update expression to be executed.
	* **using-graph-uri**: The URI of a graph to add to the default graph of the RDF Dataset for the update operation.
	* **using-named-graph-uri**: The URI of a graph to add as a named graph in the RDF Dataset for the update operation.
	
A POST operation can be used to execute a SPARQL update. There are two options for a POST:

	1. POST the URL encoded parameters in the request body and set the content type of the request to application/x-www-form-urlencoded. The update parameter
	   is required and non-repeatable. The other parameters are optional and repeatable.
	
	2. POST the unencoded SPARQL update expression in the request body and set the content type of the request to application/sparql-update. The
	   using-graph-uri and using-named-graph-uri parameters may be optionally included in the HTTP query string.



Graphs Resource
===============

.. _SPARQL 1.1 Graph Store Protocol: http://www.w3.org/TR/2013/REC-sparql11-http-rdf-update-20130321/

The Graphs Resource for a store is at ``http://localhost:8090/brightstar/{storeName}/graphs``, where {storeName} is the name
of a store on the server. 

The Graphs Resource implements the W3C `SPARQL 1.1 Graph Store Protocol`_ using indirect graph identification.

.. note::
	Direct graph identification as described in the `SPARQL 1.1 Graph Store Protocol`_ is not currently supported.

GET
---

List Graphs
+++++++++++

A GET operation with no query parameters returns a list of the URIs of all graphs in the store. The response is a simple JSON object::

	{
		"graphs": [
			"string"
		]
	}

The ``graphs`` property is an array containing the the graph URIs.

.. note::
	A GET operation with no query parameters is a BrightstarDB-specific extension to the SPARQL 1.1 Graph Store Protocol.
	
Get Default Graph Content
+++++++++++++++++++++++++

A GET operation with a ``default`` query parameter retrieves the content of the default graph in the store. The value of the 
query parameter is ignored and it can be specified without any value (e.g. http://localhost:8090/brightstar/mystore/graphs?default).
The Accept header should be used to specify the desired format of the response. 
The supported media types are described in the section :ref:`Supported_Media_Types`.

Get Named Graph Content
+++++++++++++++++++++++

A GET operation with a ``graph-uri`` query parameter retrieves the content of the graph identified by the query parameter. The value
of the ``graph-uri`` parameter must be the URI identifier of an RDF graph in the store. 
The Accept header should be used to specify the desired format of the response. 
The supported media types are described in the section :ref:`Supported_Media_Types`.

POST
----

A POST operation can be used to import RDF into a graph. The body of the POST must be the RDF data to be imported. The Content header must
specify the format of the RDF data in the body. This operation supports any of the *graph* formats defined in :ref:`Supported_Media_Types`.
The HTTP query string must include exactly one of the following parameters:

	* **default** - the data should be imported into the default graph of the store. This parameter does not require any value.
	* **graph-uri** - specifies the URI of the graph that the data is to be imported into.
	
A 200 response indicates that the data was imported successfully.

A 400 response indicates a problem with the query parameters provided in the HTTP string.

A 406 response indicates an error parsing the RDF data in the body of the request.

PUT
---

A PUT operation can be used to import RDF into a graph, completely replacing the existing graph content. 
The body of the PUT must be the RDF data to be imported. 
The Content header must specify the format of the RDF data in the body.  
The RDF formats supported are defined in :ref:`Supported_Media_Types`.
The HTTP query string must include exactly one of the following parameters:

	* **default** - the data should be imported into the default graph of the store. This parameter does not require any value.
	* **graph-uri** - specifies the URI of the graph that the data is to be imported into.
	
A 200 response indicates that the data was imported successfully.

A 400 response indicates a problem with the query parameters provided in the HTTP string.

A 406 response indicates an error parsing the RDF data in the body of the request.

DELETE
------

A DELETE operation can be used to remove a graph from the store or in the case of the default graph, empty the graph.
The HTTP query string must include exactly one of the following parameters:

	* **default** - the operation should delete all content from the default graph. This parameter doe not require any value
	* **graph-uri** - specifies the URI of the graph that is to be deleted from the store.
	
A 200 response indicates that the data was imported successfully.

A 400 response indicates a problem with the query parameters provided in the HTTP string.



.. _Job_List_Resource:

Job List Resource
=================

The Job List Resource for a store is at ``http://localhost:8090/brightstar/{storeName}/jobs``, where {storeName} is the name of 
a specific store on the server.

GET
---

A GET operation retrieves a list of the recently queued jobs for the store. The resource returns a list of job information objects
as an array::

	[
		{
			"jobId": "string",
			"label": "string",
			"jobStatus": "StatusCode",
			"statusMessage": "string",
			"storeName": "string"
			"exceptionInfo": {
				"type": "string",
				"message": "string",
				"stackTrace": "string",
				"helpLink": "string",
				"data": {},
				"innerException": {}
			},
			"queuedTime": "date/time",
			"startTime": "date/time",
			"endTime": "date/time"
		}
	]

The job information object includes the following properties:

	* **jobId** - the GUID identifier for the job.
	* **label** - an optional user-provided label for the job.
	* **jobStatus** - the current processing status of the job. Values are: 
		* ``Pending`` - the job is queued awaiting its turn for processing.
		* ``Started`` - the job is being processed.
		* ``CompletedOk`` - the job completed successfully.
		* ``TransactionError`` - the job failed.
		* ``Unknown`` - the job is in an unknown state.
	* **statusMessage** - contains the most recent processing message logged for the job.
	* **storeName** - the name of the store on which the job operates.
	* **exceptionInfo** - contains detailed error information when job processing fails. The value of this property is an object with the following properties:
		* **type** - The name of the type of exception that caused the job processing to fail.
		* **message** - The string message from the exception.
		* **stackTrace** - The exception stack trace as a string.
		* **helpLink** - A link to more help about the exception if available.
		* **data** - Additional exception data.
		* **innerException** - The inner exception that this exception object wraps. If present, it has the same properties as this object (including possibly
		  having a nested innerException).
	* **queuedTime** - the date/time when the job was initially queued for processing.
	* **startTime** - the date/time when processing of the job started.
	* **endTime** - the date/time when processing of the job finished.
	
POST
----

A POST operation can be used to queue a new job. The body of the POST must be a JSON object that describes the job parameters. The properties required depend on the type of job being created.

A 400 (Bad Request) status code in the response indicates an error in processing the request. Check that the parameters are correct and that all required parameters are present.

A 200 (OK) status code in the response indicates that the job has been queued. The response body will contain a job information object with the same properties as described for the GET operation above.
After a job has been successfully queued, it can be monitored to completion by polling the :ref:`Job_Resource`

Consolidate 
+++++++++++

Compact this store by truncating its history leaving only the current store contents.

::

	{
		jobType: "consolidate"
	}
	
Create Snapshot
+++++++++++++++

Creates a new store as a snapshot of this store.

::

	{
		"jobType": "createsnapshot",
		"jobParameters": {
			"TargetStoreName": "string",
			"PersistenceType": "string",
			"CommitId": "string"
		}
	}

where:

	* **TargetStoreName** - the name of the store to create from the snapshot.
	* **PersistenceType** - the type of persistence model to use for the target store. Allowed values are ``AppendOnly`` or ``Rewrite``.
	* **CommitId** - the unique identifier of the commit point of the source store to create the snapshot from. This parameter is optional - if not specified, the most recent commit point is used.
	
Export
++++++

Export the content of a store or a single graph in a store as RDF. The exported file will be written to the import folder of the BrightstarDB server.

::

	{
		"jobType": "export",
		"jobParameters": {
			"FileName": "string",
			"Format": "string",
			"GraphUri": "string"
		}
	}
	
where:

	* **FileName** - the name of the file to be written by the export process.
	* **Format** - The MIME type of the output format to be used by the export. This parameter is optional and defaults to application/n-quads.
	* **GraphUri** - The URI of the graph to be exported. If not specified, all of the graphs in the store will be exported.
	
The media types supported by export are described in the section :ref:`Supported_Media_Types`.

Import
++++++

Triggers an import of data from a file contained in the import directory of the BrightstarDB server.

::

	{
		"jobType": "import",
		"jobParameters": {
			"FileName": "string",
			"DefaultGraphUri": "string"
		}
	}
	
where:

	* **FileName** - the name of the file to be imported. A file with this name must exist in the import directory of the store.
	* **DefaultGraphUri** - Provides a default target graph for the data if the data does not itself specify a target graph.
	  This parameter is optional and if omitted defaults to the BrightstarDB default graph URI.
	  
Repeat Transaction
++++++++++++++++++

Repeats a previous job.

::

	{
		"jobType": "repeattransaction",
		"jobParameters": {
			"JobId": "GUID"
		}
	}
	
where:

	* **JobId** - the GUID identifier of the job to be repeated.
	
SPARQL Update
+++++++++++++

Applies a SPARQL Update operation.

::

	{
		"jobType": "sparqlupdate"
		"jobParameters": {
			"UpdateExpression": "string"
		}
	}
	
where:

	* **UpdateExpression** - the SPARQL Update expression to process
	
Transaction
+++++++++++

Applies a transactional update to the store. For more information please refer to :ref:`RDF_Transactional_Update`.

::

	{
		"jobType": "transaction",
		"jobParameters": {
			"Preconditions": "string",
			"NonexistencePreconditions": "string",
			"Deletes": "string",
			"Inserts": "string",
			"DefaultGraphUri": "string"
		}
	}
	
where:

	* **Preconditions** - Triples or Quads that must exist in the store before the transaction is applied. The string must be in N-Triples or N-Quads syntax. This parameter is optional.
	* **NonexistencePreconditions** - Triples or Quads that must not exist in the store before the transaction is applied. The string must be in N-Triples or N-Quads syntax. This parameter is optional.
	* **Deletes** - Triples or Quads to be removed from the store. The string must be in N-Triples or N-Quads syntax. This parameter is optional.
	* **Inserts** - Triples or Quads to add to the store. The string must be in N-Triples or N-Quads syntax. This parameter is optional.
	* **DefaultGraphUri** - The default graph for the transaction. This is used to convert triples to quads for both testing preconditions and for insert/delete. 
	  This parameter is optional. If not specified, it defaults to the BrightstarDB default graph uri.

.. note::
	The ``Preconditions`` and ``NonexistencePreconditions`` and ``Deletes`` parameters allow the use of the special IRI <http://www.brightstardb.com/.well-known/model/wildcard> as a wildcard match for 
	any value in that position in the triple/quad.
	
Update Statistics
+++++++++++++++++

Updates the statistics for the store.

::

	{
		"jobType": "updatestats"
	}

	
.. _Job_Resource:

Job Resource
============

The Job Resource for a specific job can be found at ``http://localhost:8090/brightstar/{storeName}/jobs/{jobId}`` where {storeName} is the name of the store and {jobId} is the GUID identifier of the job.

GET
---

A GET operation returns a JSON object that describes the current state of the job. The content of the response is a single job information object with the same properties as described in the :ref:`Job_List_Resource` above.

A 404 (Not Found) response indicates that no job with the specified GUID identifier could be found queued for the specified store. 

.. note::
	Job information is not persistent in BrightstarDB. When a BrightstarDB server is restarted the job queue and information about recently completed jobs are lost. 
	Any job that had not been completed when the server was restarted will need to be resubmitted.


Commit Points Resource
======================

The **Commit Points Resource** for a store is at ``http://localhost:8090/brightstar/{storeName}/commits``, where {storeName} is the name of a specific store on the server.

GET
---

A GET operation returns a list of the commit points for the store, optionally filtered. This operation accepts the following parameters:

	* **timestamp**: A date/time. Filters the results to return the single commit point that was current at the specified date/time.
	* **earliest**: A date/time. Filters the results to include only commit points that were created on or after the specified date/time.
	* **latest**: A date/time. Filters the results to include only commit points that were created on or before the specified date/time.
	* **skip**: Specifies the starting offset when paging results
	* **take**: Specifies the number of items to return when paging results.
	
Date/Time values should be provided in the W3C date/time format of ``YYYY-MM-DDThh:mm:ss.sTZD`` where:

	* YYYY = four-digit year
	* MM   = two-digit month (01=January, etc.)
	* DD   = two-digit day of month (01 through 31)
	* hh   = two digits of hour (00 through 23) (am/pm NOT allowed)
	* mm   = two digits of minute (00 through 59)
	* ss   = two digits of second (00 through 59)
	* s    = one or more digits representing a decimal fraction of a second
	* TZD  = time zone designator (Z or +hh:mm or -hh:mm)

The resource returns an array of objects, each of which describes a single commit point::

	[
		{
			"id": 108462,
			"storeName": "doctagstore",
			"commitTime": "2015-05-19T14:03:49.5637536+01:00",
			"jobId": "7188998a-0751-49ee-a772-7f7865bf8985"
		},
		{
			"id": 6,
			"storeName": "doctagstore",
			"commitTime": "2015-05-19T14:01:14.1064105+01:00",
			"jobId": "00000000-0000-0000-0000-000000000000"
		}
	]

The properties provided for each commit point are:

	* id: The unique commit point identifier
	* storeName: The name of the store that the commit point applies to
	* commitTime: The date/time that the commit point was created.
	* jobId: The GUID identifier of the job that resulted in the commit point being created. This may be 
	  the empty GUID ("00000000-0000-0000-0000-000000000000") for commit points that are not the result of
	  running a job (e.g. the initial commit point made when the store is first created).

.. note::
	When multiple commit points are returned they are always in order of most-recent to least-recent commit point.

POST
----

A POST operation reverts the store to a previous commit point. This operation requires the POST body to contain a single JSON
object that describes the commit point to revert to::

	{
		"id": 6,
		"storeName": "doctagstore",
		"commitTime": "2015-05-19T14:01:14.1064105+01:00",
		"jobId": "00000000-0000-0000-0000-000000000000"
	}
	
.. note::
	Only the ``id`` property is required, the other properties can all be omitted.
	
A 200 response indicates that the store was successfully reverted to the specified commit point. A 400 response (Bad Request)
indicates either that the POST body did not contain an object with an ``id`` property on it or that the commit point with
the specified ID could not be found.


.. _Statistics_List_Resource:

Statistics List Resource
========================

The **Statistics List Resource** for a store is at ``http://localhost:8090/brightstar/{storeName}/statistics``. This resource provides
access to current and historical statistics for the store.

GET
---

The GET operation can be used to retrieve current or historical statistics for the store, optionally filtering by a date/time range.
The GET operation supports the following query parameters:

	* **earliest** - Filters the results to include only statistics for commit points created on or after the specified date/time. This parameter is optional and defaults to DateTime.MinValue.
	* **latest** - Filters the results to include only statistics for commit points created on or before the specified date/time. This parameter is optional and defaults to DateTime.MaxValue.
	* **skip** - The number of statistics records to skip over when paging results. This parameter is optional and defaults to 0.
	* **take** - The maximum number of statistics records to return when paging results. This parameter is optional and defaults to 10.
	
The resource returns an array of objects each of which is a single statistics record::

	[
		{
			"commitId": "string",
			"commitTimestamp": "date/time",
			"predicateTripleCounts": {
			},
			"totalTripleCount": number
		}
	]
	
The properties for each statistics record are:

	* commitId: The unique identifier of the commit point that the statistics apply to.
	* commitTimestamp: The date/time that the commit point was created.
	* totalTripleCount: The total number of triples in the store.
	* predicateTripleCounts: A JSON object. The properties of this object are the URI identifiers of each distinct predicate in the store, and the value is the number of triples in the store that use that predicate.
	
Latest Statistics Resource
==========================

The **Latest Statistics Resource** for a store is at ``http://localhost:8090/brightstar/{storeName}/statistics/latest``. This resource 
provides access to the most recently updated statistics for a store.

GET
---

The GET operation can be used to retrieve the most recent statistics for the store.

The resource returns a single JSON statistics record object::

	{
		"commitId": "string",
		"commitTimestamp": "date/time",
		"predicateTripleCounts": {
		},
		"totalTripleCount": number
	}

The properties of this object are the same as described for the :ref:`Statistics_List_Resource` above.