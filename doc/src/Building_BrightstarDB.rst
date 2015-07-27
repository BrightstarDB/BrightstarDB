:title: Building BrightstarDB!

.. _Building_BrightstarDB:

######################
 Building BrightstarDB
######################

This section will take you through the steps necessary to build BrightstarDB from source.

.. _Build_Prerequisites:

**************
 Prerequisites
**************

Before you can build BrightstarDB you need to install the following tools.

    1.  **Visual Studio 2013/2015** or **Mono 3.2.4 or later**
    
        You can use the Professional or Ultimate editions to build everything.
        
        
    #.  **OPTIONAL: WiX**
        
        WiX is required only if you plan to build the Windows installer packages for
        BrightstarDB. You can download WiX from http://wixtoolset.org/. 
        It is recommended to build with the latest 3.x version of WiX (3.9R2 at the time 
        of writing).
        
    #. **OPTIONAL: Xamarin.Android, Xamarin.iOS**
    
       Xamarin.Android is required only if you plan to build the Android package
       for BrightstarDB, and Xamarin.iOS is needed to build the iOS package. 
       Please read :ref:`Developing_Portable_Apps` first!


.. note::

    You will require an internet connection when first building
    BrightstarDB, even after you have initially retrieved the source, as some 
    NuGet packages will need to be downloaded.
    
.. warning::

    Under Linux, NuGet requires some SSL certificates to be registered otherwise download
    will fail. Before trying to build under Linux please execute the following
    commands::

        sudo mozroots --import --machine --sync
        sudo certmgr -ssl -m https://go.microsoft.com
        sudo certmgr -ssl -m https://nugetgallery.blob.core.windows.net
        sudo certmgr -ssl -m https://nuget.org 
        
.. _Build_GettingTheSource:

*******************
 Getting The Source
*******************

    The source code for BrightstarDB is kept on GitHub and requires Git to retrieve it.
    
    The easiest way to use Git on Windows is to get the GitHub for Windows application
    from http://windows.github.com/. Alternatively you can download the Git installation
    package from http://git-scm.com/. If you do not want to install Git and are happy 
    to simply work with a snapshot of the code, the GitHub website offers ZIP file packages 
    of the source tree.
    
    Alternatively you can use the command-line Git tools from http://git-scm.com/ or your own
    favourite GUI wrapper for Git.
    
    
**Branches**

    The BrightstarDB source code is organized into multiple branches. The most important
    ones are **develop** and **master**. 
    
    The **develop** branch is the latest development
    version of the source code. Most of the time the code on **develop** should be stable
    in as much as it compiles and the unit tests all pass. However, occasionally this is 
    not the case.
    
    The **master** branch only gets updated when a new release is ready, so the head
    of the **master** branch will be the source code for the last release.
    
    Branches with the name **release/X.X** contain the source code for the named release.
    These branches will typically only exist while a new release is being prepared. To
    find a previous release in the Git repository you should instead use the Tags.
    
    Branches with the name **feature/XXX** contain work in progress and should be regarded
    as unstable.
    
**Cloning With GitHub For Windows**

    To retrieve a clone of the Git repository simply go to https://github.com/BrightstarDB/BrightstarDB
    and on the right-hand side of the page you will see a button labelled "Clone in Desktop".
    Click on that button to launch GitHub for Windows and start the process of cloning the
    repository. Once you have the cloned repository you can then use the GitHub for Windows
    GUI to select the branch you want to work with.
    
**Cloning With Git**

    To clone over HTTPS use the repository URL https://github.com/BrightstarDB/BrightstarDB.git
    To clone over SSH use `git@github.com:BrightstarDB/BrightstarDB.git`. Note that cloning
    over SSH requires that you have an SSH key set up with GitHub.
    
**Downloading a source ZIP**

    You can download the source code on a given branch as a ZIP file if you want to 
    avoid using Git. To do this, go to https://github.com/BrightstarDB/BrightstarDB
    and select the branch you want to download from the drop-down box. Then use the
    'Download ZIP' button to retrieve the source.

.. _Build_Proj:

************************************
 MSBuild/XBuild Script (build.proj)
