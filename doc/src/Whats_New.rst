
.. _Whats_New:

############
 What's New
############

.. _System.ComponentModel.INotifyPropertyChanged: http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged%28v=vs.100%29.aspx
.. _System.Collections.Specialized.INotifyCollectionChanged: http://msdn.microsoft.com/en-us/library/system.collections.specialized.inotifycollectionchanged%28v=vs.100%29.aspx


This section gives a brief outline of what is new / changed in each official release of BrightstarDB. Where there are breaking changes, that require 
either data migration or code changes in client code, these are marked with **BREAKING**. New features are marked with NEW and fixes for issues are 
marked with FIX.

****************************
 BrightstarDB 1.9.1
****************************

    This is primarily a bug-fix release with some important updates for applications using date/time values in the BrightstarDB Entity Framework.
    In addition this release adds support for the Xamarin.iOS PCL profile. This enables BrightstarDB to be used in Xamarin.Forms PCL applications 
    across Android, Windows Phone and iOS. There are no changes to the store file format, and no breaking API changes. This is a recommended update 
    for all users.

    - NEW: The PCL platform libraries now includes support for the Xamarin.iOS, Version=1.0 PCL framework. 

    - FIX: Making changes the the properties of BrightstarDB.Configuration that configure the server-side query caching will now cause the cache to be
           deleted and recreated with the new settings on the next request for the cache handle.
    
    - FIX: Added caching of master file data structures to improve performance in applications that perform large numbers of reads per write.
    
    - FIX: UTC date/time values now keep their status as UTC values. Thanks to kentcb for the bug report.
    
    - FIX: Fix for round-tripping date/time values in US locale.
    
    - FIX: Fixed an issue in the text template code generation for EF that would report an error on properties using a nullable enumeration type.
           Thanks to kentcb for the bug report on this one too!
    
    - NEW: Added caching of master file status which should improve performance in applications which perform large numbers of read/query operations
           from the same commit point.
          
           
****************************
 BrightstarDB 1.9 Release
****************************

    - NEW: The W3C SPARQL 1.1 Graph Store Protocol is now implemented by the BrightstarDB service. See :ref:`SPARQL_Endpoint` for more information.
    
    - NEW: The Polaris UI now allows the default graph IRI to be specified for import operations. Thanks to Daniel Bryars for this contribution.
    
    - NEW: The REST API implementation now reports parser error messages back to the client along with the 400 status code. Polaris has also been
           updated to display these messages to the end-user. Thanks to Daniel Bryars for this contribution.
           
    - NEW: It is now possible to configure an embedded BrightstarDB client to not log transaction data. As this transaction data can be quite large,
           the default for mobile and windows store configurations is now for transaction logging to be disabled. For all other platforms, transaction
           logging is enabled by default but this default can be overridden either by app settings or programmatically. For more information please
           refer to :ref:`Controlling_Transaction_Logging`
           
    - **BREAKING**: There is a minor API change to the BrightstarDB.Configuration API. The PreloadConfiguration property has been replaced with the
            EmbeddedServiceConfiguration property (the PreloadConfiguration can be found as a property of the EmbeddedServiceConfiguration). This 
            change will only affect applications which programmatically set the page cache preload configuration. Applications which use the app.config
            or web.config file to configure page cache preload should not be affected by this change.
           
    - NEW: The Entity Framework now allows the creation of Id properties whose value is the full IRI of the underlying RDF resource (without any
           predefined prefix). This is achieved by using the Identifier decorator with an empty string for the BaseAddress parameters ([Identifier("")]).
           For more information please refer to :ref:`Identifier_Attribute` in the Entity Framework :ref:`Annotations_Guide`.
    
****************************
 BrightstarDB 1.8 Release
