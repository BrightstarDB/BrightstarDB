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

------------------
Store Requirements
------------------

To use this functionality in BrightstarDB, the store must support query using
SPARQL 1.1 Query and update using SPARQL 1.1 Update. The store must also have
a connector available for it in DotNetRDF. A number of commercial and
non-commercial stores are supported by DotNetRDF (for a list please refer
to `the DotNetRDF documentation <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Storage/Providers>`_).

.. note:
    If the store you want to connect to supports SPARQL 1.1 Protocol for
    both query and update, then you can configure instead a direct connection to the 
    query and update endpoints.
    
-------------------------
Configuration
-------------------------

The connection to your store must be configured by providing an RDF file
that contains the configuration information. We use the DotNetRDF 
`Configuration API <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration%20API>`_
and load the configuration from a file whose path you specify in the
connection string (refer to :ref:`Connection_Strings` for more details).

In the DotNetRDF configuration file you need to either configure 
`SPARQL Query <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration/Query%20Processors>`_
and `SPARQL Update <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration/Update%20Processors>`_
Processors; a `Triple Store <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration/Triple%20Stores>`_;
or a `Storage Provider <https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Configuration/Storage%20Providers>`_
Each of these configuration resources in the RDF file must have its own URI which
you can then provide in the connection string using the *Query*, *Update* and *Store*
properties (see :ref:`Connection_Strings` for more details).

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