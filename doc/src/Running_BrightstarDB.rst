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
    
    For a step-by-step guide please refer to :ref:`BrightstarDB_In_IIS`
    
********************************
 Running BrightstarDB in Docker
********************************

From the 1.8 release we now provide pre-built `Docker <http://www.docker.com>`_ images to run the BrightstarDB service. 
Docker is an open platform for developers and sysadmins to build, ship and run distributed applications, whether on 
laptops, data center VMs, or the cloud.

The BrightstarDB Docker images are built on the most recent Ubuntu LTS and the most recent Mono stable
release. The Dockerfile and other configuration files can be found in `our Docker repository on GitHub <https://github.com/BrightstarDB/Docker>`_
where you will also find important information about how to configure and run the Docker images.

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
		<cors disabled="false">
			<allowOrigin>*</allowOrigin>
		</cors>
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
		
	`cors`
		This is the root element for configuring the way that the BrighstarDB REST server handles
		cross-origin resource sharing. See :ref:`Configuring_CORS` below.
        
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
        
    Static Provider
        This provider uses a fixed configuration that maps users or claims to permissions.
        The configuration element for a Static Permissions Provider is::
        
            <static>
                <store name="{storeName}">
                    <user name="{userName}" permissions="[Flags]" /> *
                    <claim name="{claimName}" permissions="[Flags]" /> *
                </store> *
            </static>
        
        where ``storeName`` is the name of the store that the permissions are granted on,
        ``userName`` and ``claimName`` are the names of a specific user or a claim that a
        user holds respectively, and ``[Flags]`` is one or more store permission levels.
        
        Depending on the user validation you use, the claim names may be specific claims
        about a user's identity (e.g. their email address) or about their group membership
        (e.g. group names) or both.
        
        Any number of ``store`` elements may appear inside the ``static`` element, and
        any number of ``user`` and ``claim`` elements may appear inside the ``store``
        element (in any order).
        
        
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
        
    Static Provider
        This provider uses a fixed configuration that maps users or claims to permissions.
        The configuration element for a Static Permissions Provider is::
        
            <static>
                <user name="{userName}" permissions="[Flags]" /> *
                <claim name="{claimName}" permissions="[Flags]" /> *
            </static>
        
        where ``userName`` and ``claimName`` are the names of a specific user or a claim that a
        user holds respectively, and ``[Flags]`` is one or more system permission levels.
        
        Depending on the user validation you use, the claim names may be specific claims
        about a user's identity (e.g. their email address) or about their group membership
        (e.g. group names) or both.
        
        Any number of ``user`` and ``claim`` elements may appear inside the ``static``
        element (in any order).
        
.. _Configuration_Authentication:

Configuring Authentication
==========================

Authentication is the process by which the server determines a user identity for an incoming
request. BrightstarDB has been developed to give as much flexibility as possible over how
the server authenticates a user, without (we hope!) making it to complicated to configure.

Authentication is a service that is implemented by an Authentication Provider. You can attach
multiple Authentication Providers to the BrightstarDB server and each one will attempt to 
determine the user identity from an incoming request. If none of the attached Authentication
Providers can determine the user identity, then the request is processed as if the user
were an anonymous user.

The list of Authentication Providers for the server are configured by adding an ``authenticationProviders``
element inside the ``brightstarService`` element of the configuration file. The ``authenticationProviders``
element has the following content::

    <authenticationProviders>
        <add type="{Provider Type Reference}"/> *
    </authenticationProviders>

where ``Provider Type Reference`` is the full class and assembly reference for the authentication provider
class to be used. An Authentication Provider class must implement the ``BrightstarDB.Server.Modules.Authentication.IAuthenticationProvider``
interface and it must also have a default no-args constructor. The ``add`` element used to add the provider
is passed to the provider instance after it is constructed so depending on the provider implementation
you may be allowed/required to add more configuration elements inside the ``add`` element. Check the 
documentation for the individual provider types below.

