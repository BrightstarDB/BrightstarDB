.. _SPARQL_Endpoint:

################
 SPARQL Endpoint
################

The BrightstarDB service supports query, update and the graph store protocols as specified in the SPARQL 1.1 W3C recommendation. 
Each BrightstarDB store has its own endpoints for query, update and graph management.

With the BrightstarDB service is accessible at ``{service}``, the following URI patterns are supported:

******
 Query
******

    ``GET {service}/{storename}/sparql?query={query expression}``

Will execute the query provided as the query parameter value against the store indicated.

    ``POST {service}/{storename}/sparql``

Will look for the query as an unencoded value in the request body.

*******
 Update
*******

    ``POST {service}/{storename}/update``

Will execute the update provided as the value in the request body.

********************
Graph Store Protocol
********************

    ``GET {service}/{storename}/graphs?graph=default``
    
Will retrieve the content of the default graph of the store. Use the Accept header to specify the RDF format for the serialization.

    ``GET {service}/{storename}/graphs?graph={graph uri}``
    
Will retrieve the content of the named graph with URI {graph uri}.

    ``GET {service}/{storename}/graphs``

Will retrieve a list of the URIs of all named graphs in the store. The list is returned as a SPARQL results set with a single variable 
named "graphUri". You can must specify the format of the results by setting the Accept header of the request to one of the 
:ref:`supported SPARQL result formats <sparql_results_formats>`.
    
    ``PUT {service}/{storename}/graphs?graph=default``
    ``PUT {service}/{storename}/graphs?graph={graph uri}``
    
Will replace the content of the default graph or the named graph with the RDF contained in the body of the request.
If a named graph does not already exist, it will be created by this operation. 

    ``POST {service}/{storename}/graphs?graph=default``
    ``POST {service}/{storename}/graphs?graph={graph uri}``

Will merge the RDF contained in the body of the request with the existing triples in the default graph or the named graph of the store.
As with the PUT operation, if a named graph does not exist, it will be created.

.. note::

    Both PUT and POST operations return a HTTP 204 (No Content) to indicate success when modifying an existing graph, or HTTP 201 (Created) 
    when the operation results in the creation of a new named graph.

Finally,

    ``DELETE {service}/{storename}/graphs?graph=default``
    ``DELETE {service}/{storename}/graphs?graph={graph uri}``
    
Will delete the content of the default graph or remove the named graph entirely from the store.

.. note::

    The BrightstarDB implementation of the Graph Store Protocol does not currently support Direct Graph Identification.
    
.. _sparql_results_formats:

*********************
SPARQL Result Formats
*********************

BrightstarDB currently supports returning SPARQL results in the following formats:

============================== ================================ =================================================
Format                         Preferred Mime Type              Alternate Mime Types
============================== ================================ =================================================
SPARQL XML [xml]_              application/sparql-results+xml   application/xml
SPARQL JSON [json]_            application/sparql-results+json  application/json
CSV [csv]_                     text/csv
TSV [csv]_                     text/tab-separated-values
============================== ================================ =================================================

When using a CONSTRUCT or a DESCRIBE query, results are returned in RDF. BrightstarDB currently supports returning RDF
in the following formats:

============================== ======================== =====================================================================
Format                         Preferred Mime Type      Alternate Mime Types
============================== ======================== =====================================================================
RDF/XML                        application/rdf+xml      application/xml
NTriples                       text/ntriples            text/ntriples+turtle, application/rdf-triples, application/x-ntriples
Turtle                         application/x-turtle     application/turtle
N3                             text/rdf+n3              
TriX                           application/trix         
RDF/JSON                       text/json                application/rdf+json
============================== ======================== =====================================================================

**SPARQL Query Results Recommendations**

.. [xml] `SPARQL Query Results XML Format (Second Edition) <http://www.w3.org/TR/rdf-sparql-XMLres/>`_
.. [json] `SPARQL Query Results JSON Format <http://www.w3.org/TR/sparql11-results-json/>`_
.. [csv] `SPARQL 1.1 Query Results CSV and TSV Formats <http://www.w3.org/TR/sparql11-results-csv-tsv/>`_

****************
 Further Reading
****************

For full details on these protocols, please refer to the `SPARQL 1.1 Protocol <http://www.w3.org/TR/sparql11-protocol/>`_ and 
`SPARQL 1.1 Graph Store Protocol <http://www.w3.org/TR/sparql11-http-rdf-update/>`_ recommendations.

