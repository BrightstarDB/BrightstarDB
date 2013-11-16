.. _Running_BrightstarDB:

#######################
 Running BrightstarDB
#######################

BrightstarDB can be used as an embedded database or accessed via HTTP(S) as a RESTful 
web service. The REST service can be hosted in a number of different ways or it can be
run directly from the command-line.

***********************************
 Namespace Reservation
***********************************

The BrightstarDB server requires permission from the Windows system to start listening
for connections on an HTTP port. This permission must be granted to the user that 
the service runs as. When the BrightstarDB server is run as a service, this will be the 
service user. When the BrightstarDB server is run from a command line, it will be the
user who starts the command line shell.

To grant users the permission to listen for connections on a particular endpoint 
you must run the ``http add urlacl`` command in command prompt with elevated 
(Administrator) permissions.

If you use the default port and path for the BrightstarDB service, the following
command will grant all users the required permissions to start the service::

    netsh http add urlacl url=http://+:8090/brightstar/ user=Everyone

note:
    The BrightstarDB installer will automatically make the required reservation
    for running the BrightstarDB server as a Windows service using the default
    port (8090) and path (/brightstar/)
    
note:
    If you chose to host BrightstarDB in IIS or another web application host then
    the URL reservation will not be required as IIS (or the other host application)
    should manage this on your behalf.

*********************************************
 Running BrightstarDB as a Windows Service
*********************************************

The installer will create a windows service called "BrightstarDB". 
This exposes a RESTful HTTP service endpoint that can be used to access the database. 
The configuration for this service can be found in `BrightstarService.exe.config` in the 
`[INSTALLDIR]\\Service` folder.

*****************************************
 Running BrightstarDB as an Application
*****************************************

Running the service as an application rather than a Windows service can be done by running 
the `BrightstarService.exe` located in the `[INSTALLDIR]\\Service` folder. The configuration 
from the `BrightstarService.exe.config` file is used by the service when it starts up. However, 
some properties can also be overridden using command line parameters passed to the service. 
The format of the command-line is as follows::

  BrightstarService.exe [options]

Where ``options`` are:

    ``/c``, ``/ConnectionString``
        Provides the connection string used by the service to access the BrightstarDB stores.
        Typically this connection string should be an **embedded** connection string, but it 
        is not a requirement. If this option is specified on the command-line it overrides
        any setting contained in the application configuration file. If this option is not
        specified on the command-line then a value MUST be provided in the the application
        configuration file.
        
    ``/r``, ``/RootPath``
        Specifies the full file path to the directory containing the `Views` and `assets` folder
        for the service. The default path used is the path to the directory containing the
        BrightstarService.exe file itself. This should only need to be overridden in development
        environments where it can be used to serve views/assets directly from the source folders
        rather than from the bin directory.
        
    ``/u``, ``/ServiceUri``
        Specifies the base URI path that the service will listen on for connections. This 
        parameter can be repeated multiple times to create a service that will listen on
        multiple endpoints. The default value is "http://localhost:8090/brightstar/"

***********************************
 Running BrightstarDB In IIS
***********************************

    BrightstarDB can be hosted as a .NET 4.0 web application in IIS. If you have installed
    BrightstarDB from the installer, you will find a pre-built version of the web application
    in the `INSTALLDIR\\webapp` directory.
    
    You will need to ensure that the application pool that the web application runs under
    has the necessary privileges to access the directory where the BrightstarDB stores
    are kept. It is strongly advised that this directory should be outside the directory
    structure used for the IIS website itself.
    
***********************************
 BrightstarDB Service Configuration 
***********************************

The BrightstarDB server can also be configured from its application configuration file (or web.config
when hosted in IIS). This is achieved through a custom configuration section which must be registered.
This custom configuration section grants far more control over the configuration of the service
than the command line parameters and is the recommended way of configuring the BrightstarDB service.

The sample below shows a skeleton application configuration file with just the BrightstarDB configuration
shown::

    <configuration>
      <configSections>
        <section name="brightstarService" type="BrightstarDB.Server.Modules.BrightstarServiceConfigurationSectionHandler, BrightstarDB.Server.Modules"/>
      </configSections>

      <brightstarService connectionString="type=embedded;StoresDirectory=c:\brightstar">
        <storePermissions>
          <passAll anonPermissions="All"/>
        </storePermissions>
        <systemPermissions>
          <passAll anonPermissions="All"/>
        </systemPermissions>
      </brightstarService>
      
    </configuration>
    
