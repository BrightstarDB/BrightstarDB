.. _Advanced_Entity_Framework:

***************************
 Advanced Entity Framework
***************************

The BrightstarDB Entity Framework has a number of built in extension points
to enable developers to integrate their own custom SPARQL queries;
to override the SPARQL query and update protocols used;
or to change the way local C# types and properties are mapped to RDF types
and properties.

.. _EF_Filter_Optimization:

Filter Optimization
===================

The BrightstarDB Entity Framework can optionally "optimize" certain LINQ queries
to replace FILTER statements in the generated SPARQL with more efficient pattern
matches. This can greatly improve LINQ query performance in some circumstances.

As an example of this optimization, consider the query::

  my_context.Entities.Where(x=>x.SomeProp.Equals("foo"));
  
Without optimization, the generated SPARQL will contain a pattern to match all
SomeProp values of all Entity instances and a FILTER to then reduce that set to
only those where the value of SomeProp is "foo"::

  CONSTRUCT {...} WHERE {
    ?x a <http://example.org/schema/Entity> .
    ?x <http://example.org/schema/someProp> ?v0 .
    FILTER (?v0='foo')
  }

With optimization, the query instead results in this SPARQL::

  CONSTRUCT {...} WHERE {
    ?x a <http://example.org/schema/Entity> .
    ?x <http://example.org/schema/someProp> 'foo' .
  }

For BrightstarDB this second query is much more efficient.

There is a small price to pay in terms of flexibility however, in SPARQL an equality
test in a FILTER statement can apply conversion casting (so "1"^^<xsd:double> = "1.0"^^<xsd:double>),
whereas in the pattern matching the value match has to be exact. This can cause some
unexpected results if you have not consistently used the Entity Framework to create and update
the RDF data in BrightstarDB.

The optimizations are BrightstarDB-specific and so are not recommended when using the Entity Framework
to connect to generic SPARQL endpoints; or to DotNetRDF stores.

By default, filter optimization is enabled when you create an entity context with a connection
string that specifies BrightstarDB (either REST or embedded) as its target; and disabled
when you create an entity context with a connection string that specifies a SPARQL endpoint or
DotNetRDF provider as the target of the connection, or when you create an entity context by passing
in a DataObjectStore directly.

To enable or disable filter optimization from your code, you can set the ``FilterOptimizationEnabled``
property on the context class::

    my_context.FilterOptimizationEnabled = true;
    
    // This query uses filter optimization:
    my_context.Entities.Where(x=>x.SomeProp.Equals("foo"));
    
    my_context.FilterOptimizationEnabled = false;

    // This query does not use filter optimization:
    my_context.Entities.Where(x=>x.SomeProp.Equals("foo"));
    
    
.. _EF_Custom_Queries:

Custom Queries
==============

To Be Completed


.. _EF_Custom_Sparql_Protocol:

Custom SPARQL Protocol
======================

To Be completed


.. _EF_Custom_Type_Mappings:

Custom Type Mappings
====================

To be completed
