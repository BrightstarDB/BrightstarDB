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


.. _Developing_for_Windows_Phone_7:

*****************************
 Developing for Windows Phone
*****************************


For Windows Phone 7 and Windows Phone 8 (WP) developers, BrightstarDB provides a fast, 
schema-less persistent data store, that is easily managed as part of the isolated storage for 
an app. When running on a phone, all the key features of BrighstarDB are available, including 
the :ref:`Data Object Layer <Data_Object_Layer>` and the :ref:`Entity Framework 
<Entity_Framework>` as well as the :ref:`RDF Client API <RDF_Client_API>`. This section covers 
the main differences with the .NET 4.0 version of BrightstarDB and some important 
considerations when writing your WP7 app to use BrightstarDB. The SDK provides libraries that 
are compatible with Windows Phone 7.1 and Windows Phone 8, so all apps you develop with 
BrightstarDB will need to target that version of the Windows Phone OS.


Data Storage And Connection Strings
===================================


When running on WP, BrightstarDB writes its data using the IsolatedStorage APIs. This means 
that a BrightstarDB store opened within an application will be written into the 
IsolatedStorage for that application. This keeps the stores used by different applications 
separate from each other. An application can also use multiple stores, as long as each store 
has a unique store name. As the BrightstarDB server is not running on the phone, the only 
access to the store is by using the embedded connection type. A typical connection string used 
in a WP application is shown in the code snippet below:::

  var connectionString = "type=embedded;storesdirectory=brightstar;storename=MyAppStore";


SDK Libraries
=============

The BrightstarDB libraries for WP are all contained in [INSTALLDIR]\\SDK\\WP71. You need to add 
references to these libraries to your WP application project.


Development Considerations
==========================

For the most part, working with BrightstarDB on Windows Phone is the same as working with it 
on the full .NET Framework. However due to the platform and some .NET restrictions there are a 
few things that you need to keep in mind when building your application.

Store Shutdown
--------------

Because BrightstarDB uses separate threads to process updates to its stores, it is necessary 
for any WP app that uses BrightstarDB to cleanly shutdown the database when the application is 
not in use. The easiest way to achieve this is to add code to the Application_Deactivated and 
Application_Closing methods in the main application class as shown below. There is no 
corresponding global startup required as BrightstarDB will automatically start the required 
threads the first time you access a store.

::

  // Code to execute when the application is deactivated (sent to background)
  // This code will not execute when the application is closing
  private void Application_Deactivated(object sender, DeactivatedEventArgs e)
  {
      BrightstarService.Shutdown(true);
  }


  // Code to execute when the application is closing (eg, user hit Back)
  // This code will not execute when the application is deactivated
  private void Application_Closing(object sender, ClosingEventArgs e)
  {
      BrightstarService.Shutdown(true);
  }



EntityFramework Interfaces Must Be Public
-----------------------------------------

Due to differences between the .NET Framework and Silverlight, there are is one known 
limitation on the Entity Framework. All interfaces that are marked with the [Entity] attribute 
must be public interfaces when building a Windows Phone application. This is because 
Silverlight prevents reflection on internal classes / interfaces for reasons of code security.


.. _Deploying_a_Reference_Store:


Deploying a Reference Store
===========================

As well as using BrightstarDB to store user-modifiable data, you can also ship reference data 
with your application. A BrightstarDB reference store can be embedded as part of your 
application content and deployed to the Isolated Storage on the mobile device. Once deployed, 
the store can be queried and/or updated through your code as normal. The basic steps to 
deploying a store in a mobile application are as follows:

  1. Create the reference store

  #. Add the reference store files to your application and compile it

  #. Deploy the application to the device

  #. At runtime, copy the reference store files from the application directory to Isolated Storage

  #. Access the copied store from your code


Create The Reference Store
--------------------------

BrightstarDB stores are fully portable between the desktop and a mobile device through simple 
file copy. You can create and update a BrightstarDB database using a .NET application on a 
desktop workstation or a server and use the database files on a mobile device without the need 
for any conversion.

.. note::

  If the database you are deploying has been through a number of update transactions you may 
  want to consider creating a coalesced copy of the database for deployment purposes. 
  Coalescing the database will reduce the database size by copying only the current state of 
  the database and removing all the historical states of the data.


Add Database File To Your Application
-------------------------------------

Every BrightstarDB store exists in its own folder and contains at least the following files:

  - master.bs

  - data.bs

  - resources.bs

  - transactionheaders.bs

  - transactions.bs


For normal operation you only need to add the master.bs, resources.bs and data.bs files to 
your solution. The transactionheaders.bs and transactions.bs files are required only if your 
application will need to replay the transactions that built the database.

To add the reference database to your application

  1. With Visual Studio, create a project for the Windows Phone application that consumes the 
     reference store.

  #. From the Project menu of the application, select **Add Existing Item**.

  #. From the **Add Existing Item** menu, select the ``master.bs`` file for the BrightstarDB store 
     that you want to add, then click **Add**. This will add the local file to the project.

  #. In Solution Explorer, right-click the local file and set the file properties so that the 
     file is built as Content and always copied to the output directory (Copy always).

  #. Repeat steps 3 and 4 for the data.bs file and ``resources.bs`` file

  #. Optionally repeat steps 3 and 4 for ``transactionheaders.bs`` and ``transactions.bs``

