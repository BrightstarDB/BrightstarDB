.. _Whats_New:

##########
 Whats New
##########

.. _System.ComponentModel.INotifyPropertyChanged: http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged%28v=vs.100%29.aspx
.. _System.Collections.Specialized.INotifyCollectionChanged: http://msdn.microsoft.com/en-us/library/system.collections.specialized.inotifycollectionchanged%28v=vs.100%29.aspx


This section gives a brief outline of what is new / changed in each official release of BrightstarDB. Where there are breaking changes, that require either data migration or code changes in client code, these are marked with **BREAKING**. New features are marked with NEW and fixes for issues are marked with FIX

*************************
 BrightstarDB 1.3 Release
*************************

  - NEW: First official open source release. 
         All documentation and examples updated to remove references to commercial licensing and license protection code.
		 Build updated to remove dependencies on third-party commercial tools

  - NEW: The ExecuteTransaction method now supports specifying a target graph.
  
  - NEW: The ExecuteQuery Method now supports specifying the default graph of the SPARQL dataset.
  
  - FIX: Disabled profiling code that was eating up significant amounts of memory during long running imports. Profiling can now be enabled globally by calling Logging.EnableProfiling(true);
  
*************************
 BrightstarDB 1.2 Release
*************************

  - NEW: Collection properties on entities now support compiling LINQ queries to SPARQL. This can be achieved by using the AsQueryable() method on the collection. e.g. myEntity.RelatedItems.AsQueryable()....// LINQ query follows

  - NEW: Interface and property annotations are now copied from the entity interface to the entity class by the code generator. This applies only to annotations that are not in the BrightstarDB namespace. For interface annotations, only those annotations that are also applicable to classes can be copied through to the generated class. For more information please refer to the section :ref:`Annotations <Annotations_Guide>` in the :ref:`Entity Framework <Entity_Framework>` API documentation.

  - NEW: BrightstarDB now supports XML, JSON, CSV and TSV (tab-separated values) as SPARQL reults formats. You can specify the format you want using the optional SparqlResultsFormat parameter on the ExecuteQuery methods. The SPARQL service samples has been updated to select the appropriate results format depending on the requested content type.

  - NEW: BrightstarDB generated entity classes now implement the `System.ComponentModel.INotifyPropertyChanged`_ interface and fire a notification event any time a property with a single value is modified. All collections exposed by the generated classes now implement the `System.Collections.Specialized.INotifyCollectionChanged`_ interface and fire a notification when an item is added to or removed from the collection or when the collection is reset. For more information please refer to the section :ref:`INotifyPropertyChanged and INotifyCollectionChanged Support <Local_Change_Tracking>`.

  
*************************
 BrightstarDB 1.1 Release
*************************

  - FIX: Entity Framework code generation now supports multiple levels of inheritance on interfaces.

  - NEW: Polaris now supports editing the server connection details

  - NEW: Installer now adds the BrightstarDB item templates for EntityContext and Entity to VS2012 Professional and above. VS2010 and VS2010 Express are also still supported. Please note that VS2012 Express editions are not supported at this time.

  
*************************
 BrightstarDB 1.0 Release
*************************

  - NEW: Added support for executing SPARQL Update commands to :ref:`Polaris <Using_Polaris>`

  - FIX: A few minor bug fixes

  
***********************************
 BrightstarDB 1.0 Release Candidate
***********************************

This release introduces a BREAKING file format change. If you are upgrading from a previous version of BrightstarDB and you wish to retain the data in a store, you should export all data from that store before performing the upgrade and then after the upgrade delete and recreate the store and import the exported data.

  - BREAKING: Store file format is significantly different from previous versions - please read the warning information above carefully BEFORE upgrading.

  - NEW: Store now supports a file format that reduces index file growth rate


*************************************
 BrightstarDB 1.0 Public Beta Refresh
*************************************

