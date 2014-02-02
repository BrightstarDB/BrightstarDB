.. _Other_Stores:

===========================
Connecting to Other Stores
===========================

.. warning:
    This functionality is new in 1.5 and should be considered experimental.
    
BrightstarDB provides a set of high-level APIs for working with RDF data that we
think are useful regardless of what underlying store you used to manage the RDF
data. For that reason we have done some work to enable the use of the 
:ref:`Data_Object_Layer`, :ref:`Dynamic_API` and :ref:`Entity_Framework` with
stores other than BrightstarDB.

If your store provides SPARQL 1.1 query and update endpoints that implement
the SPARQL 1.1 protocol specification you can use a SPARQL connection string. 
For other stores you can use DotNetRDF's configuration syntax to configure
a connection to the store.

------------------
Store Requirements
------------------

To use this functionality in BrightstarDB, the store must support query using
SPARQL 1.1 Query and update using SPARQL 1.1 Update. The store must also have
a connector available for it in DotNetRDF, or support SPARQL 1.1 Protocol.

A number of commercial and non-commercial stores are supported by DotNetRDF (for a list please refer
to `the DotNetRDF documentation <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Storage/Providers>`_).

   
------------------------------------
Configuration and Connection Strings
------------------------------------

The connection to your store must be configured by providing an RDF file
that contains the configuration information. We use the DotNetRDF 
`Configuration API <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration%20API>`_
and load the configuration from a file whose path you specify in the
connection string (refer to :ref:`Connection_Strings` for more details).

In the DotNetRDF configuration file you need to either configure one or more `Storage Providers <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration/Storage%20Providers>`_ or
a **single** `Storage Server (at the time of writing the configuration for these has not been
documented in the DotNetRDF project).

Using Storage Providers
-----------------------

This approach provides a flexible way to make one or more RDF data stores accessible via
the BrightstarDB APIs. You must create a DotNetRDF configuration file that contains
the configuration for each of the stores you want to access. Each store must be configured
as a `DotNetRDF StorageProvider <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Storage/Providers>`_.

Each configuration you create should have a URI identifier assigned to it (so don't use a 
blank node for the configuration in the configuration file). The full URI of the configuration resource
is used as the store name in calls to DoesStoreExist() or OpenStore(). For a shorter store name it is also 
possible to use a relative URIs - these will be resolved against the base URI of the configuration graph.

The connection string you use for BrightstarDB is then just::

    type=dotnetrdf;configuration={configuration_file_path}

where ``configuration_file_path`` is the full path to the DotNetRDF configuration file.


Using A StorageServer
---------------------

DotNetRDF supports connections to Sesame and to Stardog servers that manage multiple stores. These
connections must be configured as a DotNetRDF StorageServer. In this case, the list of stores is
managed by the storage server so you don't need to write a separate configuration for each
individual store on the server.

The configuration you create must have a URI identifier assigned to it. The full URI of this 
configuration resource is used in the connection string.

The connection string you would use for BrightstarDB in this scenario follows this template::

    type=dotnetrdf;configuration={config_file_path};storageServer={config_uri}
    
where ``config_file_path`` is the full path to the DotNetRDF configuration file, and
``config_uri`` is the URI identifier of the configuration resource for the storage server.


Using SPARQL endpoints
----------------------

If the data store you want to connect to supports SPARQL 1.1 Query and Update and the SPARQL 1.1
Protocol specification, then you can create a connection that will use the SPARQL query and update
endpoints directly. The template for this type of connection string is::

    type=sparql;query={query_endpoint_uri};update={update_endpoint_uri}
    
where ``query_endpoint_uri`` is the URI of the SPARQL query endpoint for the server and
``update_endpoint_uri`` is the URI of the SPARQL update endpoint.

You can omit the ``update=`` part of the connection string, in which case the connection
will be a read-only one and calls to ``SaveChanges()`` will result in a runtime exception.

If credentials are required to access the server, these can be passed in using optional
``userName=`` and ``password=`` parameters::

    type=sparql;query=...;update=...;userName=joe;password=secret123


----------------------------------------
Differences to BrightstarDB Connections
----------------------------------------

We have tried to keep the differences between using BrightstarDB and using
another store through the high-level APIs to a minimum. However as there
are many differences between different store implementations, so there
are a few points of potential difference to be aware of:

    #. Default dataset and update graph.
        If not overridden in code, the default dataset for a BrightstarDB
        connection is all graphs in the store; for another store the default
        dataset is defined by the server.
        Similarly if not overridden in code, the default graph for
        updates on a BrightstarDB connection is the BrightstarDB default
        graph (a graph with the URI ``http://www.brightstardb.com/.well-known/model/defaultgraph``);
        for another store, the default graph for updates is defined by the server.
        
        To minimize confusion it is a good idea to always explicitly 
        specify the update graph and default data set when your code
        may connect to stores other than BrightstarDB and to ensure
        that the update graph is included in the default data set.
        
    #. Optimistic locking
        This is currently unsupported for connections to stores other
        than BrightstarDB as its implementation depends on 
        functionality not available in SPARQL 1.1 protocol.
        
    #. Transactional Updating
        This is highly dependent on the way in which the store's SPARQL
        update implementation works. The code will send a set of SPARQL
        update commands in a single request to the store. If the store
        does not implement the processing such that the multiple updates
        are handled in a single transaction, then it will be possible
        to end up with partially completed updates. It is worth checking
        with the documentation for your store / endpoint to see what
        transactional guarantees it makes for SPARQL Update requests.
        
--------------------------
Example Configurations
--------------------------

Connecting over SPARQL Protocol
===============================

DotNetRDF configuration file (dotNetRdf.config.ttl)::

    @prefix dnr: <http://www.dotnetrdf.org/configuration#> .
    @prefix : <http://example.org/configuration#> .
    
    :sparqlQuery a dnr:SparqlQueryEndpoint ;
        dnr:type "VDS.RDF.Query.SparqlRemoteEndpoint" ;
        dnr:queryEndpointUri <http://example.org/sparql> .
        
    :sparqlUpdate a dnr:SparqlUpdateEndpoint ;
        dnr:type "VDS.RDF.Update.SparqlRemoteUpdateEndpoint" ;
        dnr:updateEndpointUri <http://example.org/update> .

connection string::

    type=dotnetrdf;configuration=c:\path\to\dotNetRdf.config.ttl;query=http://example.org/configuration#sparqlQuery;update=http://example.org/configuration#sparqlUpdate;
    
Connecting to a Fuseki Server
=============================

DotNetRDF configuration file (dotNetRdf.config.ttl)::

    @prefix dnr: <http://www.dotnetrdf.org/configuration#>
    @prefix : <http://example.org/configuration#>
    
    :fuseki a dnr:StorageProvider ;
        dnr:type "VDS.RDF.Storage.FusekiConnector" ;
        dnr:server "http://fuseki.example.org/dataset/data" .
        
connection string::
    type=dotnetrdf;configuration=c:\path\to\dotNetRdf.config.ttl;store=http://example.org/configuration#fuseki

**TBD: More examples**