.. _Dynamic_API:

************
 Dynamic API
************

The Dynamic API is a thin layer on top of the data object layer. It is designed to further 
ease the use of .NET with RDF data and to provide a model for persisting data in systems that 
make use of the .NET dynamic keyword. The .NET dynamic keyword and dynamic runtime allow 
properties of objects to be set at runtime without any prior class definition.

The following example shows how dynamics work in general. Both 'Name' and 'Type' are resolved 
at runtime. In this case they are simply stored as properties in the ExpandoObject. 

::

  dynamic d = new ExpandoObject();
  d.Name = "Brightstar";
  d.Type = "Product";

Dynamic Context
===============

A dynamic context can be used to create dynamic objects whose state is persisted as RDF in 
BrightstarDB. To use the dynamic context a normal DataObjectContext must be created first. 
From the context a store can be created or opened. The store provides methods to create and 
fetch dynamic objects. 

::

  var dataObjectContext = BrightstarService.GetDataObjectContext();
  // create a dynamic context
  var dynaContext = new BrightstarDynamicContext(dataObjectContext);
  // create a new store
  var storeId = "DynamicSample" + Guid.NewGuid().ToString();
  var dynaStore = dynaContext.CreateStore(storeId);

Dynamic Object
==============

The dynamic object is a wrapper around the ``IDataObject``. When a dynamic property is set this is 
translated into an update to the ``IDataObject`` and in turn into RDF. By default the name of the 
property is appended to  the default namespace. By using namespace mappings any RDF vocabulary 
can be mapped. To use a namespace mapping, you must access / update a property whose name is
the namespace prefix followed by ``__`` (two underscore characters) followed by the suffix part
of the URI. For example ``object.foaf__name``. 

If the value of the property is set to be a list of values then multiple triples are created, one for each value.

::

  dynamic brightstar = dynaStore.MakeNewObject();
  brightstar.name = "BrightstarDB";

  // setting a list of values creates multiple properties on the object.
  brightstar.rdfs__label = new[] { "BrightstarDB", "NoSQL Database" };

  dynamic product = dynaStore.MakeNewObject();
  // objects are connected together in the same way
  brightstar.rdfs__type = product;


Saving Changes
==============

The data updated in a context is persisted when ``SaveChanges()`` is called on the store object.::

  dynaStore.SaveChanges();


Loading Data
============

After opening a store dynamic objects can be loaded via the ``GetDataObject()`` method or the 
``BindObjectsWithSparql()`` method.

::

  dynaStore = dynaContext.OpenStore(storeId);

  // Retrieve a single object by its identifier
  var object = dynaStore.GetDataObject(aUri);

  // Use a SPARQL query that returns a single variable to return a collection of dynamic objects
  var objects = dynaStore.BindObjectsWithSparql("select distinct ?dy where { ?dy ?p ?o }");


.. _Sample_Program:

Sample Program
==============

.. note::

  The source code for this example can be found in [INSTALLDIR]\\Samples\\Dynamic\\Dynamic.sln

::

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using BrightstarDB.Dynamic;
  using BrightstarDB.Client;


  namespace DynamicSamples
  {
      public class Program
      {
          /// <summary>
          /// Assumes BrightstarDB is running as a service and exposing the 
          /// default endpoint at http://localhost:8090/brightstar 
          /// </summary>
          /// <param name="args"></param>
          static void Main(string[] args)
          {
              // gets a new BrightstarDB DataObjectContext
              var dataObjectContext = BrightstarService.GetDataObjectContext();


              // create a dynamic context
              var dynaContext = new BrightstarDynamicContext(dataObjectContext);


              // open a new store
              var storeId = "DynamicSample" + Guid.NewGuid().ToString();
              var dynaStore = dynaContext.CreateStore(storeId);


              // create some dynamic objects. 
              dynamic brightstar = dynaStore.MakeNewObject();
              dynamic product = dynaStore.MakeNewObject();


              // set some properties
              brightstar.name = "BrightstarDB";
              product.rdfs__label = "Product";
              var id = brightstar.Identity;


              // use namespace mapping (RDF and RDFS are defined by default)
              // Assigning a list creates repeated RDF properties.
              brightstar.rdfs__label = new[] { "BrightstarDB", "NoSQL Database" };


              // objects are connected together in the same way
              brightstar.rdfs__type = product;


              dynaStore.SaveChanges();


              // open store and read some data
              dynaStore = dynaContext.OpenStore(storeId);
              brightstar = dynaStore.GetDataObject(brightstar.Identity);


              // property values are ALWAYS collections.
              var name = brightstar.name.FirstOrDefault();
              Console.WriteLine("Name = " + name);


              // property can also be accessed by index
              var nameByIndex = brightstar.name[0];
              Console.WriteLine("Name = " + nameByIndex);


              // they can be enumerated without a cast
              foreach (var l in brightstar.rdfs__label)
              {
                  Console.WriteLine("Label = " + l);
              }


              // object relationships are navigated in the same way
              var p = brightstar.rdfs__type.FirstOrDefault();
              Console.WriteLine(p.rdfs__label.FirstOrDefault());


              // dynamic objects can also be loaded via sparql
              dynaStore = dynaContext.OpenStore(storeId);
              var objects = dynaStore.BindObjectsWithSparql(
                                  "select distinct ?dy where { ?dy ?p ?o }");
              foreach (var obj in objects)
              {
                  Console.WriteLine(obj.rdfs__label[0]);
              }
              
              Console.ReadLine();
          }
      }
  }