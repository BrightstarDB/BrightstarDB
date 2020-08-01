namespace BrightstarDB.Storage
{
    interface IPrefixManager
    {
        string MakePrefixedUri(string uri);
        string ResolvePrefixedUri(string prefixedUri);
    }
}
