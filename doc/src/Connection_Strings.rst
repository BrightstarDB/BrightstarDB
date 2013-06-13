.. _Connection_Strings:

*******************
 Connection Strings
*******************

BrightstarDB makes use of connection strings for accessing both embedded and remote 
BrightstarDB instances. The following section describes the different connection string 
properties.

**Type** : allowed values **embedded**, **http**, **tcp**, and **namedpipe**. This indicates 
the type of connection to create.

**StoresDirectory** : value is a file system path to the directory containing all BrightstarDB 
data. Only valid for use with **Type** set to **embedded**.

**Endpoint** : a URI that points to the service endpoint for the specified remote service. 
Valid for **http**, **tcp**, and **namedpipe**

**StoreName** : The name of a specific store to connect to. 


The following are examples of connection strings. Property value pairs are separated by ';' 
and property names are case insensitive.::

  "type=http;endpoint=http://localhost:8090/brightstar;storename=test"

  "type=tcp;endpoint=net.tcp://localhost:8095/brightstar;storename=test"

  "type=namedpipe;endpoint=net.pipe://localhost/brightstar;storename=test"

  "type=embedded;storesdirectory=c:\\brightstar;storename=test"

