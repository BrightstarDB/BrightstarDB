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


The following example shows how to create a new context using a connection string::

  var context = BrightstarService.GetDataObjectContext("Type=http;endpoint=http://localhost:8090/brightstar;");

For more information about connection strings, please read the :ref:`"Connection Strings" 
topic <Connection_Strings>`


Creating a Store
================

A new store can be creating using the following code::

  string storeName = "Store_" + Guid.NewGuid();
  context.CreateStore(storeName);


Deleting a Store
================

Deleting a store is also straight forward::

  context.DeleteStore(storeName);


Creating data objects
=====================


Data Objects can be created using the following code::

  var fred = store.MakeDataObject("http://example.org/people/fred");

The objects can be created by passing in a well formed URI as the identity, if no identity is 
given then one is automatically generated for it. 


Adding information to data objects
==================================

To add information about this object we can add properties to it.

To set the value of a single property, use the following code::

  var fullname = store.MakeDataObject("http://example.org/schemas/person/fullName");
  fred.SetProperty(fullname, "Fred Evans");

Calling ``SetProperty()`` a second time will overwrite the previous value of the property.

To add multiple properties of the same type use the code below::

  var skill = store.MakeDataObject("http://example.org/schemas/person/skill");
  fred.AddProperty(skill, csharp);
  fred.AddProperty(skill, html);
  fred.AddProperty(skill, css);

Retrieving Data Objects
=======================

A data object can be retrieved from the store using the following code::

  var fred = store.GetDataObject("http://example.org/people/fred");

If BrightstarDB does not hold any information about a given URI, then a data object is created 
for it and passed back. When the developer adds properties to it and saves it, the identity 
will be automatically added to BrightstarDB.

.. note::

  ``GetDataObject()`` will never return a null object. The data object consists of all the 
  information that is held in BrightstarDB for a particular identity.

We can check the RDF data that an object has at any time by using the following code:::

  var triples = ((DataObject)fred).Triples;


Deleting Data Objects
=====================

A data object can be deleted using the following code::

  var fred = store.GetDataObject("http://example.org/people/fred");
  fred.Delete();

This removes all triples describing that data object from the store.


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
a SPARQL XML results document

::

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


.. _Data_Object_Layer_Sample_Program:


Sample Program
==============

.. _SPARQL 1.1: http://www.w3.org/TR/sparql11-query/

.. note::

  The source code for this example can be found in 
  [INSTALLDIR]\\Samples\\DataObjectLayer\\DataObjectLayerSamples.sln

The sample project is a simple console application that runs through some of the basic usage 
for BrightstarDB's Data Object Layer.


Creating the context
--------------------

A context is created using a connection string that specifies usage of the HTTP server::

  var context = BrightstarService.GetDataObjectContext(
                       @"Type=http;endpoint=http://localhost:8090/brightstar;");

                       
Creating the store
------------------

A store is created with a unique name::

  string storeName = "DataObjectLayerSample_" + Guid.NewGuid();
  var store = context.CreateStore(storeName);

Namespace Mappings
------------------

In order to use simpler identities when we are creating and retrieving data objects, we set up 
a dictionary of namespace mappings to use when we connect to the store::

  _namespaceMappings = new Dictionary<string, string>()
      {
          {"people", "http://example.org/people/"},
          {"skills", "http://example.org/skills/"},
          {"schema", "http://example.org/schema/"}
  };

  store = context.OpenStore(storeName, _namespaceMappings);


Optimistic Locking
------------------

To enable support for optimistic locking, we must pass a boolean flag to the ``OpenStore()`` or 
``CreateStore()`` method::

  store = context.OpenStore(storeName, _namespaceMappings, true); // Open store with optimistic locking enabled


Skills
------

We create a data object to use as the type of data object for skills, and then create a number 
of skill data objects::

  var skillType = store.MakeDataObject("schema:skill");

  var csharp = store.MakeDataObject("skills:csharp");
  csharp.SetType(skillType);
  var html = store.MakeDataObject("skills:html");
  html.SetType(skillType);
  var css = store.MakeDataObject("skills:css");
  css.SetType(skillType);
  var javascript = store.MakeDataObject("skills:javascript");
  javascript.SetType(skillType);


People
------

We follow the same process to create some people data objects::

  var personType = store.MakeDataObject("schema:person");

  var fred = store.MakeDataObject("people:fred");
  fred.SetType(personType);
  var william = store.MakeDataObject("people:william");
  william.SetType(personType);


Properties
----------

We create 2 data objects to use as the types for some properties that the people data objects 
will hold::

  var fullname = store.MakeDataObject("schema:person/fullName");
  var skill = store.MakeDataObject("schema:person/skill");

We then set the single name property on the people, and add skills

.. note::

  For multiple properties we must use the ``AddProperty()`` method rather than ``SetProperty()`` which 
  would overwrite any previous value