This release introduces some BREAKING API changes (but data store format is unaffected, so only your code needs to be modified). If you are upgrading from a previous release, please read the following carefully - in particular note the BREAKING changes that are introduced in this release.

  - BREAKING: All API namespaces have now changed from NetworkedPlanet.Brightstar.* to BrightstarDB.*. Custom code will require modification and recompilation

  - BREAKING: The only DLL now required for the .NET 4.0 SDK is BrightstarDB.dll.

  - BREAKING: Entity sets exposed by the generated Entity Framework context class are now typed by the implementation class rather than the entity interface class. Code written on top of the Entity Framework will need to be refactored to use the interface rather than the concrete class or to cast the return values to the concrete class where necessary. Note, this reverses the change made in the Public Beta release. 

  - BREAKING: The default installation directory and by extension the default data store directory has changed from C:\Program Files (x86)\NetworkedPlanet\Brightstar to C:\Program Files (x86)\BrightstarDB. If using the default data directory path, after upgrading you should manually copy the contents of C:\Program Files(x86)\NetworkedPlanet\Brightstar\Data to C:\Program Files (x86)\BrightstarDB\Data.

  - NEW: Added support for binding BrightstarDB data objects to .NET dynamic objects. For more information please refer to the section :ref:`Dynamic API <Dynamic_API>`.

  - NEW: Added an optional SPARQL endpoint implementation that runs in IIS allowing BrightstarDB to be exposed as a SPARQL 1.1 endpoint. For more information please refer to the :ref:`SPARQL Endpoint <SPARQL_Endpoint>` section of the documentation.

  - NEW: The BrightstarService service executable now supports specifying the base directory, HTTP and TCP ports and named pipe that the service listens on as command-line parameters

  - NEW: The BrightstarDB API has been extended to add support for importing / exporting named graphs and for executing a transaction against a named graph.

  - NEW: Added support for SPARQL 1.1

  - NEW: Added support for SPARQL UPDATE

  - NEW: SPARQL support now includes support for querying named graphs.

  - NEW: EntityFramework now supports the use of enum property types (including Flags and Nullable enum types)

  - NEW: EntityFramework now surfaces an event that is invoked immediately before changes are saved to the store. For more information please see the section :ref:`SavingChanges Event <SavingChanges_Event>`.

  - FIX: The XML Schema "date" datatype (``http://www.w3.org/2001/XMLSchema#date``) is now recognized and mapped to a System.DateTime value by EntityFramework.

  - NEW: Added support for the LINQ .All() filter operator.

  - FIX: The WCF service mode for the BrightstarDB service now supports concurrent requests.

  - FIX: Several bug fixes for LINQ to SPARQL query generation

  - NEW: BrightstarDB now supports import of a number of additional RDF syntaxes as documented in the section :ref:`Supported RDF Syntaxes <Supported_RDF_Syntaxes>`.




*************************
 BrightstarDB Public Beta
*************************


  - FIX: Several performance fixes and the introduction of configurable client and server-side caching have significantly improved the speed of SPARQL and LINQ queries. For information about configuring caching please refer to the section :ref:`Caching <Caching>`.

  - NEW: BrightstarDB Entity Framework now adds support for creating an OData provider. For more information please see the :ref:`OData <OData>` section of the :ref:`Entity Framework <Entity_Framework>` API documentation.

  - NEW: LINQ-to-SPARQL now has support for a number of additional String functions. For details please refer to the section :ref:`LINQ Restrictions <LINQ_Restrictions>`.

  - NEW: Optimistic locking support has been added to the :ref:`Data Object Layer <Optimistic_Locking_in_DOL>` and :ref:`Entity Framework <Optimistic_Locking_in_EF>`.

  - BREAKING: Entity sets exposed by the generated Entity Framework context class are now typed by the entity interface rather than the generated implementation class. Code written on top of the Entity Framework will need to be refactored to use the interface rather than the concrete class or to cast the return values to the concrete class where necessary.

  - NEW: Logging is now performed through the standard .NET tracing framework, removing the dependency on Log4Net. Please refer to the section :ref:`Logging <Logging>` for more information.

  - NEW: Polaris now supports saving SPARQL queries between sessions and configuring commonly used URI prefixes to make it quicker and easier to write SPARQL queries and transactions. These features are documented in the section :ref:`Polaris Management Tool <Using_Polaris>`.




***************************************
 BrightstarDB Developer Preview Refresh
***************************************




  - BREAKING: A number of changes and improvements to data file format means that databases created with the initial Developer Preview cannot be used with the Developer Preview Refresh.

  - NEW: Windows Phone 7.1 support. It is now possible to create applications that target Windows Phone OS 7.1 with BrightstarDB. Databases are portable between the desktop / server and the mobile version of BrightstarDB. For more information please refer to :ref:`Developing for Windows Phone 7 <Developing_for_Windows_Phone_7>`.

  - NEW: The :ref:`Data Object Layer <Data_Object_Layer>` is now publicly exposed and documented for developers to use as a mid-point between the low-level RDF Client API and the data-binding provided by the Entity Framework.

  - BREAKING: Replaced the use of Log4Net with standard Microsoft tracing. This provides more easily configurable logging and tracing functionality.

  - NEW: Polaris now provides the ability to view the previous states of a BrightstarDB store, run queries against them, and revert the database to a previous state if required.

  - NEW: Polaris now provides keyboard shortcuts for menu items and a right-click context menu on the store list.

  - FIX: The range of native datatypes supported by the EntityFramework has been greatly expanded.

  - FIX: The scope of LINQ support by EntityFramework is now better documented,

  - NEW: EntityFramework now supports String.StartsWith, String.EndsWith and Regex.IsMatch methods for string filtering in LINQ queries.

  - NEW: BrightstarDB now provides support for conditional update. This functionality is used to provide optimistic locking support for the Data Object Layer and EntityFramework.

  - NEW: NerdDinner sample now includes examples of a .NET MembershipProvider and RoleProvider implemented on BrightstarDB.

  - NEW: EntityFramework now supports properties that are an ICollection<T> of native types such as string, int etc.

  - BREAKING: The GetColumnValue extension method on XDocument now returns a typed object rather than a string whenever the bound variable's datatype is a recognized XML Schema datatype.

  - FIX: EntityFramework now supports inheritance on Entity interfaces.

  - FIX: The service contract for the BrightstarDB WCF service now has a proper URI: http://www.networkedplanet.com/schemas/brightstar.

  - BREAKING: ICommitPointInfo and ITransactionInfo interfaces have been significantly reworked to provide better history information for BrightstarDB stores.

  - FIX: SPARQL results XML document generated by the Brightstar service now escapes all reserved XML characters in the binding values.

  - FIX: Added an optimization for the SPARQL query generated by LINQ expressions that simply retrieve an entity by its identifier.

  - NEW: Added more documentation and samples, especially for Windows Phone 7 applications and the :ref:`Admin APIs <Admin_API>`.

