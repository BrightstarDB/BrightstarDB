.. _Connection_Strings:

*******************
 Connection Strings
*******************

BrightstarDB makes use of connection strings for accessing both embedded and remote 
BrightstarDB instances. The following section describes the different connection string 
properties.

**Type** : This property specifies the type of connection to create. Allowed values are:

    ============== ======================================= ================================
    Type           Description                             Other Properties For Connection
    ============== ======================================= ================================
    embedded       Uses the embedded BrightstarDB server   StoresDirectory
                   to directly open stores from the local
                   file system.
    rest           Uses HTTP(S) to connect to a            Endpoint
                   BrightstarDB service.
    dotNetRdf      Connects to another (non-BrightstarDB)  Configuration, StoreName,
                   store using DotNetRDF connectors        Store or Query **and** Update
    ============== ======================================= ================================

**StoresDirectory** : value is a file system path to the directory containing all BrightstarDB 
data. Only valid for use with **Type** set to *embedded*.

**Endpoint** : a URI that points to the service endpoint for the specified remote service. 
Only valid for connections with **Type** set to *rest*

**StoreName** : The name of a specific store to connect to. This property is only required
when creating an EntityFramework connection or when creating a connection using the 
dotNetRdf connection type.

**Configuration** : The path to the RDF file that contains the configuration for the
DotNetRDF connector. For more information please refer to the section :ref:Other_Stores

**Store** : The URI identifier of the node in the DotNetRDF configuration file that
configures the store to connect to. The connection will then attempt to establish
the most efficient SPARQL Query and SPARQL Update connections to the configured store.
If this option is used in a connection string then any **Query** or **Update** options
in the connection string will be ignored.

**Query** : The URI identifier of the node in the DotNetRDF configuration file
that configures the SPARQL query endpoint to connect to. If this option is used 
in a connection string, then the **Update** property must also be provided.

**Update**: The URI identifier of the node in the DotNetRDF configuration file
that configures the SPARQL update endpoint to connect to. If this option is used
in a connection string, then the **Query** property must also be provided.

**OptimisticLocking**: Specifies if optimistic locking should be enabled for
the connection by default. Note that this setting can be overridden in code,
allowing developers full control over whether or not optimistic locking
is used. This option is only used by the :ref:`Data_Object_Layer` and 
:ref:`Entity_Framework` and is currently not supported on connections
of type *dotNetRDF*

The following are examples of connection strings. Property value pairs are separated by ';' 
and property names are case insensitive.::

  "type=rest;endpoint=http://localhost:8090/brightstar;storename=test"

  "type=embedded;storesdirectory=c:\brightstar;storename=test"

  "Type=embedded;StoresDirectory=c:\Brightstar"
  
  "Type=dotnetrdf;configuration=c:\brightstar\dotNetRDFConfiguration.ttl;store=http://example.org/configuration#mystore"
  
  "Type=dotnetrdf;configuration=c:\brightstar\dotNetRDFConfiguration.ttl;query=http://example.org/configuration#sparqlQuery;update=http://example.org/configuration#sparqlUpdate"
