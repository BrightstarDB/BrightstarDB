// This file is included in your project to ensure that
// the reference to the runtime library for your project
// is not stripped out by the build process.

namespace BrightstarDB.Portable.Android.Tests
{
    public static class BrightstarDBForceReference
    {
        static BrightstarDBForceReference()
        {
            ConfigurationProvider dummy = null;
        }
    }
}