****************************

    - NEW: EntityFramework now supports GUID properties.
    
    - NEW: EntityFramework now has an [Ignore] attribute which can be used to decorate interface properties
           that are not to be implemented by the generated EF class. See the :ref:`guide to EF Annotations <Annotations_Guide>` for
           more information.
           
    - NEW: Added a constructor option to generated EF entity classes that allows property initialisation in the constructor. Thanks to CyborgDE for
           the suggestion.
        
    - NEW: Added some basic logging support for Android and iOS PCL builds. These builds now log diagnostic messages when built in Debug configuration,
           and the BrightstarDB logging subsystem can be initialized with a local file name to generate persistent log files in Release configuration.
           
    - NEW: It is now possible to iterate the distinct predicates of a data object using the GetPropertyTypes method.
    
    - FIX: Fix for Polaris crash when attempting to process a query containing a syntax error.
    
    - FIX: Fixed NuGet packaging to remove an obsolete reference to Windows Phone 8. WP8 (and 8.1) are still both supported but as PCL profiles.
    
    - FIX: Performance fix for full cache scenarios. When an attempt to evict items out of a full cache results in no items being evicted, the eviction
           process will not be repeated again for another minute to allow for any current update transactions that have locked pages in the cache to complete.
           This can avoid a lot of unnecessary cache scans when a large update transaction is being processed. Thanks to CyborgDE for the bug report.
           

****************************
 BrightstarDB 1.7 Release
****************************

    - BREAKING: BrightstarDB no longer supports Windows Phone 7 development. Due to changes in the
                libraries that we use there is now only a Portable Class Library build available 
                which targets .NET 4.5, Windows Phone 8, Silverlight 5, Windows Store apps and
                Android. iOS support is in the pipeline.
                
    - NEW: EXPERIMENTAL support has been added for using DotNetRDFs virtual nodes query facility.
           This feature can improve query performance by reducing the number of times that RDF
           resource values need to be looked up. There are still some bugs left to be ironed out
           in this feature so it should not be used in production. To enable this feature set
           BrightstarDB.Configuration.EnableVirtualizedQueries to true.
           
    - NEW: Added support for non-existence preconditions on transactional updates. This precondition
           fails if one or more of the specified triples already exists in the store prior to executing
           the update. See :ref:`RDF_Transactional_Update`.
    
    - NEW: Added support for generated and composite keys for entities. See :ref:`Key_Properties_In_EF`.
           This includes a new type-based unique constraint check for entities with generated or composite keys.

    - NEW: RDF/XML is now supported as an export format.
    
    - NEW: It is now possible to retrieve an IEntitySet from the Entity Framework context using the EntitySet<T>()
           method on the context object. Thanks to NZ_Dig for the contribution.
           
    - FIX: Fixed the way that the BrightstarDB Entity Framework handles the case where the same RDF property has
           a domain or range of multiple classes. The collections provided by Entity Framework now filter to 
           exclude resources which are not of the expected type rather than trying to coerce the resources into
           the expected type. This leads to more consistent OO behaviour. Thanks to NZ_Dig for the bug report.
           
    - FIX: Added guard statements to PCL implementation of ConcurrentQueue<T> to avoid InvalidOperationExceptions
           being raised and then immediately handled in the case of an empty queue being accessed.
           
    - FIX: Major overhaul of the BinaryFilePageStore (the basis of the rewrite store type). This fixes a number of
           issues found under the PCL build and also introduces support for background writing of page updates
           to improve update performance. Thanks to CyborgDE for the bug report.
           
    - FIX: Replaced polling loop with proper synchronized handling of job status changes in the embedded store
           implementation. Thanks to CyborgDE for the fix.
    
    - FIX: A number of fixes to the JS used in the browser interface to the BrightstarDB server.
    
    - FIX: Reinstated logging for the BrightstarDB service.
    
    - FIX: Removed dependency on external System.Threading.Tasks DLL
    
    - NEW: Jobs are now given a default name if one is not specified when they are created.
    
    
***************************
 BrightstarDB 1.6.2 Release
***************************

  - FIX: Fixed an error in the LRU cache implementation that could corrupt the cache during import / update operations.
         Thanks to pcoppney for the bug report.
         
  - FIX: Fixed version number specified in the setup bootstrapper and reported when looking at the installed programs under Windows.

***************************
 BrightstarDB 1.6.1 Release
***************************

  - FIX: Restored default logging configuration for BrightstarDB service
  
  - FIX: Fix for wildcard delete patterns in a transaction processed against a SPARQL endpoint.
         Thanks to feugen24 for the bug report and suggested fix.
  
  - FIX: SPARQL endpoint connection strings now default the store name to "sparql". Thanks to 
         feugen24 for raising the bug report.
         
  - FIX: Fixed sample projects included in the MSI installer. Thanks to aleblanc70 for the bug report.
  
  - NEW: Added platform-specific default configuration settings and removed dependency on 
         third-party System.Threading.Tasks.dll from Windows Phone build.
         
