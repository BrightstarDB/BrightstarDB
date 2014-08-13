.. _BrightstarDB_Under_Mono:

###############################
 Using BrightstarDB Under Mono
###############################

This section covers how to use the BrightstarDB libraries and server 
in a Mono environment as well as how to build BrightstarDB from 
source using Mono.

.. warning::
    The use of BrightstarDB under Mono should be considered to be
    in alpha status. It is certainly not yet ready for production
    use. 
    
We would welcome any bug reports and especially reproducible
errors in BrightstarDB under Mono to help us improve the stability
of the code on this platform.

*********************************
 Using BrightstarDB Libraries
*********************************

The best way to use BrightstarDB libraries in your Mono application
is to retrieve the packages via NuGet. There are details about
using NuGet under mono in the `NuGet FAQ <http://docs.nuget.org/docs/start-here/nuget-faq>`_.

The .NET 4.0 version of BrightstarDB should work correctly under the latest version
of Mono (3.2.4 at the time of writing). It will probably not work under older versions
of Mono.


.. _mono_build:

**********************************
 Building From Source
**********************************

You can also build the core BrightstarDB libraries under Mono. Refer to
:ref:`Build_GettingTheSource` for information about getting hold of 
BrighstarDB source code.

.. note::
    You will need to build from the source code for release 1.5 or
    from the develop branch in order to have the Mono build scripts.
    
The build scripts for mono are contained in the ``mono`` directory
at the top level of the project. The build scripts use NuGet
to retrieve dependency package and a version of NuGet.exe is included
in the BrightstarDB repository to assist in that. However, NuGet
requires some SSL certificates to be registered otherwise download
will fail. Before trying to build under Linux please execute the following
commands::

    sudo mozroots --import --machine --sync
    sudo certmgr -ssl -m https://go.microsoft.com
    sudo certmgr -ssl -m https://nugetgallery.blob.core.windows.net
    sudo certmgr -ssl -m https://nuget.org 

Once this is completed, it should be possible to build BrightstarDB
simply by executing the ``build.sh`` script from within the ``mono``
directory. The resulting binaries can then be found in ``mono/build``.
At the time of writing the build script builds the following items:

    * ``BrightstarDB.dll`` : the core BrightstarDB library
    * ``BrightstarDB.Server.Modules.dll``: the core BrightstarDB server library
    * ``service`` : a directory containing the BrightstarDB server runner and all of its dependencies
    
.. warning::
    The build will result in a default BrightstarService.exe.config file that 
    contains an MSDOS path in the configuration for the BrightstarDB connection
    string. You will need to edit this to a suitable UNIX path before running the service.


**********************************
 Running a BrightstarDB Server
**********************************

Self-Hosted
===========

Assuming that you have built BrightstarDB from source as described above, the server can be run
from within `mono/build/service'.

.. warning:: 

    Before you run the service for the first time you must edit the `BrightstarService.exe.config`
    file in `mono/build/service' as this file is copied out of the Windows build and so contains DOS path names.
    You need to edit the path for the log file (in the `system.diagnostics` section) and the `storesDirectory` 
    path in the connection string specified in the `brightstarService` section.

To start the server simply run the following::

    mono BrightstarService.exe
    
The service will start listening on port 8090 at the path /brightstar. So from a local machine you can
access the service from a browser pointed at http://localhost:8090/brightstar.

TBD: Document how to run the BrightstarDB server in Apache using mod_mono

TBD: Document how to secure the BrightstarDB server under Mono

************************************
 Unit Tests
************************************

The unit tests for Mono can be run using the ``test.sh`` script in to
``mono`` directory. At present these tests do not successfully run
to completion, but this is being worked on.

************************************
 iOS and Android Support
************************************

The ultimate goal for getting a working build of BrightstarDB under
Mono is to support development of applications using `Xamarin's <http://xamarin.com>`
Xamarin.iOS and Xamarin.Android libraries.

At the time of writing, this work is postponed, waiting on Xamarin to provide
the project with the necessary licenses for testing. The work done thus far
can be found in the feature/xamarin branch in our GitHub repository.

We would welcome support for any developers familiar with either of these
platforms and with plans to make use of a NoSQL / RDF data store in their
applications.
