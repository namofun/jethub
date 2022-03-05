using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace Xylab.Management.Interop
{
    internal partial class Libc
    {
        public enum sysconf_name_t
        {
            _SC_CLK_TCK = 2,
            _SC_GETPW_R_SIZE_MAX = 70,
        }

        [DllImport(libc_so_6)]
        public static extern long sysconf(sysconf_name_t name);

        private static long s_ticksPerSecond;

        public static TimeSpan TicksToTimeSpan(double ticks)
        {
            long ticksPerSecond = Volatile.Read(ref s_ticksPerSecond);
            if (ticksPerSecond == 0)
            {
                // Look up the number of ticks per second in the system's configuration,
                // then use that to convert to a TimeSpan
                ticksPerSecond = sysconf(sysconf_name_t._SC_CLK_TCK);
                if (ticksPerSecond <= 0) throw new Win32Exception();

                Volatile.Write(ref s_ticksPerSecond, ticksPerSecond);
            }

            return TimeSpan.FromSeconds(ticks / (double)ticksPerSecond);
        }
    }
}