*************************
 BrightstarDB 1.6 Release
*************************

  - NEW: Added experimental support for Android.
  
  - NEW: Jobs created through the API can now be assigned a user-defined title string, this will be displayed / returned 
         when the jobs are listed.

  - NEW: Entity Framework internals allow better constructor injection of configuration parameters.

  - NEW: Entity Framework will now "eagerly" load the triples for entities returned by a LINQ query in a wider number of 
         circumstances, including paged and sorted LINQ queries.

  - NEW: Added a utility class to the API for retrieving the namespace prefix declarations used by entity classes and 
         formatting them for custom SPARQL queries or Turtle files.

  - NEW: Export job now has an additional optional parameter to specify the export format. Currently only NTriples and NQuads 
         are supported but this will be extended to support other export syntaxes in future releases.

  - NEW: Added support to the BrightstarDB server for using ASP.NET membership and role providers to secure access to the server 
         and its stores. For more information please refer to the section :ref:`Configuration_Authentication`.
         
  - **BREAKING**: The connection string syntax for connections to generic SPARQL endpoints and to other RDF stores via dotNetRDF
         has been changed. Please refer to the section :ref:`Connection_Strings` for more information.
  
  - FIX: Fix for bug in reading back through multiple entries in the store statistics log.

  - FIX: Fixed the New Job form in the browser interface for the BrightstarDB server so that it properly resets on page load.

  - FIX: Fixed the New Job form to allow Import and Export jobs to be created without requiring a Graph URI.

  - FIX: Fix for concurrency bug in Background Page Writer - with thanks to Michael Schulte for the bug report and suggested fix.

  
****************************
 BrightstarDB 1.5.3 Release
****************************
  - FIX: Fixes a packaging issue with the Polaris tool in the 1.5.2 release.
  
****************************
 BrightstarDB 1.5.2 Release
****************************

  - FIX: Fixed a regression bug in the SPARQL query template for the browser interface to the BrightstarDB server.
  
  - FIX: Added missing sizing parameters to the SPARQL results text box in the browser interface.
  
  - FIX: Fixed browser interface for SPARQL queries to not report an error when the form is initially loaded.

****************************
 BrightstarDB 1.5.1 Release
****************************
  - FIX: Fixed the default connection string used in the NerdDinner sample.
  
  - NEW: Installer now supports installing the VS extensions into VS2013 Professional edition and above.
  
  - NEW: Overhaul of the SPARQL query APIs to allow the specification of both SPARQL results format and RDF graph format. This
    allows RDF formats other than RDF/XML to be returned by CONSTRUCT and DESCRIBE queries. For more information please refer to
    :ref:`RDF_Client_API_SPARQL`
    
  - NEW: Added an override for GetJobInfo to list the jobs recently queued or executed for a store. Refer to :ref:`Admin_API_Jobs` for
    more information.
  
****************************
 BrightstarDB 1.5 Release
