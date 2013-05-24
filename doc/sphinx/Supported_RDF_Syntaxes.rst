.. _Supported_RDF_Syntaxes:

#######################
 Supported RDF Syntaxes
#######################

As BrightstarDB is built on the W3C RDF data model, we also provide the ability to import and export your data as RDF. 



BrightstarDB supports a number of different RDF syntaxes for file-based import. This list of supported file formats applies both to import jobs created using the BrightstarDB API (see :ref:`RDF Client API <RDF_Client_API>` for details), and to file import using Polaris (see :ref:`Polaris Management Tool <Using_Polaris>` for details). To determine the parser to be used, BrightstarDB checks the file extension, so it is important to use the correct file extension for the syntax you are importing. The supported syntaxes and their file extensions are listed in the table below as shown, BrightstarDB also supports reading from files that are compressed with the GZip compression method.



==========  =============================  ================================  
RDF Syntax  File Extension (uncompressed)  File Extension (GZip compressed)  
==========  =============================  ================================  
NTriples  .nt  .nt.gz  
NQuads  .nq  .nq.gz  
RDF/XML  .rdf  .rdf.gz  
Turtle  .ttl  .ttl.gz  
RDF/JSON  .rj or .json  .rj.gz or .json.gz  ==========  =============================  ================================  