************************************

The quickest and simplest way to build BrightstarDB is to use the build.proj MSBuild/XBuild
script. This script is found in the top-level directory of the BrightstarDB source. With
Visual Studio installed you can then build with a command line like::

    msbuild build.proj [SWITCHES]

And with Mono installed you can use xbuild instead::

    xbuild build.proj [SWITCHES]
    

The script uses the following properties:

    Configuration
        The project configuration to be built. Can be either `Debug` or `Release`. Defaults to `Debug`.
    
    NoPortable
        Do not build any of the Portable Class Libraries. Defaults to `False` on Windows, and `True` on
        Linux / OS X.

    NoXamarin
        Do not build any Xamarin targets even if a Xamarin installation is detected on the local machine.
        Defaults to `False`.
    
    NoiOS
        Do not build any iOS targets, even if a Xamarin.iOS installation is detected on the local machine.
        Defaults to `False`.
        
       
You can either override these properties on the command-line using ``/p:{Property}={Value}`` switches
or you can edit the build.proj file (the properties are defined at the top of the file).

The MSBuild script contains a number of separate targets for the different stages of the build. 
You can select the specific target or targets to be built on the command line using ``/t:{Target}``
switches.  Read through the script for a complete understanding of all of the targets, but the most 
important targets are:

    Build
        Build Core, Server, OData Server, Portable Class Libraries and the Polaris database management tool.
        Under mono, only Core and Server get built due to unsupported dependencies.
        This is the default target that will be run if you don't specify a ``/t:{Target}`` switch on the command-line.
        
    BuildCore
        Performs a clean build of the core .NET 4.0 library only. This is all you need to create applications
        that use BrightstarDB as an embedded database.
        
    BuildPortable
        Builds the Portable Class Library version of the core BrightstarDB library and whichever platform
        dependencies can be satisfied and are allowed by the command line build options described above. 
        
    BuildServer
        Builds the NancyFX REST server for BrightstarDB.
        
    BuildOData
        Builds the OData server.
        
    BuildTools
        Builds the Polaris database management tool. This target does not build under Mono as it requires
        WPF.
        
    RunTests
        Run main unit tests
        
    TestPortable
        Run the PCL unit tests
        

The ``build.proj`` script will not only compile the sources, but also package up the most commonly used binaries and
place them in a new ``build`` directory. The current contents of the ``build`` directory (assuming you build everything)
is:

    build/sdk/NET40
        The core .NET libraries for BrightstarDB
        
    build/sdk/pcl
        The core Portable Class Library assemblies for BrighstarDB
        
    build/sdk/pcl/platforms
        Individual platform-specific assemblies for supported PCL targets
        
    build/sdk/pcl_ARM
        Windows Store portable class libraries targetting the ARM architecture

    build/sdk/pcl_x86
        Windows Store portable class libraries targetting the x86 architecture
        
    build/server
        Standalone (self-hosted) BrightstarDB server. You can run this directly with ``BrightstarService`` under Windows
        or ``mono BrightstarService.exe`` when using Mono. 
        
    build/tools/codegen
        The standalone entity framework code generator.
        
    build/tools/polaris
        The Polaris desktop client application (this is a WPF application and is not available on non-Windows platforms).
        
.. warning::
    The default configuration file for the BrighstarDB server contains Windows-specific paths. Please edit this file
    to change the log file configuration and BrightstarDB service connection string before attempting to run the server
    on non-Windows system.
    
.. note::
    The ``build.proj`` script is provided to make it easy to locally build and test 
    BrightstarDB. It does not contain targets for building release packages. The
    process for building a full release is a little more involved and requires
    more pre-requisites to be installed. This is documented below.
    

.. _VisualStudio_Solution_Files:

***************************************
 Visual Studio Solution Files
***************************************

    In addition to the MSBuild script, there are a number of separate Visual Studio
    solution (.sln) files in the code base that can be used to quickly start working
    with the BrighstarDB source code.

BrightstarDB Core Libraries
***************************

    The core BrightstarDB solution can be found at ``src\core\BrighstarDB.sln``. This solution
    will build BrightstarDB's .NET 4 assemblies as well as the BrightstarDB service components
    including the Windows service wrapper.
    
