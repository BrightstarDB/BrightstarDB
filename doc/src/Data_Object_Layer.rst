.. _Data_Object_Layer:

******************
 Data Object Layer
******************


.. _SPARQL 1.1: http://www.w3.org/TR/sparql11-query/
.. _SPARQL XML Query Results Format: http://www.w3.org/TR/rdf-sparql-XMLres/


The Data Object Layer is a simple generic object wrapper for the underlying RDF data in any 
BrightstarDB store.

Data Objects are lightweight wrappers around sets of RDF triples in the underlying 
BrightstarDB store. They allow the developer to interact with the RDF data without requiring 
all information to be sent in N-Triple format.

For more information about the RDF layer of BrightstarDB, please read the :ref:`RDF Client API 
<RDF_Client_API>` section.


Creating a Data Object Context
==============================

The ``IDataObjectContext`` interface provides the methods for accessing BrightstarDB stores through the
Data Object Layer. You can use this interface to list the available stores, to open existing stores
and to create or delete stores. The following example shows how to create a new context using a 
connection string::

  var context = BrightstarService.GetDataObjectContext("Type=http;endpoint=http://localhost:8090/brightstar;");

The connection string defines the type of service you are connecting to. For more information about connection strings, 
please read the :ref:`"Connection Strings" topic <Connection_Strings>`

Using the IDataObjectContext
============================

Once you have an ``IDataObjectContext``, a new store can be creating using the ``CreateStore`` method::

  IDataObjectStore myStore = context.CreateStore("MyStore");
  
``CreateStore`` also accepts a number of optional parameters which are described in later sections.

Deleting a store is also straight forward - you just pass in the name of the store to be deleted::

  context.DeleteStore("MyStore");

To check if a store with a particular name already exists, use the ``DoesStoreExist()`` method::

  // Create MyStore if it doesn't already exist
  if (!context.DoesStoreExist("MyStore")) {
    context.CreateStore("MyStore");
  }
  
To open an existing store, use the ``OpenStore()`` method. This method will throw an exception
if the named store does not exist, so it is a good idea to test for this first::

  IDataObjectStore myStore;
  if (context.DoesStoreExist("MyStore")) {
    myStore = context.OpenStore("MyStore");
  }

Working With Data Objects
=========================

Data Objects can be created using the ``MakeDataObject()`` method on the IDataObjectStore::

  var fred = store.MakeDataObject("http://example.org/people/fred");

The objects can be created by passing in a well formed URI as the identity, if no identity is 
given then one is automatically generated for it and can be accessed via its ``Identity`` property. 
A data object can be retrieved from the store using its URI identifier::

  var fred = store.GetDataObject("http://example.org/people/fred");

If BrightstarDB does not hold any information about a given URI, then a data object is created 
for it and passed back. When the developer adds properties to it and saves it, the identity 
will be automatically added to BrightstarDB.

.. note::

  ``GetDataObject()`` will never return a null object. The data object consists of all the 
  information that is held in BrightstarDB for a particular identity.

To set the value of a single property, use the ``SetProperty()`` method. The method
requires an IDataObject instance that defines the type of the property being added,
so this needs to be created first.::

  var name = store.MakeDataObject("http://xmlns.com/foaf/0.1/name");
  fred.SetProperty(name, "Fred Evans");
  
There is also a short-hand version that takes care of creating the IDataObject for the type,
so the following is equivalent to the previous two-line example::

  fred.SetProperty("http://xmlns.com/foaf/0.1/name", "Fred Evans");

Calling ``SetProperty()`` a second time will overwrite the previous value of the property::

  fred.SetProperty("http://xmlns.com/foaf/0.1/name", "Fred Q. Evans");

If you want to add multiple properties of the same type use the ``AddProperty()`` method instead of ``SetProperty()``::

  var mbox = store.MakeDataObject("http://xmlns.com/foaf/0.1/mbox");
  fred.AddProperty(mbox, "fred@example.com");
  fred.AddProperty(mbox, "fred.evans@example.com");
  
A property value can either be a literal primitive type (supported C# primitive types are
string, bool, DateTime, Date, double, int, float, long, byte, decimal, short,
ubyte, ushort, uint, ulong, char and byte[]), or another IDataObject instance::

  var alice = store.MakeDataObject("http://example.org/people/alice");
  var knows = store.MakeDataObject("http://xmlns.com/foaf/0.1/knows");
  fred.AddProperty(knows, alice);

There is also a short-hand function for setting the RDF type property for a data object::

  var person = store.MakeDataObject("http://xmlns.com/foaf/0.1/Person");
  fred.SetType(person);

