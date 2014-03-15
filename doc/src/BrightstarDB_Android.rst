.. _BrightstarDB_Android:

==========================
 BrightstarDB for Android
==========================

The 1.6 release of BrightstarDB introduces EXPERIMENTAL support for running BrightstarDB as an
embedded store on Android. The implementation is based on the Portable Class Library build of
BrightstarDB with an Android-specific platform assembly.

-------------------------------------------------
Adding BrightstarDB to an Xamarin.Android Project
-------------------------------------------------

The easiest way to add BrightstarDB to an Android development project is via NuGet.
This is exactly the same procedure as for any Portable Class Library platform
as documented in :ref:`Developing_Portable_Apps`.

--------------------
Building From Source
--------------------

To build from source you will require an installation of Xamarin.Android at Indie level
or above. Unfortunately once BrightstarDB is included the built application size will
exceed the maximum supported by the Free version of Xamarin.Android.

The project file for Xamarin.Android support is kept in a separate solution from the
main PCL code because of this additional external dependency. So to build the 
code for Android you will need to open the solution file src\\portable\\xamarin.sln.


--------------------------------
Using BrightstarDB under Android
--------------------------------

Please note the following when using BrightstarDB from within an Android application.

    #. Due to limited resources (both time and
       money!) the code  has only been tested on the Google emulators. It has not been
       tested on any Android hardware.
       
    #. The REST client is not tested and not supported yet.
    
    #. Ensure that the StoresDirectory property of your embedded client connection string
       specifies a path that your application can write to. The persistence layer used
       will use the System.IO classes in Mono, not IsolatedStorage, so you need to be
       careful to provide a path that Android will allow your application to read from 
       and write to (including creating subdirectories and files).
       
    #. The default configuration values for PageCacheSize and QueryCacheMemory are 
       almost certainly too large for a mobile device. As there is no easy way to
       use app.config from any PCL application, we recommend that you explicitly
       set the BrightstarDB.Configuration class properties when your application
       starts up.
       
    #. Query is not currently optimized for devices with small amounts of memory.
       SPARQL queries can vary quite widely in their runtime memory footprint 
       depending both on how the query is written and on the size of data being
       queried. We plan on addressing the amount of memory used by SPARQL
       query processing in a future release.
       
OK, that is a lot of caveats, but we would really welcome one or two brave souls
trying this out in a test Android application and giving us some feedback.

--------------
Acknowledgment
--------------

We would like to thank `Xamarin <http://xamarin.com/>`_ for providing the BrightstarDB 
project with a license for their Xamarin.Android product - without it we wouldn't be 
able to continue to develop and support this branch of the code!