BrightstarDB provides the following implementations "out of the box":

    NullAuthenticationProvider
        Type Reference: ``BrightstarDB.Server.Modules.Authentication.NullAuthenticationProvider, BrightstarDB.Server.Modules``
        
        This provider does no authentication at all, so it is probably of very little interest!
        
    BasicAuthenticationProvider
        Type Reference: ``BrightstarDB.Server.Modules.Authentication.BasicAuthenticationProvider, BrightstarDB.Server.Modules``
        
        This provider authenticates a user by their credentials being passed using HTTP Basic Authentication. It uses NancyFX's
        Basic Authentication Module, which accepts a custom validator class which implements the logic that takes the user name
        and password provided and determines the user identity. This requires some additional configuration, so the 
        configuration for this provider follows this pattern::
        
            <add type="BrightstarDB.Server.Modules.Authentication.BasicAuthenticationProvider,
                       BrightstarDB.Server.Modules">
                <validator type="{Validator Type Reference}"/>
                <realm>{Authentication Realm}</realm> ?
            </add>
        
        Where ``Validator Type Reference`` is the full class and assembly reference for the validator class. A validator
        must implement the ``Nancy.Authentication.Basic.IUserValidator`` interface, which has a single method
        called Validate that receives the user name and password that the user entered and returns an IUserIdentity
        instance (or null if the username/password pair was not valid).
        
BrightstarDB provides the following "out of the box" validators:

    MembershipValidator
        Type Reference: ``BrightstarDB.Server.AspNet.Authentication, BrightstarDB.Server.AspNet``
        
        This provider uses the ASP.NET Membership and Roles framework to validate the user identity.
        To use this provider you must also configure at least a Membership Provider for the server
        and optionally a Role Provider. The validator will create a user identity where the validated
        user name from the request is mapped to the user name of the generated user identity, and the
        roles that the user is in are mapped to claims on the generated user identity.
        
An example ASP.NET-based BrightstarDB service is available in the source code for you to see how
all these pieces hang together (src\\core\\BrightstarDB.Server.AspNet.Secured).

.. note::
    Please note that at present there are no validator implementations available for BrightstarDB
    running as a Windows Service. The Membership and Role providers bring in a dependency on 
    ASP.NET that is not suitable for a Windows Service. A future release will address this 
    deficit, but for now if you want user authentication you will have to run the ASP.NET  
    implementation of the BrightstarDB server.

.. _Configuring_CORS:

Configuring CORS
================

Cross-Origin Resource Sharing (CORS) is the mechanism by which scripts in one domain can access services on another domain.
This allows a client-side web application such as a JS script that is served up from one domain
to make a request to a BrighstarDB server running on a different domain. By default a browser
will disallow this behaviour unless the server providing the resource enables CORS. 

BrightstarDB defaults to enabling cross-origin requests from any domain. This is equivalent
to setting the CORS "Access-Control-Allow-Origin" header to "*".

To restrict CORS to a specific domain, add the following snippet inside the ``brightstarService``
configuration section of the server's ``app.config`` (or ``web.config``) file::

	<cors>
		<allowOrigin>http://somedomain.com</allowOrigin>
	</cors>
    
To completely disable CORS, add the ``disabled`` attribute to the ``cors`` element and set its value to ``true``::

	<cors disabled="true"/>
	
.. _Additional_Configuration_Options:

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
  
  - ``BrightstarDB.QueryExecutionTimeout`` - specifies the amount of time (in milliseconds) that a SPARQL query is allowed to run for - queries that exceed this threshold will be aborted. This setting applies only to embedded stores - when connecting to a server, the query timeout is determined by the server configuration.

  - ``BrightstarDB.PersistenceType`` - specifies the default type of persistence used for the main BrighstarDB index files. Allowed values are "appendonly" or "rewrite" (values are case-insensitive). For more information about the store persistence types please refer to the section :ref:`Store Persistence Types <Store_Persistence_Types>`.

  - ``BrightstarDB.StatsUpdate.Timespan`` - specifies the minimum number of seconds that must pass between automatic update of store statistics.
  
  - ``BrightstarDB.StatsUpdate.TransactionCount`` - specifies the minimum number of transactions that must occur between automatic update of store statistics.

  - ``BrightstarDB.UpdateExecutionTimeout`` - specifies the amount of time (in milliseconds) that a SPARQL update is allowed to run for - updates that exceed this threshold will be aborted. This setting applies only to embedded stores - when connecting to a server, the query timeout is determined by the server configuration.

