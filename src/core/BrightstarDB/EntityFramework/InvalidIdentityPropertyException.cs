using System;
using System.Reflection;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Class of exception raised by the EntityFramework when processing a property that has been identified
    /// as the identity property for the entity but which does not meet the requirements for being an identity property.
    /// </summary>
    /// <remarks>
    /// <para>An identity property must be a property of type System.String with only a getter. An entity class has only one
    /// identity property (it can also have no identity properties). The identity property is identified by using these rules
    /// in the order given below.</para>
    /// <list type="numbered">
    /// <item>If a property is marked with the <see cref="IdentifierAttribute"/> attribute</item>
    /// <item>If the property name is the same as the interface or class name without the leading "I" and with "Id" at the end. E.g. if the interface is IPerson, the property with the name "PersonId".</item>
    /// <item>If the property name is the same as the interface or class name without the leading "I" and with "ID" at the end. E.g. if the interface is IPerson, the property with the name "PersonID".</item>
    /// <item>If the property name is "Id".</item>
    /// <item>If the property name is "ID".</item>
    /// </list>
    /// <para>The rules are matched in that order, so if the IPerson interface has a property named PersonId and another named Id, the PersonId property would be chosen as the identity property and the Id property
    /// would be processed as just a normal property.</para>
    /// <para>The property which matches these rules and is chosen as the identity property MUST be a read-only string property. If it is not, this exception is raised.</para>
    /// <para>The easiest way to avoid this exception is to always explicitly declare an identity property using the <see cref="IdentifierAttribute"/> attribute. E.g.</para>
    /// <example>
    /// public interface IPerson 
    /// {
    ///     // This property is always chosen as the identity property because of the attribute declaration. 
    ///     // Without the attribute, the PersonId property would have been chosen as the identity attribute
    ///     // and would result in an InvalidPropertyException because it has a setter.
    ///     [ResourceAddress]
    ///     string Id {get; } 
    /// 
    ///     string PersonId {get;set;}
    /// }
    /// </example>
    /// </remarks>
    [DoNotObfuscateType, DoNotPruneType]
    public sealed class InvalidIdentityPropertyException : EntityFrameworkException
    {
        internal InvalidIdentityPropertyException(PropertyInfo invalidProperty) : base(
            String.Format(Strings.InvalidPropertyMessage, invalidProperty.Name, invalidProperty.DeclaringType.FullName))
        {           
        }
    }
}
