using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Assembly attribute that indicates that entity classes in this assembly should be generated with a default accessibility of internal.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class GenerateInternalEntityClassesAttribute : Attribute
    {
    }
}