Example Server Configuration
============================

The sample below shows all the BrightstarDB options with usage comments. ::

  <?xml version="1.0"?>
  <configuration>
    <configSections>
      <!-- This configuration section is required to configure server security -->
      <section name="brightstarService" type="BrightstarDB.Server.Modules.BrightstarServiceConfigurationSectionHandler, BrightstarDB.Server.Modules" />
      <!-- This configuration section is required only for advanced configuration options 
           such as page-cache warmup -->
      <section name="brightstar" type="BrightstarDB.Config.BrightstarConfigurationSectionHandler, BrightstarDB" />
    </configSections>

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
    
    <brightstar>
    
      <!-- Enable page-cache warmup -->
      <preloadPages enabled="true" />
    
    </brightstar>
    
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


.. _Preloading_Stores:

******************
 Preloading Stores
******************

The BrightstarDB server can be configured to automatically preload the active pages from one
or more stores into the in-memory page-cache. Preloading the pages trades-off a slightly longer 
server start-up time for a reduced time to respond to the first incoming request. By default
preloading is disabled and pages will be pulled into the cache on an as-needed basis.

Configuring Basic Preloading
============================

As preloading is concerned with populating the BrightstarDB store page cache, it can only be
enabled on a BrightstarDB server that is using an embedded connection to a store directory.
Basic preloading will fill the cache with pages from all stores in the store directory in
an equal ratio, so if there are 10 stores in the directory, each will be allowed to use
up to 10% of the available cache. Basic preloading proceeds in order of store size
(from smallest to largest store based on their data file sizes), so if smaller stores
do not use up their full allocation of pages, the remaining space can be shared amongst
the remaining larger stores as they are pre-loaded. 

To enable basic preloading, the following needs to be added to the ``brightstar``
element in the server application (or web) configuration file::

  <preloadPages enabled="true" />

Advanced Preloading
===================

Basic preloading is a simple strategy that makes the assumption that all stores in a directory
are equally important - each is preloaded to the same extent. In some cases as an administrator
you may want to prioritize some stores over others. 

To allow for this you can assign one or more stores a cache ratio number. This number specifies the 
relative amount of page cache space to be assigned to the store, so a store with a cache ratio of 3 
gets 3x the pages that a store with a cache ratio of 1 is assigned, and 1.5x the pages that a store 
with a cache ratio of 2. By default all stores have a cache ratio of 1 assigned, but you can also
set this default to 0.

To configure advanced preloading you add a ``store`` element child to the ``preloadPages`` element
as shown here::

    <preloadPages enabled="true">
        <store name="storeA" cacheRatio="4" />
        <store name="storeB" cacheRatio="2" />
    </preloadPages>

To understand how cache ratios work, imagine that the server using this configuration is actually
serving 4 stores, storeA, storeB, storeC and storeD, and that the server is configured with a 
page cache size of 2048M As the default cache ratio for a store is 1, the effective ratios for 
the stores are:

========== ==============
Store Name Cache Ratio
========== ==============
storeA     4
storeB     2
storeC     1
storeD     1
========== ==============

The sum of those ratios is (4+2+1+1) = 8. So storeC and storeD are assigned one-eighth of the
page cache, storeB is assigned one-quarter and storeA one-half, making the assigned page cache
preload sizes:

========== ============== =================
Store Name Cache Ratio    Preload Size
========== ============== =================
storeA     4              1024M
storeB     2              512M
storeC     1              256M
storeD     1              256M
========== ============== =================

It is also possible to change the default cache ratio assigned to stores that are not explicitly
configured by adding a ``defaultCacheRatio`` attribute to the ``preloadPages`` element::

    <preloadPages enabled="true" defaultCacheRatio="2">
        <store name="storeA" cacheRatio="4" />
        <store name="storeB" cacheRatio="2" />
    </preloadPages>
    
