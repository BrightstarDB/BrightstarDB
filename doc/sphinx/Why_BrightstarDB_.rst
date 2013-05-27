.. _Why_BrightstarDB_:

##################
 Why BrightstarDB?
##################

BrightstarDB is a unique and powerful data storage technology for the .NET 
platform. It combines flexibility, scalability and performance while allowing 
applications to be created using tools developers are familiar with.




*********************
 An Associative Model
*********************

All databases adopt some fundamental world view about how data is stored. 
Relational databases use tables, and document stores use documents. 
BrightstarDB has adopted a very flexible, associative data model based on the 
W3C RDF data model.

BrightstarDB uses the powerful and simple RDF graph data model to represent 
all the different kinds of models that are to be stored. The model is based 
on a concept of a triple. Each triple is the assignment of a property to an 
identified resource. This simple structure can be used to describe and 
represent data of any shape. This flexibility means that evolving systems, or 
creating systems that merge data together is very simple. 

Few existing NoSQL databases offer a data model that understands, and 
automatically manages relationships between data entities. Most NoSQL 
databases require the application developer to take care of updating 'join' 
documents, or adding redundant data into 'document' representations, or 
storing extra data in a key value store. This makes many NoSQL databases not 
particularly good at dealing with many real word data models, such as social 
networks, or any graph like data structure.



***********************
 Schema-less Data Store
***********************

The associative model used in BrightstarDB means data can be inserted into a 
BrightstarDB database without the need to define a traditional database 
schema. This further enhances flexibility and supports solution evolution 
which is a critical feature of modern software solutions. 

While the schema-less data store enables data of any shape to be imported and 
linked together, application developers often need to work with a specific 
shape of data. BrightstarDB is unique in allowing application developers to 
map multiple .NET typed domain models over any BrightstarDB data store.  



**********************
 A Semantic Data Model
**********************

While many NoSQL databases are schema-less, few are inherently able to 
automatically merge together information about the same logical entity. 
BrightstarDB implements the W3C RDF data model. This is a directed graph data 
model that supports the merging of data from different sources without 
requiring any application intervention. All entities are identified by a URI. 
This means that all properties assigned to that identifier can be seen to 
constitute a partial representation of that thing.

This unique property makes BrightstarDB ideal for building enterprise 
information integration solutions where there is a fundamental need to bring 
together data about a single entity from many different systems.



***********************
 Automatic Data caching
***********************

Query results, and entity representations are cached to further improve 
performance for query intensive applications. Normally, data caching is done 
by applications but BrightstarDB provides this feature as a core capability. 



*****************************
 Full Historical Capabilities
*****************************

BrightstarDB uses a form of data storage that preserves full historical data 
at every transaction point. This allows applications to perform queries at 
any previous point in time, it ensures fully audit-able data and allows data 
stores to be returned to any previous state or snapshots taken at any point 
in time. This approach does increase the amount of disk space used, but 
BrightstarDB provides a feature to consolidate down to just the currently 
required data. 



***************************
 Developer Friendly Toolset
***************************

Most developers on .NET are accustomed to using objects and LINQ for building 
their applications. Database technologies that require a fundamental move 
away from this impose a large burden upon the developer. BrightstarDB 
provides a complete typed domain model interface to work with the data in the 
store. It adopts a unique position where the object model is an operational 
view onto the data. This means that many different object models can overlay 
the same semantic data model.



**********************************
 Native .NET Semantic Web Database
**********************************

If you are working on .NET and want the power and flexibility of a semantic 
web data store. Then BrightstarDB is a great place to start. With support for 
the SPARQL query language and also the NTriples data format building semantic 
web based applications is simple and fun with BrightstarDB.



****************************************************
 RDF is great for powering Object Oriented solutions
****************************************************

Objects are composed of properties, each property is either a literal value 
or a reference to another object. This creates a graph or related things with 
properties. ORM systems requires that tables are organised is specific ways 
to facilitate storing object state. Changes to either the object model or the 
relational schema often require a reciprocal change. RDF on the other hand 
can ideally be used to store both literal properties and object relationships 
and if the object model needs to change then new property value can be added 
as there is no fixed schema. Similalry, if additional RDF data is added to 
the store the object model can either ignore or make use of this data. In 
this way the object model is an operational, read/write, view of the RDF data.
