.. _Windows_Universal_Apps:

**************************************************
 Building Windows Universal Apps with BrightstarDB
**************************************************

Getting Started
===============

.. note::
	The following assumes you are using VS2015 with Update 1 applied.

BrightstarDB can be used in a Windows Universal Platform application targeting Windows 10, but
there are a couple of things to be aware of when starting a project:

	#. These projects must use the Portable Class Library "flavour" of BrightstarDB,
	   so you must add the NuGet package ``BrightstarDB.Platform`` to your project 
	   (this will automatically pull in the BrightstarDB and BrightstarDBLibs packages
	   as dependencies)
	   
	#. NuGet does not currently add package content files to UWP projects. This means that 
	   If you want to use the BrightstarDB Entity Framework, you will need to manually copy the 
	   T4 text template into your project. If you have installed BrightstarDB from the Windows 
	   Installer, you can find a copy of the text template in ``[INSTALLDIR]\\SDK\\EntityFramework``. 
	   Otherwise you can download the most recently released version from 
	   `GitHub <https://raw.githubusercontent.com/BrightstarDB/BrightstarDB/master/src/tools/EntityFrameworkGenerator/MyEntityContext.tt>`_

	#. The Roslyn-based code generator does not currently work with solutions that use the project.json
	   project file format. This is due to limitations with the current version of Roslyn.

Other Tips
==========

Beware of File Path Restrictions
--------------------------------

If your application is using an embedded BrightstarDB database, then you must ensure that the path to the
BrightstarDB data directory specified in the connection string is accessible to the application. UWP apps
run in a sand-boxed environment and have more restricted access to folders on the host machine.

Shutdown BrightstarDB on Suspend
--------------------------------

This is particularly an issue if your application is using an embedded BrightstarDB database. The embedded
server will start one or more background threads to process jobs. When your application receives notification
to suspend it should close down these background threads. This can be done with code like the following in
your main application class (typically App.xaml.cs)

.. code-block:: c#

	public App()
	{
		this.InitializeComponent();
		this.Suspending += OnSuspending;
	}
	
	private void OnSuspending(object sender, SuspendingEventArgs e)
	{
		var deferral = e.SuspendingOperation.GetDeferral();
		
		// Shutdown the embedded server and release resources
		BrightstarDB.Client.BrightstarService.Shutdown(true);
		
		deferral.Complete();
	}