****************************

  - **BREAKING** : The WCF server has been replaced with an HTTP server with a full RESTful API. Connection strings of type ``http``, ``tcp`` and ``namedpipe`` are 
    no longer supported and should be replaced with a connection string of type ``rest`` to connect to the HTTP server. The new HTTP server can be run under IIS
    or as a Windows Service and the distribution includes both of these configuration options. For more information please refer to :ref:`Running_BrightstarDB`.
    The configuration for the server has also been changed to enable more complex configuration options. The new configuration structure is detailed in 
    :ref:`Running_BrightstarDB`. 
    Please note when upgrading from a previous release of BrightstarDB you may have to manually edit the server configuration file
    as an existing configuration file cannot be overwritten if it was locally modified.
    
  - **BREAKING**: The SDShare server has been removed from the BrightstarDB package. This component is now managed in a separate Github repository (https://github.com/BrightstarDB/SDShare)
  
  - **BREAKING**: RDF literal values without an explicit datatype are now exposed through the Data Objects and Entity Framework APIs as instances of the type ``BrightstarDB.Rdf.PlainLiteral``
    rather than as ``System.String``. This change has been made to better enable the APIs to deal with RDF literals with language tags. This update allows both dynamic objects and
    Entity Framework interfaces to have properties typed as ``BrightstarDB.Rdf.PlainLiteral`` (or an ``ICollection<BrightstarDB.Rdf.PlainLiteral>``). The LINQ to SPARQL implementation
    has also been updated to support this type. However, this change may be **BREAKING** for some uses of the API. In particular when using either the dynamic objects API or
    the SPARQL results set ``XElement`` extension methods, the object returned for an RDF plain literal result will now be a ``BrightstarDB.Rdf.PlainLiteral`` instance rather
    than a string. The fix for this breaking change is to call ``.ToString()`` on the ``PlainLiteral`` instance. e.g::
        
            // This comparison will always return false as the object returned by 
            // GetColumnValue is a BrightstarDB.Rdf.PlainLiteral
            bool isFoo = resultRow.GetColumnValue("o").Equals("foo");
            
            // To fix this breaking change insert .ToString() like this:
            bool isActuallyFoo = resultRow.GetColumn("o").ToString().Equals("foo");
            
            // Or for a more explicit comparison
            bool isLiteralFoo = resultRow.GetColumn("o").Equals(new PlainLiteral("foo"));
        
  - NEW: Job information now includes date/time when the job was queued, started processing and completed processing.
  
  - NEW: BrightstarDB installer now includes both 32-bit and 64-bit versions and will install into ``C:\Program Files\`` on 64-bit platforms.
  
  - NEW: Added shell scripts for building BrightstarDB under mono.
  
  - NEW: BrightstarDB Entity Framework and Data Objects APIs can now connect to stores other than BrightstarDB. 
    This includes the ability to use the Entity Framework and DataObjects APIs with generic SPARQL 1.1 Query and 
    Update endpoints, as well as the ability to use these APIs with other stores supported by DotNetRDF. 
    For more information please refer to :ref:`Other_Stores`
  
  - FIX: Fixed incorrect handling of \\ escape sequences in the N-Triples and N-Quads parsers.
  
  - FIX: BrightstarDB now uses NuGet to provide the DotNetRDF library rather than using a local copy of the assemblies.

****************************
 BrightstarDB 1.4 Release
****************************

  - NEW: Stores can now extract and persist basic triple count statistics. See :ref:`Admin_Stats` for more information.
  
  - NEW: Stores can now be cloned into a new snapshot store. For stores using the append-only storage mechanism, a snapshot can be created from any previous commit point. See :ref:`Admin_Snapshots` for more information
  
  - NEW: Added support for System.Uri typed properties in Entity Framework. Thanks to github user jhashemi for the suggestion.
  
  - NEW: Portable class library build. Refer to :ref:`Developing_Portable_Apps` for more information.
  
  - NEW: Dynamic objects and Entity Framework APIs now support named graphs.
  
  - FIX: Reduced memory usage for BTree's by half.
  
  - FIX: Fixed a memory leak in the page cache code that prevented expired pages from being released to the garbage collector.
  
  - FIX: Fixed the resource ID and resource caches to support a (configurable) limit on the number of entries cached.
  
  - FIX: Fixed error in deleting an entity from the same entity framework context in which it was originally created. Thanks to github user cmerat for the report.
  
  - FIX: Fixed EntityFramework code to clean up InverseProperty collections correctly. Thanks to BrightstarDB user Alan for the bug report.
  
  - FIX: Fixed EntityFramework text template code for matching class names in generic collection properties. Thanks to github user Xsan-21 for the bug report.
  
  - FIX: Fix for Polaris hanging when trying to process a GZipped NTriples file.
  
*************************
 BrightstarDB 1.3 Release
*************************

  - NEW: First official open source release. All documentation and examples updated to remove references to commercial licensing and license protection code. Build updated to remove dependencies on third-party commercial tools

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

  - NEW: Windows Phone 7.1 support. It is now possible to create applications that target Windows Phone OS 7.1 with BrightstarDB. Databases are portable between the desktop / server and the mobile version of BrightstarDB. 

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

