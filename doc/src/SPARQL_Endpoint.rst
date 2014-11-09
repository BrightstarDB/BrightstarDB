.. _SPARQL_Endpoint:

################
 SPARQL Endpoint
################

The BrightstarDB service supports query and update as specified in the SPARQL 1.1 W3C recommendation. Each 
BrightstarDB store has its own endpoints for query and for update.


******
 Usage
******


The SPARQL service accepts both query and update operations. With the BrightstarDB service is accessible at {service},
the following URI patterns are supported:


**Query**


    ``GET {service}/{storename}/sparql?query={query expression}``

Will execute the query provided as the query parameter value against the store indicated.

    ``POST {service}/{storename}/sparql``

Will look for the query as an unencoded value in the request body.


**Update**

    ``POST {service}/{storename}/update``


Will execute the update provided as the value in the request body.

For full details on these protocols, please refer to the `SPARQL 1.1 Protocol <http://www.w3.org/TR/sparql11-protocol/>`_ recommendation.

.. note::

    The SPARQL 1.1 Graph Store Protocol is not implemented at this time.
