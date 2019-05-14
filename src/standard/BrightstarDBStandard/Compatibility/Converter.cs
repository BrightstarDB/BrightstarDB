namespace BrightstarDB.Portable.Compatibility
{
    public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
}
