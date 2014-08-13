.. _Building_BrightstarDB:

:title: Building BrightstarDB!

######################
 Building BrightstarDB
######################

This section will take you through the steps necessary to build BrightstarDB from source.

.. _Build_Prerequisites:

**************
 Prerequisites
**************

Before you can build BrightstarDB you need to install the following tools.

    1.  **Visual Studio 2012**
    
        You can use the Professional or Ultimate editions to build everything.
        
        *TBD: Check what can be built with VS 2012 Express*
        
    #.  **MSBuild Community Tasks**
        
        You must install this from the MSI installer which can be found at
        http://code.google.com/p/msbuildtasks/downloads/list. The MSI
        installer will install the MSBuild Community Tasks extension in the
        right place for it to be found by our build scripts.
        
    #.  **OPTIONAL: WiX**
        
        WiX is required only if you plan to build the installer packages for
        BrightstarDB. You can download WiX from http://wixtoolset.org/. 
        The build is currently based on version 3.5 of WiX, but it is
        recommended to build with the latest version of WiX (3.7 at the time 
        of writing).
        
    #. **OPTIONAL: Xamarin.Android**
    
        Xamarin.Android is required only if you plan to build the Android package
        for BrightstarDB. Please read :ref:`BrightstarDB_Android` first!
        
.. note:
    Please note that you will require an internet connection when first building
    BrightstarDB, even after you have initially retrieved the source, as some 
    NuGet packages will need to be downloaded.
        
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

*********************
 MSBuild build.proj
*********************

The quickest and simplest way to build BrightstarDB is to use the build.proj MSBuild
script. This script is found in the top-level directory of the BrightstarDB source.

The script uses the following properties:

	Configuration
		The project configuration to be built. Can be either 'Debug' or 'Release'. Defaults to 'Debug'.
	
	NUnitPath
		The path to your NUnit bin directory. This is used when running the unit tests. Defaults to
		`C:\Program Files (x86)\NUnit 2.6.2\bin`
		
You can either override these properties on the command-line using /p:{Property}={Value} switches
or you can edit the build.proj file (the properties are defined at the top of the file).

The MSBuild script has the following targets:

	BuildAndTest
		Build Core, Mobile, Portable and Tools and run the unit tests. This is the default target
		that will be run if you don't specify a /t:{Target} switch on the command-line.
		
	BuildCore
		Performs a clean build of the core .NET 4.0 library only.
		
	BuildMobile
		Performs a clean build of the Windows Phone library only.
		
	BuildTools
		Performs a clean build of the Polaris tool.
		
	Test
		Run Core and Portable unit tests
		
	TestCore
		Run Core unit tests only
		
	TestPortable
		Run Portable unit tests only


.. note::
	The ``build.proj`` script is provided to make it easy to locally build and test 
	BrightstarDB. It does not contain targets for building release packages. The
	process for building a full release is a little more involved and requires
	more pre-requisites to be installed. This is documented below.
	
	
.. _Build_BuildingTheCore:

****************************
 Building The Core Solution
****************************

    The core BrightstarDB solution can be found at ``src\core\BrighstarDB.sln``. This solution
    will build BrightstarDB's .NET 4 assemblies as well as the BrightstarDB service components
    including the Windows service wrapper.
    
    The BrightstarDB solution uses a some NuGet packages which are not stored in the Git 
    repository, so the first time you open the solution you will need to restore the
    missing packages. To do this, right-click on the solution in the Solution Explorer
    window in Visual Studio and select **Manage NuGet Packages for Solution...**. 
    In the dialog that opens you should see a message prompting you to restore the
    missing NuGet packages.
    
    Once the NuGet packages are restored you can build the entire solution either from
    within Visual Studio or from the command-line using the MSBuild tool.
    
.. _Build_RunningTheUnitTests:

*************************
 Running the Unit Tests
*************************

    The core solution's unit tests are all written using the NUnit framework.
    The easiest way to run all the unit tests is to use the unit test project file from
    the command prompt. To do this, open a Visual Studio command prompt and
    cd to the ``src\core`` directory under the BrightstarDB source. Then run the unit
    tests with::

        msbuild unittests.proj
    
.. _Build_BuildingThePortableClassLibraries:

***************************************
 Building the Portable Class Libraries
***************************************

	The portable class library solution can be found at ``src\portable\portable.sln``.
	As with the core solution, the portable class library solution has some NuGet 
	dependencies which need to be downloaded. Follow the same steps outlined above
	for the core solution to download and install the dependencies before trying
	to build this solution from the command line.
	
	This solution also requires that you have a Windows 8 developer license installed.
	You should be prompted by to retrieve and install this license if 
	necessary when you first open the solution file in Visual Studio.
	
.. _Build_BuildingTheTools:

*********************
 Building The Tools
*********************

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
	
..note::
	
    Building the full installer solution requires all the pre-requisites listed
    above to be installed. It also requires that you have first restored NuGet
    dependencies in both the core solution and the tools solution as described
    in the sections above.
    
*********************
 Building Under Mono
*********************

Please see :ref:`mono_build` in the section :ref:`BrightstarDB_Under_Mono`