Note that the configuration section must first be registered in the `configSections` element so that the correct
handler is invoked. The section itself consists of the following elements and attributes:

    `brightstarService`
        This is the root element for the configuration. It supports a number of attributes (documented below)
        and contains one or zero `storePermissions` elements and one or zero `systemPermissions` elements.
        
    `brightstarService/@connectionString`
        This attribute specifies the connection string that the BrightstarDB service will use to connect
        to the stores it serves. The attribute value must be a valid BrightstarDB connection string. 
        Typically the connection type will be embedded, but this is not required. See the section
        :ref:`Connection_Strings` for more information about the format of BrightstarDB connection
        strings.
        
    `storePermissions`
        This element is the root element for configuring the way that the BrightstarDB service manages
        store access permissions. See :ref:`Configuring Store Permissions` for more details.
        
    `systemPermissions`
        This element is the root element for configuring the way that the BrightstarDB service manages
        system access permissions.
        
.. _Configuring Store Permissions:

Configuring Store Permissions
=============================

When a user attempts to read or write data in a BrightstarDB store, the Store Permissions for that user
are checked to ensure that the user has the required privileges. Store Permissions for a user are 
provided by a Store Permissions Provider, and a user may have different permissions for each store
on the BrightstarDB server. For more information about Store Permissions and providers
please refer to the :ref:`Store Permissions` section of the :ref:`BrightstarDB Security` documentation.

The permissions that a user has are provided to the BrightstarDB service by one or more configured 
*Store Permission Providers*. The following providers are available "out of the box":

    Fallback Provider
        This provider grants all users (authenticated or anonymous) a specific set of permissions. It
        is meant to be used in conjunction with a Combined Permissions Provider and some other 
        providers. The configuration element for a Fallback Provider is::
        
            <fallback authenticated="[Flags]" anonymous="[Flags]"/>

        where ``[Flags]`` is one or more of the store permissions levels. Multiple values must be separated by the
        comma (,) character (e.g. "Read,Export"). The ``anonymous`` attribute can be ommitted, in which
        case anonymous users will be granted no store permissions.
            
    Combined Permissions Provider
        This provider wraps two other providers and grants a user the combination of all permissions
        granted by the two child providers. You can use this to combine a custom permissions provider
        and a Fallback or Pass All provider to provide a backstop set of permissions when your
        custom provider doesn't grant any at all. The configuration element for a Combined Permissions
        Provider is::
        
            <combine>[child providers]</combine>
        
        where ``[child providers]`` is exactly two XML elements one for each of the child permission
        providers.
        
.. _Configuring System Permissions:

Configuring System Permissions
==============================

System Permissions control the access of users to list, create and manage BrightstarDB stores. 
There is one set of System Permissions for a user on the BrightstarDB server. For more information
about System Permissions please refer to the :ref:`System Permissions` section of the 
:ref:`BrightstarDB Security` documentation.
        
The permissions that a user has are provided to the BrightstarDB service by one or more configured 
*System Permission Providers*. The following providers are available "out of the box":

    Fallback Provider
        This provider grants all users (authenticated or anonymous) a specific set of permissions. It
        is meant to be used in conjunction with a Combined Permissions Provider and some other 
        providers. The configuration element for a Fallback Provider is::
        
            <fallback authenticated="[Flags]" anonymous="[Flags]" />
        
        where ``[Flags]`` is one or more of the system permissions levels. Multiple values must be separated by the
        comma (,) character (e.g. "ListStores,CreateStore"). The ``anonymous`` attribute may be omitted
        in which case anonymous users will be granted no system permissions.
        
    Combined Permissions Provider
        This provider wraps two other providers and grants a user the combination of all permissions
        granted by the two child providers. You can use this to combine a custom permissions provider
        and a Fallback or Pass All provider to provide a backstop set of permissions when your
        custom provider doesn't grant any at all. The configuration element for a Combined Permissions
        Provider is::
        
            <combine>[child providers]</combine>
        
        where ``[child providers]`` is exactly two XML elements one for each of the child permission
        providers.
        
Additional Configuration Options
================================

