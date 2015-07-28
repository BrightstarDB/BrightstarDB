.. _Developing_Portable_Apps:

=========================
 Developing Portable Apps
=========================

BrightstarDB provides support for a restricted set of platforms through the Windows Portable 
Class library. The set of platforms has been restricted to ensure that all of the supported
platforms support the complete set of BrightstarDB features, including the :ref:`Data Object 
Layer <Data_Object_Layer>` and the :ref:`Entity Framework <Entity_Framework>` as well as the 
:ref:`RDF Client API <RDF_Client_API>`.

-------------------
Supported Platforms
-------------------

The BrightstarDB Portable Class Library supports PCL Profile 344. That includes the following platforms:
  - .NET 4.5
  - Silverlight 5
  - Windows 8
  - Windows Phone 8.1
  - Windows Phone Silverlight 8
  - Android
  - iOS
  - MonoTouch
  
--------------------------------------  
Including BrightstarDB In Your Project
--------------------------------------

The BrightstarDB Portable Class Library is split into two parts; a core library
and a platform-specific extension library for each supported platform. The 
extension library provides file-system access methods for the specific platform.
In most cases both DLLs are required. 

Using BrightstarDB from NuGet
=============================

If you are writing a Portable Library DLL, you need only include the BrightstarDB
or BrightstarDBLibs package. Your DLL will have to target the same platforms that
BrightstarDB supports (see above) or a sub-set of them.

If you are writing an application, you need to include either BrightstarDB or
BrightstarDBLibs for the core library and then you must also add the 
BrightstarDB.Platform package which will install the correct extension library
for your application platform.

Using BrightstarDB from Source
==============================

The main portable class library build file is the file src\portable\portable.sln.
Building this solution file will build the Portable Class Library and all of the 
platform-specific extension libraries. You should then include the 
BrightstarDB.Portable.DLL and one of the following extension DLLs:

  - BrightstarDB.Portable.Desktop.DLL for .NET 4.5 applications
  - BrightstarDB.Portable.Silverlight.DLL for Silverlight 5 applications
  - BrightstarDB.Portable.Android for Android applications.
  - BrightstarDB.Portable.iOS for iOS applications.
  - BrightstarDB.Portable.MonoTouch for MonoTouch applications.
  - BrightstarDB.Portable.Universal81 for Windows 8/Windows Phone 8.1 applications
  
Alternatively you can include just the relevant project files in your application
solution and build them as part of your application build.

-----------
API Changes
-----------

There are some minor differences between the APIs provided by the Portable Class
Library build and the other builds of BrightstarDB.

  1. The IBrightstarService.ExecuteTransaction and IBrightstarService.ExecuteUpdate 
     methods do not support the optional `waitForCompletion` parameter. These methods
     will always return without waiting for completion of the transaction / update 
     and you must write code to monitor the job until it completes.
  
  #. The configuration options detailed in :ref:<BrightstarDB_Configuration_Options>
     are not supported as there is no common interface for accessing application
     configuration information. Instead you can set the static properties 
     exposed by the `BrightstarDB.Configuration` class at run-time (see the API
     documentation for details).
     
--------------
Platform Notes
--------------

Due to the differences in storage model, the different platforms behave slightly
differently in where they expect / allow BrightstarDB stores to be created and
accessed from.

Desktop
=======

Paths to BrightstarDB stores are resolved relative to the applications working
directory. It is possible to create / read stores in any file location accessible
to the user that the application runs as.

Phone and Silverlight
=====================

All BrightstarDB stores are created in Isolated storage under the user-scoped
storage for the application (the store returned by 
`IsolatedStorageFile.GetUserStoreForApplication()`). Any path you specify for
a store location will be resolved relative to this isolated storage root. It is
not possible to create or access a BrightstarDB store under any other location.

Windows Universal App
=====================

For Windows Store / WP8.1 applications paths are resolved relative to the user-scoped local
storage folder for the application (the folder returned by 
`ApplicationData.Current.LocalFolder`). It is not possible to create or access a
BrightstarDB store under any other location.

Android
=======

Android support is in the early stages of development and should be considered
experimental. 

Please note the following when using BrightstarDB from within an Android application.

    #. The package targets Android API Level 10.
    
    #. Due to limited resources the code  has only been tested on the Google emulators. 
       It has not yet been tested on any Android hardware.
       
    #. The REST client is not tested and not supported yet.
    
    #. Ensure that the StoresDirectory property of your embedded client connection string
       specifies a path that your application can write to. The persistence layer used
       will use the System.IO classes in Mono, not IsolatedStorage, so you need to be
       careful to provide a path that Android will allow your application to read from 
       and write to (including creating subdirectories and files).
       
    #. As there is no easy way to use app.config from any PCL application, we recommend that 
       you explicitly set the BrightstarDB.Configuration class properties when your application
       starts up.
       
    #. Query is not currently optimized for devices with small amounts of memory.
       SPARQL queries can vary quite widely in their runtime memory footprint 
       depending both on how the query is written and on the size of data being
       queried. We plan on addressing the amount of memory used by SPARQL
       query processing in a future release.
       
OK, that is a lot of caveats, but we would really welcome one or two brave souls
trying this out in a test Android application and giving us some feedback.

iOS
===

Please note the following when using BrightstarDB from within an iOS application.

    #. The code has been tested on iOS simulators and on an iPad Air running iOS 8.1.
       
    #. The REST client is not tested and not supported yet.
    
    #. Ensure that the StoresDirectory property of your embedded client connection string
       specifies a path that your application can write to. The persistence layer used
       will use the System.IO classes in Mono, so you need to be
       careful to provide a path that Android will allow your application to read from 
       and write to (including creating subdirectories and files). We recommend using
       a sub-folder within the Library folder for your app.
       
    #. As there is no easy way to use app.config from any PCL application, we recommend that 
       you explicitly set the BrightstarDB.Configuration class properties when your application
       starts up.
       
    #. Query is not currently optimized for devices with small amounts of memory.
       SPARQL queries can vary quite widely in their runtime memory footprint 
       depending both on how the query is written and on the size of data being
       queried. We plan on addressing the amount of memory used by SPARQL
       query processing in a future release.
       
    #. The iOS build process may unintentionally strip out the BrightstarDB.Portable.iOS 
       platform support library because no code directly references it. To avoid this,
       the iOS portable library package now includes a source file named 
       BrightstarDBForceReference.cs which will automatically be included in your project.
       If you are building from source, this source file can be found in the directory
       `installer/nuget` and you should manually include this file in your project.

---------------------------------
BrightstarDB Database Portability
---------------------------------

All builds of BrightstarDB use exactly the same binary format for their data files. This
means that a BrightstarDB store created on any of the supported platforms can be successfully
opened and even updated on any other platform as long as all of the files are copied retaining
the original folder structure.

--------------
Acknowledgment
--------------

We would like to thank `Xamarin <http://xamarin.com/>`_ for providing the BrightstarDB 
project with a license for their Xamarin.Android and Xamarin.iOS products - without them we wouldn't be 
able to continue to develop and support those branches branch of the code!
