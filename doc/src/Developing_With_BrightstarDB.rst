.. _Developing_With_BrightstarDB:

#############################
 Developing With BrightstarDB
#############################

This section takes you through all of the basic principles of working with the BrightstarDB 
APIs. 

BrightstarDB provides three different levels of API:

  1. At the highest level the :ref:`Entity Framework <Entity_Framework>` allows you to define 
     your application data model in code. You can then use LINQ to query the data and simple 
     operations on your application data entities to create, update and delete objects.

  2. The :ref:`Data Object Layer <Data_Object_Layer>` provides a simple abstract API for 
     dealing with RDF resources, you can retrieve a resource and all its properties with a single 
     call. This layer provides no direct query functionality, but it can be combined with the 
     SPARQL query functionality provided by the RDF Client API. This layer also has a separate 
     abstraction for use with :ref:`Dynamic Objects <Dynamic_API>`.

  3. The :ref:`RDF Client API <RDF_Client_API>` provides the lowest level interface to 
     BrightstarDB allowing you to add or remove RDF triples and to execute SPARQL queries.

If you are new to BrightstarDB and to RDF, we recommend you start with the Entity Framework 
and take a walk through our :ref:`Developer Quick Start <Developer_Quick_Start>`. If 
you are already comfortable with RDF and SPARQL you may wish to start with the lower level APIs.

If you are the kind of person that just likes to dive straight into sample code, please take a 
moment to read about Running the BrightstarDB Samples first.


.. toctree::
    
    Developer Quick Start <Developer_Quick_Start>
    Connection Strings <Connection_Strings>
    Store Persistence Types <Store_Persistence_Types>
    Running the BrightstarDB Samples <Running_The_Samples>
    BrightstarDB Entity Framework <Entity_Framework>
    Entity Framework Samples <Entity_Framework_Samples>
    Data Object Layer <Data_Object_Layer>
    RDF Client API <RDF_Client_API>
    Admin API <Admin_API>
    API Documentation <API_Documentation>