A number of other aspects of BrightstarDB service operations can be configured by adding values to the
``appSettings`` section of the application configuration file. These are:        

  - ``BrightstarDB.LogLevel`` - configures the level of detail that is logged by the BrightstarDB application. The valid options are ERROR, INFO, WARN, DEBUG, and ALL.  For more information about logging and configuring where logs are written please refer to the section :ref:`Logging <Logging>`. For Windows Phone 7.1 this setting is fixed as ERROR and cannot be overridden.

  - ``BrightstarDB.TxnFlushTripleCount`` - specifies a batch size for importing large sets of triples. At the end of each batch BrightstarDB will perform housekeeping tasks to try to ensure a lower memory footprint. The default value is 10,000 on .NET 4.0. For applications that run on larger, more capable hardware (with available memory of 4GB or more) the value can usually be increased to 50,000 or even 100,000 - but it is worth testing the configured value before committing to it in deployment. For Windows Phone 7.1 this value is fixed as 1,000 and cannot be overridden.

  - ``BrightstarDB.PageCacheSize`` - specifies the amount of memory in MB to be used by the BrightstarDB store page cache. This setting applies only to applications that open a BrightstarDB store as the cache is used to cache pages of data from the data.bs and resources.bs data files. The default value is 2048 on .NET 4.0 and 4 on Windows Phone 7.1. Note that this memory is not all allocated on startup so actual memory usage by the application may initially be lower than this value.

  - ``BrightstarDB.ResourceCacheLimit`` - specifies the number of resource entries to keep cached for each open store. Default values are 1,000,000 on .NET 4.0 and 10,000 on Windows Phone.
  
  - ``BrightstarDB.EnableQueryCache`` - specifies whether or not the application should cache the results of SPARQL queries. Allowed values are "true" or "false" and the setting defaults to "true". Query caching is only available on .NET 4.0 so this setting has no effect on Windows Phone 7.1

  - ``BrightstarDB.QueryCacheDirectory`` - specifies the folder location where cached results are stored.

  - ``BrightstarDB.QueryCacheMemory`` - specifies the amount of memory in MB to be used by the SPARQL query cache. The default value is 256.

  - ``BrightstarDB.QueryCacheDisk`` - specifies the amount of disk space (in MB) to be used by the SPARQL query cache. The default value is 2048. The disk space used will be in a subdirectory under the location specified by the BrightstarDB.StoreLocation configuration property.

  - ``BrightstarDB.PersistenceType`` - specifies the default type of persistence used for the main BrighstarDB index files. Allowed values are "appendonly" or "rewrite" (values are case-insensitive). For more information about the store persistence types please refer to the section :ref:`Store Persistence Types <Store_Persistence_Types>`.

  - ``BrightstarDB.StatsUpdate.Timespan`` - specifies the minimum number of seconds that must pass between automatic update of store statistics.
  
  - ``BrightstarDB.StatsUpdate.TransactionCount`` - specifies the minimum number of transactions that must occur between automatic update of store statistics.

Example Server Configuration
============================

The sample below shows all the BrightstarDB options with usage comments. ::

  <?xml version="1.0"?>
  <configuration>
    <appSettings>

      <!-- The logging level for the server. -->
      <add key="BrightstarDB.LogLevel" value="ALL" />

      <!-- Indicates the number of triples in a transaction to process before doing a partial commit. 
           Larger numbers require more machine memory but result in faster transaction processing. -->
      <add key="BrightstarDB.TxnFlushTripleCount" value="100000" />

      <!-- Specifies the maximum amount of memory (in MB) to use for page caching. -->
      <add key="BrightstarDB.PageCacheSize" value="2048" />

      <!-- Enable (true) or disable (false) the caching of SPARQL query results -->
      <add key-"BrightstarDB.EnableQueryCache" value="true" />
      
      <!-- The amount of memory to use for the SPARQL query cache -->
      <add key="BrightstarDB.QueryCacheMemory" value="512" />

      <!-- The amount of disk space (in MB) to use for the SPARQL query cache. This only applies to server / embedded applications -->
      <add key="BrightstarDB.QueryCacheDisk" value="2048" />

      <!-- The default store index persistence type -->
      <add key="BrightstarDB.PersistenceType" value="AppendOnly" />

    </appSettings>
   
    <!-- Core BrightstarDB service configuration -->
    <brightstarService connectionString="type=embedded;StoresDirectory=c:\brightstar">

      <!-- Store Permissions Provider. -->
      <storePermissions>
        <!-- WARNING: This configuration Grants full access to all users -->
        <passAll anonPermissions="All"/>
      </storePermissions>

      <!-- System Permissions Provider -->
      <systemPermissions>
        <!-- WARNING: This configuration Grants full access to all users -->
        <passAll anonPermissions="All"/>
      </systemPermissions>

    </brightstarService>
  </configuration>


.. _Caching:

*********************
 Configuring Caching
*********************

BrightstarDB provides facilities for caching the results of SPARQL queries both in memory and to disk.
Caching complex SPARQL queries or queries that potentially return large numbers of results can provide
a significant performance improvement. Caching is controlled through a combination of settings in the 
application configuration file (the web.config for web apps, or the .exe.config for other executables).

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
    <configSections>
      <section name="brightstarService" type="BrightstarDB.Server.Modules.BrightstarServiceConfigurationSectionHandler, BrightstarDB.Server.Modules"/>
    </configSections>
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