The configuration above changes the cache preload sizes for the stores as follows:

========== ============== =================
Store Name Cache Ratio    Preload Size
========== ============== =================
storeA     4              819.2M
storeB     2              409.6M
storeC     2              409.6M
storeD     2              409.6M
========== ============== =================

It is also possible to use the ``defaultCacheRatio`` to disable preloading for stores
that are not explicitly named, by setting the default ratio to zero::

    <preloadPages enabled="true" defaultCacheRatio="0">
        <store name="storeA" cacheRatio="4" />
        <store name="storeB" cacheRatio="2" />
    </preloadPages>

This leads the the following preloaded cache sizes:

========== ============== =================
Store Name Cache Ratio    Preload Size
========== ============== =================
storeA     4              1365.3M
storeB     2              682.7M
storeC     0              0M
storeD     0              0M
========== ============== =================

.. _Controlling_Transaction_Logging:

********************
 Transaction Logging
********************

BrightstarDB provides a persistent text log of the transactions applied to a store. This log is contained in the file
``transactions.bs`` and is indexed by the file ``transactionheaders.bs``. The purpose of these files is to enable a 
transaction or set of transactions to be replayed at any time either against the same store or against another 
store as a form of data synchronization. The BrightstarDB API provides methods for accessing the index; retrieving
the data for specific transactions from the log files; and replaying transactions.

Disabling Transaction Logging
=============================

The ``transaction.bs`` file lists the RDF quads inserted and deleted by
each transaction executed against the store, and so over time this file can grow to be quite large. For this
reason, from release 1.9 of BrightstarDB it is now possible to control whether a store logs these transactions 
or not and it is possible for a BrightstarDB server (or embedded application) to control the default setting
for this configuration.

Disabling Store Logging
-----------------------

Transaction logging for an individual store is controlled by the existence of the ``transactionheaders.bs`` file
in the directory for the store. If this file exists when a job is processed, then the data for that job will be logged
to the ``transactions.bs`` file and an index entry appended to the ``transactionheaders.bs`` file. If the file does not 
exist when a job is processed, then no data will be logged for that job.

This makes it easy to disable logging on a store - simply delete (or rename) the ``transactionheaders.bs`` and ``transactions.bs``
files from the store's directory. In either case it is recommended to delete or rename the ``transactionheaders.bs`` file 
first.

Equally it is easy to enable logging on a store - simply create an empty file named ``transactionheaders.bs`` in the
store's directory. The ``transactions.bs`` file will be automatically created if it does not exist (if it does exist,
new transaction data will be logged to the end of the existing file).

Specifying the Server Default
-----------------------------

For regular Windows/Mono applications or web applications (i.e. those applications that can read from an ``app.config`` or
``web.config`` file), the default transaction logging configuration can be specified in the ``brightstar`` configuration section::

  <?xml version="1.0"?>
  <configuration>
    <configSections>
      <section name="brightstar" type="BrightstarDB.Config.BrightstarConfigurationSectionHandler, BrightstarDB" />
    </configSections>

    <appSettings>

        <!-- Other server configuration options can be specified here -->
    
    <brightstar>
    
      <!-- Disable transaction logging -->
      <transactionLogging enabled="false" />
    
    </brightstar>
    
  </configuration>
  

Alternatively (and for those platforms where there is no support for ``app.config files``), the configuration can be specified
programatically when creating the client by creating an instance of ``BrightstarDB.Config.EmbeddedServiceConfiguration`` and
passing that as the optional second parameter to the ``BrightstarService.GetClient()`` method::

    var client = BrightstarService.GetClient(myConnectionString,
        new EmbeddedServiceConfiguration(enableTransactionLoggingOnNewStores: false));

Note: These options merely set the default logging setting for newly created stores. In effect we are controlling whether or
not the `transactionheaders.bs` file is created when the store is first created. Logging for an individual store can still
be enabled or disabled by managing the `transactionheaders.bs` file as described in the section above.