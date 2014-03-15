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
    dotNetRdf      Connects to another (non-BrightstarDB)  Configuration, StorageServer
                   store using DotNetRDF connectors        
    sparql         Connects to another (non-BrightstarDB)  Query, Update
                   store using SPARQL protocols
    ============== ======================================= ================================

**StoresDirectory** : value is a file system path to the directory containing all BrightstarDB 
data. Only valid for use with **Type** set to *embedded*.

**Endpoint** : a URI that points to the service endpoint for the specified remote service. 
Only valid for connections with **Type** set to *rest*

**StoreName** : The name of a specific store to connect to. This property is only required
when creating an EntityFramework connection or when creating a connection using the 
dotNetRdf connection type.

**Configuration** : The path to the RDF file that contains the configuration for the
DotNetRDF connector. 
Only valid for connections with **Type** set to *dotNetRdf*.
For more information please refer to the section :ref:Other_Stores

**StorageServer** : The URI of the resource in the DotNetRDF configuration file that
configures the DotNetRDF storage server to be used for the connection.
Only valid for connections with **Type** set to *dotNetRdf*.
For more information please refer to the section :ref:Other_Stores

**Query** : The URI the SPARQL query endpoint to connect to. 
Only valid for connections with **Type** set to *sparql*.

**Update**: The URI of the SPARQL update endpoint to connect to. 
Only valid for connections with **Type** set to *sparql*.
If this option is used in a connection string, then the **Query** property must also be provided.

**OptimisticLocking**: Specifies if optimistic locking should be enabled for
the connection by default. Note that this setting can be overridden in code,
allowing developers full control over whether or not optimistic locking
is used. This option is only used by the :ref:`Data_Object_Layer` and 
:ref:`Entity_Framework` and is currently not supported on connections
of type *dotNetRDF*

**UserName**: Specifies the user name to use for authenticating with the server.
A connection string with this property must also have a **Password** property
for authentication to take place.

**Password**: Specifies the password to use for authenticating with the server.
A connection string with this property must also have a **UserName** property
for authentication to take place.

.. note::
    You should never store credentials in a connection string as plain text.
    Instead your application should store the base connection string without
    the UserName and Password properties. It should then prompt the user to enter their credentials
    just before it creates the BrightstarDB client and append the UserName and Password
    properties to the base connection string.
    
The following are examples of connection strings. Property value pairs are separated by ';' 
and property names are case insensitive.::

  // A connection to a BrightstarDB server running on localhost.
  // The connection is configured with a default store to use for the Entity Framework
  "type=rest;endpoint=http://localhost:8090/brightstar;storename=test"

  // An embedded connection to the store named "test" in the directory c:\Brightstar
  "type=embedded;storesdirectory=c:\brightstar;storename=test"

  // An embedded connection to the stores contained in the directory c:\Brightstar
  "Type=embedded;StoresDirectory=c:\Brightstar"
  
  // A connection to one or more store providers configured in a DotNetRDF configuration file
  "Type=dotnetrdf;configuration=c:\brightstar\dotNetRDFConfiguration.ttl"
  
  // A connection to a storage server such as a Sesame server configured in a DotNetRDF configuration file
  "Type=dotnetrdf;configuration=c:\brightstar\sesameConfiguration.ttl;storageServer=http://example.org/configurations/#sesameServer"
  // NOTE: It is also possible to use relative URIs (resolved against the base URI of the configuration graph) e.g.
  "Type=dotnetrdf;configuration=c:\brightstar\sesameConfiguration.ttl;storageServer=#sesameServer"
  
  // A read-write connection to a server with SPARQL query and SPARQL update endpoints
  "Type=sparql;query=http://example.org/sparql;update=http://example.org/sparql-update"
  
  // A read-only connection to a server with only a SPARQL query endpoint
  "Type=sparql;query=http://example.org/sparql"
