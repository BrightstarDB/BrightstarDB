// -----------------------------------------------------------------------
// <autogenerated>
//    This code was generated from a template.
// 
//    Changes to this file may cause incorrect behaviour and will be lost
//    if the code is regenerated.
// </autogenerated>
// ------------------------------------------------------------------------
namespace CommitPointsCore
{
    [System.CodeDom.Compiler.GeneratedCode("BrightstarDB.CodeGeneration", "0.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    public partial class EntityContext : global::BrightstarDB.EntityFramework.BrightstarEntityContext
    {
        static EntityContext()
        {
            var provider = new global::BrightstarDB.EntityFramework.ReflectionMappingProvider();
        }

        public EntityContext()
        {
            this.InitializeContext();
        }

        public EntityContext(global::BrightstarDB.Client.IDataObjectStore dataObjectStore) : base(dataObjectStore)
        {
            this.InitializeContext();
        }

        public EntityContext(string updateGraphUri, global::System.Collections.Generic.IEnumerable<global::System.String> datasetGraphUris, string versionGraphUri) : base(updateGraphUri, datasetGraphUris, versionGraphUri)
        {
            this.InitializeContext();
        }

        public EntityContext(string connectionString, global::System.Boolean? enableOptimisticLocking = null, string updateGraphUri = null, global::System.Collections.Generic.IEnumerable<global::System.String> datasetGraphUris = null, string versionGraphUri = null) : base(connectionString, enableOptimisticLocking, updateGraphUri, datasetGraphUris, versionGraphUri)
        {
            this.InitializeContext();
        }

        private void InitializeContext()
        {
        }

        public global::BrightstarDB.EntityFramework.IEntitySet<T> EntitySet<T>()
            where T : class
        {
            global::System.Type type = typeof(T);
            throw new global::System.InvalidOperationException(((typeof(T)).FullName) + (" is not a recognized entity interface type."));
        }
    }
}