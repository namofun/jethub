using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JetHub.Interop
{
    public partial class Libc
    {
        private const string libc_so_6 = @"/lib/x86_64-linux-gnu/libc.so.6";
        private const int FSHIFT = 11;
        private const int FIXED_1 = 1 << FSHIFT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LOAD_INT(ulong x) => x >> FSHIFT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LOAD_FRAC(ulong x) => LOAD_INT((x & (FIXED_1 - 1)) * 100);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double sysinfo_loads_to100(ulong x)
            => LOAD_INT((x >> 5) + (FIXED_1 / 200))
            + LOAD_FRAC((x >> 5) + (FIXED_1 / 200)) / 100.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[] sysinfo_loads_to100(ulong[] x)
            => x.Select(sysinfo_loads_to100).ToArray();

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
