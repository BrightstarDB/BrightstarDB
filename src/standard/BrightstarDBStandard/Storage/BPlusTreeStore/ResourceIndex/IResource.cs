namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal interface IResource
    {
        byte[] GetData();
        bool Matches(string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId);

        /// <summary>
        /// Get the flag that indicates if this resource represents an RDF literal (true) or a resource (false)
        /// </summary>
        bool IsLiteral { get; }

        /// <summary>
        /// Get the embedded prefix string for the resource as stored in the BTree
        /// </summary>
        /// <remarks>Using this property rather than the <see cref="Value"/> property
        /// avoids de-referencing overhead for long values.</remarks>
        string Prefix { get; }

        /// <summary>
        /// Get the full value string for the resource
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Gets the resource ID for the data type URI
        /// </summary>
        ulong DataTypeId { get; }

        /// <summary>
        /// Gets the resource ID for the language code
        /// </summary>
        ulong LanguageCodeId { get; }
    }
}