.. note::
    The BrightstarDB solution uses a some NuGet packages which are not stored in the Git 
    repository, so the first time you open the solution you will need to restore the
    missing packages. To do this, right-click on the solution in the Solution Explorer
    window in Visual Studio and select **Manage NuGet Packages for Solution...**. 
    In the dialog that opens you should see a message prompting you to restore the
    missing NuGet packages.
    
    Once the NuGet packages are restored you can build the entire solution either from
    within Visual Studio or from the command-line using the MSBuild tool.
    
Portable Class Libraries
************************

    The source code for the Portable Class Library and the platform-specific assemblies are all
    contained in ``src\portable``. There are three separate solution files.
    
    * portable.sln - this builds the core PCL assembly and the Desktop, Windows Phone, 
      Silverlight and Windows Store platform assemblies.
    
    * android.sln - this solution builds the core PCL assembly and the Android platform assembly only.
    
    * ios.sln - this solution builds the core PCL assembly and the iOS platform assembly only.


.. warning::

    All three Portable Class Library solutions are intended for use in Visual Studio 2013. 
    It has not been possible to make these solutions build under MonoDevelop / Xamarin Studio due to 
    some of the features used in the .csproj files.


    To build the Android libraries from source you will require an installation of Xamarin.Android at Indie level
    or above. Unfortunately once BrightstarDB is included the built application size will
    exceed the maximum supported by the Free version of Xamarin.Android.

    To build the iOS libraries from source you will require an installation of Xamarin.iOS. This
    configuration has not been tested in the free version of Xamarin.iOS.
    
    As with the core solution, the portable class library solution has some NuGet 
    dependencies which need to be downloaded. Follow the same steps outlined above
    for the core solution to download and install the dependencies before trying
    to build this solution from the command line.
    
    This solution also requires that you have a Windows 8 developer license installed.
    You should be prompted by to retrieve and install this license if 
    necessary when you first open the solution file in Visual Studio.
    
    
.. _Build_BuildingTheTools:

Tools
*****

    The ``src\tools`` directory contains a number of command-line and GUI tools
    including the Polaris management console. Each subdirectory contains its
    own Visual Studio solution file. As with the core solution, NuGet packages
    may need to be restored, so when opening the solution file for the first time
    right-click on the solution in the Solution Explorer window and select 
    **Manage NuGet Packages for Solution...** and if necessary follow the prompt
    to download an install missing NuGet packages.

.. _Build_BuildingTheDocumentation:

****************************
 Building The Documentation
****************************

    Documentation for BrightstarDB is in two separate parts. 
    
**Developers Guide / User Manual**

    The developer and
    user manual (this document) is maintained as RestructuredText files and
    uses Sphinx to build.
    
    Details on getting and using Sphinx can be found at http://sphinx-doc.org/.
    Sphinx is a Python based tool so it also requires a Python installation on
    your machine. You may just find it easier to get the pre-built documentation
    from http://brightstardb.readthedocs.org/
    
**API Documentation**

    The API documentation is generated using Sandcastle Help File Builder. You can
    get the installer for SHFB from http://shfb.codeplex.com/. The .shfbproj file
    for the documentation is at ``doc/api/BrightstarDB.shfbproj``. To build the
    documentation using this project file you must first build the Core in the
    Debug configuration.
    
.. _Build_BuildingThePackages:

******************************************
 Building Installation and NuGet Packages
******************************************

    An MSBuild project is provided to compile and build a complete release package
    for BrightstarDB. This project can be found at ``installer\installers.proj``.
    The project will build all of the libraries and documentation and then make
    MSI and NuGet packages.
    
.. note::
    Building the full installer solution requires all the pre-requisites listed
    above to be installed. It also requires that you have first restored NuGet
    dependencies in both the core solution and the tools solution as described
    in the sections above.
    
*********************
 Building Under Mono
*********************

There are some other factors to take into consideration when building using Mono - especially
if this is your first time using Mono under Linux. Please see :ref:`mono_build` in the 
section :ref:`BrightstarDB_Under_Mono`
