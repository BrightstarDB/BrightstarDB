#if !PORTABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BrightstarDB.Profiling
{
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

        public static uint GetMemoryLoad()
        {
            var ms = new MemoryStatusEx();
            if (GlobalMemoryStatusEx(ms))
            {
                return ms.dwMemoryLoad;
            }
            return 100;
        }
    }
}
#endif