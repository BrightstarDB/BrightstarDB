.. _BrightstarDB_Under_Mono:

###############################
 Using BrightstarDB Under Mono
###############################

This section covers how to use the BrightstarDB libraries and server 
in a Mono environment as well as how to build BrightstarDB from 
source using Mono.

*********************************
 Using BrightstarDB Libraries
*********************************

The best way to use BrightstarDB libraries in your Mono application
is to retrieve the packages via NuGet. There are details about
using NuGet under mono in the `NuGet FAQ <http://docs.nuget.org/docs/start-here/nuget-faq>`_.

The .NET 4.0 version of BrightstarDB should work correctly under the latest version
of Mono (3.12.1 at the time of writing). It will probably not work under versions
of Mono older than 3.2.4.


.. _mono_build:

**********************************
 Building From Source
**********************************

If you plan only to use BrighstarDB as an embedded database inside an application
we recommend using the NuGet package. However if you want to run a BrighstarDB 
server or just want to live on the bleeding edge of the develop branch you will
need to build BrighstarDB from source. All the details can be found in the 
section  :ref:`Building_BrightstarDB`.

**********************************
 Running a BrightstarDB Server
**********************************

Self-Hosted
===========

After building BrightstarDB from source, the server executable can be found in the directory
``build/server``. This directory contains everything
needed to run the server so you can simply copy the directory contents elsewhere and run
from that location instead.

.. warning:: 

    Before you run the service for the first time you must edit the `BrightstarService.exe.config`
    file in as this file is copied out of the Windows build and so contains DOS path names.
    You need to edit the path for the log file (in the `system.diagnostics` section) and the `storesDirectory` 
    path in the connection string specified in the `brightstarService` section.

To start the server simply run the following::

    mono BrightstarService.exe
    
The service will start listening on port 8090 at the path /brightstar. So from a local machine you can
access the service from a browser pointed at http://localhost:8090/brightstar.

BrightstarDB in Apache
======================

TBD: Document how to run the BrightstarDB server in Apache using mod_mono

BrighstarDB under nginx
========================

TBD: Document how to configure the BrighstarDB server under nginx.

BrighstarDB Server Security
===========================

TBD: Document how to secure the BrightstarDB server under Mono

