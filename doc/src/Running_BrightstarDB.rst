.. _Running_BrightstarDB:

#######################
 Running BrightstarDB
#######################

BrightstarDB can be used as an embedded database or accessed as a WCF service. 
The WCF service can be hosted either in a Windows Service which can be configured
to automatically start when the host machine starts; or it can be run as a command-line
application. 

*********************************************
 Running BrightstarDB as a Windows Service
*********************************************

The installer will create a windows service called "BrightstarDB". 
This exposes a WCF service endpoint that can be used to access the database. 
The configuration for this service can be found in BrightstarService.exe.config in the 
`[INSTALLDIR]\Service` folder.

*****************************************
 Running BrightstarDB as an Application
*****************************************

Running the service as an application rather than a Windows service can be done by running 
the BrightstarService.exe located in the `[INSTALLDIR]\Service` folder. The configuration 
from the .config file is used by the service when it starts up. However, some properties 
can also be overridden using command line parameters passed to the service. 
Note that either no parameters are passed or all four parameters are required::

  BrightstarService.exe [<base location> <http port> <tcp port> <pipe name>]


  - <base location> specifies the path to the directory where the BrightstarDB stores are located. This overrides the BrightstarDB.StoreLocation configuration option.

  - <http port> specifies the port that the HTTP interface to the BrightstarDB service will use to listen for connections. This overrides the BrightstarDB.HttpPort configuration option.

  - <tcp port> specifies the port that the TCP interface to the BrightstarDB service will use to listen for connections. This overrides the BrightstarDB.TcpPort configuration option.

  - <pipe name> specifies the name of the named pipe that the named pipe interface to the BrightstarDB service will use to listen for connections. This overrides the BrightstarDB.NetNamedPipeName configuration option.

***********************************
 BrightstarDB Configuration Options
***********************************


The following list describes all the available configuration options for BrightstarDB.

  - BrightstarDB.StoreLocation - specifies the path to the directory where stores are persisted. For Windows Phone 7.1 this path is fixed as the directory "brightstar" in the isolated storage for the application, so this setting has no effect.

  - BrightstarDB.LogLevel - configures the level of detail that is logged by the BrightstarDB application. The valid options are ERROR, INFO, WARN, DEBUG, and ALL.  For more information about logging and configuring where logs are written please refer to the section :ref:`Logging <Logging>`. For Windows Phone 7.1 this setting is fixed as ERROR and cannot be overridden.

  - BrightstarDB.TxnFlushTripleCount - specifies a batch size for importing large sets of triples. At the end of each batch BrightstarDB will perform housekeeping tasks to try to ensure a lower memory footprint. The default value is 10,000 on .NET 4.0. For applications that run on larger, more capable hardware (with available memory of 4GB or more) the value can usually be increased to 50,000 or even 100,000 - but it is worth testing the configured value before committing to it in deployment. For Windows Phone 7.1 this value is fixed as 1,000 and cannot be overridden.

  - BrightstarDB.ConnectionString - specifies the default connection string to use when creating a BrightstarDB client. This setting can be used by applications that want to enable the user to choose the store that they connect to as a runtime configuration option.

  - BrightstarDB.PageCacheSize - specifies the amount of memory in MB to be used by the BrightstarDB store page cache. This setting applies only to applications that open a BrightstarDB store as the cache is used to cache pages of data from the data.bs and resources.bs data files. The default value is 2048 on .NET 4.0 and 4 on Windows Phone 7.1. Note that this memory is not all allocated on startup so actual memory usage by the application may initially be lower than this value.

  - BrightstarDB.ResourceCacheLimit - specifies the number of resource entries to keep cached for each open store. Default values are 1,000,000 on .NET 4.0 and 10,000 on Windows Phone.
  
  - BrightstarDB.EnableQueryCache - specifies whether or not the application should cache the results of SPARQL queries. Allowed values are "true" or "false" and the setting defaults to "true". Query caching is only available on .NET 4.0 so this setting has no effect on Windows Phone 7.1

  - BrightstarDB.QueryCacheDirectory - specifies the folder location where cached results are stored.

  - BrightstarDB.QueryCacheMemory - specifies the amount of memory in MB to be used by the SPARQL query cache. The default value is 256.

  - BrightstarDB.QueryCacheDisk - specifies the amount of disk space (in MB) to be used by the SPARQL query cache. The default value is 2048. The disk space used will be in a subdirectory under the location specified by the BrightstarDB.StoreLocation configuration property.

  - BrightstarDB.HttpPort - specifies the port number used by the BrightstarDB WCF service to listen for incoming HTTP requests. The default value is 8090.

  - BrightstarDB.TcpPort - specifies the port number used by the BrightstarDB WCF service to listen for incoming TCP requests. The default value is 8095.

  - BrightstarDB.NetNamedPipeName - specifies the name of the pipe used by the BrighstarDB WCF service to listen for incoming named pipe requests. The default value is "brightstar".

  - BrightstarDB.PersistenceType - specifies the default type of persistence used for the main BrighstarDB index files. Allowed values are "appendonly" or "rewrite" (values are case-insensitive). For more information about the store persistence types please refer to the section :ref:`Store Persistence Types <Store_Persistence_Types>`.


Example Configuration
======================

