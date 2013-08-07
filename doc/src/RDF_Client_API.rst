.. _RDF_Client_API:

***************
 RDF Client API
***************


.. _SPARQL 1.1: http://www.w3.org/TR/sparql11-query/
.. _SPARQL 1.1 Update: http://www.w3.org/TR/sparql11-update/
.. _SPARQL XML Query Results Format: http://www.w3.org/TR/rdf-sparql-XMLres/

The RDF Client API provides a simple set of methods for creating and deleting stores, 
executing transactions and running queries. It should be used when the application needs to 
deal directly with RDF data. An RDF Client can connect to an embedded store or remotely to a 
running BrightstarDB instance.


Creating a client
=================

The BrightstarService class provides a number of static methods that can be used to create a 
new client. The most general one takes a connection string as a parameter and returns a client 
object. The client implements the ``BrightstarDB.IBrightstarService`` interface. 

The following example shows how to create a new service client using a connection string::

  var client = BrightstarService.GetClient(
                    "Type=http;endpoint=http://localhost:8090/brightstar;");

For more information about connection strings, please read the :ref:`"Connection Strings" 
topic <Connection_Strings>`


Creating a Store
================

A new store can be creating using the following code::

  string storeName = "Store_" + Guid.NewGuid();
  client.CreateStore(storeName);


Deleting a Store
================

Deleting a store is also straight forward::

  client.DeleteStore(storeName);


Adding data
===========

Data is added to the store by sending the data to add in N-Triples format. Each triple must be 
on a single line with no line breaks, a good way to do this is to use a ``StringBuilder`` and then 
using ``AppendLine()`` for each triple::

  var data = new StringBuilder();
  data.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/name> \\"BrightstarDB\\" .");
  data.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/category> <http://www.brightstardb.com/categories/nosql> .");
  data.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/category> <http://www.brightstardb.com/categories/.net> .");
  data.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/category> <http://www.brightstardb.com/categories/rdf> .");


The ``ExecuteTransaction()`` method is used to insert the N-Triples data into the store::

  client.ExecuteTransaction(storeName,null, null, data.ToString());


Deleting data
=============

Deletion is done by defining a pattern that should matching the triples to be deleted. The 
following example deletes all the category data about BrightstarDB, again we use the 
``StringBuilder`` to create the delete pattern.

::

  var deletePatternsData = new StringBuilder();
  deletePatternsData.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/category> <http://www.brightstardb.com/.well-known/model/wildcard> .");


The identifier ``http://www.brightstardb.com/.well-known/model/wildcard`` is a wildcard 
match for any value, so the above example deletes all triples that have a subject of
``http://www.brightstardb.com/products/brightstar`` and a predicate of
``http://www.brightstardb.com/schemas/product/category``.

The ``ExecuteTransaction()`` method is used to delete the data from the store::

  client.ExecuteTransaction(storeName, null, deletePatternsData.ToString(), null);

.. note::
  The string ``http://www.brightstardb.com/.well-known/model/wildcard`` is also defined
  as the constant string ``BrightstarDB.Constants.WildcardUri``.
  
Conditional Updates
===================

The execution of a transaction can be made conditional on certain triples existing in the 
store. The following example updates the ``productCode`` property of a resource only if its 
current value is ``640``.

::

  var preconditions = new StringBuilder();
  preconditions.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/productCode> "640"^^<http://www.w3.org/2001/XMLSchema#integer> .");
  var deletes = new StringBuilder();
  deletes.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/productCode> "640"^^<http://www.w3.org/2001/XMLSchema#integer> .");
  var inserts = new StringBuilder();
  inserts.AppendLine("<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/productCode> "973"^^<http://www.w3.org/2001/XMLSchema#integer> .");
  client.ExecuteTransaction(storeName, preconditions.ToString(), deletes.ToString(), inserts.ToString());


When a transaction contains condition triples, every triple specified in the preconditions 
must exist in the store before the transaction is applied. If one or more triples specified in 
the preconditions are not matched, a ``BrightstarClientException`` will be raised.


Data Types
==========

In the code above we used simple triples to add a string literal object to a subject, such as::

  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/name> "BrightstarDB"

Other data types can be specified for the object of a triple by using the ^^ syntax::

  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/productCode> "640"^^<http://www.w3.org/2001/XMLSchema#integer> .
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/releaseDate> "2011-11-11 12:00"^^<http://www.w3.org/2001/XMLSchema#dateTime> .
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/cost> "0.00"^^<http://www.w3.org/2001/XMLSchema#decimal> .


Updating Graphs
===============

