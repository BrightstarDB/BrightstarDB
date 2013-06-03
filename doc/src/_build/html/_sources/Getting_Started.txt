.. _Getting_Started:

################
 Getting Started
################

Welcome to BrightstarDB, the NoSQL semantic web database for .NET. The 
documentation contains lots of examples and detailed information on all 
aspects of BrightstarDB. The following sections provide some gentle hints of 
where to look depending on what you are planning to do with BrightstarDB.

It's probably a good idea, no matter what you plan to use BrightstarDB for, 
to read the :ref:`Concepts <Concepts>` section and the :ref:`'Why 
BrightstarDB?' <Why_BrightstarDB_>` section to understand the architecture 
and ideas behind the technology.

If you just want to see the simplest example of creating a BrightstarDB 
Entity Data Model then jump straight to the :ref:`Developer Quick Start 
<Developing_with_BrightstarDB>`.

We hope you enjoy developing with BrightstarDB. Please consider joining our 
community of developers and users and share any questions or comments you may 
have.

**********
 Architect
**********

If you are an architect considering using BrightstarDB then the 
:ref:`Concepts <Concepts>` section is important. Following that skimming over 
the different APIs will give you an overview of the different tools that 
developers can use to work with BrightstarDB. The other sections that provide 
a good overview of BrightstarDB's capabilities and features are the :ref:`API 
Documentation <API_Documentation>`, :ref:`Admin API <Admin_API>` and 
:ref:`Polaris Management Tool <Using_Polaris>` sections.




*****
 Data
*****

If you are coming to BrightstarDB from an RDF perspective and want to work 
with RDF Data and SPARQL then the best place to start is the :ref:`Polaris 
Management Tool <Using_Polaris>`. This shows how to create a new store 
without code, load in RDF data, and execute queries and update transactions. 
Other sections of interest will probably be :ref:`SPARQL Endpoint 
<SPARQL_Endpoint>` and if you are writing code the :ref:`RDF Client API 
<RDF_Client_API>`.



**********
 Developer
**********


BrightstarDB provides several layers of API that are aimed at specific 
development activities or scenarios. There are three main API levels, Entity 
Framework, Data Objects and RDF.

**BrightstarDB Entity Framework & LINQ**

The BrightstarDB Entity Framework is a powerful and simple to use technology 
to quickly build a typed .NET domain model that can persist object state into 
a BrightstarDB instance.To use this you create a set of .NET interfaces that 
define the data model. The BrightstarDB tooling takes these definitions and 
creates concrete implementing classes. These classes can then be used in an 
application. The flexibility of the underlying storage makes evolving the 
model very easy and straight forward. BrightstarDB is optimized for 
associative data which provides a high performance when working with objects. 
As this is a fully typed domain model it also provides LINQ and OData support.

The main sections to see for developing .NET typed domain models are the 
:ref:`Developer Quick Start <Developing_with_BrightstarDB>` section, the 
section on the BrightstarDB :ref:`Entity Framework <Entity_Framework>`, and 
the :ref:`Entity Framework Samples <Entity_Framework_Samples>`.

**Data Objects & Dynamic**

When working with data that may change shape at runtime, or when a fixed 
typed domain model is not required, the Data Object and Dynamic APIs provide 
a generic object layer on top of the RDF data. This layer provides 
abstractions that allow the developer to treat collections of triples as the 
state of a generic object. The sections :ref:`Data Object Layer 
<Data_Object_Layer>` and :ref:`Dynamic API <Dynamic_API>` provide 
documentation and examples of this APIs.

**RDF & SPARQL**

To work programmatically with RDF, SPARQL, and SPARQL see update the 
:ref:`RDF Client API <RDF_Client_API>` and :ref:`SPARQL Endpoint 
<SPARQL_Endpoint>` sections. 

**Mobile Applications**

If you are building apps for Windows Phone devices, there is 
some additional information on this in the :ref:`Developing for Windows Phone 
<Developing_for_Windows_Phone_7>` section.