The sample below shows all the BrightstarDB options with usage comments. ::

  <?xml version="1.0"?>
  <configuration>
    <appSettings>
      <!-- The folder where stores are persisted, this is set by the installer but can be changed later. -->
      <add key="BrightstarDB.StoreLocation" value="C:\Program Files (x86)\BrightstarDB\Data" />


      <!-- The logging level for the server. -->
      <add key="BrightstarDB.LogLevel" value="ALL" />


      <!-- Indicates the number of triples in a transaction to process before doing a partial commit. 
           Larger numbers require more machine memory but result in faster transaction processing. -->
      <add key="BrightstarDB.TxnFlushTripleCount" value="100000" />


      <!-- For client applications this property value is used to connect to a store. See the section below for more detail on connection strings -->
      <add key="BrightstarDB.ConnectionString" value="Type=embedded;StoresDirectory=c:\brightstar;StoreName=test" />


      <!-- Specifies the maximum amount of memory (in MB) to use for page caching. -->
      <add key="BrightstarDB.PageCacheSize" value="2048" />


      <!-- Enable (true) or disable (false) the caching of SPARQL query results -->
      <add key-"BrightstarDB.EnableQueryCache" value="true" />
      
      <!-- The amount of memory to use for the SPARQL query cache -->
      <add key="BrightstarDB.QueryCacheMemory" value="512" />


      <!-- The amount of disk space (in MB) to use for the SPARQL query cache. This only applies to server / embedded applications -->
      <add key="BrightstarDB.QueryCacheDisk" value="2048" />


      <!-- Set the http port that the brightstar service runs on. default value is 8090. -->
      <add key="BrightstarDB.HttpPort" value="8090" />


      <!-- Set the tcp port that the brightstar service runs on. default value is 8095. -->
      <add key="BrightstarDB.TcpPort" value="8095" />


      <!-- Set the tcp port that the brightstar service runs on. default value is brightstar. -->
      <add key="BrightstarDB.NetNamedPipeName" value="brightstar" />


      <!-- The default store index persistence type -->
      <add key="BrightstarDB.PersistenceType" value="AppendOnly" />
    </appSettings>
  </configuration>


.. _Caching:

*********************
 Configuring Caching
*********************


BrightstarDB provides facilities for caching the results of SPARQL queries both in memory and to disk. Caching complex SPARQL queries or queries that potentially return large numbers of results can provide a significant performance improvement. Caching is controlled through a combination of settings in the application configuration file (the web.config for web apps, or the .exe.config for other executables).


**AppSetting Key**  **Default Value**  **Description**  
BrightstarDB.EnableQueryCache  false  Boolean value ("true" or "false") that specifies if the system should cache the result of SPARQL queries.  
BrightstarDB.QueryCacheMemory  256  The size in MB of the in-memory query cache.  
BrightstarDB.QueryCacheDirectory  <undefined>  The path to the directory to be used for the disk cache. If left undefined, then the behaviour depends on whether the BrightstarDB.StoreLocation setting is provided. If it is, then a disk cache will be created in the _bscache subdirectory of the StoreLocation, otherwise disk caching will be disabled.  
BrightstarDB.QueryCacheDiskSpace  2048  The size in MB of the disk cache.  


Example Caching Configurations
==============================

To cache in the _bscache subdirectory of a fixed store location (a good choice for server 
applications), it is necessary only to enable caching and ensure that the store location 
is specified in the configuration file::

  <configuration>
    <appSettings>
      <add key="BrightstarDB.EnableQueryCache" value="true" />
      <!-- disk cache will be written to the directory d:\brightstar\_bscache -->
      <add key="BrightstarDB.StoreLocation" value="d:\brightstar\" />
    </appSettings>
  </configuration>



To cache in some other location (e.g. a fast disk dedicated to caching)::

  <configuration>
    <appSettings>
      <add key="BrightstarDB.EnableQueryCache" value="true" />
      <add key="BrightstarDB.StoreLocation" value="d:\brightstar\" />


      <!-- Cache on a different disk from the B* stores to maximize disk throughput.
           Disk cache will be written to the directory e:\bscache -->
      <add key="BrightstarDB.QueryCacheDirectory" value="e:\bscache\"/>


      <!-- Allow disk cache to grow to up to 200GB in size -->
      <add key="BrightstarDB.QueryCacheDiskSpace" value="204800" /> 
    </appSettings>
  </configuration>



This sample has no disk cache because there is no valid location for the cache to be created::

  <configuration>
    <appSettings>
      <add key="BrightstarDB.EnableQueryCache" value="true" />
      <!-- 1GB in-memory cache -->
      <add key="BrightstarDB.QueryCacheMemory" value=1024"/>


      <!-- This property is not used because there is no 
            BrightstarDB.QueryCacheDirectory or
            BrightstarDB.StoreLocation setting defined. -->
      <add key="BrightstarDB.QueryCacheDiskSpace" value="204800" /> 


    </appSettings>
  </configuration>

  
  
.. _Logging:

*********************
 Configuring Logging
*********************


.. _TraceSource: http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx


BrightstarDB uses the .NET diagnostics infrastructure for logging. This provides a good deal 
of runtime flexibility over what messages are logged and how/where they are logged. All 
logging performed by BrightstarDB is written to a `TraceSource`_ named "BrightstarDB". 

The default configuration for this trace source depends on whether or not the 
`BrightstarDB.StoreLocation` configuration setting is provided in the application configuration 
file. If this setting is provided then the BrightstarDB trace source will be automatically 
configured to write to a log.txt file contained in the directory specified as the store location.
By default the trace source is set to log Information level messages and above.

Other logging options can be configured by entries in the <system.diagnostics> section of the 
application configuration file.

To log all messages (including debug messages), you can modify the TraceSource's `switchLevel`
as follows::

  <system.diagnostics>
    <sources>
      <source name="BrightstarDB" switchValue="Verbose"/>
    </sources>
  </system.diagnostics>

Equally you can use other switchValue settings to reduce the amount of logging performed by 
BrightstarDB.









