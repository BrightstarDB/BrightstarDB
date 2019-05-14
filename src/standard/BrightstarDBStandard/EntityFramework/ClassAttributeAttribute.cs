using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Custom attribute that can be added to an interface to tell the Brightstar
    /// Entity Framework to generate a different custom attribute on the generated
    /// entity class.
    /// </summary>
    /// <remarks>The value of this custom attribute should be the full markup for the
    /// custom attribute to be added to the generated class. Because the generated class
    /// will not contain any custom namespace imports, it is important to ensure that
    /// the values specified with this attribute use the fully qualified type name
    /// for any custom attribute, otherwise the generated class will not compile.
    /// </remarks>
    /// <example>
    /// <code>
    /// [ClassAttribute("[System.ComponentModel.DisplayName(\"Person\")]")
    /// [Entity]
    /// public interface IFoafPerson {
    /// ...
    /// }
    /// </code>
    /// will result in the following generated entity code:
    /// <code>
    /// [System.ComponentModel.DisplayName("Person")]
    /// public class FoafPerson : IFoafPerson {
    /// ...
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class ClassAttributeAttribute : Attribute
    {
        /// <summary>
        /// Get the full text of the class attribute to be generated
        /// </summary>
        public string ClassAttribute { get; private set; }

        /// <summary>
        /// Defines a new class attribute to be added to the generated entity class
        /// </summary>
        /// <param name="classAttribute">The class attribute to be added.</param>
        public ClassAttributeAttribute(string classAttribute)
        {
            ClassAttribute = classAttribute;
        }
    }
}
