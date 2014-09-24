using System.Collections.Generic;

namespace BrightstarDB.Client
{
    ///<summary>
    /// DataObject provide a object centric view of data in Brightstar.
    ///</summary>
    public interface IDataObject
    {
        /// <summary>
        /// The identity of the resource that this data object wraps.
        /// </summary>
        /// <returns>The identity of the resource</returns>
        string Identity{ get; }

        /// <summary>
        /// Flag indicating if this is a new data object that will be
        /// created for the first time when the next call to 
        /// <see cref="IDataObjectStore.SaveChanges"/> completes
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        /// Flag indicating if this data object has been locally modified
        /// </summary>
        bool IsModified { get; }

        /// <summary>
        /// Sets the type of this data object
        /// </summary>
        /// <param name="type">The new data object type</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        IDataObject SetType(IDataObject type);

        /// <summary>
        /// Gets the type of this data object
        /// </summary>
        /// <returns>A list of object types</returns>
        IList<string> GetTypes();

        /// <summary>
        /// Sets the property of this object to the specified value
        /// </summary>
        /// <param name="type">The type of the property to set</param>
        /// <param name="value">The new value of the property</param>
        /// <param name="langCode">OPTIONAL : the language code for the new literal. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataObject to allow chained calls</returns>
        /// <remarks>This method will remove all existing properties of type <paramref name="type"/> from this data object
        /// and add a single replacement property of the same type with <paramref name="value"/> as the property value.</remarks>
        IDataObject SetProperty(IDataObject type, object value, string langCode = null);

        /// <summary>
        /// Sets the property of this object to the specified value
        /// </summary>
        /// <param name="type">The type of the property to set as a CURIE or URI string</param>
        /// <param name="value">The new value of the property</param>
        /// <param name="langCode">OPTIONAL: The language code for the new literal. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataObject to allow chained calls</returns>
        /// <remarks>This method will remove all existing properties of type <paramref name="type"/> from this data object
        /// and add a single replacement property of the same type with <paramref name="value"/> as the property value.</remarks>
        IDataObject SetProperty(string type, object value, string langCode = null);

        /// <summary>
        /// Adds a new property value to this object
        /// </summary>
        /// <param name="type">The type of the property to add</param>
        /// <param name="value">The value of the property</param>
        /// <param name="lang">OPTIONAL: The language code of the literal value. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataOjbect to allow chained calls</returns>
        IDataObject AddProperty(IDataObject type, object value, string lang = null);
        
        /// <summary>
        /// Adds a new property value to this object
        /// </summary>
        /// <param name="type">The type of the property to add as a CURIE or URI string</param>
        /// <param name="value">The value of the property</param>
        /// <param name="lang">OPTIONAL: The language code of the literal value. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataOjbect to allow chained calls</returns>
        IDataObject AddProperty(string type, object value, string lang = null);

        ///<summary>
        /// Removes any property on this data object with the specified type and value
        ///</summary>
        ///<param name="type">The type of the property to be removed</param>
        ///<param name="value">The value of the property to be removed</param>
        ///<param name="lang">OPTIONAL: The language code of the property to be removed. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        ///<returns>This IDataObject to allow chained calls</returns>
        ///<remarks>If this object has no matching property, then this call is a no-op</remarks>
        IDataObject RemoveProperty(IDataObject type, object value, string lang = null);

        ///<summary>
        /// Removes any property on this data object with the specified type and value
        ///</summary>
        ///<param name="type">The type of the property to be removed as a URI or CURIE</param>
        ///<param name="value">The value of the property to be removed</param>
        ///<param name="lang">OPTIONAL: The language code of the property to be removed. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        ///<returns>This IDataObject to allow chained calls</returns>
        ///<remarks>If this object has no matching property, then this call is a no-op</remarks>
        IDataObject RemoveProperty(string type, object value, string lang = null);

        ///<summary>
        /// Removes all properties of the specified type from this data object
        ///</summary>
        ///<param name="type">The type of the properties to be removed</param>
        ///<returns>This IDataObject to allow chained calls</returns>
        IDataObject RemovePropertiesOfType(IDataObject type);

        ///<summary>
        /// Removes all properties of the specified type from this data object
        ///</summary>
        ///<param name="type">The type of the properties to be removed as a URI or CURIE</param>
        ///<returns>This IDataObject to allow chained calls</returns>
        IDataObject RemovePropertiesOfType(string type);

        /// <summary>
        /// Removes properties of the specified type where this data object is the value
        /// </summary>
        /// <param name="type">The type of the properties to be removed</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        IDataObject RemoveInversePropertiesOfType(IDataObject type);

        /// <summary>
        /// Removes properties of the specified type where this data object is the value
        /// </summary>
        /// <param name="type">The type of the properties to be removed as a URI or CURIE</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        IDataObject RemoveInversePropertiesOfType(string type);

        ///<summary>
        /// Retrieves the value of the property of this data object with the specified property type
        ///</summary>
        ///<param name="type">The property type</param>
        ///<returns>The value of the first property of the specified type or null if no match was found</returns>
        object GetPropertyValue(IDataObject type);

        ///<summary>
        /// Retrieves the value of the property of this data object with the specified property type
        ///</summary>
        ///<param name="type">The property type as a URI or CURIE</param>
        ///<returns>The value of the first property of the specified type or null if no match was found</returns>
        object GetPropertyValue(string type);

        /// <summary>
        /// Retrieves the values of all properties of this data object with the specified property type
        /// </summary>
        /// <param name="type">The property type as a URI or CURIE</param>
        /// <returns>An enumeration of the values of all properties of the specified type.</returns>
        IEnumerable<object> GetPropertyValues(string type);

        /// <summary>
        /// Retrieves the values of all properties of this data object with the specified property type
        /// </summary>
        /// <param name="type">The property type</param>
        /// <returns>An enumeration of the values of all properties of the specified type.</returns>
        IEnumerable<object> GetPropertyValues(IDataObject type);

        /// <summary>
        /// Returns an enumerator over the distinct property types of the properties
        /// that this data object has.
        /// </summary>
        /// <returns>An enumeration of IDataObject instances representing the distinct property types of all properties of this object.</returns>
        IEnumerable<IDataObject> GetPropertyTypes();

        ///<summary>
        /// Returns all data objects that have a property of the specified type where
        /// the property value is this data object
        ///</summary>
        ///<param name="type">The property type</param>
        ///<returns>An enumeration of data object values</returns>
        IEnumerable<IDataObject> GetInverseOf(IDataObject type);

        ///<summary>
        /// Returns all data objects that have a property of the specified type where
        /// the property value is this data object
        ///</summary>
        ///<param name="type">The property type as a CURIE or URI</param>
        ///<returns>An enumeration of data object values</returns>
        IEnumerable<IDataObject> GetInverseOf(string type);

        /// <summary>
        /// Removes this data object from the store
        /// </summary>
        void Delete();

        /// <summary>
        /// Change the URI identifier for this data object.
        /// </summary>
        /// <remarks>This change will update all triples where the data object identity
        /// is the subject or object. It will not change predicates.</remarks>
        /// <param name="newIdentity">The new URI identifier</param>
        /// <param name="enforceClassUniqueConstraint">Add an update precondition to ensure that the update will fail if the store already
        /// contains an RDF resource with the same rdf:type(s) as this data object.</param>
        IDataObject UpdateIdentity(string newIdentity, bool enforceClassUniqueConstraint);

    }
}