::

  fred.SetProperty(fullname, "Fred Evans");
  fred.AddProperty(skill, csharp);
  fred.AddProperty(skill, html);
  fred.AddProperty(skill, css);

  william.SetProperty(fullname, "William Turner");
  william.AddProperty(skill, html);
  william.AddProperty(skill, css);
  william.AddProperty(skill, javascript);

The data type of literal properties are set automatically when a property value is added or set::

  var employeeNumber = store.MakeDataObject("schema:person/employeeNumber");
  var dateOfBirth = store.MakeDataObject("schema:person/dateOfBirth");
  var salary = store.MakeDataObject("schema:person/salary");

  fred = store.GetDataObject("people:fred");
  fred.SetProperty(employeeNumber, 123);
  fred.SetProperty(dateOfBirth, DateTime.Now.AddYears(-30));
  fred.SetProperty(salary, 18000.00);


Save Changes
------------

Now that we have created the objects we require, we save the changes to the BrightstarDB store::

  store.SaveChanges();

  
Querying the data
-----------------

In this sample, we use a SPARQL query to return all of the skills of the data object for 'fred'. 
We can then loop through the ResultDocument of the SparqlResult returned to see the identities 
of each of those skills.

::

  const string getPersonSkillsQuery = 
        "SELECT ?skill WHERE { " +
        "  <http://example.org/people/fred> <http://example.org/schemas/person/skill> ?skill " +
        "}";
  var sparqlResult = store.ExecuteSparql(getPersonSkillsQuery);


Binding Data Objects With SPARQL
--------------------------------

The Data Object Store has a very useful method called ``BindDataObjectsWithSparql()``, which takes 
a SPARQL query that is written to return the URI identities of data object. The method then 
returns the data objects for each of the URIs contained in the results.

In the sample we pass in a query to return URIs of any objects with the skill type::

  const string skillsQuery = "SELECT ?skill WHERE {?skill a <http://example.org/schemas/skill>}";
  var allSkills = store.BindDataObjectsWithSparql(skillsQuery).ToList();


Deleting Objects
----------------

To delete data objects we simply call the Delete() method of that object and save the changes 
to the store::

  foreach (var s in allSkills)
  {
      s.Delete();
  }
  store.SaveChanges();


.. _Optimistic_Locking_in_DOL:


Optimistic Locking in the Data Object Layer
===========================================


The Data Object Layer provides a basic level of optimistic locking support using the 
conditional update support provided by the RDF Client API and a special version property that 
gets assigned to data objects. To enable optimistic locking support it is necessary to enable 
locking when the ``IDataObjectStore`` instance is retrieved from the context by either the 
``OpenStore()`` or ``CreateStore()`` method (see :ref:`Sample Program <Data_Object_Layer_Sample_Program>` 
for an example).

With optimistic locking enabled, the Data Object Layer checks for the presence of a special 
version property on every object it retrieves (the property predicate URI is 
``http://www.brightstardb.com/.well-known/model/version``). If this property is present, its value 
defines the current version number of the property. If the property is not present, the object 
is recorded as being currently unversioned. On save, the DataObjectLayer uses the current 
version number of all versioned data objects as the set of preconditions for the update, if 
any of these objects have had their version number property modified on the server, the 
precondition will fail and the update will not be applied. Also as part of the save, the 
DataObjectLayer updates the version number of all versioned data objects and creates a new 
version number for all unversioned data objects.

When an concurrent modification is detected, this is notified to your code by a 
``BrightstarClientException`` being raised. In your code you should catch this exception and 
handle the error, typically either by abandoning updates (and notifying the user) or 
re-retrieving the modified objects and attempting the update again.


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

	..note::
	Note that you need to be careful when using optimistic locking to ensure that you are consistent about which graph manages
	the version information. We recommend that you either use the BrightstarDB default graph (as shown in the example above)
	or use another named graph seperate from the graphs that store the rest of the data (and define a constant for that
	graph URI).
	
To create a store that reads only the inferred properties use code like this::

    // Set storeName, prefixes and inferredGraphUri here
    var store = context.OpenStore(storeName, prefixes, updateGraph:inferredGraphUri,
                                  defaultDataSet: new[] {inferredGraphUri},
								  versionGraph:Constants.DefaultGraphUri);

When creating a new store using the ``IDataObjectContext.CreateStore()`` method the ``updateGraph`` and ``versionGraph`` options can be specified, but
the ``defaultDataSet`` parameter is not available as a new store will not have any graphs. In this case the store returned will read from and write to
the graph specified by the ``updateGraph`` parameter.

Graph Targeting and Deletions
-----------------------------

The ``RemoveProperty`` and ``SetProperty`` methods can both cause triples to be deleted from the store. In this case the triples
are removed from both the graph specified by ``updateGraph`` and all the graphs specified in the ``defaultDataSet`` (or all 
graphs in the store if the ``defaultDataSet`` is not specified or is set to null).

Similarly if you call the ``Delete`` method on a DataObject, the triples that have the DataObject as subject or object will 
be deleted from the ``updateGraph`` and all graphs in the ``defaultDataSet``.