.. _Known_Issues:

#############
 Known Issues
#############

.. _http://www.w3.org/TR/WD-html40-970708/sgml/entities.html: http://www.w3.org/TR/WD-html40-970708/sgml/entities.html



***************
 SPARQL Queries
***************




When using the less-than (<) symbol in SPARQL queries, it is necessary to put spaces between the symbol and the rest of the query to avoid a parser error. For example the following query will fail with a parser error:::

  SELECT ?p ?s WHERE { ?p a <http://example.org/schema/person> . ?p <http://example.org/schema/salary> ?s . **FILTER (?s<50000)**  } 

but the same query written as shown below will be processed correctly.::

  SELECT ?p ?s WHERE { ?p a <http://example.org/schema/person> . ?p <http://example.org/schema/salary> ?s . **FILTER (?s < 50000)**  }




*************************
 Entity Framework Tooling
*************************


'_' underscore characters are not allowed in the names of the namespace(s) containing the interfaces that are to be generated into entity classes.



Currently only the following versions of Visual Studio are provisioned with the Entity Framework item templates through the installer:



  - Visual Studio C# Express 2010

  - Visual Studio 2010 Professional and above

  - Visual Studio 2012 Professional and above



To create an entity context class in other versions of Visual Studio, we recommend that you copy the .tt file from one of the Entity Framework samples into your own project. You may rename the file if you wish as long as you retain the .tt file extension.




****************
 OData Functions
****************


The filter function 'replace' is not supported. 




*******************************************
 Avoid HTML Named Entities in String Values
*******************************************


Using HTML named entities in string values that are not also valid XML named entities will result in errors when parsing the SPARQL results if these string values are included in the results set. Examples of such entities are &pound; for a pound-symbol, &copy; for a copyright symbol etc. It is best to avoid this situation by converting all HTML named entities to their numeric entity form before storing them in BrightstarDB (e.g. &#163; instead of &pound;). A full list of HTML named entities and their numeric equivalents for HTML 4 can be found at `http://www.w3.org/TR/WD-html40-970708/sgml/entities.html`_.