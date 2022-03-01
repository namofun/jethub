using System.Runtime.InteropServices;

namespace JetHub.Interop
{
    public partial class Libc
    {
        private const string libc_so_6 = @"/lib/x86_64-linux-gnu/libc.so.6";

        [DllImport(libc_so_6)]
        public static extern int sysinfo(out sysinfo_t info);

        [StructLayout(LayoutKind.Sequential)]
        public struct sysinfo_t
        {
            public long uptime;    /* Seconds since boot */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public ulong[] loads;   /* 1, 5, and 15 minute load averages */

            public ulong totalram;  /* Total usable main memory size */
            public ulong freeram;   /* Available memory size */
            public ulong sharedram; /* Amount of shared memory */
            public ulong bufferram; /* Memory used by buffers */
            public ulong totalswap; /* Total swap space size */
            public ulong freeswap;  /* swap space still available */
            public ushort procs;    /* Number of current processes */
            public ushort pad;      /* Explicit padding for m68k */
            public ulong totalhigh; /* Total high memory size */
            public ulong freehigh;  /* Available high memory size */
            public uint mem_unit;   /* Memory unit size in bytes */

            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0)]
            // public char[] _f;       /* Padding: libc5 uses this.. */
        }
    }
}
