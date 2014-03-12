namespace BrightstarDB.Compatibility
{
    public interface IPathSeparatorProvider
    {
        char DirectorySeparator { get; }
        char AltDirectorySeparator { get; }
        char VolumeSeparator { get; }
    }
}
