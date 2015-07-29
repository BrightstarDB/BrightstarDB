using System.Runtime.InteropServices;

namespace BrightstarDB.Server
{
#if !PORTABLE
    internal class MemoryUtils
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MemoryStatusEx()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            }
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

        private static bool _noGlobalMemoryStatusEx;

        public static uint GetMemoryLoad()
        {
            if (_noGlobalMemoryStatusEx)
            {
                // In the absence of a measure of memory usage, act conservatively
                // and return a high value. Tweaking batch size configuration parameters
                // should help mitigate performance issues
                return 100;
            }

            try
            {
                var ms = new MemoryStatusEx();
                if (GlobalMemoryStatusEx(ms))
                {
                    return ms.dwMemoryLoad;
                }
            }
            catch (System.EntryPointNotFoundException)
            {
                // Indicates we are on a Mono platform that does not currently support this native method
                _noGlobalMemoryStatusEx = true;
            }
            return 100;
        }
    }
#endif
}
