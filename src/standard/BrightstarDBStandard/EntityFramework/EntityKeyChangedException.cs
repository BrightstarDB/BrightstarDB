namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Class of exception raised when the key properties of an entity have been modified
    /// in a way that results in the identity of the entity no longer matching the expected key.
    /// </summary>
    public sealed class EntityKeyChangedException : EntityFrameworkException
    {
        internal EntityKeyChangedException() : base(Strings.EntityFramework_EntityKeyChanged){}
    }
}