The ``ExecuteTransaction()`` method on the ``IBrightstarService`` interface 
accepts a parameter that defines the default graph URI. When this parameters is 
specified, all precondition triples are tested against that graph; all delete 
triple patterns are applied to that graph; and all addition triples are added
to that graph::

  // This code update the graph http://example.org/graph1
  client.ExecuteTransaction(storeName, preconditions, deletePatterns, additions, "http://example.org/graph1");

In addition, the format that is parsed for preconditions, delete patterns and additions
allows you to mix N-Triples and N-Quads formats together. N-Quads are simply N-Triples
with a fourth URI identifier on the end that specifies the graph to be updated. When
an N-Quad is encountered, its graph URI overrides that passed into the ``ExecuteTransaction()``
method. For example, in the following code, one triple is added to the graph "http://example.org/graphs/alice"
and the other is added to the default graph (because no value is specified in the call 
to ``ExecuteTransaction()``::

    var txn1Adds = new StringBuilder();
    txn1Adds.AppendLine(
        @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .");
    txn1Adds.AppendLine(@"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .");
    var result = client.ExecuteTransaction(storeName, null, null, txn1Adds.ToString());

.. note::
  The wildcard URI is also supported for the graph URI in delete patterns, allowing you
  to delete matching patterns from all graphs in the BrightstarDB store.
  
Querying data using SPARQL
==========================

BrightstarDB supports `SPARQL 1.1`_ for querying the data in the store. A simple query on the 
N-Triples above that returns all categories that the subject called "Brightstar DB" is 
connected to would look like this::

  var query = "SELECT ?category WHERE { " +
          "<http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/category> ?category " +
          "}";

This string query can then be used by the ``ExecuteQuery()`` method to create an XDocument from 
the SPARQL results (See `SPARQL XML Query Results Format`_ for format of the XML document returned). 

::

  var result = XDocument.Load(client.ExecuteQuery(storeName, query));

BrightstarDB also supports several different formats for SPARQL results. The default format is XML, 
but you can also add a BrightstarDB.SparqlResultsFormat parameter to the ExecuteQuery method 
to control the format and encoding of the results set. For example::

  var jsonResult = client.ExecuteQuery(storeName, query, SparqlResultsFormat.Json);

By default results are returned using UTF-8 encoding; however you can override this default 
behaviour by using the ``WithEncoding()`` method on the ``SparqlResultsFormat`` class. The 
``WithEncoding()`` method takes a ``System.Text.Encoding`` instance and returns a ``SparqlResultsFormat``
instance that will ask for that specific encoding::

  var unicodeXmlResult = client.ExecuteQuery(
                           storeName, query, 
                           SparqlResultsFormat.Xml.WithEncoding(Encoding.Unicode));

Querying Graphs
===============

By default a SPARQL query will be executed against the default graph in the BrightstarDB store (that is,
the graph in the store whose identifier is ``http://www.brightstardb.com/.well-known/model/defaultgraph``). In 
SPARQL terms, this means that the default graph of the dataset consists of just the default graph in the store.
You can override this default behaviour by passing the identifier of one or more graphs to the 
``ExecuteQuery()`` method. There are two overrides of ``ExecuteQuery()`` that allow this. One accepts a single
graph identifier as a ``string`` parameter, the other accepts multiple graph identifiers as an 
``IEnumerable<string>`` parameter. The three different approaches are shown below::

  // Execute query using the store's default graph as the default graph
  var result = client.ExecuteQuery(storeName, query);
  
  // Execute query using the graph http://example.org/graphs/1 as 
  // the default graph
  var result = client.ExecuteQuery(storeName, query, 
                                   "http://example.org/graphs/1");
  
  // Execute query using the graphs http://example.org/graphs/1 and 
  // http://example.org/graphs/2 as the default graph
  var result = client.ExecuteQuery(storeName, query, 
                                   new string[] {
								     "http://example.org/graphs/1", 
									 "http://example.org/graphs/2"});

.. note::
  It is also possible to use the ``FROM`` and ``FROM NAMED`` keywords in the SPARQL query to define
  both the default graph and the named graphs used in your query.

Using extension methods
=======================

To make working with the resulting XDocument easier there exist a number of extensions 
methods. The method ``SparqlResultRows()`` returns an enumeration of ``XElement`` instances 
where each ``XElement`` represents a single result row in the SPARQL results.

The ``GetColumnValue()`` method takes the name of the SPARQL result column and returns the value as 
a string. There are also methods to test if the object is a literal, and to retrieve the data type 
and language code.

::

  foreach (var sparqlResultRow in result.SparqlResultRows())
  {
     var val = sparqlResultRow.GetColumnValue("category");
     Console.WriteLine("Category is " + val);
  }


Update data using SPARQL
========================

BrightstarDB supports `SPARQL 1.1 Update`_ for updating data in the store. An update operation 
is submitted to BrightstarDB as a job. By default the call to ``ExecuteUpdate()`` will block until 
the update job completes::

  IJobInfo jobInfo = _client.ExecuteUpdate(storeName, updateExpression);

In this case, the resulting ``IJobInfo`` object will describe the final state of the update job. 
However, you can also run the job asynchronously by passing false for the optional 
``waitForCompletion`` parameter::

  IJobInfo jobInfo = _client.ExecuteUpdate(storeName, updateExpression, false);

In this case the resulting ``IJobInfo`` object will describe the current state of the update job 
and you can use calls to ``GetJobInfo()`` to poll the job for its current status.


Data Imports
============

To support the loading of large data sets BrightstarDB provides an import function. Before 
invoking the import function via the client API the data to be imported should be copied into 
a folder called 'import'. The 'import' folder should be located in the folder containing the 
BrigtstarDB store data folders. After a default installation the stores folder is 
[INSTALLDIR]\\Data, thus the import folder should be [INSTALLDIR]\\Data\\import. For information 
about the RDF syntaxes that BrightstarDB supports for import, please refer to :ref:`Supported 
RDF Syntaxes <Supported_RDF_Syntaxes>`.


With the data copied into the folder the following client method can be called. The parameter 
is the name of the file that was copied into the import folder::

  client.StartImport("data.nt");


.. _Introduction_To_NTriples:


Introduction To N-Triples
=========================


.. _here: http://www.w3.org/TR/2013/NOTE-n-triples-20130409/
.. _the XML Schema specification: http://www.w3.org/TR/xmlschema-2/#built-in-primitive-datatypes


N-Triples is a consistent and simple way to encode RDF triples. N-Triples is a line based 
format. Each N-Triples line encodes one RDF triple. Each line consists of the subject (a URI), 
followed  by whitespace, the predicate (a URI), more whitespace, and then the object (a URI or 
literal) followed by a dot and a new line. The encoding of the literal may include a datatype 
or language code as well as the value. URIs are enclosed in '<' '>' brackets. The formal 
definition of the N-Triples format can be found `here`_.

The following are examples of N-Triples data::

  # simple literal property
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/name> "Brightstar DB" .


  # relationship between two resources
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/category> <http://www.brightstardb.com/categories/nosql> .


  # A property with an integer value
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/productCode> "640"^^<http://www.w3.org/2001/XMLSchema#integer> .

  # A property with a date/time value
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/releaseDate> "2011-11-11 12:00"^^<http://www.w3.org/2001/XMLSchema#dateTime> .
  
  # A property with a decimal value
  <http://www.brightstardb.com/products/brightstar> <http://www.brightstardb.com/schemas/product/cost> "0.00"^^<http://www.w3.org/2001/XMLSchema#decimal> .


**Allowed Data Types**


Data types are defined in terms of an identifier. Common data types use the XML Schema 
identifiers. The prefix of these is ``http://www.w3.org/2001/XMLSchema#``. The common primitive 
datatypes are defined in `the XML Schema specification`_.



.. _Introduction_To_SPARQL:


Introduction To SPARQL
======================


.. _SPARQL 1.1 Query Language: http://www.w3.org/TR/sparql11-query/
.. _Introduction to RDF Query with SPARQL Tutorial: http://www.w3.org/2004/Talks/17Dec-sparql/


BrightstarDB is a triple store that implements the RDF and SPARQL standards. SPARQL, 
pronounced "sparkle", is the query language developer by the W3C for querying RDF data. SPARQL 
primarily uses pattern matching as a query mechanism. A short example follows::

  PREFIX ont: <http://www.brightstardb.com/schemas/>
  SELECT ?name ?description WHERE {
    ?product a ont:Product .
    ?product ont:name ?name .
    ?product ont:description ?description .
  }


This query is asking for all the names and descriptions of all products in the store. 

SELECT is used to specify which bound variables should appear in the result set. The result of 
this query is a table with two columns, one called "name" and the other "description". 

The PREFIX notation is used so that the query itself is more readable. Full URIs can be used 
in the query. When included in the query directly URIs are enclosed by '<' and '>'.  

Variables are specified with the '?' character in front of the variable name. 

In the above example if a product did not have a description then it would not appear in the 
results even if it had a name. If the intended result was to retrieve all products with their 
name and the description if it existed then the OPTIONAL keyword can be used. 

::

  PREFIX ont: <http://www.brightstardb.com/schemas/>
  SELECT ?name ?description WHERE {
    ?product a ont:Product .
    ?product ont:name ?name .
      
    OPTIONAL {
      ?product ont:description ?description .
    }
  }


For more information on SPARQL 1.1 and more tutorials the following resources are worth reading.


  1. `SPARQL 1.1 Query Language`_

  #. `Introduction to RDF Query with SPARQL Tutorial`_



