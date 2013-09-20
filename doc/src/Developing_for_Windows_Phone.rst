.. _Developing_for_Windows_Phone:

*****************************
 Developing for Windows Phone
*****************************


For Windows Phone 7 and Windows Phone 8 (WP) developers, BrightstarDB provides a fast, 
schema-less persistent data store, that is easily managed as part of the isolated storage for 
an app. When running on a phone, all the key features of BrighstarDB are available, including 
the :ref:`Data Object Layer <Data_Object_Layer>` and the :ref:`Entity Framework 
<Entity_Framework>` as well as the :ref:`RDF Client API <RDF_Client_API>`. This section covers 
the main differences with the .NET 4.0 version of BrightstarDB and some important 
considerations when writing your WP7 app to use BrightstarDB. The SDK provides libraries that 
are compatible with Windows Phone 7.1 and Windows Phone 8, so all apps you develop with 
BrightstarDB will need to target that version of the Windows Phone OS.


Data Storage And Connection Strings
===================================


When running on WP, BrightstarDB writes its data using the IsolatedStorage APIs. This means 
that a BrightstarDB store opened within an application will be written into the 
IsolatedStorage for that application. This keeps the stores used by different applications 
separate from each other. An application can also use multiple stores, as long as each store 
has a unique store name. As the BrightstarDB server is not running on the phone, the only 
access to the store is by using the embedded connection type. A typical connection string used 
in a WP application is shown in the code snippet below:::

  var connectionString = "type=embedded;storesdirectory=brightstar;storename=MyAppStore";


SDK Libraries
=============

The BrightstarDB libraries for WP are all contained in [INSTALLDIR]\\SDK\\WP71. You need to add 
references to these libraries to your WP application project.


Development Considerations
==========================

For the most part, working with BrightstarDB on Windows Phone is the same as working with it 
on the full .NET Framework. However due to the platform and some .NET restrictions there are a 
few things that you need to keep in mind when building your application.

Store Shutdown
--------------

Because BrightstarDB uses separate threads to process updates to its stores, it is necessary 
for any WP app that uses BrightstarDB to cleanly shutdown the database when the application is 
not in use. The easiest way to achieve this is to add code to the Application_Deactivated and 
Application_Closing methods in the main application class as shown below. There is no 
corresponding global startup required as BrightstarDB will automatically start the required 
threads the first time you access a store.

::

  // Code to execute when the application is deactivated (sent to background)
  // This code will not execute when the application is closing
  private void Application_Deactivated(object sender, DeactivatedEventArgs e)
  {
      BrightstarService.Shutdown(true);
  }


  // Code to execute when the application is closing (eg, user hit Back)
  // This code will not execute when the application is deactivated
  private void Application_Closing(object sender, ClosingEventArgs e)
  {
      BrightstarService.Shutdown(true);
  }



EntityFramework Interfaces Must Be Public
-----------------------------------------

Due to differences between the .NET Framework and Silverlight, there are is one known 
limitation on the Entity Framework. All interfaces that are marked with the [Entity] attribute 
must be public interfaces when building a Windows Phone application. This is because 
Silverlight prevents reflection on internal classes / interfaces for reasons of code security.


.. _Deploying_a_Reference_Store:


Deploying a Reference Store
===========================

As well as using BrightstarDB to store user-modifiable data, you can also ship reference data 
with your application. A BrightstarDB reference store can be embedded as part of your 
application content and deployed to the Isolated Storage on the mobile device. Once deployed, 
the store can be queried and/or updated through your code as normal. The basic steps to 
deploying a store in a mobile application are as follows:

  1. Create the reference store

  #. Add the reference store files to your application and compile it

  #. Deploy the application to the device

  #. At runtime, copy the reference store files from the application directory to Isolated Storage

  #. Access the copied store from your code


Create The Reference Store
--------------------------

BrightstarDB stores are fully portable between the desktop and a mobile device through simple 
file copy. You can create and update a BrightstarDB database using a .NET application on a 
desktop workstation or a server and use the database files on a mobile device without the need 
for any conversion.

.. note::

  If the database you are deploying has been through a number of update transactions you may 
  want to consider creating a coalesced copy of the database for deployment purposes. 
  Coalescing the database will reduce the database size by copying only the current state of 
  the database and removing all the historical states of the data.


Add Database File To Your Application
-------------------------------------

Every BrightstarDB store exists in its own folder and contains at least the following files:

  - master.bs

  - data.bs

  - resources.bs

  - transactionheaders.bs

  - transactions.bs


For normal operation you only need to add the master.bs, resources.bs and data.bs files to 
your solution. The transactionheaders.bs and transactions.bs files are required only if your 
application will need to replay the transactions that built the database.

To add the reference database to your application

  1. With Visual Studio, create a project for the Windows Phone application that consumes the 
     reference store.

  #. From the Project menu of the application, select **Add Existing Item**.

  #. From the **Add Existing Item** menu, select the ``master.bs`` file for the BrightstarDB store 
     that you want to add, then click **Add**. This will add the local file to the project.

  #. In Solution Explorer, right-click the local file and set the file properties so that the 
     file is built as Content and always copied to the output directory (Copy always).

  #. Repeat steps 3 and 4 for the data.bs file and ``resources.bs`` file

  #. Optionally repeat steps 3 and 4 for ``transactionheaders.bs`` and ``transactions.bs``

