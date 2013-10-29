.. _Connection_Strings:

*******************
 Connection Strings
*******************

BrightstarDB makes use of connection strings for accessing both embedded and remote 
BrightstarDB instances. The following section describes the different connection string 
properties.

**Type** : This property specifies the type of connection to create. Allowed values are 
**embedded** or **rest**. **embedded** indicates that BrightstarDB is to be used in
embedded mode, connecting directly to store files in the directory specified by the
**StoresDirectory** property. **rest** indicates that the REST API client is to be
used to connect to a BrightstarDB server over HTTP(S).

**StoresDirectory** : value is a file system path to the directory containing all BrightstarDB 
data. Only valid for use with **Type** set to **embedded**.

**Endpoint** : a URI that points to the service endpoint for the specified remote service. 
Only valid for connections with **Type** set to **rest**.

**StoreName** : The name of a specific store to connect to. This property is only required
when creating an EntityFramework connection. 

The following are examples of connection strings. Property value pairs are separated by ';' 
and property names are case insensitive.::

  "type=rest;endpoint=http://localhost:8090/brightstar;storename=test"

  "type=embedded;storesdirectory=c:\\brightstar;storename=test"

  "Type=embedded;StoresDirectory=c:\\Brightstar"
  