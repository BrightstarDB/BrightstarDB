.. _SPARQL_Endpoint:

################
 SPARQL Endpoint
################

BrightstarDB comes with a separate IIS service that exposes a SPARQL endpoint. The SPARQL endpoint supports update and query as specified in the SPARQL 1.1 W3C recommendation.




**************
 Configuration
**************


The SPARQL endpoint is provided as a ready to run IIS service. To configure the service following these steps:



  1. Open IIS Management studio and either create a new Website or a new Application under the default site.

  #. Set the 'Physical Path' to point to [INSTALLDIR]\SparqlService

  #. Ensure that the Application Pool for the service has the required access rights to the [INSTALLDIR]\SparqlService folder.

  #. In the [INSTALLDIR]\SparqlService\web.config file set the BrightstarDB.ConnectionString to point at a running BrightstarDB service. By default it connects to an HTTP service running on the same machine.




******
 Usage
******


The SPARQL service accepts both query and update operations. The following URI patterns are supported.



**Query**

GET /{storename}/sparql?query={query expression}



Will execute the query provided as the query parameter value against the store indicated.



POST /{storename}/sparql



Will look for the query as an unencoded value in the request body.



**Update**

POST /{storename}/update



Will execute the update provided as the value in the request body.






**************
 Customization
**************


The source code for the SPARQL endpoint is provided in the sample folder. It is provided to allow for customization and configuration of additional security options. 