.. note::

  It is good practice to put the BrightstarDB data files you are copying into a folder under 
  your project. If you want to deploy multiple reference databases, you will have to put the 
  files for each store in a separate folder to avoid name clashes. The folders you define in 
  your project will be mirrored in the installation directory when the application is deployed.


Deploy Application
------------------

Compile and deploy your application as normal. The store files you have copied will be 
available in the installation directory of the application (under the folders that you created 
in the project if applicable).


Copy Database Files To Isolated Storage
---------------------------------------

BrightstarDB on a mobile device will only access stores from a named directory in the 
application's Isolated Storage. It is therefore necessary when your application starts up to 
ensure that the data is copied or moved to Isolated Storage. Each BrightstarDB store you 
deploy must be in its own named directory, and those directories must in turn be in a named 
directory under the Isolated Storage root folder. These directory names are important as they 
form the values in the connection string you provide to BrightstarDB. The directory structure 
used by the sample application is shown below:

::

  IsolatedStorageFile Root
  |
  +-brightstar    <-- the storesDirectory value in the connection string, create a sub
    |                 create one sub-directory for each store you want to access
    |
    +-dining      <-- the storeName value in the connection string,
                      only the files for a single store should go in here

The precise way you choose to deploy or update the BrightstarDB store files is up to you, but 
the simplest method (as shown in the sample code) is to check for the presence of the store 
and if it is not there, copy the files from the application installation directory to Isolated 
Storage. The code to do this in the sample can be found in the ``App()`` constructor in the 
``App.xaml.cs`` file::

  if (!BrightstarDbDeploymentHelper.StoreExists("brightstar", "dining"))
  {
      BrightstarDbDeploymentHelper.CopyStore("data", "brightstar", "dining");
  }


The helper class can also be found in the sample project and has the following methods::

  public static class BrightstarDbDeploymentHelper
  {
      public static bool StoreExists(string storeDirectoryName, string storeName)
      {
          IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
          return iso.DirectoryExists(storeDirectoryName + "\\\\" + storeName) &&
                 iso.FileExists(storeDirectoryName + "\\\\" + storeName + "\\\\master.bs") &&
                 iso.FileExists(storeDirectoryName + "\\\\" + storeName + "\\\\resources.bs") &&
                 iso.FileExists(storeDirectoryName + "\\\\" + storeName + "\\\\data.bs");
      }


      public static void CopyStore(string resourceFolderPath, 
                                   string storeDirectoryName, 
                                   string storeName)
      {
          IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
          CopyStoreFile(iso, "data.bs", resourceFolderPath, storeDirectoryName, storeName);
          CopyStoreFile(iso, "master.bs", resourceFolderPath, storeDirectoryName, storeName);
          CopyStoreFile(iso, "resources.bs", resourceFolderPath, storeDirectoryName, storeName);
      }


      private static void CopyStoreFile(IsolatedStorageFile iso, string fileName, 
                                        string resourceFolderPath,
                                        string storeDirectoryName, string storeName)
      {
          if (!iso.DirectoryExists(storeDirectoryName))
          {
              iso.CreateDirectory(storeDirectoryName);
          }
          if (!iso.DirectoryExists(storeDirectoryName + "\\\\" + storeName))
          {
              iso.CreateDirectory(storeDirectoryName + "\\\\" + storeName);
          }


          // Create a stream for the file in the installation folder.
          var appResource =
              Application.GetResourceStream(
                new Uri(resourceFolderPath + "\\\\" + fileName, UriKind.Relative));
          if (appResource != null)
          {
              using (Stream input = appResource.Stream)
              {
                  // Create a stream for the new file in isolated storage.
                  using (
                      IsolatedStorageFileStream output =
                          iso.CreateFile(storeDirectoryName + "\\\\" + storeName + "\\\\" + fileName))
                  {
                      // Initialize the buffer.
                      var readBuffer = new byte[4096];
                      int bytesRead = -1;
                      // Copy the file from the installation folder to isolated storage. 
                      while ((bytesRead = input.Read(readBuffer, 0, readBuffer.Length)) > 0)
                      {
                          output.Write(readBuffer, 0, bytesRead);
                      }
                  }
              }
          } 
          else
          {
              // There is no application resource for this file, so create it as an empty file 
  
              iso.CreateFile(storeDirectoryName + "\\\\" + storeName + "\\\\" + fileName).Close();
          }
      }
  }


Access Reference Database Files
-------------------------------

Once deployed to Isolated Storage, the BrightstarDB store can be accessed as normal. You can 
use the RDF API, DataObjects API or EntityFramework to access the data depending on your 
application requirements. The connection string used to access the store is as follows::

  type=embedded;storesDirectory={path to directory containing store directories};storeName={name of store directory}

With our sample application, the store is contained in a directory named "dining", which is 
itself contained in a directory named "brightstar", so the full connection string is::

  type=embedded;storesDirectory=brightstar;storeName=dining

When the sample application runs, you should see a listing of top restaurants generated from a 
LINQ query against the EntityFramework.