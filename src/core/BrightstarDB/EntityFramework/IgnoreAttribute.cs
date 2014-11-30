using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Property attribute that marks those properties defined in an entity interface that should not
    /// be implemented in the BrightstarDB entity object. The application is responsible for providing
    /// an implementation in a partial class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class IgnoreAttribute : Attribute
    {
    }
}