A property can be removed from a data object using the ``RemoveProperty()`` method::

  fred.RemoveProperty(mbox, "fred@example.com");
  
``RemoveProperty()`` will only remove a property that matches exactly by type and value (and language 
code if specified). Alternatively to remove all properties of a given type, use the 
``RemovePropertiesOfType()`` method::

  fred.RemovePropertiesOfType(mbox);

All of these methods for adding/remove properties and setting a type return the data object itself,
allowing the calls to be chained::

  fred.SetType(person)
      .SetProperty(name, "Fred Q. Evans")
      .AddProperty(mbox, "fred@example.org")
      .AddProperty(knows, alice);
	  
Adding and removing properties and changing the type simply adds and removes triples from the set of 
locally managed triples for the data object. You can access the RDF data that an object has at any time 
by using the following code::

  var triples = ((DataObject)fred).Triples;

A data object can be deleted using the ``Delete()`` method on the data object itself::

  var fred = store.GetDataObject("http://example.org/people/fred");
  fred.Delete();

This will remove all triples describing that data object from the store when changes are saved.

Updates such as new properties, new objects and deletions are all tracked by the IDataObjectStore locally
and are only applied to the BrightstarDB store when you call the ``SaveChanges()`` method on the store.
``SaveChanges()`` saves your changes in a single transaction, so either all updates will be applied
to the store or the transaction will fail and none of the updates will be applied.

Namespace Mappings
==================

Namespace mappings are sets of simple string prefixes for URIs, enabling the developer to use 
identities that have been shortened to use the prefixes.

For example, the mapping::

  {"people", "http://example.org/people/"}

Means that the short string "people:fred" will be expanded to the full identity string "http://example.org/people/fred"

These mappings are passed through as a dictionary to the OpenStore() method on the context::

  _namespaceMappings = new Dictionary<string, string>()
                           {
                               {"people", "http://example.org/people/"},
                               {"skills", "http://example.org/skills/"},
                               {"schema", "http://example.org/schema/"}
                           };
  store = context.OpenStore(storeName, _namespaceMappings);

.. note::

  It is best practise to set up a static dictionary within your class or configuration


Querying data using SPARQL
==========================

BrightstarDB supports `SPARQL 1.1`_ for querying the data in the store. These queries can be 
executed via the Data Object store using the ``ExecuteSparql()`` method. 

The SparqlResult returned has the results of the SPARQL query in the ResultDocument property 
which is an XML document formatted according to the `SPARQL XML Query Results Format`_. The
BrightstarDB libraries provide some helpful extension methods for accessing the contents of
a SPARQL XML results document::

  var query = "SELECT ?skill WHERE { " +
              "<http://example.org/people/fred> <http://example.org/schemas/person/skill> ?skill " +
              "}";
  var sparqlResult = store.ExecuteSparql(query);
  foreach (var sparqlResultRow in sparqlResult.ResultDocument.SparqlResultRows())
  {
      var val = sparqlResultRow.GetColumnValue("skill");
      Console.WriteLine("Skill is " + val);
  }

Binding SPARQL Results To Data Objects
======================================

When a SPARQL query has been written to return a single variable binding, it can be passed to the 
``BindDataObjectsWithSparql()`` method. This executes the SPARQL query, and then binds each URI in 
the results to a data object, and passes back the enumeration of these instances::

  var skillsQuery = "SELECT ?skill WHERE {?skill a <http://example.org/schemas/skill>}";
  var allSkills = store.BindDataObjectsWithSparql(skillsQuery).ToList();
  foreach (var s in allSkills)
  {
      Console.WriteLine("Skill is " + s.Identity);
  }

.. _optimistic_locking_in_dol:

Optimistic Locking in the Data Object Layer
===========================================

The Data Object Layer provides a basic level of optimistic locking support using the 
conditional update support provided by the RDF Client API and a special version property that 
gets assigned to data objects. Optimistic locking is enabled in one of two ways. The
first option is to enable optimistic locking in the connection string used to create the 
``IDataObjectContext``::

    var context = BrightstarService.GetDataObjectContext(
                      "type=http;endpoint=http://localhost:8090/brightstar;optimisticLocking=true");

The other option is to enable optimistic locking in the ``OpenStore()`` or ``CreateStore()`` method used to 
retrieve the IDataObjectStore instance from the IDataObjectContext::
 
    var store = context.OpenStore("MyStore", optimisticLockingEnabled:true);

.. note::
  The optimisticLockingEnabled parameter of ``OpenStore()`` and ``CreateStore()`` is optional.
  If it is omitted, then the setting in the connection string for the IDataObjectContext is used.
  If it is specified, it always overrides the setting in the connection string.
  
