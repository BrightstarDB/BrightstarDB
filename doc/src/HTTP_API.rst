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
	
Stores Resource
===============

The **Stores Resource** is at http://localhost:8090/brightstar/. It represents the list of stores
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

The **Store Resource** is at http://localhost:8090/brightstar/{storeName}, where {storeName} is the name of the store to access.

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

Commit Points Resource
======================

The **Commit Points Resource** is at http://localhost:8090/brightstar/{storeName}/commits, where {storeName} is the name of a specific store on the server.

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