.. note::

  It is good practice to put the BrightstarDB data files you are copying into a folder under 
  your project. If you want to deploy multiple reference databases, you will have to put the 
  files for each store in a separate folder to avoid name clashes. The folders you define in 
  your project will be mirrored in the installation directory when the application is deployed.


Deploy Application
------------------

Compile and deploy your application as normal. The store files you have copied will be 
available in the installation directory of the application (under the folders that you created 
in the project if applicable).


Copy Database Files To Isolated Storage
---------------------------------------

BrightstarDB on a mobile device will only access stores from a named directory in the 
application's Isolated Storage. It is therefore necessary when your application starts up to 
ensure that the data is copied or moved to Isolated Storage. Each BrightstarDB store you 
deploy must be in its own named directory, and those directories must in turn be in a named 
directory under the Isolated Storage root folder. These directory names are important as they 
form the values in the connection string you provide to BrightstarDB. The directory structure 
used by the sample application is shown below:

::

  IsolatedStorageFile Root
  |
  +-brightstar    <-- the storesDirectory value in the connection string, create a sub
    |                 create one sub-directory for each store you want to access
    |
    +-dining      <-- the storeName value in the connection string,
                      only the files for a single store should go in here

The precise way you choose to deploy or update the BrightstarDB store files is up to you, but 
the simplest method (as shown in the sample code) is to check for the presence of the store 
and if it is not there, copy the files from the application installation directory to Isolated 
Storage. The code to do this in the sample can be found in the ``App()`` constructor in the 
``App.xaml.cs`` file::

  if (!BrightstarDbDeploymentHelper.StoreExists("brightstar", "dining"))
  {
      BrightstarDbDeploymentHelper.CopyStore("data", "brightstar", "dining");
  }


The helper class can also be found in the sample project and has the following methods::

  public static class BrightstarDbDeploymentHelper
  {
      public static bool StoreExists(string storeDirectoryName, string storeName)
      {
          IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
          return iso.DirectoryExists(storeDirectoryName + "\\\\" + storeName) &&
                 iso.FileExists(storeDirectoryName + "\\\\" + storeName + "\\\\master.bs") &&
                 iso.FileExists(storeDirectoryName + "\\\\" + storeName + "\\\\resources.bs") &&
                 iso.FileExists(storeDirectoryName + "\\\\" + storeName + "\\\\data.bs");
      }


      public static void CopyStore(string resourceFolderPath, 
                                   string storeDirectoryName, 
                                   string storeName)
      {
          IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
          CopyStoreFile(iso, "data.bs", resourceFolderPath, storeDirectoryName, storeName);
          CopyStoreFile(iso, "master.bs", resourceFolderPath, storeDirectoryName, storeName);
          CopyStoreFile(iso, "resources.bs", resourceFolderPath, storeDirectoryName, storeName);
      }


      private static void CopyStoreFile(IsolatedStorageFile iso, string fileName, 
                                        string resourceFolderPath,
                                        string storeDirectoryName, string storeName)
      {
          if (!iso.DirectoryExists(storeDirectoryName))
          {
              iso.CreateDirectory(storeDirectoryName);
          }
          if (!iso.DirectoryExists(storeDirectoryName + "\\\\" + storeName))
          {
              iso.CreateDirectory(storeDirectoryName + "\\\\" + storeName);
          }


          // Create a stream for the file in the installation folder.
          var appResource =
              Application.GetResourceStream(
                new Uri(resourceFolderPath + "\\\\" + fileName, UriKind.Relative));
          if (appResource != null)
          {
              using (Stream input = appResource.Stream)
              {
                  // Create a stream for the new file in isolated storage.
                  using (
                      IsolatedStorageFileStream output =
                          iso.CreateFile(storeDirectoryName + "\\\\" + storeName + "\\\\" + fileName))
                  {
                      // Initialize the buffer.
                      var readBuffer = new byte[4096];
                      int bytesRead = -1;
                      // Copy the file from the installation folder to isolated storage. 
                      while ((bytesRead = input.Read(readBuffer, 0, readBuffer.Length)) > 0)
                      {
                          output.Write(readBuffer, 0, bytesRead);
                      }
                  }
              }
          } 
          else
          {
              // There is no application resource for this file, so create it as an empty file 
  
              iso.CreateFile(storeDirectoryName + "\\\\" + storeName + "\\\\" + fileName).Close();
          }
      }
  }


Access Reference Database Files
-------------------------------

Once deployed to Isolated Storage, the BrightstarDB store can be accessed as normal. You can 
use the RDF API, DataObjects API or EntityFramework to access the data depending on your 
application requirements. The connection string used to access the store is as follows::

  type=embedded;storesDirectory={path to directory containing store directories};storeName={name of store directory}

With our sample application, the store is contained in a directory named "dining", which is 
itself contained in a directory named "brightstar", so the full connection string is::

  type=embedded;storesDirectory=brightstar;storeName=dining

When the sample application runs, you should see a listing of top restaurants generated from a 
LINQ query against the EntityFramework.