With optimistic locking enabled, the Data Object Layer checks for the presence of a special 
version property on every object it retrieves (the property predicate URI is 
``http://www.brightstardb.com/.well-known/model/version``). If this property is present, its value 
defines the current version number of the property. If the property is not present, the object 
is recorded as being currently unversioned. On save, the Data Object Layer uses the current 
version number of all versioned data objects as the set of preconditions for the update, if 
any of these objects have had their version number property modified on the server, the 
precondition will fail and the update will not be applied. Also as part of the save, the 
Data Object Layer updates the version number of all versioned data objects and creates a new 
version number for all unversioned data objects.

When an concurrent modification is detected, this is notified to your code by a 
``TransactionPreconditionsFailedException`` being raised. In your code you should catch this exception and 
handle the error. The ``IDataObjectStore`` interface provides a ``Refresh()`` method that implements
two common approaches to handling this status. The ``Refresh()`` method takes two parameters:
a data object instance and a ``RefreshMode`` parameter that specifies how the object
is to be updated. ``RefreshMode.StoreWins`` overwrites any local modifications made
to the object with the updated values held on the server. ``RefreshMode.ClientWins``
works the other way around, keeping the local changes and updating the version number
for the locally tracked object so that the next time ``SaveChanges()`` is attempted
the local changes will overwrite those held on the server. To find which objects
need refreshing, the ``IDataObjectStore`` provides the ``TrackedObjects`` property
that returns an enumerator over all the objects currently tracked by the store. Each
IDataObject instance provides an ``IsModified`` property that is set to true if
the store has some local changes for that object.


Graph Targeting in the Data Object API
======================================

You can use the Data Object API to update a specific named graph in the BrightstarDB store.
Each time you open a store you can specify the following optional parameters:

  * ``updateGraph`` : The identifier of the graph that new statements will be added to. Defaults to the BrightstarDB default graph (``http://www.brightstardb.com/.well-known/model/defaultgraph``)
  * ``defaultDataSet`` : The identifier of the graphs that statements will be retrieved from. Defaults to all graphs in the store.
  * ``versionGraph`` : The identifier of the graph that contains version information for optimistic locking. Defaults to the same graph as ``updateGraph``.
  
These are passed as additional optional parameters to the ``IDataObjectContext.OpenStore()`` method.

To create a store that reads properties from the default graph and adds properties to a specific graph (e.g. for recording the results of inferences), use the following::

    // Set storeName, prefixes and inferredGraphUri here
    var store = context.OpenStore(storeName, prefixes, updateGraph:inferredGraphUri,
                                  defaultDataSet: new[] {Constants.DefaultGraphUri},
								  versionGraph:Constants.DefaultGraphUri);

.. note::
	Note that you need to be careful when using optimistic locking to ensure that you are consistent about which graph manages
	the version information. We recommend that you either use the BrightstarDB default graph (as shown in the example above)
	or use another named graph separate from the graphs that store the rest of the data (and define a constant for that
	graph URI).
	
To create a store that reads only the inferred properties use code like this::

    // Set storeName, prefixes and inferredGraphUri here
    var store = context.OpenStore(storeName, prefixes, updateGraph:inferredGraphUri,
                                  defaultDataSet: new[] {inferredGraphUri},
								  versionGraph:Constants.DefaultGraphUri);

When creating a new store using the ``IDataObjectContext.CreateStore()`` method the ``updateGraph`` and ``versionGraph`` options can be specified, but
the ``defaultDataSet`` parameter is not available as a new store will not have any graphs. In this case the store returned will read from and write to
the graph specified by the ``updateGraph`` parameter.

.. _default_data_set:

Default Data Set
----------------

The ``defaultDataSet`` parameter can be used to list the URIs of the graphs that should
be queried by the ``IDataObjectStore`` returned by the method. In SPARQL parlance, 
this set of graphs is known as the *dataset*. If an update graph or
version graph is specified then those graph URIs will also be added to the data set. 
In the special case that ``updateGraph``, ``versionGraph`` and ``defaultDataSet``
are all NULL (or not specified in the call to ``OpenStore``), the default data set
will be set to cover all of the graphs in the BrightstarDB store.

Graph Targeting and Deletions
-----------------------------

The ``RemoveProperty()`` and ``SetProperty()`` methods can both cause triples to be deleted from the store. In this case the triples
are removed from both the graph specified by ``updateGraph`` and all the graphs specified in the ``defaultDataSet`` (or all 
graphs in the store if the ``defaultDataSet`` is not specified or is set to null).

Similarly if you call the ``Delete`` method on a DataObject, the triples that have the DataObject as subject or object will 
be deleted from the ``updateGraph`` and all graphs in the ``defaultDataSet``.