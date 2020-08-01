namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Enumeration that specifies how a .NET property is mapped to the underlying RDF data model
    /// </summary>
    public enum PropertyMappingType
    {
        /// <summary>
        /// The value of the .NET property is a resource address
        /// </summary>
        Address = 0,
        /// <summary>
        /// The value of the .NET property is a resource identifier which needs to be resolved against a base identifier to generate the RDF resource address
        /// </summary>
        Id = 1,
        /// <summary>
        /// The value of the .NET property is an rdf:label of a resource
        /// </summary>
        Label = 2,
        /// <summary>
        /// The value of the .NET property is the value of an RDF property of a resource that has a literal value
        /// </summary>
        Property = 3,
        /// <summary>
        /// The value of the .NET property is the value of an RDF property of a resource that has a resource address value
        /// </summary>
        Arc = 4,

        /// <summary>
        /// The value of the .NET property is the value of the inverse relationship from an RDF statement object to its subject
        /// </summary>
        InverseArc = 5,
    }
}
