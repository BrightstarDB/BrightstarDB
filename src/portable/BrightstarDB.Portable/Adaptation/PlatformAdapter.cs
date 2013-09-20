using System;

namespace BrightstarDB.Portable.Adaptation
{
    internal static class PlatformAdapter
    {
        private static readonly string[] KnownPlatforms = new[] {"Desktop", "Silverlight", "Phone", "Store"};
        private static readonly IAdapterResolver _resolver = new ProbingAdapterResolver(KnownPlatforms );

        public static T Resolve<T>()
        {
            T value = (T) _resolver.Resolve(typeof (T));
            if (value == null) throw new PlatformNotSupportedException("This API is not supported on this platform.");
            return value;
        } 
    }
}
