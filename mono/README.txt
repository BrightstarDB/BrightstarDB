BrightstarDB Mono Build
=======================

This is a very early stage Mono build. The build script is pretty
rudimentary and currently only builds the core BrightstarDB
assembly. More will be added here as work on the Mono build
progresses.


Pre-requisites
--------------

The build has been developed and tested using Mono 3.2.4 in Ubuntu 13.04
if you have a different Mono version / platform configuration your
mileage may well vary!

The build process makes use of NuGet to restore certain required
packages. NuGet.exe is included in the BrightstarDB repo, however
under Linux, use of NuGet requires that certain SSL
certificates be registered in the machine certificate store.

Before trying to build under Linux please execute the following
commands:

sudo mozroots --import --machine --sync
sudo certmgr -ssl -m https://go.microsoft.com
sudo certmgr -ssl -m https://nugetgallery.blob.core.windows.net
sudo certmgr -ssl -m https://nuget.org

Building BrightstarDB
---------------------

To build BrightstarDB under Mono cd to the mono directory in 
the repository and execute

./build.sh

Output will be copied to mono/build under the mono directory.
