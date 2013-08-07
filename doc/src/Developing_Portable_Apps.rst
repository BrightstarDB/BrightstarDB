.. _Developing_Portable_Apps:

*************************
 Developing Portable Apps
*************************

BrightstarDB provides support for a restricted set of platforms through the Windows Portable 
Class library. The set of platforms has been restricted to ensure that all of the supported
platforms support the complete set of BrightstarDB features, including the :ref:`Data Object 
Layer <Data_Object_Layer>` and the :ref:`Entity Framework <Entity_Framework>` as well as the 
:ref:`RDF Client API <RDF_Client_API>`.

Supported Platforms
===================

The BrightstarDB Portable Class Library supports the following platforms:
  - .NET 4.5
  - Silverlight 5
  - Windows Phone 8
  - Windows Store Apps
  
.. note::

  If you are building an application for Windows Phone 7, it is still possible to use 
  BrightstarDB as we have a specific port for that platform (see the section 
  :ref:`Developing For Windows Phone <Developing_for_Windows_Phone>`).
  
Including BrightstarDB In Your Project
======================================

The BrightstarDB Portable Class Library is split into two parts; a core library
and a platform-specific extension library for each supported platform. The 
extension library provides file-system access methods for the specific platform.
In most cases both DLLs are required. 

Using BrightstarDB from NuGet
-----------------------------

.. note::
   At this time the BrightstarDB Portable Class Library is only included in 
   pre-release versions of the BrightstarDB NuGet package.
   
If you are writing a Portable Library DLL, you need only include the BrightstarDB
or BrightstarDBLibs package. Your DLL will have to target the same platforms that
BrightstarDB supports (see above) or a sub-set of them.

If you are writing an application, you need to include either BrightstarDB or
BrightstarDBLibs for the core library and then you must also add the 
BrightstarDB.Platform package which will install the correct extension library
for your application platform.

Using BrightstarDB from Source
------------------------------

The main portable class library build file is the file src\portable\portable.sln.
Building this solution file will build the Portable Class Library and all of the 
platform-specific extension libraries. You should then include the 
BrightstarDB.Portable.DLL and one of the following extension DLLs:

  - BrightstarDB.Portable.Desktop.DLL for .NET 4.5 applications
  - BrightstarDB.Portable.Phone.DLL for Windows Phone 8 applications
  - BrightstarDB.Portable.Silverlight.DLL for Silverlight 5 applications
  - BrightstarDB.Portable.Store.DLL for Windows store applications.
  
Alternatively you can include just the relevant project files in your application
solution and build them as part of your application build.

API Changes
===========

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
	 
Platform Notes
==============

Due to the differences in storage model, the different platforms behave slightly
differently in where they expect / allow BrightstarDB stores to be created and
accessed from.

Desktop
-------

Paths to BrightstarDB stores are resolved relative to the applications working
directory. It is possible to create / read stores in any file location accessible
to the user that the application runs as.

Phone and Silverlight
---------------------

All BrightstarDB stores are created in Isolated storage under the user-scoped
storage for the application (the store returned by 
`IsolatedStorageFile.GetUserStoreForApplication()`). Any path you specify for
a store location will be resolved relative to this isolated storage root. It is
not possible to create or access a BrightstarDB store under any other location.

Windows Store
-------------

For Windows Store applications paths are resolved relative to the user-scoped local
storage folder for the application (the folder returned by 
`ApplicationData.Current.LocalFolder`). It is not possible to create or access a
BrightstarDB store under any other location.

BrightstarDB Database Portability
=================================

All builds of BrightstarDB use exactly the same binary format for their data files. This
means that a BrightstarDB store created on any of the supported platforms can be successfully
opened and even updated on any other platform as long as all of the files are copied retaining
the original folder structure.