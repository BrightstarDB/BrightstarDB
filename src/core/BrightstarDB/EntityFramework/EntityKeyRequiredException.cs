namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Exception raised when an attempt is made to add an entity to a context without first setting its key properties.
    /// </summary>
    /// <remarks>This type of exception will also be raised if the parameterless Create() method is called on the context entity set for
    /// an entity type that has key proeprties.</remarks>
    public sealed class EntityKeyRequiredException : EntityFrameworkException
    {
        internal EntityKeyRequiredException() : base(Strings.EntityFramework_KeyRequired){}
    }
}
